#!/usr/bin/env python3
"""
OmniBot Daily Content Bot
Generates daily robotics engineering content from git commit history
and sends it to Discord via webhook.
"""

import subprocess
import sys
import time
import urllib.request
import urllib.error
import json
from datetime import date

DISCORD_WEBHOOK_URL = (
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
)

# Discord message character limit
DISCORD_LIMIT = 2000


def send_discord_message(content: str) -> bool:
    """Send a single message to Discord webhook. Returns True on success."""
    payload = json.dumps({"content": content}).encode("utf-8")
    req = urllib.request.Request(
        DISCORD_WEBHOOK_URL,
        data=payload,
        headers={
            "Content-Type": "application/json",
            "User-Agent": "OmniBotContentBot/1.0",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            return resp.status in (200, 204)
    except urllib.error.HTTPError as e:
        print(f"HTTP error {e.code}: {e.reason}", file=sys.stderr)
        return False
    except urllib.error.URLError as e:
        print(f"URL error: {e.reason}", file=sys.stderr)
        return False


def send_chunked(text: str, delay: float = 1.0) -> None:
    """Split text into Discord-safe chunks and send each with a delay."""
    # Split on double newlines to avoid breaking mid-paragraph
    lines = text.split("\n")
    chunks = []
    current = ""

    for line in lines:
        candidate = current + line + "\n"
        if len(candidate) > DISCORD_LIMIT:
            if current.strip():
                chunks.append(current.rstrip())
            current = line + "\n"
        else:
            current = candidate

    if current.strip():
        chunks.append(current.rstrip())

    for i, chunk in enumerate(chunks):
        success = send_discord_message(chunk)
        status = "✓" if success else "✗"
        print(f"  [{status}] Chunk {i+1}/{len(chunks)} ({len(chunk)} chars)")
        if i < len(chunks) - 1:
            time.sleep(delay)


def get_first_commits(n: int = 5) -> str:
    """Return the first N commits in chronological order."""
    result = subprocess.run(
        ["git", "log", "--reverse", "--format=%H %ad %s", "--date=short"],
        capture_output=True,
        text=True,
        cwd="/home/user/OmniBot",
    )
    lines = result.stdout.strip().split("\n")
    return "\n".join(lines[:n])


def build_day1_content() -> str:
    """Build the Day 1 content based on the first commit."""
    today = date.today().isoformat()

    content = f"""# OmniBot Daily Engineering Content — {today}
> **Day 1 of the series | Starting from commit 1 of 135**
> Commit: `34a262c` — 2026-03-17 — *fix(sim): wire joints, arm control, and Android app end-to-end*
> 339 files, 25 552 insertions — the founding commit of OmniBot

---

## 1. Daily Topic

**Why Every Real Robotics Project Starts With a Protocol Debugger, Not a Framework**

---

## 2. Core Idea

OmniBot's first commit is 339 files and 25,552 lines. But buried inside it — alongside the full ROS 2 workspace, Android app, VLA engine, and data pipeline — is a collection of root-level scripts: `fuzz_motor.py`, `scan_protocols.py`, `sniff_serial.py`, `analyze_packets.py`, `fuzz_advanced.py`, `scan_feedback.py`. These aren't features. They are the archaeology that made everything else possible.

The Yahboom ROS Robot Expansion Board ships with no open protocol specification. Before a single ROS 2 node could publish odometry or drive a wheel, the binary serial protocol had to be reverse-engineered from raw USB captures. The result is a non-obvious checksum formula: `(sum(all_packet_bytes) + 5) & 0xFF` — where `5 = 257 − 0xFC`, a mathematical complement to the device ID header byte. That one constant took hours of systematic fuzzing to confirm.

The engineering lesson: in physical robotics, **the hardware protocol layer is the load-bearing wall**. Framework choice, architecture, and AI integration are all secondary. If the motor driver is sending subtly wrong checksums, the robot will spin unpredictably — and no amount of Nav2 tuning will fix it. OmniBot's entire stack — Nav2, SmolVLA, RL inference, the Android app — runs on top of `confirmed_protocol.py`. Getting that file right before writing any other file was not optional.

Practical mistake to avoid: trusting vendor documentation over empirical verification. The Yahboom board's motion command uses `int16` velocities scaled by `1000` (i.e., `vx × 1000` packed as a signed short). This is not documented anywhere. It was found by sending known velocity commands, sniffing the expected motor response, and working backwards through the packet structure. The scaffolding for that verification lives in `fuzz_motor.py` and `test_motor_direct.py` — 15 scripts total at the repo root.

---

## 3. Twitter/X Post Prompt

**Instructions for the writing model:**

Write a high-signal Twitter thread (5–7 tweets) from the perspective of a robotics systems engineer who just spent their first week on a new mobile manipulation robot project. The engineer discovered the hardware vendor ships no protocol documentation. The thread should:

- Open with a hook that subverts the usual "here's my new robot project" announcement format
- Explain concisely why protocol reverse engineering is the actual Day 0 task in physical robotics
- Reference a real protocol quirk: checksum = (sum(all bytes) + 5) & 0xFF where 5 = 257 − device_id. Explain why this is the kind of thing you can only find empirically.
- Mention that OmniBot (ROS 2 mecanum robot) has 15 root-level fuzzing/scanning scripts that predated any ROS node
- Contrast this reality with how robotics projects are usually presented online (framework selection, architecture diagrams, CI pipelines — all secondary to getting the motor to spin correctly)
- End with one practical takeaway: always write a standalone hardware protocol verifier before you write a single line of middleware
- Optionally include one controversial opinion: "Most robotics project failures I've seen trace back to hardware assumptions that were never empirically verified"
- Tone: direct, credible, experienced. No emojis beyond one optional at the end. No engagement bait. Assume the reader has written a ROS node before.

---

## 4. Substack Article Prompt

**Instructions for the writing model:**

Write a long-form technical article (1200–1800 words) titled:

**"Why Every Real Robotics Project Starts With a Protocol Debugger, Not a Framework"**

Use the OmniBot project as the central case study. The article should follow this structure:

**Introduction**
Open with the observation that robotics project write-ups almost always start at the framework layer — ROS 2, Nav2, Isaac Sim. But behind every functioning robot is a hardware protocol layer that most posts skip entirely. OmniBot's founding commit contains 15 serial protocol debugging scripts. That's the real story of Day 0.

**Problem Statement**
Explain what happens when hardware vendors ship expansion boards with closed protocols. Reference the Yahboom ROS Robot Expansion Board. Describe what "no documentation" means in practice: you have a USB serial port, a board that accepts bytes, and a motor that should spin. Nothing else.

**Why Existing Approaches Break**
Walk through the naïve approaches: guessing packet formats from similar boards, using vendor SDK source code (if it exists), relying on community forum posts. Explain why each fails in a safety-critical or deployment context. The core problem: protocol assumptions that are unverified will fail in edge cases — wrong velocity scaling, silent byte-order errors, checksum acceptance of malformed packets.

**OmniBot Case Study**
Detail the reverse engineering process:
- Sniffing raw serial output (`sniff_serial.py`)
- Fuzzing function codes and payload lengths (`fuzz_all_cmds.py`, `fuzz_advanced.py`)
- Confirming the packet structure: `[0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]`
- Cracking the checksum: `(sum(all_bytes) + 5) & 0xFF` where `5 = 257 − 0xFC`
- Confirming motion command payload: `struct.pack('<bhhh', CAR_TYPE, vx×1000, vy×1000, w×1000)`
- The result: `confirmed_protocol.py` → promoted to `packages/yahboom_ros2/protocol.py`
- Only after this: the ROS 2 `yahboom_controller_node.py` was written on top of a verified foundation

**Engineering Tradeoffs**
Time cost: 15 scripts, multiple hours of empirical testing. Alternative: trust community posts and ship something that works 95% of the time. Why 95% is unacceptable for a mobile manipulation robot: the 5% failure rate manifests as erratic base motion that corrupts arm positioning, which corrupts dataset collection, which corrupts the VLA policy. The error propagates upward through every layer.

**Lessons Learned**
1. Write a standalone protocol verifier before any middleware. No ROS dependency — just a Python script that opens the serial port and sends packets.
2. Confirm bidirectional: verify TX (commands to board) and RX (encoder/IMU feedback) separately.
3. Store the verified protocol as a library (`yahboom_ros2.protocol`), not inline in the driver node. This enables testing without hardware.
4. The 15 root-level scripts are not cleanup debt — they are the project's foundation layer and should be version-controlled with that framing.

**Future Directions**
How this pattern scales: as OmniBot grows to include the SO-101 arm (Feetech STS3215 bus protocol), the same empirical verification process applies. Hardware boundaries multiply; each one needs its own protocol verifier before being wrapped in a ROS node.

**Final Takeaways**
The framework layer (ROS 2, Nav2, SmolVLA, LangGraph) is highly documented, well-tested by the community, and relatively predictable. The hardware boundary layer is none of those things. Robotics engineers who treat hardware integration as a solved problem will rediscover this the hard way, usually during a live demo.

---

## 5. Suggested Visuals

- Terminal output of `sniff_serial.py` showing raw hex bytes from the Yahboom board
- Side-by-side: raw packet bytes annotated with field labels (header, device_id, length, func, payload, checksum)
- Python snippet from `yahboom_ros2/protocol.py` showing the `_checksum()` function with the `257 - 0xFC = 5` constant explained in a comment
- Screenshot of the repo root listing the 15 fuzz/scan/test scripts (file browser or `ls` output)
- RViz screenshot of the robot after the driver was confirmed working (robot mesh visible, TF tree live)
- Oscilloscope or logic analyzer capture of the serial line (if available from original hardware debug session)
- Graph: packet success rate vs. checksum formula variants tested during reverse engineering (reconstructed from notes)

---

## 6. Cross-Link Opportunities

**Connects to (previous topics this unlocks):**
- Hardware abstraction layers in robotics — how `yahboom_ros2.protocol` is the abstraction that protects all ROS nodes from the raw serial layer
- Why mecanum kinematics must be verified against real wheel behavior, not just math

**Unlocks (future topics):**
- Day 2: The TF tree problem — why `base_link` vs `odom` as Fixed Frame in RViz broke everything
- Day 3: Joint name namespacing — how `arm_shoulder_pan` vs `shoulder_pan` silently broke the arm state machine
- The Feetech STS3215 servo protocol for the SO-101 arm — a second protocol reverse engineering story
- Why the RX encoder packet format (`<ENCODERS,fl,fr,rl,rr>`) required a separate parser

**Related engineering themes:**
- Hardware-software contracts in embedded systems
- Protocol fuzzing as a systems engineering discipline
- Why robotics monorepos should version-control hardware verification scripts alongside production code

**YouTube expansion:**
- "I Spent a Week Reverse Engineering a Robot Controller Before Writing Any ROS Code" — walkthrough of the fuzzing process with live terminal demos, packet captures, and the moment the wheel finally spun correctly
"""
    return content


def main():
    print("OmniBot Daily Content Bot — Day 1")
    print("=" * 50)

    print("\n[1/3] Fetching first commits from git history...")
    first_commits = get_first_commits(3)
    print(first_commits)

    print("\n[2/3] Building Day 1 content...")
    content = build_day1_content()
    print(f"  Content length: {len(content)} chars")

    print("\n[3/3] Sending to Discord...")
    send_chunked(content, delay=1.2)

    print("\nDone.")


if __name__ == "__main__":
    main()
