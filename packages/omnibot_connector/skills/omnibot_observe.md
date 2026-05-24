---
name: omnibot_observe
description: Read the OmniBot's sensors — position, velocity, arm joints, and cameras
version: 1.0.0
tools:
  - get_robot_state
  - get_camera_image
  - describe_scene
  - list_locations
---
# OmniBot Observation Skill

Use this skill to perceive the robot's current state and environment.

## State Snapshot

`get_robot_state` returns:
- `position`: `{x, y, theta}` in the map frame (metres, radians)
- `velocity`: `{vx, vy, omega}` in body frame (m/s, rad/s)
- `arm_positions`: 6 joint angles in radians
- `control_mode`: current velocity mux mode
- `mission_status`: current mission state
- `rosbridge_connected`: whether the ROS bridge is live

## Camera Feeds

Three cameras available:
| Name | Source | Best for |
|------|--------|----------|
| `front` | Forward-facing USB cam | Navigation, obstacle detection |
| `wrist` | Arm-mounted cam | Manipulation, grasping |
| `bev` | Bird's-eye view (4-cam stitch) | Spatial layout, floor obstacles |

`describe_scene("front")` uses Claude vision to return a natural language description.
`get_camera_image("front")` returns a base64 JPEG you can process directly.
