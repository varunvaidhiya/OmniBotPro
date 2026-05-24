"""OmniBot Connector — MCP server (port 8081) for OpenClaw / Hermes."""

from __future__ import annotations

import base64
import json
import logging

from mcp.server import Server
from mcp.server.sse import SseServerTransport
from mcp.types import Resource, TextContent, Tool
from starlette.applications import Starlette
from starlette.routing import Mount, Route

from .config import get_config
from .ros import RosBridgeClient
from .state import RobotStateStore
from .tools import TOOLS, dispatch

logger = logging.getLogger(__name__)

_ros: RosBridgeClient | None = None
_state: RobotStateStore | None = None
_cfg = None

mcp = Server("omnibot-connector")


@mcp.list_tools()
async def list_tools():
    return [
        Tool(
            name=t["name"],
            description=t["description"],
            inputSchema=t["parameters"],
        )
        for t in TOOLS
    ]


@mcp.call_tool()
async def call_tool(name: str, arguments: dict):
    if _ros is None or _state is None or _cfg is None:
        return [
            TextContent(
                type="text", text=json.dumps({"error": "Server not initialised"})
            )
        ]
    try:
        result = await dispatch(name, arguments, _ros, _state, _cfg)
        return [TextContent(type="text", text=json.dumps(result))]
    except (ValueError, RuntimeError) as exc:
        return [TextContent(type="text", text=json.dumps({"error": str(exc)}))]


@mcp.list_resources()
async def list_resources():
    return [
        Resource(
            uri="robot://state",
            name="Robot State",
            description="Live robot state snapshot (position, velocity, arm joints, mode)",
            mimeType="application/json",
        ),
        Resource(
            uri="robot://cameras/front",
            name="Front Camera",
            description="Latest front camera JPEG",
            mimeType="image/jpeg",
        ),
        Resource(
            uri="robot://cameras/wrist",
            name="Wrist Camera",
            description="Latest wrist camera JPEG",
            mimeType="image/jpeg",
        ),
        Resource(
            uri="robot://cameras/bev",
            name="BEV Camera",
            description="Latest bird's-eye view JPEG",
            mimeType="image/jpeg",
        ),
    ]


@mcp.read_resource()
async def read_resource(uri: str):
    if _state is None:
        return json.dumps({"error": "Server not initialised"})
    if uri == "robot://state":
        snap = await _state.snapshot()
        return json.dumps(
            {
                "position": snap.position,
                "velocity": snap.velocity,
                "arm_positions": snap.arm_positions,
                "control_mode": snap.control_mode,
                "mission_status": snap.mission_status,
                "rosbridge_connected": snap.rosbridge_connected,
            }
        )
    if uri.startswith("robot://cameras/"):
        cam = uri.split("/")[-1]
        jpeg = await _state.get_camera(cam)
        if jpeg:
            return base64.b64encode(jpeg).decode()
        return ""
    raise ValueError(f"Unknown resource URI: {uri}")


def _build_starlette_app(cfg) -> Starlette:
    transport = SseServerTransport("/messages")

    async def handle_sse(scope, receive, send):
        async with mcp.run_sse_async(transport):
            await transport.connect_sse(scope, receive, send)

    async def handle_messages(scope, receive, send):
        await transport.handle_post_message(scope, receive, send)

    return Starlette(
        routes=[
            Route("/sse", endpoint=handle_sse),
            Mount("/messages", app=transport.handle_post_message),
        ]
    )


def main() -> None:
    import asyncio

    import uvicorn

    global _ros, _state, _cfg

    logging.basicConfig(level=logging.INFO)
    cfg = get_config()
    _cfg = cfg

    _state = RobotStateStore()
    _ros = RosBridgeClient(cfg.rosbridge_url, _state)

    async def run():
        await _ros.start()
        starlette_app = _build_starlette_app(cfg)
        config = uvicorn.Config(
            starlette_app, host="0.0.0.0", port=cfg.mcp_port, log_level="info"
        )
        server = uvicorn.Server(config)
        await server.serve()

    asyncio.run(run())
