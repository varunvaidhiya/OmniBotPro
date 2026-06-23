# OhhO Care

**Fix it before it breaks.**

- **Category:** Operations
- **Accent:** cyan
- **Live app:** [Open maintenance](/care)

Predictive maintenance and service workflow for robot fleets. Turn
motor-degradation signals from OhhO Fleet into scheduled service, ordered parts and
logged repairs — closing the loop from monitoring to maintenance.

## Overview (hero)

OhhO Fleet tells you a motor is degrading; OhhO Care turns that signal into a
workflow. Predictive maintenance, parts ordering from the OhhO Build bill of
materials, technician dispatch, and a repair log that feeds back into OhhO Comply's
audit trail — closing the loop from 'something is wearing' to 'it's fixed and
documented.'

## Highlights

- Predictive maintenance alerts
- Auto-sourced parts from Build BOM
- Technician dispatch + scheduling
- Repair log → Comply audit trail
- Downtime tracking + MTBF

## What you get

- Monitoring tells you a robot is about to fail. Maintenance is what you do about
  it. OhhO Care is the product that bridges that gap — turning the degradation
  signals OhhO Fleet and OhhO Twin surface into a structured service workflow that
  ends with a fixed, documented robot.
- When a motor's temperature trend crosses a threshold or a joint's torque ripple
  changes, Care raises a predictive maintenance ticket — not a generic alert, but
  a structured work order with the affected robot, the suspect component, the
  predicted failure window, and the replacement part pulled from the robot's OhhO
  Build bill of materials. One click orders the part; another schedules a
  technician.
- When the repair is done, Care logs it: what was replaced, when, by whom, with
  what part batch — and that record flows straight into OhhO Comply's audit trail,
  so the robot's maintenance history is part of its certification evidence.
  Downtime, MTBF and mean-time-to-repair metrics roll up into the Fleet dashboard,
  so you see the operational cost of maintenance, not just the technical signals.

## Features

- **Predictive alerts** — Degradation signals from Fleet and Twin — motor
  temperature, torque ripple, vibration — become structured work orders, not just
  alerts.
- **Parts from your BOM** — The replacement part is pulled from the robot's OhhO
  Build bill of materials, with supplier links and lead times — so ordering is one
  click, not a scavenger hunt.
- **Technician dispatch** — Schedule a field service visit, assign a technician
  and block the robot's calendar — all from the work order.
- **Repair logging** — Every repair is logged with the part replaced, the
  timestamp, the technician and the part batch — and flows into OhhO Comply's audit
  trail.
- **Downtime + MTBF** — Care tracks downtime, mean-time-between-failures and
  mean-time-to-repair per robot and across the fleet, surfaced in the Fleet
  dashboard.
- **Closes the loop** — Fleet detects, Care schedules, Bench calibrates the
  replacement, Comply records it. The whole maintenance lifecycle, one platform.

## How it works

1. **Detect** — Fleet and Twin surface a degradation signal — a motor running hot,
   a joint drifting.
2. **Triage** — Care raises a work order with the affected robot, the suspect
   component and the predicted failure window.
3. **Order + dispatch** — One click orders the replacement part from the Build
   BOM; another schedules the technician.
4. **Repair + log** — The repair is logged and flows into Comply's audit trail;
   MTBF and downtime update in Fleet.

## Specs

| Spec | Value |
|---|---|
| Signals | Motor temp, torque ripple, vibration (from Fleet + Twin) |
| Parts | Auto-sourced from OhhO Build BOM |
| Workflow | Work order → dispatch → repair → log |
| Metrics | Downtime, MTBF, MTTR per robot + fleet |
| Audit | Repair log → OhhO Comply audit trail |
| Integrates | OhhO Fleet, Twin, Build, Comply |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | Basic maintenance scheduling | ✅ |
| Fleet | Predictive maintenance + parts ordering | ✅ |
| Forge | Full service workflow + SLA tracking | ✅ |

**Recommended plan: Fleet.** Care is most valuable when you're running a real
fleet and need predictive maintenance, not just a calendar reminder. Builder gives
you basic scheduling for a few robots; Fleet adds predictive alerts, auto-sourced
parts and MTBF metrics. Enterprises with field service teams choose Forge for SLA
tracking and custom workflows.

## FAQ

**How does Care know a part is about to fail?**
Care reads degradation signals from OhhO Fleet (motor temperature, current draw,
torque limits) and OhhO Twin (vibration trends, trajectory deviation). When a
signal crosses a learned threshold, Care raises a predictive work order.

**Does Care order parts for me?**
Care pulls the replacement part from the robot's OhhO Build bill of materials —
with supplier links and lead times. One click takes you to the supplier's
checkout. On Forge, ordering can be fully automated.

**How does Care relate to Comply?**
Every repair Care logs — what was replaced, when, by whom, with what batch — flows
into OhhO Comply's audit trail, so the robot's maintenance history is part of its
certification evidence.

## Related products

- [OhhO Fleet](./fleet.md)
- [OhhO Twin](./twin.md)
- [OhhO Comply](./comply.md)
