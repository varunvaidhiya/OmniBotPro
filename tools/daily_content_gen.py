#!/usr/bin/env python3
"""
OmniBot Daily Content Generator

Walks OmniBot git commits in chronological order (earliest first), picks the
next unprocessed substantive commit, generates robotics engineering content
with Claude, and delivers it to a Discord webhook.

State is persisted in tools/content_gen_state.json so the CI job always
advances to the next commit, never repeating unless all commits are exhausted
(at which point the index resets to 0).

Required environment variables:
    ANTHROPIC_API_KEY   — Anthropic API key
    DISCORD_WEBHOOK_URL — Discord incoming webhook URL
"""

import json
import os
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import anthropic
import httpx

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
REPO_ROOT = Path(__file__).resolve().parent.parent
STATE_FILE = Path(__file__).resolve().parent / "content_gen_state.json"

# ---------------------------------------------------------------------------
# Discord
# ---------------------------------------------------------------------------
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL", "").strip()
DISCORD_MAX_CHARS = 1900  # leave headroom below the 2 000-char limit

# ---------------------------------------------------------------------------
# Prompts
# ---------------------------------------------------------------------------
SYSTEM_PROMPT = """\
You are a senior robotics systems engineer with deep, hands-on experience in
ROS 2, embodied AI, mobile manipulation, and real-world robot deployment.
You are building OmniBot — a ROS 2 Jazzy mobile manipulation robot consisting
of a mecanum-wheel omnidirectional base, an SO-101 6-DoF arm, a Raspberry Pi 5
for onboard control, and a separate NVIDIA GPU workstation for VLA inference
(OpenVLA / SmolVLA).

You write technical content for an audience of robotics engineers, ROS 2
developers, and embodied AI researchers — NOT beginners. Your writing:
- Is technically accurate and defensible
- Exposes engineering tradeoffs and deployment realities honestly
- Avoids AI hype, motivational fluff, and generic startup language
- Sounds like someone who is actively building and breaking physical robots
- Connects software decisions to physical-world consequences
"""

USER_PROMPT = """\
Analyze the following OmniBot git commit and generate one day's engineering
content. Base the insight on the specific engineering problem this commit
solves, the decision made, and what it teaches.

=== COMMIT METADATA ===
Hash: {commit_hash}
Date: {commit_date}
Subject: {subject}
Body:
{body}

Changed files summary:
{changed_files}

=== TASK ===
Return a single JSON object with exactly these keys (no markdown fencing,
no extra keys):

{{
  "daily_topic": "A single, sharp topic title that exposes a non-obvious robotics engineering truth revealed by this commit. Examples: 'Why ros_gz_bridge YAML Activation Matters for Simulation Fidelity', 'Why Joint Name Prefixes in URDF Break Cross-Layer Integration'",

  "core_idea": "Three tight paragraphs. Paragraph 1: the central engineering insight and why it is non-obvious. Paragraph 2: why this matters in production robotics systems and what breaks if ignored. Paragraph 3: what OmniBot hit specifically, the mistake made, how it was debugged, and the concrete fix applied. Include quantifiable details where possible.",

  "twitter_prompt": "A detailed prompt for another Claude instance to write a 6-8 tweet Twitter/X thread. The prompt must specify: (1) open with a hook that names the specific technical failure, not generic wisdom; (2) tweet 2 explains the root cause at the code/architecture level; (3) tweets 3-5 walk through the diagnosis and fix with specifics; (4) tweet 6 states the transferable engineering rule; (5) optional tweet 7 is a controversial or counterintuitive opinion for robotics engineers; (6) tone is experienced practitioner, no excessive emojis, no hype, no engagement bait; (7) mention OmniBot naturally in one tweet.",

  "substack_prompt": "A detailed prompt for another Claude instance to write a 1800-2500 word Substack engineering article. The prompt must specify: exact section headings (Introduction, The Problem, Why Existing Approaches Break, OmniBot Case Study, Engineering Tradeoffs, Lessons Learned, Future Directions, Final Takeaways); specific technical details to include in each section; the depth of systems analysis expected; the exact audience (ROS 2 engineers, robotics systems builders); tone (analytical, direct, no fluff); and that the article should include at least two concrete tradeoffs with pros/cons.",

  "suggested_visuals": [
    "list of 4-6 specific visuals that would strengthen the post — be precise (e.g. 'RViz screenshot showing TF tree before and after the joint naming fix', not just 'RViz screenshot')"
  ],

  "cross_link_opportunities": {{
    "previous_topics": ["2-3 topics this naturally follows from"],
    "future_topics": ["2-3 topics this naturally unlocks"],
    "youtube_angles": ["1-2 specific YouTube video ideas this could become"]
  }}
}}
"""


# ---------------------------------------------------------------------------
# Git helpers
# ---------------------------------------------------------------------------

def get_substantive_commits() -> list[dict]:
    """Return all non-merge commits in chronological order."""
    result = subprocess.run(
        [
            "git", "log",
            "--reverse",
            "--no-merges",
            "--format=%H|||%ad|||%s",
            "--date=short",
        ],
        capture_output=True,
        text=True,
        cwd=REPO_ROOT,
        check=True,
    )
    commits = []
    for line in result.stdout.strip().splitlines():
        parts = line.split("|||", 2)
        if len(parts) == 3:
            commits.append(
                {"hash": parts[0].strip(), "date": parts[1].strip(), "subject": parts[2].strip()}
            )
    return commits


def get_commit_body(commit_hash: str) -> str:
    result = subprocess.run(
        ["git", "show", "--format=%b", "-s", commit_hash],
        capture_output=True,
        text=True,
        cwd=REPO_ROOT,
        check=True,
    )
    return result.stdout.strip()


def get_commit_stat(commit_hash: str) -> str:
    result = subprocess.run(
        ["git", "show", "--stat", "--format=", commit_hash],
        capture_output=True,
        text=True,
        cwd=REPO_ROOT,
        check=True,
    )
    # Trim to avoid blowing out the context window
    lines = result.stdout.strip().splitlines()
    trimmed = lines[:40]
    if len(lines) > 40:
        trimmed.append(f"... ({len(lines) - 40} more lines)")
    return "\n".join(trimmed)


# ---------------------------------------------------------------------------
# State management
# ---------------------------------------------------------------------------

def load_state() -> dict:
    if STATE_FILE.exists():
        try:
            return json.loads(STATE_FILE.read_text())
        except (json.JSONDecodeError, OSError):
            pass
    return {"last_processed_index": -1, "processed_commits": [], "last_run": None}


def save_state(state: dict) -> None:
    STATE_FILE.write_text(json.dumps(state, indent=2) + "\n")


# ---------------------------------------------------------------------------
# Content generation
# ---------------------------------------------------------------------------

def _make_anthropic_client() -> anthropic.Anthropic:
    """
    Build an Anthropic client.

    Supports two auth modes:
    1. OAuth bearer token — used by Claude Code web sessions. Sourced from
       ANTHROPIC_BEARER_TOKEN env var or the file at
       CLAUDE_SESSION_INGRESS_TOKEN_FILE.
    2. Standard API key via ANTHROPIC_API_KEY env var (production / CI secret).
    """
    bearer_token = os.environ.get("ANTHROPIC_BEARER_TOKEN", "").strip()
    if not bearer_token:
        token_file = os.environ.get("CLAUDE_SESSION_INGRESS_TOKEN_FILE", "")
        if token_file and Path(token_file).exists():
            bearer_token = Path(token_file).read_text().strip()

    if bearer_token:
        return anthropic.Anthropic(auth_token=bearer_token)

    # Fall back to standard ANTHROPIC_API_KEY
    return anthropic.Anthropic()


def generate_content(commit: dict) -> dict:
    body = get_commit_body(commit["hash"])
    stat = get_commit_stat(commit["hash"])

    client = _make_anthropic_client()

    message = client.messages.create(
        model="claude-opus-4-8",
        max_tokens=4096,
        system=SYSTEM_PROMPT,
        messages=[
            {
                "role": "user",
                "content": USER_PROMPT.format(
                    commit_hash=commit["hash"][:8],
                    commit_date=commit["date"],
                    subject=commit["subject"],
                    body=body[:2500] if body else "(no extended body)",
                    changed_files=stat[:1500] if stat else "(no file stat)",
                ),
            }
        ],
    )

    raw = message.content[0].text.strip()

    # Strip markdown code fences if the model wraps the JSON
    if raw.startswith("```"):
        raw = raw.split("\n", 1)[1]
        raw = raw.rsplit("```", 1)[0].strip()

    return json.loads(raw)


# ---------------------------------------------------------------------------
# Discord delivery
# ---------------------------------------------------------------------------

def _post_chunk(webhook_url: str, text: str, retries: int = 3) -> None:
    delay = 2
    for attempt in range(retries):
        try:
            resp = httpx.post(webhook_url, json={"content": text}, timeout=10)
            if resp.status_code == 429:
                wait = int(resp.headers.get("Retry-After", delay))
                time.sleep(wait)
                continue
            resp.raise_for_status()
            return
        except httpx.HTTPError as exc:
            if attempt == retries - 1:
                raise
            time.sleep(delay)
            delay *= 2


def _split_and_send(webhook_url: str, text: str) -> None:
    """Split text into ≤DISCORD_MAX_CHARS chunks and post each."""
    while text:
        chunk, text = text[:DISCORD_MAX_CHARS], text[DISCORD_MAX_CHARS:]
        _post_chunk(webhook_url, chunk)
        if text:
            time.sleep(0.5)  # avoid rate limits


def send_to_discord(content: dict, commit: dict, webhook_url: str) -> None:
    visuals_md = "\n".join(f"- {v}" for v in content.get("suggested_visuals", []))
    clo = content.get("cross_link_opportunities", {})
    prev = ", ".join(clo.get("previous_topics", []))
    future = ", ".join(clo.get("future_topics", []))
    yt = ", ".join(clo.get("youtube_angles", []))

    sections = [
        (
            f"# OmniBot Daily Content — {commit['date']}\n"
            f"**Commit:** `{commit['hash'][:8]}` — *{commit['subject']}*\n"
            f"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n"
        ),
        f"## 1. Daily Topic\n**{content['daily_topic']}**\n",
        f"## 2. Core Idea\n{content['core_idea']}\n",
        f"## 3. Twitter/X Thread Prompt\n{content['twitter_prompt']}\n",
        f"## 4. Substack Article Prompt\n{content['substack_prompt']}\n",
        f"## 5. Suggested Visuals\n{visuals_md}\n",
        (
            f"## 6. Cross-Link Opportunities\n"
            f"**Previous topics:** {prev}\n"
            f"**Future topics:** {future}\n"
            f"**YouTube angles:** {yt}\n"
        ),
    ]

    for section in sections:
        _split_and_send(webhook_url, section)
        time.sleep(0.3)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> int:
    webhook_url = DISCORD_WEBHOOK_URL
    if not webhook_url:
        print("ERROR: DISCORD_WEBHOOK_URL environment variable is not set.", file=sys.stderr)
        return 1

    # Verify at least one auth mechanism is present
    has_key = bool(os.environ.get("ANTHROPIC_API_KEY", "").strip())
    has_bearer = bool(os.environ.get("ANTHROPIC_BEARER_TOKEN", "").strip())
    token_file = os.environ.get("CLAUDE_SESSION_INGRESS_TOKEN_FILE", "")
    has_token_file = bool(token_file and Path(token_file).exists())
    if not (has_key or has_bearer or has_token_file):
        print(
            "ERROR: No Anthropic auth found. Set ANTHROPIC_API_KEY or ANTHROPIC_BEARER_TOKEN.",
            file=sys.stderr,
        )
        return 1

    commits = get_substantive_commits()
    if not commits:
        print("No commits found in repository.", file=sys.stderr)
        return 1

    state = load_state()
    next_index = state["last_processed_index"] + 1

    if next_index >= len(commits):
        print(
            f"All {len(commits)} commits processed. Cycling back to the beginning.",
            file=sys.stderr,
        )
        next_index = 0

    commit = commits[next_index]
    print(
        f"Processing commit {next_index + 1}/{len(commits)}: "
        f"{commit['hash'][:8]} ({commit['date']}) — {commit['subject']}"
    )

    content = generate_content(commit)
    print(f"Generated topic: {content['daily_topic']}")

    send_to_discord(content, commit, webhook_url)
    print("Delivered to Discord.")

    state["last_processed_index"] = next_index
    state["processed_commits"].append(commit["hash"])
    state["last_run"] = datetime.now(timezone.utc).isoformat()
    state["last_topic"] = content["daily_topic"]
    save_state(state)

    return 0


if __name__ == "__main__":
    sys.exit(main())
