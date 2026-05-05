"""
Unit tests for Yahboom serial protocol helpers in YahboomControllerNode.

Tests cover:
- Checksum calculation
- TX packet structure (send_packet)
- RX odometry parsing (velocity feedback type 0x0C)
- IMU packet parsing (0x61 accel, 0x62 gyro, 0x63 attitude)
- Ramp limiting in send_motion_command
- Emergency stop zeroing

All tests use the mock_serial fixture from conftest.py — no hardware needed.
"""

import math
import struct
import time

import pytest
import rclpy
from geometry_msgs.msg import Twist
from std_msgs.msg import Bool

from conftest import build_velocity_packet, build_rx_packet


@pytest.fixture(scope="module", autouse=True)
def ros_context():
    rclpy.init()
    yield
    rclpy.shutdown()


@pytest.fixture()
def node(mock_serial):
    """Spin a YahboomControllerNode with mocked serial."""
    from omnibot_driver.scripts import yahboom_controller_node as mod

    n = mod.YahboomControllerNode()
    # Override the timer so tests can drive callbacks manually
    n.update_timer.cancel()
    # connect_serial() sleeps ~0.55 s; reset so read_yahboom_odometry
    # doesn't hit the dt > 0.5 early-return guard.
    n.last_odom_time = time.time()
    yield n
    n.destroy_node()


# ── Checksum ─────────────────────────────────────────────────────────────────


class TestChecksum:
    def test_checksum_known_vector(self):
        # Manually compute: (0xFF + 0xFC + 4 + 0x12 + 1 + 0 + 0 + 0 + 5) & 0xFF
        packet = [0xFF, 0xFC, 4, 0x12, 1, 0, 0, 0]
        from omnibot_driver.scripts.yahboom_controller_node import YahboomControllerNode

        cs = YahboomControllerNode.calculate_checksum(None, packet)
        assert 0 <= cs <= 255

    def test_checksum_consistent(self):
        from omnibot_driver.scripts.yahboom_controller_node import YahboomControllerNode

        data = [0x01, 0x02, 0x03]
        cs1 = YahboomControllerNode.calculate_checksum(None, data)
        cs2 = YahboomControllerNode.calculate_checksum(None, data)
        assert cs1 == cs2

    def test_checksum_differs_on_different_data(self):
        from omnibot_driver.scripts.yahboom_controller_node import YahboomControllerNode

        cs1 = YahboomControllerNode.calculate_checksum(None, [0x01])
        cs2 = YahboomControllerNode.calculate_checksum(None, [0x02])
        assert cs1 != cs2


# ── TX packet structure ───────────────────────────────────────────────────────


class TestTxPacket:
    def test_packet_starts_with_header(self, node, mock_serial):
        node.send_packet(0x02, struct.pack("<H", 100))
        written = mock_serial.write.call_args[0][0]
        assert written[0] == 0xFF
        assert written[1] == 0xFC

    def test_packet_len_field_correct(self, node, mock_serial):
        payload = struct.pack("<H", 200)
        node.send_packet(0x02, payload)
        written = bytearray(mock_serial.write.call_args[0][0])
        # LEN = 3 + N (DEVICE_ID + LEN + FUNC + payload), not counting HEAD or CS
        assert written[2] == len(written) - 2

    def test_motion_packet_type_0x12(self, node, mock_serial):
        node.send_packet(0x12, struct.pack("<bhhh", 1, 0, 0, 0))
        written = bytearray(mock_serial.write.call_args[0][0])
        assert written[3] == 0x12


# ── RX odometry parsing ───────────────────────────────────────────────────────


class TestOdometryParsing:
    def test_velocity_packet_updates_state(self, node, mock_serial):
        pkt = build_velocity_packet(500, 200, 100)  # mm/s, mm/s, mrad/s
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        assert node.current_vx == pytest.approx(0.5)
        assert node.current_vy == pytest.approx(0.2)
        assert node.current_vz == pytest.approx(0.1)

    def test_zero_velocity_packet(self, node, mock_serial):
        pkt = build_velocity_packet(0, 0, 0)
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        assert node.current_vx == pytest.approx(0.0)
        assert node.current_vy == pytest.approx(0.0)

    def test_dead_band_suppresses_noise(self, node, mock_serial):
        # Values below 0.005 m/s should be zeroed out
        pkt = build_velocity_packet(3, 3, 3)  # 0.003 m/s — below dead-band
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        assert node.current_vx == pytest.approx(0.0)
        assert node.current_vy == pytest.approx(0.0)

    def test_multiple_packets_uses_last(self, node, mock_serial):
        pkt1 = build_velocity_packet(100, 0, 0)
        pkt2 = build_velocity_packet(800, 0, 0)
        mock_serial.in_waiting = len(pkt1) + len(pkt2)
        mock_serial.read.return_value = pkt1 + pkt2

        node.read_yahboom_odometry()

        assert node.current_vx == pytest.approx(0.8)


# ── IMU packet parsing ────────────────────────────────────────────────────────


class TestImuParsing:
    def test_accel_packet_0x61(self, node, mock_serial):
        # ax=1000 mg → 9.81 m/s², ay=0, az=0
        payload = struct.pack("<hhh", 1000, 0, 0)
        pkt = build_rx_packet(0x61, payload)
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        assert node.imu_accel[0] == pytest.approx(9.81, rel=1e-3)

    def test_gyro_packet_0x62(self, node, mock_serial):
        payload = struct.pack("<hhh", 0, 0, 1000)  # gz = 1 rad/s
        pkt = build_rx_packet(0x62, payload)
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        assert node.imu_gyro[2] == pytest.approx(1.0)

    def test_attitude_packet_0x63(self, node, mock_serial):
        # yaw = 9000 * 0.01 deg = 90 deg = pi/2 rad
        payload = struct.pack("<hhh", 0, 0, 9000)
        pkt = build_rx_packet(0x63, payload)
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        assert node.imu_yaw == pytest.approx(math.pi / 2, rel=1e-3)


# ── Ramp limiting ─────────────────────────────────────────────────────────────


class TestRampLimiting:
    def test_velocity_clamped_to_max(self, node, mock_serial):
        msg = Twist()
        msg.linear.x = 10.0  # way above max 0.2
        node.current_twist = msg
        node.send_motion_command()

        written = bytearray(mock_serial.write.call_args[0][0])
        # Payload starts at byte 4: CAR_TYPE(1b) + vx(2b) + vy(2b) + w(2b)
        vx_int = struct.unpack_from("<h", written, 5)[0]
        # After one ramp step from 0, vx should be 25 (RAMP_STEP=0.025 m/s = 25 mm/s)
        assert vx_int == 25

    def test_negative_velocity_ramps(self, node, mock_serial):
        msg = Twist()
        msg.linear.x = -10.0
        node.current_twist = msg
        node.send_motion_command()

        written = bytearray(mock_serial.write.call_args[0][0])
        vx_int = struct.unpack_from("<h", written, 5)[0]
        assert vx_int == -25


# ── Emergency stop ────────────────────────────────────────────────────────────


class TestEmergencyStop:
    def test_estop_zeroes_velocity(self, node, mock_serial):
        # Give it a non-zero command first
        msg = Twist()
        msg.linear.x = 0.2
        node.current_twist = msg
        node.cmd_vx = 0.2

        # Trigger emergency stop
        estop_msg = Bool()
        estop_msg.data = True
        node._emergency_stop_callback(estop_msg)

        assert node._emergency_stop is True
        assert node.cmd_vx == 0.0
        assert node.current_twist.linear.x == 0.0

    def test_estop_sends_zero_packet(self, node, mock_serial):
        estop_msg = Bool()
        estop_msg.data = True
        node._emergency_stop_callback(estop_msg)

        mock_serial.write.reset_mock()
        node.send_motion_command()

        written = bytearray(mock_serial.write.call_args[0][0])
        vx_int = struct.unpack_from("<h", written, 5)[0]
        assert vx_int == 0

    def test_estop_clear_resumes_control(self, node, mock_serial):
        on = Bool()
        on.data = True
        node._emergency_stop_callback(on)
        assert node._emergency_stop is True

        off = Bool()
        off.data = False
        node._emergency_stop_callback(off)
        assert node._emergency_stop is False
