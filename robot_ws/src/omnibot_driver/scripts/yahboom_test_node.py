#!/usr/bin/env python3

import rclpy
from rclpy.node import Node
import serial
import time


class YahboomTestNode(Node):
    """
    Test node for Yahboom ROS Robot Expansion Board.

    This node provides simple test commands to verify the board is working correctly.
    """

    def __init__(self):
        super().__init__("yahboom_test_node")

        # Declare parameters
        self.declare_parameter("serial_port", "/dev/ttyUSB0")
        self.declare_parameter("baud_rate", 115200)

        # Get parameters
        self.port_name = self.get_parameter("serial_port").value
        self.baud_rate = self.get_parameter("baud_rate").value

        # Setup serial communication
        try:
            self.serial_port = serial.Serial(
                port=self.port_name, baudrate=self.baud_rate, timeout=1.0
            )
            self.get_logger().info(
                f"Successfully connected to Yahboom board on {self.port_name}"
            )
        except serial.SerialException as e:
            self.serial_port = None
            self.get_logger().error(
                f"Failed to open serial port {self.port_name}: {str(e)}"
            )

        # Create timer for test commands
        self.test_timer = self.create_timer(5.0, self.run_test_sequence)
        self.test_step = 0

        self.get_logger().info("Yahboom test node initialized")

    def run_test_sequence(self):
        """
        Run a sequence of test commands to verify board functionality.
        """
        if self.serial_port is None or not self.serial_port.is_open:
            self.get_logger().warning("Serial port not open, skipping test")
            return

        test_commands = [
            ("INIT", "Initialize board"),
            ("MODE_MECANUM", "Set mecanum mode"),
            ("MOTOR,50,50,50,50", "Move forward"),
            ("MOTOR,-50,-50,-50,-50", "Move backward"),
            ("MOTOR,0,0,0,0", "Stop"),
            ("MOTOR,50,-50,50,-50", "Rotate left"),
            ("MOTOR,-50,50,-50,50", "Rotate right"),
            ("MOTOR,0,0,0,0", "Stop"),
            ("STATUS", "Get board status"),
        ]

        if self.test_step < len(test_commands):
            command, description = test_commands[self.test_step]
            self.get_logger().info(f"Test step {self.test_step + 1}: {description}")

            try:
                self.serial_port.write(f"{command}\n".encode("utf-8"))
                self.get_logger().info(f"Sent command: {command}")

                # Wait for response
                time.sleep(1.0)

                if self.serial_port.in_waiting > 0:
                    response = self.serial_port.readline().decode("utf-8").strip()
                    self.get_logger().info(f"Received: {response}")

            except serial.SerialException as e:
                self.get_logger().error(f"Failed to send command: {str(e)}")

            self.test_step += 1
        else:
            # Reset test sequence
            self.test_step = 0
            self.get_logger().info("Test sequence completed, restarting...")

    def send_custom_command(self, command):
        """
        Send a custom command to the Yahboom board.

        Args:
            command (str): Command to send
        """
        if self.serial_port is None or not self.serial_port.is_open:
            self.get_logger().warning("Serial port not open, cannot send command")
            return False

        try:
            self.serial_port.write(f"{command}\n".encode("utf-8"))
            self.get_logger().info(f"Sent custom command: {command}")
            return True
        except serial.SerialException as e:
            self.get_logger().error(f"Failed to send command: {str(e)}")
            return False


def main(args=None):
    rclpy.init(args=args)
    node = YahboomTestNode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
