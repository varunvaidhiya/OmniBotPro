import unittest

from ohho.adapters.composite import CompositeTransport
from ohho.adapters.feetech import HOME_TICKS, TICKS_PER_RAD, FeetechTransport
from ohho.adapters.sim import SimTransport
from ohho.adapters.yahboom import YahboomTransport
from ohho.adapters import _yahboom_proto as proto
from ohho.registry import get_spec
from ohho.schema import Velocity

from test_yahboom_proto import make_rx_velocity


class FakeSerial:
    def __init__(self) -> None:
        self.written = bytearray()
        self._rx = bytearray()

    def write(self, data: bytes) -> int:
        self.written.extend(data)
        return len(data)

    def read(self, n: int = 1) -> bytes:
        chunk = bytes(self._rx[:n])
        del self._rx[:n]
        return chunk

    def feed(self, data: bytes) -> None:
        self._rx.extend(data)

    def close(self) -> None:
        pass


class FakeFeetechBus:
    def __init__(self, names: tuple[str, ...]) -> None:
        self.names = names
        self.present = {n: HOME_TICKS for n in names}
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


def _make_composite():
    spec = get_spec("omnibot")
    fs = FakeSerial()
    base = YahboomTransport(spec, "/dev/fake", serial_factory=lambda: fs)
    bus = FakeFeetechBus(spec.joint_names)
    arm = FeetechTransport(spec, "/dev/fakeACM", bus_factory=lambda: bus)
    return CompositeTransport(base, arm), fs, bus


class TestCompositeTransport(unittest.TestCase):
    def test_connect_connects_both(self):
        ct, fs, bus = _make_composite()
        ct.connect()
        try:
            self.assertTrue(bus.connected)
            self.assertIn(bytes(proto.packet_set_car_type()), bytes(fs.written))
        finally:
            ct.disconnect()

    def test_send_velocity_goes_to_base(self):
        ct, fs, bus = _make_composite()
        ct.connect()
        try:
            fs.written.clear()
            ct.send_velocity(Velocity(0.1, 0.0, 0.2))
            self.assertEqual(bytes(fs.written), proto.packet_motion(0.1, 0.0, 0.2))
            self.assertEqual(bus.goals, {})
        finally:
            ct.disconnect()

    def test_send_joint_command_goes_to_arm(self):
        ct, fs, bus = _make_composite()
        ct.connect()
        try:
            fs.written.clear()
            ct.send_joint_command("arm_shoulder_pan", 0.5)
            expected = int(round(0.5 * TICKS_PER_RAD + HOME_TICKS))
            self.assertEqual(bus.goals["arm_shoulder_pan"], expected)
            self.assertEqual(len(fs.written), 0)
        finally:
            ct.disconnect()

    def test_read_merges_base_and_arm(self):
        ct, fs, bus = _make_composite()
        ct.connect()
        try:
            ct._base._ingest(make_rx_velocity(0.3, 0.0, 0.5))
            bus.present["arm_gripper"] = HOME_TICKS + 200
            ct._arm._poll_once()
            t = ct.read()
            self.assertAlmostEqual(t.odom.vx, 0.3, places=3)
            self.assertAlmostEqual(t.odom.omega, 0.5, places=3)
            pos = {j.name: j.position for j in t.joints}
            self.assertAlmostEqual(pos["arm_gripper"], 200 / TICKS_PER_RAD, places=3)
        finally:
            ct.disconnect()

    def test_emergency_stop_hits_both(self):
        ct, fs, bus = _make_composite()
        ct.connect()
        try:
            ct.emergency_stop()
            self.assertEqual(set(bus.torque.values()), {0})
            fs.written.clear()
            ct.send_velocity(Velocity(0.1, 0.0, 0.0))
            self.assertEqual(len(fs.written), 0)
        finally:
            ct.disconnect()

    def test_disconnect_disconnects_both(self):
        ct, fs, bus = _make_composite()
        ct.connect()
        ct.disconnect()
        self.assertFalse(bus.connected)


class TestCompositeWithSim(unittest.TestCase):
    """Composite works with sim transports too (useful for integration tests)."""

    def test_sim_composite_merges(self):
        spec = get_spec("omnibot")
        base = SimTransport(spec)
        arm = SimTransport(spec)
        ct = CompositeTransport(base, arm)
        ct.connect()
        try:
            ct.send_velocity(Velocity(0.1, 0.0, 0.0))
            ct.send_joint_command("arm_shoulder_pan", 0.5)
            base.step(0.1)
            arm.step(0.1)
            t = ct.read()
            self.assertAlmostEqual(t.odom.vx, 0.1, places=3)
        finally:
            ct.disconnect()


if __name__ == "__main__":
    unittest.main()
