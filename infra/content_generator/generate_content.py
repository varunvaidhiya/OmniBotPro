#!/usr/bin/env python3
"""
OmniBot Daily Content Generator

Reads git commits in chronological order, generates robotics engineering content
via Claude API, and sends formatted output to Discord.

Usage:
    python generate_content.py                  # generate and send next day's content
    python generate_content.py --day 1          # force a specific day
    python generate_content.py --dry-run        # print without sending to Discord
    python generate_content.py --show-state     # print current progress

State is tracked in .content_state.json next to this script.
Set ANTHROPIC_API_KEY and DISCORD_WEBHOOK_URL in environment or .env file.
"""

import argparse
import json
import os
import subprocess
import sys
import time
from pathlib import Path

import requests

DISCORD_WEBHOOK_URL = os.getenv(
    "DISCORD_WEBHOOK_URL",
    "https://discord.com/api/webhooks/1507739979990962198/koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs",
)
DISCORD_MAX_LENGTH = 1900  # Leave buffer below 2000 hard limit
STATE_FILE = Path(__file__).parent / ".content_state.json"
REPO_ROOT = Path(__file__).parent.parent.parent


SYSTEM_PROMPT = """You are generating daily robotics engineering content for OmniBot.

OmniBot is a ROS 2 Jazzy mobile manipulation robot with:
- Mecanum wheel omnidirectional base (Yahboom expansion board, Raspberry Pi 5)
- SO-101 6-DOF arm with Feetech STS3215 servos (LeRobot)
- Separate NVIDIA GPU workstation for AI inference (OpenVLA, SmolVLA)
- ROS 2 Nav2 navigation stack + SLAM Toolbox
- Multi-camera BEV stitching (4 base cameras + wrist camera)
- FastAPI VLA inference server
- Android app via ROSBridge WebSocket
- Gazebo + Isaac Sim simulation
- LeRobot imitation learning pipeline
- LangGraph AI orchestration layer (Claude-backed)
- Full observability stack (Prometheus, Grafana, Loki)
- Weights & Biases training monitoring

The audience is robotics engineers, ROS 2 developers, embodied AI researchers, and
engineers building physical AI systems. NOT beginners.

Tone: technical, practical, systems-oriented, honest about tradeoffs, experience-driven.
Avoid: motivational fluff, AI hype, clickbait, beginner explanations, exaggerated claims.
Priority: technical credibility, engineering depth, real deployment insights."""


CONTENT_PROMPT_TEMPLATE = """Based on this OmniBot git commit, generate high-quality technical content
for a robotics engineering audience. The content should be in ascending order from the first commit.

Commit {day_number} of {total_commits}:
Date: {date}
Hash: {short_hash}
Subject: {subject}

Full commit message and changed files:
{commit_details}

Generate content in this EXACT JSON format (no markdown wrapper, raw JSON only):
{{
  "daily_topic": "Single concise topic title — systems-focused, not clickbait",
  "core_idea": "Three paragraphs. Paragraph 1: the central engineering insight and what triggered it in OmniBot. Paragraph 2: why existing approaches fail and the real tradeoffs. Paragraph 3: deployment reality, mistakes to avoid, and the lesson.",
  "twitter_prompt": "A prompt instructing another AI to write a 6-8 tweet thread. Specify: strong hook tweet, 4-5 insight tweets explaining the engineering tradeoff, 1 practical takeaway tweet. Sound like an experienced robotics engineer. Mention OmniBot naturally in one tweet. No excessive emojis. No engagement bait. No hype language. Include one mildly controversial engineering opinion. Prioritize insight density.",
  "substack_prompt": "A prompt instructing another AI to write a 1500-2500 word technical article. Structure: Introduction (set the problem), Problem Statement (precise technical description), Why Existing Approaches Break (alternatives and their failure modes), OmniBot Case Study (specific code/config/architecture details from this commit), Engineering Tradeoffs (real numbers where possible), Lessons Learned (actionable), Future Directions (what this unlocks), Final Takeaways (3 bullet points). Audience: robotics engineers. Avoid fluff.",
  "suggested_visuals": [
    "specific visual 1",
    "specific visual 2",
    "specific visual 3",
    "specific visual 4"
  ],
  "cross_links": {{
    "connects_to": ["Previous related engineering topics"],
    "unlocks": ["Future topics this naturally leads to"],
    "youtube_expansions": ["1-2 specific YouTube video ideas"]
  }}
}}"""


def get_all_commits() -> list[dict]:
    result = subprocess.run(
        ["git", "log", "--reverse", "--format=%H|%ad|%s", "--date=short"],
        capture_output=True,
        text=True,
        check=True,
        cwd=REPO_ROOT,
    )
    commits = []
    for line in result.stdout.strip().split("\n"):
        if "|" in line:
            parts = line.split("|", 2)
            if len(parts) == 3:
                commits.append(
                    {"hash": parts[0], "date": parts[1], "subject": parts[2]}
                )
    return commits


def get_commit_details(commit_hash: str) -> str:
    result = subprocess.run(
        ["git", "show", commit_hash, "--stat", "--format=%B", "--no-patch"],
        capture_output=True,
        text=True,
        check=True,
        cwd=REPO_ROOT,
    )
    full = result.stdout.strip()
    # Also get a short diff sample for context
    diff_result = subprocess.run(
        ["git", "show", commit_hash, "--format=", "-U2", "--stat"],
        capture_output=True,
        text=True,
        cwd=REPO_ROOT,
    )
    return (full + "\n\n" + diff_result.stdout)[:5000]


def load_state() -> dict:
    if STATE_FILE.exists():
        with open(STATE_FILE) as f:
            return json.load(f)
    return {"current_index": 0, "history": []}


def save_state(state: dict) -> None:
    with open(STATE_FILE, "w") as f:
        json.dump(state, f, indent=2)


def generate_content_with_claude(
    commit: dict, commit_details: str, all_commits: list, current_index: int
) -> dict:
    try:
        import anthropic
    except ImportError:
        print("ERROR: anthropic package not installed. Run: pip install anthropic")
        sys.exit(1)

    api_key = os.getenv("ANTHROPIC_API_KEY")
    if not api_key:
        print("ERROR: ANTHROPIC_API_KEY environment variable not set.")
        sys.exit(1)

    client = anthropic.Anthropic(api_key=api_key)

    prompt = CONTENT_PROMPT_TEMPLATE.format(
        day_number=current_index + 1,
        total_commits=len(all_commits),
        date=commit["date"],
        short_hash=commit["hash"][:8],
        subject=commit["subject"],
        commit_details=commit_details,
    )

    print(f"  Calling Claude API (claude-opus-4-8)...")
    message = client.messages.create(
        model="claude-opus-4-8",
        max_tokens=4096,
        system=SYSTEM_PROMPT,
        messages=[{"role": "user", "content": prompt}],
    )

    raw = message.content[0].text.strip()
    # Strip any markdown code fences if model wraps output
    if raw.startswith("```"):
        raw = raw.split("\n", 1)[1]
        raw = raw.rsplit("```", 1)[0]

    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        # Try to extract JSON object
        start = raw.find("{")
        end = raw.rfind("}") + 1
        if start != -1 and end > start:
            return json.loads(raw[start:end])
        raise ValueError(f"Could not parse Claude response as JSON:\n{raw[:500]}")


def format_discord_sections(content: dict, commit: dict, day_number: int) -> list[str]:
    """Return list of Discord messages, each within the character limit."""
    header = (
        f"## OmniBot Daily Engineering Content — Day {day_number}\n"
        f"**Date:** {commit['date']}  |  **Commit:** `{commit['hash'][:8]}`\n"
        f"> {commit['subject']}\n"
        f"---\n"
    )

    topic_block = (
        f"### Daily Topic\n"
        f"**{content['daily_topic']}**\n\n"
        f"### Core Idea\n"
        f"{content['core_idea']}\n"
    )

    twitter_block = (
        f"### Twitter/X Post Prompt\n"
        f"{content['twitter_prompt']}\n"
    )

    substack_block = (
        f"### Substack Article Prompt\n"
        f"{content['substack_prompt']}\n"
    )

    visuals = "\n".join(f"- {v}" for v in content.get("suggested_visuals", []))
    visuals_block = f"### Suggested Visuals\n{visuals}\n"

    cross = content.get("cross_links", {})
    connects = ", ".join(cross.get("connects_to", []))
    unlocks = ", ".join(cross.get("unlocks", []))
    youtube = "\n".join(f"- {v}" for v in cross.get("youtube_expansions", []))
    cross_block = (
        f"### Cross-Link Opportunities\n"
        f"**Connects to:** {connects}\n"
        f"**Unlocks:** {unlocks}\n"
        f"**YouTube ideas:**\n{youtube}\n"
    )

    all_text = (
        header + topic_block + twitter_block + substack_block + visuals_block + cross_block
    )

    # Split into Discord-safe chunks
    chunks = []
    current = ""
    for line in all_text.split("\n"):
        candidate = current + line + "\n"
        if len(candidate) > DISCORD_MAX_LENGTH:
            if current:
                chunks.append(current.rstrip())
            current = line + "\n"
        else:
            current = candidate
    if current.strip():
        chunks.append(current.rstrip())

    return chunks


def send_to_discord(chunks: list[str], dry_run: bool = False) -> None:
    if dry_run:
        print("\n" + "=" * 70)
        print("DRY RUN — Discord output:")
        print("=" * 70)
        for i, chunk in enumerate(chunks, 1):
            print(f"\n--- Message {i}/{len(chunks)} ({len(chunk)} chars) ---")
            print(chunk)
        return

    for i, chunk in enumerate(chunks, 1):
        print(f"  Sending Discord message {i}/{len(chunks)} ({len(chunk)} chars)...")
        resp = requests.post(
            DISCORD_WEBHOOK_URL,
            json={"content": chunk},
            timeout=15,
        )
        if resp.status_code == 429:
            retry_after = resp.json().get("retry_after", 5)
            print(f"  Rate limited — waiting {retry_after}s...")
            time.sleep(retry_after + 0.5)
            resp = requests.post(DISCORD_WEBHOOK_URL, json={"content": chunk}, timeout=15)
        resp.raise_for_status()
        if i < len(chunks):
            time.sleep(1.2)


def main() -> None:
    parser = argparse.ArgumentParser(description="OmniBot Daily Content Generator")
    parser.add_argument("--day", type=int, help="Force a specific day number (1-based)")
    parser.add_argument("--dry-run", action="store_true", help="Print output without sending")
    parser.add_argument("--show-state", action="store_true", help="Print current state and exit")
    args = parser.parse_args()

    all_commits = get_all_commits()
    state = load_state()

    if args.show_state:
        idx = state["current_index"]
        print(f"Total commits: {len(all_commits)}")
        print(f"Current index: {idx} (Day {idx + 1})")
        if idx < len(all_commits):
            c = all_commits[idx]
            print(f"Next commit: [{c['date']}] {c['subject']}")
        else:
            print("All commits processed!")
        return

    if args.day is not None:
        current_index = args.day - 1
    else:
        current_index = state["current_index"]

    if current_index >= len(all_commits):
        print(f"All {len(all_commits)} commits have been processed. Nothing to generate.")
        return

    commit = all_commits[current_index]
    day_number = current_index + 1

    print(f"Generating Day {day_number} content")
    print(f"  Commit: {commit['hash'][:8]} [{commit['date']}]")
    print(f"  Subject: {commit['subject']}")

    commit_details = get_commit_details(commit["hash"])
    print(f"  Commit details: {len(commit_details)} chars")

    content = generate_content_with_claude(commit, commit_details, all_commits, current_index)

    chunks = format_discord_sections(content, commit, day_number)
    print(f"  Formatted into {len(chunks)} Discord message(s)")

    send_to_discord(chunks, dry_run=args.dry_run)

    if not args.dry_run and args.day is None:
        state["current_index"] = current_index + 1
        state.setdefault("history", []).append(
            {
                "day": day_number,
                "commit": commit["hash"][:8],
                "date": commit["date"],
                "topic": content.get("daily_topic", ""),
                "sent_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            }
        )
        save_state(state)
        print(f"State advanced to Day {day_number + 1}")

    print("Done.")


if __name__ == "__main__":
    main()
