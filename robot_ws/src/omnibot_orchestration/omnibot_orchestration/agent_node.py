"""agent_node — runs the continuous AgentHarness on the robot.

This is the ROS edge of ``agent_engine``: it builds the harness with
ROS-backed tools (publishing to ``mission_planner`` and the muxes), a cloud/
on-device hybrid reasoner, working + episodic memory, the learning-engine
safety verifier and reflector, then drives ``harness.tick()`` from a timer.

The harness's deliberative step (LLM planning) can take seconds, so each tick
runs in a single-worker thread and the timer skips while one is in flight —
the ROS executor stays responsive to new commands and e-stop.

Goals arrive as natural language on ``/ai/command`` (or structured on
``/agent/goal``); world state arrives fused on ``/agent/world_state`` from
``world_state_node``. Requires ``pip install -e agent_engine`` in the
environment (the pure-Python harness core), like the other *_engine packages.
"""

import json
import time
from concurrent.futures import ThreadPoolExecutor

import rclpy
from geometry_msgs.msg import PoseStamped  # noqa: F401  (future: structured goals)
from rclpy.node import Node
from std_msgs.msg import Bool, String

from agent_engine.core.blackboard import WorldState
from agent_engine.core.harness import AgentHarness
from agent_engine.core.tools import ToolParam, ToolRegistry, ToolSpec
from agent_engine.core.types import ToolResult
from agent_engine.memory.working_memory import WorkingMemory
from agent_engine.reasoners.claude_tool_caller import ClaudeToolCallingReasoner
from agent_engine.reasoning.factory import build_reasoning_router


class AgentNode(Node):
    def __init__(self) -> None:
        super().__init__("agent_node")
        self.declare_parameter("agent_hz", 1.0)
        self.declare_parameter("max_steps_per_goal", 12)
        self.declare_parameter("anthropic_api_key", "")
        self.declare_parameter("prefer_local_reasoning", False)
        self.declare_parameter("replay_dataset_path", "~/datasets/agent_episodes")
        self.declare_parameter("entity_memory_path", "~/.omnibot/entity_memory.json")
        self.declare_parameter("environment", "real")

        self._latest_world = WorldState()

        # Publishers (the agent's actuators, via mission_planner + muxes).
        self._pub_mission = self.create_publisher(String, "/mission/command", 10)
        self._pub_cancel = self.create_publisher(String, "/mission/cancel", 10)
        self._pub_mode = self.create_publisher(String, "/control_mode", 10)
        self._pub_arm_mode = self.create_publisher(String, "/arm/cmd_mode", 10)
        self._pub_human = self.create_publisher(String, "/ai/response_needed", 10)
        self._pub_status = self.create_publisher(String, "/ai/status", 10)

        self._harness = self._build_harness()

        self.create_subscription(String, "/agent/world_state", self._on_world, 10)
        self.create_subscription(String, "/ai/command", self._on_command, 10)
        self.create_subscription(String, "/agent/goal", self._on_command, 10)
        self.create_subscription(Bool, "/emergency_stop", self._on_estop, 10)
        self.create_subscription(
            String, "/agent/human_response", self._on_human_response, 10
        )

        self._worker = ThreadPoolExecutor(max_workers=1)
        self._busy = False
        hz = float(self.get_parameter("agent_hz").value)
        self.create_timer(1.0 / max(hz, 0.1), self._on_timer)
        self.get_logger().info(f"agent_node ticking the harness at {hz} Hz")

    # -- Perceptor port ----------------------------------------------------
    def perceive(self) -> WorldState:
        return self._latest_world

    # -- subscriptions -----------------------------------------------------
    def _on_world(self, msg: String) -> None:
        try:
            self._latest_world = WorldState.from_dict(json.loads(msg.data))
        except (ValueError, TypeError) as exc:
            self.get_logger().warning(f"bad /agent/world_state: {exc}")

    def _on_command(self, msg: String) -> None:
        if msg.data.strip():
            self._harness.submit_goal(msg.data.strip())

    def _on_estop(self, msg: Bool) -> None:
        self._harness.set_emergency_stop(bool(msg.data))

    def _on_human_response(self, msg: String) -> None:
        self._harness.provide_human_response(msg.data)

    # -- the tick loop -----------------------------------------------------
    def _on_timer(self) -> None:
        if self._busy:
            return
        self._busy = True
        self._worker.submit(self._tick)

    def _tick(self) -> None:
        try:
            report = self._harness.tick()
            self._pub_status.publish(
                String(
                    data=f"phase={report.next_phase.value} "
                    f"goal={report.goal_id} detail={report.detail}"
                )
            )
        except Exception as exc:  # noqa: BLE001 — a tick must never kill the node
            self.get_logger().error(f"harness tick failed: {exc}")
        finally:
            self._busy = False

    # -- harness assembly --------------------------------------------------
    def _build_harness(self) -> AgentHarness:
        tools = self._build_tools()
        api_key = str(self.get_parameter("anthropic_api_key").value)
        prefer_local = bool(self.get_parameter("prefer_local_reasoning").value)
        router = build_reasoning_router(api_key=api_key, prefer_local=prefer_local)
        reasoner = ClaudeToolCallingReasoner(router, tools)

        memory = self._build_memory()
        reflector, verifier = self._build_learning_adapters()

        return AgentHarness(
            self,  # node implements the Perceptor port
            reasoner,
            tools,
            memory=memory,
            reflector=reflector,
            verifier=verifier,
            max_steps_per_goal=int(self.get_parameter("max_steps_per_goal").value),
            logger=lambda m: self.get_logger().info(m),
        )

    def _build_tools(self) -> ToolRegistry:
        reg = ToolRegistry()

        def _mission(cmd: str) -> ToolResult:
            self._pub_mission.publish(String(data=cmd))
            return ToolResult(True, f"published /mission/command: {cmd}")

        reg.register(
            ToolSpec(
                "navigate_to",
                "Drive to a named location via Nav2.",
                lambda location: _mission(f"navigate:{location}"),
                [ToolParam("location", "named location, e.g. 'kitchen'")],
            )
        )
        reg.register(
            ToolSpec(
                "navigate_then_execute",
                "Navigate to a location, then run a manipulation task there.",
                lambda location, task: _mission(f"navigate:{location},vla:{task}"),
                [ToolParam("location", "named location"), ToolParam("task", "task")],
            )
        )
        reg.register(
            ToolSpec(
                "run_skill",
                "Run a skill now: skill in {vla, rl_nav, rl_arm}, task is the goal.",
                lambda skill, task: _mission(f"{skill}:{task}"),
                [ToolParam("skill", "vla|rl_nav|rl_arm"), ToolParam("task", "task")],
            )
        )

        def _set_mode(mode: str) -> ToolResult:
            (self._pub_arm_mode if mode == "rl_arm" else self._pub_mode).publish(
                String(data=mode)
            )
            return ToolResult(True, f"control mode -> {mode}")

        reg.register(
            ToolSpec(
                "set_control_mode",
                "Set base/arm control mode: nav2|vla|teleop|rl_nav|policy|rl_arm.",
                _set_mode,
                [ToolParam("mode", "control mode")],
            )
        )
        reg.register(
            ToolSpec(
                "cancel_mission",
                "Abort the current mission.",
                lambda: (
                    self._pub_cancel.publish(String(data="cancel"))
                    or ToolResult(True, "mission cancelled")
                ),
            )
        )
        reg.register(
            ToolSpec(
                "ask_human",
                "Ask the operator a clarifying question and wait for a reply.",
                lambda question: (
                    self._pub_human.publish(String(data=question))
                    or ToolResult(True, "asked operator")
                ),
                [ToolParam("question", "the question")],
            )
        )
        reg.register(
            ToolSpec(
                "wait",
                "Pause briefly before re-assessing the world.",
                lambda seconds=1.0: (
                    time.sleep(min(float(seconds), 5.0)) or ToolResult(True, "waited")
                ),
                [ToolParam("seconds", "seconds (<=5)", required=False, type="number")],
            )
        )
        reg.register(
            ToolSpec(
                "query_world_state",
                "Get a fresh natural-language summary of the world state.",
                lambda: ToolResult(True, self._latest_world.summarize()),
            )
        )
        return reg

    def _build_memory(self) -> WorkingMemory:
        entity = None
        try:
            from omnibot_orchestration.memory.entity_memory import EntityMemory

            entity = EntityMemory(str(self.get_parameter("entity_memory_path").value))
        except Exception as exc:  # noqa: BLE001
            self.get_logger().warning(f"entity memory unavailable: {exc}")
        episode_source = None
        try:
            from agent_engine.integrations.learning_engine import ReplayMemorySource
            from learning_engine.data.replay_dataset import ReplayDataset

            ds = ReplayDataset(
                str(self.get_parameter("replay_dataset_path").value),
                store_images=False,
            )
            episode_source = ReplayMemorySource(ds)
        except Exception as exc:  # noqa: BLE001
            self.get_logger().info(f"episodic memory source unavailable: {exc}")
        return WorkingMemory(entity_store=entity, episode_source=episode_source)

    def _build_learning_adapters(self):
        """Reflector (+ episode persistence) and safety verifier from
        learning_engine; both optional so the node runs without it installed."""
        reflector = verifier = None
        try:
            from agent_engine.integrations.learning_engine import (
                EvaluatorReflector,
                WorldStateVerifier,
            )
            from learning_engine.data.replay_dataset import ReplayDataset

            ds = ReplayDataset(
                str(self.get_parameter("replay_dataset_path").value),
                store_images=False,
            )
            reflector = EvaluatorReflector(
                dataset=ds,
                environment=str(self.get_parameter("environment").value),
            )
            verifier = WorldStateVerifier()
        except Exception as exc:  # noqa: BLE001
            self.get_logger().warning(
                f"learning_engine adapters unavailable (no reflection/verify): {exc}"
            )
        return reflector, verifier


def main(args=None) -> None:
    rclpy.init(args=args)
    node = AgentNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
