---
name: omnibot_control
description: System-level control — modes, emergency stop, missions, and AI commands
version: 1.0.0
tools:
  - set_control_mode
  - emergency_stop
  - cancel_mission
  - send_ai_command
  - get_robot_state
---
# OmniBot System Control Skill

Use this skill for safety, mode switching, and high-level mission control.

## Control Modes

Switch modes before issuing motion commands:

| Mode | Description |
|------|-------------|
| `nav2` | Autonomous navigation via Nav2 planner (default) |
| `vla` | VLA policy drives the base |
| `teleop` | Manual teleoperation (joystick / Android) |
| `rl_nav` | RL navigation policy (short-range, ≤3 m goals) |

```
set_control_mode("nav2")   # before navigating
set_control_mode("teleop") # before Android joystick
```

## Emergency Stop

```
emergency_stop(true)   # halt all motion immediately
emergency_stop(false)  # clear e-stop and resume
```

Always clear the e-stop after the hazard is resolved.

## AI Agent Commands

Send natural language to the LangGraph orchestration agent (requires `use_langchain:=true`):

```
send_ai_command("find the red cup and bring it to the kitchen table")
send_ai_command("patrol the lab and report any obstacles")
```

Monitor the result via `get_robot_state()` → `ai_status` and `mission_status`.
