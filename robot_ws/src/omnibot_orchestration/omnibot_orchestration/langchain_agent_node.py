#!/usr/bin/env python3
"""
LangGraph AI orchestration node for OmniBot.

Subscribes to /ai/command (natural language String) and uses a LangGraph
state machine backed by Claude to decompose the request into structured robot
commands, publishing to /mission/command and /mission/cancel (consumed by
mission_planner).

Runs on the AI desktop PC alongside the VLA nodes, NOT on the Raspberry Pi.

Usage:
  export ANTHROPIC_API_KEY=sk-ant-...
  ros2 launch omnibot_orchestration langchain_agent.launch.py

  # Send a natural language command
  ros2 topic pub --once /ai/command std_msgs/msg/String \\
    "data: 'Fetch the coffee cup from the kitchen and bring it to me'"
"""

import concurrent.futures
import os
import threading
import time
from typing import Any, Dict, Optional

import numpy as np
import rclpy
import yaml
from rclpy.node import Node
from std_msgs.msg import String

try:
    from opentelemetry import trace
    from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
    from opentelemetry.sdk.resources import Resource
    from opentelemetry.sdk.trace import TracerProvider
    from opentelemetry.sdk.trace.export import BatchSpanProcessor

    _OTEL_AVAILABLE = True
except ImportError:
    _OTEL_AVAILABLE = False

try:
    from sensor_msgs.msg import Image
    from cv_bridge import CvBridge

    CV_BRIDGE_AVAILABLE = True
except ImportError:
    CV_BRIDGE_AVAILABLE = False

from omnibot_orchestration.memory.entity_memory import EntityMemory
from omnibot_orchestration.vision.scene_describer import SceneDescriber

try:
    from omnibot_orchestration.graph.mission_graph import build_mission_graph
    from omnibot_orchestration.graph.state import initial_state

    _LANGGRAPH_AVAILABLE = True
except ImportError:
    _LANGGRAPH_AVAILABLE = False


class LangchainAgentNode(Node):
    """LangGraph state-machine agent as a ROS 2 node."""

    def __init__(self):
        super().__init__("langchain_agent_node")

        self._declare_parameters()

        p = self.get_parameter
        self._api_key: str = p("anthropic_api_key").value
        self._vla_serve_url: str = p("vla_serve_url").value
        self._locations_yaml: str = p("locations_yaml").value
        self._entity_memory_path: str = p("entity_memory_path").value
        self._use_claude_vision: bool = p("use_claude_vision").value

        # ── LangSmith tracing (optional) ───────────────────────────────────
        if p("langchain_tracing_v2").value:
            os.environ["LANGCHAIN_TRACING_V2"] = "true"
            os.environ["LANGCHAIN_API_KEY"] = p("langchain_api_key").value
            os.environ["LANGCHAIN_PROJECT"] = p("langchain_project").value

        # ── OpenTelemetry tracing → Grafana Tempo ──────────────────────────
        self._tracer: Optional[Any] = None
        if _OTEL_AVAILABLE and p("otel_enabled").value:
            self._setup_otel(p("otel_endpoint").value)
        elif not _OTEL_AVAILABLE:
            self.get_logger().info(
                "opentelemetry packages not installed — distributed tracing disabled. "
                "Run: pip install opentelemetry-sdk opentelemetry-exporter-otlp-proto-grpc"
            )

        # ── Thread-shared state ────────────────────────────────────────────
        self._image_lock = threading.Lock()
        self._status_lock = threading.Lock()
        self._latest_front_image: Optional[np.ndarray] = None
        self._latest_wrist_image: Optional[np.ndarray] = None
        self._latest_mission_status: Optional[str] = None
        # Latest metric object detections from object_perception_node
        # (JSON string from /perception/object_info), with receive time.
        self._latest_perception: Optional[str] = None
        self._latest_perception_time: float = 0.0

        # ── ROS interfaces ─────────────────────────────────────────────────
        self._setup_ros_interfaces()

        # ── Locations ─────────────────────────────────────────────────────
        self._locations: Dict[str, Any] = {}
        self._load_locations()

        # ── Memory ────────────────────────────────────────────────────────
        self._entity_memory = EntityMemory(self._entity_memory_path)

        # ── Vision ────────────────────────────────────────────────────────
        self._vision = SceneDescriber(
            node=self,
            use_claude_vision=self._use_claude_vision,
            vla_serve_url=self._vla_serve_url,
            anthropic_api_key=self._api_key,
        )

        # ── LangGraph mission graph ────────────────────────────────────────
        self._graph: Optional[Any] = None
        self._build_langchain_agent()

        # ── Thread pool (max 1 concurrent agent call) ──────────────────────
        self._executor = concurrent.futures.ThreadPoolExecutor(max_workers=1)
        self._active_future: Optional[concurrent.futures.Future] = None

        self.get_logger().info(
            "LangGraph mission agent ready. Listening on /ai/command."
        )

    # ── OpenTelemetry setup ────────────────────────────────────────────────

    def _setup_otel(self, endpoint: str) -> None:
        try:
            resource = Resource.create({"service.name": "omnibot.orchestration"})
            provider = TracerProvider(resource=resource)
            provider.add_span_processor(
                BatchSpanProcessor(OTLPSpanExporter(endpoint=endpoint, insecure=True))
            )
            trace.set_tracer_provider(provider)
            self._tracer = trace.get_tracer("omnibot.orchestration")
            self.get_logger().info(
                f"OpenTelemetry tracing enabled — exporting to {endpoint}"
            )
        except Exception as e:
            self.get_logger().warn(f"Failed to initialise OpenTelemetry: {e}")
            self._tracer = None

    # ── Parameter declarations ─────────────────────────────────────────────

    def _declare_parameters(self) -> None:
        self.declare_parameter("anthropic_api_key", "")
        self.declare_parameter("vla_serve_url", "http://localhost:8000")
        self.declare_parameter("locations_yaml", "")
        self.declare_parameter("entity_memory_path", "~/.omnibot/entity_memory.json")
        self.declare_parameter("use_claude_vision", True)
        self.declare_parameter("use_sim_time", False)
        # LangSmith observability
        self.declare_parameter("langchain_tracing_v2", False)
        self.declare_parameter("langchain_api_key", "")
        self.declare_parameter("langchain_project", "omnibot")
        # OpenTelemetry tracing → Grafana Tempo
        self.declare_parameter("otel_endpoint", "http://localhost:4317")
        self.declare_parameter("otel_enabled", True)

    # ── ROS setup ──────────────────────────────────────────────────────────

    def _setup_ros_interfaces(self) -> None:
        qos = rclpy.qos.QoSProfile(depth=10)

        self._mission_pub = self.create_publisher(String, "/mission/command", qos)
        self._cancel_pub = self.create_publisher(String, "/mission/cancel", qos)
        self._status_pub = self.create_publisher(String, "/ai/status", qos)
        self._response_needed_pub = self.create_publisher(
            String, "/ai/response_needed", qos
        )

        self.create_subscription(String, "/ai/command", self._on_ai_command, qos)
        self.create_subscription(
            String, "/mission/status", self._on_mission_status, qos
        )
        # Metric object perception (distance / bearing / pose per object)
        self.create_subscription(
            String, "/perception/object_info", self._on_perception_info, qos
        )

        if CV_BRIDGE_AVAILABLE:
            self._bridge = CvBridge()
            self.create_subscription(
                Image, "/camera/front/image_raw", self._on_front_image, qos
            )
            self.create_subscription(
                Image, "/camera/wrist/image_raw", self._on_wrist_image, qos
            )
        else:
            self._bridge = None
            self.get_logger().warn(
                "cv_bridge not available — scene description from camera disabled."
            )

    # ── Location loading ───────────────────────────────────────────────────

    def _load_locations(self) -> None:
        path = self._locations_yaml
        if not path:
            try:
                from ament_index_python.packages import get_package_share_directory

                pkg_dir = get_package_share_directory("omnibot_hybrid")
                path = os.path.join(pkg_dir, "config", "named_locations.yaml")
            except Exception:
                self.get_logger().warn(
                    "Could not resolve omnibot_hybrid share directory. "
                    "Set locations_yaml parameter explicitly."
                )
                return

        try:
            with open(path, "r") as f:
                data = yaml.safe_load(f)
            self._locations = data.get("locations", {}) or {}
            self.get_logger().info(
                f"Loaded {len(self._locations)} locations from {path}"
            )
        except FileNotFoundError:
            self.get_logger().warn(f"Locations file not found: {path}")
        except Exception as e:
            self.get_logger().error(f"Failed to load locations: {e}")

    # ── Graph construction ─────────────────────────────────────────────────

    def _build_langchain_agent(self) -> None:
        if not _LANGGRAPH_AVAILABLE:
            self.get_logger().fatal(
                "langgraph / langchain_core not installed — agent disabled. "
                "Run: pip install langgraph langchain-core langchain-anthropic"
            )
            self._graph = None
            return
        try:
            self._graph = build_mission_graph(ros_node=self)
            self.get_logger().info("LangGraph mission graph built successfully.")
        except ImportError as e:
            self.get_logger().fatal(
                f"Failed to build graph — missing package: {e}. "
                "Run: pip install -r requirements.txt in omnibot_orchestration/"
            )
            self._graph = None
        except Exception as e:
            self.get_logger().fatal(f"Failed to build graph: {e}")
            self._graph = None

    # ── ROS callbacks ──────────────────────────────────────────────────────

    def _on_ai_command(self, msg: String) -> None:
        if self._graph is None:
            self._publish_ai_status(
                "ERROR: Agent not initialized. Check ANTHROPIC_API_KEY and dependencies."
            )
            return

        if self._active_future and not self._active_future.done():
            self.get_logger().warn(
                "Agent is busy processing a previous command — ignoring new command. "
                f'Ignored: "{msg.data}"'
            )
            self._publish_ai_status("BUSY: Agent is already processing a command.")
            return

        self.get_logger().info(f'[AI] Received command: "{msg.data}"')
        self._publish_ai_status(f"PROCESSING: {msg.data}")
        self._active_future = self._executor.submit(self._run_agent, msg.data)

    def _on_mission_status(self, msg: String) -> None:
        with self._status_lock:
            self._latest_mission_status = msg.data

    def _on_perception_info(self, msg: String) -> None:
        import time as _time

        with self._status_lock:
            self._latest_perception = msg.data
            self._latest_perception_time = _time.time()

    def get_fresh_perception(self, max_age_s: float = 2.0) -> Optional[str]:
        """Latest /perception/object_info JSON if newer than max_age_s."""
        import time as _time

        with self._status_lock:
            if (
                self._latest_perception is not None
                and _time.time() - self._latest_perception_time <= max_age_s
            ):
                return self._latest_perception
        return None

    def _on_front_image(self, msg: "Image") -> None:
        if self._bridge is None:
            return
        try:
            cv_img = self._bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
            with self._image_lock:
                self._latest_front_image = np.array(cv_img)
        except Exception as e:
            self.get_logger().warn(
                f"Front image conversion error: {e}", throttle_duration_sec=5.0
            )

    def _on_wrist_image(self, msg: "Image") -> None:
        if self._bridge is None:
            return
        try:
            cv_img = self._bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
            with self._image_lock:
                self._latest_wrist_image = np.array(cv_img)
        except Exception as e:
            self.get_logger().warn(
                f"Wrist image conversion error: {e}", throttle_duration_sec=5.0
            )

    # ── Agent execution (runs in thread pool) ──────────────────────────────

    def _run_agent(self, text: str) -> None:
        ctx = (
            self._tracer.start_as_current_span(
                "langchain.execute_mission",
                attributes={
                    "mission.command": text,
                    "mission.command_length": len(text),
                },
            )
            if self._tracer
            else _NullSpan()
        )
        t0 = time.monotonic()
        with ctx as span:
            try:
                result = self._graph.invoke(
                    initial_state(text),
                    config={
                        "configurable": {
                            "thread_id": "omnibot",
                            "ros_node": self,
                        }
                    },
                )
                output = result.get("response") or "Mission complete."
                duration_ms = (time.monotonic() - t0) * 1000
                self.get_logger().info(
                    f"[AI] Mission result: {output} ({duration_ms:.0f} ms)"
                )
                if self._tracer and hasattr(span, "set_attribute"):
                    span.set_attribute("mission.result", "success")
                    span.set_attribute("mission.duration_ms", round(duration_ms))
                # finalize_node publishes DONE: to /ai/status directly;
                # human_checkpoint suspends the graph and publishes its question.
            except Exception as e:
                msg = f"Agent error: {e}"
                self.get_logger().error(msg)
                self._publish_ai_status(f"ERROR: {msg}")
                if self._tracer and hasattr(span, "set_attribute"):
                    span.set_attribute("mission.result", "error")
                    span.set_attribute("error.message", str(e))

    # ── Publisher helpers (called from graph nodes via ros_node reference) ─

    def _publish_mission(self, command: str) -> None:
        msg = String()
        msg.data = command
        self._mission_pub.publish(msg)
        self.get_logger().info(f'[Mission] Published: "{command}"')

    def _publish_cancel(self, reason: str = "cancel") -> None:
        msg = String()
        msg.data = reason
        self._cancel_pub.publish(msg)
        self.get_logger().info("[Mission] Cancel published.")

    def _publish_ai_status(self, status: str) -> None:
        msg = String()
        msg.data = status
        self._status_pub.publish(msg)

    # ── Lifecycle ──────────────────────────────────────────────────────────

    def destroy_node(self) -> None:
        self._executor.shutdown(wait=False)
        # Flush any pending OTEL spans before shutdown
        if _OTEL_AVAILABLE:
            provider = trace.get_tracer_provider()
            if hasattr(provider, "force_flush"):
                provider.force_flush(timeout_millis=3000)
        super().destroy_node()


class _NullSpan:
    """No-op context manager used when OpenTelemetry is not available."""

    def __enter__(self):
        return self

    def __exit__(self, *_):
        pass


def main(args=None):
    rclpy.init(args=args)
    node = LangchainAgentNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
