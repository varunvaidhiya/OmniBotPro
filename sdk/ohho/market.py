"""Skill & robot market registry — discover and share robot behaviors.

A *skill* is a named, reusable behavior package: a manifest that says which
robot capabilities it needs, a short description, and an entry point. Skills
register at import time (just like robot adapters); ``ohho market`` lists them.

The registry is pure Python (no network, no pip) — it's a catalog, not a
package manager. A skill's ``handler`` is a callable that takes a
:class:`~ohho.robot.Robot` and optional kwargs and does something useful.

Example — registering a skill::

    from ohho.market import skill, Skill

    @skill("patrol", "Drive a square pattern", requires=["base.drive"])
    def patrol(robot, side=1.0, speed=0.1):
        for _ in range(4):
            robot.drive(vx=speed)
            time.sleep(side / speed)
            robot.drive(w=0.5)
            time.sleep(3.14)
        robot.stop()

Example — using a skill::

    from ohho.market import run_skill
    run_skill("patrol", bot, side=2.0)
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List, Optional

from .robot import Robot

SkillHandler = Callable[..., Any]


@dataclass
class Skill:
    """A registered skill — a named, reusable robot behavior."""

    name: str
    description: str
    handler: SkillHandler
    requires: List[str] = field(default_factory=list)
    author: str = ""
    version: str = "0.1.0"
    tags: List[str] = field(default_factory=list)

    def can_run(self, robot: Robot) -> bool:
        """Check if the robot has all required capabilities."""
        return all(robot.has(cap) for cap in self.requires)

    def run(self, robot: Robot, **kwargs: Any) -> Any:
        """Execute the skill on a robot. Raises ``SkillRequirementsNotMet`` if
        the robot lacks required capabilities."""
        if not self.can_run(robot):
            missing = [c for c in self.requires if not robot.has(c)]
            raise SkillRequirementsNotMet(
                f"skill '{self.name}' requires capabilities: {missing}. "
                f"Robot '{robot.spec.id}' has: {list(robot.spec.capabilities)}"
            )
        return self.handler(robot, **kwargs)


class SkillRequirementsNotMet(RuntimeError):
    """Raised when a robot lacks the capabilities a skill needs."""


_REGISTRY: Dict[str, Skill] = {}


def skill(
    name: str,
    description: str,
    requires: Optional[List[str]] = None,
    author: str = "",
    version: str = "0.1.0",
    tags: Optional[List[str]] = None,
) -> Callable[[SkillHandler], SkillHandler]:
    """Decorator: register a function as a skill.

    ::

        @skill("wave", "Wave the arm", requires=["manipulation"])
        def wave(robot):
            robot.move_joints([0, 0, 0, 0, 0, 0.5])
            time.sleep(0.5)
            robot.move_joints([0, 0, 0, 0, 0, 0.0])
    """

    def deco(fn: SkillHandler) -> SkillHandler:
        reg = Skill(
            name=name,
            description=description,
            handler=fn,
            requires=requires or [],
            author=author,
            version=version,
            tags=tags or [],
        )
        register_skill(reg)
        return fn

    return deco


def register_skill(s: Skill) -> Skill:
    """Add (or replace) a skill in the registry."""
    _REGISTRY[s.name] = s
    return s


def get_skill(name: str) -> Skill:
    """Look up a skill by name."""
    try:
        return _REGISTRY[name]
    except KeyError:
        raise KeyError(
            f"unknown skill '{name}'. Available: {sorted(_REGISTRY)}"
        ) from None


def list_skills() -> List[Skill]:
    """All registered skills, sorted by name."""
    return [_REGISTRY[k] for k in sorted(_REGISTRY)]


def run_skill(name: str, robot: Robot, **kwargs: Any) -> Any:
    """Look up and run a skill by name."""
    return get_skill(name).run(robot, **kwargs)


# ── Built-in skills ───────────────────────────────────────────────────────────


@skill(
    "patrol",
    "Drive a square patrol pattern. Args: side (m), speed (m/s).",
    requires=["base.drive"],
    tags=["navigation", "demo"],
)
def _patrol(robot: Robot, side: float = 1.0, speed: float = 0.1) -> str:
    import time

    for _ in range(4):
        robot.drive(vx=speed)
        time.sleep(side / max(speed, 0.01))
        robot.stop()
        robot.drive(w=0.5)
        time.sleep(3.14)
        robot.stop()
    return "patrol complete"


@skill(
    "wave",
    "Wave the arm gripper. Args: reps (count).",
    requires=["manipulation"],
    tags=["arm", "demo"],
)
def _wave(robot: Robot, reps: int = 3) -> str:
    import time

    for _ in range(reps):
        robot.move_joints([0, 0, 0, 0, 0, 0.5])
        time.sleep(0.3)
        robot.move_joints([0, 0, 0, 0, 0, 0.0])
        time.sleep(0.3)
    return f"waved {reps} times"


@skill(
    "stop",
    "Emergency stop the robot.",
    requires=[],
    tags=["safety"],
)
def _stop(robot: Robot) -> str:
    robot.emergency_stop()
    return "emergency stop engaged"


@skill(
    "status",
    "Print the robot's telemetry and connection status.",
    requires=[],
    tags=["diagnostics"],
)
def _status(robot: Robot) -> str:
    t = robot.telemetry()
    s = robot.status()
    parts = [f"status={s.state.value}", f"protocol={s.protocol}"]
    if t.odom:
        parts.append(f"odom(x={t.odom.x:.2f},y={t.odom.y:.2f},th={t.odom.theta:.2f})")
    if t.battery is not None:
        parts.append(f"battery={t.battery * 100:.0f}%")
    return " ".join(parts)


__all__ = [
    "Skill",
    "SkillRequirementsNotMet",
    "skill",
    "register_skill",
    "get_skill",
    "list_skills",
    "run_skill",
]
