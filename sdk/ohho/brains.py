"""Agent-engine wiring — bridges ``ohho.Robot`` to ``agent_engine.AgentHarness``.

This module is the M2 "real brain": it builds a :class:`ToolRegistry` from the
robot's capabilities, a :class:`RobotPerceptor` that turns ``Robot.telemetry()``
into a :class:`WorldState`, and a :class:`ClaudeToolCallingReasoner` over a
:class:`ReasoningRouter` (cloud Claude when ``ANTHROPIC_API_KEY`` + ``anthropic``
are present, an echo fallback otherwise). Everything is wrapped behind the
:class:`Brain` protocol so ``Agent(bot).run(goal)`` works unchanged.

Heavy imports (``agent_engine`` needs numpy, ``anthropic`` is optional) are lazy:
importing ``ohho.agent`` never touches this module. It loads only when
``_default_brain()`` tries to upgrade from ``ScriptedBrain``.
"""

from __future__ import annotations

from typing import List

from . import capabilities as caps
from .robot import Robot


def harness_available() -> bool:
    """True when the full agent stack can actually be imported.

    ``agent_engine`` lives in this repo and needs numpy — merely *finding* the
    package is not enough (its import explodes without numpy), so this probe
    performs the real imports and treats any failure as "not available".
    """
    try:
        import agent_engine.core.harness  # noqa: F401
        import agent_engine.core.tools  # noqa: F401
        import agent_engine.reasoners.claude_tool_caller  # noqa: F401
    except Exception:
        return False
    return True


def build_tool_registry(robot: Robot):
    """Build an ``agent_engine.ToolRegistry`` from the robot's capabilities.

    Each capability maps to a callable tool the LLM can invoke:
      - ``base.drive`` → ``drive(vx, vy, w)``
      - ``manipulation`` → ``move_joints(j0, j1, …)``
      - always → ``stop``, ``emergency_stop``, ``get_telemetry``, ``get_status``
    """
    from agent_engine.core.tools import ToolParam, ToolRegistry, ToolSpec

    reg = ToolRegistry()

    if robot.has(caps.BASE_DRIVE):
        reg.register(
            ToolSpec(
                name="drive",
                description=(
                    "Drive the robot base. vx=forward m/s, vy=left m/s "
                    "(holonomic only), w=angular rad/s. Clamped to limits."
                ),
                params=[
                    ToolParam("vx", "forward velocity m/s", type="number"),
                    ToolParam(
                        "vy", "lateral velocity m/s (0 if not holonomic)", type="number"
                    ),
                    ToolParam("w", "angular velocity rad/s", type="number"),
                ],
                handler=lambda vx=0.0, vy=0.0, w=0.0: robot.drive(
                    vx=float(vx), vy=float(vy), w=float(w)
                ),
                low_level=True,
            )
        )

    if robot.has(caps.MANIPULATION):
        n = len(robot.spec.joint_names)
        reg.register(
            ToolSpec(
                name="move_joints",
                description=(
                    f"Command the {n}-DOF arm joints (radians, in spec order: "
                    f"{', '.join(robot.spec.joint_names)})."
                ),
                params=[
                    ToolParam(
                        f"j{i}",
                        f"joint {i} target (rad)",
                        type="number",
                    )
                    for i in range(n)
                ],
                handler=lambda **kwargs: robot.move_joints(
                    [kwargs[f"j{i}"] for i in range(n)]
                ),
            )
        )

    reg.register(
        ToolSpec(
            name="stop",
            description="Stop the base (zero velocity).",
            params=[],
            handler=lambda: robot.stop(),
        )
    )
    reg.register(
        ToolSpec(
            name="emergency_stop",
            description="Emergency stop — zero all motion and disable actuators.",
            params=[],
            handler=lambda: robot.emergency_stop(),
            low_level=True,
        )
    )
    reg.register(
        ToolSpec(
            name="get_telemetry",
            description="Read the current telemetry (odometry, joints, battery).",
            params=[],
            handler=lambda: _telemetry_str(robot),
        )
    )
    reg.register(
        ToolSpec(
            name="get_status",
            description="Read the transport connection status.",
            params=[],
            handler=lambda: _status_str(robot),
        )
    )
    return reg


def _telemetry_str(robot: Robot) -> str:
    t = robot.telemetry()
    parts = []
    if t.odom:
        parts.append(
            f"odom(x={t.odom.x:.3f},y={t.odom.y:.3f},th={t.odom.theta:.3f},"
            f"vx={t.odom.vx:.3f},vy={t.odom.vy:.3f},w={t.odom.omega:.3f})"
        )
    if t.joints:
        parts.append(
            "joints(" + ",".join(f"{j.name}={j.position:.3f}" for j in t.joints) + ")"
        )
    if t.battery is not None:
        parts.append(f"battery={t.battery * 100:.0f}%")
    return " ".join(parts) or "no telemetry"


def _status_str(robot: Robot) -> str:
    s = robot.status()
    return f"state={s.state.value} protocol={s.protocol} label={s.label}"


class RobotPerceptor:
    """Reads ``Robot.telemetry()`` and builds an ``agent_engine.WorldState``."""

    def __init__(self, robot: Robot) -> None:
        self._robot = robot

    def perceive(self):
        from agent_engine.core.blackboard import WorldState

        t = self._robot.telemetry()
        odom = t.odom
        arm = [j.position for j in t.joints] if t.joints else []
        return WorldState(
            base_pose=(odom.x, odom.y, odom.theta) if odom else (0.0, 0.0, 0.0),
            base_velocity=(odom.vx, odom.vy, odom.omega) if odom else (0.0, 0.0, 0.0),
            arm_joint_positions=arm,
            battery_pct=t.battery,
            mission_phase="active",
        )


def build_reasoner(tools):
    """Build a ``ClaudeToolCallingReasoner`` over a ``ReasoningRouter``.

    Cloud Claude is preferred when ``ANTHROPIC_API_KEY`` + ``anthropic`` are
    available; an ``EchoBackend`` that returns a goal-complete stub is the
    always-available fallback (so the loop runs offline / in tests).
    """
    from agent_engine.reasoners.claude_tool_caller import ClaudeToolCallingReasoner
    from agent_engine.reasoning.router import EchoBackend, ReasoningRouter

    backends = []
    try:
        from agent_engine.reasoning.cloud_claude import CloudClaudeBackend

        cloud = CloudClaudeBackend()
        if cloud.available():
            backends.append(cloud)
    except Exception:
        pass

    # Echo fallback — returns a goal-complete JSON stub so the harness
    # completes gracefully when no LLM is wired.
    echo = EchoBackend(
        responder=lambda prompt, system: (
            '{"rationale":"no LLM available — completing goal",'
            '"tool_calls":[],"goal_complete":true,"give_up":false,"ask_human":""}'
        )
    )
    backends.append(echo)

    router = ReasoningRouter(backends)
    return ClaudeToolCallingReasoner(router, tools)


class HarnessBrain:
    """Real agent brain — wraps ``agent_engine.AgentHarness`` behind ``Brain``.

    Falls back to the echo backend (goal-complete stub) when no LLM is
    available, so ``Agent(bot).run(goal)`` always returns a log. With
    ``ANTHROPIC_API_KEY`` + ``[agent]`` extra installed, it performs a real
    perceive → reason → act → reflect loop.
    """

    def __init__(self, max_steps: int = 12) -> None:
        self.max_steps = max_steps

    def run(self, robot: Robot, goal: str, max_steps: int = 20) -> List[str]:
        try:
            from agent_engine.core.harness import AgentHarness
        except Exception as e:
            # agent_engine (or its numpy dependency) is missing — degrade to
            # the zero-dependency scripted brain instead of crashing the CLI.
            from .agent import ScriptedBrain

            log = [
                f"harness brain unavailable ({e.__class__.__name__}: {e}) — "
                "falling back to the scripted brain. Install numpy + the "
                "repo's agent_engine (pip install 'ohho-os[agent]') for the "
                "full perceive→reason→act→reflect loop."
            ]
            return log + ScriptedBrain().run(robot, goal, max_steps)

        tools = build_tool_registry(robot)
        perceptor = RobotPerceptor(robot)
        reasoner = build_reasoner(tools)
        log: list[str] = [
            f"goal: {goal}",
            f"robot: {robot.spec.name} ({robot.spec.category})",
            f"capabilities: {', '.join(robot.spec.capabilities) or 'none'}",
            f"tools: {', '.join(tools.names())}",
        ]
        harness = AgentHarness(
            perceptor=perceptor,
            reasoner=reasoner,
            tools=tools,
            max_steps_per_goal=min(max_steps, self.max_steps),
            logger=lambda msg: log.append(msg),
        )
        harness.submit_goal(goal)
        reports = harness.run_until_idle(max_ticks=max_steps * 4)
        for r in reports:
            log.append(f"  [{r.phase.value}→{r.next_phase.value}] {r.detail}")
        refl = harness.last_reflection
        if refl is not None:
            log.append(
                f"reflection: success={refl.success} "
                f"confidence={refl.confidence:.2f} — {refl.summary}"
            )
        log.append("done (harness brain)")
        return log
