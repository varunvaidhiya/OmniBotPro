#!/usr/bin/env python3
"""
OmniBot Daily Content Generator

Reads git commits in ascending order, groups them by engineering session (date),
calls Claude to generate robotics engineering content, and delivers to Discord.

Usage:
    python daily_content.py              # advance to next day
    python daily_content.py --day 5      # force a specific day number (1-based)
    python daily_content.py --dry-run    # generate + print, skip Discord
    python daily_content.py --list       # show all sessions without generating
"""

import argparse
import json
import os
import subprocess
import sys
import time
from pathlib import Path

import anthropic
import requests

# ── Paths & config ────────────────────────────────────────────────────────────
REPO_ROOT = Path(__file__).resolve().parent.parent.parent
STATE_FILE = REPO_ROOT / ".content_state.json"
DISCORD_WEBHOOK = os.environ.get("DISCORD_WEBHOOK_URL", "")
ANTHROPIC_API_KEY = os.environ.get("ANTHROPIC_API_KEY", "")
DISCORD_MAX = 2000

# Subjects to skip when picking the "hero" commits for analysis
_SKIP_PREFIXES = ("Merge pull request", "Merge branch", "chore: ignore", "docs:")


# ── Git helpers ───────────────────────────────────────────────────────────────

def get_all_commits() -> list[dict]:
    """Return every commit in chronological (ascending) order."""
    result = subprocess.run(
        ["git", "log", "--reverse", "--format=%H|%ad|%s", "--date=short"],
        capture_output=True, text=True, cwd=REPO_ROOT, check=True,
    )
    commits = []
    for line in result.stdout.strip().splitlines():
        parts = line.split("|", 2)
        if len(parts) == 3:
            commits.append({"hash": parts[0], "date": parts[1], "subject": parts[2]})
    return commits


def group_by_date(commits: list[dict]) -> list[tuple[str, list[dict]]]:
    """Group commits by commit date, preserving chronological order."""
    groups: dict[str, list[dict]] = {}
    for c in commits:
        groups.setdefault(c["date"], []).append(c)
    return sorted(groups.items())


def commit_diff_summary(sha: str, max_chars: int = 1200) -> str:
    """Return a trimmed stat+body summary for a single commit."""
    result = subprocess.run(
        ["git", "show", "--stat", "--format=%B", sha],
        capture_output=True, text=True, cwd=REPO_ROOT,
    )
    return result.stdout[:max_chars]


# ── State ─────────────────────────────────────────────────────────────────────

def load_state() -> dict:
    if STATE_FILE.exists():
        with open(STATE_FILE) as f:
            return json.load(f)
    return {"current_day_index": 0, "processed": []}


def save_state(state: dict) -> None:
    with open(STATE_FILE, "w") as f:
        json.dump(state, f, indent=2)
    print(f"State saved → day index now {state['current_day_index']}")


# ── Content generation (Claude) ───────────────────────────────────────────────

SYSTEM_PROMPT = """\
You are generating daily robotics engineering content for the OmniBot project \
(github.com/varunvaidhiya/OmniBot). OmniBot is a ROS 2 Jazzy mecanum-wheel \
mobile-manipulation robot: omnidirectional base, SO-101 6-DoF arm, Raspberry Pi 5 \
for onboard control, NVIDIA GPU workstation for VLA inference (OpenVLA / SmolVLA), \
Nav2, SLAM Toolbox, multi-camera BEV stitching, FastAPI inference server, \
Android ROSBridge app, LeRobot imitation-learning pipeline, Isaac Sim / Gazebo, \
Isaac Lab RL sim-to-real, LangGraph AI orchestration.

The audience: experienced robotics engineers, ROS 2 developers, embodied-AI researchers, \
startup robotics engineers. NOT beginners.

Tone: technical, practical, systems-oriented, honest about tradeoffs, \
experience-driven. No hype. No clickbait. No generic motivational content. \
Assume the reader understands software systems and has heard of ROS 2. \
Prefer strong engineering reasoning, concrete examples, direct language."""


def generate_content(date: str, commits: list[dict], day_number: int) -> dict:
    """Call Claude and return parsed JSON content dict."""
    client = anthropic.Anthropic(api_key=ANTHROPIC_API_KEY)

    # Build commit context (skip pure-merge commits for the analysis body)
    signal_commits = [c for c in commits if not c["subject"].startswith("Merge pull request")]
    if not signal_commits:
        signal_commits = commits

    commit_blocks = []
    for c in signal_commits[:6]:
        detail = commit_diff_summary(c["hash"])
        commit_blocks.append(f"[{c['subject']}]\n{detail}")

    commit_context = "\n\n---\n\n".join(commit_blocks)

    user_prompt = f"""\
Day {day_number} — engineering session date: {date}

Commits to analyze:
{commit_context}

Based on the real engineering work above, produce the following content.
Be specific: name the actual files, decisions, and tradeoffs visible in the commits.
Do NOT invent features or problems that are not in the diff.

Respond with ONLY a single valid JSON object, no markdown fences, matching this schema:
{{
  "daily_topic": "<One concise title, e.g. 'Why ROS 2 Joint-Name Mismatches Break the Entire TF Tree'>",
  "core_idea": "<3-4 paragraph engineering insight. Explain the central problem, why it matters in real robotics, what specific decision in these commits exposed it, and what mistake future engineers can avoid. Include concrete tradeoffs.>",
  "twitter_prompt": "<200-250 word prompt instructing another AI model to write a high-signal Twitter/X thread. Must include: one strong hook tweet, the core engineering insight as thread body (3-5 tweets), one practical takeaway tweet, optionally one controversial engineering opinion. Sound like a senior robotics engineer. Reference OmniBot naturally. No emojis overload. No engagement bait.>",
  "substack_prompt": "<300-350 word prompt for a long-form Substack article. Structure: Introduction, Problem Statement, Why Existing Approaches Break, OmniBot Case Study (use specific commit details), Engineering Tradeoffs, Lessons Learned, Future Directions, Final Takeaways. Include what diagrams or code snippets to embed. Specify the target reader and what they should take away.>",
  "suggested_visuals": [
    "<specific screenshot, diagram, or photo — be precise about what it shows>",
    "<specific screenshot, diagram, or photo>",
    "<specific screenshot, diagram, or photo>",
    "<specific screenshot, diagram, or photo>"
  ],
  "cross_links": {{
    "connects_to": ["<previous engineering topic this naturally follows from>"],
    "unlocks": ["<future topics this opens up>"],
    "related_themes": ["<theme 1>", "<theme 2>"],
    "youtube_expansion": "<One specific YouTube video idea with a working title>"
  }}
}}"""

    response = client.messages.create(
        model="claude-opus-4-8",
        max_tokens=4096,
        system=SYSTEM_PROMPT,
        messages=[{"role": "user", "content": user_prompt}],
    )

    raw = response.content[0].text.strip()
    # Strip markdown fences if the model adds them despite instructions
    if raw.startswith("```"):
        raw = raw.split("```", 2)[1]
        if raw.startswith("json"):
            raw = raw[4:]
        raw = raw.rsplit("```", 1)[0].strip()

    return json.loads(raw)


# ── Discord delivery ──────────────────────────────────────────────────────────

def _split_message(text: str, max_len: int = DISCORD_MAX) -> list[str]:
    """Split at blank-line boundaries, then hard-split if still too long."""
    if len(text) <= max_len:
        return [text]

    chunks, current = [], ""
    for para in text.split("\n\n"):
        candidate = (current + "\n\n" + para).lstrip() if current else para
        if len(candidate) > max_len:
            if current:
                chunks.append(current.strip())
            # Hard split the para itself
            while len(para) > max_len:
                chunks.append(para[:max_len])
                para = para[max_len:]
            current = para
        else:
            current = candidate

    if current.strip():
        chunks.append(current.strip())
    return chunks


def build_discord_messages(content: dict, date: str, day_number: int,
                            commits: list[dict]) -> list[str]:
    """Assemble up to 4 Discord messages from the generated content."""
    commit_lines = "\n".join(f"• {c['subject']}" for c in commits[:6])
    extra = f"\n• …and {len(commits) - 6} more" if len(commits) > 6 else ""

    msg1 = (
        f"## OmniBot Daily Content — Day {day_number}  ({date})\n\n"
        f"**Commits covered:**\n{commit_lines}{extra}\n\n"
        f"---\n\n"
        f"## Daily Topic\n**{content['daily_topic']}**\n\n"
        f"## Core Idea\n{content['core_idea']}"
    )

    msg2 = (
        f"---\n"
        f"## Twitter/X Post Prompt — Day {day_number}\n\n"
        f"{content['twitter_prompt']}"
    )

    msg3 = (
        f"---\n"
        f"## Substack Article Prompt — Day {day_number}\n\n"
        f"{content['substack_prompt']}"
    )

    visuals = "\n".join(f"• {v}" for v in content["suggested_visuals"])
    cl = content["cross_links"]
    connects = ", ".join(cl.get("connects_to", []))
    unlocks = ", ".join(cl.get("unlocks", []))
    themes = ", ".join(cl.get("related_themes", []))
    youtube = cl.get("youtube_expansion", "")

    msg4 = (
        f"---\n"
        f"## Suggested Visuals\n{visuals}\n\n"
        f"## Cross-Link Opportunities\n"
        f"**Connects to:** {connects}\n"
        f"**Unlocks:** {unlocks}\n"
        f"**Related themes:** {themes}\n"
        f"**YouTube expansion:** {youtube}"
    )

    return [msg1, msg2, msg3, msg4]


def send_to_discord(messages: list[str], dry_run: bool = False) -> None:
    if dry_run:
        for i, msg in enumerate(messages, 1):
            print(f"\n{'='*60}\nDISCORD MESSAGE {i} ({len(msg)} chars)\n{'='*60}\n{msg}")
        return

    if not DISCORD_WEBHOOK:
        print("WARNING: DISCORD_WEBHOOK_URL not set — printing only", file=sys.stderr)
        for msg in messages:
            print(msg)
        return

    for msg in messages:
        for chunk in _split_message(msg):
            if not chunk.strip():
                continue
            resp = requests.post(DISCORD_WEBHOOK, json={"content": chunk}, timeout=10)
            if resp.status_code == 204:
                print(f"  Sent {len(chunk)} chars to Discord")
            else:
                print(f"  Discord error {resp.status_code}: {resp.text}", file=sys.stderr)
            time.sleep(1.2)  # stay well inside Discord's 50 req/s global limit


# ── CLI ────────────────────────────────────────────────────────────────────────

def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="OmniBot daily content generator")
    p.add_argument("--day", type=int, default=None,
                   help="Force a specific 1-based day number (overrides state file)")
    p.add_argument("--dry-run", action="store_true",
                   help="Generate content and print to stdout; do not send to Discord")
    p.add_argument("--list", action="store_true",
                   help="List all available engineering sessions and exit")
    return p.parse_args()


def main() -> None:
    args = parse_args()

    commits = get_all_commits()
    sessions = group_by_date(commits)

    if args.list:
        print(f"{'#':>3}  {'Date':<12}  {'Commits':>7}  First subject")
        print("-" * 80)
        for i, (date, cs) in enumerate(sessions, 1):
            first = cs[0]["subject"][:55]
            print(f"{i:>3}  {date:<12}  {len(cs):>7}  {first}")
        return

    state = load_state()

    if args.day is not None:
        idx = args.day - 1
    else:
        idx = state.get("current_day_index", 0)

    if idx >= len(sessions):
        print(f"All {len(sessions)} sessions covered. Cycling back to day 1.")
        idx = 0

    date, day_commits = sessions[idx]
    day_number = idx + 1

    print(f"\nGenerating Day {day_number}/{len(sessions)} → {date} ({len(day_commits)} commits)")

    content = generate_content(date, day_commits, day_number)
    discord_messages = build_discord_messages(content, date, day_number, day_commits)
    send_to_discord(discord_messages, dry_run=args.dry_run)

    if not args.dry_run and args.day is None:
        state["current_day_index"] = idx + 1
        state.setdefault("processed", []).append({"day": day_number, "date": date})
        save_state(state)

    next_idx = idx + 1
    if next_idx < len(sessions):
        print(f"\nNext run will cover: Day {next_idx + 1} — {sessions[next_idx][0]}")
    else:
        print("\nAll sessions covered. Next run will cycle back to Day 1.")


if __name__ == "__main__":
    main()
