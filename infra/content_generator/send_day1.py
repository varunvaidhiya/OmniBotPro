#!/usr/bin/env python3
"""Send Day 1 content to Discord — hardcoded, no API key required."""

import json
import time
import requests

DISCORD_WEBHOOK_URL = (
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
)
DISCORD_MAX_LENGTH = 1900


DAY1_CONTENT = {
    "day_number": 1,
    "commit_hash": "34a262c",
    "commit_date": "2026-03-17",
    "commit_subject": "fix(sim): wire joints, arm control, and Android app end-to-end",
    "daily_topic": "Why Your ROS 2 Simulation Breaks Silently When Joint Names Don't Match Across Every Layer",
    "core_idea": (
        "In ROS 2 + Gazebo, joint names are a shared contract between at least five independent "
        "systems: the URDF, the ros_gz_bridge YAML config, the arm driver node, robot_state_publisher, "
        "and any external client like an Android app. When one layer uses bare names (shoulder_pan) "
        "and another uses URDF-prefixed names (arm_shoulder_pan), the TF tree silently becomes "
        "inconsistent. The robot appears to exist in RViz but doesn't move — no error, no warning, "
        "just a stale transform.\n\n"
        "The root cause in OmniBot was a mismatch between the arm_driver_node's joint name defaults "
        "and the URDF-declared joint names. Gazebo was publishing wheel states, the arm driver was "
        "publishing arm states, and robot_state_publisher was trying to merge both into one coherent "
        "TF tree. Because the arm joint names didn't match the URDF, robot_state_publisher silently "
        "dropped the arm states. The bridge YAML had all ten mappings defined but was never loaded "
        "because simulation.launch.py was passing hard-coded inline bridge args instead of the YAML "
        "file — a configuration file that existed but was never activated.\n\n"
        "The lesson: in any distributed system where multiple components share a string-keyed "
        "interface (joint names, topic names, frame IDs), treat that contract as a typed API. "
        "Define constants in one place (constants.py, constants.kt, or a shared YAML) and import "
        "them everywhere. Mismatches in string-keyed interfaces are the hardest bugs to find because "
        "they produce no runtime error — the system just silently ignores data it can't match. "
        "Absolute topic paths (/joint_states not joint_states) are non-negotiable when crossing "
        "the Gazebo-to-ROS2 bridge boundary."
    ),
    "twitter_prompt": (
        "Write a 7-tweet Twitter/X thread for an experienced robotics engineer audience. "
        "No hype language. No emojis except sparingly. No engagement bait.\n\n"
        "Topic: ROS 2 + Gazebo simulation silently breaks when joint names don't match across layers.\n\n"
        "Tweet 1 (hook): Open with the specific failure mode — robot visible in RViz, simulation running, "
        "no errors, but nothing moves. Hook the reader with the detective story, not a generic statement.\n\n"
        "Tweet 2: Explain why this is a systems integration problem, not a software bug. "
        "The URDF, ros_gz_bridge YAML, arm driver, robot_state_publisher, and Android app are all "
        "independent systems that share one contract: joint name strings.\n\n"
        "Tweet 3: Explain relative vs absolute topic paths in ROS 2 + Gazebo. "
        "joint_states vs /joint_states across a gz bridge is not a typo — it's a namespace boundary. "
        "Include the OmniBot example naturally.\n\n"
        "Tweet 4: The configuration file trap. OmniBot had a ros_gz_bridge YAML with all mappings "
        "defined, but simulation.launch.py was passing inline args and never loading the file. "
        "Dead config is worse than no config — it creates false confidence.\n\n"
        "Tweet 5 (mildly controversial): Most robotics simulation bugs are not physics problems. "
        "They are string-matching problems. Your sim fails because a bridge config wasn't loaded, "
        "not because your dynamics model is wrong.\n\n"
        "Tweet 6: Practical fix — define joint names as typed constants in one file. "
        "Import them everywhere: URDF xacro parameters, Python driver defaults, Kotlin Android constants. "
        "String-keyed interfaces need the same discipline as typed APIs.\n\n"
        "Tweet 7 (takeaway): If your ROS 2 simulation runs without errors but doesn't move, "
        "check joint names before checking physics. Run `ros2 topic echo /joint_states` and "
        "compare against your URDF <joint name=\"...\"> declarations. The mismatch is always there."
    ),
    "substack_prompt": (
        "Write a 1800-word technical article for robotics engineers on the topic: "
        "'Why ROS 2 Simulation Joint State Naming Is a Full-Stack Contract, and How to Enforce It.'\n\n"
        "Structure:\n\n"
        "Introduction: Set the scene — you've launched the simulation, Gazebo is running, RViz shows "
        "the robot model, all ROS nodes are green, but the robot doesn't move when you send a command. "
        "No errors. The reader has been here before.\n\n"
        "Problem Statement: Explain precisely how robot_state_publisher merges multiple JointState "
        "publishers. Name matching is exact string comparison. One source can publish wheel states "
        "from Gazebo and another can publish arm states from a driver — but if the names don't match "
        "the URDF joint declarations, that source's data is silently ignored.\n\n"
        "Why Existing Approaches Break: Explain the three common failure modes: (1) relative vs "
        "absolute topic paths across ros_gz_bridge, (2) bare joint names vs URDF-prefixed joint "
        "names in driver parameter defaults, (3) bridge configuration files that are defined but "
        "never loaded (the dead config antipattern).\n\n"
        "OmniBot Case Study: Walk through the exact fix from commit 34a262c. Specifically: "
        "changing topic from 'joint_states' to '/joint_states' in the URDF, adding the "
        "gz.msgs.Model bridge entry for arm states, switching simulation.launch.py from inline args "
        "to the YAML config file, and renaming arm_driver joint_names from bare to arm_-prefixed. "
        "Show the ros_gz_bridge YAML structure. Explain why arm_driver_node needed to remap "
        "/arm/joint_states → /joint_states for robot_state_publisher to merge wheel and arm data.\n\n"
        "Engineering Tradeoffs: Discuss the tradeoff between flexibility (parameterized joint names) "
        "and reliability (constants defined once). Explain why the Android app's ARM_JOINT_NAMES "
        "list in Constants.kt must match arm_driver_node defaults — and how to prevent drift.\n\n"
        "Lessons Learned: Five actionable engineering rules: (1) always use absolute topic paths "
        "across any bridge boundary, (2) define joint names in one canonical location and import, "
        "(3) audit bridge YAML files are actually loaded in launch files, (4) test joint state "
        "publishing with `ros2 topic echo /joint_states` before debugging physics, (5) treat "
        "robot_state_publisher warnings as errors during development.\n\n"
        "Future Directions: How typed joint state interfaces and parameter validation at launch time "
        "could prevent this class of bug. Brief note on potential ROS 2 tooling improvements.\n\n"
        "Final Takeaways: Three bullet points. The reader should leave knowing exactly what to check "
        "the next time their simulation robot is visible but unresponsive."
    ),
    "suggested_visuals": [
        "RViz screenshot showing robot model frozen despite Gazebo running — the exact failure state",
        "Diagram of the data flow: Gazebo → ros_gz_bridge → /joint_states → robot_state_publisher → TF tree, with the name mismatch point highlighted",
        "Side-by-side terminal: `ros2 topic echo /joint_states` output vs URDF joint name declarations showing the mismatch",
        "ros_gz_bridge.yaml file snippet showing the bridge mapping structure",
        "Android app arm joint names constant vs arm_driver_node joint_names parameter — showing they must match",
        "RViz before/after: frozen robot model vs moving robot with correct joint states",
    ],
    "cross_links": {
        "connects_to": [
            "ROS 2 TF tree debugging fundamentals",
            "ros_gz_bridge configuration patterns",
            "robot_state_publisher internals",
        ],
        "unlocks": [
            "Why distributed ROS 2 systems need contract testing between nodes",
            "How Android ROSBridge clients must mirror ROS 2 type conventions",
            "URDF joint naming conventions at scale in multi-arm systems",
        ],
        "youtube_expansions": [
            "Live debugging: 'My simulation robot doesn't move' — walkthrough of diagnosing joint name mismatches in ROS 2 + Gazebo in real time",
            "OmniBot architecture deep-dive: how Gazebo, ros_gz_bridge, arm driver, and Android app share one joint name contract",
        ],
    },
}


def format_chunks(data: dict) -> list[str]:
    header = (
        f"## OmniBot Daily Engineering Content — Day {data['day_number']}\n"
        f"**Date:** {data['commit_date']}  |  **Commit:** `{data['commit_hash']}`\n"
        f"> {data['commit_subject']}\n"
        f"{'─' * 50}\n"
    )

    topic_block = (
        f"### Daily Topic\n"
        f"**{data['daily_topic']}**\n\n"
        f"### Core Idea\n"
        f"{data['core_idea']}\n"
    )

    twitter_block = (
        f"### Twitter/X Post Prompt\n"
        f"{data['twitter_prompt']}\n"
    )

    substack_block = (
        f"### Substack Article Prompt\n"
        f"{data['substack_prompt']}\n"
    )

    visuals = "\n".join(f"- {v}" for v in data["suggested_visuals"])
    visuals_block = f"### Suggested Visuals\n{visuals}\n"

    cross = data["cross_links"]
    connects = "\n".join(f"- {c}" for c in cross["connects_to"])
    unlocks = "\n".join(f"- {u}" for u in cross["unlocks"])
    youtube = "\n".join(f"- {y}" for y in cross["youtube_expansions"])
    cross_block = (
        f"### Cross-Link Opportunities\n"
        f"**Connects to:**\n{connects}\n\n"
        f"**Unlocks:**\n{unlocks}\n\n"
        f"**YouTube ideas:**\n{youtube}\n"
    )

    full = header + topic_block + twitter_block + substack_block + visuals_block + cross_block

    chunks = []
    current = ""
    for line in full.split("\n"):
        candidate = current + line + "\n"
        if len(candidate) > DISCORD_MAX_LENGTH:
            if current.strip():
                chunks.append(current.rstrip())
            current = line + "\n"
        else:
            current = candidate
    if current.strip():
        chunks.append(current.rstrip())

    return chunks


def send(chunks: list[str]) -> None:
    print(f"Sending {len(chunks)} Discord message(s)...")
    for i, chunk in enumerate(chunks, 1):
        print(f"  [{i}/{len(chunks)}] {len(chunk)} chars")
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
        if not resp.ok:
            print(f"  ERROR {resp.status_code}: {resp.text}")
            resp.raise_for_status()
        if i < len(chunks):
            time.sleep(1.2)
    print("Done.")


if __name__ == "__main__":
    chunks = format_chunks(DAY1_CONTENT)
    send(chunks)
