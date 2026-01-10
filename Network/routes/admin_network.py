# Network/routes/admin_network.py
from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, Request, status

from Network.network.models import AdminPeerPatch, InviteCreate, InviteInfo, PeerInfo
from Network.network.wireguard import WireGuardManager
from Network.routes.network import require_trusted_network

router = APIRouter(prefix="/api/admin/network", tags=["admin-network"])


def get_current_user_id(request: Request) -> str:
    uid = request.headers.get("x-user-id")
    if not uid:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Missing auth (x-user-id)")
    return uid


def require_admin(request: Request) -> None:
    role = request.headers.get("x-user-role", "")
    if role.lower() not in ("admin", "owner"):
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Admin role required")


def require_admin_dep(
    request: Request,
    _: None = Depends(require_trusted_network),
) -> None:
    require_admin(request)


def get_wg_manager(request: Request) -> WireGuardManager:
    mgr = getattr(request.app.state, "wg_manager", None)
    if not mgr:
        raise HTTPException(status_code=500, detail="WireGuard manager not initialized")
    return mgr


@router.post("/invites", response_model=InviteInfo)
def create_invite(
    payload: InviteCreate,
    __: None = Depends(require_admin_dep),
    _actor_user_id: str = Depends(get_current_user_id),
    wg: WireGuardManager = Depends(get_wg_manager),
):
    return wg.create_invite(user_id=payload.user_id, device_id=payload.device_id, ttl_seconds=payload.ttl_seconds)


@router.get("/peers", response_model=list[PeerInfo])
def list_peers(
    __: None = Depends(require_admin_dep),
    _actor_user_id: str = Depends(get_current_user_id),
    wg: WireGuardManager = Depends(get_wg_manager),
):
    return wg.list_peers()


@router.post("/peers/{peer_id}/revoke", response_model=PeerInfo)
def revoke_peer(
    peer_id: str,
    __: None = Depends(require_admin_dep),
    actor_user_id: str = Depends(get_current_user_id),
    wg: WireGuardManager = Depends(get_wg_manager),
):
    try:
        return wg.revoke_peer(actor_user_id=actor_user_id, peer_id=peer_id)
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.patch("/peers/{peer_id}", response_model=PeerInfo)
def patch_peer(
    peer_id: str,
    payload: AdminPeerPatch,
    __: None = Depends(require_admin_dep),
    actor_user_id: str = Depends(get_current_user_id),
    wg: WireGuardManager = Depends(get_wg_manager),
):
    try:
        return wg.update_peer(
            actor_user_id=actor_user_id,
            peer_id=peer_id,
            allowed_ips=payload.allowed_ips,
            tags=payload.tags,
        )
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
