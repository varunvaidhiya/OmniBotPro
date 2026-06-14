# mecanum_drive_ros2

> Generic mecanum wheel kinematics library for ROS 2 — hardware-agnostic,
> works with any motor board.

## Features

- **C++17 header-only** library (`mecanum_kinematics.hpp`) — drop into any C++ ROS 2 package
- **Pure Python** mirror (`mecanum_drive_ros2.kinematics`) — use in nodes, tests, notebooks
- Inverse kinematics: body twist → wheel angular velocities
- Forward kinematics: wheel angular velocities → body twist
- Pose integration helper with heading wrap to [-π, π]
- Parameterized geometry — configure for any mecanum robot

## Kinematics

```
Wheel layout:         Sign conventions (ROS REP 103):
  FL ─── FR             vx  = forward  (+x)
  │       │             vy  = left     (+y)
  BL ─── BR             ω   = CCW      (+z)

  L = wheel_separation_length / 2
  W = wheel_separation_width  / 2
  k = L + W
  r = wheel_radius

Inverse kinematics (body twist → wheel rad/s):
  FL = (vx - vy - k·ω) / r
  FR = (vx + vy + k·ω) / r
  BL = (vx + vy - k·ω) / r
  BR = (vx - vy + k·ω) / r

Forward kinematics (wheel rad/s → body twist):
  vx    = r · ( FL + FR + BL + BR) / 4
  vy    = r · (-FL + FR + BL - BR) / 4
  omega = r · (-FL + FR - BL + BR) / (4·k)
```

`integrate_pose` / `integratePose` advances a 2-D pose `(x, y, theta)` by a
body twist over `dt`, wrapping `theta` to `[-π, π]`.

## Usage — Python

Public API (`from mecanum_drive_ros2 import ...`): `RobotGeometry`,
`WheelVelocities`, `inverse_kinematics`, `forward_kinematics`, `integrate_pose`.

```python
from mecanum_drive_ros2 import RobotGeometry, inverse_kinematics, forward_kinematics, integrate_pose

geom = RobotGeometry(
    wheel_radius=0.04,
    wheel_separation_width=0.215,
    wheel_separation_length=0.165,
)

# Inverse kinematics: body twist → (fl, fr, bl, br) rad/s
fl, fr, bl, br = inverse_kinematics(0.3, 0.0, 0.0, geom)

# Forward kinematics: wheel rad/s → (vx, vy, omega)
vx, vy, omega = forward_kinematics((fl, fr, bl, br), geom)

# Pose integration: returns (x, y, theta) with theta wrapped to [-pi, pi]
x, y, theta = integrate_pose(0.0, 0.0, 0.0, vx, vy, omega, dt=0.05)
```

`inverse_kinematics(vx, vy, omega, geom)` and
`forward_kinematics(wheels, geom)` take positional arguments;
`geom` is not a keyword in the signatures.

## Usage — C++

Namespace `mecanum_drive`. `WheelVelocities` is a `std::array<double, 4>`
(`[FL, FR, BL, BR]`); `forwardKinematics` returns a `BodyTwist{vx, vy, omega}`.

```cpp
#include "mecanum_drive_ros2/mecanum_kinematics.hpp"

// RobotGeometry{wheel_radius, wheel_separation_width, wheel_separation_length}
mecanum_drive::RobotGeometry geom{0.04, 0.215, 0.165};

mecanum_drive::WheelVelocities wheels =
    mecanum_drive::inverseKinematics(0.3, 0.0, 0.0, geom);
mecanum_drive::BodyTwist twist =
    mecanum_drive::forwardKinematics(wheels, geom);

double x = 0, y = 0, theta = 0;
mecanum_drive::integratePose(x, y, theta, twist.vx, twist.vy, twist.omega, 0.05);
```

## Default parameters

`config/mecanum_params.yaml` ships defaults matching the OmniBot platform
(`wheel_radius: 0.04`, `wheel_separation_width: 0.215`,
`wheel_separation_length: 0.165`, plus optional `serial_port` / `baud_rate`).
Tune these to your robot's measurements.

## Install

ROS 2 / C++ build (`ament_cmake`):

```bash
cd ~/ros2_ws/src
ln -s /path/to/OmniBotPro/packages/mecanum_drive_ros2 .
cd ~/ros2_ws && colcon build --packages-select mecanum_drive_ros2
```

Standalone Python (pure-Python kinematics, no ROS):

```bash
pip install -e packages/mecanum_drive_ros2
```

## License

Apache-2.0
