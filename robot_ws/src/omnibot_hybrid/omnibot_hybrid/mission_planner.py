#!/usr/bin/env python3
"""
Mission Planner — Hybrid Nav2 + VLA + RL orchestrator for OmniBot.

Parses natural mission strings and executes them in sequential phases:
  Phase 1 (nav2):    Navigate to a named location via Nav2 NavigateToPose.
  Phase 2 (vla):     Execute a semantic VLA task at the destination.
  Phase 1b (rl_nav): Navigate short-range (≤ 3 m) using RL local nav policy.
  Phase 2b (rl_arm): Execute precise arm manipulation using RL arm policy.

Mission command format  (/mission/command  std_msgs/String)
────────────────────────────────────────────────────────────
  Full hybrid (Nav2 + VLA):
    "navigate:kitchen,vla:find the red cup"
  Navigation only (Nav2):
    "navigate:bedroom"
  VLA only:
    "vla:follow the person"
  RL navigation + RL arm:
    "rl_nav:kitchen,rl_arm:pick up the cup"
  RL arm only:
    "rl_arm:pick up the cup"
  RL navigation only:
    "rl_nav:workspace"

Named locations are read from config/named_locations.yaml at startup.
rl_nav uses the same named locations as navigate.

Published topics
────────────────
  /control_mode     std_msgs/String  — "nav2" | "vla" | "teleop" | "rl_nav"
  /vla/prompt       std_msgs/String  — task description forwarded to VLA node
  /mission/status   std_msgs/String  — human-readable status string
  /rl_nav/goal      geometry_msgs/PoseStamped  — goal for RL nav node
  /arm/cmd_mode     std_msgs/String  — "smolvla" | "rl_arm"

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
from std_msgs.msg import Bool, String

# RL nav arrival detection: subscribe to /rl_nav/goal distance feedback
# via /odom. Mission planner polls for arrival using a simple timer.
_RL_NAV_ARRIVAL_POLL_HZ = 2.0   # Hz
_RL_NAV_GOAL_TOLERANCE  = 0.30  # m — slightly looser than rl_nav_node's own tol


class MissionPlanner(Node):
    """
    High-level mission coordinator for hybrid Nav2 + VLA + RL control.

    State machine
    ─────────────
      idle  →  navigating  →  vla       →  done  →  idle
                    ↓               ↓
              rl_navigating  →  rl_arm  →  done  →  idle
              (cancel from any phase returns to idle)
    """

    def __init__(self):
        super().__init__("mission_planner")

        self._cb_group = ReentrantCallbackGroup()

        # ── Named locations database ──────────────────────────────────────────
        self._locations: dict = {}
        self._load_locations()

        # ── State ─────────────────────────────────────────────────────────────
        # idle | navigating | vla | rl_navigating | rl_arm | done
        self._phase: str = "idle"
        self._mission: dict | None = None
        self._rl_nav_goal: PoseStamped | None = None  # for arrival polling
        self._odom_x: float = 0.0
        self._odom_y: float = 0.0

        # ── Publishers ────────────────────────────────────────────────────────
        self._mode_pub = self.create_publisher(String, "/control_mode", 10)
        self._prompt_pub = self.create_publisher(String, "/vla/prompt", 10)
        self._status_pub = self.create_publisher(String, "/mission/status", 10)
        self._rl_goal_pub = self.create_publisher(PoseStamped, "/rl_nav/goal", 10)
        self._arm_mode_pub = self.create_publisher(String, "/arm/cmd_mode", 10)
        # SmolVLA-specific: task description and enable/disable
        self._smolvla_task_pub = self.create_publisher(String, "/smolvla/task", 10)
        self._smolvla_enable_pub = self.create_publisher(Bool, "/smolvla/enable", 10)

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

        # Odometry for RL nav arrival polling
        from nav_msgs.msg import Odometry as _Odometry
        self.create_subscription(
            _Odometry, '/odom', self._odom_cb, 10,
            callback_group=self._cb_group)

        # ── Nav2 action client ────────────────────────────────────────────────
        self._nav_client = ActionClient(
            self, NavigateToPose, "navigate_to_pose", callback_group=self._cb_group
        )

        # Publish status at 2 Hz
        self.create_timer(2.0, self._publish_status)
        # Poll RL nav arrival at _RL_NAV_ARRIVAL_POLL_HZ
        self.create_timer(1.0 / _RL_NAV_ARRIVAL_POLL_HZ, self._poll_rl_nav_arrival)

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
          'navigate': str  — named location (Nav2)
          'vla':      str  — task description (SmolVLA)
          'rl_nav':   str  — named location (RL nav policy)
          'rl_arm':   str  — task description (RL arm policy)

        Accepted formats
        ────────────────
          "navigate:kitchen,vla:find the red cup"
          "navigate:bedroom"
          "vla:pick up the bottle"
          "go:hallway,vla:wave at the person"   (go/nav/nav2 are aliases)
          "rl_nav:kitchen,rl_arm:pick up the cup"
          "rl_arm:pick up the cup"
          "rl_nav:workspace"
        """
        NAV_KEYS = {"navigate", "nav", "nav2", "go"}
        RL_NAV_KEYS = {"rl_nav", "rl_go"}
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
            elif key in RL_NAV_KEYS:
                mission["rl_nav"] = val
            elif key == "rl_arm":
                mission["rl_arm"] = val
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
                'or "rl_nav:<location>,rl_arm:<task>" '
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
        self._rl_nav_goal = None
        self._set_mode("nav2")        # return base control to Nav2
        self._set_arm_mode("smolvla")  # return arm control to SmolVLA
        self._publish_status()

    def _odom_cb(self, msg) -> None:
        self._odom_x = msg.pose.pose.position.x
        self._odom_y = msg.pose.pose.position.y

    # ── Execution ─────────────────────────────────────────────────────────────

    def _execute_mission(self) -> None:
        if self._mission is None:
            return

        if "navigate" in self._mission:
            # Phase 1 (Nav2): navigate to named location
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

        elif "rl_nav" in self._mission:
            # Phase 1b (RL nav): short-range RL navigation to named location
            location = self._mission["rl_nav"]
            pose = self._resolve_location(location)
            if pose is None:
                self.get_logger().error(
                    f'Unknown location "{location}". '
                    f"Known: {sorted(self._locations.keys())}"
                )
                self._phase = "idle"
                self._publish_status()
                return
            self._start_rl_nav_phase(pose, location)

        elif "vla" in self._mission:
            # VLA-only mission — skip navigation
            self._start_vla_phase()

        elif 'rl_arm' in self._mission:
            # RL arm only — skip navigation
            self._start_rl_arm_phase()

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

    # ── RL navigation phase ───────────────────────────────────────────────────

    def _start_rl_nav_phase(self, pose: PoseStamped, location: str) -> None:
        self.get_logger().info(
            f'[RL Phase 1] RL navigation to "{location}" '
            f'({pose.pose.position.x:.2f}, {pose.pose.position.y:.2f})')
        self._phase = 'rl_navigating'
        self._rl_nav_goal = pose
        self._set_mode('rl_nav')
        self._publish_status()
        # Publish goal to rl_nav_node
        self._rl_goal_pub.publish(pose)

    def _poll_rl_nav_arrival(self) -> None:
        """Check if robot has reached the RL nav goal (polled at 2 Hz)."""
        if self._phase != 'rl_navigating' or self._rl_nav_goal is None:
            return

        gx = self._rl_nav_goal.pose.position.x
        gy = self._rl_nav_goal.pose.position.y
        dist = math.sqrt(
            (gx - self._odom_x) ** 2 + (gy - self._odom_y) ** 2)

        if dist < _RL_NAV_GOAL_TOLERANCE:
            self.get_logger().info('[RL Phase 1] RL navigation goal reached.')
            self._rl_nav_goal = None
            if self._mission and 'rl_arm' in self._mission:
                self._start_rl_arm_phase()
            else:
                self._phase = 'done'
                self._set_mode('nav2')   # return base to Nav2 after RL nav
                self._publish_status()
                self.get_logger().info('Mission complete (rl_nav-only).')

    # ── RL arm phase ──────────────────────────────────────────────────────────

    def _start_rl_arm_phase(self) -> None:
        task = self._mission.get('rl_arm', '') if self._mission else ''
        self.get_logger().info(f'[RL Phase 2] RL arm task — "{task}"')
        self._phase = 'rl_arm'
        self._set_arm_mode('rl_arm')
        self._publish_status()
        # Note: rl_arm_node reads /rl_arm/target_pose from rl_object_pose_node.
        # Mission planner does not need to supply a specific target — the pose
        # estimator continuously updates /rl_arm/target_pose.
        self.get_logger().info(
            'RL arm active. Waiting for object detection via /rl_arm/target_pose.')

    # ── VLA phase ─────────────────────────────────────────────────────────────

    def _start_vla_phase(self) -> None:
        task = self._mission.get("vla", "")
        self.get_logger().info(f'[Phase 2/2] Starting VLA task — "{task}"')
        self._phase = "vla"
        self._set_mode("vla")
        self._set_arm_mode("smolvla")
        self._publish_status()

        # SmolVLA: set task description then enable inference
        task_msg = String()
        task_msg.data = task
        self._smolvla_task_pub.publish(task_msg)

        enable_msg = Bool()
        enable_msg.data = True
        self._smolvla_enable_pub.publish(enable_msg)

        # OpenVLA (legacy): also publish to /vla/prompt for backward compatibility
        prompt = String()
        prompt.data = task
        self._prompt_pub.publish(prompt)

        self.get_logger().info(f'VLA task started: "{task}"')

    # ── Helpers ───────────────────────────────────────────────────────────────

    def _set_mode(self, mode: str) -> None:
        msg = String()
        msg.data = mode
        self._mode_pub.publish(msg)
        # Disable SmolVLA inference whenever we leave "vla" mode
        if mode != "vla":
            disable = Bool()
            disable.data = False
            self._smolvla_enable_pub.publish(disable)
        self.get_logger().info(f"[MissionPlanner] Control mode → {mode}")

    def _set_arm_mode(self, mode: str) -> None:
        msg = String()
        msg.data = mode
        self._arm_mode_pub.publish(msg)
        self.get_logger().info(f'[MissionPlanner] Arm mode → {mode}')

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
