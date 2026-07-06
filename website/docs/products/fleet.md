# OhhO Fleet

**Update 50 robots like you update an app.**

- **Category:** Operations
- **Accent:** cyan
- **Live app:** [Open mission control](/fleet)

Fleet management, signed over-the-air updates, VDA 5050 warehouse integration,
Open-RMF multi-robot coordination, and a full observability stack — Prometheus,
Grafana and alerting, pre-configured.

## Overview (hero)

Update 50 robots like you update an app. Fleet management, signed over-the-air
updates, VDA 5050 warehouse integration, Open-RMF multi-robot coordination, and
a full observability stack — Prometheus, Grafana and alerting, pre-configured.

## Highlights

- Fleet health dashboard
- Signed OTA updates (code + models)
- Staged / canary rollouts
- VDA 5050 compatible (AGV/AMR fleet standard)
- Open-RMF multi-robot coordination
- Prometheus + Grafana + alerts
- Per-robot telemetry

## What you get

- One robot is a project; fifty is an operation. OhhO Fleet is mission control for
  a whole fleet — see every robot's health, push software and AI-model updates
  over the air, and get paged before a problem becomes an outage.
- Fleet ships the observability stack already wired: Prometheus scraping, Grafana
  dashboards, Loki, Tempo and AlertManager. Robot telemetry, GPU metrics and VLA
  latency all land in one place.
- Over-the-air updates cover both the ROS 2 workspace and ONNX policy models, with
  staged rollouts so you can canary a release to a few robots before it reaches
  the rest. And because Fleet speaks the VDA 5050 AGV/AMR standard over MQTT, your
  robots integrate with the warehouse management systems and master control
  software your facility already runs — while Open-RMF handles multi-robot
  traffic management, task allocation and conflict-free navigation across mixed
  fleets.

## Features

- **Single pane of glass** — Every robot's status, version, battery and
  last-seen in one dashboard.
- **OTA for code and models** — Update the ROS 2 workspace and ONNX policies
  remotely — no field visits.
- **Staged rollouts** — Canary to a subset, watch the metrics, then roll forward
  or back.
- **VDA 5050 warehouse integration** — Speaks the VDA 5050 AGV/AMR fleet standard
  over MQTT — so OhhO Fleet interoperates with warehouse management systems and
  master control software from Linde, Toyota, MiR, KION and the rest of the VDA
  5050 ecosystem. Your robots join the fleet your warehouse already runs.
- **Open-RMF multi-robot coordination** — Open Robotics Middleware Framework
  integration for multi-robot traffic management, task allocation and
  conflict-free navigation — so mixed fleets from different vendors share the
  same floor without collisions or deadlocks.
- **Observability included** — Prometheus, Grafana, Loki, Tempo and AlertManager,
  pre-provisioned with dashboards.
- **Alerting that pages** — Rules for offline robots, latency spikes and resource
  exhaustion route to email or Slack.

## How it works

1. **Enroll robots** — Each robot reports telemetry to the Fleet stack.
2. **Watch the fleet** — Health, versions and metrics on pre-built dashboards.
3. **Roll out updates** — Push signed OTA bundles; canary first.
4. **Get alerted** — AlertManager pages you before users notice.

## Specs

| Spec | Value |
|---|---|
| Metrics | Prometheus (robot compute, GPU, VLA, DCGM) |
| Dashboards | Grafana (auto-provisioned) |
| Logs / traces | Loki + Tempo |
| Alerting | AlertManager → email / Slack |
| OTA | Workspace + ONNX models, signed |
| Rollouts | Staged / canary |
| Fleet protocol | VDA 5050 over MQTT |
| Coordination | Open-RMF compatible (multi-robot) |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | Not included | ❌ |
| Fleet | Up to 100 robots, OTA | ✅ |
| Forge | Unlimited robots, on-prem | ✅ |

**Recommended plan: Fleet.** Fleet is the plan named for this product: up to 100
robots with OTA and the full observability stack. Above 100 robots, or to run it
all on-prem, choose Forge.

## FAQ

**Can I host the observability stack myself?**
Yes. It deploys via Docker Compose on your own infrastructure; Forge adds on-prem
licensing and SSO.

**Are OTA updates safe?**
Updates are signed and staged; pair with OhhO Shield for end-to-end supply-chain
integrity.

**Does Fleet integrate with my warehouse management system?**
Yes. Fleet speaks the VDA 5050 AGV/AMR fleet standard over MQTT — the same
standard used by Linde, Toyota, MiR, KION and other major warehouse robotics
vendors. Your robots appear in your WMS as standard VDA 5050 vehicles, and master
control software can dispatch them alongside any other compliant AGV.

**Can Fleet coordinate mixed fleets from different vendors?**
Yes. Open-RMF integration provides multi-robot traffic management, task
allocation and conflict-free navigation across mixed fleets — so robots from
different vendors share the same floor without collisions or deadlocks.

## Related products

- [OhhO Serve](./serve.md)
- [OhhO Shield](./shield.md)
- [OhhO Proof](./proof.md)
- [OhhO Twin](./twin.md)
- [OhhO Care](./care.md)
