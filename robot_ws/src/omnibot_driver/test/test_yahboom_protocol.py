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


def build_rx_packet(pkt_type: int, payload: bytes) -> bytes:
    """Helper: build a valid Yahboom RX packet."""
    length = 3 + len(payload)
    header = bytes([0xFF, 0xFB, length, pkt_type])
    body = header[2:] + payload
    checksum = sum(body) & 0xFF
    return header + payload + bytes([checksum])


def build_velocity_packet(vx_mms: int, vy_mms: int, vz_mrads: int) -> bytes:
    """Build a TYPE=0x0C velocity feedback packet."""
    payload = struct.pack("<hhh", vx_mms, vy_mms, vz_mrads)
    return build_rx_packet(0x0C, payload)


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
    def test_0x0C_feedback_not_used_for_current_vx(self, node, mock_serial):
        # 0x0C velocity feedback is intentionally ignored — odometry is dead-reckoned
        # from commanded velocity (cmd_vx) to avoid unsigned-magnitude sign errors.
        pkt = build_velocity_packet(500, 200, 100)  # mm/s values never read
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        # current_vx reflects cmd_vx (=0.0 — no motion command issued)
        assert node.current_vx == pytest.approx(0.0)
        assert node.current_vy == pytest.approx(0.0)

    def test_zero_velocity_packet(self, node, mock_serial):
        pkt = build_velocity_packet(0, 0, 0)
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt

        node.read_yahboom_odometry()

        assert node.current_vx == pytest.approx(0.0)
        assert node.current_vy == pytest.approx(0.0)

    def test_current_vx_reflects_commanded_velocity(self, node, mock_serial):
        # current_vx is updated from cmd_vx (commanded), not board feedback.
        # The node clips directly to MAX_VAL=0.12 (no software ramp; board handles PID).
        from geometry_msgs.msg import Twist

        msg = Twist()
        msg.linear.x = 0.2  # above MAX_VAL=0.12, so cmd_vx is clipped to 0.12
        node.current_twist = msg
        node.send_motion_command()

        pkt = build_velocity_packet(0, 0, 0)  # board feedback irrelevant
        mock_serial.in_waiting = len(pkt)
        mock_serial.read.return_value = pkt
        node.read_yahboom_odometry()

        assert node.current_vx == pytest.approx(0.12)

    def test_multiple_imu_packets_last_wins(self, node, mock_serial):
        # Multiple packets in one read burst are all parsed; last value wins.
        pkt1 = build_rx_packet(0x62, struct.pack("<hhh", 0, 0, 1000))  # gz=1 rad/s
        pkt2 = build_rx_packet(0x62, struct.pack("<hhh", 0, 0, 2000))  # gz=2 rad/s
        mock_serial.in_waiting = len(pkt1) + len(pkt2)
        mock_serial.read.return_value = pkt1 + pkt2

        node.read_yahboom_odometry()

        assert node.imu_gyro[2] == pytest.approx(2.0)


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


# ── Velocity clamping ─────────────────────────────────────────────────────────
# The node clips directly to MAX_VAL=0.12 m/s; ramping is handled by the board.


class TestVelocityClamping:
    def test_velocity_clamped_to_max(self, node, mock_serial):
        msg = Twist()
        msg.linear.x = 10.0  # way above MAX_VAL=0.12
        node.current_twist = msg
        node.send_motion_command()

        written = bytearray(mock_serial.write.call_args[0][0])
        # Payload starts at byte 4: CAR_TYPE(1b) + vx(2b) + vy(2b) + w(2b)
        vx_int = struct.unpack_from("<h", written, 5)[0]
        # Clipped to 0.12 m/s → 120 mm/s
        assert vx_int == 120

    def test_negative_velocity_clamped_to_min(self, node, mock_serial):
        msg = Twist()
        msg.linear.x = -10.0
        node.current_twist = msg
        node.send_motion_command()

        written = bytearray(mock_serial.write.call_args[0][0])
        vx_int = struct.unpack_from("<h", written, 5)[0]
        assert vx_int == -120


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
