from __future__ import annotations

import os
from types import SimpleNamespace
from pathlib import Path
from typing import Optional

from dotenv import load_dotenv, find_dotenv
from loguru import logger

from Network.network.store import InMemoryNetworkStore
from Network.network.wireguard import WireGuardConfig, WireGuardManager


_runtime: SimpleNamespace | None = None


def _load_env() -> str | None:
    """
    Lädt .env deterministisch:
    1) NETWORK_ENV_PATH (expliziter Pfad)
    2) Kandidaten /app/.env, CWD, Parent
    3) fallback: find_dotenv(usecwd=True)
    """
    explicit = (os.getenv("NETWORK_ENV_PATH") or "").strip()
    candidates: list[str] = []

    if explicit:
        candidates.append(explicit)

    candidates += [
        "/app/.env",
        str(Path.cwd() / ".env"),
        str(Path.cwd().parent / ".env"),
    ]

    for p in candidates:
        try:
            if p and Path(p).is_file():
                load_dotenv(p, override=False)
                return str(Path(p).resolve())
        except Exception:
            pass

    try:
        found = find_dotenv(usecwd=True)
        if found:
            load_dotenv(found, override=False)
            return str(Path(found).resolve())
    except Exception:
        pass

    return None


def _env_int(name: str, default: int) -> int:
    raw = (os.getenv(name) or "").strip()
    if not raw:
        return default
    try:
        return int(raw)
    except ValueError:
        return default


async def ensure_runtime() -> SimpleNamespace:
    global _runtime
    if _runtime is not None:
        return _runtime

    env_path = _load_env()
    env_name = (os.getenv("NETWORK_ENV_NAME") or "").strip()  # z.B. "net.prod.env"

    if env_path:
        logger.info("🔧 [NetworkBootstrap] .env geladen: {}", env_path)
    else:
        if env_name:
            logger.info(
                "ℹ️ [NetworkBootstrap] Keine .env gefunden (ok) – nutze Prozess-ENV (env_file={}).",
                env_name,
            )
        else:
            logger.info(
                "ℹ️ [NetworkBootstrap] Keine .env gefunden (ok) – nutze Prozess-ENV (z.B. docker-compose env_file)."
            )

    # ---- WireGuard ENV ----
    wg_subnet = os.getenv("WG_SUBNET", "10.77.0.0/16")
    wg_server_overlay_ip = os.getenv("WG_SERVER_OVERLAY_IP", "10.77.0.10")
    wg_server_public_key = (os.getenv("WG_SERVER_PUBLIC_KEY") or "").strip()
    wg_server_endpoint = (os.getenv("WG_SERVER_ENDPOINT") or "").strip()
    wg_dns = (os.getenv("WG_DNS") or "").strip() or None
    wg_keepalive = _env_int("WG_KEEPALIVE", 25)

    if not wg_server_public_key or not wg_server_endpoint:
        logger.warning(
            "⚠️ [NetworkBootstrap] WireGuard unvollständig: WG_SERVER_PUBLIC_KEY/WG_SERVER_ENDPOINT fehlen. "
            "Enroll liefert dann nur Platzhalter-Config."
        )

    store = InMemoryNetworkStore()
    cfg = WireGuardConfig(
        wg_subnet=wg_subnet,
        wg_server_overlay_ip=wg_server_overlay_ip,
        wg_server_public_key=wg_server_public_key or "CHANGE_ME",
        wg_server_endpoint=wg_server_endpoint or "YOUR_STATIC_IP:51820",
        wg_dns=wg_dns,
        wg_persistent_keepalive=wg_keepalive,
        default_allowed_ips=[wg_subnet],
    )
    mgr = WireGuardManager(store=store, cfg=cfg)

    _runtime = SimpleNamespace(
        wg_store=store,
        wg_manager=mgr,
        wg_cfg=cfg,
    )

    logger.info("🛡️ [NetworkBootstrap] Runtime ready (subnet={}, endpoint={})", wg_subnet, cfg.wg_server_endpoint)
    return _runtime
