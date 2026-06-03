#!/usr/bin/env python3
"""
OmniBot Daily Content Generator
Generates robotics engineering content from git history and posts to Discord.
"""

import subprocess
import json
import urllib.request
import urllib.error
import time
import sys

WEBHOOK_URL = "https://discord.com/api/webhooks/1507739979990962198/koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
DISCORD_CHAR_LIMIT = 1990


def get_commits():
    result = subprocess.run(
        ["git", "log", "--reverse", "--format=%H|%s|%ad", "--date=short"],
        capture_output=True, text=True
    )
    commits = []
    for line in result.stdout.strip().split("\n"):
        if "|" in line:
            parts = line.split("|", 2)
            if len(parts) == 3:
                commits.append({"hash": parts[0], "message": parts[1], "date": parts[2]})
    return commits


def send_to_discord(content: str):
    """Send a single message to Discord, handling the 2000 char limit."""
    chunks = []
    while len(content) > DISCORD_CHAR_LIMIT:
        split_at = content.rfind("\n", 0, DISCORD_CHAR_LIMIT)
        if split_at == -1:
            split_at = DISCORD_CHAR_LIMIT
        chunks.append(content[:split_at])
        content = content[split_at:].lstrip("\n")
    chunks.append(content)

    for i, chunk in enumerate(chunks):
        payload = json.dumps({"content": chunk}).encode("utf-8")
        req = urllib.request.Request(
            WEBHOOK_URL,
            data=payload,
            headers={
                "Content-Type": "application/json",
                "User-Agent": "OmniBot-ContentBot/1.0",
            },
            method="POST"
        )
        retries = 0
        delay = 2
        while retries <= 4:
            try:
                with urllib.request.urlopen(req) as resp:
                    if resp.status == 204:
                        print(f"  Chunk {i+1}/{len(chunks)} sent.")
                        break
            except urllib.error.HTTPError as e:
                if e.code == 429:
                    retry_after = float(e.headers.get("Retry-After", delay))
                    print(f"  Rate limited. Retrying in {retry_after}s...")
                    time.sleep(retry_after)
                    retries += 1
                    continue
                else:
                    print(f"  HTTP error {e.code}: {e.read()}")
                    break
            except Exception as ex:
                print(f"  Error: {ex}. Retrying in {delay}s...")
                time.sleep(delay)
                delay *= 2
                retries += 1
        time.sleep(0.8)  # Respect Discord rate limits between chunks


# ─────────────────────────────────────────────────────────────────────────────
# DAY 1 CONTENT
# Based on first commit: fix(sim): wire joints, arm control, and Android app end-to-end
# Date: 2026-03-17
# ─────────────────────────────────────────────────────────────────────────────

DAILY_TOPIC = "Why ROS 2 Simulation Setups Silently Fail at Integration"

CORE_IDEA = """\
The first push of OmniBot's simulation stack had four separate failures — none of which caused \
a crash or error message. The robot rendered in RViz. Gazebo started. Nodes launched. \
And yet nothing actually worked.

The failures:
1. `joint_states` (relative) vs `/joint_states` (absolute) — the Gazebo bridge couldn't see \
the topic in the global namespace, so wheel angles never reached robot_state_publisher. \
Everything published. Nothing connected.
2. A 10-mapping bridge YAML config was written, loaded into a variable, and then completely \
ignored — the launch file passed hard-coded inline args to ros_gz_bridge instead. Result: \
depth camera, point cloud, and 8 other bridges were silently inactive.
3. RViz Fixed Frame set to `base_link` instead of `odom`. The robot drove in place. \
Visually, nothing moved. No error.
4. Arm joint names published as `shoulder_pan` but URDF expected `arm_shoulder_pan`. \
Silent TF tree breakage — arm transforms simply didn't exist.

The real lesson: in robotics integration, the system does not tell you when string mismatches \
break it. You have to know where to look — ros2 topic echo, tf2_tools view_frames, \
ros2 node info. The robot looks alive and is actually disconnected.
"""

TWITTER_PROMPT = """\
Write a concise, high-signal Twitter thread (5-7 tweets) for an audience of experienced \
robotics engineers and ROS 2 developers. The author is building OmniBot — a ROS 2 Jazzy \
mecanum wheel + SO-101 arm robot.

Topic: The silent integration failures that happened on the first day of simulation bringup.

Thread requirements:
- Hook tweet: the paradox of a robot that LOOKS alive but is completely disconnected internally
- Tweet 2: Explain the topic namespace trap — relative `joint_states` vs absolute `/joint_states` \
in Gazebo bridge context. In ROS 2, namespacing rules between the Gz namespace and ROS namespace \
create invisible mismatches.
- Tweet 3: The bridge YAML defined but never activated — a config file that existed, loaded \
into a variable, but was never passed to ros_gz_bridge. Hard-coded inline args overrode it. \
10 bridge mappings silently inactive.
- Tweet 4: RViz Fixed Frame = base_link (robot never visually moves), joint names without \
URDF prefix (arm disappears from TF tree). Both fail silently.
- Tweet 5: Debugging method — which ROS 2 CLI commands actually expose these failures \
(ros2 topic list, ros2 topic echo, ros2 run tf2_tools view_frames, ros2 node info)
- Tweet 6: The core systems principle — robotics integration failures are not exceptions, \
they are silent misconfigurations. The system keeps running. You have to instrument it.
- Final tweet: practical takeaway for engineers building sim stacks from scratch.

Tone: experienced systems engineer, not a teacher. Direct. No hype. No emojis except \
sparingly for structure. Sound like someone who debugged this at 11pm.
Mention OmniBot naturally once or twice. No engagement bait.
"""

SUBSTACK_PROMPT = """\
Write a long-form Substack article (1500-2500 words) for robotics engineers and ROS 2 \
developers. Assume readers understand ROS 2 basics.

Title: "Why Your ROS 2 Simulation Bringup Is Probably Broken (And Not Telling You)"

Article structure:

**Introduction**
Start with the experience: you launch the simulation, Gazebo opens, RViz shows the robot, \
nodes are running — and the robot is completely non-functional. Not crashed. Not erroring. \
Just silently disconnected. This is the first-day OmniBot sim bringup experience.

**Problem Statement**
ROS 2 integration failures in simulation are categorical. They fall into:
- Topic namespace mismatches (relative vs absolute)
- Bridge configuration files loaded but not activated
- Coordinate frame misconfigurations (wrong Fixed Frame in RViz)
- Node parameter mismatches (joint names not matching URDF)
None produce errors. All produce silence.

**Why Existing Approaches Break**
Explain WHY ros_gz_bridge namespace resolution works differently than pure ROS 2 topics. \
The Gz internal namespace is a separate message bus — bridging requires exact absolute paths. \
Relative topic names in URDF/xacro plugin config behave differently than in ROS 2 launch files. \
Explain how YAML bridge configs in ros_gz_bridge must be explicitly passed via `config_file` \
arg — a YAML file being loaded into a Python variable in a launch file does nothing unless \
it is actually passed to the node.

**OmniBot Case Study**
Walk through each of the four failures found in the first simulation commit:
1. `joint_states` → `/joint_states` in URDF joint_state_publisher plugin
2. bridge YAML defined, variable created, but hard-coded inline args passed instead — \
   all 10 bridge entries inactive
3. RViz Fixed Frame base_link → odom — why this matters for odometry visualization
4. Arm joint names: `shoulder_pan` vs `arm_shoulder_pan` — TF tree gap, \
   no transform for the arm, no error thrown

**Engineering Tradeoffs**
Discuss the tradeoff between explicit configuration vs convention-based defaults. \
ROS 2 allows a lot of implicit behavior (relative topic names, default namespacing) \
that works in purely ROS environments but breaks at system boundaries like the \
Gazebo bridge. The more systems you integrate (ROS 2 + Gz + RViz + Android app), \
the more these boundary failures compound.

**Debugging Approach**
Concrete debugging workflow:
- `ros2 topic list` — are the expected topics present at all?
- `ros2 topic hz /joint_states` — is data flowing?
- `ros2 run tf2_tools view_frames` — is the TF tree complete?
- `ros2 node info <node>` — what topics is it actually subscribed to?
- `ros2 topic echo /joint_states --once` — is the data what you expect?
Explain why each of these is necessary for different failure categories.

**Lessons Learned**
- In multi-system robotics integration, always verify at each boundary, not just at launch
- Config files must be explicitly activated — loading a variable is not using it
- Absolute topic paths at Gz↔ROS boundaries are required, not optional
- RViz visualization configuration directly affects your debugging ability
- Joint name strings are the glue between URDF, driver nodes, and TF — any mismatch breaks silently

**Future Directions**
Brief note on how OmniBot's CLAUDE.md enforces joint name consistency as a documented \
constraint — one source of truth for joint names used by arm driver, SmolVLA node, \
and Android app. Suggest integration test patterns for catching these failures in CI.

**Final Takeaways**
3-4 concrete, actionable points for engineers setting up their own ROS 2 + Gazebo stacks.

Tone: technical, practical, experience-driven. First person where appropriate. \
No fluff. No beginner hand-holding. Treat the reader as a capable engineer.
"""

SUGGESTED_VISUALS = """\
**Suggested Visuals:**
- `ros2 run tf2_tools view_frames` output PDF showing incomplete TF tree (missing arm transforms)
- Terminal split: `ros2 topic list` with and without the `/joint_states` fix side by side
- RViz screenshot: robot frozen at origin (wrong Fixed Frame) vs robot moving in world (correct)
- Gazebo + RViz side-by-side showing the arm visually disconnected from the robot body
- ros_gz_bridge.yaml config file snippet with annotation showing the variable vs the actual arg
- `ros2 topic echo /joint_states` showing joint names with/without `arm_` prefix
- Architecture diagram: Gz message bus → bridge boundary → ROS 2 topic graph, \
  showing where namespace resolution happens
"""

CROSS_LINKS = """\
**Cross-Link Opportunities:**
- Connects forward to: Android ROSBridge integration (joint name consistency end-to-end)
- Connects forward to: SmolVLA joint state subscriptions (same naming constraint)
- Connects forward to: Debugging the TF tree in real hardware deployment
- Unlocks future topic: "Why ROS 2 Topic Graphs Need Observability Tools"
- Unlocks future topic: "How CI Can Catch ROS 2 Integration Failures Before Hardware"
- YouTube expansion: Live debug walkthrough — launching sim, finding failures with CLI tools, \
  fixing each one, ending with a working sim stack
"""


def build_messages():
    """Build Discord message sections."""
    header = (
        "# OmniBot Daily Engineering Content\n"
        f"**Day 1 | 2026-03-17 | Commit: `34a262c`**\n"
        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n"
    )

    topic_section = (
        "## 1. Daily Topic\n"
        f"**{DAILY_TOPIC}**\n"
    )

    core_section = (
        "## 2. Core Idea\n"
        f"{CORE_IDEA}"
    )

    twitter_section = (
        "## 3. Twitter/X Thread Prompt\n"
        f"{TWITTER_PROMPT}"
    )

    substack_section = (
        "## 4. Substack Article Prompt\n"
        f"{SUBSTACK_PROMPT}"
    )

    visuals_section = SUGGESTED_VISUALS

    crosslinks_section = CROSS_LINKS

    return [
        header + topic_section + core_section,
        twitter_section,
        substack_section,
        visuals_section + "\n" + crosslinks_section,
    ]


def main():
    commits = get_commits()
    print(f"Total commits found: {len(commits)}")
    print(f"First commit: {commits[0]['date']} — {commits[0]['message']}")
    print(f"\nGenerating Day 1 content...")
    print(f"Topic: {DAILY_TOPIC}\n")

    messages = build_messages()
    print(f"Sending {len(messages)} message(s) to Discord...")

    for i, msg in enumerate(messages):
        print(f"\n[Section {i+1}] Sending ({len(msg)} chars)...")
        send_to_discord(msg)

    print("\nDone. All content sent to Discord.")


if __name__ == "__main__":
    main()
