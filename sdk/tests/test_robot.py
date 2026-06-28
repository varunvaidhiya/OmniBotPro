import unittest

from ohho import capabilities as caps
from ohho.adapters.sim import SimTransport
from ohho.registry import RobotSpec, get_spec
from ohho.robot import Robot
from ohho.runtime import NativeRuntime


def _make(spec):
    """Build a Robot over a sim transport without starting any threads, so tests
    can step physics deterministically."""
    tp = SimTransport(spec)
    tp.connect()
    rt = NativeRuntime()
    return Robot(spec, tp, rt), tp


class TestRobot(unittest.TestCase):
    def test_has(self):
        bot, _ = _make(get_spec("omnibot"))
        self.assertTrue(bot.has(caps.MANIPULATION))
        bot2, _ = _make(get_spec("sim"))
        self.assertFalse(bot2.has(caps.MANIPULATION))

    def test_drive_clamps_to_limits(self):
        spec = get_spec("omnibot")  # max_lin 0.2
        bot, tp = _make(spec)
        bot.drive(vx=10.0)
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.x, 0.2, places=5)

    def test_non_holonomic_ignores_vy(self):
        spec = RobotSpec(
            id="diff-test",
            name="Diff",
            category="wheeled",
            capabilities=(caps.BASE_DRIVE,),  # no holonomic
            max_lin=1.0,
            max_ang=1.0,
        )
        bot, tp = _make(spec)
        bot.drive(vy=1.0)
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.y, 0.0, places=6)

    def test_holonomic_allows_vy(self):
        bot, tp = _make(get_spec("sim"))  # holonomic, max_lin 1.0
        bot.drive(vy=0.5)
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.y, 0.5, places=5)

    def test_move_joints_noop_without_manipulation(self):
        bot, tp = _make(get_spec("sim"))
        bot.move_joints([1, 2, 3])  # should not raise
        self.assertEqual(tp.read().joints, [])

    def test_move_joints_sets_targets(self):
        bot, tp = _make(get_spec("omnibot"))
        bot.move_joints([0.5] * 6)
        for _ in range(100):
            tp.step(0.05)
        positions = {j.name: j.position for j in tp.read().joints}
        self.assertAlmostEqual(positions["arm_shoulder_pan"], 0.5, places=2)

    def test_emergency_stop(self):
        bot, tp = _make(get_spec("sim"))
        bot.emergency_stop()
        bot.drive(vx=1.0)
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.x, 0.0, places=6)

    def test_context_manager(self):
        spec = get_spec("sim")
        tp = SimTransport(spec)
        tp.connect()
        rt = NativeRuntime()
        with Robot(spec, tp, rt) as bot:
            self.assertFalse(bot.simulated is None)
        # disconnected cleanly on exit (no exception)


if __name__ == "__main__":
    unittest.main()
