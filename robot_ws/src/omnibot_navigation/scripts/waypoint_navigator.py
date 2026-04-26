#!/usr/bin/env python3

import rclpy
from rclpy.node import Node
from geometry_msgs.msg import PoseStamped
from nav2_msgs.action import NavigateToPose
from nav2_msgs.action import NavigateThroughPoses
from rclpy.action import ActionClient
import yaml
import os
from ament_index_python.packages import get_package_share_directory


class WaypointNavigator(Node):
    """
    Autonomous waypoint navigation for OmniBot.

    This node provides autonomous navigation capabilities by following
    predefined waypoints or accepting waypoint commands.
    """

    def __init__(self):
        super().__init__("waypoint_navigator")

        # Declare parameters
        self.declare_parameter("waypoint_file", "waypoints.yaml")
        self.declare_parameter("loop_waypoints", False)
        self.declare_parameter("waypoint_timeout", 30.0)

        # Get parameters
        self.waypoint_file = self.get_parameter("waypoint_file").value
        self.loop_waypoints = self.get_parameter("loop_waypoints").value
        self.waypoint_timeout = self.get_parameter("waypoint_timeout").value

        # Action clients
        self.navigate_to_pose_client = ActionClient(
            self, NavigateToPose, "navigate_to_pose"
        )
        self.navigate_through_poses_client = ActionClient(
            self, NavigateThroughPoses, "navigate_through_poses"
        )

        # State variables
        self.waypoints = []
        self.current_waypoint_index = 0
        self.is_navigating = False

        # Load waypoints
        self.load_waypoints()

        # Create timer for autonomous navigation
        self.navigation_timer = self.create_timer(1.0, self.navigation_loop)

        self.get_logger().info("Waypoint Navigator initialized")
        self.get_logger().info(f"Loaded {len(self.waypoints)} waypoints")

    def load_waypoints(self):
        """Load waypoints from YAML file."""
        try:
            package_dir = get_package_share_directory("omnibot_navigation")
            waypoint_path = os.path.join(package_dir, "config", self.waypoint_file)

            if os.path.exists(waypoint_path):
                with open(waypoint_path, "r") as file:
                    data = yaml.safe_load(file)
                    self.waypoints = data.get("waypoints", [])
                    self.get_logger().info(f"Loaded waypoints from {waypoint_path}")
            else:
                # Create default waypoints if file doesn't exist
                self.create_default_waypoints()
                self.save_waypoints()

        except Exception as e:
            self.get_logger().error(f"Error loading waypoints: {str(e)}")
            self.create_default_waypoints()

    def create_default_waypoints(self):
        """Create default waypoints for testing."""
        self.waypoints = [
            {
                "name": "start",
                "position": {"x": 0.0, "y": 0.0, "z": 0.0},
                "orientation": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0},
            },
            {
                "name": "waypoint_1",
                "position": {"x": 1.0, "y": 0.0, "z": 0.0},
                "orientation": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0},
            },
            {
                "name": "waypoint_2",
                "position": {"x": 1.0, "y": 1.0, "z": 0.0},
                "orientation": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0},
            },
            {
                "name": "waypoint_3",
                "position": {"x": 0.0, "y": 1.0, "z": 0.0},
                "orientation": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0},
            },
        ]
        self.get_logger().info("Created default waypoints")

    def save_waypoints(self):
        """Save waypoints to YAML file."""
        try:
            package_dir = get_package_share_directory("omnibot_navigation")
            waypoint_path = os.path.join(package_dir, "config", self.waypoint_file)

            data = {"waypoints": self.waypoints}
            with open(waypoint_path, "w") as file:
                yaml.dump(data, file, default_flow_style=False)

        except Exception as e:
            self.get_logger().error(f"Error saving waypoints: {str(e)}")

    def navigation_loop(self):
        """Main navigation loop for autonomous operation."""
        if not self.is_navigating and len(self.waypoints) > 0:
            self.start_autonomous_navigation()

    def start_autonomous_navigation(self):
        """Start autonomous navigation through waypoints."""
        if len(self.waypoints) == 0:
            self.get_logger().warn("No waypoints available for navigation")
            return

        self.get_logger().info("Starting autonomous navigation")
        self.is_navigating = True

        # Create goal poses
        goal_poses = []
        for waypoint in self.waypoints:
            pose = PoseStamped()
            pose.header.frame_id = "map"
            pose.header.stamp = self.get_clock().now().to_msg()

            pose.pose.position.x = waypoint["position"]["x"]
            pose.pose.position.y = waypoint["position"]["y"]
            pose.pose.position.z = waypoint["position"]["z"]

            pose.pose.orientation.x = waypoint["orientation"]["x"]
            pose.pose.orientation.y = waypoint["orientation"]["y"]
            pose.pose.orientation.z = waypoint["orientation"]["z"]
            pose.pose.orientation.w = waypoint["orientation"]["w"]

            goal_poses.append(pose)

        # Send navigation goal
        self.send_navigation_goal(goal_poses)

    def send_navigation_goal(self, poses):
        """Send navigation goal to Nav2."""
        goal_msg = NavigateThroughPoses.Goal()
        goal_msg.poses = poses

        self.get_logger().info(f"Sending navigation goal with {len(poses)} poses")

        # Wait for action server
        if not self.navigate_through_poses_client.wait_for_server(timeout_sec=5.0):
            self.get_logger().error("NavigateThroughPoses action server not available")
            self.is_navigating = False
            return

        # Send goal
        self.send_goal_future = self.navigate_through_poses_client.send_goal_async(
            goal_msg, feedback_callback=self.feedback_callback
        )
        self.send_goal_future.add_done_callback(self.goal_response_callback)

    def goal_response_callback(self, future):
        """Handle goal response."""
        goal_handle = future.result()
        if not goal_handle.accepted:
            self.get_logger().info("Goal rejected by server")
            self.is_navigating = False
            return

        self.get_logger().info("Goal accepted by server")
        self.result_future = goal_handle.get_result_async()
        self.result_future.add_done_callback(self.get_result_callback)

    def feedback_callback(self, feedback_msg):
        """Handle navigation feedback."""
        feedback = feedback_msg.feedback
        self.get_logger().info(
            f"Navigation feedback: {feedback.current_pose.pose.position.x:.2f}, {feedback.current_pose.pose.position.y:.2f}"
        )

    def get_result_callback(self, future):
        """Handle navigation result."""
        future.result().result
        self.get_logger().info("Navigation completed")
        self.is_navigating = False

        if self.loop_waypoints:
            self.get_logger().info("Looping waypoints - restarting navigation")
            self.create_timer(2.0, self.start_autonomous_navigation)

    def add_waypoint(self, name, x, y, z=0.0, qx=0.0, qy=0.0, qz=0.0, qw=1.0):
        """Add a new waypoint."""
        waypoint = {
            "name": name,
            "position": {"x": x, "y": y, "z": z},
            "orientation": {"x": qx, "y": qy, "z": qz, "w": qw},
        }
        self.waypoints.append(waypoint)
        self.save_waypoints()
        self.get_logger().info(f"Added waypoint: {name}")

    def clear_waypoints(self):
        """Clear all waypoints."""
        self.waypoints = []
        self.current_waypoint_index = 0
        self.save_waypoints()
        self.get_logger().info("Cleared all waypoints")


def main(args=None):
    rclpy.init(args=args)

    waypoint_navigator = WaypointNavigator()

    try:
        rclpy.spin(waypoint_navigator)
    except KeyboardInterrupt:
        waypoint_navigator.get_logger().info("Shutting down waypoint navigator")
    finally:
        waypoint_navigator.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
