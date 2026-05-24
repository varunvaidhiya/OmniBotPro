---
name: omnibot_navigate
description: Navigate the OmniBot robot base to named locations or map coordinates
version: 1.0.0
tools:
  - navigate_to_location
  - navigate_to_pose
  - navigate_then_manipulate
  - list_locations
  - get_robot_state
  - cancel_mission
---
# OmniBot Navigation Skill

Use this skill to move the OmniBot robot base autonomously using Nav2.

## Workflow

1. Call `list_locations` to see all available named destinations.
2. Call `navigate_to_location` with the exact location name returned.
3. For combined navigation + manipulation missions, use `navigate_then_manipulate`.
4. Monitor progress with `get_robot_state` (check `mission_status`).
5. Call `cancel_mission` to abort if needed.

## Notes

- Navigation is asynchronous — the tool returns immediately after sending the command.
- `mission_status` cycles through: `idle → navigating → done/failed`.
- Use `navigate_to_pose` when you have a specific map coordinate (e.g. from a previously saved observation).
- The robot's Nav2 planner requires a valid SLAM map to be running.
