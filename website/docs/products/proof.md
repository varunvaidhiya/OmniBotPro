# OhhO Proof

**Prove the robot is safe before it ships.**

- **Category:** Trust
- **Accent:** violet
- **Live app:** [Open validation suite](/proof)

Run your robot through thousands of simulated scenarios, track regression and
coverage, and assemble the evidence into a versioned safety case.

## Overview (hero)

Prove the robot is safe before it ships. OhhO Proof runs your robot through
thousands of simulated scenarios, tracks regression and coverage, and assembles
the evidence into a safety case.

## Highlights

- Scenario-based simulation testing
- Regression vs known-good builds
- Coverage map of tested conditions
- Failure / fault injection
- Versioned safety-case report

## What you get

- You wouldn't ship software without tests; a robot deserves more. OhhO Proof is
  the testing and validation product — it puts your robot through scenario-based
  trials in simulation and on hardware, and tells you, with evidence, whether it's
  ready.
- Proof runs large batches of randomized scenarios — navigation, manipulation,
  edge cases and failure injection — across Gazebo and Isaac Sim, scoring each run
  against safety and task criteria. A coverage map shows what you've actually
  tested, not what you hope you have.
- Every release is checked for regressions against the last known-good build, and
  the results roll up into a versioned safety case you can hand to QA, to OhhO
  Comply for certification, or to a customer who needs assurance.

## Features

- **Scenario suites** — Thousands of randomized navigation and manipulation
  scenarios across Gazebo and Isaac Sim.
- **Pass / fail criteria** — Score each run against task success, collisions,
  safety-zone and timing criteria.
- **Regression tracking** — Every build is compared to the last known-good;
  regressions are flagged before release.
- **Coverage map** — See which conditions — speeds, payloads, lighting, layouts —
  you've actually exercised.
- **Fault injection** — Inject sensor dropouts, latency and actuator faults to
  test failure handling.
- **Safety case** — Results assemble into a versioned report that feeds OhhO
  Comply and your QA sign-off.

## How it works

1. **Define scenarios** — Pick suites or describe the conditions your robot must
   handle.
2. **Run at scale** — Proof executes batches across simulators in parallel.
3. **Read the coverage** — See pass rates, regressions and the coverage map.
4. **Export the evidence** — Generate a safety-case report for QA and Comply.

## Specs

| Spec | Value |
|---|---|
| Simulators | Gazebo Harmonic + Isaac Sim |
| Scenarios | Navigation, manipulation, edge cases, faults |
| Scoring | Task success, collisions, safety zones, timing |
| Regression | Versus last known-good build |
| Coverage | Condition-space coverage map |
| Output | Versioned safety-case report |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Single-scenario sim tests | ✅ |
| Builder | Scenario suites + regression | ✅ |
| Fleet | Large-batch + coverage + fault injection | ✅ |
| Forge | Custom scenarios + safety-case sign-off | ✅ |

**Recommended plan: Fleet.** Hobby projects can validate single scenarios on
Spark. Teams shipping to real users want Fleet for large-batch testing, coverage
and fault injection; safety-critical programs choose Forge for custom scenarios and
formal sign-off.

## FAQ

**Do I need hardware to test?**
No. Proof runs primarily in simulation, with optional hardware-in-the-loop runs
for final validation.

**How does it relate to Comply?**
Proof produces the test evidence; Comply files it as part of the certification
record.

## Related products

- [OhhO Comply](./comply.md)
- [OhhO Data](./data.md)
- [OhhO Fleet](./fleet.md)
