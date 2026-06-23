# OhhO Serve

**Robot AI inference, as an API.**

- **Category:** Intelligence
- **Accent:** cyan
- **Live app:** [Open the console](/serve)

Deploy a Vision-Language-Action model behind a REST endpoint in one command.
Pluggable model backends, batching and Prometheus metrics built in.

## Overview (hero)

Robot AI inference, as an API. Deploy a Vision-Language-Action model behind a REST
endpoint in one command — pluggable model backends, batching, and Prometheus
metrics built in.

## Highlights

- One-command model deploy
- REST predict API
- Pluggable backends (OpenVLA, SmolVLA, ACT…)
- 4-bit quantization option
- Prometheus metrics built in

## What you get

- Modern robots run on large Vision-Language-Action (VLA) models, but getting one
  into production means GPU servers, model loading, batching, versioning and
  monitoring. OhhO Serve turns all of that into a single endpoint.
- Point Serve at a model — OpenVLA, SmolVLA, ACT, a diffusion policy, or your own
  fine-tune — and it exposes a clean REST API your robot calls with an image and
  an instruction, and gets back an action. Swap models without touching robot
  code.
- Serve is built for the real world: health checks, hot model loading, 4-bit
  quantization for tighter VRAM budgets, and first-class Prometheus metrics so you
  can see latency and throughput at a glance.

## Features

- **Model-agnostic backend** — OpenVLA, SmolVLA, ACT, diffusion, or a custom
  class — selected by config, not code.
- **Clean predict API** — POST an image + instruction, get back an action vector.
  Your robot doesn't need to know which model is behind it.
- **Hot loading & health** — /health, /load_model and /predict endpoints let you
  swap or reload models with zero downtime.
- **Fits your VRAM** — Optional 4-bit loading runs 7B-class models on a single GPU
  with 16 GB or more.
- **Observability first** — Latency, throughput and GPU metrics export to
  Prometheus and the OhhO Fleet dashboards.

## How it works

1. **Choose a model** — Set the model class and checkpoint via environment
   variables.
2. **Launch the server** — One command starts the FastAPI inference server.
3. **Call /predict** — Your robot sends camera + instruction and receives an
   action.
4. **Watch it scale** — Metrics stream to Prometheus / Grafana; raise call limits
   as you grow.

## Specs

| Spec | Value |
|---|---|
| API | REST — /health, /load_model, /predict |
| Backends | OpenVLA, SmolVLA, ACT, diffusion, custom |
| Quantization | Optional 4-bit |
| Hardware | NVIDIA GPU, 16 GB+ VRAM |
| Metrics | Prometheus /metrics endpoint |
| Deploy | Docker, single command |

## Plans

| Plan | What this product gives you | Included |
|---|---|---|
| Spark | Not included | ❌ |
| Builder | 500 API calls / day | ✅ |
| Fleet | 10K API calls / day | ✅ |
| Forge | On-prem license, unlimited | ✅ |

**Recommended plan: Fleet.** Builder's 500 calls/day is for prototyping. If real
robots are calling the model in production, choose Fleet for 10K/day — or Forge
for an on-prem license with no metering.

## FAQ

**Can I run my own fine-tuned model?**
Yes. Serve loads any compatible checkpoint, including models you've fine-tuned
with OhhO Data and the OmniVLA engine.

**Where does inference run?**
On your GPU — desktop, server or cloud. Serve is software; you keep the model and
the data.

## Related products

- [OhhO Train](./train.md)
- [OhhO Data](./data.md)
- [OhhO View](./view.md)
