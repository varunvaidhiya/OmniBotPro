#!/usr/bin/env python3

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
        self.declare_parameter("device", "cuda")  # Use 'cuda' or 'cpu'
        self.declare_parameter(
            "load_in_4bit", False
        )  # Set True for 4-bit quantization if VRAM is low

        # Load Model configuration
        model_path = self.get_parameter("model_path").value
        self.device = self.get_parameter("device").value
        load_in_4bit = self.get_parameter("load_in_4bit").value

        self.get_logger().info(
            f"Loading OpenVLA model: {model_path} on {self.device}..."
        )

        # Initialize Processor and Model
        self.processor = AutoProcessor.from_pretrained(
            model_path, trust_remote_code=True
        )

        # Load model with optimizations
        # Note: 4-bit loading requires 'bitsandbytes' installed
        if load_in_4bit:
            self.model = AutoModelForVision2Seq.from_pretrained(
                model_path,
                attn_implementation="flash_attention_2",
                torch_dtype=torch.float16,
                load_in_4bit=True,
                trust_remote_code=True,
            )
        else:
            self.model = AutoModelForVision2Seq.from_pretrained(
                model_path,
                attn_implementation="flash_attention_2",
                torch_dtype=torch.float16,
                trust_remote_code=True,
            ).to(self.device)

        self.get_logger().info("Model loaded successfully!")

        # ROS 2 Interfaces
        self.bridge = CvBridge()
        self.last_image = None
        self.current_prompt = "Move forward"  # Default prompt

        self.create_subscription(Image, "/image_raw", self.image_callback, 10)
        self.create_subscription(String, "/vla/prompt", self.prompt_callback, 10)
        # Publish to /cmd_vel/vla so the cmd_vel_mux can select it when in
        # 'vla' mode.  The mux forwards the selected source to /cmd_vel/out,
        # which the robot driver reads.
        self.cmd_vel_pub = self.create_publisher(Twist, "/cmd_vel/vla", 10)

        # Processing Timer
        self.create_timer(
            1.0, self.inference_loop
        )  # Run inference at 1Hz (adjust based on performance)

    def image_callback(self, msg):
        try:
            # Convert ROS Image to OpenCV -> PIL
            cv_image = self.bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
            self.last_image = PILImage.fromarray(cv_image)
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

        try:
            # Prepare inputs
            inputs = self.processor(prompt, self.last_image).to(
                self.device, dtype=torch.float16
            )

            # Predict Action
            # OpenVLA returns specific action tokens that need decoding
            with torch.inference_mode():
                action = self.model.predict_action(
                    **inputs, unnorm_key="bridge_orig", do_sample=False
                )

            # Action is typically [x, y, z, r, p, y, gripper] (7-DOF)
            # We map this to 2D Base Control (vx, vy, w)
            # MAPPING:
            # action[0] -> Linear X (Forward/Back)
            # action[1] -> Linear Y (Left/Right)
            # action[5] -> Angular Z (Yaw)

            # Move to CPU numpy
            action = action.cpu().numpy()  # Shape [1, 7] usually

            twist = Twist()
            twist.linear.x = float(action[0])  # Scale if necessary
            twist.linear.y = float(action[1])
            twist.angular.z = float(action[5])

            self.cmd_vel_pub.publish(twist)
            self.get_logger().info(
                f"Action: [vx={twist.linear.x:.2f}, vy={twist.linear.y:.2f}, w={twist.angular.z:.2f}]"
            )

        except Exception as e:
            self.get_logger().error(f"Inference failed: {e}")


def main(args=None):
    rclpy.init(args=args)
    node = VLANode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
