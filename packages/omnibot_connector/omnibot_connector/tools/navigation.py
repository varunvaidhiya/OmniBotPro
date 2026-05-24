from __future__ import annotations


async def navigate_to_location(location: str, ros, state, cfg) -> dict:
    locations = await _load_locations(cfg)
    if locations and location not in locations:
        raise ValueError(
            f"Unknown location '{location}'. Available: {list(locations.keys())}"
        )
    cmd = f"navigate:{location}"
    await ros.publish("/mission/command", {"data": cmd})
    return {"status": "sent", "command": cmd}


async def navigate_to_pose(x: float, y: float, yaw: float, ros, state, cfg) -> dict:
    cmd = f"navigate_pose:{x:.3f},{y:.3f},{yaw:.3f}"
    await ros.publish("/mission/command", {"data": cmd})
    return {"status": "sent", "command": cmd}


async def navigate_then_manipulate(location: str, task: str, ros, state, cfg) -> dict:
    locations = await _load_locations(cfg)
    if locations and location not in locations:
        raise ValueError(
            f"Unknown location '{location}'. Available: {list(locations.keys())}"
        )
    cmd = f"navigate:{location},vla:{task}"
    await ros.publish("/mission/command", {"data": cmd})
    return {"status": "sent", "command": cmd}


async def _load_locations(cfg) -> dict:
    import os

    import yaml

    path = cfg.locations_yaml or _discover_locations()
    if not path or not os.path.exists(path):
        return {}
    with open(path) as f:
        data = yaml.safe_load(f)
    return data.get("locations", {}) if data else {}


def _discover_locations() -> str:
    import os

    candidates = [
        os.path.expanduser("~/robot_ws/src/omnibot_hybrid/config/named_locations.yaml"),
        "/home/ros/workspace/robot_ws/src/omnibot_hybrid/config/named_locations.yaml",
        "/config/named_locations.yaml",
    ]
    for c in candidates:
        if os.path.exists(c):
            return c
    return ""
