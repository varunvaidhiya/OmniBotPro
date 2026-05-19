#!/usr/bin/env python3
"""
VR Recording Bridge Node

Bridges /vr/record_start and /vr/record_stop to the teleop_recorder_node
by republishing as joy button events, and provides an HTTP server to
receive JSONL episode uploads from the VR headset.

Topics consumed (from VR headset via ROSBridge):
  /vr/record_start  (std_msgs/String) — episode name
  /vr/record_stop   (std_msgs/Bool)   — True = save, False = discard

Topics mirrored for VR monitoring:
  /arm/joint_states  → /vr/obs  (JSON string)
  /odom              → /vr/obs
  /cmd_vel/teleop    → /vr/obs

HTTP API (aiohttp, port 8765 by default):
  GET  /health           → 200 OK
  POST /upload_episode   → multipart/form-data with 'file' field (JSONL)
"""

import asyncio
import json
import os
import threading
import time
from pathlib import Path

import rclpy
from rclpy.node import Node
from rclpy.qos import QoSProfile, ReliabilityPolicy

from std_msgs.msg import String, Bool
from sensor_msgs.msg import JointState
from nav_msgs.msg import Odometry
from geometry_msgs.msg import Twist

try:
    import aiohttp
    from aiohttp import web
    _AIOHTTP_AVAILABLE = True
except ImportError:
    _AIOHTTP_AVAILABLE = False


class VRRecordingBridgeNode(Node):
    """
    Bridges VR recording signals and provides an HTTP upload endpoint for
    JSONL episode files exported from the VR headset.
    """

    def __init__(self):
        super().__init__('vr_recording_bridge')

        # ── Declare parameters ───────────────────────────────────────────────
        self.declare_parameter('upload_dir', '~/datasets/vr_episodes')
        self.declare_parameter('http_port',  8765)

        upload_dir_raw = self.get_parameter('upload_dir').get_parameter_value().string_value
        self._upload_dir = Path(upload_dir_raw).expanduser()
        self._http_port  = self.get_parameter('http_port').get_parameter_value().integer_value

        # Ensure upload directory exists
        self._upload_dir.mkdir(parents=True, exist_ok=True)
        self.get_logger().info(f'Episode upload directory: {self._upload_dir}')

        # ── QoS ───────────────────────────────────────────────────────────────
        qos = QoSProfile(depth=10)
        reliable_qos = QoSProfile(
            depth=10,
            reliability=ReliabilityPolicy.RELIABLE,
        )

        # ── Subscribers — VR signals ─────────────────────────────────────────
        self._sub_record_start = self.create_subscription(
            String, '/vr/record_start',
            self._on_record_start, reliable_qos)

        self._sub_record_stop = self.create_subscription(
            Bool, '/vr/record_stop',
            self._on_record_stop, reliable_qos)

        # ── Subscribers — robot observations (for /vr/obs mirror) ───────────
        self._sub_joint_states = self.create_subscription(
            JointState, '/arm/joint_states',
            self._on_joint_states, qos)

        self._sub_odom = self.create_subscription(
            Odometry, '/odom',
            self._on_odom, qos)

        self._sub_cmd_vel = self.create_subscription(
            Twist, '/cmd_vel/teleop',
            self._on_cmd_vel, qos)

        # ── Publisher — record trigger (republish start signal) ──────────────
        # Publishes the episode name so other nodes (e.g. teleop_recorder_node)
        # can pick up recording events.
        self._pub_record_trigger = self.create_publisher(
            String, '/vr/record_trigger', reliable_qos)

        # ── Publisher — /vr/obs (JSON mirror for monitoring tools) ───────────
        self._pub_vr_obs = self.create_publisher(String, '/vr/obs', qos)

        # ── Cached state ──────────────────────────────────────────────────────
        self._latest_joint_states: dict = {}
        self._latest_odom: dict         = {}
        self._latest_cmd_vel: dict      = {}

        self._recording_active = False
        self._current_episode  = ''

        # ── Start aiohttp server in background thread ─────────────────────────
        if _AIOHTTP_AVAILABLE:
            self._http_thread = threading.Thread(
                target=self._run_http_server, daemon=True)
            self._http_thread.start()
            self.get_logger().info(
                f'HTTP episode upload server starting on port {self._http_port}')
        else:
            self.get_logger().warn(
                'aiohttp not installed — HTTP upload server disabled. '
                'Install with: pip install aiohttp')

        self.get_logger().info('VRRecordingBridgeNode ready.')

    # ── VR recording signal handlers ─────────────────────────────────────────

    def _on_record_start(self, msg: String):
        episode_name = msg.data.strip() if msg.data else ''
        if not episode_name:
            episode_name = f'ep_{int(time.time())}'

        self._recording_active = True
        self._current_episode  = episode_name

        self.get_logger().info(f'Recording START: episode="{episode_name}"')

        # Republish as trigger so teleop_recorder_node or other nodes can react
        trigger = String()
        trigger.data = episode_name
        self._pub_record_trigger.publish(trigger)

    def _on_record_stop(self, msg: Bool):
        save = msg.data
        episode = self._current_episode

        if not self._recording_active:
            self.get_logger().warn('Received /vr/record_stop but not recording — ignoring.')
            return

        self._recording_active = False

        if save:
            self.get_logger().info(f'Recording STOP (save): episode="{episode}"')
        else:
            self.get_logger().info(f'Recording STOP (discard): episode="{episode}"')

        self._current_episode = ''

    # ── Robot observation subscribers ────────────────────────────────────────

    def _on_joint_states(self, msg: JointState):
        self._latest_joint_states = {
            'name':     list(msg.name),
            'position': list(msg.position),
            'velocity': list(msg.velocity),
            'effort':   list(msg.effort),
        }
        self._publish_vr_obs()

    def _on_odom(self, msg: Odometry):
        p = msg.pose.pose.position
        q = msg.pose.pose.orientation
        v = msg.twist.twist.linear
        w = msg.twist.twist.angular
        self._latest_odom = {
            'pos':     {'x': p.x, 'y': p.y, 'z': p.z},
            'orient':  {'x': q.x, 'y': q.y, 'z': q.z, 'w': q.w},
            'lin_vel': {'x': v.x, 'y': v.y, 'z': v.z},
            'ang_vel': {'x': w.x, 'y': w.y, 'z': w.z},
        }

    def _on_cmd_vel(self, msg: Twist):
        self._latest_cmd_vel = {
            'linear':  {'x': msg.linear.x,  'y': msg.linear.y,  'z': msg.linear.z},
            'angular': {'x': msg.angular.x, 'y': msg.angular.y, 'z': msg.angular.z},
        }

    def _publish_vr_obs(self):
        """Publishes a merged observation snapshot to /vr/obs (JSON string)."""
        obs = {
            't':          time.time(),
            'joint':      self._latest_joint_states,
            'odom':       self._latest_odom,
            'cmd_vel':    self._latest_cmd_vel,
            'recording':  self._recording_active,
            'episode':    self._current_episode,
        }
        out = String()
        out.data = json.dumps(obs, separators=(',', ':'))
        self._pub_vr_obs.publish(out)

    # ── HTTP server (aiohttp) ─────────────────────────────────────────────────

    def _run_http_server(self):
        """Runs the aiohttp web server in its own asyncio event loop."""
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        app = web.Application(client_max_size=100 * 1024 * 1024)  # 100 MB max upload
        app.router.add_get('/health',          self._handle_health)
        app.router.add_post('/upload_episode', self._handle_upload)

        runner = web.AppRunner(app)
        loop.run_until_complete(runner.setup())
        site = web.TCPSite(runner, '0.0.0.0', self._http_port)
        loop.run_until_complete(site.start())

        self.get_logger().info(
            f'HTTP server listening on http://0.0.0.0:{self._http_port}')
        loop.run_forever()

    async def _handle_health(self, request: 'web.Request') -> 'web.Response':
        """GET /health → 200 OK"""
        return web.Response(text='ok', status=200)

    async def _handle_upload(self, request: 'web.Request') -> 'web.Response':
        """
        POST /upload_episode
        Receives a multipart/form-data request with a 'file' field containing
        a JSONL episode file. Saves it to the upload_dir.
        """
        try:
            reader = await request.multipart()
        except Exception as exc:
            self.get_logger().error(f'Upload: failed to read multipart: {exc}')
            return web.Response(text=f'Bad request: {exc}', status=400)

        saved_files = []

        async for part in reader:
            if part.name != 'file':
                # Drain and skip unknown fields
                await part.read()
                continue

            filename = part.filename or f'episode_{int(time.time())}.jsonl'
            # Sanitise filename: keep only safe characters
            safe_name = ''.join(c for c in filename if c.isalnum() or c in ('_', '-', '.'))
            if not safe_name.endswith('.jsonl'):
                safe_name += '.jsonl'

            dest_path = self._upload_dir / safe_name

            data = await part.read()
            if not data:
                self.get_logger().warn(f'Upload: empty file received for {safe_name}')
                continue

            dest_path.write_bytes(data)

            # Count frames (lines) for logging
            try:
                line_count = data.count(b'\n')
                self.get_logger().info(
                    f'Upload: saved {safe_name} '
                    f'({len(data)} bytes, ~{line_count} frames) '
                    f'→ {dest_path}')
            except Exception:
                self.get_logger().info(f'Upload: saved {safe_name} ({len(data)} bytes)')

            saved_files.append(safe_name)

        if saved_files:
            return web.json_response({'status': 'ok', 'saved': saved_files})
        else:
            return web.Response(text='No file part found in request.', status=400)


def main(args=None):
    rclpy.init(args=args)
    node = VRRecordingBridgeNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        if rclpy.ok():
            rclpy.shutdown()


if __name__ == '__main__':
    main()
