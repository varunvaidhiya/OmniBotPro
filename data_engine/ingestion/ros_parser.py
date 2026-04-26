"""ROS2 bag parsing utilities"""

from rosbags.rosbag2 import Reader
from rosbags.serde import deserialize_cdr
import numpy as np
from typing import Dict, List, Tuple, Any
from pathlib import Path


class ROSBagParser:
    """Parse ROS2 bag files and extract data"""

    def __init__(self, bag_path: str, topic_config: Dict):
        """
        Args:
            bag_path: Path to ROS2 bag directory
            topic_config: Dict mapping data types to topics
                Example:
                {
                    'cameras': {
                        'front': '/camera/front/image_raw/compressed',
                        'wrist': '/camera/wrist/image_raw/compressed'
                    },
                    'state': '/robot/odom',
                    'cmd_vel': '/cmd_vel'
                }
        """
        self.bag_path = Path(bag_path)
        self.topic_config = topic_config
        self.reader = None

    def __enter__(self):
        self.reader = Reader(self.bag_path)
        self.reader.open()
        return self

    def __exit__(self, *args):
        if self.reader:
            self.reader.close()

    def get_topic_messages(self, topic: str) -> List[Tuple[int, Any]]:
        """
        Extract all messages from a topic.

        Returns:
            List of (timestamp_nanoseconds, deserialized_message) tuples
        """
        messages = []

        # Get connections for this topic
        connections = [c for c in self.reader.connections if c.topic == topic]

        if not connections:
            print(f"Warning: No data found for topic {topic}")
            return messages

        # Read messages
        for connection, timestamp, rawdata in self.reader.messages(
            connections=connections
        ):
            msg = deserialize_cdr(rawdata, connection.msgtype)
            messages.append((timestamp, msg))

        return messages

    def decode_compressed_image(self, msg) -> np.ndarray:
        """
        Decode CompressedImage message to numpy array.

        Args:
            msg: sensor_msgs/CompressedImage message

        Returns:
            (H, W, 3) RGB uint8 array
        """
        import cv2

        # Decode JPEG/PNG
        nparr = np.frombuffer(msg.data, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

        if img is None:
            raise RuntimeError("Failed to decode image")

        # Convert BGR to RGB
        rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        return rgb

    def decode_image(self, msg) -> np.ndarray:
        """
        Decode raw Image message to numpy array.

        Args:
            msg: sensor_msgs/Image message

        Returns:
            (H, W, 3) RGB uint8 array
        """
        import cv2

        # Reshape according to image dimensions
        img = np.frombuffer(msg.data, dtype=np.uint8)
        img = img.reshape((msg.height, msg.width, -1))

        # Handle encoding
        if msg.encoding == "bgr8":
            img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        elif msg.encoding == "rgb8":
            pass  # Already RGB
        else:
            raise ValueError(f"Unsupported encoding: {msg.encoding}")

        return img

    def parse_odometry(self, msg) -> np.ndarray:
        """
        Parse Odometry message to state vector.

        Args:
            msg: nav_msgs/Odometry message

        Returns:
            State array [x, y, theta, vx, vy, omega, 0, 0, 0, 0]
            Note: Motor values filled with zeros (get from motor topic)
        """
        # Position
        x = msg.pose.pose.position.x
        y = msg.pose.pose.position.y

        # Orientation (quaternion to yaw)
        qx = msg.pose.pose.orientation.x
        qy = msg.pose.pose.orientation.y
        qz = msg.pose.pose.orientation.z
        qw = msg.pose.pose.orientation.w

        # Convert to Euler angle (yaw)
        import math

        siny_cosp = 2 * (qw * qz + qx * qy)
        cosy_cosp = 1 - 2 * (qy * qy + qz * qz)
        theta = math.atan2(siny_cosp, cosy_cosp)

        # Velocity
        vx = msg.twist.twist.linear.x
        vy = msg.twist.twist.linear.y
        omega = msg.twist.twist.angular.z

        # State vector (motors filled separately)
        state = np.array([x, y, theta, vx, vy, omega, 0, 0, 0, 0], dtype=np.float32)
        return state

    def parse_twist(self, msg) -> np.ndarray:
        """
        Parse Twist message (cmd_vel) to action vector.

        Returns:
            [linear_x, linear_y, angular_z]
        """
        action = np.array([msg.linear.x, msg.linear.y, msg.angular.z], dtype=np.float32)
        return action

    def extract_all_data(self) -> Dict[str, List[Tuple]]:
        """
        Extract all relevant data from bag.

        Returns:
            {
                'cameras': {
                    'front': [(timestamp, image), ...],
                    'wrist': [(timestamp, image), ...]
                },
                'states': [(timestamp, state_array), ...],
                'actions': [(timestamp, action_array), ...]
            }
        """
        from tqdm import tqdm

        data = {"cameras": {}, "states": [], "actions": []}

        # Extract camera data
        if "cameras" in self.topic_config:
            for cam_name, topic in self.topic_config["cameras"].items():
                print(f"Extracting {cam_name} camera from {topic}...")
                messages = self.get_topic_messages(topic)

                images = []
                for ts, msg in tqdm(messages, desc=f"Decoding {cam_name}"):
                    # Handle both compressed and raw images
                    if hasattr(msg, "format"):  # CompressedImage
                        img = self.decode_compressed_image(msg)
                    else:  # Raw Image
                        img = self.decode_image(msg)
                    images.append((ts, img))

                data["cameras"][cam_name] = images

        # Extract state
        if "state" in self.topic_config:
            print(f"Extracting state from {self.topic_config['state']}...")
            messages = self.get_topic_messages(self.topic_config["state"])
            for ts, msg in tqdm(messages, desc="Parsing odometry"):
                state = self.parse_odometry(msg)
                data["states"].append((ts, state))

        # Extract actions
        if "cmd_vel" in self.topic_config:
            print(f"Extracting actions from {self.topic_config['cmd_vel']}...")
            messages = self.get_topic_messages(self.topic_config["cmd_vel"])
            for ts, msg in tqdm(messages, desc="Parsing cmd_vel"):
                action = self.parse_twist(msg)
                data["actions"].append((ts, action))

        return data
