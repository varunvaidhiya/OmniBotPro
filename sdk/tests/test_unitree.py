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
        with self.assertRaises(AdapterUnavailable):
            tp.connect()

    def test_estop_safe_without_client(self):
        tp = UnitreeDdsTransport(get_spec("unitree-go2"))
        tp.emergency_stop()
        tp.send_velocity(Velocity(0.5))


class TestUnitreeStatePolling(unittest.TestCase):
    """Verify the DDS state reader thread maps state dicts → telemetry."""

    def test_state_factory_feeds_telemetry(self):
        state = {
            "position": [1.0, 2.0, 0.0],
            "velocity": [0.3, 0.0, 0.0],
            "yaw_speed": 0.4,
            "imu_rpy": [0.0, 0.0, 1.57],
            "battery": 0.9,
        }

        def factory():
            return lambda: state

        tp = UnitreeDdsTransport(get_spec("unitree-go2"), "eth0", state_factory=factory)
        seen = []
        tp.on_telemetry(seen.append)
        tp.connect()
        try:
            import time as _t

            _t.sleep(0.15)
            self.assertGreaterEqual(len(seen), 1)
            t = tp.read()
            self.assertAlmostEqual(t.odom.x, 1.0)
            self.assertAlmostEqual(t.odom.vx, 0.3)
            self.assertAlmostEqual(t.battery, 0.9)
        finally:
            tp.disconnect()

    def test_state_factory_none_is_safe(self):
        def factory():
            return lambda: None

        tp = UnitreeDdsTransport(get_spec("unitree-go2"), state_factory=factory)
        tp.connect()
        try:
            t = tp.read()
            self.assertEqual(t.odom.x, 0.0)
        finally:
            tp.disconnect()

    def test_telemetry_callback_fires_on_state(self):
        def factory():
            return lambda: {
                "position": [1.0, 0.0, 0.0],
                "velocity": [0.2, 0.0, 0.0],
                "yaw_speed": 0.0,
                "imu_rpy": [0.0, 0.0, 0.0],
            }

        tp = UnitreeDdsTransport(get_spec("unitree-go2"), state_factory=factory)
        seen = []
        tp.on_telemetry(seen.append)
        tp.connect()
        try:
            import time as _t

            _t.sleep(0.15)
            self.assertGreaterEqual(len(seen), 1)
        finally:
            tp.disconnect()


if __name__ == "__main__":
    unittest.main()
