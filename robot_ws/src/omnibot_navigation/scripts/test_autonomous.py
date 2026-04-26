#!/usr/bin/env python3

import rclpy
from rclpy.node import Node
from geometry_msgs.msg import PoseStamped, Twist
from nav2_msgs.action import NavigateToPose
from rclpy.action import ActionClient
import math


class AutonomousTester(Node):
    """
    Test script for autonomous navigation capabilities.

    This node provides simple test commands for autonomous operation.
    """

    def __init__(self):
        super().__init__("autonomous_tester")

        # Action client for navigation
        self.navigate_to_pose_client = ActionClient(
            self, NavigateToPose, "navigate_to_pose"
        )

        # Publisher for manual control (fallback)
        self.cmd_vel_pub = self.create_publisher(Twist, "cmd_vel", 10)

        # Timer for test sequence
        self.test_timer = self.create_timer(5.0, self.run_test_sequence)
        self.test_step = 0

        self.get_logger().info("Autonomous Tester initialized")

    def run_test_sequence(self):
        """Run a sequence of autonomous navigation tests."""
        if self.test_step == 0:
            self.get_logger().info("Starting autonomous navigation test")
            self.test_simple_movement()
        elif self.test_step == 1:
            self.test_rotation()
        elif self.test_step == 2:
            self.test_diagonal_movement()
        elif self.test_step == 3:
            self.test_return_home()
        elif self.test_step == 4:
            self.get_logger().info("Autonomous test sequence completed")
            self.test_timer.cancel()
            return

        self.test_step += 1

    def test_simple_movement(self):
        """Test simple forward movement."""
        self.get_logger().info("Test 1: Simple forward movement")
        goal = self.create_pose_stamped(1.0, 0.0, 0.0)
        self.send_navigation_goal(goal)

    def test_rotation(self):
        """Test rotation in place."""
        self.get_logger().info("Test 2: Rotation in place")
        goal = self.create_pose_stamped(1.0, 0.0, math.pi / 2)
        self.send_navigation_goal(goal)

    def test_diagonal_movement(self):
        """Test diagonal movement."""
        self.get_logger().info("Test 3: Diagonal movement")
        goal = self.create_pose_stamped(1.5, 1.0, math.pi / 4)
        self.send_navigation_goal(goal)

    def test_return_home(self):
        """Test returning to home position."""
        self.get_logger().info("Test 4: Return to home")
        goal = self.create_pose_stamped(0.0, 0.0, 0.0)
        self.send_navigation_goal(goal)

    def create_pose_stamped(self, x, y, yaw):
        """Create a PoseStamped message."""
        pose = PoseStamped()
        pose.header.frame_id = "map"
        pose.header.stamp = self.get_clock().now().to_msg()

        pose.pose.position.x = x
        pose.pose.position.y = y
        pose.pose.position.z = 0.0

        # Convert yaw to quaternion
        qx = 0.0
        qy = 0.0
        qz = math.sin(yaw / 2.0)
        qw = math.cos(yaw / 2.0)

        pose.pose.orientation.x = qx
        pose.pose.orientation.y = qy
        pose.pose.orientation.z = qz
        pose.pose.orientation.w = qw

        return pose

    def send_navigation_goal(self, pose):
        """Send navigation goal to Nav2."""
        goal_msg = NavigateToPose.Goal()
        goal_msg.pose = pose

        self.get_logger().info(
            f"Sending navigation goal: x={pose.pose.position.x:.2f}, y={pose.pose.position.y:.2f}"
        )

        # Wait for action server
        if not self.navigate_to_pose_client.wait_for_server(timeout_sec=5.0):
            self.get_logger().error("NavigateToPose action server not available")
            return

        # Send goal
        self.send_goal_future = self.navigate_to_pose_client.send_goal_async(goal_msg)
        self.send_goal_future.add_done_callback(self.goal_response_callback)

    def goal_response_callback(self, future):
        """Handle goal response."""
        goal_handle = future.result()
        if not goal_handle.accepted:
            self.get_logger().info("Goal rejected by server")
            return

        self.get_logger().info("Goal accepted by server")
        self.result_future = goal_handle.get_result_async()
        self.result_future.add_done_callback(self.get_result_callback)

    def get_result_callback(self, future):
        """Handle navigation result."""
        future.result().result
        self.get_logger().info("Navigation test completed")

    def emergency_stop(self):
        """Emergency stop - send zero velocity command."""
        twist = Twist()
        self.cmd_vel_pub.publish(twist)
        self.get_logger().warn("Emergency stop activated")


def main(args=None):
    rclpy.init(args=args)

    autonomous_tester = AutonomousTester()

    try:
        rclpy.spin(autonomous_tester)
    except KeyboardInterrupt:
        autonomous_tester.get_logger().info("Shutting down autonomous tester")
        autonomous_tester.emergency_stop()
    finally:
        autonomous_tester.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
