import unittest

from ohho.adapters.errors import AdapterUnavailable
from ohho.adapters.unitree import (
    UnitreeDdsTransport,
    sportstate_to_telemetry,
    velocity_to_move,
)
from ohho.registry import get_spec
from ohho.schema import Velocity


class TestUnitreeMapping(unittest.TestCase):
    def test_velocity_to_move(self):
        self.assertEqual(velocity_to_move(Velocity(0.5, 0.1, 0.2)), (0.5, 0.1, 0.2))

    def test_sportstate_to_telemetry(self):
        t = sportstate_to_telemetry(
            {
                "position": [1.0, 2.0, 0.0],
                "velocity": [0.3, 0.0, 0.0],
                "yaw_speed": 0.4,
                "imu_rpy": [0.0, 0.0, 1.57],
                "battery": 0.8,
            }
        )
        self.assertAlmostEqual(t.odom.x, 1.0)
        self.assertAlmostEqual(t.odom.y, 2.0)
        self.assertAlmostEqual(t.odom.theta, 1.57)
        self.assertAlmostEqual(t.odom.vx, 0.3)
        self.assertAlmostEqual(t.odom.omega, 0.4)
        self.assertAlmostEqual(t.battery, 0.8)

    def test_sportstate_handles_missing_fields(self):
        t = sportstate_to_telemetry({})
        self.assertEqual(t.odom.x, 0.0)
        self.assertIsNone(t.battery)

    def test_connect_without_sdk_raises_clean_error(self):
        tp = UnitreeDdsTransport(get_spec("unitree-go2"), "eth0")
        # unitree_sdk2py is not installed in this environment.
        with self.assertRaises(AdapterUnavailable):
            tp.connect()

    def test_estop_safe_without_client(self):
        tp = UnitreeDdsTransport(get_spec("unitree-go2"))
        tp.emergency_stop()  # no client yet -> must not raise
        tp.send_velocity(Velocity(0.5))  # ignored while stopped / no client


if __name__ == "__main__":
    unittest.main()
