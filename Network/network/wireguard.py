# backend/network/wireguard.py
from __future__ import annotations

import base64
import os
import secrets
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from ipaddress import ip_network
from typing import List, Optional, Tuple

from .models import AuditEvent, EnrollmentRequest, EnrollmentResponse, InviteInfo, PeerInfo
from .store import NetworkStore, utcnow


def _rand_code(n: int = 18) -> str:
    # URL-safe invite codes
    return secrets.token_urlsafe(n)


def _b64(nbytes: int) -> str:
    return base64.b64encode(os.urandom(nbytes)).decode("ascii").strip()


# client generates keys; backend never generates/holds private keys


@dataclass(frozen=True)
class WireGuardConfig:
    wg_subnet: str                 # e.g. "10.77.0.0/16"
    wg_server_overlay_ip: str      # e.g. "10.77.0.10"
    wg_server_public_key: str      # server pubkey for client config
    wg_server_endpoint: str        # "your.static.ip:51820"
    wg_server_port: int = 51820
    wg_dns: Optional[str] = None   # e.g. "1.1.1.1"
    wg_persistent_keepalive: int = 25
    default_allowed_ips: Optional[List[str]] = None  # e.g. ["10.77.0.0/16"]

    def __post_init__(self):
        if self.default_allowed_ips is None:
            object.__setattr__(self, "default_allowed_ips", [self.wg_subnet])


class WireGuardManager:
    def __init__(self, *, store: NetworkStore, cfg: WireGuardConfig) -> None:
        self._store = store
        self._cfg = cfg
        self._pool = ip_network(cfg.wg_subnet, strict=False)
        self._server_ip = cfg.wg_server_overlay_ip

    # ----------------- Invites -----------------
    def create_invite(self, *, user_id: str, device_id: Optional[str], ttl_seconds: int) -> InviteInfo:
        now = utcnow()
        inv = InviteInfo(
            invite_code=_rand_code(),
            user_id=user_id,
            device_id=device_id,
            created_at=now,
            expires_at=now + timedelta(seconds=ttl_seconds),
            used_at=None,
        )
        self._store.save_invite(inv)
        self._store.append_audit(AuditEvent(ts=now, actor_user_id=user_id, action="network.invite.create", subject=inv.invite_code, meta={"device_id": device_id, "ttl": ttl_seconds}))
        return inv

    # ----------------- Enrollment -----------------
    def enroll(self, *, actor_user_id: str, req: EnrollmentRequest) -> EnrollmentResponse:
        inv = self._store.consume_invite(req.invite_code)
        if not inv:
            raise ValueError("Invalid/expired/used invite_code")

        # Optional: if invite was bound to a device_id, enforce it
        if inv.device_id and inv.device_id != req.device.device_id:
            raise ValueError("Invite is bound to a different device_id")

        # Idempotency: if peer already exists for user+device, return that config again
        existing = self._store.find_peer_by_user_device(inv.user_id, req.device.device_id)
        if existing and not existing.revoked_at:
            cfg_text = self._render_client_config(existing.public_key, existing.overlay_ip)
            self._store.append_audit(AuditEvent(ts=utcnow(), actor_user_id=actor_user_id, action="network.enroll.reuse", subject=existing.peer_id, meta={"user_id": inv.user_id, "device_id": req.device.device_id}))
            return EnrollmentResponse(
                peer_id=existing.peer_id,
                overlay_ip=existing.overlay_ip,
                client_config_text=cfg_text,
                server_endpoint=self._cfg.wg_server_endpoint,
                expires_at=None,
            )

        if not req.client_public_key:
            raise ValueError("client_public_key required (client generates keys locally)")
        peer_pub = req.client_public_key
        overlay_ip = self._alloc_overlay_ip()
        peer_id = f"wg_{secrets.token_hex(8)}"
        allowed_ips = list(self._cfg.default_allowed_ips or [self._cfg.wg_subnet])
        peer = PeerInfo(
            peer_id=peer_id,
            user_id=inv.user_id,
            device_id=req.device.device_id,
            overlay_ip=overlay_ip,
            allowed_ips=allowed_ips,
            public_key=peer_pub,
            created_at=utcnow(),
            revoked_at=None,
            tags={k: v for k, v in (req.device.model_dump(exclude_none=True)).items() if isinstance(v, str)},
        )
        self._store.save_peer(peer)

        self._store.append_audit(AuditEvent(
            ts=utcnow(),
            actor_user_id=actor_user_id,
            action="network.enroll",
            subject=peer_id,
            meta={"user_id": inv.user_id, "device_id": req.device.device_id, "overlay_ip": overlay_ip},
        ))

        # Note: We do NOT return peer_priv in MVP (client-side keys recommended).
        cfg_text = self._render_client_config(peer_pub, overlay_ip)
        return EnrollmentResponse(
            peer_id=peer.peer_id,
            overlay_ip=peer.overlay_ip,
            client_config_text=cfg_text,
            server_endpoint=self._cfg.wg_server_endpoint,
            expires_at=None,
        )

    # ----------------- Peer admin -----------------
    def list_peers(self) -> List[PeerInfo]:
        return self._store.list_peers()

    def revoke_peer(self, *, actor_user_id: str, peer_id: str) -> PeerInfo:
        p = self._store.revoke_peer(peer_id)
        if not p:
            raise KeyError("peer not found")
        self._store.append_audit(AuditEvent(ts=utcnow(), actor_user_id=actor_user_id, action="network.peer.revoke", subject=peer_id, meta={}))
        return p

    def update_peer(self, *, actor_user_id: str, peer_id: str, allowed_ips: Optional[List[str]] = None, tags: Optional[dict] = None) -> PeerInfo:
        p = self._store.update_peer(peer_id, allowed_ips=allowed_ips, tags=tags)
        if not p:
            raise KeyError("peer not found")
        self._store.append_audit(AuditEvent(ts=utcnow(), actor_user_id=actor_user_id, action="network.peer.update", subject=peer_id, meta={"allowed_ips": allowed_ips is not None, "tags": tags is not None}))
        return p

    # ----------------- Status -----------------
    def status_for_peer(self, *, peer: Optional[PeerInfo]) -> dict:
        return {
            "connected": bool(peer and not peer.revoked_at),
            "server_endpoint": self._cfg.wg_server_endpoint,
            "overlay_ip": peer.overlay_ip if peer else None,
            "peer_seen_at": None,  # can be filled later from telemetry/heartbeats
        }

    # ----------------- Internals -----------------
    def _alloc_overlay_ip(self) -> str:
        """
        Simple allocator: sequential scan for a free host address.
        Reserve server ip and network/broadcast.
        """
        used = {p.overlay_ip for p in self._store.list_peers() if not p.revoked_at}
        used.add(self._server_ip)

        for host in self._pool.hosts():
            ip = str(host)
            if ip in used:
                continue
            return ip
        raise RuntimeError("Overlay IP pool exhausted")

    def _render_client_config(self, peer_public_key: str, overlay_ip: str) -> str:
        """
        Minimal client config. In a full setup you'd also provide:
        - DNS
        - PostUp routing docs
        - per-role allowed IP sets
        """
        dns_line = f"DNS = {self._cfg.wg_dns}\n" if self._cfg.wg_dns else ""
        # NOTE: We are not embedding a private key (client should generate it).
        return (
            "[Interface]\n"
            f"Address = {overlay_ip}/32\n"
            f"{dns_line}"
            "\n"
            "[Peer]\n"
            f"PublicKey = {self._cfg.wg_server_public_key}\n"
            f"Endpoint = {self._cfg.wg_server_endpoint}\n"
            f"AllowedIPs = {', '.join(self._cfg.default_allowed_ips or [self._cfg.wg_subnet])}\n"
            f"PersistentKeepalive = {self._cfg.wg_persistent_keepalive}\n"
        )
