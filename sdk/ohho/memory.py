"""Spatio-temporal memory — entities tracked through time and space.

The robot's "where did I see the cup?" memory: observations are associated to
persistent entities (object permanence — the same label seen near a known
position is the same thing), every sighting is time-stamped into an event log,
and the store answers spatial (*what's near X?*) and temporal (*when did I last
see Y?*) queries. JSON persistence makes memory survive across sessions and CLI
invocations. Pure standard library, thread-safe.
"""

from __future__ import annotations

import json
import math
import threading
import time
import uuid
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import List, Optional


@dataclass
class Entity:
    """A persistent object hypothesis in world coordinates."""

    id: str
    label: str
    x: float
    y: float
    first_seen: float
    last_seen: float
    count: int = 1
    attrs: dict = field(default_factory=dict)

    def age(self, now: Optional[float] = None) -> float:
        """Seconds since the last sighting."""
        return (now if now is not None else time.time()) - self.last_seen


@dataclass
class MemoryEvent:
    t: float
    kind: str  # "seen" | "updated" | "note"
    label: str
    x: Optional[float] = None
    y: Optional[float] = None
    note: str = ""


def default_memory_path(robot_id: str) -> str:
    """Per-robot persistent memory file (shared by the CLI and the agent)."""
    return str(Path("~/.ohho/memory").expanduser() / f"{robot_id}.json")


class SpatialMemory:
    """Entity store with association, spatial/temporal queries and persistence."""

    def __init__(
        self,
        *,
        associate_radius: float = 0.75,
        max_events: int = 500,
        path: Optional[str] = None,
    ) -> None:
        self.associate_radius = associate_radius
        self.max_events = max_events
        self.path = path
        self._entities: list[Entity] = []
        self._events: list[MemoryEvent] = []
        self._lock = threading.RLock()
        if path and Path(path).expanduser().exists():
            self.load(path)

    # ── writing ───────────────────────────────────────────────────────────────
    def observe(
        self,
        label: str,
        x: float,
        y: float,
        *,
        t: Optional[float] = None,
        attrs: Optional[dict] = None,
    ) -> Entity:
        """Record a sighting. Same label within ``associate_radius`` of a known
        entity updates that entity (object permanence); otherwise a new entity
        is born."""
        now = t if t is not None else time.time()
        with self._lock:
            best: Optional[Entity] = None
            best_d = self.associate_radius
            for e in self._entities:
                if e.label != label:
                    continue
                d = math.hypot(e.x - x, e.y - y)
                if d <= best_d:
                    best, best_d = e, d
            if best is not None:
                # exponential position update keeps old evidence but tracks drift
                best.x = 0.7 * best.x + 0.3 * x
                best.y = 0.7 * best.y + 0.3 * y
                best.last_seen = now
                best.count += 1
                if attrs:
                    best.attrs.update(attrs)
                self._log(MemoryEvent(now, "updated", label, x, y))
                return best
            e = Entity(
                id=uuid.uuid4().hex[:8],
                label=label,
                x=x,
                y=y,
                first_seen=now,
                last_seen=now,
                attrs=dict(attrs or {}),
            )
            self._entities.append(e)
            self._log(MemoryEvent(now, "seen", label, x, y, note="new entity"))
            return e

    def note(self, text: str, *, t: Optional[float] = None) -> None:
        """Free-form event ("picked up the cup", "charging started", …)."""
        with self._lock:
            self._log(
                MemoryEvent(t if t is not None else time.time(), "note", "", note=text)
            )

    def _log(self, ev: MemoryEvent) -> None:
        self._events.append(ev)
        if len(self._events) > self.max_events:
            del self._events[: len(self._events) - self.max_events]

    # ── queries ───────────────────────────────────────────────────────────────
    def entities(self, label: Optional[str] = None) -> List[Entity]:
        with self._lock:
            out = [e for e in self._entities if label is None or e.label == label]
        return sorted(out, key=lambda e: e.last_seen, reverse=True)

    def where_is(self, label: str) -> Optional[Entity]:
        """Most recently seen entity with this label."""
        found = self.entities(label)
        return found[0] if found else None

    def near(self, x: float, y: float, radius: float = 1.0) -> List[Entity]:
        with self._lock:
            out = [e for e in self._entities if math.hypot(e.x - x, e.y - y) <= radius]
        return sorted(out, key=lambda e: math.hypot(e.x - x, e.y - y))

    def timeline(
        self, label: Optional[str] = None, since: Optional[float] = None
    ) -> List[MemoryEvent]:
        with self._lock:
            return [
                ev
                for ev in self._events
                if (label is None or ev.label == label)
                and (since is None or ev.t >= since)
            ]

    def describe(self, now: Optional[float] = None) -> str:
        """A compact natural-language summary (fed to the agent's prompt)."""
        ents = self.entities()
        if not ents:
            return "memory: empty"
        lines = [f"memory: {len(ents)} known object(s)"]
        for e in ents[:12]:
            lines.append(
                f"  {e.label} at ({e.x:.2f}, {e.y:.2f}) — seen {e.count}×, "
                f"last {e.age(now):.0f}s ago"
            )
        return "\n".join(lines)

    # ── persistence ───────────────────────────────────────────────────────────
    def save(self, path: Optional[str] = None) -> str:
        target = Path(path or self.path or "").expanduser()
        if not str(target):
            raise ValueError("no path given and no default path configured")
        target.parent.mkdir(parents=True, exist_ok=True)
        with self._lock:
            payload = {
                "version": 1,
                "entities": [asdict(e) for e in self._entities],
                "events": [asdict(ev) for ev in self._events],
            }
        target.write_text(json.dumps(payload, indent=2), encoding="utf-8")
        return str(target)

    def load(self, path: Optional[str] = None) -> None:
        source = Path(path or self.path or "").expanduser()
        data = json.loads(source.read_text(encoding="utf-8"))
        with self._lock:
            self._entities = [Entity(**e) for e in data.get("entities", [])]
            self._events = [MemoryEvent(**ev) for ev in data.get("events", [])]

    def clear(self) -> None:
        with self._lock:
            self._entities.clear()
            self._events.clear()


__all__ = [
    "Entity",
    "MemoryEvent",
    "SpatialMemory",
    "default_memory_path",
]
