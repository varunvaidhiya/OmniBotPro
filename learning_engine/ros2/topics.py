"""ROS 2 topic names the learning engine touches.

Single source of truth for the bridge code in this package — values mirror
the topic map in CLAUDE.md. If a topic is renamed in robot_ws, update here.
"""

from __future__ import annotations

# Observations consumed (robot → learning engine)
ODOM = "/odom"
ARM_JOINT_STATES = "/arm/joint_states"
CAMERA_FRONT = "/camera/front/image_raw"
CAMERA_WRIST = "/camera/wrist/image_raw"
CAMERA_BEV = "/camera/base/bev/image_raw"
CAMERA_DEPTH = "/camera/depth/image_raw"

# Executed commands logged as actions
CMD_VEL_OUT = "/cmd_vel/out"  # post-mux base command actually sent to driver
ARM_COMMANDS_OUT = "/arm/joint_commands/out"  # post-mux arm command

# Task / outcome context
MISSION_COMMAND = "/mission/command"
MISSION_STATUS = "/mission/status"
VLA_PROMPT = "/vla/prompt"
EMERGENCY_STOP = "/emergency_stop"

# Learning-engine control surface (new topics, namespaced /learning)
EPISODE_START = "/learning/episode/start"  # String: task instruction
EPISODE_STOP = "/learning/episode/stop"  # String: outcome hint ("", "success"...)
EPISODE_STATUS = "/learning/episode/status"  # String: idle|recording:<id>
