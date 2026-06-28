"""Tests for the ROS 2 runtime + transport — no rclpy required.

A ``FakeRos2Node`` simulates the rclpy node interface (create_publisher,
create_subscription, create_timer, get_clock, declare_parameter, etc.) so the
full Ros2Runtime + Ros2Transport chain is testable on any OS without ROS 2.
"""

import json
import math
import time
import unittest

from ohho.adapters.ros2 import Ros2Transport
from ohho.adapters import AdapterUnavailable, resolve_transport
from ohho.registry import get_spec
from ohho.runtime.ros2 import Ros2Runtime
from ohho.runtime.base import RuntimeUnavailable
from ohho.schema import Velocity


# ── Fake ROS 2 node + message types ───────────────────────────────────────────


class FakeVector3:
    def __init__(self, x=0.0, y=0.0, z=0.0, w=1.0):
        self.x = float(x)
        self.y = float(y)
        self.z = float(z)
        self.w = float(w)


class FakeTwist:
    def __init__(self):
        self.linear = FakeVector3()
        self.angular = FakeVector3()


class FakePose:
    def __init__(self):
        self.position = FakeVector3()
        self.orientation = FakeVector3(w=1.0)


class FakePoseStamped:
    def __init__(self):
        self.pose = FakePose()


class FakeTwistBody:
    def __init__(self, vx=0.0, vy=0.0, omega=0.0):
        self.linear = FakeVector3(vx, vy, 0.0)
        self.angular = FakeVector3(0.0, 0.0, omega)


class FakeTwistStamped:
    def __init__(self, vx=0.0, vy=0.0, omega=0.0):
        self.twist = FakeTwistBody(vx, vy, omega)


class FakeOdometry:
    def __init__(self, x=0.0, y=0.0, theta=0.0, vx=0.0, vy=0.0, omega=0.0):
        self.pose = FakePoseStamped()
        self.twist = FakeTwistStamped(vx, vy, omega)
        self.pose.pose.position.x = x
        self.pose.pose.position.y = y
        # Encode yaw as quaternion (z, w)
        self.pose.pose.orientation.z = math.sin(theta / 2)
        self.pose.pose.orientation.w = math.cos(theta / 2)


class FakeJointState:
    def __init__(self, names=None, positions=None):
        self.name = names or []
        self.position = positions or []


class FakeString:
    def __init__(self, data=""):
        self.data = data


class FakePublisher:
    """Records published messages for inspection in tests."""

    def __init__(self, msg_type, topic):
        self.msg_type = msg_type
        self.topic = topic
        self.published = []

    def publish(self, msg):
        self.published.append(msg)


class FakeSubscription:
    def __init__(self, topic, callback, msg_type):
        self.topic = topic
        self.callback = callback
        self.msg_type = msg_type


class FakeTimer:
    def __init__(self):
        self.callback = None
        self.cancelled = False

    def cancel(self):
        self.cancelled = True


class FakeClock:
    class Time:
        nanoseconds = 0

    def now(self):
        t = FakeClock.Time()
        t.nanoseconds = int(time.monotonic() * 1e9)
        return t


class FakeParameter:
    def __init__(self, name, value):
        self.name = name
        self.value = value


class FakeRos2Node:
    """Simulates an rclpy Node for testing — no ROS 2 install needed."""

    def __init__(self, name="ohho_test"):
        self.name = name
        self._publishers: dict[str, FakePublisher] = {}
        self._subscriptions: list[FakeSubscription] = []
        self._timers: list[FakeTimer] = []
        self._params: dict[str, Any] = {}

    def create_publisher(self, msg_type, topic, qos):
        pub = FakePublisher(msg_type, topic)
        self._publishers[topic] = pub
        return pub

    def create_subscription(self, msg_type, topic, callback, qos):
        sub = FakeSubscription(topic, callback, msg_type)
        self._subscriptions.append(sub)
        return sub

    def create_timer(self, period_s, callback):
        timer = FakeTimer()
        timer.callback = callback
        timer.period = period_s
        self._timers.append(timer)
        # Fire once immediately for test simplicity
        callback()
        return timer

    def get_clock(self):
        return FakeClock()

    def declare_parameter(self, name, value):
        self._params[name] = value
        return FakeParameter(name, value)

    def get_parameter(self, name):
        if name in self._params:
            return FakeParameter(name, self._params[name])
        raise KeyError(name)

    def destroy_publisher(self, pub):
        pass

    def destroy_subscription(self, sub):
        pass

    def destroy_timer(self, timer):
        pass

    def destroy_node(self):
        pass

    def spin(self):
        pass

    # Test helper: simulate an incoming message on a subscription
    def inject_message(self, topic, msg):
        for sub in self._subscriptions:
            if sub.topic == topic:
                sub.callback(msg)
                return True
        return False

    def get_publisher(self, topic):
        return self._publishers.get(topic)


# Make `Any` available for FakeRos2Node typing
from typing import Any


# ── Tests ─────────────────────────────────────────────────────────────────────


class TestRos2Runtime(unittest.TestCase):
    def _runtime(self):
        return Ros2Runtime(node_factory=lambda: FakeRos2Node("test"))

    def test_create_runtime_with_fake_node(self):
        rt = self._runtime()
        self.assertEqual(rt.name, "ros2")
        self.assertIsNotNone(rt.node)

    def test_timer_fires(self):
        rt = self._runtime()
        hits = []
        rt.create_timer(0.01, lambda: hits.append(1))
        # FakeRos2Node.create_timer fires immediately
        self.assertGreaterEqual(len(hits), 1)

    def test_pubsub_uses_ros2_topics(self):
        rt = self._runtime()
        node = rt.node
        got = []
        off = rt.subscribe("/test_topic", got.append)
        rt.publish("/test_topic", {"key": "value"})
        # FakePublisher records the String message
        pub = node.get_publisher("/test_topic")
        self.assertIsNotNone(pub)
        self.assertEqual(len(pub.published), 1)
        # The payload is JSON-encoded in a String
        msg = pub.published[0]
        self.assertEqual(json.loads(msg.data), {"key": "value"})
        off()

    def test_params_use_node(self):
        rt = self._runtime()
        rt.set_param("speed", 0.5)
        self.assertEqual(rt.get_param("speed"), 0.5)

    def test_now_returns_float(self):
        rt = self._runtime()
        t = rt.now()
        self.assertIsInstance(t, float)
        self.assertGreater(t, 0)

    def test_start_stop_with_fake_node(self):
        rt = self._runtime()
        rt.start()  # fake node has no spin() that blocks → no-op
        rt.stop()  # should not raise

    def test_without_rclpy_raises(self):
        # When node_factory is None and rclpy isn't installed:
        with self.assertRaises(RuntimeUnavailable):
            Ros2Runtime(node_factory=None)


class FakeMsgFactory:
    """Provides fake ROS 2 message types for testing."""

    Twist = FakeTwist
    Vector3 = FakeVector3
    Odometry = FakeOdometry
    JointState = FakeJointState


class TestRos2Transport(unittest.TestCase):
    def _make(self, robot_id="omnibot"):
        spec = get_spec(robot_id)
        node = FakeRos2Node("test")
        rt = Ros2Runtime(node_factory=lambda: node)
        tp = Ros2Transport(spec, "", node=node, msg_factory=FakeMsgFactory)
        return tp, node, rt

    def test_connect_creates_publishers(self):
        tp, node, _ = self._make()
        tp.connect()
        self.assertIsNotNone(node.get_publisher("/cmd_vel"))
        self.assertIsNotNone(node.get_publisher("/arm/joint_commands"))

    def test_send_velocity_publishes_twist(self):
        tp, node, _ = self._make()
        tp.connect()
        tp.send_velocity(Velocity(0.1, 0.0, 0.2))
        pub = node.get_publisher("/cmd_vel")
        self.assertEqual(len(pub.published), 1)
        twist = pub.published[0]
        self.assertAlmostEqual(twist.linear.x, 0.1)
        self.assertAlmostEqual(twist.angular.z, 0.2)

    def test_send_joint_command_publishes_joint_state(self):
        tp, node, _ = self._make()
        tp.connect()
        tp.send_joint_command("arm_shoulder_pan", 0.5)
        pub = node.get_publisher("/arm/joint_commands")
        self.assertEqual(len(pub.published), 1)
        js = pub.published[0]
        self.assertEqual(js.name, ["arm_shoulder_pan"])
        self.assertAlmostEqual(js.position[0], 0.5)

    def test_odometry_subscription(self):
        tp, node, _ = self._make()
        tp.connect()
        # Simulate /odom message
        odom_msg = FakeOdometry(x=1.0, y=2.0, theta=0.5, vx=0.1, vy=0.0, omega=0.2)
        node.inject_message("/odom", odom_msg)
        t = tp.read()
        self.assertAlmostEqual(t.odom.x, 1.0)
        self.assertAlmostEqual(t.odom.y, 2.0)
        self.assertAlmostEqual(t.odom.theta, 0.5, places=3)
        self.assertAlmostEqual(t.odom.vx, 0.1)
        self.assertAlmostEqual(t.odom.omega, 0.2)

    def test_joint_states_subscription(self):
        tp, node, _ = self._make()
        tp.connect()
        js_msg = FakeJointState(
            names=["arm_shoulder_pan", "arm_gripper"],
            positions=[0.3, 0.1],
        )
        node.inject_message("/arm/joint_states", js_msg)
        t = tp.read()
        self.assertEqual(len(t.joints), 2)
        self.assertAlmostEqual(t.joints[0].position, 0.3)

    def test_emergency_stop_blocks_velocity(self):
        tp, node, _ = self._make()
        tp.connect()
        tp.emergency_stop()
        node.get_publisher("/cmd_vel").published.clear()
        tp.send_velocity(Velocity(0.1, 0.0, 0.0))
        self.assertEqual(len(node.get_publisher("/cmd_vel").published), 0)

    def test_estop_sends_zero_twist(self):
        tp, node, _ = self._make()
        tp.connect()
        node.get_publisher("/cmd_vel").published.clear()
        tp.emergency_stop()
        pub = node.get_publisher("/cmd_vel")
        self.assertEqual(len(pub.published), 1)
        twist = pub.published[0]
        self.assertAlmostEqual(twist.linear.x, 0.0)
        self.assertAlmostEqual(twist.angular.z, 0.0)

    def test_namespace_prefix(self):
        spec = get_spec("omnibot")
        node = FakeRos2Node("test")
        tp = Ros2Transport(spec, "/robot1", node=node, msg_factory=FakeMsgFactory)
        tp.connect()
        self.assertIsNotNone(node.get_publisher("/robot1/cmd_vel"))

    def test_go2_has_no_arm_publisher(self):
        tp, node, _ = self._make("unitree-go2")
        tp.connect()
        self.assertIsNotNone(node.get_publisher("/cmd_vel"))
        self.assertIsNone(node.get_publisher("/arm/joint_commands"))

    def test_telemetry_callback_fires_on_odom(self):
        tp, node, _ = self._make()
        seen = []
        tp.on_telemetry(seen.append)
        tp.connect()
        node.inject_message("/odom", FakeOdometry(x=1.0))
        self.assertGreaterEqual(len(seen), 1)

    def test_without_node_raises(self):
        tp = Ros2Transport(get_spec("omnibot"), "", msg_factory=FakeMsgFactory)
        with self.assertRaises(AdapterUnavailable):
            tp.connect()


class TestResolveRos2Transport(unittest.TestCase):
    def test_ros2_scheme_resolves(self):
        # resolve_transport with ros2:// needs a runtime with a node

        node = FakeRos2Node("test")
        rt = Ros2Runtime(node_factory=lambda: node)
        tp = resolve_transport("ros2://", get_spec("omnibot"), runtime=rt)
        self.assertEqual(tp.protocol, "ros2")

    def test_ros2_scheme_without_runtime_raises(self):

        tp = resolve_transport("ros2://", get_spec("omnibot"))
        # connect() should raise because there's no node (runtime wasn't passed)
        with self.assertRaises(AdapterUnavailable):
            tp.connect()


class TestAutoRuntimeSelection(unittest.TestCase):
    """auto should prefer ros2 when available, native otherwise."""

    def test_auto_prefers_native_without_rclpy(self):
        from ohho.runtime import get_runtime, NativeRuntime

        # On Windows (no rclpy), auto should pick native
        rt = get_runtime("auto")
        self.assertIsInstance(rt, NativeRuntime)


if __name__ == "__main__":
    unittest.main()
