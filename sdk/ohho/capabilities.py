"""The capability vocabulary.

A robot declares *what it can do*; behaviors ask ``robot.has(...)`` rather than
assuming hardware. This is what lets one behavior — or a whole agent — run across
a wheeled base, a quadruped, a humanoid and an arm, degrading gracefully when a
capability is absent.
"""

from __future__ import annotations

# Base / locomotion
BASE_DRIVE = "base.drive"
BASE_HOLONOMIC = "base.holonomic_drive"  # implies BASE_DRIVE, plus strafing (linear_y)
LEGGED_POSE = "legged.pose"

# Manipulation
MANIPULATION = "manipulation"

# Perception
PERCEPTION_RGB = "perception.rgb"
PERCEPTION_DEPTH = "perception.depth"
PERCEPTION_SCAN = "perception.scan"  # planar range scan (lidar / depth-derived)

ALL: frozenset[str] = frozenset(
    {
        BASE_DRIVE,
        BASE_HOLONOMIC,
        LEGGED_POSE,
        MANIPULATION,
        PERCEPTION_RGB,
        PERCEPTION_DEPTH,
        PERCEPTION_SCAN,
    }
)


def is_known(capability: str) -> bool:
    """True if ``capability`` is part of the recognised vocabulary."""
    return capability in ALL
