# Network/routes/network.py
from __future__ import annotations

import os
from ipaddress import ip_address, ip_network

from fastapi import APIRouter, Depends, HTTPException, Request, status

from Network.network.models import EnrollmentRequest, EnrollmentResponse, NetworkStatus, PeerInfo
from Network.network.wireguard import WireGuardManager

router = APIRouter(prefix="/api/network", tags=["network"])


def get_current_user_id(request: Request) -> str:
    uid = request.headers.get("x-user-id")
    if not uid:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing auth (x-user-id)",
        )
    return uid


def get_wg_manager(request: Request) -> WireGuardManager:
    mgr = getattr(request.app.state, "wg_manager", None)
    if not mgr:
        raise HTTPException(status_code=500, detail="WireGuard manager not initialized")
    return mgr


def _parse_trusted_cidrs() -> list:
    raw = (os.getenv("NETWORK_TRUSTED_CIDRS") or "").strip()
    if not raw:
        raw = "10.0.0.0/8,172.16.0.0/12,192.168.0.0/16"
    cidrs = []
    for part in raw.split(","):
        part = part.strip()
        if part:
            cidrs.append(ip_network(part, strict=False))
    return cidrs


def _get_client_ip(request: Request) -> str:
    trust_proxy = (os.getenv("NETWORK_TRUST_PROXY") or "0").strip() == "1"
    if trust_proxy:
        xff = (request.headers.get("x-forwarded-for") or "").strip()
        if xff:
            return xff.split(",")[0].strip()
        xri = (request.headers.get("x-real-ip") or "").strip()
        if xri:
            return xri

    if request.client and request.client.host:
        return request.client.host
    return ""


def require_trusted_network(request: Request) -> None:
    ip_s = _get_client_ip(request)
    if not ip_s:
        raise HTTPException(status_code=403, detail="Untrusted network (no client ip)")

    try:
        ip = ip_address(ip_s)
    except Exception:
        raise HTTPException(status_code=403, detail="Untrusted network (bad client ip)")

    for net in _parse_trusted_cidrs():
        if ip in net:
            return

    raise HTTPException(status_code=403, detail="Untrusted network")


@router.get("/status", response_model=NetworkStatus)
def network_status(
    _: None = Depends(require_trusted_network),
    user_id: str = Depends(get_current_user_id),
    wg: WireGuardManager = Depends(get_wg_manager),
):
    peers = wg.list_peers()
    peer = next((p for p in peers if p.user_id == user_id and not p.revoked_at), None)
    return NetworkStatus(**wg.status_for_peer(peer=peer))


@router.get("/peers/self", response_model=PeerInfo)
def peer_self(
    _: None = Depends(require_trusted_network),
    user_id: str = Depends(get_current_user_id),
    wg: WireGuardManager = Depends(get_wg_manager),
):
    peers = wg.list_peers()
    peer = next((p for p in peers if p.user_id == user_id and not p.revoked_at), None)
    if not peer:
        raise HTTPException(status_code=404, detail="No active peer for user")
    return peer


@router.post("/enroll", response_model=EnrollmentResponse)
def enroll(
    payload: EnrollmentRequest,
    _: None = Depends(require_trusted_network),  
    user_id: str = Depends(get_current_user_id),
    wg: WireGuardManager = Depends(get_wg_manager),
):
    try:
        return wg.enroll(actor_user_id=user_id, req=payload)
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
