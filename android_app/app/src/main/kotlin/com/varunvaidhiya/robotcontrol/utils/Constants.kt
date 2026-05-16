package com.varunvaidhiya.robotcontrol.utils

object Constants {
    // Network
    const val DEFAULT_ROBOT_IP         = "192.168.1.100"
    const val DEFAULT_ROSBRIDGE_PORT   = 9090
    const val CONNECTION_TIMEOUT_MS    = 10000L

    // Control
    const val JOYSTICK_DEADZONE        = 0.1f
    const val MAX_LINEAR_VELOCITY      = 1.5f   // m/s
    const val MAX_ANGULAR_VELOCITY     = 2.0f   // rad/s
    const val CMD_VEL_PUBLISH_RATE_HZ  = 20

    // ── ROS Topics: publish ───────────────────────────────────────────────────
    // Joystick velocity goes to the cmd_vel_mux teleop slot so it is only
    // forwarded when /control_mode == "teleop". Set the mode explicitly before
    // driving manually (repository.sendControlMode("teleop")).
    const val TOPIC_CMD_VEL            = "/cmd_vel/teleop"
    const val TOPIC_EMERGENCY_STOP     = "/emergency_stop"
    const val TOPIC_ROBOT_MODE         = "/robot_mode"        // monitoring only

    // Control mode for cmd_vel_mux ("nav2" | "vla" | "teleop" | "rl_nav")
    const val TOPIC_CONTROL_MODE       = "/control_mode"

    // Mission / AI orchestration
    const val TOPIC_MISSION_COMMAND    = "/mission/command"   // std_msgs/String
    const val TOPIC_VLA_PROMPT         = "/vla/prompt"        // std_msgs/String

    // LangGraph AI orchestration (requires omnibot_orchestration node running)
    const val TOPIC_AI_COMMAND         = "/ai/command"        // std_msgs/String
    const val TOPIC_AI_STATUS          = "/ai/status"         // std_msgs/String
    const val TOPIC_AI_RESPONSE_NEEDED = "/ai/response_needed" // std_msgs/String

    // Dataset recording (handled by a ROS 2 recorder node)
    const val TOPIC_RECORD_START       = "/rosbag_recorder/start"  // std_msgs/String (bag name)
    const val TOPIC_RECORD_STOP        = "/rosbag_recorder/stop"   // std_msgs/Empty

    // Arm topics
    const val TOPIC_ARM_JOINT_COMMANDS = "/arm/joint_commands"
    const val TOPIC_ARM_ENABLE         = "/arm/enable"

    // ── ROS Topics: subscribe ─────────────────────────────────────────────────
    const val TOPIC_ODOM               = "/odom"
    const val TOPIC_MAP                = "/map"
    const val TOPIC_IMU                = "/imu/data"
    const val TOPIC_DIAGNOSTICS        = "/diagnostics"
    const val TOPIC_ARM_JOINT_STATES   = "/arm/joint_states"

    // Mission feedback
    const val TOPIC_MISSION_STATUS     = "/mission/status"    // std_msgs/String

    // Depth camera point cloud
    const val TOPIC_POINT_CLOUD        = "/camera/depth/points"  // sensor_msgs/PointCloud2

    // Legacy custom topics
    const val TOPIC_ROBOT_STATUS       = "/robot_status"
    const val TOPIC_WHEEL_SPEEDS       = "/wheel_speeds"
    const val TOPIC_MOTOR_PWM          = "/motor_pwm"

    // Arm joint names (must match URDF arm_* prefix)
    val ARM_JOINT_NAMES = listOf(
        "arm_shoulder_pan",
        "arm_shoulder_lift",
        "arm_elbow_flex",
        "arm_wrist_flex",
        "arm_wrist_roll",
        "arm_gripper"
    )

    // UI
    const val CHART_MAX_DATA_POINTS    = 300
    const val DASHBOARD_UPDATE_RATE_HZ = 10

    // Logging
    const val LOG_FILE_MAX_SIZE_MB     = 50
    const val LOG_RETENTION_DAYS       = 7

    // Point cloud: max points rendered per frame (performance guard)
    const val POINT_CLOUD_MAX_POINTS   = 20_000
}
