# Training pipelines

OhhO OS does not stop at control. The same package collects demonstrations,
trains policies, and serves them back to the robot — one toolchain, one data
schema, end to end.

## The loop

```
record  →  train  →  verify  →  serve  →  (continual learning)
ohho.data  ohho.train         ohho.serve
```

## 1. Collect data

Record teleoperation episodes — base motion, arm joints and synchronized
multi-camera video — in the standard LeRobot dataset format (Parquet + MP4):

```python
from ohho.data import Recorder

rec = Recorder(bot, repo_id="local/mobile_manipulation")
rec.record_episode(task="pick up the cup")     # until you stop or discard
```

The recorded schema is the same one the trainer consumes and the policy runs in
production — what you collect is exactly what you deploy.

## 2. Train

Fine-tune the method that fits — behavior cloning, VLA fine-tuning (SmolVLA, ACT,
diffusion, OpenVLA), or reinforcement learning — selected by config, not a
rewrite:

```python
from ohho.train import finetune

ckpt = finetune(
    dataset="local/mobile_manipulation",
    policy="smolvla",
    device="auto",            # cuda / mps / cpu, resolved for you
)
```

You can also train entirely in simulation with domain randomization, then
fine-tune on real episodes for sim-to-real transfer.

## 3. Verify

Best-of-N evaluation with hard safety and reachability checks (against the real
hardware limits) gates what is allowed to ship.

## 4. Serve

Expose the policy behind a REST endpoint your robot calls with an image and an
instruction:

```bash
ohho serve --checkpoint ./checkpoints/smolvla --port 8000
```

```python
bot.run_policy("http://localhost:8000")        # closes the loop
```

## 5. Continual learning

A post-training loop can re-train as new episodes and tasks arrive, with
prioritized replay and outcome-stratified episodic memory — the robot keeps
getting better in the field.

## Hardware-aware everywhere

Every component takes `device="auto"` and selects the right execution provider
for your machine — TensorRT on Jetson, CUDA on a workstation GPU, CoreML on
Apple Silicon, CPU elsewhere.
