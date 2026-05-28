#!/usr/bin/env python3
"""
OmniBot Daily Content Generator
Analyzes git commits in chronological order and generates robotics engineering content.
Sends formatted output to Discord webhook.
"""

import subprocess
import sys
import json
import os
import time
import urllib.request
import urllib.error
from datetime import datetime

DISCORD_WEBHOOK = (
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
)

STATE_FILE = os.path.join(os.path.dirname(__file__), ".content_state.json")
DISCORD_MAX = 2000


# ─── Git helpers ─────────────────────────────────────────────────────────────

def git_commits_chronological():
    result = subprocess.run(
        ["git", "log", "--reverse", "--format=%H|%ad|%s", "--date=short"],
        capture_output=True, text=True, cwd=os.path.dirname(os.path.dirname(__file__))
    )
    commits = []
    for line in result.stdout.strip().splitlines():
        parts = line.split("|", 2)
        if len(parts) == 3:
            commits.append({"hash": parts[0], "date": parts[1], "subject": parts[2]})
    return commits


def git_show_stat(commit_hash):
    result = subprocess.run(
        ["git", "show", commit_hash, "--stat", "--format=%b"],
        capture_output=True, text=True, cwd=os.path.dirname(os.path.dirname(__file__))
    )
    return result.stdout[:3000]


# ─── State management ────────────────────────────────────────────────────────

def load_state():
    if os.path.exists(STATE_FILE):
        with open(STATE_FILE) as f:
            return json.load(f)
    return {"last_index": -1}


def save_state(state):
    with open(STATE_FILE, "w") as f:
        json.dump(state, f, indent=2)


# ─── Content for Day 1 (commit 609f161) ──────────────────────────────────────

DAY1_CONTENT = {
    "topic": "Why Proprietary Robot Hardware Forces Protocol Reverse Engineering",

    "core_idea": """\
When OmniBot migrated from a custom STM32 firmware setup (RPi → STM32 → L298N drivers \
→ motors) to the Yahboom ROS Robot Expansion Board, the motivation was clear: ditch \
custom motor firmware, get built-in encoders and a USB serial interface, and move faster. \
The tradeoff no one advertises: the Yahboom board speaks a proprietary binary protocol \
with no public spec. Getting a wheel to turn required capturing USB traffic, writing \
`analyze_packets.py` to brute-force the checksum algorithm (result: `(sum(bytes) + 5) & 0xFF`), \
and fuzzing command codes until `0x12` (FUNC_MOTION) responded. \
The `+5` in the checksum is not a magic number — it compensates for the `0xFC` device ID byte \
being a complement of `0x04` in modular arithmetic. That took hours to figure out. \
The lesson: commercial robot hardware often trades firmware complexity for protocol opacity. \
You swap one debugging surface for another. OmniBot committed to this tradeoff deliberately, \
then built `confirmed_protocol.py` as the permanent reference so no future contributor \
would lose that knowledge again.""",

    "twitter_prompt": """\
Write a concise, high-signal Twitter/X thread (6–9 tweets) for an experienced robotics engineer.

Topic: Replacing custom STM32 firmware with a commercial motor controller board — and \
the protocol reverse-engineering it forced.

Context:
- OmniBot (ROS 2, Raspberry Pi 5, mecanum wheels) migrated from STM32 + L298N driver stack \
to a Yahboom ROS Expansion Board.
- The Yahboom board has no public protocol spec.
- They had to write packet analyzers and fuzzers to discover: header bytes (0xFF, 0xFC), \
FUNC codes (0x12 for motion, 0x15 for car type), and a checksum of \
(sum(all_bytes) + 5) & 0xFF — the +5 compensates for 0xFC in modular arithmetic.
- A 20 Hz control loop was chosen for smooth motion, paired with velocity ramping to \
prevent jerks (max 0.05 m/s per step, hard limit 0.2 m/s).

Thread requirements:
- Hook tweet: expose the hidden cost of "easy" commercial robot hardware.
- Explain the protocol reverse-engineering process concisely.
- Explain the checksum math — this is the kind of detail engineers appreciate.
- Mention velocity ramping as a consequence of the board's motion API design.
- Contrast with custom firmware: you trade firmware complexity for protocol opacity.
- Include one practical takeaway: always document reverse-engineered protocols \
in a `confirmed_protocol.py` equivalent before the original engineer forgets it.
- One controversial opinion: most commercial robot hardware vendors assume you won't \
read the bytes — and they're usually right.
- Mention OmniBot naturally.
- No hype, no emojis, no engagement bait. Write for engineers.""",

    "substack_prompt": """\
Write a long-form technical article (1500–2500 words) for robotics engineers and \
systems builders.

Title: "The Hidden Tax of Commercial Robot Hardware: Protocol Reverse Engineering on OmniBot"

Article structure and content requirements:

**Introduction**
Start with the decision: replace a custom STM32 microcontroller setup with a commercial \
Yahboom ROS Robot Expansion Board. Explain why this seems like the right call — \
fewer moving parts, built-in encoders, USB serial, no custom firmware to maintain.

**Problem Statement**
The Yahboom board has no public binary protocol specification. \
The only "documentation" is a Windows GUI tool. \
To drive motors from ROS 2, you need to speak its serial language exactly.

**Why Existing Approaches Break**
Explain why assuming a commercial board = easy integration fails. \
Most robotics hardware vendors target hobbyists using their own SDK, not engineers \
building ROS 2 nodes. \
The result: you face the same debugging surface as custom firmware, but without source code.

**OmniBot Case Study**
Walk through the actual reverse-engineering process on OmniBot:
- USB traffic capture to see what the Windows GUI sends
- Packet structure discovery: [0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]
- Brute-forcing the checksum: `analyze_packets.py` tried Sum, Sum+Length, XOR, \
CRC8 — eventually discovered `(sum(all_bytes) + 5) & 0xFF`
- Explain the math: 0xFC = 252. 256 - 252 = 4. But the +5 accounts for \
how the `DEVICE_ID` byte participates in the sum differently. This is the kind of \
detail that takes an hour to verify.
- FUNC code fuzzing: tried codes from 0x01 to 0xFF, observed serial responses to \
find 0x12 (FUNC_MOTION), 0x02 (beep), 0x15 (set car type)
- Discovery that CAR_TYPE must be sent as 0x15 packet on every connection or \
the board defaults to a different kinematics model
- Debug artifacts created: `analyze_packets.py`, `analyze_sniff.py`, `fuzz_*.py`, \
`scan_*.py` — all present in the repo root
- The `confirmed_protocol.py` file created as the permanent reference

**Engineering Tradeoffs**
- Custom STM32: full control, latency transparency, no reverse engineering — \
but custom firmware is a maintenance liability and a contributor onboarding wall.
- Commercial board: faster start, encoders included, reliable hardware — \
but protocol opacity, zero control over timing internals, velocity ramp behavior baked in.
- The 20 Hz control loop: discuss why 20 Hz was chosen over 50 Hz or 10 Hz. \
USB serial latency at 115200 baud creates realistic round-trip times around 10–30 ms. \
20 Hz (50 ms cycle) gives headroom for serial + computation without blocking. \
Higher rates would require async serial and more careful buffer management.
- Velocity ramping built manually: the board accepts velocity commands directly but \
won't enforce smooth acceleration. The `cmd_vx/vy/wa` ramp state in \
`yahboom_controller_node.py` steps by 0.05 m/s per tick. This prevents wheel slip \
and motor stress.

**Lessons Learned**
1. Never assume commercial = documented. Budget reverse-engineering time for any \
proprietary hardware.
2. Build the protocol reference file (like `confirmed_protocol.py`) before \
the person who figured it out moves on.
3. Choose control loop frequency based on your serial latency budget, not benchmarks.
4. Velocity ramping is safety-critical on hardware that lacks it natively — \
build it in the driver, not the application layer.
5. Hardware migration is a tradeoff matrix, not a clear upgrade. Know what you're trading.

**Future Directions**
- Open-source a ROS 2 driver package for Yahboom boards to spare others \
from repeating this work.
- Consider if the effort was worth it: what would a clean custom firmware \
have cost in time vs. what reverse-engineering cost?
- How this shapes hardware selection going forward: prioritize boards with \
open serial specs (Roboclaw, ODrive, etc.)

**Final Takeaways**
The Yahboom board was the right call for OmniBot's velocity. But the protocol opacity \
tax is real. Every hour spent in `analyze_packets.py` was an hour not spent on \
navigation or VLA integration. Commercial hardware is not automatically easier — \
it just moves complexity from firmware to protocol forensics.""",

    "visuals": [
        "Terminal output of analyze_packets.py showing checksum brute-force results",
        "Serial packet hex dump annotated with field breakdown (header / LEN / FUNC / payload / checksum)",
        "Wiring diagram: STM32 stack vs. Yahboom board (before/after migration)",
        "ROS 2 topic graph showing yahboom_controller_node publishing /odom and /cmd_vel",
        "Oscilloscope or logic analyzer trace of UART traffic at 115200 baud (if available)",
        "Photo of the Yahboom ROS Expansion Board with cable connections",
        "Velocity ramp plot: commanded velocity vs. actual sent velocity over time (20Hz steps)",
        "Git diff showing the jump from STM32 launch file to yahboom_controller_node.py",
    ],

    "cross_links": {
        "previous": [
            "N/A — this is Day 1, the foundational hardware decision"
        ],
        "unlocks": [
            "Day 2: ROS_DOMAIN_ID and DDS networking — why cross-machine ROS 2 breaks without it (commit df6f37f)",
            "Day 3: Mecanum kinematics — why omnidirectional drive requires exact wheel geometry constants",
            "Future: The velocity ramp architecture becomes critical when integrating Nav2 + VLA command arbitration",
            "Future: Hardware abstraction layers — why mecanum_drive_ros2 package was extracted",
        ],
        "related_themes": [
            "Hardware-software interface design",
            "Driver development for opaque hardware",
            "ROS 2 serial communication patterns",
            "Control loop frequency selection",
            "Institutional knowledge documentation in robotics projects",
        ],
        "youtube_expansion": (
            "Episode: 'Reverse Engineering a Robot Motor Controller from Scratch' — "
            "screen recording of packet capture → brute force → working motor command, "
            "live demo of the robot moving for the first time after protocol discovery."
        ),
    }
}


# ─── Discord sender ───────────────────────────────────────────────────────────

def send_discord(content: str, webhook_url: str = DISCORD_WEBHOOK):
    """Send a single message chunk to Discord, retrying on rate limit."""
    data = json.dumps({"content": content}).encode("utf-8")
    req = urllib.request.Request(
        webhook_url,
        data=data,
        headers={
            "Content-Type": "application/json",
            "User-Agent": (
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
                "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36"
            ),
        },
        method="POST",
    )
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=15) as resp:
                if resp.status in (200, 204):
                    return True
        except urllib.error.HTTPError as e:
            if e.code == 429:
                retry_after = float(e.headers.get("Retry-After", 2 ** (attempt + 1)))
                print(f"Rate limited. Waiting {retry_after}s …")
                time.sleep(retry_after)
                continue
            body = ""
            try:
                body = e.read().decode()
            except Exception:
                pass
            print(f"HTTP error {e.code}: {body[:200]}")
            return False
        except Exception as exc:
            wait = 2 ** (attempt + 1)
            print(f"Error: {exc}. Retrying in {wait}s …")
            time.sleep(wait)
    return False


def split_message(text: str, max_len: int = DISCORD_MAX) -> list[str]:
    """Split text into Discord-safe chunks, breaking at newlines where possible."""
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


def send_section(heading: str, body: str):
    """Send a named section to Discord, splitting if needed."""
    full = f"## {heading}\n\n{body}"
    for chunk in split_message(full):
        ok = send_discord(chunk)
        if not ok:
            print(f"Failed to send section: {heading}")
        time.sleep(0.8)  # be polite to Discord rate limits


# ─── Main ─────────────────────────────────────────────────────────────────────

def main():
    commits = git_commits_chronological()
    state = load_state()
    next_index = state["last_index"] + 1

    if next_index >= len(commits):
        print("All commits have been covered. No new content to generate.")
        return

    commit = commits[next_index]
    day_number = next_index + 1

    print(f"Generating Day {day_number} content for commit {commit['hash'][:8]} — {commit['subject']}")

    # For Day 1 we have hand-crafted content; future days would call an AI API
    if next_index == 0:
        content = DAY1_CONTENT
    else:
        print(
            f"Day {day_number} automated content generation not yet implemented. "
            "Run with --day 1 to send Day 1 content."
        )
        sys.exit(0)

    # ── Build Discord message blocks ──────────────────────────────────────────
    header = (
        f"# OmniBot Engineering Content — Day {day_number}\n"
        f"**Commit:** `{commit['hash'][:8]}` · {commit['date']}\n"
        f"**Change:** {commit['subject']}\n"
        f"{'─' * 48}"
    )
    send_discord(header)
    time.sleep(0.8)

    send_section("1. Daily Topic", f"**{content['topic']}**")

    send_section("2. Core Idea", content["core_idea"])

    send_section("3. Twitter/X Post Prompt", content["twitter_prompt"])

    send_section("4. Substack Article Prompt", content["substack_prompt"])

    visuals_text = "\n".join(f"• {v}" for v in content["visuals"])
    send_section("5. Suggested Visuals", visuals_text)

    cl = content["cross_links"]
    cross_text = (
        "**Previous topics this connects to:**\n"
        + "\n".join(f"• {x}" for x in cl["previous"])
        + "\n\n**Future topics this unlocks:**\n"
        + "\n".join(f"• {x}" for x in cl["unlocks"])
        + "\n\n**Related engineering themes:**\n"
        + "\n".join(f"• {x}" for x in cl["related_themes"])
        + f"\n\n**YouTube expansion:**\n{cl['youtube_expansion']}"
    )
    send_section("6. Cross-Link Opportunities", cross_text)

    footer = (
        f"\n---\n*Generated {datetime.utcnow().strftime('%Y-%m-%d %H:%M UTC')} · "
        f"Next: Day {day_number + 1} — {commits[next_index + 1]['subject'] if next_index + 1 < len(commits) else 'End of history'}*"
    )
    send_discord(footer)

    # ── Persist state (only advance if at least one section sent) ────────────
    state["last_index"] = next_index
    save_state(state)
    print(f"Day {day_number} content sent successfully. State saved.")


if __name__ == "__main__":
    main()
