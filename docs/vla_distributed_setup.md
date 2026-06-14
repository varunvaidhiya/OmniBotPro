# Distributed OpenVLA Setup Guide

This guide explains how to run the AI on your **Desktop PC (RTX 5060 Ti)** while controlling the **Robot (Raspberry Pi 5)**.

## 1. Network Setup (Crucial)
Both machines must be on the same Wi-Fi network and must talk to each other.

### On Both Machines (Pi & PC):
1.  Open `.bashrc`: `nano ~/.bashrc`
2.  Add this line to the bottom (use the same number, e.g., 42):
    ```bash
    export ROS_DOMAIN_ID=42
    ```
3.  Save and source: `source ~/.bashrc`

To test connection:
- Run `ros2 topic list` on the PC while the Pi driver is running. You should see `/odom` and `/image_raw`.

## 2. Desktop PC Setup (Inference)
You need to install the heavy AI libraries on your PC.

1.  **Install System Dependencies**:
    ```bash
    sudo apt install python3-pip ros-jazzy-cv-bridge
    ```

2.  **Install Python Libraries**:
    ```bash
    pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
    pip3 install transformers accelerate bitsandbytes protobuf scipy
    ```

3.  **Build the VLA Package**:
    ```bash
    cd ~/OmniBotPro
    colcon build --packages-select omnibot_vla
    source install/setup.bash
    ```

## 3. Usage Steps

### Step 1: Start Robot (On Pi)
Start the drivers and camera.
```bash
ros2 launch omnibot_bringup robot.launch.py
# (Ensure you also have a camera node running, e.g., v4l2_camera or usb_cam)
```

### Step 2: Start AI (On PC)
Launch the VLA node. It will download the 7B model (approx 15GB) on the first run.
```bash
ros2 launch omnibot_vla vla_desktop.launch.py
```

### Step 3: Send Commands
Open a **new terminal on the PC** and tell the robot what to do:
```bash
ros2 topic pub --once /vla/prompt std_msgs/msg/String "data: 'Find the blue bottle'"
```

## Troubleshooting
- **Out of Memory (OOM)**: If the 5060 Ti (8GB/16GB VRAM) crashes, edit `vla_desktop.launch.py` and set `'load_in_4bit': True`.
- **Latency**: If the robot lags, check your Wi-Fi signal. Sending raw images requires good bandwidth.
