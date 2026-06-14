# Running the OmniBot: Full System Guide

This guide explains how to start up the entire robot system across your three devices:
1.  **Yahboom ROS Board** (Microcontroller)
2.  **Raspberry Pi 5** (Robot Brain)
3.  **Windows PC** (AI & VLA Brain)

## Device 1: Yahboom ROS Board
*   **Role**: Motor Controller (Low-level)
*   **Startup**: Automatic
    1.  Simply **Turn on the Battery Switch** on the robot.
    2.  The board will boot up.
    3.  **Check**: Ensure the USB cable is connected from the Board to the Raspberry Pi.

---

## Device 2: Raspberry Pi 5 (The Robot)
*   **Role**: Hardware Drivers, Camera, Networking
*   **Prerequisite**: Ensure you have pulled the latest code and installed dependencies (`python3-serial`, `joy`, etc).

### Step 1: Login
SSH into your Pi or open a terminal on it.

### Step 2: Build (If code changed)
```bash
cd ~/OmniBotPro
colcon build --packages-select omnibot_bringup omnibot_driver
source install/setup.bash
```

### Step 3: Launch Drivers
This command starts the:
- **Yahboom Driver** (Talks to Device 1)
- **Robot State Publisher** (URDF)
- **Camera Node** (optional, add if needed)

```bash
# General Robot Launch
ros2 launch omnibot_bringup robot.launch.py
```

### Step 4: Launch Camera (Crucial for VLA)
Open a **new terminal** on the Pi and start your camera (Webcam or Kinect).
```bash
# Example for USB Webcam
ros2 run usb_cam usb_cam_node_exe
```

---

## Device 3: Windows PC (The AI Brain)
*   **Role**: OpenVLA Inference, Xbox Teleop (Optional), Visualization
*   **Prerequisite**: WSL2 or Native Windows ROS 2 installed. Code built.

### Step 1: Network Check ⚠️
Ensure your PC and Pi are on the same Wi-Fi.
Test it:
```bash
# On PC
ros2 topic list
# You should see /odom and /image_raw from the Pi!
```

### Step 2: Run OpenVLA (AI Control)
If you want the AI to control the robot:
```bash
cd ~/OmniBotPro
source install/setup.bash
ros2 launch omnibot_vla vla_desktop.launch.py
```
*Wait for the model to load (can take 1-2 mins).*

### Step 3: Send Commands
Open a **new terminal** on PC:
```bash
ros2 topic pub --once /vla/prompt std_msgs/msg/String "data: 'Find the red cup'"
```

---

## Alternative: Xbox Control (Manual)
If you just want to drive manually (no AI):

1.  **Connect Xbox Controller** to the **PC** or **Pi** (wherever you run the joy node).
2.  Run the teleop launch:
    ```bash
    ros2 launch omnibot_bringup joy_teleop.launch.py
    ```
    *(Note: If controller is on PC, run this on PC. If on Pi, run on Pi).*

## Summary Checklist

| Device | Action | Indicator |
| :--- | :--- | :--- |
| **Yahboom** | Power Switch ON | Lights ON |
| **Pi 5** | `robot.launch.py` | "Yahboom board initialized" |
| **Pi 5** | Camera Node | `/image_raw` is publishing |
| **PC** | `vla_desktop.launch.py` | "Model loaded successfully" |
