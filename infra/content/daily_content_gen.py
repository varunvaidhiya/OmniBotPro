#!/usr/bin/env python3
"""
OmniBot Daily Content Generator
Analyzes recent repo activity, generates robotics engineering content,
and delivers to Discord via webhook stored in DISCORD_WEBHOOK_URL.
"""

import os
import subprocess
import sys
import time
import json
import urllib.request
import urllib.error
from datetime import datetime

DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL", "")
DISCORD_MAX_CHARS = 2000


# ── Git helpers ──────────────────────────────────────────────────────────────

def get_recent_commits(n: int = 10) -> str:
    result = subprocess.run(
        ["git", "log", "--oneline", f"-{n}"],
        capture_output=True, text=True
    )
    return result.stdout.strip()


def get_recent_diff_stat() -> str:
    result = subprocess.run(
        ["git", "show", "--stat", "HEAD"],
        capture_output=True, text=True
    )
    return result.stdout.strip()[:1500]


# ── Content ──────────────────────────────────────────────────────────────────

def build_content() -> dict:
    today = datetime.utcnow().strftime("%Y-%m-%d")

    daily_topic = (
        "Why Your Robot Controller App Should Have Built-In Observability"
    )

    core_idea = """\
When you're physically operating a robot in a lab or workspace, opening \
Grafana on a laptop is a workflow interruption you can't afford. \
OmniBot's Android controller app recently gained native Prometheus, \
AlertManager, Loki, and W&B integration — all accessible from the same \
screen used to drive the robot. The engineering insight is simple but \
underappreciated: **monitoring and control UX collapse into each other \
the moment you leave a desk and stand next to the physical system**.

The implementation exposes real tradeoffs. Four different backend APIs \
(Prometheus HTTP polling, AlertManager REST, Loki WebSocket live-tail, \
W&B API) now coexist in a single Android app, each with different auth \
models, polling cadences, and failure modes. The Prometheus stack scrapes \
from three machines (Pi :8888, GPU desktop :8889, DCGM exporter :9400) \
every 5 seconds — the Android app now implicitly depends on GPU desktop \
reachability, not just the Pi. Loki WebSocket live-tailing on a mobile \
device is battery-intensive but operationally invaluable: you see the \
exact log line that caused an anomaly while you're still looking at \
the robot. The broader lesson for robotics engineers: production deployment \
forces observability tooling to follow the operator, not the other way \
around."""

    twitter_prompt = """\
**Prompt for AI model — Twitter/X Thread**

Write a high-signal Twitter/X thread (6–8 tweets) from the perspective \
of an experienced robotics systems engineer. The topic is: why robot \
controller apps need built-in observability, not just separate dashboards.

Context to weave in naturally:
- OmniBot is a ROS 2 mecanum-wheel mobile manipulation robot (Pi 5 + \
  GPU workstation + SO-101 arm)
- Its Android controller app just gained native Prometheus, AlertManager, \
  Loki, and W&B integration — on the same screen used to drive the robot
- The Prometheus stack scrapes three machines: Pi (ROS 2 metrics), \
  GPU desktop (VLA/RL inference metrics), DCGM exporter (GPU VRAM/temp)
- Loki live-tail via WebSocket runs from the mobile app

Requirements:
- Strong hook in tweet 1: observation about a real operational problem \
  (context-switching between controller and dashboard while standing \
  next to a physical robot)
- Thread should explain the engineering tradeoff (richer situational \
  awareness vs. network coupling, battery, app complexity)
- One tweet on the specific failure mode this solves: "you see the \
  alert AFTER you've already caused the crash"
- One tweet on why this is different from web backend monitoring \
  (you're physically proximate to the system)
- One controversial/honest take: most robotics teams build monitoring \
  for their laptop, not for their operator's actual workflow
- Practical takeaway in final tweet
- No excessive emojis. No engagement bait. Sound like someone who \
  actually debugged this at 11pm in a lab.
- Tone: technical, direct, slightly opinionated"""

    substack_prompt = """\
**Prompt for AI model — Substack Long-Form Article**

Write a deep technical article titled:
"Why Robot Operators Need Observability at the Point of Control"

Audience: robotics engineers, ROS 2 developers, embodied AI researchers, \
startup robotics engineers. Not beginners. Assume familiarity with \
Prometheus, Grafana, ROS 2 topics, and Android development.

Article structure:

**Introduction** (~200 words)
Describe the real operational scenario: you're standing in a lab, \
controlling a mobile manipulation robot via an Android app over ROSBridge. \
The robot starts behaving oddly. Your monitoring dashboard is on a laptop \
across the room. By the time you check Grafana, the moment has passed. \
This is the observability UX gap that most robotics teams ignore.

**Problem Statement** (~300 words)
Traditional robotics monitoring assumes: operator at a desk, dashboard \
on a second monitor, physical separation from the robot. That assumption \
breaks down during commissioning, field testing, and real lab use. \
Explain the cognitive cost of context-switching between a robot controller \
and a monitoring tool when you're physically proximate to a live system.

**Why Existing Approaches Break** (~400 words)
- Grafana dashboards are designed for NOC-style monitoring, not \
  mobile operator workflows
- ROS 2 introspection tools (rqt, ros2 topic echo) require a terminal \
  and don't aggregate cross-machine state
- W&B dashboards require a browser session — not usable while controlling \
  a robot with a phone
- Most robotics teams wire up monitoring for post-hoc analysis, not \
  real-time operational situational awareness

**OmniBot Case Study** (~500 words)
Detail OmniBot's observability architecture:
- Prometheus scraping three machines: Pi (:8888 ROS 2 metrics bridge), \
  GPU desktop (:8889 VLA/RL inference metrics), DCGM exporter (:9400 \
  GPU VRAM/temp), VLA FastAPI (:8000/metrics), node_exporter on both machines
- AlertManager with defined alert rules: ControlLoopSlow (P95 > 55ms), \
  EmergencyStopActive (immediate critical), VLAInferenceSlow (> 2s), \
  GPUVRAMCritical (> 95%)
- Loki with promtail shipping ~/.ros/log from both machines
- Tempo receiving OTEL traces from LangGraph agent (Claude-backed \
  orchestration)
- W&B tracking training runs across SmolVLA, RL nav, RL arm pipelines

Then explain what was added: native Android integration of all four \
backends into the robot controller app (same app, same screen used to \
drive the robot via ROSBridge WebSocket). Four API clients: \
PrometheusApi, AlertManagerApi, LokiApi, WandBApi. A Flow-based \
ObservabilityRepository aggregating all four. Loki live-tail via \
WebSocket. W&B run list with MPAndroidChart metrics charts.

**Engineering Tradeoffs** (~400 words)
- Network coupling: the Android app now requires GPU desktop reachability, \
  not just Pi reachability. What happens if the GPU desktop is off?
- Battery: WebSocket live-tail is expensive on mobile
- Auth complexity: W&B requires API key in app preferences, \
  Prometheus/Loki assume local network (no auth) — a different threat model
- App complexity: four polling cadences, four failure modes, \
  four different data schemas to model
- The alternative (just use Grafana mobile) and why it fails for the \
  robotics use case

**Lessons Learned** (~300 words)
- Monitoring tooling should follow the operator's workflow, not assume \
  a fixed workstation
- The right question is not "is Grafana available?" but "where will the \
  operator actually be when something goes wrong?"
- In multi-machine robotics systems, the Android/controller app is \
  uniquely positioned to aggregate cross-machine state because it's \
  already connected to the local network
- Loki WebSocket live-tail from mobile is surprisingly practical: \
  latency is low on LAN, and seeing the exact log line while looking \
  at the robot is irreplaceable

**Future Directions** (~200 words)
- Push notifications from AlertManager directly to the controller app \
  (instead of polling)
- Embedding a minimal ROS 2 introspection view (topic echo, node list) \
  directly in the app
- Operator-facing alert severity triage built into the controller UI

**Final Takeaways** (bullet points)
Concise distillation of the core engineering lessons.

Tone: analytical, experience-driven, honest about tradeoffs. \
Do not oversell. Do not add motivational language. \
This is an engineering post for engineers."""

    visuals = """\
**Suggested Visuals**

1. Android app screenshot — Observability screen showing HEALTH tab \
   (Prometheus KPIs: robot vx/vy/omega, VLA latency, RL inference ms, \
   node cycle times)
2. Android app screenshot — ALERTS tab (AlertManager active alerts list, \
   silence button)
3. Android app screenshot — LOGS tab (Loki live-tail with filter field, \
   timestamped log entries)
4. Android app screenshot — TRAIN tab (W&B run list + MPAndroidChart \
   showing loss curve)
5. Architecture diagram — full observability data flow: Pi → promtail → \
   Loki; Pi/GPU → prometheus_bridge → Prometheus; GPU → DCGM → Prometheus; \
   LangGraph → OTEL → Tempo; All → Android app
6. Prometheus `prometheus.yml` screenshot showing the three-machine \
   scrape targets
7. AlertManager alert rules YAML (ControlLoopSlow, EmergencyStopActive, \
   VLAInferenceSlow rules)
8. Grafana dashboard screenshot — OmniBot Robot Health panel \
   (showing real data during a test run)
9. Terminal screenshot — Loki query via LogQL filtering for a specific \
   ROS 2 node failure"""

    cross_links = """\
**Cross-Link Opportunities**

Previous topics this connects to:
- W&B integration across SmolVLA + RL training pipelines \
  (just merged — training observability)
- LangGraph/Claude orchestration with OTEL traces to Tempo \
  (the distributed tracing layer)

Future topics this unlocks:
- Why Distributed Tracing Matters in Multi-Machine Robot Systems \
  (Tempo + OTEL in a ROS 2 context)
- Why AlertManager-to-Android Push Beats Dashboard Polling for Robot Ops
- Why ROS 2 Diagnostics Are Underused as a Metrics Source
- Why Multi-Machine ROS 2 Deployments Need a Network Topology Plan \
  Before They Need Features

Related engineering themes:
- Observability in distributed systems (applied to robotics)
- Mobile UI as a monitoring surface (not just a controller)
- Cross-machine state aggregation in heterogeneous systems (Pi + GPU)

YouTube expansion:
- "Building a Full Robotics Observability Stack: Prometheus + Loki + \
  Tempo + W&B on OmniBot" — walkthrough of the full infra/observability/ \
  directory, showing real scrape targets, alert rules, and the Android \
  integration live"""

    return {
        "date": today,
        "daily_topic": daily_topic,
        "core_idea": core_idea,
        "twitter_prompt": twitter_prompt,
        "substack_prompt": substack_prompt,
        "visuals": visuals,
        "cross_links": cross_links,
    }


# ── Discord delivery ─────────────────────────────────────────────────────────

def send_to_discord(content: str, webhook_url: str) -> bool:
    payload = json.dumps({"content": content}).encode("utf-8")
    req = urllib.request.Request(
        webhook_url,
        data=payload,
        headers={
            "Content-Type": "application/json",
            "User-Agent": "OmniBotContentBot/1.0 (discord-webhook-client)",
        },
        method="POST",
    )
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=15) as resp:
                return resp.status in (200, 204)
        except urllib.error.HTTPError as e:
            if e.code == 429:
                retry_after = float(e.headers.get("Retry-After", 2 ** attempt))
                time.sleep(retry_after)
            else:
                print(f"Discord HTTP error {e.code}: {e.read().decode()}", file=sys.stderr)
                return False
        except Exception as e:
            wait = 2 ** attempt
            print(f"Attempt {attempt+1} failed: {e}. Retrying in {wait}s…", file=sys.stderr)
            time.sleep(wait)
    return False


def chunk_message(header: str, body: str, max_chars: int = DISCORD_MAX_CHARS) -> list[str]:
    """Split a header+body into Discord-safe chunks."""
    full = f"{header}\n{body}"
    if len(full) <= max_chars:
        return [full]

    chunks = []
    current = header + "\n"
    for line in body.splitlines(keepends=True):
        if len(current) + len(line) > max_chars:
            chunks.append(current.rstrip())
            current = line
        else:
            current += line
    if current.strip():
        chunks.append(current.rstrip())
    return chunks


def deliver(content: dict, webhook_url: str) -> None:
    sections = [
        (
            f"# OmniBot Daily Content — {content['date']}",
            f"## Daily Topic\n**{content['daily_topic']}**",
        ),
        (
            "## Core Idea",
            content["core_idea"],
        ),
        (
            "## Twitter/X Post Prompt",
            content["twitter_prompt"],
        ),
        (
            "## Substack Article Prompt",
            content["substack_prompt"],
        ),
        (
            "## Suggested Visuals",
            content["visuals"],
        ),
        (
            "## Cross-Link Opportunities",
            content["cross_links"],
        ),
    ]

    for header, body in sections:
        for chunk in chunk_message(header, body):
            ok = send_to_discord(chunk, webhook_url)
            if not ok:
                print(f"Failed to send chunk starting: {chunk[:60]!r}", file=sys.stderr)
            time.sleep(0.5)


# ── Entry point ──────────────────────────────────────────────────────────────

def main() -> None:
    webhook_url = DISCORD_WEBHOOK_URL
    if not webhook_url:
        print("ERROR: DISCORD_WEBHOOK_URL environment variable is not set.", file=sys.stderr)
        sys.exit(1)

    print("Analysing repository…")
    print("Recent commits:")
    print(get_recent_commits())
    print()

    content = build_content()

    print(f"Topic: {content['daily_topic']}")
    print("Delivering to Discord…")
    deliver(content, webhook_url)
    print("Done.")


if __name__ == "__main__":
    main()
