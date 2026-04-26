"""
Tests for yahboom_ros2.protocol — TX builder and RX parser.

These cover the exact byte layout that goes over the serial wire to the
Yahboom board. Any regression here causes silent motor misbehaviour or
lost telemetry, making them critical safety checks.
"""

import math
import struct

import pytest

from yahboom_ros2.protocol import (
    HEAD_TX,
    DEVICE_ID,
    HEAD_RX,
    FUNC_BEEP,
    FUNC_MOTION,
    FUNC_MOTOR,
    FUNC_SET_CAR_TYPE,
    CAR_TYPE_MECANUM_X3,
    TYPE_VELOCITY,
    TYPE_ACCEL,
    TYPE_GYRO,
    TYPE_ATTITUDE,
    build_packet,
    packet_beep,
    packet_motion,
    packet_motor,
    packet_set_car_type,
    parse_rx_buffer,
    VelocityPacket,
    ImuAccelPacket,
    ImuGyroPacket,
    ImuAttitudePacket,
)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _build_rx_pkt(pkt_type: int, payload: bytes) -> bytes:
    """Construct a valid RX packet: [0xFF, 0xFB, LEN, TYPE, PAYLOAD..., CS]."""
    length = 3 + len(payload)
    header = bytes([HEAD_TX, HEAD_RX, length, pkt_type])
    body = bytes([length, pkt_type]) + payload
    cs = sum(body) & 0xFF
    return header + payload + bytes([cs])


# ---------------------------------------------------------------------------
# TX packet structure (build_packet)
# ---------------------------------------------------------------------------


class TestBuildPacket:
    def test_header_bytes(self):
        pkt = build_packet(FUNC_BEEP, b"\x00\x00")
        assert pkt[0] == HEAD_TX
        assert pkt[1] == DEVICE_ID

    def test_func_byte_at_index_3(self):
        pkt = build_packet(FUNC_MOTION, b"\x00" * 7)
        assert pkt[3] == FUNC_MOTION

    def test_len_field_value(self):
        payload = b"\x01\x02\x03"
        pkt = build_packet(FUNC_BEEP, payload)
        # LEN = 1(ID) + 1(LEN) + 1(FUNC) + len(payload)
        expected_len = 1 + 1 + 1 + len(payload)
        assert pkt[2] == expected_len

    def test_checksum_is_last_byte(self):
        pkt = build_packet(FUNC_SET_CAR_TYPE, b"\x01")
        cs = pkt[-1]
        # Recompute: (sum(all bytes except last) + 5) & 0xFF
        expected = (sum(pkt[:-1]) + 5) & 0xFF
        assert cs == expected

    def test_checksum_changes_with_payload(self):
        pkt1 = build_packet(FUNC_BEEP, b"\x64\x00")  # 100ms
        pkt2 = build_packet(FUNC_BEEP, b"\xc8\x00")  # 200ms
        assert pkt1[-1] != pkt2[-1]

    def test_output_is_bytes(self):
        assert isinstance(build_packet(FUNC_BEEP, b"\x00\x00"), bytes)


# ---------------------------------------------------------------------------
# packet_set_car_type
# ---------------------------------------------------------------------------


class TestPacketSetCarType:
    def test_func_code(self):
        pkt = packet_set_car_type()
        assert pkt[3] == FUNC_SET_CAR_TYPE

    def test_default_mecanum_x3(self):
        pkt = packet_set_car_type()
        # payload is a signed byte: CAR_TYPE_MECANUM_X3 = 1
        (car_type,) = struct.unpack_from("<b", pkt, 4)
        assert car_type == CAR_TYPE_MECANUM_X3

    def test_custom_car_type(self):
        pkt = packet_set_car_type(car_type=3)
        (car_type,) = struct.unpack_from("<b", pkt, 4)
        assert car_type == 3


# ---------------------------------------------------------------------------
# packet_beep
# ---------------------------------------------------------------------------


class TestPacketBeep:
    def test_func_code(self):
        assert packet_beep(100)[3] == FUNC_BEEP

    def test_duration_encoded(self):
        pkt = packet_beep(250)
        (duration,) = struct.unpack_from("<h", pkt, 4)
        assert duration == 250

    def test_zero_stops_beep(self):
        pkt = packet_beep(0)
        (duration,) = struct.unpack_from("<h", pkt, 4)
        assert duration == 0


# ---------------------------------------------------------------------------
# packet_motion
# ---------------------------------------------------------------------------


class TestPacketMotion:
    def test_func_code(self):
        assert packet_motion(0.0, 0.0, 0.0)[3] == FUNC_MOTION

    def test_zero_velocities(self):
        pkt = packet_motion(0.0, 0.0, 0.0)
        car, vx, vy, vz = struct.unpack_from("<bhhh", pkt, 4)
        assert vx == 0 and vy == 0 and vz == 0

    def test_vx_scaled_to_millimetres(self):
        pkt = packet_motion(0.5, 0.0, 0.0)
        _, vx, _, _ = struct.unpack_from("<bhhh", pkt, 4)
        assert vx == 500  # 0.5 m/s × 1000

    def test_vy_scaled(self):
        pkt = packet_motion(0.0, -0.2, 0.0)
        _, _, vy, _ = struct.unpack_from("<bhhh", pkt, 4)
        assert vy == -200

    def test_vz_scaled(self):
        pkt = packet_motion(0.0, 0.0, 1.0)
        _, _, _, vz = struct.unpack_from("<bhhh", pkt, 4)
        assert vz == 1000

    def test_car_type_default(self):
        pkt = packet_motion(0.0, 0.0, 0.0)
        (car,) = struct.unpack_from("<b", pkt, 4)
        assert car == CAR_TYPE_MECANUM_X3


# ---------------------------------------------------------------------------
# packet_motor
# ---------------------------------------------------------------------------


class TestPacketMotor:
    def test_func_code(self):
        assert packet_motor(0, 0, 0, 0)[3] == FUNC_MOTOR

    def test_values_encoded(self):
        pkt = packet_motor(100, -100, 200, -200)
        fl, fr, bl, br = struct.unpack_from("<hhhh", pkt, 4)
        assert fl == 100 and fr == -100 and bl == 200 and br == -200


# ---------------------------------------------------------------------------
# parse_rx_buffer — velocity feedback
# ---------------------------------------------------------------------------


class TestParseRxVelocity:
    def test_parses_velocity_packet(self):
        payload = struct.pack("<hhh", 500, 200, 100)  # 0.5, 0.2, 0.1
        buf = _build_rx_pkt(TYPE_VELOCITY, payload)
        results = parse_rx_buffer(buf)
        assert len(results) == 1
        pkt = results[0][1]
        assert isinstance(pkt, VelocityPacket)
        assert pkt.vx == pytest.approx(0.5)
        assert pkt.vy == pytest.approx(0.2)
        assert pkt.vz == pytest.approx(0.1)

    def test_zero_velocity(self):
        payload = struct.pack("<hhh", 0, 0, 0)
        buf = _build_rx_pkt(TYPE_VELOCITY, payload)
        pkt = parse_rx_buffer(buf)[0][1]
        assert pkt.vx == 0.0 and pkt.vy == 0.0 and pkt.vz == 0.0

    def test_negative_velocity(self):
        payload = struct.pack("<hhh", -500, -200, -100)
        buf = _build_rx_pkt(TYPE_VELOCITY, payload)
        pkt = parse_rx_buffer(buf)[0][1]
        assert pkt.vx == pytest.approx(-0.5)

    def test_multiple_packets_returns_all(self):
        p = struct.pack("<hhh", 100, 0, 0)
        buf = _build_rx_pkt(TYPE_VELOCITY, p) + _build_rx_pkt(TYPE_VELOCITY, p)
        results = parse_rx_buffer(buf)
        assert len(results) == 2

    def test_empty_buffer_returns_empty(self):
        assert parse_rx_buffer(b"") == []

    def test_garbage_data_returns_empty(self):
        assert parse_rx_buffer(b"\x00\x01\x02\x03\x04") == []

    def test_incomplete_packet_returns_empty(self):
        payload = struct.pack("<hhh", 100, 0, 0)
        buf = _build_rx_pkt(TYPE_VELOCITY, payload)[:-2]  # truncate
        assert parse_rx_buffer(buf) == []


# ---------------------------------------------------------------------------
# parse_rx_buffer — IMU packets
# ---------------------------------------------------------------------------


class TestParseRxImu:
    def test_accel_packet(self):
        payload = struct.pack("<hhh", 1000, 0, 0)  # ax = 1g
        buf = _build_rx_pkt(TYPE_ACCEL, payload)
        pkt = parse_rx_buffer(buf)[0][1]
        assert isinstance(pkt, ImuAccelPacket)
        assert pkt.ax == pytest.approx(9.81, rel=1e-3)
        assert pkt.ay == pytest.approx(0.0)

    def test_gyro_packet(self):
        payload = struct.pack("<hhh", 0, 0, 1000)  # gz = 1 rad/s
        buf = _build_rx_pkt(TYPE_GYRO, payload)
        pkt = parse_rx_buffer(buf)[0][1]
        assert isinstance(pkt, ImuGyroPacket)
        assert pkt.gz == pytest.approx(1.0)

    def test_attitude_packet_yaw_90deg(self):
        # 9000 × 0.01 deg = 90 deg = pi/2 rad
        payload = struct.pack("<hhh", 0, 0, 9000)
        buf = _build_rx_pkt(TYPE_ATTITUDE, payload)
        pkt = parse_rx_buffer(buf)[0][1]
        assert isinstance(pkt, ImuAttitudePacket)
        assert pkt.yaw == pytest.approx(math.pi / 2, rel=1e-3)

    def test_mixed_packet_types(self):
        vel = _build_rx_pkt(TYPE_VELOCITY, struct.pack("<hhh", 100, 0, 0))
        gyro = _build_rx_pkt(TYPE_GYRO, struct.pack("<hhh", 0, 0, 500))
        results = parse_rx_buffer(vel + gyro)
        assert len(results) == 2
        assert isinstance(results[0][1], VelocityPacket)
        assert isinstance(results[1][1], ImuGyroPacket)
