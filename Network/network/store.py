# backend/network/store.py
from __future__ import annotations

from abc import ABC, abstractmethod
from datetime import datetime, timezone
from typing import Dict, List, Optional

from .models import AuditEvent, InviteInfo, PeerInfo


def utcnow() -> datetime:
    return datetime.now(timezone.utc)


class NetworkStore(ABC):
    @abstractmethod
    def save_peer(self, peer: PeerInfo) -> None: ...

    @abstractmethod
    def get_peer(self, peer_id: str) -> Optional[PeerInfo]: ...

    @abstractmethod
    def list_peers(self) -> List[PeerInfo]: ...

    @abstractmethod
    def revoke_peer(self, peer_id: str, *, ts: Optional[datetime] = None) -> Optional[PeerInfo]: ...

    @abstractmethod
    def update_peer(self, peer_id: str, *, allowed_ips: Optional[List[str]] = None, tags: Optional[Dict[str, str]] = None) -> Optional[PeerInfo]: ...

    @abstractmethod
    def find_peer_by_user_device(self, user_id: str, device_id: str) -> Optional[PeerInfo]: ...

    @abstractmethod
    def save_invite(self, invite: InviteInfo) -> None: ...

    @abstractmethod
    def get_invite(self, invite_code: str) -> Optional[InviteInfo]: ...

    @abstractmethod
    def consume_invite(self, invite_code: str) -> Optional[InviteInfo]: ...

    @abstractmethod
    def append_audit(self, event: AuditEvent) -> None: ...

    @abstractmethod
    def list_audit(self, limit: int = 200) -> List[AuditEvent]: ...


class InMemoryNetworkStore(NetworkStore):
    def __init__(self) -> None:
        self._peers: Dict[str, PeerInfo] = {}
        self._invites: Dict[str, InviteInfo] = {}
        self._audit: List[AuditEvent] = []

    def save_peer(self, peer: PeerInfo) -> None:
        self._peers[peer.peer_id] = peer

    def get_peer(self, peer_id: str) -> Optional[PeerInfo]:
        return self._peers.get(peer_id)

    def list_peers(self) -> List[PeerInfo]:
        return list(self._peers.values())

    def revoke_peer(self, peer_id: str, *, ts: Optional[datetime] = None) -> Optional[PeerInfo]:
        p = self._peers.get(peer_id)
        if not p:
            return None
        if p.revoked_at:
            return p
        p = p.model_copy(update={"revoked_at": ts or utcnow()})
        self._peers[peer_id] = p
        return p

    def update_peer(self, peer_id: str, *, allowed_ips: Optional[List[str]] = None, tags: Optional[Dict[str, str]] = None) -> Optional[PeerInfo]:
        p = self._peers.get(peer_id)
        if not p:
            return None
        upd = {}
        if allowed_ips is not None:
            upd["allowed_ips"] = allowed_ips
        if tags is not None:
            upd["tags"] = tags
        p = p.model_copy(update=upd)
        self._peers[peer_id] = p
        return p

    def find_peer_by_user_device(self, user_id: str, device_id: str) -> Optional[PeerInfo]:
        for p in self._peers.values():
            if p.user_id == user_id and p.device_id == device_id:
                return p
        return None

    def save_invite(self, invite: InviteInfo) -> None:
        self._invites[invite.invite_code] = invite

    def get_invite(self, invite_code: str) -> Optional[InviteInfo]:
        return self._invites.get(invite_code)

    def consume_invite(self, invite_code: str) -> Optional[InviteInfo]:
        inv = self._invites.get(invite_code)
        if not inv:
            return None
        if inv.used_at:
            return None
        if inv.expires_at <= utcnow():
            return None
        inv = inv.model_copy(update={"used_at": utcnow()})
        self._invites[invite_code] = inv
        return inv

    def append_audit(self, event: AuditEvent) -> None:
        self._audit.append(event)
        # keep bounded
        if len(self._audit) > 5000:
            self._audit = self._audit[-5000:]

    def list_audit(self, limit: int = 200) -> List[AuditEvent]:
        return list(reversed(self._audit[-limit:]))
