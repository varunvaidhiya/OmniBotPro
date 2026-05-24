from __future__ import annotations

import base64
import time


async def get_robot_state(ros, state, cfg) -> dict:
    snap = await state.snapshot()
    return {
        "position": snap.position,
        "velocity": snap.velocity,
        "arm_positions": snap.arm_positions,
        "arm_velocities": snap.arm_velocities,
        "control_mode": snap.control_mode,
        "mission_status": snap.mission_status,
        "ai_status": snap.ai_status,
        "rosbridge_connected": snap.rosbridge_connected,
        "timestamp": snap.last_updated,
    }


async def get_camera_image(camera: str, ros, state, cfg) -> dict:
    valid = {"front", "wrist", "bev"}
    if camera not in valid:
        raise ValueError(f"camera must be one of {valid}, got '{camera}'")
    jpeg = await state.get_camera(camera)
    if jpeg is None:
        return {
            "camera": camera,
            "image_base64": None,
            "error": "No frame available yet",
        }
    b64 = base64.b64encode(jpeg).decode()
    return {"camera": camera, "image_base64": b64, "timestamp": time.time()}


async def describe_scene(camera: str, ros, state, cfg) -> dict:
    valid = {"front", "wrist", "bev"}
    if camera not in valid:
        raise ValueError(f"camera must be one of {valid}, got '{camera}'")
    jpeg = await state.get_camera(camera)
    if jpeg is None:
        return {"description": "No camera frame available"}
    b64 = base64.b64encode(jpeg).decode()
    if cfg.anthropic_api_key:
        return {"description": _describe_with_claude(b64, cfg)}
    if cfg.vla_serve_url:
        return {"description": _describe_with_vla_serve(b64, cfg)}
    return {
        "description": "No vision API configured (set ANTHROPIC_API_KEY or VLA_SERVE_URL)"
    }


def _describe_with_claude(b64: str, cfg) -> str:
    import anthropic

    client = anthropic.Anthropic(api_key=cfg.anthropic_api_key)
    resp = client.messages.create(
        model="claude-sonnet-4-6",
        max_tokens=256,
        messages=[
            {
                "role": "user",
                "content": [
                    {
                        "type": "image",
                        "source": {
                            "type": "base64",
                            "media_type": "image/jpeg",
                            "data": b64,
                        },
                    },
                    {
                        "type": "text",
                        "text": "Describe the robot's current scene briefly. What objects and areas do you see?",
                    },
                ],
            }
        ],
    )
    return resp.content[0].text


def _describe_with_vla_serve(b64: str, cfg) -> str:
    import requests

    resp = requests.post(
        f"{cfg.vla_serve_url}/predict",
        json={"instruction": "Describe the scene.", "image_base64": b64},
        timeout=30,
    )
    resp.raise_for_status()
    return resp.json().get("raw_output", "")
