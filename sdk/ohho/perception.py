"""Perception — detections that feed the spatio-temporal memory.

Two perceptors behind one shape:

- :class:`SimPerceptor` — deterministic, dependency-free: "sees" the labeled
  objects of the sim world within a field of view and range (with range/bearing
  computed from the robot's pose), so the perceive→remember loop is fully
  testable without hardware.
- :class:`VlmPerceptor` — optional Claude-vision detector for real camera
  frames: give it JPEG/PNG bytes, get labeled detections back. ``anthropic`` is
  imported lazily; a fake client can be injected for tests.

``remember_detections`` writes any position-bearing detections into a
:class:`~ohho.memory.SpatialMemory`.
"""

from __future__ import annotations

import base64
import json
import math
from dataclasses import dataclass
from typing import List, Optional, Protocol

from .memory import SpatialMemory


@dataclass
class Detection:
    label: str
    confidence: float = 1.0
    x: Optional[float] = None  # world frame, when known
    y: Optional[float] = None
    bearing: Optional[float] = None  # rad, relative to robot heading
    range: Optional[float] = None  # m

    def __str__(self) -> str:
        where = ""
        if self.x is not None and self.y is not None:
            where = f" at ({self.x:.2f}, {self.y:.2f})"
        elif self.bearing is not None:
            where = f" bearing {self.bearing:+.2f} rad"
        return f"{self.label}{where} (conf {self.confidence:.2f})"


class Perceptor(Protocol):
    def look(self, *args, **kwargs) -> List[Detection]: ...


def _wrap(angle: float) -> float:
    while angle > math.pi:
        angle -= 2.0 * math.pi
    while angle < -math.pi:
        angle += 2.0 * math.pi
    return angle


class SimPerceptor:
    """Detects the sim world's labeled objects within FOV + range."""

    def __init__(
        self, robot, *, fov: float = math.radians(120), max_range: float = 3.5
    ) -> None:
        self.robot = robot
        self.fov = fov
        self.max_range = max_range

    def look(self) -> List[Detection]:
        world_objects = getattr(self.robot.transport, "world_objects", None)
        if not callable(world_objects):
            return []
        t = self.robot.telemetry()
        if not (t and t.odom):
            return []
        px, py, th = t.odom.x, t.odom.y, t.odom.theta
        out: list[Detection] = []
        for ob in world_objects():
            dx, dy = ob.x - px, ob.y - py
            rng = math.hypot(dx, dy)
            if rng > self.max_range:
                continue
            bearing = _wrap(math.atan2(dy, dx) - th)
            if abs(bearing) > self.fov / 2.0:
                continue
            confidence = max(0.3, 1.0 - 0.5 * rng / self.max_range)
            out.append(
                Detection(
                    label=ob.label,
                    confidence=round(confidence, 3),
                    x=ob.x,
                    y=ob.y,
                    bearing=round(bearing, 4),
                    range=round(rng, 3),
                )
            )
        out.sort(key=lambda d: d.range if d.range is not None else 1e9)
        return out


_VLM_PROMPT = (
    "You are a robot's object detector. List the distinct physical objects "
    "visible in this image as a JSON array — nothing else. Each item: "
    '{"label": "<short noun>", "confidence": <0..1>}. At most 12 items.'
)


class VlmPerceptor:
    """Claude-vision detector for real camera frames (optional dependency).

    ``client`` is injectable for tests; otherwise the ``anthropic`` package and
    ``ANTHROPIC_API_KEY`` are required at ``look()`` time.
    """

    def __init__(
        self, client=None, *, model: str = "claude-sonnet-5", max_tokens: int = 512
    ):
        self._client = client
        self.model = model
        self.max_tokens = max_tokens

    def _get_client(self):
        if self._client is not None:
            return self._client
        import anthropic  # lazy — the [agent] extra

        self._client = anthropic.Anthropic()
        return self._client

    def look(
        self, image_bytes: bytes, media_type: str = "image/jpeg"
    ) -> List[Detection]:
        client = self._get_client()
        message = client.messages.create(
            model=self.model,
            max_tokens=self.max_tokens,
            messages=[
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "image",
                            "source": {
                                "type": "base64",
                                "media_type": media_type,
                                "data": base64.b64encode(image_bytes).decode("ascii"),
                            },
                        },
                        {"type": "text", "text": _VLM_PROMPT},
                    ],
                }
            ],
        )
        text = "".join(
            getattr(block, "text", "") for block in getattr(message, "content", [])
        )
        return parse_vlm_detections(text)


def parse_vlm_detections(text: str) -> List[Detection]:
    """Tolerantly parse the model's JSON array of {label, confidence}."""
    start, end = text.find("["), text.rfind("]")
    if start < 0 or end <= start:
        return []
    try:
        raw = json.loads(text[start : end + 1])
    except json.JSONDecodeError:
        return []
    out: list[Detection] = []
    for item in raw:
        if not isinstance(item, dict) or "label" not in item:
            continue
        try:
            conf = float(item.get("confidence", 0.5))
        except (TypeError, ValueError):
            conf = 0.5
        out.append(
            Detection(label=str(item["label"]), confidence=max(0.0, min(1.0, conf)))
        )
    return out


def remember_detections(
    memory: SpatialMemory, detections: List[Detection], *, t: Optional[float] = None
) -> int:
    """Write position-bearing detections into memory. Returns how many stuck."""
    n = 0
    for d in detections:
        if d.x is None or d.y is None:
            continue
        memory.observe(d.label, d.x, d.y, t=t, attrs={"confidence": d.confidence})
        n += 1
    return n


__all__ = [
    "Detection",
    "Perceptor",
    "SimPerceptor",
    "VlmPerceptor",
    "parse_vlm_detections",
    "remember_detections",
]
