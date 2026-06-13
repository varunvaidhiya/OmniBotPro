#!/usr/bin/env python3

import collections
import os
import statistics as _statistics
import time
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image
from geometry_msgs.msg import Twist
from std_msgs.msg import String
from cv_bridge import CvBridge
import torch
from transformers import AutoModelForVision2Seq, AutoProcessor
from PIL import Image as PILImage


class VLANode(Node):
    def __init__(self):
        super().__init__("vla_node")

        # Parameters
        self.declare_parameter("model_path", "openvla/openvla-7b")
        self.declare_parameter("device", "cuda")
        self.declare_parameter("load_in_4bit", False)
        # Set True to publish rolling inference timing to /diagnostics at 1 Hz.
        self.declare_parameter("publish_diagnostics", False)

        # Load Model configuration
        model_path = self.get_parameter("model_path").value
        self.device = self.get_parameter("device").value
        load_in_4bit = self.get_parameter("load_in_4bit").value

        self.get_logger().info(
            f"Loading OpenVLA model: {model_path} on {self.device}..."
        )

        # Validate model source — trust_remote_code is required for OpenVLA
        # but introduces arbitrary code execution risk. Only enable it for
        # models from the trusted openvla HuggingFace organisation.
        _trusted = model_path.startswith("openvla/") or os.path.isdir(model_path)
        if not _trusted:
            self.get_logger().warn(
                f"Loading model from untrusted path: {model_path}. "
                "trust_remote_code=True allows arbitrary code execution. "
                "Ensure you trust this model source."
            )

        # Initialize Processor and Model
        self.processor = AutoProcessor.from_pretrained(
            model_path, trust_remote_code=_trusted
        )

        # Load model with optimizations
        # Note: 4-bit loading requires 'bitsandbytes' installed
        if load_in_4bit:
            self.model = AutoModelForVision2Seq.from_pretrained(
                model_path,
                attn_implementation="flash_attention_2",
                torch_dtype=torch.float16,
                load_in_4bit=True,
                trust_remote_code=_trusted,
            )
        else:
            self.model = AutoModelForVision2Seq.from_pretrained(
                model_path,
                attn_implementation="flash_attention_2",
                torch_dtype=torch.float16,
                trust_remote_code=_trusted,
            ).to(self.device)

        self.get_logger().info("Model loaded successfully!")

        self._diag_enabled = self.get_parameter("publish_diagnostics").value
        self._t_inference = collections.deque(maxlen=100)
        self._t_preprocess = collections.deque(maxlen=100)

        # ROS 2 Interfaces
        self.bridge = CvBridge()
        self.last_image = None
        self.current_prompt = "Move forward"

        self.create_subscription(Image, "/image_raw", self.image_callback, 10)
        self.create_subscription(String, "/vla/prompt", self.prompt_callback, 10)
        # Publish to /cmd_vel/vla so the cmd_vel_mux can select it when in
        # 'vla' mode.  The mux forwards the selected source to /cmd_vel/out,
        # which the robot driver reads.
        self.cmd_vel_pub = self.create_publisher(Twist, "/cmd_vel/vla", 10)

        # Processing Timer — 1 Hz
        self.create_timer(1.0, self.inference_loop)

        if self._diag_enabled:
            from diagnostic_msgs.msg import DiagnosticArray, DiagnosticStatus, KeyValue

            self._DiagnosticArray = DiagnosticArray
            self._DiagnosticStatus = DiagnosticStatus
            self._KeyValue = KeyValue
            self._diag_pub_vla = self.create_publisher(
                DiagnosticArray, "/diagnostics", 10
            )
            self.create_timer(1.0, self._publish_diagnostics)

    def image_callback(self, msg):
        try:
            _tp0 = time.perf_counter() if self._diag_enabled else None
            self.last_image = self.bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
            if self._diag_enabled and _tp0 is not None:
                self._t_preprocess.append((time.perf_counter() - _tp0) * 1000.0)
        except Exception as e:
            self.get_logger().error(f"Image conversion failed: {e}")

    def prompt_callback(self, msg):
        self.current_prompt = msg.data
        self.get_logger().info(f"Received new prompt: {self.current_prompt}")

    def inference_loop(self):
        if self.last_image is None:
            return

        prompt = (
            f"In: What action should the robot take to {self.current_prompt}?\nOut:"
        )

        _ti0 = time.perf_counter() if self._diag_enabled else None
        try:
            inputs = self.processor(prompt, PILImage.fromarray(self.last_image)).to(
                self.device, dtype=torch.float16
            )

            with torch.inference_mode():
                action = self.model.predict_action(
                    **inputs, unnorm_key="bridge_orig", do_sample=False
                )

            if self._diag_enabled and _ti0 is not None:
                self._t_inference.append((time.perf_counter() - _ti0) * 1000.0)

            # Action: [x, y, z, roll, pitch, yaw, gripper] — map to base Twist
            action = action.cpu().numpy()
            twist = Twist()
            twist.linear.x = float(action[0])
            twist.linear.y = float(action[1])
            twist.angular.z = float(action[5])

            self.cmd_vel_pub.publish(twist)
            self.get_logger().info(
                f"Action: [vx={twist.linear.x:.2f}, vy={twist.linear.y:.2f}, w={twist.angular.z:.2f}]"
            )

        except Exception as e:
            self.get_logger().error(f"Inference failed: {e}")

    def _publish_diagnostics(self) -> None:
        msg = self._DiagnosticArray()
        msg.header.stamp = self.get_clock().now().to_msg()

        def _make(name, deque_, warn_ms, err_ms):
            st = self._DiagnosticStatus()
            st.name = name
            if not deque_:
                st.level = self._DiagnosticStatus.OK
                st.message = "no data"
                return st
            s = sorted(deque_)
            n = len(s)
            p95 = s[max(0, int(0.95 * n) - 1)]
            st.level = (
                self._DiagnosticStatus.ERROR
                if p95 > err_ms
                else self._DiagnosticStatus.WARN
                if p95 > warn_ms
                else self._DiagnosticStatus.OK
            )
            st.message = f"p95={p95:.0f}ms"
            for k, v in [
                ("mean_ms", _statistics.mean(s)),
                ("p50_ms", s[n // 2]),
                ("p95_ms", p95),
                ("max_ms", s[-1]),
                ("n", float(n)),
            ]:
                kv = self._KeyValue()
                kv.key = k
                kv.value = f"{v:.1f}"
                st.values.append(kv)
            return st

        msg.status = [
            _make("openvla/inference_ms", self._t_inference, 500.0, 2000.0),
            _make("openvla/image_preprocess_ms", self._t_preprocess, 5.0, 20.0),
        ]
        self._diag_pub_vla.publish(msg)


def main(args=None):
    rclpy.init(args=args)
    node = VLANode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
