from __future__ import annotations

import os

import yaml

VALID_MODES = {"nav2", "vla", "teleop", "rl_nav"}


async def set_control_mode(mode: str, ros, state, cfg) -> dict:
    if mode not in VALID_MODES:
        raise ValueError(f"mode must be one of {VALID_MODES}, got '{mode}'")
    await ros.publish("/control_mode", {"data": mode})
    return {"status": "sent", "mode": mode}


async def emergency_stop(active: bool, ros, state, cfg) -> dict:
    await ros.publish("/emergency_stop", {"data": active})
    return {"status": "sent", "active": active}


async def cancel_mission(ros, state, cfg) -> dict:
    await ros.publish("/mission/cancel", {"data": "cancel"})
    return {"status": "sent"}


async def list_locations(ros, state, cfg) -> dict:
    path = cfg.locations_yaml or _discover_locations()
    if not path or not os.path.exists(path):
        return {"locations": [], "error": "named_locations.yaml not found"}
    with open(path) as f:
        data = yaml.safe_load(f)
    locs = (data or {}).get("locations", {})
    return {"locations": list(locs.keys()), "details": locs}


async def send_ai_command(command: str, ros, state, cfg) -> dict:
    await ros.publish("/ai/command", {"data": command})
    return {"status": "sent", "command": command}


def _discover_locations() -> str:
    candidates = [
        os.path.expanduser("~/robot_ws/src/omnibot_hybrid/config/named_locations.yaml"),
        "/home/ros/workspace/robot_ws/src/omnibot_hybrid/config/named_locations.yaml",
        "/config/named_locations.yaml",
    ]
    for c in candidates:
        if os.path.exists(c):
            return c
    return ""
