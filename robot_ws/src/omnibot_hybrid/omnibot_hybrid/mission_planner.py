#!/usr/bin/env python3
"""
Mission Planner — Hybrid Nav2 + VLA orchestrator for OmniBot.

Parses natural mission strings and executes them in sequential phases:
  Phase 1 (nav2):  Navigate to a named location via Nav2 NavigateToPose.
  Phase 2 (vla):   Execute a semantic VLA task at the destination.

Mission command format  (/mission/command  std_msgs/String)
────────────────────────────────────────────────────────────
  Full hybrid:        "navigate:kitchen,vla:find the red cup"
  Navigation only:    "navigate:bedroom"
  VLA only:           "vla:follow the person"

Named locations are read from config/named_locations.yaml at startup.

Published topics
────────────────
  /control_mode     std_msgs/String  — "nav2" | "vla" | "teleop"
  /vla/prompt       std_msgs/String  — task description forwarded to VLA node
  /mission/status   std_msgs/String  — human-readable status string

Subscribed topics
─────────────────
  /mission/command  std_msgs/String  — new mission instruction
  /mission/cancel   std_msgs/String  — any message aborts current mission
"""

import math
import os

import yaml
from ament_index_python.packages import get_package_share_directory

import rclpy
from rclpy.action import ActionClient
from rclpy.callback_groups import ReentrantCallbackGroup
from rclpy.node import Node

from geometry_msgs.msg import PoseStamped
from nav2_msgs.action import NavigateToPose
from std_msgs.msg import String


class MissionPlanner(Node):
    """
    High-level mission coordinator for hybrid Nav2 + VLA control.

    State machine
    ─────────────
      idle  →  navigating  →  vla  →  done  →  idle
                    ↓               ↓
                 (cancel)       (cancel)
                    ↓               ↓
                  idle            idle
    """

    def __init__(self):
        super().__init__("mission_planner")

        self._cb_group = ReentrantCallbackGroup()

        # ── Named locations database ──────────────────────────────────────────
        self._locations: dict = {}
        self._load_locations()

        # ── State ─────────────────────────────────────────────────────────────
        self._phase: str = "idle"  # idle | navigating | vla | done
        self._mission: dict | None = None  # parsed mission dict

        # ── Publishers ────────────────────────────────────────────────────────
        self._mode_pub = self.create_publisher(String, "/control_mode", 10)
        self._prompt_pub = self.create_publisher(String, "/vla/prompt", 10)
        self._status_pub = self.create_publisher(String, "/mission/status", 10)

        # ── Subscribers ───────────────────────────────────────────────────────
        self.create_subscription(
            String,
            "/mission/command",
            self._on_command,
            10,
            callback_group=self._cb_group,
        )
        self.create_subscription(
            String,
            "/mission/cancel",
            self._on_cancel,
            10,
            callback_group=self._cb_group,
        )

        # ── Nav2 action client ────────────────────────────────────────────────
        self._nav_client = ActionClient(
            self, NavigateToPose, "navigate_to_pose", callback_group=self._cb_group
        )

        # Publish status at 2 Hz
        self.create_timer(2.0, self._publish_status)

        self.get_logger().info(
            f"MissionPlanner ready. Known locations: {sorted(self._locations.keys())}"
        )

    # ── Location loading ──────────────────────────────────────────────────────

    def _load_locations(self) -> None:
        try:
            pkg_dir = get_package_share_directory("omnibot_hybrid")
            path = os.path.join(pkg_dir, "config", "named_locations.yaml")
            if os.path.exists(path):
                with open(path, "r") as f:
                    data = yaml.safe_load(f)
                    self._locations = data.get("locations", {})
                self.get_logger().info(
                    f"Loaded {len(self._locations)} named locations from {path}"
                )
            else:
                self.get_logger().warn(
                    f"named_locations.yaml not found at {path}. "
                    "Navigation phases will fail until the file is present."
                )
        except Exception as exc:
            self.get_logger().error(f"Failed to load named locations: {exc}")

    # ── Command parsing ───────────────────────────────────────────────────────

    def _parse_command(self, raw: str) -> dict | None:
        """
        Parse a mission string into a dict with optional keys:
          'navigate': str  — named location
          'vla':      str  — task description

        Accepted formats
        ────────────────
          "navigate:kitchen,vla:find the red cup"
          "navigate:bedroom"
          "vla:pick up the bottle"
          "go:hallway,vla:wave at the person"   (go/nav/nav2 are aliases)
        """
        NAV_KEYS = {"navigate", "nav", "nav2", "go"}
        mission: dict = {}
        for part in raw.split(","):
            part = part.strip()
            if ":" not in part:
                continue
            key, _, val = part.partition(":")
            key = key.strip().lower()
            val = val.strip()
            if key in NAV_KEYS:
                mission["navigate"] = val
            elif key == "vla":
                mission["vla"] = val
        return mission if mission else None

    # ── Subscriber callbacks ──────────────────────────────────────────────────

    def _on_command(self, msg: String) -> None:
        if self._phase not in ("idle", "done"):
            self.get_logger().warn(
                f'Mission already running (phase="{self._phase}"). '
                "Send any message to /mission/cancel first."
            )
            return

        mission = self._parse_command(msg.data)
        if not mission:
            self.get_logger().error(
                f'Could not parse mission: "{msg.data}". '
                'Expected format: "navigate:<location>,vla:<task>" '
                'or "navigate:<location>" or "vla:<task>"'
            )
            return

        self.get_logger().info(f"New mission: {mission}")
        self._mission = mission
        self._execute_mission()

    def _on_cancel(self, _msg: String) -> None:
        self.get_logger().info(f'Mission cancelled (was phase="{self._phase}").')
        self._phase = "idle"
        self._mission = None
        self._set_mode("nav2")  # return control to Nav2 on cancel
        self._publish_status()

    # ── Execution ─────────────────────────────────────────────────────────────

    def _execute_mission(self) -> None:
        if self._mission is None:
            return

        if "navigate" in self._mission:
            # Phase 1: navigate to named location
            location = self._mission["navigate"]
            pose = self._resolve_location(location)
            if pose is None:
                self.get_logger().error(
                    f'Unknown location "{location}". '
                    f"Known: {sorted(self._locations.keys())}"
                )
                self._phase = "idle"
                self._publish_status()
                return

            self._phase = "navigating"
            self._set_mode("nav2")
            self._publish_status()
            self.get_logger().info(
                f'[Phase 1/2] Navigating to "{location}" '
                f"({pose.pose.position.x:.2f}, {pose.pose.position.y:.2f})"
            )
            self._send_nav2_goal(pose)

        elif "vla" in self._mission:
            # VLA-only mission — skip navigation
            self._start_vla_phase()

    def _resolve_location(self, name: str) -> PoseStamped | None:
        loc = self._locations.get(name)
        if loc is None:
            return None

        pose = PoseStamped()
        pose.header.frame_id = "map"
        pose.header.stamp = self.get_clock().now().to_msg()
        pose.pose.position.x = float(loc["x"])
        pose.pose.position.y = float(loc["y"])
        pose.pose.position.z = 0.0

        yaw = float(loc.get("yaw", 0.0))
        pose.pose.orientation.z = math.sin(yaw / 2.0)
        pose.pose.orientation.w = math.cos(yaw / 2.0)
        return pose

    # ── Nav2 interaction ──────────────────────────────────────────────────────

    def _send_nav2_goal(self, pose: PoseStamped) -> None:
        if not self._nav_client.wait_for_server(timeout_sec=5.0):
            self.get_logger().error(
                "navigate_to_pose action server not available. "
                "Is Nav2 running? Aborting mission."
            )
            self._phase = "idle"
            self._set_mode("nav2")
            self._publish_status()
            return

        goal = NavigateToPose.Goal()
        goal.pose = pose
        future = self._nav_client.send_goal_async(goal)
        future.add_done_callback(self._on_nav2_goal_response)

    def _on_nav2_goal_response(self, future) -> None:
        goal_handle = future.result()
        if not goal_handle.accepted:
            self.get_logger().error("Nav2 goal was rejected.")
            self._phase = "idle"
            self._publish_status()
            return
        self.get_logger().info("Nav2 goal accepted — navigating...")
        goal_handle.get_result_async().add_done_callback(self._on_nav2_result)

    def _on_nav2_result(self, future) -> None:
        self.get_logger().info("Nav2 navigation complete.")
        if self._mission and "vla" in self._mission:
            self._start_vla_phase()
        else:
            self._phase = "done"
            self._set_mode("nav2")
            self._publish_status()
            self.get_logger().info("Mission complete (nav2-only).")

    # ── VLA phase ─────────────────────────────────────────────────────────────

    def _start_vla_phase(self) -> None:
        task = self._mission.get("vla", "")
        self.get_logger().info(f'[Phase 2/2] Starting VLA task — "{task}"')
        self._phase = "vla"
        self._set_mode("vla")
        self._publish_status()

        prompt = String()
        prompt.data = task
        self._prompt_pub.publish(prompt)
        self.get_logger().info(f'VLA prompt published: "{task}"')

    # ── Helpers ───────────────────────────────────────────────────────────────

    def _set_mode(self, mode: str) -> None:
        msg = String()
        msg.data = mode
        self._mode_pub.publish(msg)
        self.get_logger().info(f"[MissionPlanner] Control mode → {mode}")

    def _publish_status(self) -> None:
        mission_str = str(self._mission) if self._mission else "none"
        msg = String()
        msg.data = f"phase={self._phase} mission={mission_str}"
        self._status_pub.publish(msg)


def main(args=None):
    rclpy.init(args=args)
    node = MissionPlanner()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
