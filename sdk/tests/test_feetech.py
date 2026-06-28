import unittest

from ohho.adapters.errors import AdapterUnavailable
from ohho.adapters.feetech import (
    DEFAULT_JOINT_MAX,
    DEFAULT_JOINT_MIN,
    HOME_TICKS,
    TICKS_PER_RAD,
    FeetechTransport,
    clamp_joints,
    radians_to_ticks,
    ticks_to_radians,
)
from ohho.registry import get_spec
from ohho.schema import Velocity


class TestFeetechConversions(unittest.TestCase):
    def test_zero_radians_is_home_ticks(self):
        self.assertEqual(radians_to_ticks([0.0] * 6), [HOME_TICKS] * 6)

    def test_roundtrip(self):
        rads = [0.5, -0.3, 1.0, -1.57, 0.0, 0.2]
        ticks = radians_to_ticks(rads)
        back = ticks_to_radians(ticks)
        for r, b in zip(rads, back):
            self.assertAlmostEqual(r, b, places=2)

    def test_clamp_joints(self):
        clamped = clamp_joints([10.0, -10.0, 0.5])
        self.assertEqual(clamped[0], DEFAULT_JOINT_MAX[0])
        self.assertEqual(clamped[1], DEFAULT_JOINT_MIN[1])
        self.assertAlmostEqual(clamped[2], 0.5)


class FakeFeetechBus:
    """In-memory FeetechMotorsBus stand-in."""

    def __init__(self) -> None:
        self.present: dict[str, int] = {n: HOME_TICKS for n in _OMNIBOT_JOINTS}
        self.goals: dict[str, int] = {}
        self.torque: dict[str, int] = {}
        self.connected = False

    def connect(self) -> None:
        self.connected = True

    def disconnect(self) -> None:
        self.connected = False

    def read(self, key: str) -> dict:
        if key == "Present_Position":
            return dict(self.present)
        return {}

    def write(self, key: str, values: dict) -> None:
        if key == "Goal_Position":
            self.goals.update(values)
            self.present.update(values)
        elif key == "Torque_Enable":
            self.torque.update(values)


_OMNIBOT_JOINTS = (
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
)


def _transport(bus: FakeFeetechBus) -> FeetechTransport:
    return FeetechTransport(
        get_spec("omnibot"),
        "/dev/fakeACM",
        bus_factory=lambda: bus,
    )


class TestFeetechAdapter(unittest.TestCase):
    def test_connect_opens_bus(self):
        bus = FakeFeetechBus()
        tp = _transport(bus)
        tp.connect()
        try:
            self.assertTrue(bus.connected)
            self.assertEqual(tp.port, "/dev/fakeACM")
        finally:
            tp.disconnect()

    def test_send_joint_command_writes_goal_position(self):
        bus = FakeFeetechBus()
        tp = _transport(bus)
        tp.connect()
        try:
            tp.send_joint_command("arm_shoulder_pan", 0.5)
            expected_tick = int(round(0.5 * TICKS_PER_RAD + HOME_TICKS))
            self.assertEqual(bus.goals["arm_shoulder_pan"], expected_tick)
        finally:
            tp.disconnect()

    def test_send_velocity_is_noop(self):
        bus = FakeFeetechBus()
        tp = _transport(bus)
        tp.connect()
        try:
            tp.send_velocity(Velocity(1.0, 0.0, 0.0))
            self.assertEqual(bus.goals, {})
        finally:
            tp.disconnect()

    def test_emergency_stop_disables_torque(self):
        bus = FakeFeetechBus()
        tp = _transport(bus)
        tp.connect()
        try:
            tp.emergency_stop()
            self.assertEqual(set(bus.torque.values()), {0})
            tp.send_joint_command("arm_shoulder_pan", 0.5)
            self.assertNotIn("arm_shoulder_pan", bus.goals)
        finally:
            tp.disconnect()

    def test_release_stop_re_enables(self):
        bus = FakeFeetechBus()
        tp = _transport(bus)
        tp.connect()
        try:
            tp.emergency_stop()
            tp.release_stop()
            tp.send_joint_command("arm_gripper", 0.3)
            self.assertIn("arm_gripper", bus.goals)
        finally:
            tp.disconnect()

    def test_poll_reads_present_positions(self):
        bus = FakeFeetechBus()
        bus.present["arm_shoulder_pan"] = HOME_TICKS + 100
        tp = _transport(bus)
        tp.connect()
        try:
            tp._poll_once()
            t = tp.read()
            pos = {j.name: j.position for j in t.joints}
            self.assertAlmostEqual(
                pos["arm_shoulder_pan"], 100 / TICKS_PER_RAD, places=3
            )
        finally:
            tp.disconnect()

    def test_connect_without_lerobot_raises_clean_error(self):
        tp = FeetechTransport(get_spec("omnibot"), "/dev/null")
        with self.assertRaises(AdapterUnavailable):
            tp.connect()


if __name__ == "__main__":
    unittest.main()
