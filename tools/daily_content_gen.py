#!/usr/bin/env python3
"""
OmniBot Daily Content Generator
Generates robotics engineering content from git history and sends to Discord.
Processes commits in chronological order, one per run (state tracked in .content_state).
"""

import json
import os
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

import requests

WEBHOOK_URL = (
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
)
STATE_FILE = Path(__file__).parent / ".content_state.json"
REPO_ROOT = Path(__file__).parent.parent
DISCORD_MAX_CHARS = 2000


def run_git(args: list[str]) -> str:
    result = subprocess.run(
        ["git"] + args,
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip()


def get_commits_chronological() -> list[dict]:
    log = run_git(["log", "--reverse", "--format=%H|||%s|||%ai"])
    commits = []
    for line in log.splitlines():
        parts = line.split("|||", 2)
        if len(parts) == 3:
            commits.append({"hash": parts[0], "subject": parts[1], "date": parts[2]})
    return commits


def load_state() -> dict:
    if STATE_FILE.exists():
        return json.loads(STATE_FILE.read_text())
    return {"last_index": -1}


def save_state(state: dict) -> None:
    STATE_FILE.write_text(json.dumps(state, indent=2))


def get_commit_diff_summary(commit_hash: str) -> str:
    stat = run_git(["show", commit_hash, "--stat"])
    return stat[:2000]


def build_day1_content() -> dict:
    """Day 1: First commit — reverse-engineering the Yahboom serial protocol."""
    return {
        "topic": "Day 1 — Why Robotics Software Development Starts with Protocol Archaeology",
        "core_idea": (
            "Before OmniBot could move a single wheel under ROS 2 control, the Yahboom "
            "Rosmaster serial protocol had to be reverse-engineered from scratch. "
            "The board ships without a public API spec. The commit that initialised the "
            "entire project (`609f161`) contains `analyze_packets.py` and `analyze_sniff.py` "
            "— brute-force checksum searchers run against USB-sniffed hex dumps. "
            "The discovered formula: `checksum = (sum(all_bytes) + 5) & 0xFF` where "
            "`5 = 257 - 0xFC` compensates for the device-ID complement. "
            "This is the hidden first cost of building on commercial embedded hardware: "
            "your driver work begins with packet archaeology, not SLAM tuning. "
            "Skipping this stage — trusting vendor sample code blindly — causes silent "
            "motion errors that corrupt your odometry from frame one. "
            "The lesson: never assume a binary protocol is correct; always capture and "
            "verify with known-good ground-truth motion before integrating with ROS."
        ),
        "twitter_prompt": (
            "Write a concise, high-signal Twitter/X thread (5–7 tweets) for a senior "
            "robotics engineer audience. The author is building OmniBot — a ROS 2 "
            "mecanum-wheel mobile manipulation robot. Today's topic: the hidden first "
            "task in any commercial-hardware robot project is reverse-engineering the "
            "serial protocol. The hook: 'Before OmniBot drove a single millimetre under "
            "ROS 2 control, I had to reverse-engineer a binary serial protocol.' "
            "Cover: why vendor docs fail, the brute-force checksum hunt, the actual "
            "formula discovered (sum + complement trick), why silent wrong checksums "
            "corrupt odometry, and the lesson for robotics engineers. Tone: experienced, "
            "direct, no hype. One controversial opinion: most open-source ROS drivers "
            "for commercial hardware skip protocol validation entirely. No excessive "
            "emojis. End with one practical takeaway a reader can apply this week."
        ),
        "substack_prompt": (
            "Write a long-form Substack engineering article titled: "
            "'Why Robotics Software Development Starts with Protocol Archaeology'. "
            "Target audience: ROS 2 engineers and robotics developers, NOT beginners. "
            "The author is building OmniBot — a ROS 2 Jazzy mecanum-wheel robot with a "
            "Yahboom Rosmaster expansion board as the motor controller. "
            "\n\nStructure the article as follows:\n"
            "1. Introduction — the hidden first task nobody warns you about\n"
            "2. Problem Statement — why vendor serial protocols are often undocumented "
            "or only partially documented\n"
            "3. Why Existing Approaches Break — trusting vendor SDK sample code, "
            "byte-copying without validation\n"
            "4. OmniBot Case Study — the actual reverse-engineering process: USB sniffing, "
            "packet capture, brute-force checksum algorithms tested (sum, XOR, sum+len, "
            "sum+payload), and the discovered formula: `(sum(all_bytes) + 257 - DEVICE_ID) "
            "& 0xFF`\n"
            "5. Engineering Tradeoffs — time cost vs correctness, maintaining a canonical "
            "protocol module vs inlining magic bytes\n"
            "6. Failure Cases — what happens when the checksum is silently wrong: motor "
            "commands ignored, odometry corrupted, SLAM drift from frame one\n"
            "7. Lessons Learned — always capture real packets, build a standalone "
            "protocol library before the ROS node, test with ground-truth motion\n"
            "8. Future Directions — fuzz testing the protocol layer, CI for serial "
            "protocol regression\n"
            "9. Final Takeaways — three actionable points for any robotics engineer "
            "starting with commercial hardware\n"
            "\nTone: analytical, systems-oriented, honest about the time lost. "
            "Include code snippets where relevant. No motivational fluff."
        ),
        "visuals": [
            "Terminal output of `analyze_packets.py` showing checksum candidates being tested",
            "Side-by-side: raw hex dump vs decoded packet fields (annotated screenshot)",
            "Photo of Yahboom Rosmaster board + USB cable to Raspberry Pi 5",
            "ROS 2 `rqt_topic` or `ros2 topic echo /odom` showing clean odometry after correct protocol",
            "Diff snippet from `confirmed_protocol.py` showing the checksum formula",
            "Block diagram: Android App → ROSBridge → ROS 2 node → Serial → Yahboom board",
        ],
        "cross_links": {
            "previous": ["N/A — this is Day 1"],
            "next": [
                "Day 2: Why 20 Hz matters — control loop timing in embedded serial drivers",
                "Day 3: Velocity ramping as a hardware safety layer",
                "Future: Why standalone protocol packages save ROS projects",
            ],
            "related_themes": [
                "Hardware abstraction layers",
                "ROS 2 driver architecture",
                "Embedded serial debugging",
                "CI for hardware protocol tests",
            ],
            "youtube_expansion": (
                "Live-coding episode: sniff a commercial robot serial port, "
                "identify the checksum, and write a ROS 2 driver from scratch"
            ),
        },
    }


def build_content_for_commit(index: int, commit: dict) -> dict:
    subject = commit["subject"]
    date = commit["date"][:10]

    # Day-specific content dispatch
    if index == 0:
        content = build_day1_content()
    else:
        # Generic template for subsequent days (extend with more specific handlers)
        content = {
            "topic": f"Day {index + 1} — {subject}",
            "core_idea": (
                f"Commit #{index + 1} ({date}): '{subject}'. "
                "Engineering insight and lessons from this change to be developed."
            ),
            "twitter_prompt": f"Write a Twitter thread about: {subject}",
            "substack_prompt": f"Write a Substack article about: {subject}",
            "visuals": ["Architecture diagram", "Terminal output", "Hardware photo"],
            "cross_links": {"previous": [], "next": [], "related_themes": [], "youtube_expansion": ""},
        }

    content["commit_index"] = index + 1
    content["commit_hash"] = commit["hash"][:8]
    content["commit_subject"] = subject
    content["commit_date"] = date
    return content


def chunk_message(text: str, max_len: int = DISCORD_MAX_CHARS) -> list[str]:
    if len(text) <= max_len:
        return [text]
    chunks = []
    while text:
        if len(text) <= max_len:
            chunks.append(text)
            break
        split_at = text.rfind("\n", 0, max_len)
        if split_at == -1:
            split_at = max_len
        chunks.append(text[:split_at])
        text = text[split_at:].lstrip("\n")
    return chunks


def send_to_discord(content: dict) -> bool:
    today = datetime.now().strftime("%Y-%m-%d")
    commit_info = (
        f"**Commit #{content['commit_index']}** · `{content['commit_hash']}` · "
        f"{content['commit_date']}\n_{content['commit_subject']}_"
    )

    sections = [
        f"# OmniBot Daily Content — {today}\n{commit_info}",
        f"## Daily Topic\n**{content['topic']}**",
        f"## Core Idea\n{content['core_idea']}",
        f"## Twitter/X Post Prompt\n{content['twitter_prompt']}",
        f"## Substack Article Prompt\n{content['substack_prompt']}",
        "## Suggested Visuals\n" + "\n".join(f"• {v}" for v in content["visuals"]),
        (
            "## Cross-Link Opportunities\n"
            f"**Previous topics:** {', '.join(content['cross_links']['previous'])}\n"
            f"**Next topics:** {', '.join(content['cross_links']['next'])}\n"
            f"**Related themes:** {', '.join(content['cross_links']['related_themes'])}\n"
            f"**YouTube expansion:** {content['cross_links']['youtube_expansion']}"
        ),
    ]

    all_messages: list[str] = []
    for section in sections:
        all_messages.extend(chunk_message(section))

    success = True
    for i, msg in enumerate(all_messages):
        payload = {"content": msg, "username": "OmniBot Content Bot"}
        for attempt in range(4):
            try:
                resp = requests.post(WEBHOOK_URL, json=payload, timeout=10)
                if resp.status_code in (200, 204):
                    print(f"  [sent {i + 1}/{len(all_messages)}]")
                    break
                elif resp.status_code == 429:
                    retry_after = resp.json().get("retry_after", 2)
                    print(f"  [rate-limited, waiting {retry_after}s]")
                    time.sleep(retry_after)
                else:
                    print(f"  [HTTP {resp.status_code}: {resp.text[:200]}]")
                    time.sleep(2 ** attempt)
            except requests.RequestException as exc:
                print(f"  [error attempt {attempt + 1}: {exc}]")
                time.sleep(2 ** attempt)
        else:
            print(f"  [failed to send chunk {i + 1}]")
            success = False
        time.sleep(0.5)

    return success


def main() -> None:
    commits = get_commits_chronological()
    if not commits:
        print("No commits found.")
        sys.exit(1)

    state = load_state()
    next_index = state["last_index"] + 1

    if next_index >= len(commits):
        print(f"All {len(commits)} commits processed. Reset .content_state.json to restart.")
        sys.exit(0)

    commit = commits[next_index]
    print(f"Generating content for commit {next_index + 1}/{len(commits)}: {commit['subject']}")

    content = build_content_for_commit(next_index, commit)

    print("Sending to Discord...")
    ok = send_to_discord(content)

    if ok:
        state["last_index"] = next_index
        save_state(state)
        print(f"Done. Next run will process commit {next_index + 2}.")
    else:
        print("Delivery had errors — state NOT advanced.")
        sys.exit(1)


if __name__ == "__main__":
    main()
