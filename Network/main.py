# Network/main.py 
from __future__ import annotations

import os
import sys
from contextlib import asynccontextmanager
from time import perf_counter
from uuid import uuid4

from fastapi import FastAPI, Request
from loguru import logger

from Network import bootstrap
from Network.routes.network import router as network_router
from Network.routes.admin_network import router as admin_network_router


LOG_LEVEL = os.getenv("NETWORK_LOG_LEVEL", os.getenv("GATEWAY_LOG_LEVEL", "INFO"))
logger.remove()
logger.add(sys.stderr, level=LOG_LEVEL)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("🚀 [NetworkService] Start – bootstrap…")
    rt = await bootstrap.ensure_runtime()

    app.state.wg_manager = rt.wg_manager
    app.state.wg_store = rt.wg_store
    app.state.wg_cfg = rt.wg_cfg

    yield
    logger.info("🧹 [NetworkService] Shutdown.")


app = FastAPI(
    lifespan=lifespan,
    title="Gateway Network API",
    version="1.0",
)

app.include_router(network_router)
app.include_router(admin_network_router)


@app.middleware("http")
async def add_corr_id(request: Request, call_next):
    cid = request.headers.get("x-corr-id") or uuid4().hex
    start = perf_counter()

    response = await call_next(request)

    ms = (perf_counter() - start) * 1000.0
    response.headers["x-corr-id"] = cid
    logger.info("HTTP {status} {method} {path} cid={cid} ({ms:.1f} ms)",
                status=response.status_code,
                method=request.method,
                path=request.url.path,
                cid=cid,
                ms=ms)
    return response


@app.get("/")
def root():
    return {"status": "ok", "service": "network"}
