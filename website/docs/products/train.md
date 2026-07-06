# OhhO Train

**Turn demonstrations into policies.**

- **Category:** Intelligence
- **Accent:** cyan
- **Live app:** [Open training console](/train)

The training engine behind the stack. Fine-tune VLA, imitation and
reinforcement-learning policies from your OhhO Data episodes or in simulation —
then export straight to Serve and Fleet.

## Overview (hero)

OhhO Data collects the demonstrations; OhhO Serve runs the model — Train is the
engine in between. Fine-tune Vision-Language-Action, imitation-learning and
reinforcement-learning policies from your own episodes or in simulation, track
every run, and export a deployment-ready checkpoint.

## Highlights

- Fine-tune VLA, ACT, diffusion & RL policies
- Trains from OhhO Data or in simulation
- Built on the open OmniVLA engine
- Experiment tracking & sweeps (W&B)
- Exports to Serve + ONNX for Fleet OTA

## What you get

- A policy is only as good as the loop that produced it. OhhO Train is the
  standardized training engine for embodied AI — the productized OmniVLA engine —
  so you fine-tune real models without stitching together a different toolchain
  for every method.
- Train covers the methods that matter: behavior cloning and VLA fine-tuning
  (SmolVLA, ACT, diffusion, OpenVLA) on the LeRobot datasets you record with OhhO
  Data, plus reinforcement learning in Isaac Lab with domain randomization for
  sim-to-real. A continual-learning loop can re-train as new episodes and tasks
  arrive, with replay, multi-objective rewards and AI-judged self-evaluation.
- Every run is tracked — losses, success rate, evaluation — with Weights & Biases
  sweeps to find good hyperparameters. When a checkpoint passes verification,
  Train exports it in the format the rest of the stack expects: a checkpoint OhhO
  Serve loads directly, or an ONNX policy OhhO Fleet ships over the air.

## Features

- **Many methods, one engine** — Behavior cloning, VLA fine-tuning (SmolVLA / ACT
  / diffusion / OpenVLA), offline RL and on-policy RL — selected by config, not a
  rewrite.
- **Trains from your data** — Point Train at a LeRobot dataset from OhhO Data, or
  generate experience in Gazebo and Isaac Sim with domain randomization for
  sim-to-real.
- **Continual learning** — A post-training loop re-trains as new episodes and
  tasks arrive, with prioritized replay and outcome-stratified episodic memory.
- **Rewards & self-evaluation** — Multi-objective rewards (task, safety,
  efficiency, smoothness) plus vision rewards and AI judges score behavior, not
  just loss.
- **Tracked & reproducible** — Losses, success rate and eval stream to Weights &
  Biases; Bayesian sweeps search hyperparameters for you.
- **Export-ready** — Verified checkpoints export to OhhO Serve and to ONNX for
  OhhO Fleet OTA, with hardware-aware execution providers baked in.

## How it works

1. **Choose method & data** — Pick a policy type and point Train at an OhhO Data
   dataset — or a simulation task.
2. **Train & track** — Launch the run; losses, success rate and eval stream to
   W&B in real time.
3. **Verify** — Best-of-N evaluation with hard safety and reachability checks
   gates what's allowed to ship.
4. **Export & deploy** — Push the checkpoint to OhhO Serve, or export ONNX for
   OhhO Fleet to roll out.

## Specs

| Spec | Value |
|---|---|
| Methods | BC, VLA fine-tune, ACT, diffusion, offline + online RL |
| Data sources | OhhO Data (LeRobot), Gazebo, Isaac Sim |
| Foundation | Open-source OmniVLA engine |
| Tracking | Weights & Biases + Bayesian sweeps |
| Hardware | Your GPU — we help you size it or recommend a rig |
| Export | Serve checkpoint + ONNX for Fleet OTA |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Local training, single run | ✅ |
| Builder | Cloud training + experiment tracking | ✅ |
| Fleet | + sweeps & continual-learning loop | ✅ |
| Forge | On-prem cluster + managed training | ✅ |

**Recommended plan: Builder.** Builder gives you cloud training with full
experiment tracking — enough to fine-tune a first real policy. Teams running
sweeps or standing up a continual-learning loop want Fleet; enterprises training
on their own cluster choose Forge.

## FAQ

**Is the training engine open?**
Yes. Train is built on the open-source OmniVLA engine, so your training code and
checkpoints are portable — you're never locked in.

**Do I need real-robot data to start?**
No. You can train entirely in simulation with domain randomization, then fine-tune
on real OhhO Data episodes for sim-to-real transfer.

## Related products

- [OhhO Data](./data.md)
- [OhhO Serve](./serve.md)
- [OhhO Proof](./proof.md)
- [OhhO Market](./market.md)
