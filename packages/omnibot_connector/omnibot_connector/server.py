"""OmniBot Connector — FastAPI REST API (port 8080)."""

from __future__ import annotations

import time
from contextlib import asynccontextmanager
from typing import Any

import yaml
from fastapi import FastAPI, HTTPException, Request
from prometheus_fastapi_instrumentator import Instrumentator
from pydantic import BaseModel

from .config import get_config
from .episodes import EpisodeRecorder
from .ros import RosBridgeClient
from .state import RobotStateStore
from .streaming import ws_server as _ws
from .tools import TOOLS, dispatch
from .tools.perception import get_camera_image, get_robot_state
from .tools.system import _discover_locations


@asynccontextmanager
async def lifespan(app: FastAPI):
    cfg = get_config()
    state_store = RobotStateStore()
    ros = RosBridgeClient(cfg.rosbridge_url, state_store)
    recorder = EpisodeRecorder(cfg)
    _ws.init(state_store, cfg)
    await recorder.start()
    await ros.start()
    app.state.ros = ros
    app.state.state_store = state_store
    app.state.cfg = cfg
    app.state.recorder = recorder
    yield
    await ros.stop()


app = FastAPI(title="OmniBot Connector", version="0.1.0", lifespan=lifespan)
Instrumentator().instrument(app).expose(app)


def _check_api_key(request: Request) -> None:
    cfg = get_config()
    if not cfg.api_key:
        return
    key = request.headers.get("X-API-Key", "")
    if key != cfg.api_key:
        raise HTTPException(status_code=401, detail="Invalid or missing X-API-Key")


class ToolCallRequest(BaseModel):
    tool: str
    arguments: dict[str, Any] = {}


@app.get("/health")
async def health(request: Request):
    snap = await request.app.state.state_store.snapshot()
    return {"status": "ok", "rosbridge_connected": snap.rosbridge_connected}


@app.get("/v1/tools")
async def list_tools(request: Request):
    _check_api_key(request)
    return {"tools": TOOLS}


@app.post("/v1/tools/call")
async def call_tool(body: ToolCallRequest, request: Request):
    _check_api_key(request)
    t0 = time.monotonic()
    try:
        result = await dispatch(
            body.tool,
            body.arguments,
            request.app.state.ros,
            request.app.state.state_store,
            request.app.state.cfg,
        )
        await request.app.state.recorder.record_tool_call(
            body.tool, body.arguments, result
        )
        return {
            "result": result,
            "latency_ms": round((time.monotonic() - t0) * 1000, 1),
        }
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc))
    except RuntimeError as exc:
        raise HTTPException(status_code=503, detail=str(exc))


@app.get("/v1/observe")
async def observe(request: Request):
    _check_api_key(request)
    return await get_robot_state(
        request.app.state.ros,
        request.app.state.state_store,
        request.app.state.cfg,
    )


@app.get("/v1/cameras/{camera}")
async def camera(camera: str, request: Request):
    _check_api_key(request)
    return await get_camera_image(
        camera,
        request.app.state.ros,
        request.app.state.state_store,
        request.app.state.cfg,
    )


@app.post("/v1/evolve/locations")
async def evolve_locations(body: dict, request: Request):
    """Agents push newly learned locations back into named_locations.yaml."""
    _check_api_key(request)
    cfg = request.app.state.cfg
    path = cfg.locations_yaml or _discover_locations()
    if not path:
        raise HTTPException(status_code=404, detail="named_locations.yaml not found")
    try:
        with open(path) as f:
            data = yaml.safe_load(f) or {"locations": {}}
        data.setdefault("locations", {}).update(body.get("locations", {}))
        with open(path, "w") as f:
            yaml.safe_dump(data, f)
    except OSError as exc:
        raise HTTPException(status_code=500, detail=f"Failed to write locations: {exc}")
    return {"status": "updated", "count": len(body.get("locations", {}))}


def main() -> None:
    import uvicorn

    cfg = get_config()
    uvicorn.run(
        "omnibot_connector.server:app",
        host="0.0.0.0",
        port=cfg.rest_port,
        reload=False,
    )
