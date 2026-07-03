import unittest

from ohho.adapters.sim import SimTransport
from ohho.agent import Agent, ScriptedBrain
from ohho.brains import harness_available
from ohho.registry import get_spec
from ohho.robot import Robot
from ohho.runtime import NativeRuntime

# agent_engine lives in the repo and needs numpy; without it these tests skip
# (the SDK's base install must keep working — Agent falls back to ScriptedBrain).
_HAS_AGENT_ENGINE = harness_available()
_SKIP_REASON = "agent_engine (+ numpy) not importable — install 'ohho-os[agent]'"


def _bot(robot_id="omnibot"):
    spec = get_spec(robot_id)
    tp = SimTransport(spec)
    tp.connect()
    return Robot(spec, tp, NativeRuntime())


@unittest.skipUnless(_HAS_AGENT_ENGINE, _SKIP_REASON)
class TestToolRegistry(unittest.TestCase):
    def test_omnibot_has_drive_and_arm_tools(self):
        from ohho.brains import build_tool_registry

        reg = build_tool_registry(_bot("omnibot"))
        self.assertIn("drive", reg)
        self.assertIn("move_joints", reg)
        self.assertIn("stop", reg)
        self.assertIn("emergency_stop", reg)
        self.assertIn("get_telemetry", reg)
        self.assertIn("get_status", reg)

    def test_go2_has_drive_but_no_arm(self):
        from ohho.brains import build_tool_registry

        reg = build_tool_registry(_bot("unitree-go2"))
        self.assertIn("drive", reg)
        self.assertNotIn("move_joints", reg)

    def test_sim_bot_has_drive_but_no_arm(self):
        from ohho.brains import build_tool_registry

        reg = build_tool_registry(_bot("sim"))
        self.assertIn("drive", reg)
        self.assertNotIn("move_joints", reg)

    def test_tool_dispatch_drive(self):
        from ohho.brains import build_tool_registry
        from agent_engine.core.types import ToolCall

        bot = _bot("sim")
        reg = build_tool_registry(bot)
        result = reg.dispatch(ToolCall("drive", {"vx": 0.1, "vy": 0.0, "w": 0.0}))
        self.assertTrue(result.ok)

    def test_tool_dispatch_get_telemetry(self):
        from ohho.brains import build_tool_registry
        from agent_engine.core.types import ToolCall

        bot = _bot("sim")
        reg = build_tool_registry(bot)
        result = reg.dispatch(ToolCall("get_telemetry", {}))
        self.assertTrue(result.ok)
        self.assertIn("odom", result.output)

    def test_nav_and_memory_tools_registered(self):
        from ohho.brains import build_tool_registry
        from ohho.memory import SpatialMemory
        from ohho.nav import Navigator
        from ohho.perception import SimPerceptor

        bot = _bot("sim")
        reg = build_tool_registry(
            bot,
            navigator=Navigator(bot),
            memory=SpatialMemory(),
            perceptor=SimPerceptor(bot),
        )
        for tool in (
            "navigate_to",
            "explore",
            "where_is",
            "objects_near",
            "look_around",
        ):
            self.assertIn(tool, reg)

    def test_memory_tools_answer(self):
        from agent_engine.core.types import ToolCall
        from ohho.brains import build_tool_registry
        from ohho.memory import SpatialMemory

        memory = SpatialMemory()
        memory.observe("cup", 1.0, 2.0)
        reg = build_tool_registry(_bot("sim"), memory=memory)
        result = reg.dispatch(ToolCall("where_is", {"label": "cup"}))
        self.assertTrue(result.ok)
        self.assertIn("cup", result.output)

    def test_tool_to_anthropic_schema(self):
        from ohho.brains import build_tool_registry

        reg = build_tool_registry(_bot("omnibot"))
        schema = reg.to_anthropic_schema()
        names = [s["name"] for s in schema]
        self.assertIn("drive", names)
        self.assertIn("move_joints", names)
        # drive should have vx, vy, w params
        drive_schema = next(s for s in schema if s["name"] == "drive")
        self.assertIn("vx", drive_schema["input_schema"]["properties"])


@unittest.skipUnless(_HAS_AGENT_ENGINE, _SKIP_REASON)
class TestRobotPerceptor(unittest.TestCase):
    def test_perceive_returns_world_state(self):
        from ohho.brains import RobotPerceptor

        bot = _bot("omnibot")
        perc = RobotPerceptor(bot)
        ws = perc.perceive()
        self.assertAlmostEqual(ws.base_pose[0], 0.0)
        self.assertEqual(len(ws.arm_joint_positions), 6)

    def test_perceive_reflects_motion(self):
        from ohho.brains import RobotPerceptor

        bot = _bot("sim")
        bot.drive(vx=0.1)
        bot.transport.step(0.1)  # type: ignore[attr-defined]
        perc = RobotPerceptor(bot)
        ws = perc.perceive()
        self.assertAlmostEqual(ws.base_velocity[0], 0.1, places=3)


@unittest.skipUnless(_HAS_AGENT_ENGINE, _SKIP_REASON)
class TestHarnessBrain(unittest.TestCase):
    def test_harness_brain_runs_with_echo(self):
        from ohho.brains import HarnessBrain

        bot = _bot("sim")
        brain = HarnessBrain(max_steps=3)
        log = brain.run(bot, "explore the room", max_steps=3)
        self.assertTrue(any("goal: explore the room" in line for line in log))
        self.assertTrue(any("done" in line for line in log))
        self.assertTrue(any("tools:" in line for line in log))

    def test_harness_brain_completes_goal(self):
        from ohho.brains import HarnessBrain

        bot = _bot("omnibot")
        brain = HarnessBrain(max_steps=3)
        log = brain.run(bot, "pick up the cup", max_steps=3)
        # Echo backend returns goal_complete=true → harness reflects + ends
        self.assertTrue(any("reflection:" in line for line in log))

    def test_agent_uses_harness_when_available(self):
        """Agent(bot) should pick HarnessBrain over ScriptedBrain when
        agent_engine is importable."""
        agent = Agent(_bot("sim"))
        # In this env agent_engine is installed, so the brain should NOT be
        # the scripted fallback.
        self.assertNotIsInstance(agent.brain, ScriptedBrain)


if __name__ == "__main__":
    unittest.main()
