#!/usr/bin/env python3
"""
VLA trigger / handoff node.

Monitors perception state and decides when to escalate from
detection/tracking to VLA inference:

  track mode  +  target found    → stay in track mode, keep RL arm active
  track mode  +  target lost > T → switch perception to 'vla', enable SmolVLA
  vla mode    +  target found    → optionally switch back to track + RL arm

Also forwards a structured task description to /smolvla/task based on
the class name of the last detected target.

Publishes:
  /perception/mode       String  – switches perception_node mode
  /smolvla/enable        Bool    – gate SmolVLA inference
  /smolvla/task          String  – task description for SmolVLA
  /vla/prompt            String  – free-text prompt for OpenVLA
  /arm/cmd_mode          String  – switches arm_cmd_mux (smolvla | rl_arm)
"""

import json

import rclpy
from rclpy.node import Node
from std_msgs.msg import Bool, String


class VlaTriggerNode(Node):
    def __init__(self):
        super().__init__("vla_trigger_node")

        self.declare_parameter("loss_threshold_s", 1.5)
        self.declare_parameter("task_template", "pick up the {object} and place it")
        self.declare_parameter("auto_vla", False)
        self.declare_parameter("target_class", "")
        self.declare_parameter("reacquire_returns_to_track", True)
        self.declare_parameter("check_hz", 5.0)

        p = self.get_parameter
        self._loss_thresh = p("loss_threshold_s").value
        self._task_tpl = p("task_template").value
        self._auto_vla = p("auto_vla").value
        self._target_class = p("target_class").value.lower()
        self._reacquire = p("reacquire_returns_to_track").value

        self._vla_active = False
        self._tracking_lost = False
        self._loss_start: rclpy.time.Time | None = None
        self._last_class = ""
        self._current_perception_mode = "track"

        # ── Subscriptions ─────────────────────────────────────────────────────
        self.create_subscription(Bool, "/perception/tracking_lost", self._lost_cb, 10)
        self.create_subscription(String, "/perception/detections", self._dets_cb, 10)
        self.create_subscription(String, "/perception/mode", self._mode_cb, 10)
        # External command to force VLA on/off
        self.create_subscription(String, "/vla_trigger/command", self._cmd_cb, 10)

        # ── Publications ──────────────────────────────────────────────────────
        self._pub_pmode = self.create_publisher(String, "/perception/mode", 10)
        self._pub_smolvla_en = self.create_publisher(Bool, "/smolvla/enable", 10)
        self._pub_smolvla_task = self.create_publisher(String, "/smolvla/task", 10)
        self._pub_vla_prompt = self.create_publisher(String, "/vla/prompt", 10)
        self._pub_arm_mode = self.create_publisher(String, "/arm/cmd_mode", 10)
        self._pub_status = self.create_publisher(
            String, "/perception/vla_trigger_status", 10
        )

        hz = max(1.0, p("check_hz").value)
        self.create_timer(1.0 / hz, self._check)
        self.get_logger().info(
            f"VlaTriggerNode ready  auto_vla={self._auto_vla}  "
            f"target={self._target_class!r}"
        )

    # ── Callbacks ─────────────────────────────────────────────────────────────

    def _lost_cb(self, msg: Bool) -> None:
        now = self.get_clock().now()
        if msg.data and not self._tracking_lost:
            self._loss_start = now
        elif not msg.data:
            self._loss_start = None
            if self._vla_active and self._reacquire:
                self.get_logger().info("Target reacquired – deactivating VLA")
                self._deactivate_vla()
        self._tracking_lost = msg.data

    def _dets_cb(self, msg: String) -> None:
        try:
            dets: list[dict] = json.loads(msg.data)
        except (json.JSONDecodeError, ValueError):
            return
        if dets:
            best = max(dets, key=lambda d: d.get("confidence", 0))
            self._last_class = best.get("class_name", "").lower()

    def _mode_cb(self, msg: String) -> None:
        self._current_perception_mode = msg.data.strip().lower()

    def _cmd_cb(self, msg: String) -> None:
        cmd = msg.data.strip().lower()
        if cmd == "activate_vla":
            self._activate_vla()
        elif cmd == "deactivate_vla":
            self._deactivate_vla()
        elif cmd.startswith("target:"):
            self._target_class = cmd.split(":", 1)[1].strip()
            self.get_logger().info(f"Target class updated → {self._target_class!r}")

    # ── Timer check ───────────────────────────────────────────────────────────

    def _check(self) -> None:
        if not self._auto_vla or self._vla_active:
            return
        if not (self._tracking_lost and self._loss_start is not None):
            return
        elapsed = (self.get_clock().now() - self._loss_start).nanoseconds / 1e9
        if elapsed >= self._loss_thresh:
            self.get_logger().info(f"Tracking lost for {elapsed:.1f}s → activating VLA")
            self._activate_vla()

    # ── VLA activation helpers ────────────────────────────────────────────────

    def _activate_vla(self) -> None:
        self._vla_active = True
        obj = self._target_class or self._last_class or "object"
        task = self._task_tpl.format(object=obj)
        self.get_logger().info(f"VLA activated  task={task!r}")

        self._send_str(self._pub_pmode, "vla")
        self._send_bool(self._pub_smolvla_en, True)
        self._send_str(self._pub_smolvla_task, task)
        self._send_str(self._pub_vla_prompt, task)
        self._send_str(self._pub_arm_mode, "smolvla")
        self._send_str(self._pub_status, f"vla_active:{task}")

    def _deactivate_vla(self) -> None:
        self._vla_active = False
        self._send_bool(self._pub_smolvla_en, False)
        self._send_str(self._pub_pmode, "track")
        self._send_str(self._pub_arm_mode, "rl_arm")
        self._send_str(self._pub_status, "tracking")

    # ── Helpers ───────────────────────────────────────────────────────────────

    def _send_str(self, pub, text: str) -> None:
        msg = String()
        msg.data = text
        pub.publish(msg)

    def _send_bool(self, pub, value: bool) -> None:
        msg = Bool()
        msg.data = value
        pub.publish(msg)


def main(args=None) -> None:
    rclpy.init(args=args)
    node = VlaTriggerNode()
    try:
        rclpy.spin(node)
    finally:
        node.destroy_node()
        rclpy.shutdown()
