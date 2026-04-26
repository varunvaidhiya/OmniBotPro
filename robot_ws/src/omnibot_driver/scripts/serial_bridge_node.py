#!/usr/bin/env python3

import math
import threading
import time

import rclpy
from rclpy.node import Node
import serial

from geometry_msgs.msg import Twist, TransformStamped
from nav_msgs.msg import Odometry
from std_msgs.msg import String
import tf2_ros


class SerialBridgeNode(Node):
    """
    ROS2 Node for bridging communication between ROS and the STM32 microcontroller.

    This node handles the serial communication protocol between the Raspberry Pi
    and the STM32 microcontroller for the OmniBot mecanum wheel robot.
    """

    def __init__(self):
        super().__init__("serial_bridge_node")

        # Declare parameters
        self.declare_parameter("serial_port", "/dev/ttyACM0")
        self.declare_parameter("baud_rate", 115200)
        self.declare_parameter("read_timeout", 0.1)  # seconds
        # Mecanum wheel physical parameters (must match yahboom_params.yaml)
        self.declare_parameter("wheel_radius", 0.04)  # metres
        self.declare_parameter("wheel_separation_width", 0.215)  # y-axis (left↔right)
        self.declare_parameter("wheel_separation_length", 0.165)  # x-axis (front↔rear)
        self.declare_parameter("odom_frame", "odom")
        self.declare_parameter("base_frame", "base_link")

        # Get parameters
        self.port_name = self.get_parameter("serial_port").value
        self.baud_rate = self.get_parameter("baud_rate").value
        self.read_timeout = self.get_parameter("read_timeout").value
        self.wheel_radius = self.get_parameter("wheel_radius").value
        self.lx = self.get_parameter("wheel_separation_length").value / 2.0
        self.ly = self.get_parameter("wheel_separation_width").value / 2.0
        self.odom_frame = self.get_parameter("odom_frame").value
        self.base_frame = self.get_parameter("base_frame").value

        # Create publishers and subscribers
        self.serial_pub = self.create_publisher(String, "serial_data", 10)
        self.odom_pub = self.create_publisher(Odometry, "odom", 10)
        self.cmd_vel_sub = self.create_subscription(
            Twist, "cmd_vel", self.cmd_vel_callback, 10
        )

        # TF broadcaster for odom → base_link
        self.tf_broadcaster = tf2_ros.TransformBroadcaster(self)

        # Odometry state
        self.odom_x = 0.0
        self.odom_y = 0.0
        self.odom_theta = 0.0
        self.last_encoder_time = self.get_clock().now()
        self.odom_lock = threading.Lock()

        # Initialize serial port
        self.serial_port = None
        self.connect_serial()

        # Create read thread
        self.read_thread = threading.Thread(target=self.read_serial)
        self.read_thread.daemon = True
        self.read_thread.start()

        # Create heartbeat timer
        self.heartbeat_timer = self.create_timer(1.0, self.send_heartbeat)

        self.get_logger().info("Serial bridge node initialized")

    def connect_serial(self):
        """Connect to the serial port."""
        try:
            self.serial_port = serial.Serial(
                port=self.port_name, baudrate=self.baud_rate, timeout=self.read_timeout
            )
            self.get_logger().info(
                f"Successfully connected to serial port {self.port_name}"
            )
            return True
        except serial.SerialException as e:
            self.serial_port = None
            self.get_logger().error(
                f"Failed to open serial port {self.port_name}: {str(e)}"
            )
            return False

    def cmd_vel_callback(self, msg):
        """Callback for velocity commands."""
        if self.serial_port is None or not self.serial_port.is_open:
            self.get_logger().warning(
                "Serial port not open, cannot send velocity command"
            )
            return

        # Format the command for the STM32
        # Format: "<CMD_VEL,x,y,z>\n"
        command = (
            f"<CMD_VEL,{msg.linear.x:.4f},{msg.linear.y:.4f},{msg.angular.z:.4f}>\n"
        )

        try:
            self.serial_port.write(command.encode("utf-8"))
            self.get_logger().debug(f"Sent command: {command.strip()}")
        except serial.SerialException as e:
            self.get_logger().error(f"Failed to send command: {str(e)}")
            self.connect_serial()

    def read_serial(self):
        """Thread function to continuously read from the serial port."""
        while rclpy.ok():
            if self.serial_port is None or not self.serial_port.is_open:
                time.sleep(1.0)
                self.connect_serial()
                continue

            try:
                if self.serial_port.in_waiting > 0:
                    line = self.serial_port.readline().decode("utf-8").strip()
                    if line:
                        # Publish the raw received data
                        msg = String()
                        msg.data = line
                        self.serial_pub.publish(msg)
                        self.get_logger().debug(f"Received: {line}")

                        self.process_serial_data(line)
                else:
                    time.sleep(0.01)
            except serial.SerialException as e:
                self.get_logger().error(f"Serial read error: {str(e)}")
                self.serial_port = None
                time.sleep(1.0)
                self.connect_serial()

    def process_serial_data(self, data):
        """
        Process data received from the serial port.

        Args:
            data (str): Data received from the serial port.
                        Expected format: "<TYPE,value1,value2,...>"
        """
        if data.startswith("<") and data.endswith(">"):
            data = data[1:-1]
            parts = data.split(",")

            if len(parts) > 0:
                data_type = parts[0]

                if data_type == "ENCODERS" and len(parts) >= 5:
                    try:
                        # Wheel angular velocities [rad/s]: front-left, front-right,
                        # rear-left, rear-right (positive = forward).
                        fl = float(parts[1])
                        fr = float(parts[2])
                        rl = float(parts[3])
                        rr = float(parts[4])
                        self.get_logger().debug(
                            f"Encoder values: FL={fl:.3f}, FR={fr:.3f}, "
                            f"RL={rl:.3f}, RR={rr:.3f}"
                        )
                        self._update_odometry(fl, fr, rl, rr)
                    except ValueError:
                        self.get_logger().warning(f"Invalid encoder data: {data}")

                elif data_type == "STATUS":
                    self.get_logger().info(f"STM32 status: {','.join(parts[1:])}")

    # ------------------------------------------------------------------
    # Odometry helpers
    # ------------------------------------------------------------------

    def _update_odometry(self, fl: float, fr: float, rl: float, rr: float):
        """
        Compute mecanum-wheel forward kinematics and integrate odometry.

        Mecanum equations (standard sign convention, all wheels spinning
        forward gives positive vx):
            vx      =  r/4  * ( fl + fr + rl + rr)
            vy      =  r/4  * (-fl + fr + rl - rr)
            omega_z =  r/(4*(lx+ly)) * (-fl + fr - rl + rr)

        where r  = wheel radius,
              lx = half wheel-base length (front↔rear / 2),
              ly = half track width      (left↔right / 2).

        Args:
            fl, fr, rl, rr: Wheel angular velocities [rad/s].
        """
        r = self.wheel_radius
        lx = self.lx
        ly = self.ly

        vx = r / 4.0 * (fl + fr + rl + rr)
        vy = r / 4.0 * (-fl + fr + rl - rr)
        omega = r / (4.0 * (lx + ly)) * (-fl + fr - rl + rr)

        now = self.get_clock().now()
        with self.odom_lock:
            dt = (now - self.last_encoder_time).nanoseconds * 1e-9
            self.last_encoder_time = now

            if dt <= 0.0 or dt > 1.0:
                # Skip first sample or spuriously large gaps
                return

            # Integrate using robot-frame velocities (Euler method)
            cos_t = math.cos(self.odom_theta)
            sin_t = math.sin(self.odom_theta)
            self.odom_x += (vx * cos_t - vy * sin_t) * dt
            self.odom_y += (vx * sin_t + vy * cos_t) * dt
            self.odom_theta += omega * dt

            x = self.odom_x
            y = self.odom_y
            theta = self.odom_theta

        self._publish_odometry(now, x, y, theta, vx, vy, omega)

    def _publish_odometry(self, stamp, x, y, theta, vx, vy, omega):
        """Publish nav_msgs/Odometry and broadcast the odom→base_link TF."""
        # --- Quaternion from yaw (roll=pitch=0) ---
        qz = math.sin(theta / 2.0)
        qw = math.cos(theta / 2.0)

        # --- Odometry message ---
        odom_msg = Odometry()
        odom_msg.header.stamp = stamp.to_msg()
        odom_msg.header.frame_id = self.odom_frame
        odom_msg.child_frame_id = self.base_frame

        odom_msg.pose.pose.position.x = x
        odom_msg.pose.pose.position.y = y
        odom_msg.pose.pose.position.z = 0.0
        odom_msg.pose.pose.orientation.x = 0.0
        odom_msg.pose.pose.orientation.y = 0.0
        odom_msg.pose.pose.orientation.z = qz
        odom_msg.pose.pose.orientation.w = qw

        odom_msg.twist.twist.linear.x = vx
        odom_msg.twist.twist.linear.y = vy
        odom_msg.twist.twist.angular.z = omega

        self.odom_pub.publish(odom_msg)

        # --- TF broadcast ---
        tf_msg = TransformStamped()
        tf_msg.header.stamp = stamp.to_msg()
        tf_msg.header.frame_id = self.odom_frame
        tf_msg.child_frame_id = self.base_frame

        tf_msg.transform.translation.x = x
        tf_msg.transform.translation.y = y
        tf_msg.transform.translation.z = 0.0
        tf_msg.transform.rotation.x = 0.0
        tf_msg.transform.rotation.y = 0.0
        tf_msg.transform.rotation.z = qz
        tf_msg.transform.rotation.w = qw

        self.tf_broadcaster.sendTransform(tf_msg)

    def send_heartbeat(self):
        """Send a heartbeat message to the STM32."""
        if self.serial_port is None or not self.serial_port.is_open:
            return
        try:
            self.serial_port.write(b"<HEARTBEAT>\n")
        except serial.SerialException:
            self.connect_serial()


def main(args=None):
    rclpy.init(args=args)
    node = SerialBridgeNode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
