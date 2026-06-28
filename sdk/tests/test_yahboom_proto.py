import struct
import unittest

from ohho.adapters import _yahboom_proto as proto


def make_rx_velocity(vx: float, vy: float, vz: float) -> bytes:
    """Build a valid RX velocity frame the way the board would."""
    payload = struct.pack("<hhh", int(vx * 1000), int(vy * 1000), int(vz * 1000))
    length = 3 + len(payload)  # LEN + TYPE + payload + CS
    body = bytes([length, proto.TYPE_VELOCITY]) + payload
    cs = sum(body) & 0xFF
    return bytes([proto.HEAD_TX, proto.HEAD_RX]) + body + bytes([cs])


class TestYahboomProto(unittest.TestCase):
    def test_motion_packet_structure(self):
        pkt = proto.packet_motion(0.1, 0.0, 0.2)
        self.assertEqual(pkt[0], 0xFF)
        self.assertEqual(pkt[1], 0xFC)
        self.assertEqual(pkt[3], proto.FUNC_MOTION)
        # checksum is the documented (sum + 5) & 0xFF over all preceding bytes
        self.assertEqual(pkt[-1], (sum(pkt[:-1]) + 5) & 0xFF)

    def test_motion_payload_scales_by_1000(self):
        pkt = proto.packet_motion(0.123, -0.05, 1.0)
        car, vx, vy, vz = struct.unpack_from("<bhhh", pkt, 4)
        self.assertEqual(car, proto.CAR_TYPE_MECANUM_X3)
        self.assertEqual((vx, vy, vz), (123, -50, 1000))

    def test_set_car_type(self):
        pkt = proto.packet_set_car_type()
        self.assertEqual(pkt[3], proto.FUNC_SET_CAR_TYPE)

    def test_parse_stream_decodes_velocity(self):
        frame = make_rx_velocity(0.3, -0.1, 0.5)
        packets, consumed = proto.parse_stream(frame)
        self.assertEqual(consumed, len(frame))
        self.assertEqual(len(packets), 1)
        v = packets[0]
        self.assertIsInstance(v, proto.VelocityPacket)
        self.assertAlmostEqual(v.vx, 0.3, places=3)
        self.assertAlmostEqual(v.vy, -0.1, places=3)
        self.assertAlmostEqual(v.vz, 0.5, places=3)

    def test_parse_stream_keeps_partial_remainder(self):
        frame = make_rx_velocity(0.2, 0.0, 0.0)
        buf = frame + frame[:4]  # one whole frame + a partial second
        packets, consumed = proto.parse_stream(buf)
        self.assertEqual(len(packets), 1)
        self.assertEqual(consumed, len(frame))  # partial tail left unconsumed
        self.assertEqual(buf[consumed:], frame[:4])

    def test_parse_stream_skips_leading_garbage(self):
        frame = make_rx_velocity(0.1, 0.0, 0.0)
        packets, consumed = proto.parse_stream(b"\x00\x01\x02" + frame)
        self.assertEqual(len(packets), 1)
        self.assertEqual(consumed, 3 + len(frame))


if __name__ == "__main__":
    unittest.main()
