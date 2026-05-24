---
name: omnibot_manipulate
description: Control the OmniBot SO-101 arm and execute VLA manipulation tasks
version: 1.0.0
tools:
  - execute_manipulation
  - move_arm
  - get_robot_state
  - get_camera_image
  - describe_scene
---
# OmniBot Manipulation Skill

Use this skill to control the robot's SO-101 6-DOF arm or trigger VLA policies.

## VLA Tasks (recommended)

Call `execute_manipulation` with a natural language task description. The SmolVLA/OpenVLA policy handles all arm motions:

```
execute_manipulation("pick up the red cup")
execute_manipulation("open the drawer")
execute_manipulation("place the object on the shelf")
```

## Direct Arm Control

Use `move_arm` for precise joint-level control. Joint order:
`[shoulder_pan, shoulder_lift, elbow_flex, wrist_flex, wrist_roll, gripper]`

Limits: pan ±3.14, lift ±1.57, elbow ±1.57, wrist_flex ±1.57, roll ±3.14, gripper [-0.1, 0.8]

```
move_arm([0.0, -0.5, 1.0, 0.5, 0.0, 0.3])
```

## Scene Understanding

Use `describe_scene("wrist")` before manipulation to understand what the arm camera sees.
Use `get_camera_image("wrist")` to get the raw JPEG for your own vision analysis.
