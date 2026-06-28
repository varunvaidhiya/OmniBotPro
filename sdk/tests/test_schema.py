import unittest

from ohho.schema import ConnectionState, Odometry, Telemetry, Velocity, clamp


class TestSchema(unittest.TestCase):
    def test_clamp(self):
        self.assertEqual(clamp(5, 0, 1), 1)
        self.assertEqual(clamp(-5, 0, 1), 0)
        self.assertEqual(clamp(0.5, 0, 1), 0.5)

    def test_velocity_defaults(self):
        v = Velocity()
        self.assertEqual((v.linear_x, v.linear_y, v.angular_z), (0.0, 0.0, 0.0))

    def test_odometry_defaults(self):
        o = Odometry()
        self.assertEqual(o.x, 0.0)
        self.assertEqual(o.theta, 0.0)

    def test_telemetry_defaults(self):
        t = Telemetry()
        self.assertIsNone(t.odom)
        self.assertEqual(t.joints, [])
        self.assertIsInstance(t.timestamp, float)

    def test_connection_state(self):
        self.assertEqual(ConnectionState.CONNECTED.value, "connected")


if __name__ == "__main__":
    unittest.main()
