# OhhO Shield

**Security for robots that touch the real world.**

- **Category:** Trust
- **Accent:** cyan
- **Live app:** [Open security dashboard](/shield)

Give every robot a hardware identity, encrypt its links, sign its updates, and
watch its software bill of materials for vulnerabilities — across the whole fleet.

## Overview (hero)

Security for robots that touch the real world. OhhO Shield gives every robot a
hardware identity, encrypts its links, signs its updates, and watches its software
bill of materials for vulnerabilities.

## Highlights

- Hardware-rooted device identity
- Mutually-authenticated encrypted links
- Signed OTA / secure boot
- SBOM + CVE monitoring
- Fleet-wide risk posture

## What you get

- A robot is a computer with wheels and an arm — and an attack surface to match. A
  compromised robot isn't a data breach; it's a physical-safety incident. OhhO
  Shield is the security layer for the whole fleet.
- Shield gives each robot a cryptographic identity rooted in hardware, establishes
  mutually-authenticated, encrypted channels for teleop and telemetry, and signs
  every over-the-air update so a robot only ever runs code you approved.
- It continuously inventories every robot's software bill of materials (SBOM),
  matches it against known CVEs, and surfaces a single risk posture across the
  fleet — turning security from a one-time audit into a live signal.

## Features

- **Device identity** — Each robot gets a hardware-rooted key and certificate — no
  shared passwords, no anonymous nodes.
- **Encrypted by default** — Teleop, telemetry and ROS traffic run over
  mutually-authenticated, encrypted channels.
- **Trusted boot & updates** — Secure boot and signed OTA ensure a robot only runs
  code with a valid signature.
- **SBOM & CVE watch** — An automatic software bill-of-materials per robot,
  continuously checked against vulnerability feeds.
- **Zero-trust access** — Role-based, audited access to robots and the fleet
  console, with SSO on enterprise plans.
- **One risk posture** — A live security score per robot and across the fleet,
  integrated into OhhO Fleet.

## How it works

1. **Enroll identity** — Each robot provisions a hardware-rooted key on first
   boot.
2. **Encrypt the links** — Shield establishes authenticated channels for all
   traffic.
3. **Sign the supply chain** — OTA bundles are signed; robots verify before
   applying.
4. **Monitor continuously** — SBOM + CVE scanning feeds a live posture and alerts.

## Specs

| Spec | Value |
|---|---|
| Identity | Hardware-rooted keys + X.509 certs |
| Transport | Mutually-authenticated TLS / encrypted DDS |
| Integrity | Secure boot + signed OTA |
| SBOM | Per-robot, CVE-matched |
| Access | RBAC + audit log (SSO on Forge) |
| Integrates | OhhO Fleet, OhhO Pilot |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | Encrypted links + signed updates | ✅ |
| Fleet | + device identity, SBOM / CVE monitoring | ✅ |
| Forge | + secure boot, SSO, on-prem | ✅ |

**Recommended plan: Fleet.** Any robot reachable over a network should at least be
on Builder for encrypted links and signed updates. Fleets in production want Fleet
for device identity and CVE monitoring; regulated or enterprise deployments choose
Forge for secure boot and SSO.

## FAQ

**Does Shield slow the robot down?**
Encryption and verification are designed for embedded targets; the security
overhead is negligible next to perception and control.

**Does it work with my existing ROS 2 stack?**
Yes. Shield layers onto standard ROS 2 / DDS and the ROSBridge transport OhhO
Pilot uses.

## Related products

- [OhhO Fleet](./fleet.md)
- [OhhO Pilot](./pilot.md)
- [OhhO Comply](./comply.md)
