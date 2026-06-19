"""The world-state blackboard — one fused snapshot of the robot's situation.

OmniBot's state is scattered across ``/odom``, ``/arm/joint_states``,
``/perception/object_info``, ``/mission/status`` and the cameras. The harness
needs a single structured view to reason over each tick. :class:`WorldState`
is that view; the ROS ``world_state_node`` fuses the topics and publishes it on
``/agent/world_state``, and the harness consumes it through a ``Perceptor``.

``to_observation`` produces the flat ``{"state": (9,)}`` dict used by the
learning-engine schema (``learning_engine.data.schema.OBS_STATE``) so the same
snapshot can feed the safety verifier without coupling this module to ROS or
to the learning engine.
"""

from __future__ import annotations

import time
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Tuple

import numpy as np

# Mirrors learning_engine.data.schema (kept as literals to avoid importing it).
OBS_STATE = "state"
ARM_DIM = 6


@dataclass
class DetectedObject:
    """One perceived object, as published on ``/perception/object_info``."""

    label: str
    confidence: float = 0.0
    distance_m: float = float("nan")
    bearing_rad: float = float("nan")
    position: Tuple[float, float, float] = (0.0, 0.0, 0.0)  # base_link x, y, z
    yaw: float = 0.0

    def to_dict(self) -> Dict[str, Any]:
        return {
            "label": self.label,
            "confidence": self.confidence,
            "distance_m": self.distance_m,
            "bearing_rad": self.bearing_rad,
            "position": list(self.position),
            "yaw": self.yaw,
        }

    @classmethod
    def from_dict(cls, d: Dict[str, Any]) -> "DetectedObject":
        pos = d.get("position", [0.0, 0.0, 0.0])
        return cls(
            label=str(d.get("label", "object")),
            confidence=float(d.get("confidence", 0.0)),
            distance_m=float(d.get("distance_m", float("nan"))),
            bearing_rad=float(d.get("bearing_rad", float("nan"))),
            position=(float(pos[0]), float(pos[1]), float(pos[2])),
            yaw=float(d.get("yaw", 0.0)),
        )


@dataclass
class WorldState:
    """A single fused snapshot of the robot's situation at one tick."""

    stamp: float = field(default_factory=time.time)
    base_pose: Tuple[float, float, float] = (0.0, 0.0, 0.0)  # map x, y, yaw
    base_velocity: Tuple[float, float, float] = (0.0, 0.0, 0.0)  # vx, vy, omega
    arm_joint_positions: List[float] = field(default_factory=list)  # 6 (rad)
    detected_objects: List[DetectedObject] = field(default_factory=list)
    nearest_distance_m: float = float("nan")
    mission_phase: str = "idle"
    scene_description: str = ""
    memory_summary: str = ""
    emergency_stop: bool = False
    battery_pct: Optional[float] = None
    health: Dict[str, Any] = field(default_factory=dict)
    extra: Dict[str, Any] = field(default_factory=dict)

    # -- queries -----------------------------------------------------------
    def nearest_object(self) -> Optional[DetectedObject]:
        candidates = [o for o in self.detected_objects if not np.isnan(o.distance_m)]
        return min(candidates, key=lambda o: o.distance_m) if candidates else None

    def object_by_label(self, label: str) -> Optional[DetectedObject]:
        label = label.lower()
        matches = [o for o in self.detected_objects if o.label.lower() == label]
        matches.sort(key=lambda o: (np.isnan(o.distance_m), o.distance_m))
        return matches[0] if matches else None

    # -- conversions -------------------------------------------------------
    def to_observation(self) -> Dict[str, np.ndarray]:
        """Flat observation dict for the safety verifier.

        ``state`` is the canonical 9-D vector: 6 arm joints + (vx, vy, omega).
        """
        arm = list(self.arm_joint_positions[:ARM_DIM])
        arm += [0.0] * (ARM_DIM - len(arm))
        state = np.asarray(arm + list(self.base_velocity), dtype=np.float32)
        return {OBS_STATE: state}

    def summarize(self) -> str:
        """Compact natural-language summary for the reasoner's prompt."""
        lines = [
            f"Mission phase: {self.mission_phase}.",
            "Base at x={:.2f} y={:.2f} yaw={:.2f} (map).".format(*self.base_pose),
        ]
        if self.emergency_stop:
            lines.append("EMERGENCY STOP is engaged.")
        if self.detected_objects:
            obj_strs = []
            for o in self.detected_objects:
                d = "" if np.isnan(o.distance_m) else f" at {o.distance_m:.2f} m"
                obj_strs.append(f"{o.label}{d}")
            lines.append("Visible objects: " + ", ".join(obj_strs) + ".")
        else:
            lines.append("No objects currently detected.")
        if self.scene_description:
            lines.append(f"Scene: {self.scene_description}")
        return " ".join(lines)

    def to_dict(self) -> Dict[str, Any]:
        return {
            "stamp": self.stamp,
            "base_pose": list(self.base_pose),
            "base_velocity": list(self.base_velocity),
            "arm_joint_positions": list(self.arm_joint_positions),
            "detected_objects": [o.to_dict() for o in self.detected_objects],
            "nearest_distance_m": self.nearest_distance_m,
            "mission_phase": self.mission_phase,
            "scene_description": self.scene_description,
            "memory_summary": self.memory_summary,
            "emergency_stop": self.emergency_stop,
            "battery_pct": self.battery_pct,
            "health": dict(self.health),
            "extra": dict(self.extra),
        }

    @classmethod
    def from_dict(cls, d: Dict[str, Any]) -> "WorldState":
        def _triple(key: str) -> Tuple[float, float, float]:
            v = d.get(key, [0.0, 0.0, 0.0])
            return (float(v[0]), float(v[1]), float(v[2]))

        return cls(
            stamp=float(d.get("stamp", time.time())),
            base_pose=_triple("base_pose"),
            base_velocity=_triple("base_velocity"),
            arm_joint_positions=[float(x) for x in d.get("arm_joint_positions", [])],
            detected_objects=[
                DetectedObject.from_dict(o) for o in d.get("detected_objects", [])
            ],
            nearest_distance_m=float(d.get("nearest_distance_m", float("nan"))),
            mission_phase=str(d.get("mission_phase", "idle")),
            scene_description=str(d.get("scene_description", "")),
            memory_summary=str(d.get("memory_summary", "")),
            emergency_stop=bool(d.get("emergency_stop", False)),
            battery_pct=d.get("battery_pct"),
            health=dict(d.get("health", {})),
            extra=dict(d.get("extra", {})),
        )
