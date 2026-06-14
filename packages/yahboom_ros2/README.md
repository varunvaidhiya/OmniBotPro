# yahboom_ros2

> ROS 2 driver for the **Yahboom ROS Robot Expansion Board** (Rosmaster series).

This package contains the first fully open-source ROS 2 driver and **complete
serial protocol documentation** for Yahboom expansion boards — reverse-engineered
from hardware captures and published here for the community.

## Features

- Full serial protocol encoder/decoder (`protocol.py`) — **no ROS dependency**,
  usable standalone for testing and firmware work
- `yahboom_driver` node: subscribes `/cmd_vel`, publishes `/odom` + `/imu/data` + TF
- Velocity ramping to prevent motor brownouts
- On-board IMU publishing (accelerometer, gyroscope, Euler attitude)
- Automatic serial reconnect on cable unplug
- Optional joystick button → buzzer (`/joy` topic, button A)

## Protocol Reference

```
TX packet:  [0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]
RX packet:  [0xFF, 0xFB, LEN, TYPE, PAYLOAD..., CS]

TX CHECKSUM = (sum(all packet bytes) + 5) & 0xFF   # 5 = 257 - 0xFC
RX CS       = sum(LEN, TYPE, PAYLOAD...) & 0xFF     # header bytes excluded
```

| Function Code | Value | Description |
|---|---|---|
| `FUNC_BEEP` | `0x02` | Buzzer (int16 duration ms) |
| `FUNC_MOTOR` | `0x10` | Direct PWM (4 × int16) |
| `FUNC_MOTION` | `0x12` | Holonomic: CAR_TYPE + vx/vy/vz ×1000 int16 |
| `FUNC_SET_CAR_TYPE` | `0x15` | Set robot type (1 = Mecanum X3) |

| Response Type | Value | Payload |
|---|---|---|
| `TYPE_VELOCITY` | `0x0C` | vx, vy, vz (int16 ×1000) |
| `TYPE_ACCEL` | `0x61` | ax, ay, az (int16, mg) |
| `TYPE_GYRO` | `0x62` | gx, gy, gz (int16, mrad/s) |
| `TYPE_ATTITUDE` | `0x63` | roll, pitch, yaw (int16, 0.01 deg) |

## Install

```bash
# In your ROS 2 workspace
cd ~/ros2_ws/src
git clone https://github.com/varunvaidhiya/OmniBotPro.git
ln -s OmniBotPro/packages/yahboom_ros2 .
cd ~/ros2_ws
colcon build --packages-select yahboom_ros2
source install/setup.bash
```

## Usage

```bash
# With defaults (USB0, 115200 baud)
ros2 launch yahboom_ros2 yahboom_driver.launch.py

# Override port
ros2 launch yahboom_ros2 yahboom_driver.launch.py serial_port:=/dev/ttyUSB1

# With param file
ros2 run yahboom_ros2 yahboom_driver --ros-args --params-file config/yahboom_params.yaml
```

## Standalone Protocol Usage (no ROS)

```python
from yahboom_ros2.protocol import packet_motion, packet_beep, parse_rx_buffer
import serial

ser = serial.Serial('/dev/ttyUSB0', 115200)

# Move forward 0.2 m/s
ser.write(packet_motion(0.2, 0.0, 0.0))

# Parse response
data = ser.read(ser.in_waiting)
for offset, pkt in parse_rx_buffer(data):
    print(pkt)
```

## Parameters

| Parameter | Default | Description |
|---|---|---|
| `serial_port` | `/dev/ttyUSB0` | Serial device path |
| `baud_rate` | `115200` | Baud rate |
| `wheel_radius` | `0.04` | Wheel radius (m) |
| `wheel_separation_width` | `0.215` | Left–right track width (m) |
| `wheel_separation_length` | `0.165` | Front–back wheelbase (m) |
| `max_linear_vel` | `0.5` | Max linear speed clamp (m/s) |
| `max_angular_vel` | `2.0` | Max angular speed clamp (rad/s) |
| `ramp_step` | `0.05` | Velocity ramp per tick (m/s) |
| `update_hz` | `20.0` | Control loop frequency (Hz) |
| `debug_serial` | `false` | Log unknown RX packets |

## Topics

| Topic | Type | Direction |
|---|---|---|
| `/cmd_vel` | `geometry_msgs/Twist` | Subscribed |
| `/joy` | `sensor_msgs/Joy` | Subscribed |
| `/odom` | `nav_msgs/Odometry` | Published |
| `/imu/data` | `sensor_msgs/Imu` | Published |
| `tf` odom→base_link | TF2 | Broadcast |

## License

Apache-2.0
