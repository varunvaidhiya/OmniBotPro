# OmniBot USD Assets

This directory holds the Isaac Sim USD (Universal Scene Description) version
of the OmniBot robot model.  The file `omnibot.usd` is **not checked in** to
git because it must be generated on the target machine using the Isaac Sim
URDF importer (the binary format embeds absolute paths and machine-local
physics articulation handles).

## How to Generate omnibot.usd

### Step 1 — Bake the xacro to plain URDF

```bash
source /opt/ros/jazzy/setup.bash
xacro robot_ws/src/omnibot_description/urdf/omnibot.urdf.xacro \
    > /tmp/omnibot.urdf
```

### Step 2 — Import into Isaac Sim (GUI method)

1. Open Isaac Sim 4.x.
2. Menu: **Isaac Utils → Workflows → URDF Importer**
3. Set **Input File** to `/tmp/omnibot.urdf`.
4. Set **Output Directory** to this folder:
   `robot_ws/src/omnibot_description/usd/`
5. Tick **Fix Base Link = OFF** (the robot is mobile).
6. Tick **Merge Fixed Joints = ON** for performance.
7. Click **Import**.
8. Save the opened stage as `omnibot.usd` in this directory.

### Step 3 — Verify articulation

In the Isaac Sim stage tree:
- `/World/OmniBot` should appear as an **Articulation** root.
- Four wheel joints (`front_left_wheel_joint`, `front_right_wheel_joint`,
  `rear_left_wheel_joint`, `rear_right_wheel_joint`) should be **DriveAPI**
  continuous joints.
- Six arm joints (`arm_shoulder_pan`, `arm_shoulder_lift`, `arm_elbow_flex`,
  `arm_wrist_flex`, `arm_wrist_roll`, `arm_gripper`) should also be **DriveAPI**
  revolute joints.

### Step 4 — Configure ROS 2 Bridge OmniGraph

Inside Isaac Sim, create an OmniGraph Action Graph:
1. **IsaacArticulationController** — target prim `/World/OmniBot`, reads
   `/arm/joint_commands` and `/cmd_vel`.
2. **ROS2PublishOdometry** — publishes `/odom`.
3. **ROS2PublishJointState** — publishes `/joint_states`.
4. **ROS2PublishImu** — publishes `/imu/data`.
5. **ROS2PublishImage** (×5) — publishes each camera topic listed in
   `omnibot_bringup/config/isaac_ros_bridge.yaml`.
6. **ROS2PublishTransformTree** — publishes `/tf`.

Save the stage.  The file is now ready to use with:
```bash
ros2 launch omnibot_bringup isaac_sim.launch.py
```

## Notes

- `omnibot.usd` is listed in `.gitignore` — do not force-add it.
- Physics material assignments (friction, restitution) and drive stiffness /
  damping values can be tuned in Isaac Sim's **Physics** inspector after import.
- For headless / CI use, NVIDIA provides the `urdf2usd` CLI tool:
  ```bash
  <isaac_sim_root>/python.sh \
    -c "from omni.isaac.urdf import _urdf; \
        _urdf.acquire_urdf_interface().import_robot('/tmp/omnibot.urdf')"
  ```
