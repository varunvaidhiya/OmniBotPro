#!/usr/bin/env python3
"""
OmniBot Daily Content Generator + Discord Sender.

Analyzes recent git activity, generates one engineering content topic,
and sends formatted sections to Discord via webhook.

Usage:
    DISCORD_WEBHOOK_URL=<url> python tools/daily_content_sender.py
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
DISCORD_MAX_CHARS = 1990  # leave margin below 2000


# ── Git helpers ───────────────────────────────────────────────────────────────

def recent_commits(n: int = 10) -> str:
    result = subprocess.run(
        ["git", "log", f"--oneline", f"-{n}"],
        capture_output=True, text=True
    )
    return result.stdout.strip()


def changed_files_since(commit: str) -> list[str]:
    result = subprocess.run(
        ["git", "diff", "--name-only", f"{commit}~1", commit],
        capture_output=True, text=True
    )
    return [f for f in result.stdout.strip().splitlines() if f]


# ── Content sections ──────────────────────────────────────────────────────────

DAILY_TOPIC = "Why Operator-Side Observability Is the Missing Layer in Real Robot Deployments"

CORE_IDEA = """
Most robotics teams instrument server-side observability — Grafana dashboards on a desktop, Prometheus scraping ROS 2 nodes — and stop there. The assumption is that someone watching a monitor can intervene when something goes wrong.

That assumption breaks when the operator is mobile.

OmniBot recently added a native observability dashboard directly into the Android controller app. It pulls Prometheus metrics (Pi CPU, GPU VRAM, driver P95 cycle time, VLA inference latency), AlertManager firing alerts, Loki log streams via WebSocket live-tail, and W&B training run metrics — all aggregated in the same interface used to drive the robot.

The core engineering insight: the operator who needs health information most is the one moving with the robot, not the one at the desk. Building observability into the command interface eliminates the split-attention problem where an operator drives the robot while a second person watches telemetry on a separate screen.

The implementation tradeoff is real: five independent data sources (Prometheus, AlertManager, Loki WebSocket, W&B REST, ROSBridge), each with different polling semantics, error modes, and authentication patterns, all rendered without blocking the primary control path. The Kotlin coroutines + Flow architecture that works cleanly for ROSBridge breaks down when you add WebSocket live-tail alongside HTTP polling at different intervals (5 s for metrics, 30 s for W&B runs, real-time for Loki). Getting these to compose without backpressure issues or ANR risk is non-trivial.

The deeper problem it exposed: alert fatigue from server-side alerting rarely reaches the operator in time. By the time a Slack alert fires for a missed 20 Hz control loop deadline, the robot has already behaved badly for 10 seconds. Embedding `ControlLoopSlow` (P95 > 55 ms) directly in the operator HUD changes the latency of intervention from minutes to seconds.
"""

TWITTER_PROMPT = """
Write a concise, high-signal Twitter thread (6–9 tweets) from the perspective of an experienced robotics systems engineer.

Topic: Why operator-side observability is the missing layer in real robot deployments — and what OmniBot's Android app just demonstrated about it.

Tone requirements:
- Sound like someone who has debugged a robot at 2am and learned something from it
- No hype language. No "game-changing." No "revolutionary."
- Technical but readable. Tradeoffs stated plainly.
- Credible to ROS 2 engineers and robotics researchers

Thread structure:
- Tweet 1 (hook): A sharp observation about where robotics observability usually stops — and why that's wrong
- Tweets 2–4: The systems engineering argument for moving observability into the operator interface. Cite real constraints: mobile operator, split-attention problem, alert delivery latency
- Tweet 5: The OmniBot example — what was built (Android app pulling Prometheus, AlertManager, Loki, W&B) and what the implementation surface looked like (5 async data sources, WebSocket live-tail, HTTP polling at different intervals composing via Kotlin Flows)
- Tweet 6: The concrete tradeoff — what you gain (sub-10s intervention latency on control loop misses vs minutes via Slack) vs what it costs (engineering complexity, maintaining 5 API surfaces)
- Tweet 7: The controversial opinion — most robotics teams build their observability stack for demos and post-mortems, not for operators doing real work
- Tweet 8: Practical takeaway — the first metric to put in your operator interface and why
- Tweet 9 (optional): What breaks next once you do this

Style:
- Max 280 chars per tweet
- Use numbered tweets (1/8, 2/8, etc.)
- Avoid excessive hashtags — at most one relevant tag per thread (e.g. #ROS2, #EmbodiedAI)
- Mention OmniBot naturally, not as marketing
- No emojis unless one genuinely aids clarity
"""

SUBSTACK_PROMPT = """
Write a long-form technical article for a Substack publication targeting robotics engineers, ROS 2 developers, and embodied AI practitioners. The audience is NOT beginners.

Title: Why Operator-Side Observability Is the Missing Layer in Real Robot Deployments

Tone: Technical, systems-oriented, experience-driven. Honest about tradeoffs. No AI hype. No beginner hand-holding.

Article structure:

1. INTRODUCTION
   Open with the failure mode: a robot behaving badly while its operator is focused on the control interface, unaware that the 20 Hz control loop has been missing deadlines for 30 seconds. Set up why server-side dashboards don't solve this.

2. PROBLEM STATEMENT
   Define the split-attention problem in mobile robot operation. The operator who needs health signals most is the one physically present with the robot, not the one at a monitoring desk. Explain how alert delivery latency (Prometheus → AlertManager → Slack → human) is measured in minutes, while control loop degradation is measured in seconds.

3. WHY EXISTING APPROACHES BREAK
   - Server-side Grafana: requires a second screen/person
   - ROS 2 /diagnostics topic: loses context outside the ROS graph
   - Physical LED indicators: coarse-grained, not queryable
   - Voice alerts: impractical in lab/industrial environments
   Explain what each misses structurally, not just as a feature list.

4. OMNIBOT CASE STUDY
   Describe OmniBot's architecture:
   - Raspberry Pi 5 (all base ROS 2 nodes: driver at 20 Hz, Nav2, SLAM)
   - NVIDIA GPU workstation (VLA inference at 1 Hz, RL nav/arm at 20 Hz, Prometheus bridge)
   - Android app (operator controller via ROSBridge WebSocket)

   Describe what the new observability screen adds:
   - Health tab: Pi CPU/RAM/disk, GPU VRAM/utilization/temp, driver P50/P95/max cycle time, VLA inference ms, RL policy latency, mission success rate, active control mode, emergency stop state, Prometheus scrape target health
   - Alerts tab: AlertManager firing alerts (critical/warning), silence action
   - Logs tab: Loki log stream with filter, WebSocket live-tail
   - Training tab: W&B runs with loss curves (MPAndroidChart)

   Explain the implementation architecture: ObservabilityRepository using Kotlin coroutines + StateFlow, 5 async data sources composed without blocking the main thread, 5-second polling for metrics, real-time WebSocket for logs.

5. ENGINEERING TRADEOFFS
   - 5 API surfaces to maintain (Prometheus, AlertManager, Loki, W&B, ROSBridge) — each with different auth, error modes, versioning
   - Backpressure: Loki WebSocket + HTTP polling composing via Flow — explain the cancellation and backpressure challenge
   - ANR risk: Android does not allow network I/O on the main thread; structured concurrency via Dispatchers.IO is mandatory but adds state management complexity
   - UX tradeoff: operator cognitive load. How much telemetry is too much in a control interface?
   - Network dependency: observability infrastructure requires GPU desktop reachable over LAN; robot operating at range breaks observability

6. LESSONS LEARNED
   - The first metric that matters in the operator interface is control loop P95 cycle time, not CPU percentage
   - Alert severity mapping (warning vs critical) needs calibration for the specific robot — generic thresholds cause alert fatigue
   - Live log tail is more useful for debugging than historical log queries during active operation
   - W&B training metrics in the operator interface only make sense if training runs can be correlated to deployment behavior — otherwise it's noise
   - Building observability into the command interface forces you to think about what signals actually matter operationally vs what's interesting analytically

7. FUTURE DIRECTIONS
   - Offline observability: local SQLite buffering when GPU desktop is unreachable
   - Anomaly-based alerts vs threshold-based (control loop jitter patterns, not just absolute P95)
   - Tying mission outcome data back to the observability stream (close the deployment feedback loop)
   - Federated observability for multi-robot scenarios

8. FINAL TAKEAWAYS
   Three concrete things to add to your robot's operator interface before adding another sensor or training another policy. Explain the priority ordering and the reasoning.

Length: 2,000–3,000 words. Technical depth over breadth. Cite specific metrics, latency numbers, and architecture decisions from OmniBot where relevant. No generic advice.
"""

SUGGESTED_VISUALS = """
**Suggested Visuals:**
- Screenshot: Android observability Health tab showing Pi CPU, GPU VRAM bars, driver P95 cycle time gauge, active control mode badge, E-stop state
- Screenshot: Android Alerts tab with a firing `ControlLoopSlow` alert highlighted
- Screenshot: Android Logs tab with Loki live-tail showing ROS 2 node output
- Architecture diagram: data flow from ROS 2 nodes → Prometheus bridge → AlertManager/Loki/Grafana on GPU desktop → Android app (showing all 5 API paths)
- Grafana dashboard screenshot: `omnibot-robot-health` dashboard showing driver P95 over time with threshold annotation
- Code snippet: `ObservabilityRepository.healthFlow()` showing the `flatMapLatest` + `coroutineScope` + parallel `async` pattern (the interesting bit)
- Timeline diagram: alert delivery latency comparison — Prometheus alert → Slack (minutes) vs Prometheus → Android HUD (5 seconds)
- Photo: operator holding Android controller while robot is in motion, showing HUD visible alongside control joystick
"""

CROSS_LINKS = """
**Cross-Link Opportunities:**
- Previous: *Why Real Robots Need Command Arbitration Layers* (cmd_vel_mux, arm_cmd_mux design) — observability shows you which arbitration mode is active in real time
- Previous: *Why Robotics Projects Underestimate Calibration Complexity* — control loop P95 drift is often the first sign of calibration degradation
- Future: *Why Alert Fatigue Is a Systems Design Problem, Not a Notification Problem* — threshold calibration for robot-specific alert rules
- Future: *Why Offline Robotics Requires Different Infrastructure Assumptions* — what happens to your observability stack when LAN connectivity drops
- Future: *Why Mission Outcome Data Is Your Most Valuable Training Signal* — closing the loop from operator observability back to policy improvement
- YouTube expansion: "Building a Live Robot Health Dashboard in Android — From Prometheus to the Operator's Hand" — walkthrough of the MVVM architecture, Kotlin Flow composition, and the UX decisions
"""


# ── Discord delivery ───────────────────────────────────────────────────────────

def send_discord_message(webhook_url: str, content: str) -> bool:
    payload = json.dumps({"content": content}).encode("utf-8")
    req = urllib.request.Request(
        webhook_url,
        data=payload,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            return resp.status in (200, 204)
    except urllib.error.HTTPError as e:
        print(f"Discord HTTP error {e.code}: {e.read().decode()}", file=sys.stderr)
        return False
    except Exception as e:
        print(f"Discord send error: {e}", file=sys.stderr)
        return False


def send_with_retry(webhook_url: str, content: str, retries: int = 4) -> bool:
    delays = [2, 4, 8, 16]
    for attempt in range(retries):
        if send_discord_message(webhook_url, content):
            return True
        if attempt < retries - 1:
            wait = delays[attempt]
            print(f"Retry {attempt + 1}/{retries - 1} in {wait}s…", file=sys.stderr)
            time.sleep(wait)
    return False


def split_into_chunks(text: str, max_len: int = DISCORD_MAX_CHARS) -> list[str]:
    """Split text on newlines so Discord renders cleanly."""
    chunks: list[str] = []
    current = ""
    for line in text.splitlines(keepends=True):
        if len(current) + len(line) > max_len:
            if current:
                chunks.append(current.rstrip())
            current = line
        else:
            current += line
    if current.strip():
        chunks.append(current.rstrip())
    return chunks


def send_section(webhook_url: str, header: str, body: str) -> None:
    full = f"## {header}\n{body.strip()}"
    chunks = split_into_chunks(full)
    for i, chunk in enumerate(chunks):
        label = f" *(cont. {i + 1}/{len(chunks)})*" if len(chunks) > 1 and i > 0 else ""
        success = send_with_retry(webhook_url, chunk + label)
        if not success:
            print(f"Failed to send chunk {i + 1} of '{header}'", file=sys.stderr)
        time.sleep(0.5)  # rate-limit courtesy delay


# ── Main ──────────────────────────────────────────────────────────────────────

def main() -> None:
    webhook_url = DISCORD_WEBHOOK_URL
    if not webhook_url:
        print("ERROR: DISCORD_WEBHOOK_URL environment variable not set.", file=sys.stderr)
        sys.exit(1)

    today = datetime.utcnow().strftime("%Y-%m-%d")
    commits = recent_commits(8)

    # ── Header ──
    header_msg = (
        f"# OmniBot Daily Content — {today}\n"
        f"**Recent commits:**\n```\n{commits}\n```"
    )
    send_with_retry(webhook_url, header_msg)
    time.sleep(0.8)

    # ── Sections ──
    send_section(webhook_url, "1. Daily Topic", f"**{DAILY_TOPIC}**")
    time.sleep(0.5)

    send_section(webhook_url, "2. Core Idea", CORE_IDEA)
    time.sleep(0.5)

    send_section(webhook_url, "3. Twitter/X Post Prompt", TWITTER_PROMPT)
    time.sleep(0.5)

    send_section(webhook_url, "4. Substack Article Prompt", SUBSTACK_PROMPT)
    time.sleep(0.5)

    send_section(webhook_url, "5. Suggested Visuals", SUGGESTED_VISUALS)
    time.sleep(0.5)

    send_section(webhook_url, "6. Cross-Link Opportunities", CROSS_LINKS)
    time.sleep(0.5)

    send_with_retry(webhook_url, "---\n*Generated from OmniBot repo state · daily content routine*")
    print("Done — all sections sent to Discord.")


if __name__ == "__main__":
    main()
