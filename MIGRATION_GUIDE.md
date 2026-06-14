# Migration Guide: STM32 to Yahboom Board

This guide helps you migrate from the STM32 microcontroller to the Yahboom ROS Robot Expansion Board for your OmniBot mecanum wheel robot.

## Overview of Changes

The main changes involve:
1. **Hardware replacement**: STM32 → Yahboom board
2. **Communication protocol**: Custom serial → Simplified commands
3. **Software updates**: New controller nodes and configuration
4. **Firmware**: No longer needed (Yahboom board handles low-level control)

## Hardware Changes

### What to Remove
- STM32F407VG Discovery board
- L298N motor drivers (x2)
- External encoder connections
- Custom wiring for PWM and encoder signals

### What to Add
- Yahboom ROS Robot Expansion Board
- USB connection cable
- Power supply for the Yahboom board

### Wiring Changes
```
OLD (STM32):
Raspberry Pi → STM32 → Motor Drivers → Motors
           ↓
        Encoders

NEW (Yahboom):
Raspberry Pi → USB → Yahboom Board → Motors
                           ↓
                        Built-in Encoders
```

## Software Changes

### New Files Added
- `src/omnibot_driver/scripts/yahboom_controller_node.py`
- `src/omnibot_driver/scripts/yahboom_test_node.py`
- `src/omnibot_driver/launch/yahboom_base_control.launch.py`
- `src/omnibot_driver/launch/yahboom_test.launch.py`
- `src/omnibot_driver/config/yahboom_params.yaml`

### Files Modified
- `src/omnibot_driver/CMakeLists.txt` - Added new scripts
- `src/omnibot_driver/package.xml` - Updated dependencies
- `README.md` - Updated documentation

### Files to Keep (Legacy Support)
- `src/omnibot_driver/scripts/mecanum_controller_node.py`
- `src/omnibot_driver/scripts/serial_bridge_node.py`
- `src/omnibot_driver/launch/base_control.launch.py`
- `src/omnibot_driver/config/mecanum_params.yaml`
- `src/omnibot_firmware/` - Keep for reference

## Communication Protocol Changes

### STM32 Protocol
```python
# Commands sent to STM32
"<FL,FR,RL,RR>\n"  # Wheel velocities in rad/s
"<CMD_VEL,x,y,z>\n"  # Twist commands

# Data received from STM32
"<ENCODERS,fl,fr,rl,rr>\n"  # Encoder ticks
"<STATUS,message>\n"  # Status messages
```

### Yahboom Protocol
```python
# Commands sent to Yahboom board
"INIT\n"  # Initialize board
"MODE_MECANUM\n"  # Set mecanum mode
"MOTOR,fl,fr,rl,rr\n"  # PWM percentages (-100 to 100)
"STATUS\n"  # Get board status

# Data received from Yahboom board
"ODOM,x,y,theta\n"  # Odometry data
"ENCODERS,fl,fr,rl,rr\n"  # Encoder ticks
"STATUS,message\n"  # Status messages
```

## Migration Steps

### Step 1: Hardware Setup
1. **Disconnect STM32**: Remove power and USB connections
2. **Connect Yahboom board**: 
   - Connect USB cable to Raspberry Pi
   - Connect power supply to Yahboom board
   - Connect motors to Yahboom board motor outputs
3. **Verify connections**: Check that all motors are properly connected

### Step 2: Software Installation
1. **Build the updated workspace**:
   ```bash
   cd OmniBotPro
   colcon build
   source install/setup.bash
   ```

2. **Install dependencies**:
   ```bash
   pip install pyserial numpy
   ```

### Step 3: Test the Yahboom Board
1. **Run the test node**:
   ```bash
   ros2 launch omnibot_driver yahboom_test.launch.py
   ```

2. **Check for successful communication**:
   - Look for "Successfully connected to Yahboom board" message
   - Verify that test commands are sent and responses received
   - Check that motors respond to test movements

### Step 4: Configure Parameters
1. **Edit robot parameters** in `src/omnibot_driver/config/yahboom_params.yaml`:
   ```yaml
   wheel_radius: 0.04  # Adjust to your wheel size
   wheel_separation_width: 0.215  # Adjust to your robot
   wheel_separation_length: 0.165  # Adjust to your robot
   serial_port: /dev/ttyUSB0  # Verify correct port
   ```

2. **Test with different serial ports** if needed:
   ```bash
   ls /dev/ttyUSB*
   ls /dev/ttyACM*
   ```

### Step 5: Run the Robot
1. **Launch the controller**:
   ```bash
   ros2 launch omnibot_driver yahboom_base_control.launch.py
   ```

2. **Test movement**:
   ```bash
   # Forward movement
   ros2 topic pub /cmd_vel geometry_msgs/msg/Twist "{linear: {x: 0.5, y: 0.0, z: 0.0}, angular: {x: 0.0, y: 0.0, z: 0.0}}"
   
   # Sideways movement
   ros2 topic pub /cmd_vel geometry_msgs/msg/Twist "{linear: {x: 0.0, y: 0.5, z: 0.0}, angular: {x: 0.0, y: 0.0, z: 0.0}}"
   
   # Rotation
   ros2 topic pub /cmd_vel geometry_msgs/msg/Twist "{linear: {x: 0.0, y: 0.0, z: 0.0}, angular: {x: 0.0, y: 0.0, z: 1.0}}"
   ```

## Troubleshooting

### Common Issues

1. **Port not found**:
   ```bash
   # Check available ports
   ls /dev/ttyUSB*
   ls /dev/ttyACM*
   
   # Add user to dialout group
   sudo usermod -a -G dialout $USER
   # Log out and back in, or reboot
   ```

2. **Permission denied**:
   ```bash
   # Fix permissions
   sudo chmod 666 /dev/ttyUSB0
   ```

3. **No motor response**:
   - Check motor connections to Yahboom board
   - Verify power supply to Yahboom board
   - Check if motors are compatible with Yahboom board voltage

4. **Wrong movement direction**:
   - Swap motor connections on Yahboom board
   - Or modify the kinematics in the controller code

### Debugging Commands

1. **Monitor serial communication**:
   ```bash
   # In another terminal
   ros2 topic echo /serial_data
   ```

2. **Check node status**:
   ```bash
   ros2 node list
   ros2 node info /yahboom_controller_node
   ```

3. **View parameters**:
   ```bash
   ros2 param list /yahboom_controller_node
   ros2 param get /yahboom_controller_node serial_port
   ```

## Performance Comparison

| Aspect | STM32 | Yahboom Board |
|--------|-------|---------------|
| **Setup Time** | 2-3 hours | 30 minutes |
| **Reliability** | High (custom) | High (tested) |
| **Flexibility** | High (customizable) | Medium (fixed features) |
| **Cost** | Lower | Higher |
| **Maintenance** | More complex | Simpler |

## Rollback Plan

If you need to revert to STM32:

1. **Hardware**: Reconnect STM32 and motor drivers
2. **Software**: Use the original launch file:
   ```bash
   ros2 launch omnibot_driver base_control.launch.py
   ```
3. **Configuration**: Use `mecanum_params.yaml` instead of `yahboom_params.yaml`

## Next Steps

After successful migration:

1. **Calibrate odometry** using the encoder data
2. **Tune motor parameters** for optimal performance
3. **Add additional sensors** (IMU, LIDAR, etc.)
4. **Implement navigation** using Nav2 stack
5. **Add SLAM** for mapping capabilities

## Support

- **Yahboom Documentation**: [https://www.yahboom.net/study/ROS-Driver-Board](https://www.yahboom.net/study/ROS-Driver-Board)
- **ROS2 Documentation**: [https://docs.ros.org/](https://docs.ros.org/)
- **Project Issues**: Create an issue in the GitHub repository

## Conclusion

The migration to Yahboom board simplifies the hardware setup and reduces development time while maintaining the same ROS2 functionality. The new controller provides a more robust and tested solution for mecanum wheel control.
