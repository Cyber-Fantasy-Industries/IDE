from __future__ import annotations

from datetime import datetime
from ipaddress import ip_network
from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field, field_validator


class DeviceIdentity(BaseModel):
    device_id: str = Field(..., min_length=3, max_length=128)
    fingerprint: Optional[str] = Field(default=None, max_length=512)
    platform: Optional[str] = Field(default=None, max_length=64)
    client_version: Optional[str] = Field(default=None, max_length=64)


class EnrollmentRequest(BaseModel):
    invite_code: str = Field(..., min_length=6, max_length=256)
    device: DeviceIdentity
    # Optional: Wenn du client-side keys willst, kann der Client seinen public key liefern
    client_public_key: Optional[str] = Field(default=None, max_length=128)


class EnrollmentResponse(BaseModel):
    peer_id: str
    overlay_ip: str
    client_config_text: str
    server_endpoint: str
    expires_at: Optional[datetime] = None


class PeerInfo(BaseModel):
    peer_id: str
    user_id: str
    device_id: str
    overlay_ip: str
    allowed_ips: List[str] = Field(default_factory=list)
    public_key: str
    created_at: datetime
    revoked_at: Optional[datetime] = None
    tags: Dict[str, str] = Field(default_factory=dict)

    @field_validator("allowed_ips", mode="before")
    @classmethod
    def _validate_allowed_ips(cls, v: Any) -> Any:
        if v is None:
            return v
        for cidr in v:
            try:
                ip_network(cidr, strict=False)
            except Exception as e:
                raise ValueError(f"Invalid CIDR in allowed_ips: {cidr}") from e
        return v


class NetworkStatus(BaseModel):
    connected: bool
    server_endpoint: str
    overlay_ip: Optional[str] = None
    peer_seen_at: Optional[datetime] = None


class AdminPeerCreate(BaseModel):
    user_id: str = Field(..., min_length=1, max_length=128)
    device_id: str = Field(..., min_length=3, max_length=128)
    # optional: admin pre-binds to existing device record
    client_public_key: Optional[str] = Field(default=None, max_length=128)
    allowed_ips: List[str] = Field(default_factory=list)
    tags: Dict[str, str] = Field(default_factory=dict)

    @field_validator("allowed_ips", mode="before")
    @classmethod
    def _validate_allowed_ips(cls, v: Any) -> Any:
        if v is None:
            return v
        for cidr in v:
            try:
                ip_network(cidr, strict=False)
            except Exception as e:
                raise ValueError(f"Invalid CIDR in allowed_ips: {cidr}") from e
        return v


class AdminPeerPatch(BaseModel):
    allowed_ips: Optional[List[str]] = None
    tags: Optional[Dict[str, str]] = None

    @field_validator("allowed_ips", mode="before")
    @classmethod
    def _validate_allowed_ips(cls, v: Any) -> Any:
        if v is None:
            return v
        for cidr in v:
            try:
                ip_network(cidr, strict=False)
            except Exception as e:
                raise ValueError(f"Invalid CIDR in allowed_ips: {cidr}") from e
        return v


class InviteCreate(BaseModel):
    user_id: str = Field(..., min_length=1, max_length=128)
    device_id: Optional[str] = Field(default=None, max_length=128)
    ttl_seconds: int = Field(default=900, ge=60, le=86400)


class InviteInfo(BaseModel):
    invite_code: str
    user_id: str
    device_id: Optional[str] = None
    created_at: datetime
    expires_at: datetime
    used_at: Optional[datetime] = None


class AuditEvent(BaseModel):
    ts: datetime
    actor_user_id: Optional[str] = None
    action: str
    subject: Optional[str] = None
    meta: Dict[str, Any] = Field(default_factory=dict)
