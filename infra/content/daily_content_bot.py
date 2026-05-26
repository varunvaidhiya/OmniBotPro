#!/usr/bin/env python3
"""
OmniBot Daily Content Bot
=========================
Generates one high-signal robotics engineering content idea per day,
progressing through the OmniBot git history in chronological order.

Usage:
    python infra/content/daily_content_bot.py

State is stored in infra/content/.content_state.json to track which
commit batch was last used, so each run advances to the next day's content.

Sends output to Discord via webhook. Splits messages to stay under
Discord's 2000-character limit.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import textwrap
import time
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Optional
import urllib.request
import urllib.error

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

REPO_ROOT = Path(__file__).resolve().parents[2]
STATE_FILE = Path(__file__).parent / ".content_state.json"
DISCORD_WEBHOOK = (
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
)
DISCORD_MAX_LEN = 1900  # leave buffer below 2000 hard limit


# ---------------------------------------------------------------------------
# Git utilities
# ---------------------------------------------------------------------------

def run(cmd: list[str], cwd: Path = REPO_ROOT) -> str:
    result = subprocess.run(
        cmd, cwd=cwd, capture_output=True, text=True, check=True
    )
    return result.stdout.strip()


def get_all_commits() -> list[dict]:
    """Return all commits in chronological order (oldest first)."""
    raw = run(
        ["git", "log", "--reverse", "--format=%H|||%ad|||%s|||%b", "--date=short"]
    )
    commits = []
    for entry in raw.split("\n"):
        parts = entry.split("|||", 3)
        if len(parts) >= 3:
            commits.append(
                {
                    "hash": parts[0].strip(),
                    "date": parts[1].strip(),
                    "subject": parts[2].strip(),
                    "body": parts[3].strip() if len(parts) > 3 else "",
                }
            )
    return commits


def get_commit_diff_summary(commit_hash: str) -> str:
    """Return a short --stat summary of what changed in a commit."""
    try:
        return run(["git", "show", "--stat", "--no-patch", commit_hash])
    except subprocess.CalledProcessError:
        return ""


def get_commit_files(commit_hash: str) -> list[str]:
    """Return list of files touched by a commit."""
    try:
        raw = run(["git", "show", "--name-only", "--format=", commit_hash])
        return [f for f in raw.splitlines() if f.strip()]
    except subprocess.CalledProcessError:
        return []


# ---------------------------------------------------------------------------
# State management
# ---------------------------------------------------------------------------

def load_state() -> dict:
    if STATE_FILE.exists():
        with open(STATE_FILE) as f:
            return json.load(f)
    return {"last_commit_index": -1, "posts_sent": 0}


def save_state(state: dict) -> None:
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(STATE_FILE, "w") as f:
        json.dump(state, f, indent=2)


# ---------------------------------------------------------------------------
# Content generation — hardcoded day-by-day topics
# ---------------------------------------------------------------------------

# Each entry maps to a commit index range (start, end inclusive).
# The content is pre-written based on analysis of those commits.
CONTENT_CALENDAR: list[dict] = [
    # ── Day 1 — First commit (March 8, 2026) ─────────────────────────────────
    {
        "commit_range": (0, 0),
        "topic": "Why Robotics Engineers Are Forced to Become Protocol Reverse Engineers",
        "core_idea": (
            "When you buy commercial robot hardware, you rarely get a complete, "
            "accurate protocol specification. Yahboom's Rosmaster board ships with "
            "partial docs: enough to move the wheels, not enough to trust the "
            "odometry. OmniBot's first engineering task wasn't writing ROS nodes — "
            "it was writing `decode_encoders.py`, a script that commands specific "
            "mecanum wheel motions (forward, strafe, rotate) and statistically "
            "analyzes which bytes in the 0x0E response packet correspond to which "
            "wheel in which unit. The checksum formula alone (`(sum(all_bytes) + 5) "
            "& 0xFF`, where 5 compensates for the header complement) took two "
            "iterations to get right. The lesson: hardware-software contracts in "
            "robotics are often discovered, not delivered. If you ship a ROS driver "
            "without first validating the protocol at the wire level, your odometry "
            "will silently lie to your navigation stack."
        ),
        "twitter_prompt": (
            "Write a concise, high-signal Twitter thread (5-7 tweets) for an "
            "experienced robotics engineer audience. The thread should cover:\n\n"
            "HOOK (tweet 1): Most robot hardware ships with incomplete protocol "
            "documentation. The first real engineering task on a new robot is "
            "usually not writing a ROS node — it's figuring out what the hardware "
            "actually sends.\n\n"
            "DEVELOPMENT (tweets 2-4): Explain the approach used in OmniBot — "
            "writing a standalone Python script (`decode_encoders.py`) that "
            "commands specific motions (forward, backward, rotate, strafe) and "
            "statistically analyzes response bytes to identify which bytes map to "
            "which wheel encoder. Mention the checksum reverse engineering "
            "(TX checksum = `(sum(all bytes) + 5) & 0xFF`). Mention the STM32 "
            "→ Yahboom migration that triggered this work.\n\n"
            "TRADEOFF (tweet 5): The alternative to this kind of low-level "
            "validation is trusting vendor docs. In OmniBot's case, vendor docs "
            "were partially wrong. Silent odometry errors compound in SLAM and "
            "Nav2. A 2-hour protocol investigation saves 20 hours of nav stack "
            "debugging.\n\n"
            "TAKEAWAY (tweet 6): Any robotics engineer who has shipped a ROS "
            "driver without first verifying the protocol on a scope or in a "
            "purpose-built test script has probably also debugged mysterious "
            "localization drift at 2am.\n\n"
            "OPINION (tweet 7, optional): The robotics industry needs standardized "
            "hardware protocol testing frameworks the same way web APIs have "
            "contract testing. Nobody should have to write a custom packet decoder "
            "every time they buy a new motor controller.\n\n"
            "Style: Technical, first-person experience, no hype. Sound like someone "
            "who has done this, not someone describing it abstractly. No excessive "
            "emojis. Mention OmniBot naturally."
        ),
        "substack_prompt": (
            "Write a long-form technical article for a Substack aimed at "
            "robotics developers and ROS 2 engineers. The article should be "
            "2000-2500 words. Use the following structure:\n\n"
            "**Title**: Why Robotics Engineers Are Forced to Become Protocol "
            "Reverse Engineers\n\n"
            "**Introduction**: The assumption that buying a 'supported' robot "
            "board gives you a working driver is optimistic. Walk through what "
            "actually happened when OmniBot migrated from an STM32-based motor "
            "driver to a Yahboom ROS Robot Expansion Board — and why the first "
            "code written wasn't a ROS node.\n\n"
            "**Problem Statement**: Commercial robot hardware ships with partial "
            "specs. The Yahboom board's documentation describes basic motion "
            "commands but is incomplete on encoder packet format, unit conventions, "
            "and byte ordering. You can make the wheels spin, but can you trust "
            "the odometry?\n\n"
            "**Why Existing Approaches Break**: Naive approaches — just trust the "
            "docs, use the vendor's Python library, or copy a community ROS driver "
            "— all fail in different ways. Community drivers are often written for "
            "different hardware revisions. Vendor Python libraries abstract away "
            "the wire protocol, making it impossible to verify what's actually sent. "
            "Silent protocol bugs become silent odometry errors that corrupt your "
            "SLAM map and misdirect Nav2.\n\n"
            "**OmniBot Case Study**: Describe the `decode_encoders.py` approach "
            "in detail. Commanding specific mecanum motions (pure forward, pure "
            "strafe, pure rotation), collecting N response packets per motion, "
            "computing means and standard deviations across byte fields, and "
            "identifying which fields respond to which wheel based on sign "
            "inversion. Also cover the TX checksum formula derivation "
            "(`(sum + 5) & 0xFF` where 5 = 257 - 0xFC). Include the packet "
            "format: `[0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]`.\n\n"
            "**Engineering Tradeoffs**: Time invested in protocol validation vs "
            "time lost debugging downstream. The standalone-script approach (no "
            "ROS dependency) allows faster iteration. Codifying the validated "
            "protocol into a pure-Python library (`yahboom_ros2.protocol`) that "
            "the ROS driver imports — separating concerns cleanly.\n\n"
            "**Lessons Learned**: 1) Protocol validation is not optional. "
            "2) Write your driver test before your driver. 3) Statistical "
            "approaches to byte-field identification are more reliable than "
            "single-shot observations. 4) The hardware-software contract in "
            "robotics is discovered, not delivered.\n\n"
            "**Future Directions**: What would a proper robotics hardware "
            "protocol testing framework look like? Reference how web APIs use "
            "contract testing (Pact, OpenAPI) and argue robotics needs an "
            "equivalent.\n\n"
            "**Final Takeaways**: Concise bullet list of actionable insights.\n\n"
            "Tone: Technical, experience-driven, direct. No motivational fluff. "
            "Concrete enough that a ROS 2 engineer can apply this to their next "
            "hardware integration."
        ),
        "visuals": [
            "Terminal output of `decode_encoders.py` showing byte-field analysis table",
            "Side-by-side: STM32 wiring diagram vs Yahboom board USB connection",
            "Hex dump of a raw Yahboom TX packet with field annotations",
            "Graph: encoder byte values vs commanded velocity (forward/strafe/rotate)",
            "Photo of Yahboom board connected to Raspberry Pi via USB",
            "Code snippet: the checksum formula with derivation comment",
            "RViz screenshot showing SLAM map — good odometry vs drifted odometry",
        ],
        "cross_links": {
            "previous": [],
            "future": [
                "ROS_DOMAIN_ID and DDS networking failures (commit 3)",
                "Why control loop timing matters: 20Hz vs 10Hz (commit 2)",
                "STM32→Yahboom migration: hardware abstraction in practice",
                "Why pure-Python protocol libraries should be ROS-independent",
            ],
            "youtube": (
                "Live hardware session: reverse engineering a robot serial protocol "
                "from scratch using only a USB sniffer and a Python script"
            ),
        },
    },

    # ── Day 2 — Commits 2-3 (March 8, 2026) ──────────────────────────────────
    {
        "commit_range": (1, 2),
        "topic": "Why 10Hz Control Loops Are Not Enough for Mecanum Wheel Robots",
        "core_idea": (
            "OmniBot's second commit is a one-line change that reveals a "
            "fundamental control systems decision: bumping the Yahboom controller "
            "node from 10Hz to 20Hz and retuning the Xbox teleop scales. At 10Hz, "
            "a mecanum robot with 0.04m wheels and 0.05 m/s ramp steps produces "
            "velocity discontinuities that are physically noticeable — the robot "
            "jerks rather than accelerates smoothly. At 20Hz with the same ramp "
            "step, transitions are imperceptible. The third commit exposes a second "
            "constraint: ROS_DOMAIN_ID=30 must be set consistently across all "
            "machines in a multi-workstation setup, or DDS discovery silently fails. "
            "These two changes — timing and networking — are the two most common "
            "silent failure modes in real ROS 2 deployments."
        ),
        "twitter_prompt": (
            "Write a tight 4-5 tweet thread for robotics engineers covering:\n\n"
            "HOOK: The second commit on OmniBot was a control loop frequency bump "
            "from 10Hz to 20Hz. This is not a minor tuning change — it's the "
            "difference between a robot that feels responsive and one that feels "
            "like it's fighting itself.\n\n"
            "DEVELOPMENT: Explain why mecanum robots are particularly sensitive to "
            "control loop rate. Four independently driven wheels, velocity ramp "
            "steps, and holonomic kinematics mean that any timing jitter produces "
            "asymmetric wheel commands that the robot interprets as unintended "
            "rotation. At 10Hz, the ramp step of 0.05 m/s represents 500ms of "
            "acceleration time from 0 to 0.1 m/s — perceptible and unstable.\n\n"
            "TRADEOFF: Higher control loop rate increases CPU load on the "
            "Raspberry Pi 5. 20Hz was chosen as the sweet spot for this platform. "
            "Mention that the same commit also exposed a ROS_DOMAIN_ID=30 "
            "misconfiguration that was causing silent DDS discovery failure in "
            "multi-machine setups.\n\n"
            "TAKEAWAY: Before tuning PID gains, tune your control loop rate. "
            "Most mecanum robot instability at low speeds is a timing problem, "
            "not a gain problem.\n\n"
            "Style: Direct, experience-driven. Mention OmniBot naturally."
        ),
        "substack_prompt": (
            "Write a 1500-word technical article titled: 'Why Control Loop "
            "Frequency Is the First Tuning Parameter in Mobile Robotics'.\n\n"
            "Cover: the relationship between control loop rate, velocity ramp "
            "resolution, and perceived smoothness on mecanum robots. Use the "
            "OmniBot 10Hz→20Hz change as the case study. Discuss Pi 5 CPU "
            "constraints and how to choose update rate for your hardware. Include "
            "a section on ROS_DOMAIN_ID as the most common silent failure in "
            "multi-machine ROS 2 setups — it causes zero error messages but "
            "complete topic invisibility across machines."
        ),
        "visuals": [
            "Oscilloscope or timing trace: 10Hz vs 20Hz command intervals",
            "Velocity profile graph: ramp at 10Hz vs 20Hz (staircase vs smooth)",
            "Pi 5 CPU usage: top output during 20Hz control loop",
            "ROS 2 topic list showing /cmd_vel appearing/disappearing with wrong DOMAIN_ID",
            "Code diff: the 2-line change that bumped the loop rate",
        ],
        "cross_links": {
            "previous": ["Day 1: Protocol reverse engineering — knowing what the hardware sends"],
            "future": [
                "Nav2 and SLAM: how control loop rate affects localization quality",
                "Multi-machine DDS networking deep dive",
                "Raspberry Pi 5 as a robot brain: performance headroom analysis",
            ],
            "youtube": (
                "Live tuning session: finding the right control loop rate "
                "for a mecanum robot on Raspberry Pi 5"
            ),
        },
    },

    # ── Day 3 — Commit 4 (March 9, 2026) ─────────────────────────────────────
    {
        "commit_range": (3, 3),
        "topic": "How to Write a Technical README That Actually Attracts Serious Contributors",
        "core_idea": (
            "OmniBot's fourth commit rewrites the README from scratch with a "
            "project hook, a cost comparison table, and a quickstart. This is "
            "not a content marketing decision — it is a systems engineering "
            "decision. A robotics project README must immediately answer: what "
            "does this robot cost, what compute does it require, and what can "
            "it actually do today? The cost comparison (OmniBot vs commercial "
            "alternatives) sets a credibility anchor. The quickstart sets "
            "operational expectations. A vague README signals a vague project "
            "and drives away exactly the contributors you want — engineers "
            "who evaluate systems before committing time."
        ),
        "twitter_prompt": (
            "Write a 4-tweet thread on why robotics project documentation is "
            "an engineering discipline, not a writing task. Hook: most open "
            "source robot repos lose contributors at the README. The signals "
            "that work: a cost breakdown, a clear capability statement, and a "
            "working quickstart. The signals that fail: architecture diagrams "
            "with no deployment instructions. OmniBot's README rewrite was "
            "commit 4 — before any major feature was added. Takeaway: document "
            "the thing you built, not the thing you want to build."
        ),
        "substack_prompt": (
            "Write a 1200-word article: 'What Makes a Robotics Project README "
            "Work for Technical Contributors'. Cover: what serious robotics "
            "engineers look for before cloning a repo (hardware cost, compute "
            "requirements, deployment instructions, current capability vs roadmap). "
            "Include the OmniBot README structure as a case study. Contrast with "
            "common failures: repos that lead with architecture diagrams but have "
            "no working build instructions."
        ),
        "visuals": [
            "Before/after README screenshot (generic → structured with cost table)",
            "Cost comparison table: OmniBot vs commercial alternatives",
            "GitHub repo star/fork analytics showing contributor spike post-rewrite",
        ],
        "cross_links": {
            "previous": ["Day 1: Protocol reverse engineering", "Day 2: Control loop timing"],
            "future": [
                "CI/CD for robotics: why it matters from commit 1",
                "CLAUDE.md as living architecture documentation",
            ],
            "youtube": "Walkthrough: how to write a robotics project README that converts visitors to contributors",
        },
    },
]


# ---------------------------------------------------------------------------
# Content rendering
# ---------------------------------------------------------------------------

def render_day(day: dict, commit_info: list[dict], day_number: int) -> str:
    """Render a full day's content as a Discord-ready Markdown string."""
    commits_str = "\n".join(
        f"  • `{c['hash'][:8]}` {c['date']} — {c['subject']}"
        for c in commit_info
    )

    lines = [
        f"# OmniBot Daily Engineering Content — Day {day_number}",
        f"**Date**: {datetime.utcnow().strftime('%Y-%m-%d')}",
        f"**Source commits**:\n{commits_str}",
        "",
        "---",
        "",
        "## 1. Daily Topic",
        f"**{day['topic']}**",
        "",
        "---",
        "",
        "## 2. Core Idea",
        day["core_idea"],
        "",
        "---",
        "",
        "## 3. Twitter/X Post Prompt",
        "*(Give this prompt to an AI to generate the thread)*",
        "",
        day["twitter_prompt"],
        "",
        "---",
        "",
        "## 4. Substack Article Prompt",
        "*(Give this prompt to an AI to generate the long-form article)*",
        "",
        day["substack_prompt"],
        "",
        "---",
        "",
        "## 5. Suggested Visuals",
        "\n".join(f"- {v}" for v in day["visuals"]),
        "",
        "---",
        "",
        "## 6. Cross-Link Opportunities",
        "**Previous topics**:",
        "\n".join(f"- {p}" for p in day["cross_links"].get("previous", ["(first post — no previous)"]))
        or "- (first post — no previous)",
        "",
        "**Future topics this unlocks**:",
        "\n".join(f"- {f}" for f in day["cross_links"].get("future", [])),
        "",
        "**YouTube expansion**:",
        day["cross_links"].get("youtube", ""),
    ]
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Discord delivery
# ---------------------------------------------------------------------------

def discord_send(text: str, webhook: str) -> None:
    """Send text to Discord, splitting on DISCORD_MAX_LEN if needed."""
    chunks = split_for_discord(text)
    for i, chunk in enumerate(chunks):
        payload = json.dumps({"content": chunk}).encode("utf-8")
        req = urllib.request.Request(
            webhook,
            data=payload,
            headers={
                "Content-Type": "application/json",
                "User-Agent": "OmniBotContentBot/1.0",
            },
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=15) as resp:
                status = resp.status
                if status not in (200, 204):
                    print(f"  [warn] Discord returned HTTP {status} for chunk {i+1}")
                else:
                    print(f"  [ok] Chunk {i+1}/{len(chunks)} sent ({len(chunk)} chars)")
        except urllib.error.HTTPError as e:
            print(f"  [error] Discord HTTP {e.code} for chunk {i+1}: {e.reason}")
            print(f"         Response body: {e.read().decode('utf-8', errors='replace')}")
            sys.exit(1)
        except urllib.error.URLError as e:
            print(f"  [error] Discord send failed for chunk {i+1}: {e}")
            sys.exit(1)
        if i < len(chunks) - 1:
            time.sleep(1)  # rate-limit courtesy pause


def split_for_discord(text: str, max_len: int = DISCORD_MAX_LEN) -> list[str]:
    """
    Split text into chunks ≤ max_len, preferring to break on blank lines
    so markdown blocks stay intact.
    """
    if len(text) <= max_len:
        return [text]

    chunks: list[str] = []
    paragraphs = text.split("\n\n")
    current = ""

    for para in paragraphs:
        candidate = (current + "\n\n" + para).lstrip("\n")
        if len(candidate) <= max_len:
            current = candidate
        else:
            if current:
                chunks.append(current)
            # If a single paragraph itself is too long, split by line
            if len(para) > max_len:
                for line in para.splitlines(keepends=True):
                    if len(current) + len(line) <= max_len:
                        current += line
                    else:
                        if current:
                            chunks.append(current.rstrip())
                        current = line
            else:
                current = para

    if current:
        chunks.append(current.rstrip())

    return [c for c in chunks if c.strip()]


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    state = load_state()
    all_commits = get_all_commits()
    day_number = state["posts_sent"] + 1

    # Find which content day to use
    if day_number - 1 >= len(CONTENT_CALENDAR):
        print(
            f"All {len(CONTENT_CALENDAR)} pre-written content days have been sent. "
            "Add more entries to CONTENT_CALENDAR."
        )
        return

    day = CONTENT_CALENDAR[day_number - 1]
    start_idx, end_idx = day["commit_range"]
    commit_info = all_commits[start_idx : end_idx + 1]

    print(f"\n{'='*60}")
    print(f" OmniBot Content Bot — Day {day_number}")
    print(f"{'='*60}")
    print(f" Topic: {day['topic']}")
    print(f" Commits: {start_idx}–{end_idx} ({len(commit_info)} commits)")
    print()

    content = render_day(day, commit_info, day_number)

    print(f" Total content length: {len(content)} chars")
    print(f" Sending to Discord …")
    discord_send(content, DISCORD_WEBHOOK)

    # Advance state
    state["last_commit_index"] = end_idx
    state["posts_sent"] = day_number
    save_state(state)

    print(f"\n Done. Day {day_number} content sent.")
    print(f" Next run will send Day {day_number + 1}.")


if __name__ == "__main__":
    main()
