#!/usr/bin/env python3

import collections
import os
import rclpy
from rclpy.node import Node
from geometry_msgs.msg import Twist, TransformStamped
from nav_msgs.msg import Odometry
from sensor_msgs.msg import Joy, Imu
from std_msgs.msg import Bool
from tf2_ros import TransformBroadcaster
import serial
import math
import numpy as np
from tf_transformations import quaternion_from_euler
import time
import struct
import traceback

_DEBUG_LOG_PATH = os.path.join(os.path.expanduser('~'), '.ros', 'omnibot_debug.log')

# Xbox controller axis/button indices (Linux xpad driver)
_AXIS_LS_X   = 0   # Left Stick horizontal  → strafe
_AXIS_LS_Y   = 1   # Left Stick vertical    → forward / back
_AXIS_RS_X   = 3   # Right Stick horizontal → car-steering (speed-coupled)
_AXIS_DPAD_X = 6   # D-Pad horizontal       → pure in-place rotation
_BTN_A       = 0   # A button               → beep
_BTN_RB      = 5   # RB (Right Bumper)      → deadman switch (btn5, matches xbox_teleop.yaml)
_BTN_START   = 7   # Start                  → turbo

# Normal / turbo speed scales
_LIN_NORMAL  = 0.15    # m/s
_LIN_TURBO   = 0.30    # m/s
_ANG_NORMAL  = 0.5     # rad/s
_ANG_TURBO   = 1.0     # rad/s
# Car-steering gain: at full forward speed + full stick → max angular rate
# gain = ang_scale / lin_scale = 2.0 rad/m (constant across normal/turbo)
_STEER_GAIN  = _ANG_NORMAL / _LIN_NORMAL

class YahboomControllerNode(Node):
    def __init__(self):
        super().__init__('yahboom_controller_node')
        
        # Declare parameters
        self.declare_parameter('wheel_radius', 0.04)
        self.declare_parameter('wheel_separation_width', 0.215)
        self.declare_parameter('wheel_separation_length', 0.165)
        self.declare_parameter('serial_port', '/dev/ttyUSB0')
        self.declare_parameter('baud_rate', 115200)
        # Set to true to log every unknown RX packet type — use this to
        # discover actual IMU packet codes sent by the Yahboom board.
        self.declare_parameter('debug_serial', False)
        # Set false when robot_localization EKF is running — the EKF owns
        # the odom→base_link TF; broadcasting it here too causes conflicts.
        self.declare_parameter('publish_tf', True)
        # Set True to publish rolling cycle-time stats to /diagnostics at 1 Hz.
        # Zero overhead when False — the guard is checked before every perf_counter call.
        self.declare_parameter('publish_diagnostics', False)

        # Get parameters
        self.wheel_radius = self.get_parameter('wheel_radius').value
        self.wheel_separation_width = self.get_parameter('wheel_separation_width').value
        self.wheel_separation_length = self.get_parameter('wheel_separation_length').value
        self.port_name = self.get_parameter('serial_port').value
        self.baud_rate = self.get_parameter('baud_rate').value
        self.debug_serial = self.get_parameter('debug_serial').value
        self.publish_tf = self.get_parameter('publish_tf').value
        self._diag_enabled = self.get_parameter('publish_diagnostics').value

        # Rolling timing accumulators (active only when _diag_enabled=True)
        self._t_cycle = collections.deque(maxlen=100)
        self._t_read = collections.deque(maxlen=100)
        self._t_pub = collections.deque(maxlen=100)
        self._t_send = collections.deque(maxlen=100)

        # Robot state
        self.x_pos = 0.0
        self.y_pos = 0.0
        self.theta = 0.0
        
        # Store latest command for throttling
        self.current_twist = Twist()

        # Emergency stop state
        self._emergency_stop = False

        self.last_beep_time = 0
        
        # Ramping State
        self.cmd_vx = 0.0
        self.cmd_vy = 0.0
        self.cmd_wa = 0.0

        # Odometry velocity (from board feedback, used by publish_odometry)
        self.current_vx = 0.0
        self.current_vy = 0.0
        self.current_vz = 0.0
        self.last_odom_time = time.time()

        # IMU state from Yahboom board serial packets
        # Packet types (RX): 0x61=accel, 0x62=gyro, 0x63=attitude (Euler)
        # NOTE: packet type codes need hardware verification if data looks wrong.
        self.imu_accel = [0.0, 0.0, 9.81]  # m/s² (z defaults to g at rest)
        self.imu_gyro  = [0.0, 0.0, 0.0]   # rad/s
        self.imu_roll  = 0.0                # rad
        self.imu_pitch = 0.0                # rad
        self.imu_yaw   = 0.0                # rad
        
        # Create publishers and subscribers
        self.cmd_vel_sub = self.create_subscription(
            Twist,
            'cmd_vel',
            self.cmd_vel_callback,
            10)
            
        self.joy_sub = self.create_subscription(Joy, 'joy', self.joy_callback, 10)

        self.create_subscription(
            Bool, '/emergency_stop', self._emergency_stop_callback, 10)

        self.odom_pub = self.create_publisher(Odometry, 'odom', 10)
        self.imu_pub  = self.create_publisher(Imu, 'imu/data', 10)
        self.tf_broadcaster = TransformBroadcaster(self)
        
        # Timer: 20Hz (0.05s)
        self.update_timer = self.create_timer(0.05, self.update_callback)
        
        # Serial Setup
        self.serial_port = None
        self.connect_serial()

        if self._diag_enabled:
            from diagnostic_msgs.msg import DiagnosticArray, DiagnosticStatus, KeyValue
            self._DiagnosticArray = DiagnosticArray
            self._DiagnosticStatus = DiagnosticStatus
            self._KeyValue = KeyValue
            self._diag_pub = self.create_publisher(
                DiagnosticArray, '/diagnostics', 10
            )
            self.create_timer(1.0, self._publish_diagnostics)

        self.get_logger().info('Yahboom controller node initialized')

    def _emergency_stop_callback(self, msg: Bool) -> None:
        if msg.data and not self._emergency_stop:
            self.get_logger().warn('EMERGENCY STOP activated — zeroing velocity.')
            self._emergency_stop = True
            self.current_twist = Twist()
            self.cmd_vx = 0.0
            self.cmd_vy = 0.0
            self.cmd_wa = 0.0
        elif not msg.data and self._emergency_stop:
            self.get_logger().info('Emergency stop cleared — resuming normal control.')
            self._emergency_stop = False

    def destroy_node(self):
        try:
            if self.serial_port and self.serial_port.is_open:
                for _ in range(3):
                    self.send_packet(0x12, struct.pack('<bhhh', 1, 0, 0, 0))
                    time.sleep(0.02)
                self.serial_port.close()
        except Exception:
            pass
        super().destroy_node()

    def log_to_file(self, msg):
        try:
            os.makedirs(os.path.dirname(_DEBUG_LOG_PATH), exist_ok=True)
            with open(_DEBUG_LOG_PATH, 'a') as f:
                f.write(f"{time.time()}: {msg}\n")
        except Exception:
            pass
    
    def connect_serial(self):
        try:
            if self.serial_port and self.serial_port.is_open:
                self.serial_port.close()
            
            self.serial_port = serial.Serial(
                port=self.port_name,
                baudrate=self.baud_rate,
                timeout=1.0)
            self.get_logger().info(f'Connected to {self.port_name}')
            
            # Stop any motion left over from a previous session
            time.sleep(0.1)
            for _ in range(3):
                self.send_packet(0x12, struct.pack('<bhhh', 1, 0, 0, 0))
                time.sleep(0.02)

            # Initialize CAR_TYPE = 1 (Mecanum X3) - must be set each session
            time.sleep(0.2)
            for _ in range(5):
                self.send_packet(0x15, struct.pack('<b', 1))
                time.sleep(0.05)
            self.get_logger().info('CAR_TYPE set to 1 (Mecanum X3)')
        except Exception as e:
            self.get_logger().error(f'Serial Connection Error: {e}')
            self.serial_port = None

    def calculate_checksum(self, data):
        return (sum(data) + 5) & 0xFF

    def send_packet(self, msg_type, payload):
        if self.serial_port is None:
            return
            
        try:
            HEAD = 0xFF
            DEVICE_ID = 0xFC
            
            # [HEAD, ID, LEN, TYPE] + Payload
            packet = [HEAD, DEVICE_ID, 0, msg_type] + list(payload)
            packet[2] = len(packet) - 1 # LEN value
            
            checksum = self.calculate_checksum(packet)
            packet.append(checksum)
            
            # Log successful packet generation for debugging
            # if msg_type == 0x12: 
            #    self.get_logger().info(f"TX: {[hex(x) for x in packet]}")
            
            self.serial_port.write(bytearray(packet))
            time.sleep(0.002) # Critical delay
            
        except Exception as e:
            self.get_logger().error(f'Tx Error (Closing): {e}')
            if self.serial_port:
                self.serial_port.close()
            self.serial_port = None

    def joy_callback(self, msg):
        try:
            def btn(i):
                return len(msg.buttons) > i and msg.buttons[i] == 1
            def axis(i):
                return float(msg.axes[i]) if len(msg.axes) > i else 0.0

            # Button A → beep (debounced to 1 per 0.5 s)
            now = time.time()
            if btn(_BTN_A):
                if (now - self.last_beep_time) > 0.5:
                    self.get_logger().info('Button A: BEEP')
                    self.send_packet(0x02, struct.pack('<h', 100))
                    self.last_beep_time = now

            # Deadman: RB (btn5) must be held — release zeroes velocity immediately
            if not btn(_BTN_RB):
                self.current_twist = Twist()
                return

            turbo      = btn(_BTN_START)
            lin_scale  = _LIN_TURBO  if turbo else _LIN_NORMAL
            ang_scale  = _ANG_TURBO  if turbo else _ANG_NORMAL

            # Left stick → forward/back (vx) and strafe (vy)
            vx = axis(_AXIS_LS_Y) * lin_scale
            vy = axis(_AXIS_LS_X) * lin_scale

            # D-pad ←→ → pure in-place rotation (discrete ±1)
            wz = axis(_AXIS_DPAD_X) * ang_scale

            # Right stick ←→ → car-like steering
            wz += axis(_AXIS_RS_X) * vx * _STEER_GAIN

            twist = Twist()
            twist.linear.x  = vx
            twist.linear.y  = vy
            twist.angular.z = wz
            self.current_twist = twist

        except Exception as e:
            self.get_logger().error(f'Joy Error: {e}')


    def cmd_vel_callback(self, msg):
        # Just store the command. Do NOT send to serial here.
        # This prevents flooding the serial bus if joy publisher is fast.
        self.current_twist = msg

    def send_motion_command(self):
        if self._emergency_stop:
            # Keep sending zero velocity while e-stop is active
            self.send_packet(0x12, struct.pack('<bhhh', 1, 0, 0, 0))
            return
        try:
            msg = self.current_twist
            
            # CLAMP SPEED to prevent Brownout/Over-current - SAFE MODE
            MAX_VAL = 0.2   # m/s
            MAX_ANG = 1.0   # rad/s
            RAMP_STEP     = 0.025  # m/s per tick — halved for smoother linear accel
            RAMP_STEP_ANG = 0.05   # rad/s per tick — smooth angular start/stop

            target_vx = np.clip(msg.linear.x,  -MAX_VAL, MAX_VAL)
            target_vy = np.clip(msg.linear.y,  -MAX_VAL, MAX_VAL)
            target_w  = np.clip(msg.angular.z, -MAX_ANG, MAX_ANG)

            # Ramping Logic — linear
            if self.cmd_vx < target_vx:
                self.cmd_vx = min(self.cmd_vx + RAMP_STEP, target_vx)
            elif self.cmd_vx > target_vx:
                self.cmd_vx = max(self.cmd_vx - RAMP_STEP, target_vx)

            if self.cmd_vy < target_vy:
                self.cmd_vy = min(self.cmd_vy + RAMP_STEP, target_vy)
            elif self.cmd_vy > target_vy:
                self.cmd_vy = max(self.cmd_vy - RAMP_STEP, target_vy)

            # Ramping Logic — angular (previously instant, now smoothed)
            if self.cmd_wa < target_w:
                self.cmd_wa = min(self.cmd_wa + RAMP_STEP_ANG, target_w)
            elif self.cmd_wa > target_w:
                self.cmd_wa = max(self.cmd_wa - RAMP_STEP_ANG, target_w)
            
            # Send onboard Mecanum kinematics command (0x12)
            # Board computes wheel speeds internally using CAR_TYPE=1 algorithm
            vx_int = int(self.cmd_vx * 1000)  # mm/s
            vy_int = int(self.cmd_vy * 1000)
            w_int  = int(self.cmd_wa * 1000)
            
            CAR_TYPE = 1  # Mecanum X3
            payload = struct.pack('<bhhh', CAR_TYPE, vx_int, vy_int, w_int)
            self.send_packet(0x12, payload)
            
        except Exception as e:
            self.get_logger().error(f'CmdVel Error: {e}')
            self.log_to_file(f'CmdVel Error: {e}')
            self.log_to_file(traceback.format_exc())

    def update_callback(self):
        _t0 = time.perf_counter() if self._diag_enabled else None
        try:
            if self.serial_port is None:
                self.connect_serial()
                return

            # 1. Read Odom
            self.read_yahboom_odometry()
            _t1 = time.perf_counter() if self._diag_enabled else None

            # 2. Publish Odom + IMU
            now = self.get_clock().now()
            self.publish_odometry(now)
            self.publish_imu(now)
            _t2 = time.perf_counter() if self._diag_enabled else None

            # 3. Send Motor Command (Throttled to 10Hz)
            self.send_motion_command()
            _t3 = time.perf_counter() if self._diag_enabled else None

            if self._diag_enabled and _t0 is not None:
                ms = lambda a, b: (b - a) * 1000.0
                self._t_cycle.append(ms(_t0, _t3))
                self._t_read.append(ms(_t0, _t1))
                self._t_pub.append(ms(_t1, _t2))
                self._t_send.append(ms(_t2, _t3))
        except Exception as e:
            self.get_logger().error(f'Update Error: {e}')
            self.log_to_file(f'Update Error: {e}')

    def read_yahboom_odometry(self):
        if self.serial_port is None:
            return
        try:
            waiting = self.serial_port.in_waiting
            if waiting == 0:
                return

            data = self.serial_port.read(waiting)

            # RX packet format: [0xFF, 0xFB, LEN, TYPE, PAYLOAD..., CS]
            # Drain the serial buffer and parse IMU packets only.
            # NOTE: 0x0C velocity feedback is NOT used for odometry — diagnostic
            # tests confirmed it reports unsigned magnitude (backward motion reads
            # as positive vx), making it useless for directional integration.
            # Odometry is computed from commanded velocity below instead.
            idx = 0
            while idx < len(data) - 2:
                if data[idx] != 0xFF or data[idx + 1] != 0xFB:
                    idx += 1
                    continue
                if idx + 3 > len(data):
                    break
                length = data[idx + 2]
                total = 2 + length
                if idx + total > len(data):
                    break
                pkt = data[idx:idx + total]
                pkt_type = pkt[3]
                payload = pkt[4:-1]
                if pkt_type == 0x61 and len(payload) >= 6:
                    ax, ay, az = struct.unpack_from('<hhh', payload, 0)
                    self.imu_accel = [ax / 1000.0 * 9.81,
                                      ay / 1000.0 * 9.81,
                                      az / 1000.0 * 9.81]
                elif pkt_type == 0x62 and len(payload) >= 6:
                    gx, gy, gz = struct.unpack_from('<hhh', payload, 0)
                    self.imu_gyro = [gx / 1000.0, gy / 1000.0, gz / 1000.0]
                elif pkt_type == 0x63 and len(payload) >= 6:
                    r, p, y = struct.unpack_from('<hhh', payload, 0)
                    self.imu_roll  = r / 100.0 * math.pi / 180.0
                    self.imu_pitch = p / 100.0 * math.pi / 180.0
                    self.imu_yaw   = y / 100.0 * math.pi / 180.0
                elif self.debug_serial and pkt_type != 0x0C:
                    self.get_logger().info(
                        f'[debug_serial] unknown pkt type=0x{pkt_type:02X} '
                        f'payload={[hex(b) for b in payload]}'
                    )
                idx += total

            # Integrate ramped commanded velocity into pose.
            # cmd_vx/vy/wa are the values actually sent to the board this tick,
            # so this dead-reckons exactly what was commanded — correct sign,
            # zero when stopped (no drift), accurate during motion.
            now = time.time()
            dt = now - self.last_odom_time
            self.last_odom_time = now
            if dt > 0.5:
                return

            vx = self.cmd_vx
            vy = self.cmd_vy
            vz = self.cmd_wa

            cos_th = math.cos(self.theta)
            sin_th = math.sin(self.theta)
            self.x_pos += (vx * cos_th - vy * sin_th) * dt
            self.y_pos += (vx * sin_th + vy * cos_th) * dt
            self.theta += vz * dt
            self.theta = math.atan2(math.sin(self.theta), math.cos(self.theta))

            self.current_vx = vx
            self.current_vy = vy
            self.current_vz = vz

        except Exception as e:
            self.get_logger().error(f'Rx Error: {e}')
            # Do not close connection on RX error


    def publish_odometry(self, stamp):
        try:
            q = quaternion_from_euler(0, 0, self.theta)

            ts = TransformStamped()
            ts.header.stamp = stamp.to_msg()
            ts.header.frame_id = 'odom'
            ts.child_frame_id = 'base_link'
            ts.transform.translation.x = self.x_pos
            ts.transform.translation.y = self.y_pos
            ts.transform.translation.z = 0.0
            ts.transform.rotation.x = q[0]
            ts.transform.rotation.y = q[1]
            ts.transform.rotation.z = q[2]
            ts.transform.rotation.w = q[3]
            if self.publish_tf:
                self.tf_broadcaster.sendTransform(ts)

            odom = Odometry()
            odom.header.stamp = stamp.to_msg()
            odom.header.frame_id = 'odom'
            odom.child_frame_id = 'base_link'
            odom.pose.pose.position.x = self.x_pos
            odom.pose.pose.position.y = self.y_pos
            odom.pose.pose.position.z = 0.0
            odom.pose.pose.orientation.x = q[0]
            odom.pose.pose.orientation.y = q[1]
            odom.pose.pose.orientation.z = q[2]
            odom.pose.pose.orientation.w = q[3]
            odom.twist.twist.linear.x  = self.current_vx
            odom.twist.twist.linear.y  = self.current_vy
            odom.twist.twist.angular.z = self.current_vz

            # Pose covariance (row-major 6×6: x,y,z,roll,pitch,yaw).
            # Dead-reckoning from commanded velocity accumulates error over
            # time; these values are intentionally conservative so the EKF
            # down-weights the wheel odometry relative to the IMU orientation.
            odom.pose.covariance[0]  = 0.05   # x
            odom.pose.covariance[7]  = 0.05   # y
            odom.pose.covariance[35] = 0.1    # yaw

            # Twist covariance — commanded velocity is accurate at the start
            # of a move but drifts; moderate uncertainty.
            odom.twist.covariance[0]  = 0.01  # vx
            odom.twist.covariance[7]  = 0.01  # vy
            odom.twist.covariance[35] = 0.05  # vz (angular)

            self.odom_pub.publish(odom)
        except Exception as e:
            self.get_logger().error(f'Pub Error: {e}')

    def publish_imu(self, stamp):
        try:
            q = quaternion_from_euler(self.imu_roll, self.imu_pitch, self.imu_yaw)
            msg = Imu()
            msg.header.stamp = stamp.to_msg()
            msg.header.frame_id = 'imu_link'

            msg.orientation.x = q[0]
            msg.orientation.y = q[1]
            msg.orientation.z = q[2]
            msg.orientation.w = q[3]
            # Diagonal covariance (rad²)
            msg.orientation_covariance[0] = 0.01
            msg.orientation_covariance[4] = 0.01
            msg.orientation_covariance[8] = 0.01

            msg.angular_velocity.x = self.imu_gyro[0]
            msg.angular_velocity.y = self.imu_gyro[1]
            msg.angular_velocity.z = self.imu_gyro[2]
            msg.angular_velocity_covariance[0] = 0.001
            msg.angular_velocity_covariance[4] = 0.001
            msg.angular_velocity_covariance[8] = 0.001

            msg.linear_acceleration.x = self.imu_accel[0]
            msg.linear_acceleration.y = self.imu_accel[1]
            msg.linear_acceleration.z = self.imu_accel[2]
            msg.linear_acceleration_covariance[0] = 0.1
            msg.linear_acceleration_covariance[4] = 0.1
            msg.linear_acceleration_covariance[8] = 0.1

            self.imu_pub.publish(msg)
        except Exception as e:
            self.get_logger().error(f'IMU Pub Error: {e}')

    def _rolling_stats(self, deque_: collections.deque) -> dict:
        if not deque_:
            return {}
        s = sorted(deque_)
        n = len(s)
        import statistics as _st
        return {
            "mean": _st.mean(s),
            "p50": s[n // 2],
            "p95": s[max(0, int(0.95 * n) - 1)],
            "max": s[-1],
        }

    def _publish_diagnostics(self) -> None:
        msg = self._DiagnosticArray()
        msg.header.stamp = self.get_clock().now().to_msg()

        def _make_status(name, deque_):
            st = self._DiagnosticStatus()
            st.name = name
            stats = self._rolling_stats(deque_)
            if not stats:
                st.level = self._DiagnosticStatus.OK
                st.message = "no data yet"
                return st
            p95 = stats["p95"]
            st.level = (
                self._DiagnosticStatus.ERROR if p95 > 50.0
                else self._DiagnosticStatus.WARN if p95 > 20.0
                else self._DiagnosticStatus.OK
            )
            st.message = f"p95={p95:.2f}ms"
            for k, v in stats.items():
                kv = self._KeyValue()
                kv.key = k
                kv.value = f"{v:.3f}ms"
                st.values.append(kv)
            return st

        msg.status = [
            _make_status("yahboom/cycle_total_ms", self._t_cycle),
            _make_status("yahboom/read_odom_ms", self._t_read),
            _make_status("yahboom/publish_ms", self._t_pub),
            _make_status("yahboom/send_motion_ms", self._t_send),
        ]
        self._diag_pub.publish(msg)


def main(args=None):
    rclpy.init(args=args)
    try:
        node = YahboomControllerNode()
        rclpy.spin(node)
    except Exception as e:
        print(f"CRITICAL NODE FAILURE: {e}")
        traceback.print_exc()
    finally:
        if 'node' in locals():
            node.destroy_node()
        rclpy.shutdown()

if __name__ == '__main__':
    main()
