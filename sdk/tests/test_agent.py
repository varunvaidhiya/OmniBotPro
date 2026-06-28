import unittest

from ohho.adapters.sim import SimTransport
from ohho.agent import Agent, ScriptedBrain
from ohho.registry import get_spec
from ohho.robot import Robot
from ohho.runtime import NativeRuntime


class TestAgent(unittest.TestCase):
    def _bot(self, robot_id="sim"):
        spec = get_spec(robot_id)
        tp = SimTransport(spec)
        tp.connect()
        return Robot(spec, tp, NativeRuntime())

    def test_scripted_brain_runs(self):
        log = Agent(self._bot(), brain=ScriptedBrain()).run("test goal")
        self.assertTrue(any("goal: test goal" in line for line in log))
        self.assertTrue(any("done" in line for line in log))

    def test_default_brain(self):
        agent = Agent(self._bot())
        self.assertIsInstance(agent.run("explore"), list)


if __name__ == "__main__":
    unittest.main()
