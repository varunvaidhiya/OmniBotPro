"""
robot_episode_dataset.schema
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
User-configurable schema primitives for describing robot state, actions,
and cameras.  Replace the defaults with values for your own robot.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List, Tuple


@dataclass
class StateSpec:
    """Describes the robot state vector."""

    names: List[str]
    ranges: List[Tuple[float, float]]  # (min, max) per dimension
    units: List[str]

    @property
    def dim(self) -> int:
        return len(self.names)


@dataclass
class ActionSpec:
    """Describes the robot action vector."""

    names: List[str]
    ranges: List[Tuple[float, float]]
    units: List[str]

    @property
    def dim(self) -> int:
        return len(self.names)


@dataclass
class CameraConfig:
    """Per-camera configuration."""

    name: str
    topic: str
    resolution: Tuple[int, int]  # (height, width)
    fps: int
    encoding: str = "bgr8"

    @property
    def height(self) -> int:
        return self.resolution[0]

    @property
    def width(self) -> int:
        return self.resolution[1]


@dataclass
class DatasetSchema:
    """
    Complete schema description for an episode dataset.

    Pass an instance of this to the ingestion pipeline and dataset loader
    so they know how to interpret state/action/camera data.
    """

    state_spec: StateSpec
    action_spec: ActionSpec
    cameras: List[CameraConfig]
    robot_name: str = "robot"
    description: str = ""

    @property
    def camera_names(self) -> List[str]:
        return [c.name for c in self.cameras]

    @property
    def lerobot_camera_keys(self) -> Dict[str, CameraConfig]:
        return {f"observation.images.{c.name}": c for c in self.cameras}


# ---------------------------------------------------------------------------
# Example: mecanum base + 6-DOF arm (matches OmniBot)
# ---------------------------------------------------------------------------

OMNIBOT_SCHEMA = DatasetSchema(
    robot_name="omnibot",
    description="Mecanum-wheel mobile manipulator (SO-101 arm + mecanum base)",
    state_spec=StateSpec(
        names=[
            "shoulder_pan",
            "shoulder_lift",
            "elbow_flex",
            "wrist_flex",
            "wrist_roll",
            "gripper",
            "base_vx",
            "base_vy",
            "base_vz",
        ],
        ranges=[
            (-3.14, 3.14),
            (-1.57, 1.57),
            (-1.57, 1.57),
            (-1.57, 1.57),
            (-3.14, 3.14),
            (-0.10, 0.80),
            (-0.30, 0.30),
            (-0.30, 0.30),
            (-1.00, 1.00),
        ],
        units=["rad"] * 6 + ["m/s", "m/s", "rad/s"],
    ),
    action_spec=ActionSpec(
        names=[
            "shoulder_pan",
            "shoulder_lift",
            "elbow_flex",
            "wrist_flex",
            "wrist_roll",
            "gripper",
            "base_vx",
            "base_vy",
            "base_vz",
        ],
        ranges=[
            (-3.14, 3.14),
            (-1.57, 1.57),
            (-1.57, 1.57),
            (-1.57, 1.57),
            (-3.14, 3.14),
            (-0.10, 0.80),
            (-0.30, 0.30),
            (-0.30, 0.30),
            (-1.00, 1.00),
        ],
        units=["rad"] * 6 + ["m/s", "m/s", "rad/s"],
    ),
    cameras=[
        CameraConfig("bev", "/camera/base/bev/image_raw", (480, 640), 30),
        CameraConfig("wrist", "/camera/wrist/image_raw", (240, 320), 30),
    ],
)
