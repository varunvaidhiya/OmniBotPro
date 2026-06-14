# Xbox Controller Setup & Teleop Guide

This guide explains how to connect your Xbox controller to the Raspberry Pi and use it to control the OmniBot.

## 1. Hardware Connection

You can connect the controller via **USB** or **Bluetooth**.

### Option A: USB (Simplest)
1.  Connect the Xbox controller to the Raspberry Pi 5 using a USB-C cable.
2.  The controller should vibrate, and the Xbox logo light should turn solid.

### Option B: Bluetooth
1.  On your Raspberry Pi terminal, run `bluetoothctl`:
    ```bash
    sudo bluetoothctl
    ```
2.  In the bluetooth prompt:
    ```bash
    scan on
    ```
3.  Put your Xbox controller in **Pairing Mode** (Hold the small wireless button on top until the Xbox logo flashes fast).
4.  Look for "Xbox Wireless Controller" in the scan list and note its MAC address (e.g., `XX:XX:XX:XX:XX:XX`).
5.  Pair and trust:
    ```bash
    pair XX:XX:XX:XX:XX:XX
    trust XX:XX:XX:XX:XX:XX
    connect XX:XX:XX:XX:XX:XX
    ```
6.  The light should turn solid. Type `exit` to quit.

## 2. Verify Connection

Check if the system detects the joystick:

```bash
ls /dev/input/js*
```
You should see `/dev/input/js0`.

Test the inputs:
```bash
jstest /dev/input/js0
```
Move the sticks and press buttons. You should see the numbers change. Press `Ctrl+C` to exit.

## 3. How It Works (Data Flow)

Here is how your button press turns into robot motion:

1.  **`joy_node`**: Reads raw electrical signals from `/dev/input/js0` and publishes them as ROS 2 messages on the `/joy` topic.
2.  **`teleop_twist_joy_node`**: Subscribes to `/joy`. It uses the configuration map (in `xbox_teleop.yaml`) to convert stick positions into velocity commands (linear X/Y, angular Z).
3.  **`/cmd_vel`**: The velocity command is published to this topic.
4.  **`omnibot_driver`**: Subscribes to `/cmd_vel`, converts the target velocity into individual motor speeds (mecanum kinematics), and sends them to the **Yahboom Board**.

## 4. Running the Teleop

### Step 1: Install Dependencies
On your Raspberry Pi:
```bash
sudo apt update
sudo apt install ros-jazzy-joy ros-jazzy-teleop-twist-joy joystick
```

### Step 2: Build Workspace
Ensure you have built the latest changes (with the new launch files):
```bash
cd ~/OmniBotPro
colcon build --packages-select omnibot_bringup
source install/setup.bash
```

### Step 3: Launch
You can run everything in two separate terminals for better visibility.

**Terminal 1: Start the Joystick & Teleop Logic**
```bash
ros2 launch omnibot_bringup joy_teleop.launch.py
```

**Terminal 2: Start the Robot Driver**
```bash
ros2 launch omnibot_bringup robot.launch.py
```

## 5. Controls (Default Mapping)

| Input | Action | Note |
| :--- | :--- | :--- |
| **RB (Hold)** | **Enable / Deadman** | **You MUST hold this button to move!** |
| **Left Stick** | Move Forward/Back/Left/Right | Omnidirectional movement |
| **Right Stick** | Rotate Left/Right | |
| **RT (Hold)** | Turbo Mode | Move faster while holding |
