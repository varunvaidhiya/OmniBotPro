# OhhO Fleet

**Update 50 robots like you update an app.**

- **Category:** Operations
- **Accent:** cyan
- **Live app:** [Open mission control](/fleet)

Fleet management, signed over-the-air updates and a full observability stack —
Prometheus, Grafana and alerting, pre-configured.

## Overview (hero)

Update 50 robots like you update an app. Fleet management, signed over-the-air
updates and a full observability stack — Prometheus, Grafana and alerting,
pre-configured.

## Highlights

- Fleet health dashboard
- Signed OTA updates (code + models)
- Staged / canary rollouts
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
  the rest.

## Features

- **Single pane of glass** — Every robot's status, version, battery and last-seen
  in one dashboard.
- **OTA for code and models** — Update the ROS 2 workspace and ONNX policies
  remotely — no field visits.
- **Staged rollouts** — Canary to a subset, watch the metrics, then roll forward
  or back.
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
| Metrics | Prometheus (Pi, GPU, VLA, DCGM) |
| Dashboards | Grafana (auto-provisioned) |
| Logs / traces | Loki + Tempo |
| Alerting | AlertManager → email / Slack |
| OTA | Workspace + ONNX models, signed |
| Rollouts | Staged / canary |

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

## Related products

- [OhhO Serve](./serve.md)
- [OhhO Shield](./shield.md)
- [OhhO Proof](./proof.md)
- [OhhO Twin](./twin.md)
- [OhhO Care](./care.md)
