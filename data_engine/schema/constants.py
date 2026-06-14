"""Data specifications and constants"""

from dataclasses import dataclass
from typing import List, Tuple
import numpy as np

# ---------------------------------------------------------------------------
# Mobile manipulation — arm joint names
# ---------------------------------------------------------------------------
ARM_JOINT_NAMES = [
    "shoulder_pan",
    "shoulder_lift",
    "elbow_flex",
    "wrist_flex",
    "wrist_roll",
    "gripper",
]


@dataclass
class StateSpec:
    """Robot state specification"""

    names: List[str]
    ranges: List[Tuple[float, float]]  # (min, max)
    units: List[str]
    dim: int


# State: [x, y, theta, vel_x, vel_y, omega, motor_fl, motor_fr, motor_bl, motor_br]
STATE_SPEC = StateSpec(
    names=[
        "x",
        "y",
        "theta",
        "vel_x",
        "vel_y",
        "omega",
        "motor_fl",
        "motor_fr",
        "motor_bl",
        "motor_br",
    ],
    ranges=[
        (-10.0, 10.0),  # x (m)
        (-10.0, 10.0),  # y (m)
        (-np.pi, np.pi),  # theta (rad)
        (-2.0, 2.0),  # vel_x (m/s)
        (-2.0, 2.0),  # vel_y (m/s)
        (-3.0, 3.0),  # omega (rad/s)
        (-255, 255),  # motor FL PWM
        (-255, 255),  # motor FR PWM
        (-255, 255),  # motor BL PWM
        (-255, 255),  # motor BR PWM
    ],
    units=["m", "m", "rad", "m/s", "m/s", "rad/s", "pwm", "pwm", "pwm", "pwm"],
    dim=10,
)


@dataclass
class ActionSpec:
    """Robot action specification"""

    names: List[str]
    ranges: List[Tuple[float, float]]
    units: List[str]
    dim: int


# Action: [linear_x, linear_y, angular_z]
ACTION_SPEC = ActionSpec(
    names=["linear_x", "linear_y", "angular_z"],
    ranges=[
        (-2.0, 2.0),  # linear_x (m/s)
        (-2.0, 2.0),  # linear_y (m/s)
        (-3.0, 3.0),  # angular_z (rad/s)
    ],
    units=["m/s", "m/s", "rad/s"],
    dim=3,
)


@dataclass
class CameraConfig:
    """Camera configuration"""

    name: str
    topic: str
    resolution: Tuple[int, int]  # (height, width)
    fps: int
    encoding: str

    @property
    def height(self) -> int:
        return self.resolution[0]

    @property
    def width(self) -> int:
        return self.resolution[1]


# ── Base cameras (4× OV9732, 100° FOV, on bottom plate edges) ────────────────
CAMERA_FRONT = CameraConfig(
    name="front",
    topic="/camera/front/image_raw",
    resolution=(480, 640),
    fps=30,
    encoding="bgr8",
)
CAMERA_REAR = CameraConfig(
    name="rear",
    topic="/camera/rear/image_raw",
    resolution=(480, 640),
    fps=30,
    encoding="bgr8",
)
CAMERA_LEFT = CameraConfig(
    name="left",
    topic="/camera/left/image_raw",
    resolution=(480, 640),
    fps=30,
    encoding="bgr8",
)
CAMERA_RIGHT = CameraConfig(
    name="right",
    topic="/camera/right/image_raw",
    resolution=(480, 640),
    fps=30,
    encoding="bgr8",
)

# ── BEV composite (ros2_bev_stitcher output) ──────────────────────────────────
CAMERA_BEV = CameraConfig(
    name="bev",
    topic="/camera/base/bev/image_raw",
    resolution=(800, 800),
    fps=30,
    encoding="bgr8",
)

# ── Wrist camera (OV9732 on SO-101 gripper) ───────────────────────────────────
CAMERA_WRIST = CameraConfig(
    name="wrist",
    topic="/camera/wrist/image_raw",
    resolution=(480, 640),
    fps=30,
    encoding="bgr8",
)

# ── Orbbec Astra Pro — top plate, rear-facing, 12° down-tilt ─────────────────
# Depth range: 0.6–8.0 m | FOV: 60°H × 49.5°V | 165×30×40 mm, 300 g
CAMERA_DEPTH = CameraConfig(
    name="depth",
    topic="/camera/depth/image_raw",
    resolution=(480, 640),
    fps=30,
    encoding="mono16",  # 16-bit depth in mm
)

BASE_CAMERAS = [CAMERA_FRONT, CAMERA_REAR, CAMERA_LEFT, CAMERA_RIGHT]
ALL_CAMERAS = [
    CAMERA_FRONT,
    CAMERA_REAR,
    CAMERA_LEFT,
    CAMERA_RIGHT,
    CAMERA_BEV,
    CAMERA_WRIST,
    CAMERA_DEPTH,
]

# LeRobot feature key → CameraConfig mapping.
# Mirrors the camera streams written by bag_to_omnibot.py (front + wrist).
LEROBOT_CAMERA_KEYS = {
    "observation.images.front": CAMERA_FRONT,
    "observation.images.wrist": CAMERA_WRIST,
}

# ---------------------------------------------------------------------------
# Mobile manipulation — unified 9D state / action specs
# ---------------------------------------------------------------------------
# State: [shoulder_pan, shoulder_lift, elbow_flex, wrist_flex, wrist_roll,
#          gripper, base_vx, base_vy, base_vz]
MOBILE_MANIP_STATE_SPEC = StateSpec(
    names=ARM_JOINT_NAMES + ["base_vx", "base_vy", "base_vz"],
    ranges=[
        (-3.14, 3.14),  # shoulder_pan  (rad)
        (-1.57, 1.57),  # shoulder_lift (rad)
        (-1.57, 1.57),  # elbow_flex    (rad)
        (-1.57, 1.57),  # wrist_flex    (rad)
        (-3.14, 3.14),  # wrist_roll    (rad)
        (-0.10, 0.80),  # gripper       (rad)
        (-0.30, 0.30),  # base_vx       (m/s)
        (-0.30, 0.30),  # base_vy       (m/s)
        (-1.00, 1.00),  # base_vz / omega (rad/s)
    ],
    units=["rad", "rad", "rad", "rad", "rad", "rad", "m/s", "m/s", "rad/s"],
    dim=9,
)

# Action: same structure as state — direct position targets for arm,
#         velocity commands for base.
MOBILE_MANIP_ACTION_SPEC = ActionSpec(
    names=ARM_JOINT_NAMES + ["base_vx", "base_vy", "base_vz"],
    ranges=[
        (-3.14, 3.14),  # shoulder_pan  (rad)
        (-1.57, 1.57),  # shoulder_lift (rad)
        (-1.57, 1.57),  # elbow_flex    (rad)
        (-1.57, 1.57),  # wrist_flex    (rad)
        (-3.14, 3.14),  # wrist_roll    (rad)
        (-0.10, 0.80),  # gripper       (rad)
        (-0.30, 0.30),  # base_vx       (m/s)
        (-0.30, 0.30),  # base_vy       (m/s)
        (-1.00, 1.00),  # base_vz / omega (rad/s)
    ],
    units=["rad", "rad", "rad", "rad", "rad", "rad", "m/s", "m/s", "rad/s"],
    dim=9,
)
