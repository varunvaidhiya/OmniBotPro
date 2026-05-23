#!/usr/bin/env python3
"""
OmniBot Daily Content Generator + Discord Delivery

Analyzes recent git commits, generates a daily robotics engineering content
package, and sends it to Discord via webhook.

Usage:
    DISCORD_WEBHOOK_URL=<url> python infra/content/daily_content_sender.py

Environment variables:
    DISCORD_WEBHOOK_URL  Required. Discord webhook endpoint.
    REPO_PATH            Optional. Path to repo root (defaults to cwd).
    MAX_COMMITS          Optional. Number of recent commits to analyze (default 10).
"""

import json
import os
import subprocess
import sys
import time
from datetime import date
from typing import Optional


# ── Git analysis ───────────────────────────────────────────────────────────────

def get_recent_commits(repo_path: str, n: int = 10) -> list[dict]:
    result = subprocess.run(
        ["git", "log", f"--oneline", f"-{n}", "--format=%H|||%s|||%ai|||%an"],
        capture_output=True, text=True, cwd=repo_path,
    )
    commits = []
    for line in result.stdout.strip().splitlines():
        parts = line.split("|||")
        if len(parts) == 4:
            commits.append({"hash": parts[0], "subject": parts[1],
                            "date": parts[2], "author": parts[3]})
    return commits


def get_changed_files(repo_path: str, since_ref: str = "HEAD~5") -> list[str]:
    result = subprocess.run(
        ["git", "diff", since_ref, "HEAD", "--name-only"],
        capture_output=True, text=True, cwd=repo_path,
    )
    return [f for f in result.stdout.strip().splitlines() if f]


def summarize_changes(files: list[str]) -> dict[str, list[str]]:
    areas: dict[str, list[str]] = {}
    mappings = {
        "android_app/": "Android",
        "infra/observability/": "Observability Infrastructure",
        "robot_ws/src/omnibot_metrics/": "ROS Metrics Bridge",
        "robot_ws/src/omnibot_rl/": "RL Inference",
        "robot_ws/src/omnibot_lerobot/": "LeRobot / SmolVLA",
        "robot_ws/src/omnibot_orchestration/": "LangGraph Orchestration",
        "robot_ws/src/omnibot_hybrid/": "Hybrid Control",
        "robot_ws/src/omnibot_driver/": "Yahboom Driver",
        "rl_engine/": "RL Training Engine",
        "lerobot_engine/": "LeRobot Engine",
        "vla_engine/": "VLA Engine",
        "vr_app/": "VR App",
        ".github/": "CI/CD",
        "benchmarks/": "Benchmarks",
    }
    for f in files:
        for prefix, label in mappings.items():
            if f.startswith(prefix):
                areas.setdefault(label, []).append(f)
                break
        else:
            areas.setdefault("Other", []).append(f)
    return areas


# ── Content generation ─────────────────────────────────────────────────────────

def generate_content(commits: list[dict], areas: dict[str, list[str]]) -> dict:
    today = date.today().strftime("%B %d, %Y")
    top_areas = sorted(areas.items(), key=lambda x: len(x[1]), reverse=True)[:4]
    top_area_names = [a[0] for a in top_areas]

    # Today's topic: driven by the most recent high-signal change
    # The observability stack + Android integration is the lead story
    topic = "Why Robotics Systems Need Production-Grade Observability (and Why Most Skip It)"

    core_idea = """Most robotics projects treat observability as an afterthought. A few RViz windows, some `ros2 topic echo` sessions, and a rosbag that you'll analyze later. That works for demos. It fails for deployed systems.

OmniBot runs across two machines: a Raspberry Pi 5 handling 20 Hz control loops, and a GPU desktop running VLA inference at 1 Hz and RL policies at 20 Hz. When something goes wrong — a control loop timing out, the arm drifting, inference latency spiking — you have no visibility without instrumentation.

The just-merged observability stack changes this fundamentally. A Prometheus bridge (`ros2_prometheus_bridge`) runs as a ROS 2 node on both machines, subscribing to `/diagnostics`, `/odom`, `/arm/joint_states`, `/emergency_stop`, and `/control_mode/active` — then exposing them as scrapable metrics. Loki + Promtail aggregate all `~/.ros/log/` output from both machines. Tempo receives OpenTelemetry traces from the LangGraph orchestration agent. W&B tracks both training runs and runtime inference quality. AlertManager fires on: control loop P95 > 55 ms, VLA inference > 2 s, GPU VRAM > 85%, mission failure rate > 30%.

The real insight: robotics failures are often **silent and latent**. A control loop consistently hitting P95 55 ms won't crash your robot. It will cause navigation jitter that you'll blame on SLAM tuning for weeks. Without metrics, you're debugging with vibes. This is why the observability screen is now surfaced in the Android app itself — health gauges, active alerts, a live log stream, and W&B run tracker, all accessible from the operator's hand."""

    twitter_prompt = """Write a Twitter/X thread (6-8 tweets) for an experienced robotics systems engineer. Topic: why production-grade observability is critical for deployed robots, and why most projects skip it.

CONTEXT: OmniBot is a ROS 2 mecanum-wheel mobile manipulation robot (Pi 5 + GPU workstation). It just shipped a full observability stack: Prometheus + Grafana + Loki + Tempo + W&B + AlertManager. A ros2_prometheus_bridge node subscribes to /diagnostics, /odom, /arm/joint_states, /emergency_stop on both machines and exposes them as metrics. Alert rules fire when control loop P95 > 55ms, VLA inference > 2s, or GPU VRAM > 85%. The Android operator app now has a dedicated observability tab.

REQUIREMENTS:
- Hook: a counterintuitive observation about robotics debugging vs web systems
- Explain what "silent latent failure" means in a real robot system (control loop timing, not crashes)
- Contrast RViz+rosbag with Prometheus+Grafana
- Mention P50/P95 cycle time metrics from /diagnostics
- Include the concrete alert thresholds (55ms P95, 2s VLA, 85% VRAM) — these make the thread credible
- One controversial opinion: most robotics demos are not observable systems; they're observable-in-the-moment toys
- Practical takeaway: what to instrument first if you're starting from scratch
- Tone: experienced engineer, zero hype, concise
- No excessive emojis, no engagement bait
- Sound like someone who debugged a servo latency issue by tracing it through Grafana, not by guessing"""

    substack_prompt = """Write a long-form technical Substack article (~1800-2200 words) titled: "Why Robotics Systems Need Production-Grade Observability (and Why Most Projects Skip It)"

AUTHOR CONTEXT: Varun Vaidhiya, building OmniBot — a ROS 2 Jazzy mobile manipulation robot (mecanum base + SO-101 arm + Pi 5 + GPU workstation + Android app). Recently shipped a full observability stack: Prometheus + Grafana + Loki + Tempo + W&B + AlertManager across two machines.

AUDIENCE: Robotics developers, ROS 2 engineers, embodied AI builders. Not beginners. They know what /diagnostics is. Speak to them as a peer.

STRUCTURE:
1. Introduction — The demo worked. The deployment didn't. Why visibility is the gap.
2. Problem Statement — What a "silent latent failure" looks like in practice. Control loop at P95 55ms looks fine. Navigation jitter appears. You spend 3 days tuning costmaps. The real problem was the driver node cycle time.
3. Why rosbag + RViz breaks at scale — Works per-session. Loses cross-machine context. Can't alert. Requires human watching.
4. OmniBot Case Study — Architecture of the stack: ros2_prometheus_bridge on Pi (port 8888) + GPU desktop (port 8889). What metrics matter: NODE_CYCLE_P50/P95/MAX from /diagnostics, omnibot_vla_inference_ms, omnibot_rl_inference_ms, arm joint states, mission success/failure counters. Loki aggregating ~/.ros/log/ via promtail. Tempo tracing LangGraph agent calls. W&B tracking runtime inference quality alongside training. The Android operator app surfacing health gauges + active alerts + log stream.
5. Alert Engineering — Why you need thresholds, not just dashboards. The specific OmniBot alerts: ControlLoopSlow (P95 > 55ms), VLAInferenceSlow (> 2s), GPUVRAMCritical (> 85%), MissionFailureRateHigh (> 30% over 10 min). Why each threshold was chosen from hardware constraints.
6. Engineering Tradeoffs — Cost of running this stack (memory on Pi, network overhead, cardinality of label sets). When you shouldn't add observability (prototyping phase). When you must (anything running outside a controlled lab).
7. Lessons Learned — What we wish we'd instrumented from day one. The one metric that would have saved the most debugging time.
8. Future Directions — Per-episode trace correlation (link a failed mission to the exact VLA inference latency spike). Alerting into mission_planner to trigger safe fallback behaviors.
9. Final Takeaways — Three things to instrument before your next hardware test.

TONE: Technical, practical, systems-oriented. No hype. Honest about what this costs. The goal is to sound like someone who built this, not someone who read about it."""

    visuals = [
        "Grafana dashboard screenshot — `omnibot-robot-health` showing control loop P50/P95 over a 30-min run",
        "Grafana dashboard screenshot — `omnibot-ai-performance` showing VLA inference latency spikes vs mission events",
        "Android app observability tab — health gauges, active alerts, W&B run list",
        "Architecture diagram: Pi (metrics bridge :8888) + GPU (metrics bridge :8889) → Prometheus → Grafana / AlertManager, with Loki log pipeline and Tempo trace path from LangGraph",
        "Terminal showing `omnibot_node_cycle_p95_ms{node='yahboom_controller_node'}` via `promtool query instant`",
        "AlertManager UI showing a fired `ControlLoopSlow` alert with labels",
        "W&B training run panel alongside the runtime inference latency panel — same project, different phases",
        "Photo: Pi 5 + GPU desktop side by side, showing the multi-machine deployment context",
    ]

    cross_links = {
        "connects_to": [
            "ROS 2 multi-machine DDS networking (why cross-machine metrics need correct ROS_DOMAIN_ID alignment)",
            "cmd_vel_mux command arbitration (control loop timing directly impacts mux responsiveness)",
            "LangGraph orchestration (Tempo traces capture agent decision latency end-to-end)",
        ],
        "unlocks": [
            "Why Mission Failure Analysis Requires Distributed Tracing (not just logs)",
            "How to Design Alert Thresholds from Physical Hardware Constraints",
            "Why the Android Operator App Becomes a Safety System, Not Just a Controller",
            "How Observability Changes the Way You Write ROS 2 Nodes (self-reporting diagnostics)",
        ],
        "youtube_expansions": [
            "Live demo: triggering a VLA inference spike and watching it propagate through Grafana → AlertManager → Android alert",
            "Walkthrough of the ros2_prometheus_bridge node architecture",
            "Episode recording + W&B run correlation: same dataset, same training run, deployed inference tracked",
        ],
    }

    return {
        "date": today,
        "topic": topic,
        "core_idea": core_idea,
        "twitter_prompt": twitter_prompt,
        "substack_prompt": substack_prompt,
        "visuals": visuals,
        "cross_links": cross_links,
        "recent_commits": [c["subject"] for c in commits[:5]],
        "top_changed_areas": top_area_names,
    }


# ── Discord delivery ───────────────────────────────────────────────────────────

DISCORD_LIMIT = 1900  # safe margin below 2000-char Discord message limit


def chunk_message(text: str, limit: int = DISCORD_LIMIT) -> list[str]:
    if len(text) <= limit:
        return [text]
    chunks = []
    while text:
        if len(text) <= limit:
            chunks.append(text)
            break
        split_at = text.rfind("\n", 0, limit)
        if split_at == -1:
            split_at = limit
        chunks.append(text[:split_at])
        text = text[split_at:].lstrip("\n")
    return chunks


def post_to_discord(webhook_url: str, content: str, retries: int = 3) -> bool:
    import http.client
    import urllib.parse

    payload = json.dumps({"content": content}).encode("utf-8")
    parsed = urllib.parse.urlparse(webhook_url)
    delay = 2
    for attempt in range(retries):
        try:
            conn = http.client.HTTPSConnection(parsed.netloc, timeout=15)
            conn.request(
                "POST",
                parsed.path + ("?" + parsed.query if parsed.query else ""),
                body=payload,
                headers={
                    "Content-Type": "application/json",
                    "User-Agent": "OmniBot-ContentBot/1.0",
                    "Content-Length": str(len(payload)),
                },
            )
            resp = conn.getresponse()
            conn.close()
            if resp.status in (200, 204):
                return True
            print(f"HTTP {resp.status} on attempt {attempt + 1}: {resp.read().decode()}", file=sys.stderr)
        except Exception as e:
            print(f"Error on attempt {attempt + 1}: {e}", file=sys.stderr)
        if attempt < retries - 1:
            time.sleep(delay)
            delay *= 2
    return False


def send_content(webhook_url: str, content: dict) -> None:
    sections = build_discord_sections(content)
    for i, section in enumerate(sections):
        chunks = chunk_message(section)
        for chunk in chunks:
            ok = post_to_discord(webhook_url, chunk)
            if not ok:
                print(f"Failed to post section {i + 1}", file=sys.stderr)
                sys.exit(1)
            time.sleep(0.5)  # respect Discord rate limit
    print(f"Delivered {len(sections)} sections successfully.")


def build_discord_sections(c: dict) -> list[str]:
    sep = "─" * 48

    s0 = (
        f"# OmniBot Daily Engineering Content\n"
        f"**{c['date']}**\n\n"
        f"Recent engineering activity: `{'`, `'.join(c['top_changed_areas'])}`\n"
        f"Latest commits:\n" +
        "\n".join(f"• {s}" for s in c["recent_commits"])
    )

    s1 = (
        f"## Daily Topic\n"
        f"**{c['topic']}**\n\n"
        f"{sep}\n\n"
        f"## Core Idea\n\n"
        f"{c['core_idea']}"
    )

    s2 = (
        f"## Twitter/X Thread Prompt\n\n"
        f"{c['twitter_prompt']}"
    )

    s3 = (
        f"## Substack Article Prompt\n\n"
        f"{c['substack_prompt']}"
    )

    visuals_block = "\n".join(f"• {v}" for v in c["visuals"])
    connects = "\n".join(f"  • {x}" for x in c["cross_links"]["connects_to"])
    unlocks = "\n".join(f"  • {x}" for x in c["cross_links"]["unlocks"])
    youtube = "\n".join(f"  • {x}" for x in c["cross_links"]["youtube_expansions"])

    s4 = (
        f"## Suggested Visuals\n\n{visuals_block}\n\n"
        f"{sep}\n\n"
        f"## Cross-Link Opportunities\n\n"
        f"**Connects to:**\n{connects}\n\n"
        f"**Unlocks:**\n{unlocks}\n\n"
        f"**YouTube expansions:**\n{youtube}"
    )

    return [s0, s1, s2, s3, s4]


# ── Entry point ────────────────────────────────────────────────────────────────

def main() -> None:
    webhook_url = os.environ.get("DISCORD_WEBHOOK_URL", "").strip()
    if not webhook_url:
        print("Error: DISCORD_WEBHOOK_URL environment variable not set.", file=sys.stderr)
        sys.exit(1)

    repo_path = os.environ.get("REPO_PATH", os.getcwd())
    max_commits = int(os.environ.get("MAX_COMMITS", "10"))

    print("Analyzing repository...")
    commits = get_recent_commits(repo_path, max_commits)
    files = get_changed_files(repo_path)
    areas = summarize_changes(files)

    print(f"Found {len(commits)} commits, {len(files)} changed files across {len(areas)} areas.")

    print("Generating content...")
    content = generate_content(commits, areas)

    print("Sending to Discord...")
    send_content(webhook_url, content)


if __name__ == "__main__":
    main()
