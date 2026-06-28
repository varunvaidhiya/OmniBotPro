import unittest

from ohho.adapters.sim import SimTransport
from ohho.registry import get_spec
from ohho.schema import ConnectionState, Velocity


class TestSimTransport(unittest.TestCase):
    def _sim(self, robot_id="sim"):
        tp = SimTransport(get_spec(robot_id))
        tp.connect()
        return tp

    def test_connect_status(self):
        tp = self._sim()
        self.assertEqual(tp.status().state, ConnectionState.CONNECTED)
        self.assertEqual(tp.protocol, "simulated")

    def test_forward_integration(self):
        tp = self._sim()
        tp.send_velocity(Velocity(linear_x=1.0))
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.x, 1.0, places=5)
        self.assertAlmostEqual(tp.read().odom.y, 0.0, places=5)

    def test_rotation_integration(self):
        tp = self._sim()
        tp.send_velocity(Velocity(angular_z=1.0))
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.theta, 1.0, places=5)

    def test_emergency_stop(self):
        tp = self._sim()
        tp.send_velocity(Velocity(linear_x=1.0))
        tp.step(1.0)  # x -> 1.0
        tp.emergency_stop()
        tp.send_velocity(Velocity(linear_x=1.0))  # ignored while stopped
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.x, 1.0, places=5)
        tp.release_stop()
        tp.send_velocity(Velocity(linear_x=1.0))
        tp.step(1.0)
        self.assertAlmostEqual(tp.read().odom.x, 2.0, places=5)

    def test_joints_servo_toward_target(self):
        tp = self._sim("omnibot")  # has a 6-DOF arm
        tp.send_joint_command("arm_gripper", 1.0)
        for _ in range(200):  # 10s at dt=0.05 — plenty to converge at 2 rad/s
            tp.step(0.05)
        positions = {j.name: j.position for j in tp.read().joints}
        self.assertAlmostEqual(positions["arm_gripper"], 1.0, places=2)

    def test_telemetry_callback(self):
        tp = self._sim()
        seen = []
        tp.on_telemetry(seen.append)
        tp._emit_telemetry(tp.step(0.1))
        self.assertEqual(len(seen), 1)


if __name__ == "__main__":
    unittest.main()
