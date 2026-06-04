#!/usr/bin/env python3
"""
OmniBot Daily Content Generator
Analyzes git commits in ascending order, generates robotics engineering content,
and sends formatted output to Discord via webhook.
"""

import json
import subprocess
import sys
import textwrap
from datetime import datetime

import httpx

DISCORD_WEBHOOK = (
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
)

DISCORD_MAX_LENGTH = 2000


def get_commits_chronological():
    result = subprocess.run(
        ["git", "log", "--reverse", "--format=%H|%ad|%s", "--date=short"],
        capture_output=True, text=True, check=True
    )
    commits = []
    for line in result.stdout.strip().splitlines():
        if "|" in line:
            parts = line.split("|", 2)
            if len(parts) == 3:
                commits.append({"hash": parts[0], "date": parts[1], "subject": parts[2]})
    return commits


def get_commit_detail(commit_hash):
    result = subprocess.run(
        ["git", "show", commit_hash, "--format=%B", "--stat"],
        capture_output=True, text=True, check=True
    )
    return result.stdout


# ─── Day 1 content: hard-coded from first commit analysis ────────────────────

DAY1_CONTENT = {
    "topic": "Why ROS 2 Topic Namespacing Silently Breaks Full-Stack Robot Integration",

    "core_idea": textwrap.dedent("""\
        The first OmniBot commit wasn't a feature — it was a debugging session across four
        subsystems that all looked correct individually but were silently broken together.

        A single missing `/` prefix in the URDF JointStatePublisher declaration
        (`joint_states` vs `/joint_states`) meant ros_gz_bridge couldn't resolve the
        topic in Gazebo's global namespace. Separately, the Gazebo bridge had a full
        YAML configuration correctly specifying 10 topic mappings — including depth
        camera and point cloud — but the launch file was passing hard-coded inline args
        and never loading the YAML file. Both components worked in isolation. The
        bridge config was valid. The URDF was valid. Nothing logged an error. The robot
        just silently had no joint state data.

        On top of that: arm joint names in arm_params.yaml used bare names
        (`shoulder_pan`) while the URDF used prefixed names (`arm_shoulder_pan`), causing
        robot_state_publisher to silently drop the arm from the TF tree. And RViz had
        `fixed_frame: base_link` — so even if the robot moved, RViz rendered it stationary.

        Four independent silent failures. No single error message pointing to any of them.
        This is the real cost of systems integration in robotics: individually correct
        components that only reveal their incompatibilities when wired together end-to-end.

        Lesson: in a full-stack robot system (simulator → bridge → ROS graph → driver →
        Android app), name consistency is a contract. Violate it anywhere and you get no
        diagnostic — just a robot that doesn't move and a TF tree missing half its nodes.
    """),

    "twitter_prompt": textwrap.dedent("""\
        You are writing a Twitter/X thread as an experienced robotics systems engineer
        who builds full-stack robot systems with ROS 2, Gazebo, and Android control apps.

        Write a concise, high-signal thread (6–8 tweets) about the following engineering
        experience:

        Topic: Why ROS 2 topic namespacing silently breaks full-stack robot integration.

        Real scenario from OmniBot (a ROS 2 mecanum-wheel mobile manipulation robot):
        - A URDF used `joint_states` (relative) instead of `/joint_states` (absolute).
          ros_gz_bridge couldn't resolve the Gazebo topic. No error logged — just missing data.
        - The Gazebo bridge had a full YAML config with 10 topic mappings correctly written,
          but the launch file used inline args and never loaded the YAML. 10 bridges silently
          disabled. No warning.
        - Arm joint names in the driver config (`shoulder_pan`) didn't match the URDF
          (`arm_shoulder_pan`). robot_state_publisher silently dropped the arm from the TF tree.
        - RViz fixed_frame was set to `base_link` — so even when the robot moved,
          the visualization showed it stationary.

        Thread requirements:
        - Open with a strong hook about silent failures in robotics (not about ROS 2 specifically)
        - Explain the namespacing trap: relative vs absolute topic paths in ROS 2 contexts
        - Explain the YAML-vs-inline-args failure: config defined, never loaded
        - Explain the joint name contract: every layer (URDF, driver, YAML, app) must agree exactly
        - Include one controversial opinion: silent failures are a ROS 2 architectural problem,
          not a user error problem
        - End with a practical takeaway for robotics engineers building sim-to-hardware stacks
        - Mention OmniBot naturally once (not as a product pitch)
        - Avoid hype language, excessive emojis, engagement bait
        - Sound credible to engineers who have debugged ROS systems
        - Target: robotics developers, ROS 2 engineers, embodied AI researchers
    """),

    "substack_prompt": textwrap.dedent("""\
        You are writing a Substack article for an audience of experienced robotics engineers,
        ROS 2 developers, and embodied AI researchers. The author is building OmniBot —
        a ROS 2 Jazzy mecanum-wheel mobile manipulation robot with Gazebo simulation,
        a 6-DOF arm, and an Android control app via ROSBridge.

        Write a deep technical article on the following topic:

        Title: Why Full-Stack Robot Integration Fails Silently: A ROS 2 Namespacing Story

        Use this real debugging session as the case study:
        The first OmniBot commit was a fix session resolving four simultaneous silent failures:
        1. URDF topic: `joint_states` (relative) → `/joint_states` (absolute) for ros_gz_bridge
        2. Gazebo bridge YAML config correctly written with 10 mappings, but launch file using
           inline args — YAML never loaded, 10 bridges silently disabled
        3. Arm driver params: `shoulder_pan` vs URDF's `arm_shoulder_pan` — TF tree silently
           incomplete
        4. RViz fixed_frame: `base_link` instead of `odom` — robot visually stationary
           regardless of actual movement

        Article structure:
        1. Introduction — the full-stack robotics integration problem
        2. Problem Statement — why robotics has more silent failure modes than web systems
        3. Why Existing Approaches Break — how ROS 2's namespace design creates implicit contracts
           that aren't enforced
        4. OmniBot Case Study — walk through each of the four failures with the exact code context
        5. Engineering Tradeoffs — when does namespace flexibility help vs. hurt? How does
           ros_gz_bridge differ from ROS 2 native pub/sub in namespace handling?
        6. Lessons Learned — the "naming contract" principle: every layer must agree
        7. Future Directions — what would better tooling look like? ROS 2 graph validation,
           name-checking launch tools
        8. Final Takeaways — three rules robotics engineers should follow when wiring
           multi-subsystem robot stacks

        Requirements:
        - Go deep on ROS 2 namespace mechanics: relative vs absolute vs private topic names
        - Explain ros_gz_bridge namespace context specifically (why Gazebo topics live in a
          different namespace and why relative paths fail)
        - Cover robot_state_publisher's joint name matching requirement
        - Discuss why RViz fixed frame choice is a coordinate frame contract, not just a
          visualization preference
        - Include the actual diff/fix for each failure (code snippet style)
        - Discuss how this scales: in a 20-node robot system, how many implicit naming contracts
          exist and how do you audit them?
        - Avoid beginner-level ROS explanations — assume the reader knows what a topic is
        - Target length: 1800–2500 words
        - Tone: engineering diary meets systems design retrospective
    """),

    "visuals": [
        "ROS 2 topic graph (`ros2 topic list` / `rqt_graph`) showing missing joint_states connections",
        "Side-by-side diff: URDF `joint_states` vs `/joint_states`",
        "ros_gz_bridge YAML config file showing 10 mappings that were silently disabled",
        "RViz screenshot: robot frozen at origin (base_link fixed frame) vs moving (odom fixed frame)",
        "TF tree (`ros2 run tf2_tools view_frames`) with and without arm joints",
        "Terminal output: `ros2 topic echo /joint_states` showing empty / no arm joints",
        "Android app arm control UI screenshot (after fix)",
        "Simulation.launch.py diff: inline args → YAML config loading",
    ],

    "cross_links": {
        "connects_to": [
            "ROS 2 launch system architecture (how parameters and configs are actually loaded)",
            "Gazebo-ROS bridge internals (namespace bridging mechanics)",
            "URDF naming conventions and robot_state_publisher contracts",
        ],
        "unlocks": [
            "CI/CD for robotics: automated topic graph validation",
            "ROS 2 launch testing strategies",
            "Simulation-to-real gap: why sim wiring bugs don't surface until hardware",
            "Android ROSBridge integration: the full data pipeline from robot to mobile UI",
        ],
        "related_themes": [
            "Systems integration complexity in full-stack robotics",
            "Silent failure modes as an architectural anti-pattern",
            "Naming conventions as distributed system contracts",
        ],
        "youtube_expansion": (
            "Video: 'Debugging a Full-Stack Robot in 4 Silent Failures' — "
            "live walkthrough of rqt_graph, tf2 view_frames, ros2 topic echo "
            "to diagnose each failure before knowing what's broken"
        ),
    },
}


def split_for_discord(text, max_len=DISCORD_MAX_LENGTH):
    """Split text into Discord-safe chunks, respecting max_len."""
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


def send_to_discord(content: str, label: str = ""):
    """Send a message to Discord webhook, splitting if needed."""
    chunks = split_for_discord(content)
    for i, chunk in enumerate(chunks):
        suffix = f" *(part {i+1}/{len(chunks)})*" if len(chunks) > 1 else ""
        payload = {"content": chunk + suffix}
        try:
            resp = httpx.post(DISCORD_WEBHOOK, json=payload, timeout=15)
            resp.raise_for_status()
            print(f"  Sent {label} chunk {i+1}/{len(chunks)} ({len(chunk)} chars) — HTTP {resp.status_code}")
        except httpx.HTTPError as e:
            print(f"  ERROR sending {label}: {e}", file=sys.stderr)
            return False
    return True


def build_discord_messages(commits, day=1):
    c = commits[0]
    content = DAY1_CONTENT
    date_str = datetime.now().strftime("%Y-%m-%d")

    header = (
        f"# OmniBot Daily Content — Day {day} | {date_str}\n"
        f"**Commit:** `{c['hash'][:8]}` · {c['date']} · _{c['subject']}_\n"
        f"---\n"
    )

    topic_block = (
        f"## 1. Daily Topic\n"
        f"**{content['topic']}**\n"
    )

    core_block = (
        f"## 2. Core Idea\n"
        f"{content['core_idea']}"
    )

    twitter_block = (
        f"## 3. Twitter/X Post Prompt\n"
        f"{content['twitter_prompt'].strip()}"
    )

    substack_block = (
        f"## 4. Substack Article Prompt\n"
        f"{content['substack_prompt'].strip()}"
    )

    visuals_lines = "\n".join(f"- {v}" for v in content["visuals"])
    visuals_block = f"## 5. Suggested Visuals\n{visuals_lines}\n"

    cl = content["cross_links"]
    cross_block = (
        f"## 6. Cross-Link Opportunities\n"
        f"**Connects to:**\n" +
        "\n".join(f"- {x}" for x in cl["connects_to"]) +
        f"\n\n**Unlocks:**\n" +
        "\n".join(f"- {x}" for x in cl["unlocks"]) +
        f"\n\n**Related themes:**\n" +
        "\n".join(f"- {x}" for x in cl["related_themes"]) +
        f"\n\n**YouTube expansion:** {cl['youtube_expansion']}\n"
    )

    return [
        header + topic_block,
        core_block,
        twitter_block,
        substack_block,
        visuals_block + "\n" + cross_block,
    ]


def main():
    print("Fetching OmniBot git history...")
    commits = get_commits_chronological()
    print(f"Found {len(commits)} commits. Using commit 1: {commits[0]['hash'][:8]} — {commits[0]['subject']}")

    messages = build_discord_messages(commits, day=1)

    print(f"\nSending {len(messages)} message sections to Discord...")
    for i, msg in enumerate(messages, 1):
        label = ["Header+Topic", "Core Idea", "Twitter Prompt", "Substack Prompt", "Visuals+Cross-Links"][i - 1]
        ok = send_to_discord(msg, label=label)
        if not ok:
            print(f"Failed on section {i}. Aborting.")
            sys.exit(1)

    print("\nAll sections sent successfully.")


if __name__ == "__main__":
    main()
