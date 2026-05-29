#!/usr/bin/env python3
"""
OmniBot Daily Content Generator

Walks through the git commit history chronologically (one calendar-day batch
per invocation), generates Twitter/Substack/visual content using the Claude API,
and posts the result to a Discord webhook.

State is stored in ~/.omnibot_content_state.json so each run automatically
advances to the next day's commits.

Usage:
    python daily_content.py                        # generate and post next day
    python daily_content.py --dry-run              # generate only, no Discord post
    python daily_content.py --day 3                # force a specific day index
    python daily_content.py --reset                # reset state to day 0
    python daily_content.py --list-days            # print all available content days
    python daily_content.py --webhook URL          # override webhook URL

Required env var: ANTHROPIC_API_KEY
Optional env var: DISCORD_WEBHOOK_URL (can also pass via --webhook)
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import Any

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

REPO_ROOT = Path(__file__).resolve().parents[2]
STATE_FILE = Path.home() / ".omnibot_content_state.json"

DISCORD_WEBHOOK_URL = os.getenv(
    "DISCORD_WEBHOOK_URL",
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs",
)

# Discord hard limit is 2 000 chars per message; we leave headroom for markup
DISCORD_MAX_CHARS = 1900

MODEL = "claude-sonnet-4-6"

# ---------------------------------------------------------------------------
# Git helpers
# ---------------------------------------------------------------------------


def run_git(*args: str, cwd: Path = REPO_ROOT) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=cwd,
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip()


def get_all_commit_dates() -> list[str]:
    """Return sorted unique commit dates (YYYY-MM-DD), oldest first, no merges."""
    log = run_git(
        "log",
        "--no-merges",
        "--format=%ad",
        "--date=short",
        "--reverse",
    )
    dates = list(dict.fromkeys(log.splitlines()))  # deduplicate, preserve order
    return dates


def get_commits_for_date(date: str) -> list[dict[str, str]]:
    """Return commits on a given date (no merges), oldest first."""
    log = run_git(
        "log",
        "--no-merges",
        f"--after={date} 00:00:00",
        f"--before={date} 23:59:59",
        "--format=%H|%ai|%s|%b",
        "--reverse",
    )
    commits = []
    for line in log.splitlines():
        if "|" not in line:
            continue
        parts = line.split("|", 3)
        if len(parts) < 3:
            continue
        commits.append(
            {
                "hash": parts[0],
                "date": parts[1],
                "subject": parts[2],
                "body": parts[3].strip() if len(parts) > 3 else "",
            }
        )
    return commits


def get_commit_stat(sha: str) -> str:
    """Return --stat summary for a single commit (file counts, not full diff)."""
    try:
        return run_git("show", "--stat", "--no-patch", sha)
    except subprocess.CalledProcessError:
        return ""


def get_diff_summary(sha: str, max_lines: int = 80) -> str:
    """Return a trimmed unified diff for the commit."""
    try:
        diff = run_git("show", "--unified=2", "--no-color", sha)
        lines = diff.splitlines()
        if len(lines) > max_lines:
            lines = lines[:max_lines] + [f"... [{len(lines) - max_lines} more lines]"]
        return "\n".join(lines)
    except subprocess.CalledProcessError:
        return ""


# ---------------------------------------------------------------------------
# State management
# ---------------------------------------------------------------------------


def load_state() -> dict[str, Any]:
    if STATE_FILE.exists():
        try:
            return json.loads(STATE_FILE.read_text())
        except (json.JSONDecodeError, OSError):
            pass
    return {"current_day_index": 0, "last_run": None, "completed_dates": []}


def save_state(state: dict[str, Any]) -> None:
    STATE_FILE.write_text(json.dumps(state, indent=2))


# ---------------------------------------------------------------------------
# Content generation (Claude API)
# ---------------------------------------------------------------------------

SYSTEM_PROMPT = """\
You are a senior robotics systems engineer with deep expertise in ROS 2, \
embodied AI, mobile manipulation, and real-world robot deployment. \
You are writing content for an engineering audience — robotics developers, \
ROS 2 engineers, embodied AI researchers — NOT beginners and NOT a mass market. \

Tone requirements:
- Technical, practical, systems-oriented
- Honest about tradeoffs and failures
- Experience-driven and concise
- No motivational fluff, no AI hype, no clickbait
- Sound like someone actively building robots, not a content marketer
- Share results, benchmarks, and metrics where relevant

The project is OmniBot: a ROS 2 Jazzy mecanum-wheel mobile-manipulation robot \
running on a Raspberry Pi 5 (onboard) + NVIDIA GPU workstation (AI inference), \
with a SO-101 6-DoF arm, Nav2, SLAM Toolbox, OpenVLA/SmolVLA integration, \
5-camera BEV stitching, LeRobot imitation learning, Isaac Lab RL, \
Grafana/Prometheus observability, and an Android ROSBridge controller app. \
The audience already knows what ROS 2 is.
"""


def build_generation_prompt(
    date: str,
    day_number: int,
    commits: list[dict[str, str]],
    diff_snippets: list[str],
) -> str:
    commit_block = "\n".join(
        f"- [{c['hash'][:8]}] {c['subject']}" + (f"\n  {c['body']}" if c["body"] else "")
        for c in commits
    )

    diff_block = "\n\n---\n\n".join(diff_snippets) if diff_snippets else "(no diffs available)"

    return f"""\
Today is content day {day_number} in the OmniBot project timeline. \
The project commit date is {date}.

## Commits on this date
{commit_block}

## Key diffs (truncated)
{diff_block}

---

Based on the engineering work above, generate ONE day's worth of robotics content. \
Pick the single strongest engineering insight, failure, tradeoff, or system design \
decision visible in these commits. Prefer content that:
- exposes a non-obvious robotics engineering problem
- has practical deployment consequences
- connects hardware decisions to software architecture
- shows honest debugging or iteration

Output EXACTLY the following Markdown sections, with no extra prose before or after:

## 1. Daily Topic
A single concise topic title (no more than 12 words).

## 2. Core Idea
2–4 paragraphs explaining:
- the central engineering insight
- why it matters in real robotics systems
- what practical issue triggered this in OmniBot
- engineering tradeoffs, deployment realities, lessons learned

## 3. Twitter/X Thread Prompt
A detailed prompt (200–300 words) instructing another AI model to write \
a high-signal Twitter thread. The prompt must specify:
- Hook tweet (1 punchy sentence, no questions)
- 4–6 body tweets with concrete technical insight
- Final tweet with one practical takeaway
- Style: experienced engineer, no hype, no emoji spam, no engagement bait
- Mention OmniBot naturally in context

## 4. Substack Article Prompt
A detailed long-form article brief (300–500 words) covering:
- Introduction
- Problem Statement
- Why Existing Approaches Break
- OmniBot Case Study
- Engineering Tradeoffs
- Lessons Learned
- Future Directions
- Final Takeaways
Include specific technical details from OmniBot that the article should reference.

## 5. Suggested Visuals
A bullet list of 4–8 specific visuals (screenshots, diagrams, terminal logs, \
hardware photos, RViz views, Grafana graphs, etc.) that would strengthen the post.

## 6. Cross-Link Opportunities
3–5 bullet points linking this topic to:
- related previous OmniBot engineering themes
- future topics this naturally unlocks
- possible YouTube talking-point expansions
"""


def call_claude(prompt: str) -> str:
    try:
        import anthropic
    except ImportError:
        print("ERROR: anthropic package not installed. Run: pip install anthropic")
        sys.exit(1)

    api_key = os.getenv("ANTHROPIC_API_KEY", "")
    if not api_key:
        print("ERROR: ANTHROPIC_API_KEY environment variable is not set.")
        sys.exit(1)

    client = anthropic.Anthropic(api_key=api_key)
    message = client.messages.create(
        model=MODEL,
        max_tokens=4096,
        system=SYSTEM_PROMPT,
        messages=[{"role": "user", "content": prompt}],
    )
    return message.content[0].text


# ---------------------------------------------------------------------------
# Discord delivery
# ---------------------------------------------------------------------------


def split_for_discord(text: str, max_chars: int = DISCORD_MAX_CHARS) -> list[str]:
    """Split a long string into chunks that fit within Discord's message limit."""
    if len(text) <= max_chars:
        return [text]

    chunks: list[str] = []
    current = ""
    for para in text.split("\n\n"):
        block = para + "\n\n"
        if len(current) + len(block) > max_chars:
            if current:
                chunks.append(current.rstrip())
            # If a single paragraph is too long, hard-split it
            while len(block) > max_chars:
                chunks.append(block[:max_chars])
                block = block[max_chars:]
            current = block
        else:
            current += block
    if current.strip():
        chunks.append(current.rstrip())
    return chunks


def post_to_discord(webhook_url: str, content: str) -> bool:
    """Post content to Discord via curl, splitting if needed. Returns True on success."""
    chunks = split_for_discord(content)
    success = True
    for i, chunk in enumerate(chunks):
        payload = json.dumps({"content": chunk})
        result = subprocess.run(
            [
                "curl", "-s", "-w", "\nHTTP:%{http_code}",
                "-H", "Content-Type: application/json",
                "-X", "POST",
                "-d", payload,
                webhook_url,
                "--max-time", "15",
            ],
            capture_output=True,
            text=True,
        )
        out = result.stdout
        code = out.split("HTTP:")[-1].strip() if "HTTP:" in out else "???"
        if code not in ("200", "204"):
            print(f"Discord chunk {i + 1}/{len(chunks)}: HTTP {code}")
            success = False
        else:
            print(f"Discord chunk {i + 1}/{len(chunks)}: HTTP {code}")
        if i < len(chunks) - 1:
            time.sleep(0.8)  # rate-limit guard
    return success


def format_discord_message(
    day_number: int,
    date: str,
    commits: list[dict[str, str]],
    generated: str,
) -> str:
    header = (
        f"# OmniBot Daily Content — Day {day_number}\n"
        f"**Commit date:** {date} | "
        f"**Commits covered:** {len(commits)}\n"
        f"**Subjects:** {', '.join(c['subject'] for c in commits[:3])}"
        + (" …" if len(commits) > 3 else "")
        + "\n\n"
    )
    return header + generated


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="OmniBot daily content generator")
    parser.add_argument("--dry-run", action="store_true", help="Generate but do not post to Discord")
    parser.add_argument("--day", type=int, default=None, help="Force a specific day index (0-based)")
    parser.add_argument("--reset", action="store_true", help="Reset state to day 0")
    parser.add_argument("--list-days", action="store_true", help="Print all available content days")
    parser.add_argument("--webhook", type=str, default=None, help="Override Discord webhook URL")
    parser.add_argument("--no-diff", action="store_true", help="Skip diff collection (faster)")
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    webhook = args.webhook or DISCORD_WEBHOOK_URL

    if args.reset:
        save_state({"current_day_index": 0, "last_run": None, "completed_dates": []})
        print("State reset to day 0.")
        return

    # Collect all content days (unique commit dates, no merges)
    all_dates = get_all_commit_dates()
    if not all_dates:
        print("No commits found in repository.")
        sys.exit(1)

    if args.list_days:
        for i, d in enumerate(all_dates):
            commits = get_commits_for_date(d)
            subjects = "; ".join(c["subject"] for c in commits)
            print(f"Day {i:3d}  {d}  ({len(commits)} commits)  {subjects[:80]}")
        return

    state = load_state()
    day_index = args.day if args.day is not None else state["current_day_index"]

    if day_index >= len(all_dates):
        print(
            f"All {len(all_dates)} content days have been processed. "
            "Use --reset to start over."
        )
        return

    date = all_dates[day_index]
    commits = get_commits_for_date(date)

    if not commits:
        print(f"No non-merge commits on {date}. Skipping.")
        state["current_day_index"] = day_index + 1
        save_state(state)
        return

    print(f"\n{'=' * 60}")
    print(f"Content Day {day_index}  |  {date}  |  {len(commits)} commit(s)")
    for c in commits:
        print(f"  [{c['hash'][:8]}] {c['subject']}")
    print("=" * 60)

    # Gather diffs (up to 3 commits, truncated)
    diff_snippets: list[str] = []
    if not args.no_diff:
        for c in commits[:3]:
            snippet = get_diff_summary(c["hash"])
            if snippet:
                diff_snippets.append(f"### {c['subject']}\n{snippet}")

    print("\nCalling Claude API to generate content...")
    prompt = build_generation_prompt(date, day_index, commits, diff_snippets)
    generated = call_claude(prompt)

    # Compose final Discord message
    discord_msg = format_discord_message(day_index, date, commits, generated)

    print("\n" + "─" * 60)
    print(discord_msg)
    print("─" * 60)

    if args.dry_run:
        print("\n[DRY RUN] Discord post skipped.")
    else:
        print("\nPosting to Discord...")
        ok = post_to_discord(webhook, discord_msg)
        if ok:
            print("Posted successfully.")
        else:
            print("WARNING: One or more Discord chunks failed.")

    # Advance state
    if args.day is None:
        state["current_day_index"] = day_index + 1
        state["last_run"] = datetime.utcnow().isoformat()
        state.setdefault("completed_dates", []).append(date)
        save_state(state)
        print(f"\nState advanced to day {day_index + 1} ({all_dates[day_index + 1] if day_index + 1 < len(all_dates) else 'end'}).")


if __name__ == "__main__":
    main()
