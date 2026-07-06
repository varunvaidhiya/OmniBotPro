# OhhO Market

**Download a skill. Or sell one.**

- **Category:** Intelligence
- **Accent:** cyan
- **Live app:** [Open the marketplace](/market)

A cross-brand marketplace for trained robot skills and policies. Download a
verified pick-and-place policy for your humanoid, or publish one you trained with
OhhO Train — signed, safety-checked and robot-ready.

## Overview (hero)

OEM app stores are per-robot. OhhO Market is the cross-brand equivalent for
trained behaviors — a marketplace where you download a verified policy for your
specific robot, or publish one you trained with OhhO Train. Every skill is
signed, safety-checked through OhhO Proof, and tagged by robot model, task and
success rate.

## Highlights

- Cross-brand skill marketplace
- Verified, signed policies
- Tagged by robot + task
- One-click deploy via Serve
- Sell skills, take-rate model

## What you get

- A trained policy is the most valuable artifact in robotics — and today, every
  team trains their own from scratch. OhhO Market changes that. It's a
  marketplace where a verified pick-and-place policy for a humanoid, a patrol
  skill for a quadruped, or a welding trajectory for an industrial arm can be
  downloaded, deployed and monetized.
- Every skill on Market is produced through the OhhO pipeline: trained with OhhO
  Train, validated through OhhO Proof's scenario suites, signed with OhhO Shield's
  supply-chain keys, and tagged with the robot models it runs on, the task it
  performs, and its measured success rate. You know what you're buying before you
  download it.
- Market creates a network effect that compounds: more robots on the platform
  attract more skill authors, more skills attract more robot owners, and the
  take-rate model rewards both. For a startup that just bought a humanoid,
  Market means deploying a working skill on day one instead of spending three
  months collecting data and training.

## Features

- **Cross-brand, not per-robot** — Unlike OEM app stores, Market spans every
  robot the platform supports. A skill tagged for 'any mecanum base' works on a
  mecanum manipulator, a research robot and a custom AMR alike.
- **Verified, not posted** — Every published skill passes through OhhO Proof's
  scenario suites before it's listed — so the success rate on the listing is the
  measured rate, not a marketing claim.
- **Signed and tamper-proof** — Each skill package is signed with OhhO Shield's
  supply-chain keys, so a robot verifies the skill's integrity before loading it
  via OhhO Serve.
- **One-click deploy** — Download a skill and Serve loads it — no manual
  checkpoint conversion, no model-class mismatch. The skill package carries its
  model class and config.
- **Sell or share** — Authors set a price or publish for free. Market handles
  licensing, versioning and robot-model compatibility checks. The take-rate funds
  the platform.
- **Training-to-market loop** — Train a skill with OhhO Train, validate with OhhO
  Proof, sign with OhhO Shield, publish to Market — the whole pipeline is one
  platform.

## How it works

1. **Browse skills** — Filter by robot model, task type, success rate and price —
   or search 'pick and place' for your humanoid.
2. **Verify the claim** — Each listing shows the Proof scenario results, the
   training data size and the measured success rate.
3. **Deploy** — Download the signed skill package and OhhO Serve loads it — or
   push it to your fleet via OhhO Fleet OTA.
4. **Publish your own** — Train with OhhO Train, pass OhhO Proof, sign with OhhO
   Shield, and list on Market — free or priced.

## Specs

| Spec | Value |
|---|---|
| Listing format | Signed skill package (checkpoint + config + Proof report) |
| Compatibility | Tagged by robot model, brand and locomotion type |
| Verification | OhhO Proof scenario suites (pass rate published) |
| Signing | OhhO Shield supply-chain signatures |
| Deploy | OhhO Serve load or OhhO Fleet OTA |
| Model | Free + paid listings, platform take-rate |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Browse + free skills | ✅ |
| Builder | Download paid skills | ✅ |
| Fleet | Sell skills + team licenses | ✅ |
| Forge | Private marketplace + white-label | ✅ |

**Recommended plan: Builder.** Anyone can browse and download free skills on
Spark. Builder adds paid skills — most teams want at least one commercial policy
to skip months of training. Teams selling skills or buying team licenses choose
Fleet; enterprises running a private marketplace choose Forge.

## FAQ

**How is this different from an OEM app store?**
OEM stores are per-robot apps for one brand's hardware only. Market is
cross-brand — a skill tagged 'any mecanum base' works on any compatible robot,
not just one OEM's. And every skill is verified through OhhO Proof, not just
posted.

**Can I sell a skill I trained?**
Yes. Train with OhhO Train, pass OhhO Proof's scenario suites, sign with OhhO
Shield, and list it on Market at any price. The platform take-rate funds
verification and hosting.

**What if a skill doesn't work on my robot?**
Every listing is tagged with compatible robot models. Market checks compatibility
before download, and the Proof report shows the exact scenarios the skill was
tested in.

## Related products

- [OhhO Train](./train.md)
- [OhhO Serve](./serve.md)
- [OhhO Proof](./proof.md)
