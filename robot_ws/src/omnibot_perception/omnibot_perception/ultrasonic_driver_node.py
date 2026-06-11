#!/usr/bin/env python3
"""
HC-SR04 ultrasonic distance sensor driver for Raspberry Pi 5.

Raspberry Pi 5 uses the RP1 I/O controller — RPi.GPIO and pigpio do not
work.  This driver uses the standard Linux kernel sysfs / chardev interface
via the `gpiod` Python bindings (python3-gpiod, available in Ubuntu 24.04).

Wiring (3.3 V logic level — Pi 5 is NOT 5 V tolerant):
  TRIG → GPIO pin declared in 'trig_pin'   (output)
  ECHO → GPIO pin declared in 'echo_pin'   (input, 5 V → 3.3 V via voltage divider)

Publishes:
  /sensors/ultrasonic/range   sensor_msgs/Range   (ULTRASOUND, 2 cm – 4 m)

Set 'simulate' to True to run without hardware (emits a sine-wave distance).
"""

import math
import time
import threading

import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Range

try:
    import gpiod

    _GPIOD_OK = True
except ImportError:
    _GPIOD_OK = False


class UltrasonicDriverNode(Node):
    # HC-SR04 constants
    _TRIG_PULSE_S = 10e-6  # 10 µs trigger pulse
    _SPEED_OF_SOUND = 343.0  # m/s at ~20 °C
    _ECHO_TIMEOUT_S = 0.030  # 30 ms → ~5 m max range
    _MIN_RANGE = 0.02  # 2 cm
    _MAX_RANGE = 4.0  # 4 m (reliable HC-SR04 spec)

    def __init__(self):
        super().__init__("ultrasonic_driver_node")

        self.declare_parameter("chip", "gpiochip4")  # Pi 5 RP1 GPIO chip
        self.declare_parameter("trig_pin", 23)
        self.declare_parameter("echo_pin", 24)
        self.declare_parameter("publish_hz", 10.0)
        self.declare_parameter("frame_id", "ultrasonic_link")
        self.declare_parameter("simulate", False)
        self.declare_parameter("field_of_view_deg", 15.0)

        p = self.get_parameter
        self._frame_id = p("frame_id").value
        self._simulate = p("simulate").value
        self._fov = math.radians(p("field_of_view_deg").value)
        hz = max(1.0, min(20.0, p("publish_hz").value))

        self._chip = None
        self._trig = None
        self._echo = None
        self._lock = threading.Lock()
        self._sim_t = 0.0

        if not self._simulate:
            self._init_gpio(
                p("chip").value,
                p("trig_pin").value,
                p("echo_pin").value,
            )

        self._pub = self.create_publisher(Range, "/sensors/ultrasonic/range", 10)
        self.create_timer(1.0 / hz, self._measure_and_publish)
        self.get_logger().info(
            f"UltrasonicDriverNode ready  simulate={self._simulate}  hz={hz:.0f}"
        )

    def _init_gpio(self, chip_name: str, trig: int, echo: int) -> None:
        if not _GPIOD_OK:
            self.get_logger().warn(
                "python3-gpiod not installed; switching to simulate=True. "
                "Install with: sudo apt install python3-gpiod"
            )
            self._simulate = True
            return
        try:
            self._chip = gpiod.Chip(chip_name)
            self._trig = self._chip.get_line(trig)
            self._echo = self._chip.get_line(echo)
            self._trig.request(
                consumer="ultrasonic_trig",
                type=gpiod.LINE_REQ_DIR_OUT,
                default_val=0,
            )
            self._echo.request(
                consumer="ultrasonic_echo",
                type=gpiod.LINE_REQ_DIR_IN,
            )
            self.get_logger().info(
                f"GPIO ready  chip={chip_name}  trig={trig}  echo={echo}"
            )
        except Exception as exc:
            self.get_logger().error(
                f"GPIO init failed ({exc}); switching to simulate=True"
            )
            self._simulate = True

    def _measure_and_publish(self) -> None:
        dist = self._simulate_reading() if self._simulate else self._hw_reading()
        if dist is None:
            return

        msg = Range()
        msg.header.stamp = self.get_clock().now().to_msg()
        msg.header.frame_id = self._frame_id
        msg.radiation_type = Range.ULTRASOUND
        msg.field_of_view = self._fov
        msg.min_range = self._MIN_RANGE
        msg.max_range = self._MAX_RANGE
        msg.range = float(dist)
        self._pub.publish(msg)

    def _hw_reading(self) -> float | None:
        """Fire trigger, time echo pulse, return distance in metres."""
        with self._lock:
            # Send 10 µs trigger
            self._trig.set_value(1)
            time.sleep(self._TRIG_PULSE_S)
            self._trig.set_value(0)

            # Wait for echo HIGH
            t_start = time.monotonic()
            while self._echo.get_value() == 0:
                if time.monotonic() - t_start > self._ECHO_TIMEOUT_S:
                    return None
            t0 = time.monotonic()

            # Wait for echo LOW
            while self._echo.get_value() == 1:
                if time.monotonic() - t0 > self._ECHO_TIMEOUT_S:
                    return None
            t1 = time.monotonic()

        dist = (t1 - t0) * self._SPEED_OF_SOUND / 2.0
        if dist < self._MIN_RANGE or dist > self._MAX_RANGE:
            return None
        return dist

    def _simulate_reading(self) -> float:
        self._sim_t += 0.1
        # Oscillate between 0.3 m and 1.5 m
        return 0.9 + 0.6 * math.sin(self._sim_t)

    def destroy_node(self) -> None:
        if self._chip is not None:
            try:
                self._trig.release()
                self._echo.release()
                self._chip.close()
            except Exception:
                pass
        super().destroy_node()


def main(args=None) -> None:
    rclpy.init(args=args)
    node = UltrasonicDriverNode()
    try:
        rclpy.spin(node)
    finally:
        node.destroy_node()
        rclpy.shutdown()
