import unittest

from ohho.adapters import _yahboom_proto as proto
from ohho.adapters.yahboom import YahboomTransport
from ohho.registry import get_spec
from ohho.schema import Velocity

from test_yahboom_proto import make_rx_velocity


class FakeSerial:
    """In-memory serial stand-in: records writes, replays fed RX bytes."""

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


def _tp(fs: FakeSerial) -> YahboomTransport:
    return YahboomTransport(get_spec("omnibot"), "/dev/fake", serial_factory=lambda: fs)


class TestYahboomAdapter(unittest.TestCase):
    def test_connect_sets_car_type(self):
        fs = FakeSerial()
        tp = _tp(fs)
        tp.connect()
        try:
            self.assertIn(bytes(proto.packet_set_car_type()), bytes(fs.written))
        finally:
            tp.disconnect()

    def test_send_velocity_writes_motion_packet(self):
        fs = FakeSerial()
        tp = _tp(fs)
        tp.connect()
        try:
            fs.written.clear()
            tp.send_velocity(Velocity(0.1, 0.0, 0.2))
            self.assertEqual(bytes(fs.written), proto.packet_motion(0.1, 0.0, 0.2))
        finally:
            tp.disconnect()

    def test_emergency_stop_blocks_motion(self):
        fs = FakeSerial()
        tp = _tp(fs)
        tp.connect()
        try:
            fs.written.clear()
            tp.emergency_stop()
            self.assertEqual(bytes(fs.written), proto.packet_motion(0.0, 0.0, 0.0))
            fs.written.clear()
            tp.send_velocity(Velocity(0.1, 0.0, 0.0))  # ignored while stopped
            self.assertEqual(len(fs.written), 0)
            tp.release_stop()
            tp.send_velocity(Velocity(0.1, 0.0, 0.0))
            self.assertGreater(len(fs.written), 0)
        finally:
            tp.disconnect()

    def test_ingest_decodes_telemetry(self):
        # No connect() -> no reader thread; feed bytes deterministically.
        fs = FakeSerial()
        tp = _tp(fs)
        tp._ingest(make_rx_velocity(0.3, 0.0, 0.5))
        t = tp.read()
        self.assertAlmostEqual(t.odom.vx, 0.3, places=3)
        self.assertAlmostEqual(t.odom.omega, 0.5, places=3)

    def test_telemetry_callback_fires_on_ingest(self):
        fs = FakeSerial()
        tp = _tp(fs)
        seen = []
        tp.on_telemetry(seen.append)
        tp._ingest(make_rx_velocity(0.1, 0.0, 0.0))
        self.assertEqual(len(seen), 1)


if __name__ == "__main__":
    unittest.main()
