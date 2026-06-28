"""The ROS 2 runtime backend — rclpy timers, pub/sub, and params.

Implements the :class:`~ohho.runtime.base.Runtime` port using a real rclpy node.
The same agent/training/application code runs unchanged — ``runtime="ros2"``
gives you ROS 2 timers, DDS pub/sub, and node parameters instead of the
in-process native equivalents.

rclpy is imported lazily (it ships with ROS 2 Jazzy, not pip). A ``node_factory``
can be injected so the runtime is fully testable without a ROS 2 installation.

Pub/sub bridge: ohho's generic bus carries arbitrary Python objects; ROS 2
topics are typed. The bridge uses ``std_msgs/String`` with JSON-encoded payloads
for the generic bus, so every runtime consumer stays type-agnostic. The
:class:`~ohho.adapters.ros2.Ros2Transport` uses real message types
(``Twist``, ``Odometry``, ``JointState``) for the robot's hardware topics.
"""

from __future__ import annotations

import json
import threading
from typing import Any, Callable, Optional

from .base import Runtime, RuntimeUnavailable, TimerHandle


def _rclpy_present() -> bool:
    try:
        import importlib.util

        return importlib.util.find_spec("rclpy") is not None
    except Exception:
        return False


NodeFactory = Callable[[], Any]


class Ros2Runtime(Runtime):
    """ROS 2 runtime — rclpy node behind the Runtime port.

    Parameters:
        node_name: the rclpy node name (default ``"ohho_runtime"``).
        node_factory: injectable factory that returns an rclpy-like node.
            When ``None``, a real rclpy node is created (requires ROS 2).
    """

    name = "ros2"

    def __init__(
        self,
        node_name: str = "ohho_runtime",
        node_factory: Optional[NodeFactory] = None,
    ) -> None:
        super().__init__()
        self._node_name = node_name
        self._node_factory = node_factory
        self._node: Any = None
        self._thread: Optional[threading.Thread] = None
        self._stop = threading.Event()
        self._spun = False
        self._publishers: dict[str, Any] = {}
        self._subscriptions: dict[str, list[Any]] = {}
        self._timers: list[Any] = []
        self._timer_handles: list[TimerHandle] = []
        self._init_node()

    def _init_node(self) -> None:
        if self._node_factory is not None:
            self._node = self._node_factory()
            return
        if not _rclpy_present():
            raise RuntimeUnavailable(
                "The ROS 2 runtime requires rclpy. Source your ROS 2 (Jazzy) "
                "setup, then: pip install 'ohho-os[ros2]'. "
                "Until then use runtime='native'."
            )
        import rclpy

        rclpy.init(args=None)
        self._node = rclpy.create_node(self._node_name)

    @classmethod
    def is_available(cls) -> bool:
        """True if rclpy is importable in this environment."""
        return _rclpy_present()

    @property
    def node(self) -> Any:
        """The underlying rclpy node (for Ros2Transport to reuse)."""
        return self._node

    # ── pub/sub (overridden to bridge to ROS 2 topics) ───────────────────────
    def _get_string_msg(self):
        """Get the String message class — std_msgs if available, else a stub."""
        try:
            from std_msgs.msg import String  # type: ignore

            return String
        except ImportError:
            # Stub that matches the std_msgs/String interface (for fake nodes)
            class _StringStub:
                def __init__(self):
                    self.data = ""

            return _StringStub

    def subscribe(self, topic: str, cb: Callable[[Any], None]) -> Callable[[], None]:
        """Subscribe to a ROS 2 topic. Payloads are JSON-decoded String messages.

        Falls back to the in-process bus if the node lacks ``create_subscription``
        (e.g. a fake node that only simulates timers).
        """
        node = self._node
        create_sub = getattr(node, "create_subscription", None)
        if callable(create_sub):
            String = self._get_string_msg()

            def _on_msg(msg):
                try:
                    payload = json.loads(msg.data)
                except (json.JSONDecodeError, TypeError):
                    payload = msg.data
                try:
                    cb(payload)
                except Exception:
                    pass

            sub = create_sub(String, topic, _on_msg, 10)
            self._subscriptions.setdefault(topic, []).append(sub)

            def off() -> None:
                try:
                    self._subscriptions.get(topic, []).remove(sub)
                except ValueError:
                    pass
                destroy = getattr(node, "destroy_subscription", None)
                if callable(destroy):
                    destroy(sub)

            return off

        # Fallback: in-process bus (base class behaviour)
        return super().subscribe(topic, cb)

    def publish(self, topic: str, msg: Any) -> None:
        """Publish to a ROS 2 topic. Payload is JSON-encoded into a String.

        Falls back to the in-process bus if the node lacks ``create_publisher``.
        """
        node = self._node
        create_pub = getattr(node, "create_publisher", None)
        if callable(create_pub):
            String = self._get_string_msg()
            pub = self._publishers.get(topic)
            if pub is None:
                pub = create_pub(String, topic, 10)
                self._publishers[topic] = pub

            ros_msg = String()
            ros_msg.data = json.dumps(msg, default=str)
            pub.publish(ros_msg)
            return

        # Fallback: in-process bus
        super().publish(topic, msg)

    # ── params (overridden to use the ROS 2 node parameter API) ──────────────
    def get_param(self, key: str, default: Any = None) -> Any:
        node = self._node
        get_param = getattr(node, "get_parameter", None)
        if callable(get_param):
            try:
                param = get_param(key)
                val = getattr(param, "value", None)
                return val if val is not None else default
            except Exception:
                return default
        return super().get_param(key, default)

    def set_param(self, key: str, value: Any) -> None:
        node = self._node
        declare = getattr(node, "declare_parameter", None)
        if callable(declare):
            try:
                try:
                    declare(key, value)
                except Exception:
                    set_param = getattr(node, "set_parameters", None)
                    if callable(set_param):
                        from rclpy.parameter import Parameter  # type: ignore

                        set_param([Parameter(name=key, value=value)])
                    return
                return
            except ImportError:
                pass
        super().set_param(key, value)

    # ── lifecycle / timers ───────────────────────────────────────────────────
    def now(self) -> float:
        node = self._node
        get_clock = getattr(node, "get_clock", None)
        if callable(get_clock):
            try:
                clock = get_clock()
                return float(clock.now().nanoseconds) / 1e9
            except Exception:
                pass
        import time

        return time.monotonic()

    def create_timer(
        self, period_s: float, callback: Callable[[], None]
    ) -> TimerHandle:
        handle = TimerHandle()
        node = self._node
        create_timer = getattr(node, "create_timer", None)
        if callable(create_timer):
            try:

                def _safe_cb():
                    if not handle.cancelled:
                        try:
                            callback()
                        except Exception:
                            pass

                timer = create_timer(period_s, _safe_cb)
                self._timers.append(timer)
                self._timer_handles.append(handle)
                return handle
            except Exception:
                pass

        # Fallback: simple threading timer (for fake nodes without create_timer)
        import threading as _t

        def _thread_timer():
            while not handle.cancelled and not self._stop.is_set():
                try:
                    callback()
                except Exception:
                    pass
                self._stop.wait(period_s)

        thread = _t.Thread(target=_thread_timer, daemon=True, name="ohho-ros2-timer")
        thread.start()
        return handle

    def start(self) -> None:
        if self._thread is not None:
            return
        self._stop.clear()

        # If rclpy is real, spin in a thread. Fakes may not need spinning.
        spin = getattr(self._node, "spin", None)
        if not callable(spin):
            return

        def _spin():
            import rclpy

            while not self._stop.is_set():
                try:
                    rclpy.spin_once(self._node, timeout_sec=0.05)
                except Exception:
                    break

        self._thread = threading.Thread(
            target=_spin, name="ohho-ros2-runtime", daemon=True
        )
        self._thread.start()

    def stop(self) -> None:
        self._stop.set()
        thread = self._thread
        if thread is not None:
            thread.join(timeout=1.0)
            self._thread = None

        # Destroy timers
        node = self._node
        destroy_timer = getattr(node, "destroy_timer", None)
        for timer in self._timers:
            if callable(destroy_timer):
                try:
                    destroy_timer(timer)
                except Exception:
                    pass
        self._timers.clear()
        self._timer_handles.clear()

        # Destroy publishers and subscriptions
        destroy_pub = getattr(node, "destroy_publisher", None)
        for pub in self._publishers.values():
            if callable(destroy_pub):
                try:
                    destroy_pub(pub)
                except Exception:
                    pass
        self._publishers.clear()

        destroy_sub = getattr(node, "destroy_subscription", None)
        for subs in self._subscriptions.values():
            for sub in subs:
                if callable(destroy_sub):
                    try:
                        destroy_sub(sub)
                    except Exception:
                        pass
        self._subscriptions.clear()

        # Shutdown rclpy (only if we initialised it)
        if self._node_factory is None:
            try:
                import rclpy

                node_destroy = getattr(node, "destroy_node", None)
                if callable(node_destroy):
                    node_destroy()
                if rclpy.ok():
                    rclpy.shutdown()
            except Exception:
                pass
