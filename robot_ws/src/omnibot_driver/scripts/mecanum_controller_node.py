#!/usr/bin/env python3

import rclpy
from rclpy.node import Node
from geometry_msgs.msg import Twist, TransformStamped
from nav_msgs.msg import Odometry
from tf2_ros import TransformBroadcaster
import serial
from tf_transformations import quaternion_from_euler


class MecanumControllerNode(Node):
    """
    ROS2 Node for controlling a mecanum wheel robot.

    This node subscribes to velocity commands and converts them to individual
    wheel velocities based on the mecanum wheel kinematics equations.
    """

    def __init__(self):
        super().__init__("mecanum_controller_node")

        # Declare parameters
        self.declare_parameter("wheel_radius", 0.04)  # 4 cm radius
        self.declare_parameter(
            "wheel_separation_width", 0.215
        )  # 21.5 cm between left and right wheels
        self.declare_parameter(
            "wheel_separation_length", 0.165
        )  # 16.5 cm between front and back wheels
        self.declare_parameter("serial_port", "/dev/ttyACM0")
        self.declare_parameter("baud_rate", 115200)

        # Get parameters
        self.wheel_radius = self.get_parameter("wheel_radius").value
        self.wheel_separation_width = self.get_parameter("wheel_separation_width").value
        self.wheel_separation_length = self.get_parameter(
            "wheel_separation_length"
        ).value
        self.port_name = self.get_parameter("serial_port").value
        self.baud_rate = self.get_parameter("baud_rate").value

        # Robot state
        self.x_pos = 0.0
        self.y_pos = 0.0
        self.theta = 0.0

        # Create publishers and subscribers
        self.cmd_vel_sub = self.create_subscription(
            Twist, "cmd_vel", self.cmd_vel_callback, 10
        )

        self.odom_pub = self.create_publisher(Odometry, "odom", 10)

        # Create transform broadcaster
        self.tf_broadcaster = TransformBroadcaster(self)

        # Create timer for periodic updates
        self.update_timer = self.create_timer(0.01, self.update_callback)  # 100 Hz

        # Setup serial communication
        try:
            self.serial_port = serial.Serial(
                port=self.port_name, baudrate=self.baud_rate, timeout=1.0
            )
            self.get_logger().info(
                f"Successfully connected to serial port {self.port_name}"
            )
        except serial.SerialException as e:
            self.serial_port = None
            self.get_logger().error(
                f"Failed to open serial port {self.port_name}: {str(e)}"
            )

        self.get_logger().info("Mecanum controller node initialized")

    def cmd_vel_callback(self, msg):
        """
        Callback for velocity commands.

        Args:
            msg (Twist): Velocity command message
        """
        # Calculate wheel velocities based on mecanum kinematics
        front_left, front_right, rear_left, rear_right = (
            self.calculate_wheel_velocities(msg)
        )

        # Send velocities to the controller
        self.send_velocities_to_controller(
            front_left, front_right, rear_left, rear_right
        )

    def calculate_wheel_velocities(self, twist):
        """
        Calculate wheel velocities based on mecanum kinematics.

        Args:
            twist (Twist): Velocity command

        Returns:
            tuple: (front_left, front_right, rear_left, rear_right) wheel velocities
        """
        # Extract velocity components
        vx = twist.linear.x
        vy = twist.linear.y
        omega = twist.angular.z

        # Calculate wheel velocities using mecanum kinematics equations
        # For a mecanum wheel robot, the wheel velocities are calculated as follows:
        # FL = Vx - Vy - (L+W)*omega
        # FR = Vx + Vy + (L+W)*omega
        # RL = Vx + Vy - (L+W)*omega
        # RR = Vx - Vy + (L+W)*omega
        # Where L is half the wheel separation length and W is half the wheel separation width

        L = self.wheel_separation_length / 2.0
        W = self.wheel_separation_width / 2.0
        k = L + W  # Distance from center to wheel (for rotation component)

        front_left = vx - vy - k * omega
        front_right = vx + vy + k * omega
        rear_left = vx + vy - k * omega
        rear_right = vx - vy + k * omega

        # Convert linear velocities to angular velocities (rad/s)
        front_left /= self.wheel_radius
        front_right /= self.wheel_radius
        rear_left /= self.wheel_radius
        rear_right /= self.wheel_radius

        return front_left, front_right, rear_left, rear_right

    def send_velocities_to_controller(
        self, front_left, front_right, rear_left, rear_right
    ):
        """
        Send wheel velocities to the microcontroller.

        Args:
            front_left (float): Front left wheel velocity
            front_right (float): Front right wheel velocity
            rear_left (float): Rear left wheel velocity
            rear_right (float): Rear right wheel velocity
        """
        if self.serial_port is None or not self.serial_port.is_open:
            self.get_logger().warning("Serial port not open, cannot send velocities")
            return

        # Format: "<FL,FR,RL,RR>\n"
        # Convert rad/s to a suitable range for the microcontroller (e.g., PWM values)
        # For simplicity, we'll just send the raw rad/s values and let the STM32 handle conversion

        command = f"<{front_left},{front_right},{rear_left},{rear_right}>\n"

        try:
            self.serial_port.write(command.encode("utf-8"))
        except serial.SerialException as e:
            self.get_logger().error(f"Failed to send velocities: {str(e)}")

    def update_callback(self):
        """
        Timer callback for periodic updates.
        """
        # In a real implementation, we would read odometry data from the STM32 controller
        # For now, we'll just publish a dummy odometry message

        current_time = self.get_clock().now()
        self.publish_odometry(current_time)

    def publish_odometry(self, time):
        """
        Publish odometry information.

        Args:
            time (Time): Current time
        """
        # Create quaternion from yaw
        q = quaternion_from_euler(0, 0, self.theta)

        # Create and publish transform
        transform_stamped = TransformStamped()
        transform_stamped.header.stamp = time.to_msg()
        transform_stamped.header.frame_id = "odom"
        transform_stamped.child_frame_id = "base_link"
        transform_stamped.transform.translation.x = self.x_pos
        transform_stamped.transform.translation.y = self.y_pos
        transform_stamped.transform.translation.z = 0.0
        transform_stamped.transform.rotation.x = q[0]
        transform_stamped.transform.rotation.y = q[1]
        transform_stamped.transform.rotation.z = q[2]
        transform_stamped.transform.rotation.w = q[3]

        self.tf_broadcaster.sendTransform(transform_stamped)

        # Create and publish odometry message
        odom_msg = Odometry()
        odom_msg.header.stamp = time.to_msg()
        odom_msg.header.frame_id = "odom"
        odom_msg.child_frame_id = "base_link"

        # Set position
        odom_msg.pose.pose.position.x = self.x_pos
        odom_msg.pose.pose.position.y = self.y_pos
        odom_msg.pose.pose.position.z = 0.0
        odom_msg.pose.pose.orientation.x = q[0]
        odom_msg.pose.pose.orientation.y = q[1]
        odom_msg.pose.pose.orientation.z = q[2]
        odom_msg.pose.pose.orientation.w = q[3]

        # Publish odometry message
        self.odom_pub.publish(odom_msg)


def main(args=None):
    rclpy.init(args=args)
    node = MecanumControllerNode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
