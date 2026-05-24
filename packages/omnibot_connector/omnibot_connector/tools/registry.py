from __future__ import annotations

from typing import Any

from .manipulation import execute_manipulation, move_arm
from .navigation import navigate_then_manipulate, navigate_to_location, navigate_to_pose
from .perception import describe_scene, get_camera_image, get_robot_state
from .system import (
    cancel_mission,
    emergency_stop,
    list_locations,
    send_ai_command,
    set_control_mode,
)

TOOLS: list[dict] = [
    {
        "name": "navigate_to_location",
        "description": "Navigate the robot base to a named location via Nav2 autonomous navigation.",
        "parameters": {
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "Named location key (e.g. 'kitchen', 'home'). Use list_locations to see all available.",
                }
            },
            "required": ["location"],
        },
    },
    {
        "name": "navigate_to_pose",
        "description": "Navigate the robot to an absolute map pose (x, y, yaw).",
        "parameters": {
            "type": "object",
            "properties": {
                "x": {
                    "type": "number",
                    "description": "X coordinate in map frame (metres)",
                },
                "y": {
                    "type": "number",
                    "description": "Y coordinate in map frame (metres)",
                },
                "yaw": {
                    "type": "number",
                    "description": "Heading in radians (-π to π)",
                },
            },
            "required": ["x", "y", "yaw"],
        },
    },
    {
        "name": "navigate_then_manipulate",
        "description": "Navigate to a named location then execute a VLA manipulation task.",
        "parameters": {
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "Named destination location",
                },
                "task": {
                    "type": "string",
                    "description": "Natural language manipulation task, e.g. 'pick up the red cup'",
                },
            },
            "required": ["location", "task"],
        },
    },
    {
        "name": "execute_manipulation",
        "description": "Execute a VLA manipulation task at the current robot location.",
        "parameters": {
            "type": "object",
            "properties": {
                "task": {
                    "type": "string",
                    "description": "Natural language task description, e.g. 'open the drawer'",
                }
            },
            "required": ["task"],
        },
    },
    {
        "name": "move_arm",
        "description": "Move the SO-101 arm to specific joint positions (radians). All 6 joints required.",
        "parameters": {
            "type": "object",
            "properties": {
                "joint_positions": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 6,
                    "maxItems": 6,
                    "description": "Joint angles in radians for [shoulder_pan, shoulder_lift, elbow_flex, wrist_flex, wrist_roll, gripper]",
                }
            },
            "required": ["joint_positions"],
        },
    },
    {
        "name": "set_control_mode",
        "description": "Switch the robot's velocity mux mode. Must be set before driving.",
        "parameters": {
            "type": "object",
            "properties": {
                "mode": {
                    "type": "string",
                    "enum": ["nav2", "vla", "teleop", "rl_nav"],
                    "description": "Control mode: nav2=autonomous nav, vla=VLA policy, teleop=manual, rl_nav=RL navigation",
                }
            },
            "required": ["mode"],
        },
    },
    {
        "name": "emergency_stop",
        "description": "Activate or clear the emergency stop. Set active=true to halt all motion immediately.",
        "parameters": {
            "type": "object",
            "properties": {
                "active": {
                    "type": "boolean",
                    "description": "true to activate e-stop, false to clear",
                }
            },
            "required": ["active"],
        },
    },
    {
        "name": "cancel_mission",
        "description": "Cancel the currently running mission (navigation or manipulation).",
        "parameters": {"type": "object", "properties": {}, "required": []},
    },
    {
        "name": "get_robot_state",
        "description": "Get the current robot state: position, velocity, arm joints, control mode, mission status.",
        "parameters": {"type": "object", "properties": {}, "required": []},
    },
    {
        "name": "get_camera_image",
        "description": "Get a base64-encoded JPEG from one of the robot's cameras.",
        "parameters": {
            "type": "object",
            "properties": {
                "camera": {
                    "type": "string",
                    "enum": ["front", "wrist", "bev"],
                    "description": "Camera name: front=forward-facing, wrist=arm-mounted, bev=bird's-eye-view",
                }
            },
            "required": ["camera"],
        },
    },
    {
        "name": "list_locations",
        "description": "List all named locations the robot can navigate to.",
        "parameters": {"type": "object", "properties": {}, "required": []},
    },
    {
        "name": "send_ai_command",
        "description": "Send a natural language command to the LangGraph AI agent (requires use_langchain:=true).",
        "parameters": {
            "type": "object",
            "properties": {
                "command": {
                    "type": "string",
                    "description": "Natural language command, e.g. 'find the red cup and bring it to me'",
                }
            },
            "required": ["command"],
        },
    },
    {
        "name": "describe_scene",
        "description": "Get a natural language description of what the robot currently sees through a camera.",
        "parameters": {
            "type": "object",
            "properties": {
                "camera": {
                    "type": "string",
                    "enum": ["front", "wrist", "bev"],
                    "description": "Camera to describe",
                }
            },
            "required": ["camera"],
        },
    },
]

_DISPATCH: dict[str, Any] = {
    "navigate_to_location": navigate_to_location,
    "navigate_to_pose": navigate_to_pose,
    "navigate_then_manipulate": navigate_then_manipulate,
    "execute_manipulation": execute_manipulation,
    "move_arm": move_arm,
    "set_control_mode": set_control_mode,
    "emergency_stop": emergency_stop,
    "cancel_mission": cancel_mission,
    "get_robot_state": get_robot_state,
    "get_camera_image": get_camera_image,
    "list_locations": list_locations,
    "send_ai_command": send_ai_command,
    "describe_scene": describe_scene,
}


async def dispatch(name: str, arguments: dict, ros, state, cfg) -> dict:
    fn = _DISPATCH.get(name)
    if fn is None:
        raise ValueError(f"Unknown tool '{name}'. Available: {list(_DISPATCH.keys())}")
    return await fn(**arguments, ros=ros, state=state, cfg=cfg)
