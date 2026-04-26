#!/usr/bin/env python3
"""
yahboom_ros2 — ROS 2 driver node for the Yahboom ROS Robot Expansion Board.

Subscribes:
  /cmd_vel  (geometry_msgs/Twist)  — velocity commands
  /joy      (sensor_msgs/Joy)      — optional joystick (button A = beep)

Publishes:
  /odom     (nav_msgs/Odometry)    — wheel-encoder odometry + TF odom→base_link
  /imu/data (sensor_msgs/Imu)     — on-board MPU9250 IMU

ROS Parameters:
  serial_port              (str,   default /dev/ttyUSB0)
  baud_rate                (int,   default 115200)
  wheel_radius             (float, default 0.04)   metres
  wheel_separation_width   (float, default 0.215)  metres (left–right)
  wheel_separation_length  (float, default 0.165)  metres (front–back)
  max_linear_vel           (float, default 0.5)    m/s clamp
  max_angular_vel          (float, default 2.0)    rad/s clamp
  ramp_step                (float, default 0.05)   m/s per tick
  update_hz                (float, default 20.0)   control-loop frequency
  debug_serial             (bool,  default false)  log unknown RX packets
"""

import math
import time
import traceback
from typing import Optional

import rclpy
from rclpy.node import Node
from geometry_msgs.msg import Twist, TransformStamped
from nav_msgs.msg import Odometry
from sensor_msgs.msg import Joy, Imu
from tf2_ros import TransformBroadcaster

try:
    import serial
except ImportError:
    serial = None  # type: ignore

try:
    import numpy as np
    from tf_transformations import quaternion_from_euler
except ImportError:
    np = None  # type: ignore
    quaternion_from_euler = None  # type: ignore

from .protocol import (
    packet_set_car_type,
    packet_beep,
    packet_motion,
    parse_rx_buffer,
    VelocityPacket,
    ImuAccelPacket,
    ImuGyroPacket,
    ImuAttitudePacket,
    CAR_TYPE_MECANUM_X3,
)


class YahboomDriverNode(Node):
    """Full-featured ROS 2 driver for the Yahboom expansion board."""

    def __init__(self) -> None:
        super().__init__("yahboom_driver")

        # ── Parameters ──────────────────────────────────────────────────────
        self.declare_parameter("serial_port", "/dev/ttyUSB0")
        self.declare_parameter("baud_rate", 115200)
        self.declare_parameter("wheel_radius", 0.04)
        self.declare_parameter("wheel_separation_width", 0.215)
        self.declare_parameter("wheel_separation_length", 0.165)
        self.declare_parameter("max_linear_vel", 0.5)
        self.declare_parameter("max_angular_vel", 2.0)
        self.declare_parameter("ramp_step", 0.05)
        self.declare_parameter("update_hz", 20.0)
        self.declare_parameter("debug_serial", False)

        self._port_name = self.get_parameter("serial_port").value
        self._baud_rate = self.get_parameter("baud_rate").value
        self._wheel_radius = self.get_parameter("wheel_radius").value
        self._sep_width = self.get_parameter("wheel_separation_width").value
        self._sep_length = self.get_parameter("wheel_separation_length").value
        self._max_lin = self.get_parameter("max_linear_vel").value
        self._max_ang = self.get_parameter("max_angular_vel").value
        self._ramp_step = self.get_parameter("ramp_step").value
        self._update_hz = self.get_parameter("update_hz").value
        self._debug_serial = self.get_parameter("debug_serial").value

        # ── Robot state ──────────────────────────────────────────────────────
        self._x = self._y = self._theta = 0.0
        self._current_vx = self._current_vy = self._current_vz = 0.0
        self._cmd_vx = self._cmd_vy = self._cmd_wa = 0.0
        self._last_odom_time = time.time()
        self._last_beep_time = 0.0
        self._current_twist = Twist()

        # IMU defaults (z-accel = g at rest)
        self._imu_accel = [0.0, 0.0, 9.81]
        self._imu_gyro = [0.0, 0.0, 0.0]
        self._imu_roll = self._imu_pitch = self._imu_yaw = 0.0

        # ── ROS interfaces ───────────────────────────────────────────────────
        self._cmd_sub = self.create_subscription(Twist, "cmd_vel", self._cmd_vel_cb, 10)
        self._joy_sub = self.create_subscription(Joy, "joy", self._joy_cb, 10)
        self._odom_pub = self.create_publisher(Odometry, "odom", 10)
        self._imu_pub = self.create_publisher(Imu, "imu/data", 10)
        self._tf_broadcaster = TransformBroadcaster(self)

        # ── Serial ───────────────────────────────────────────────────────────
        self._serial: Optional[object] = None
        self._connect_serial()

        # ── Control loop ─────────────────────────────────────────────────────
        self.create_timer(1.0 / self._update_hz, self._update_cb)
        self.get_logger().info(
            f"yahboom_driver ready on {self._port_name} @ {self._baud_rate} baud"
        )

    # ── Serial helpers ───────────────────────────────────────────────────────

    def _connect_serial(self) -> None:
        if serial is None:
            self.get_logger().error("pyserial not installed.")
            return
        try:
            if self._serial and self._serial.is_open:
                self._serial.close()
            self._serial = serial.Serial(self._port_name, self._baud_rate, timeout=1.0)
            time.sleep(0.3)
            # Board requires CAR_TYPE to be set each session
            for _ in range(5):
                self._serial.write(packet_set_car_type(CAR_TYPE_MECANUM_X3))
                time.sleep(0.05)
            self.get_logger().info("Serial connected; CAR_TYPE set to Mecanum X3.")
        except Exception as exc:
            self.get_logger().error(f"Serial connect failed: {exc}")
            self._serial = None

    def _send(self, data: bytes) -> None:
        if self._serial is None:
            return
        try:
            self._serial.write(data)
            time.sleep(0.002)  # board needs a small inter-packet gap
        except Exception as exc:
            self.get_logger().error(f"Serial TX error: {exc}")
            self._serial.close()
            self._serial = None

    # ── ROS callbacks ────────────────────────────────────────────────────────

    def _cmd_vel_cb(self, msg: Twist) -> None:
        self._current_twist = msg

    def _joy_cb(self, msg: Joy) -> None:
        now = time.time()
        if msg.buttons and msg.buttons[0] == 1:
            if now - self._last_beep_time > 0.5:
                self._send(packet_beep(100))
                self._last_beep_time = now

    # ── Control loop ────────────────────────────────────────────────────────

    def _update_cb(self) -> None:
        try:
            if self._serial is None:
                self._connect_serial()
                return
            self._read_rx()
            now = self.get_clock().now()
            self._publish_odometry(now)
            self._publish_imu(now)
            self._send_motion()
        except Exception as exc:
            self.get_logger().error(f"Update error: {exc}")

    def _send_motion(self) -> None:
        import numpy as _np  # local import so we fail gracefully if missing

        msg = self._current_twist
        target_vx = float(_np.clip(msg.linear.x, -self._max_lin, self._max_lin))
        target_vy = float(_np.clip(msg.linear.y, -self._max_lin, self._max_lin))
        target_wa = float(_np.clip(msg.angular.z, -self._max_ang, self._max_ang))

        # Velocity ramping
        step = self._ramp_step
        self._cmd_vx = _ramp(self._cmd_vx, target_vx, step)
        self._cmd_vy = _ramp(self._cmd_vy, target_vy, step)
        self._cmd_wa = target_wa

        self._send(packet_motion(self._cmd_vx, self._cmd_vy, self._cmd_wa))

    def _read_rx(self) -> None:
        if self._serial is None:
            return
        try:
            waiting = self._serial.in_waiting
            if waiting == 0:
                return
            data = self._serial.read(waiting)
        except Exception as exc:
            self.get_logger().error(f"Serial RX error: {exc}")
            return

        for _offset, pkt in parse_rx_buffer(data):
            if isinstance(pkt, VelocityPacket):
                vx = 0.0 if abs(pkt.vx) < 0.005 else pkt.vx
                vy = 0.0 if abs(pkt.vy) < 0.005 else pkt.vy
                vz = 0.0 if abs(pkt.vz) < 0.005 else pkt.vz
                now = time.time()
                dt = now - self._last_odom_time
                self._last_odom_time = now
                if 0 < dt < 0.5:
                    cos_th = math.cos(self._theta)
                    sin_th = math.sin(self._theta)
                    self._x += (vx * cos_th - vy * sin_th) * dt
                    self._y += (vx * sin_th + vy * cos_th) * dt
                    self._theta += vz * dt
                    self._theta = math.atan2(
                        math.sin(self._theta), math.cos(self._theta)
                    )
                self._current_vx = vx
                self._current_vy = vy
                self._current_vz = vz
            elif isinstance(pkt, ImuAccelPacket):
                self._imu_accel = [pkt.ax, pkt.ay, pkt.az]
            elif isinstance(pkt, ImuGyroPacket):
                self._imu_gyro = [pkt.gx, pkt.gy, pkt.gz]
            elif isinstance(pkt, ImuAttitudePacket):
                self._imu_roll = pkt.roll
                self._imu_pitch = pkt.pitch
                self._imu_yaw = pkt.yaw
            elif self._debug_serial:
                self.get_logger().info(f"[debug_serial] unknown packet: {pkt}")

    def _publish_odometry(self, stamp: rclpy.time.Time) -> None:
        q = quaternion_from_euler(0, 0, self._theta)
        ts = TransformStamped()
        ts.header.stamp = stamp.to_msg()
        ts.header.frame_id = "odom"
        ts.child_frame_id = "base_link"
        ts.transform.translation.x = self._x
        ts.transform.translation.y = self._y
        ts.transform.translation.z = 0.0
        ts.transform.rotation.x = q[0]
        ts.transform.rotation.y = q[1]
        ts.transform.rotation.z = q[2]
        ts.transform.rotation.w = q[3]
        self._tf_broadcaster.sendTransform(ts)

        odom = Odometry()
        odom.header.stamp = stamp.to_msg()
        odom.header.frame_id = "odom"
        odom.child_frame_id = "base_link"
        odom.pose.pose.position.x = self._x
        odom.pose.pose.position.y = self._y
        odom.pose.pose.orientation.x = q[0]
        odom.pose.pose.orientation.y = q[1]
        odom.pose.pose.orientation.z = q[2]
        odom.pose.pose.orientation.w = q[3]
        odom.twist.twist.linear.x = self._current_vx
        odom.twist.twist.linear.y = self._current_vy
        odom.twist.twist.angular.z = self._current_vz
        self._odom_pub.publish(odom)

    def _publish_imu(self, stamp: rclpy.time.Time) -> None:
        q = quaternion_from_euler(self._imu_roll, self._imu_pitch, self._imu_yaw)
        msg = Imu()
        msg.header.stamp = stamp.to_msg()
        msg.header.frame_id = "imu_link"
        msg.orientation.x = q[0]
        msg.orientation.y = q[1]
        msg.orientation.z = q[2]
        msg.orientation.w = q[3]
        msg.orientation_covariance[0] = msg.orientation_covariance[
            4
        ] = msg.orientation_covariance[8] = 0.01
        msg.angular_velocity.x = self._imu_gyro[0]
        msg.angular_velocity.y = self._imu_gyro[1]
        msg.angular_velocity.z = self._imu_gyro[2]
        msg.angular_velocity_covariance[0] = msg.angular_velocity_covariance[
            4
        ] = msg.angular_velocity_covariance[8] = 0.001
        msg.linear_acceleration.x = self._imu_accel[0]
        msg.linear_acceleration.y = self._imu_accel[1]
        msg.linear_acceleration.z = self._imu_accel[2]
        msg.linear_acceleration_covariance[0] = msg.linear_acceleration_covariance[
            4
        ] = msg.linear_acceleration_covariance[8] = 0.1
        self._imu_pub.publish(msg)


# ── Helpers ──────────────────────────────────────────────────────────────────


def _ramp(current: float, target: float, step: float) -> float:
    if current < target:
        return min(current + step, target)
    elif current > target:
        return max(current - step, target)
    return current


# ── Entry point ──────────────────────────────────────────────────────────────


def main(args=None) -> None:
    rclpy.init(args=args)
    try:
        node = YahboomDriverNode()
        rclpy.spin(node)
    except Exception as exc:
        print(f"FATAL: {exc}")
        traceback.print_exc()
    finally:
        if "node" in dir():
            node.destroy_node()  # type: ignore[possibly-undefined]
        rclpy.shutdown()


if __name__ == "__main__":
    main()
