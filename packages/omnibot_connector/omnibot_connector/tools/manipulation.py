from __future__ import annotations

JOINT_MIN = [-3.14, -1.57, -1.57, -1.57, -3.14, -0.10]
JOINT_MAX = [3.14, 1.57, 1.57, 1.57, 3.14, 0.80]
JOINT_NAMES = [
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
]


async def execute_manipulation(task: str, ros, state, cfg) -> dict:
    cmd = f"vla:{task}"
    await ros.publish("/mission/command", {"data": cmd})
    return {"status": "sent", "command": cmd}


async def move_arm(joint_positions: list, ros, state, cfg) -> dict:
    if len(joint_positions) != 6:
        raise ValueError(
            f"move_arm requires exactly 6 joint positions, got {len(joint_positions)}"
        )
    for i, (pos, lo, hi) in enumerate(zip(joint_positions, JOINT_MIN, JOINT_MAX)):
        if not (lo <= pos <= hi):
            raise ValueError(
                f"Joint {i} ({JOINT_NAMES[i]}) value {pos:.3f} out of limits [{lo}, {hi}]"
            )
    msg = {
        "header": {"stamp": {"sec": 0, "nanosec": 0}, "frame_id": ""},
        "name": JOINT_NAMES,
        "position": list(joint_positions),
        "velocity": [],
        "effort": [],
    }
    await ros.publish("/arm/joint_commands", msg)
    return {"status": "sent", "joint_positions": list(joint_positions)}
