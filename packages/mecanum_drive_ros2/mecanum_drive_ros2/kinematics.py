"""
mecanum_drive_ros2.kinematics
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Pure-Python mecanum wheel kinematics (no ROS dependency).

Mirrors the C++ header-only library so you can use the same maths from
Python scripts, unit tests, and Jupyter notebooks.
"""

from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Tuple


@dataclass
class RobotGeometry:
    wheel_radius: float  # metres
    wheel_separation_width: float  # metres, left–right full track
    wheel_separation_length: float  # metres, front–rear full wheelbase


# Type alias: (FL, FR, BL, BR) angular velocities in rad/s
WheelVelocities = Tuple[float, float, float, float]


def inverse_kinematics(
    vx: float,
    vy: float,
    omega: float,
    geom: RobotGeometry,
) -> WheelVelocities:
    """
    Body twist → wheel angular velocities.

    Args:
        vx:    Forward velocity (m/s, +x = forward)
        vy:    Lateral velocity (m/s, +y = left)
        omega: Angular velocity (rad/s, +z = CCW)
        geom:  Robot geometry

    Returns:
        (fl, fr, bl, br) wheel speeds in rad/s
    """
    L = geom.wheel_separation_length / 2.0
    W = geom.wheel_separation_width / 2.0
    k = L + W
    r = geom.wheel_radius

    fl = (vx - vy - k * omega) / r
    fr = (vx + vy + k * omega) / r
    bl = (vx + vy - k * omega) / r
    br = (vx - vy + k * omega) / r
    return fl, fr, bl, br


def forward_kinematics(
    wheels: WheelVelocities,
    geom: RobotGeometry,
) -> Tuple[float, float, float]:
    """
    Wheel angular velocities → body twist.

    Args:
        wheels: (fl, fr, bl, br) in rad/s
        geom:   Robot geometry

    Returns:
        (vx, vy, omega) body twist in m/s, m/s, rad/s
    """
    fl, fr, bl, br = wheels
    L = geom.wheel_separation_length / 2.0
    W = geom.wheel_separation_width / 2.0
    k = L + W
    r = geom.wheel_radius

    vx = r * (fl + fr + bl + br) / 4.0
    vy = r * (-fl + fr + bl - br) / 4.0
    omega = r * (-fl + fr - bl + br) / (4.0 * k)
    return vx, vy, omega


def integrate_pose(
    x: float,
    y: float,
    theta: float,
    vx: float,
    vy: float,
    omega: float,
    dt: float,
) -> Tuple[float, float, float]:
    """
    Integrate body twist into 2-D pose over time dt.

    Returns updated (x, y, theta) with theta wrapped to [-pi, pi].
    """
    cos_th = math.cos(theta)
    sin_th = math.sin(theta)
    x += (vx * cos_th - vy * sin_th) * dt
    y += (vx * sin_th + vy * cos_th) * dt
    theta += omega * dt
    theta = math.atan2(math.sin(theta), math.cos(theta))
    return x, y, theta
