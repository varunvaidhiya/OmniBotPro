"""Canonical observation/action schema for the learning engine.

Must stay consistent with ``data_engine/schema/constants.py`` (the episode
collection pipeline) and with the smolvla_node / teleop_recorder_node key
conventions. The learning engine uses flat dict observations; the LeRobot
key map below converts to/from the HF dataset format.
"""

from __future__ import annotations

from typing import Dict

SCHEMA_VERSION = "1.0"

# ---------------------------------------------------------------------------
# Observation keys (values are numpy arrays)
# ---------------------------------------------------------------------------
OBS_STATE = "state"  # proprioception vector
OBS_IMAGE_FRONT = "images.front"  # 480x640x3 bgr8  (/camera/front/image_raw)
OBS_IMAGE_WRIST = "images.wrist"  # 240x320x3 bgr8  (/camera/wrist/image_raw)
OBS_IMAGE_BEV = "images.bev"  # stitched BEV   (/camera/base/bev/image_raw)
OBS_DEPTH = "depth"  # depth image     (/camera/depth/image_raw)
OBS_LIDAR_SECTORS = "lidar_sectors"  # 8-sector ranges synthesized from depth
OBS_GOAL = "goal"  # goal-conditioned tasks: [x, y, yaw] or ee target

IMAGE_KEYS = (OBS_IMAGE_FRONT, OBS_IMAGE_WRIST, OBS_IMAGE_BEV)

# ---------------------------------------------------------------------------
# Dimensions — mirror data_engine MOBILE_MANIP_*_SPEC (9-D = 6 arm + 3 base)
# ---------------------------------------------------------------------------
ARM_DIM = 6
BASE_STATE_DIM = 3  # vx, vy, omega (base velocities)
BASE_ACTION_DIM = 3  # linear_x, linear_y, angular_z
MOBILE_MANIP_STATE_DIM = ARM_DIM + BASE_STATE_DIM  # 9
MOBILE_MANIP_ACTION_DIM = ARM_DIM + BASE_ACTION_DIM  # 9

ARM_JOINT_NAMES = [
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
]

# Hardware limits the safety checks rely on (see CLAUDE.md / omnibot_arm)
JOINT_MIN = [-3.14, -1.57, -1.57, -1.57, -3.14, -0.1]
JOINT_MAX = [3.14, 1.57, 1.57, 1.57, 3.14, 0.8]
MAX_LINEAR_VEL = 0.20  # m/s — Yahboom driver hardcoded clamp
MAX_ANGULAR_VEL = 1.00  # rad/s
MAX_ARM_DELTA_PER_STEP = 0.05  # rad/step at 20 Hz — matches rl_arm_node

# ---------------------------------------------------------------------------
# LeRobot HF dataset key map (export/import via robot_episode_dataset)
# ---------------------------------------------------------------------------
LEROBOT_KEY_MAP: Dict[str, str] = {
    OBS_STATE: "observation.state",
    OBS_IMAGE_FRONT: "observation.images.front",
    OBS_IMAGE_WRIST: "observation.images.wrist",
    OBS_IMAGE_BEV: "observation.images.bev",
}
LEROBOT_ACTION_KEY = "action"
