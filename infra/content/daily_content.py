#!/usr/bin/env python3
"""
OmniBot Daily Content Generation Script.

Walks through git commits in chronological order (one per run) and generates
robotics engineering content for Twitter/X, Substack, and YouTube, then
posts to a Discord webhook.

State is persisted in infra/content/state.json so each run advances to the
next commit automatically.
"""

import json
import os
import subprocess
import sys
import textwrap
import time
from datetime import datetime
from pathlib import Path
import urllib.request
import urllib.error

# ── Configuration ────────────────────────────────────────────────────────────

DISCORD_WEBHOOK_URL = (
    "https://discord.com/api/webhooks/1507739979990962198/"
    "koQiBhjqmu6AymM7_2kS43o42L8hmMMz6GmhYKkM5l-5W7FlhqTfUuOx4luXaIKl5FBs"
)

REPO_ROOT = Path(__file__).resolve().parents[2]
STATE_FILE = Path(__file__).parent / "state.json"

DISCORD_MAX_CHARS = 2000  # hard Discord limit per message

# ── Git helpers ───────────────────────────────────────────────────────────────


def get_commits_ascending() -> list[dict]:
    """Return all commits oldest→newest as list of dicts."""
    result = subprocess.run(
        ["git", "log", "--reverse", "--format=%H|%ai|%s"],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )
    commits = []
    for line in result.stdout.strip().splitlines():
        parts = line.split("|", 2)
        if len(parts) == 3:
            commits.append({"hash": parts[0], "date": parts[1], "message": parts[2]})
    return commits


def get_commit_diff_stat(commit_hash: str) -> str:
    """Return --stat summary for a commit."""
    result = subprocess.run(
        ["git", "show", "--stat", "--format=", commit_hash],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
    )
    lines = result.stdout.strip().splitlines()
    # Last summary line e.g. "42 files changed, 1234 insertions(+), 56 deletions(-)"
    return lines[-1] if lines else ""


def get_commit_files(commit_hash: str) -> list[str]:
    """Return list of files changed in a commit."""
    result = subprocess.run(
        ["git", "show", "--name-only", "--format=", commit_hash],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
    )
    return [l for l in result.stdout.strip().splitlines() if l.strip()]


# ── State management ──────────────────────────────────────────────────────────


def load_state() -> dict:
    if STATE_FILE.exists():
        with open(STATE_FILE) as f:
            return json.load(f)
    return {"last_commit_index": -1}


def save_state(state: dict) -> None:
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(STATE_FILE, "w") as f:
        json.dump(state, f, indent=2)


# ── Content generation ────────────────────────────────────────────────────────

# Each entry: (keywords_in_files_or_message, topic_fn)
# topic_fn(commit) → dict with keys: topic, core_idea, twitter_prompt,
#                    substack_prompt, visuals, cross_links

def generate_content_for_commit(commit: dict, commit_index: int) -> dict:
    """
    Map a commit to a day's content.

    For the initial large commit (index 0) the story is the Yahboom serial
    protocol reverse-engineering and the 20 Hz control loop decision.
    Subsequent commits follow ascending commit order.
    """
    msg = commit["message"].lower()
    files = get_commit_files(commit["hash"])
    files_str = " ".join(files).lower()
    stat = get_commit_diff_stat(commit["hash"])
    commit_date = commit["date"][:10]

    # ── Day 0: First commit — protocol reverse-engineering + 20 Hz loop ──────
    if commit_index == 0:
        return _content_day0_protocol(commit, stat)

    # ── Day 1: ROS_DOMAIN_ID / DDS fix ───────────────────────────────────────
    if "ros_domain_id" in msg or "dds" in msg or "domain" in msg:
        return _content_dds(commit, stat)

    # ── README / documentation day ────────────────────────────────────────────
    if msg.startswith("docs:") and "readme" in msg:
        return _content_readme(commit, stat)

    # ── IMU / sensor integration ──────────────────────────────────────────────
    if "imu" in msg or "sensor" in msg:
        return _content_imu_sensors(commit, stat)

    # ── Multi-camera / BEV ────────────────────────────────────────────────────
    if "camera" in msg or "bev" in msg or "bird" in msg:
        return _content_bev(commit, stat)

    # ── Standalone packages ───────────────────────────────────────────────────
    if "packages" in msg or "standalone" in msg:
        return _content_packages(commit, stat)

    # ── 3-D mapping / SLAM ───────────────────────────────────────────────────
    if "mapping" in msg or "rtab" in msg or "slam" in msg or "octomap" in msg:
        return _content_slam(commit, stat)

    # ── Hybrid Nav2 + VLA control ─────────────────────────────────────────────
    if "hybrid" in msg or ("nav" in msg and "vla" in msg):
        return _content_hybrid(commit, stat)

    # ── URDF / robot description ──────────────────────────────────────────────
    if "urdf" in msg or "urdf" in files_str or "xacro" in files_str:
        return _content_urdf(commit, stat)

    # ── Android app ───────────────────────────────────────────────────────────
    if "android" in msg or ".kt" in files_str:
        return _content_android(commit, stat)

    # ── RL / Isaac Lab ────────────────────────────────────────────────────────
    if "rl" in msg.split() or "isaac" in msg or "onnx" in msg:
        return _content_rl(commit, stat)

    # ── Observability / Prometheus ────────────────────────────────────────────
    if "observ" in msg or "prometheus" in msg or "grafana" in msg or "wandb" in msg:
        return _content_observability(commit, stat)

    # ── CI/CD ─────────────────────────────────────────────────────────────────
    if "ci" in msg or "github action" in msg or "workflow" in msg:
        return _content_ci(commit, stat)

    # ── OTA updates ───────────────────────────────────────────────────────────
    if "ota" in msg or "over-the-air" in msg:
        return _content_ota(commit, stat)

    # ── Generic fallback ──────────────────────────────────────────────────────
    return _content_generic(commit, stat, commit_index)


# ── Individual content generators ─────────────────────────────────────────────


def _content_day0_protocol(commit: dict, stat: str) -> dict:
    return {
        "topic": "Reverse-Engineering an Undocumented Motor Controller Protocol in ROS 2",
        "core_idea": textwrap.dedent("""\
            Before writing a single line of ROS 2 driver code for OmniBot's Yahboom mecanum
            base, the protocol had to be reverse-engineered from scratch. The vendor's
            documentation described the packet format at a high level, but the checksum
            formula was buried and wrong in the datasheet. The actual formula turned out to
            be `(sum(all_packet_bytes) + 5) & 0xFF`, where the magic "5" is `257 - 0xFC`
            — the arithmetic complement of the device ID byte. This kind of undocumented
            quirk is extremely common in low-cost robotics hardware and has real consequences:
            it means your first hours of integration are spent on packet-level debugging
            rather than robotics. OmniBot exposed this immediately because the very first
            controller commit was about getting a correct MOTION packet to the board, not
            about kinematics or Nav2. The 20 Hz control loop rate — with a mandatory 2 ms
            inter-packet delay — also had to be discovered empirically. The lesson: always
            budget protocol reverse-engineering time before any software milestone."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a Twitter/X thread (5–7 tweets) from the perspective of an experienced
            robotics engineer who has just finished reverse-engineering the Yahboom Rosmaster
            serial protocol to build a ROS 2 mecanum drive controller.

            Context:
            - The Yahboom board uses a custom binary serial protocol at 115200 baud.
            - Packet format: [0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]
            - The checksum is (sum(all_bytes_in_packet) + 5) & 0xFF, where 5 = 257 - 0xFC.
              The datasheet did NOT document this correctly.
            - CAR_TYPE must be sent 5× on startup or the board ignores motion commands.
            - A 2 ms delay is required between every packet or the board drops them.
            - The 20 Hz control loop rate was determined empirically, not from docs.
            - This is for OmniBot, a ROS 2 Jazzy mecanum-wheel mobile manipulation robot.

            Thread requirements:
            - Hook tweet: a specific, surprising fact about the checksum formula
            - Explain WHY undocumented protocols cost robotics engineers real time
            - Mention the startup handshake (CAR_TYPE x5) as a concrete example of
              undocumented requirements
            - Include one controversial take: "cheap hardware has hidden integration costs
              that outweigh the BOM savings"
            - End with a practical rule: always capture a protocol sniffer trace before
              trusting vendor docs
            - No emoji spam, no hype language, no beginner-level explanations
            - Sound credible to engineers who have debugged UART/SPI/I2C peripherals"""),
        "substack_prompt": textwrap.dedent("""\
            Write a long-form Substack article titled:
            "The Hidden Cost of Cheap Robotics Hardware: Reverse-Engineering the Yahboom
            Serial Protocol"

            Audience: ROS 2 engineers, embedded robotics developers, people building
            real hardware robots.

            Article structure:
            1. Introduction — The false economy of undocumented hardware
            2. Problem Statement — What happens when a vendor's datasheet is wrong
            3. Why Existing Approaches Break — Trusting vendor docs, using example code
               that hides the protocol, skipping oscilloscope/sniffer validation
            4. OmniBot Case Study — Walk through the exact Yahboom packet format:
               [0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]
               where CHECKSUM = (sum(all_bytes) + 5) & 0xFF
               and 5 = 257 - 0xFC (the device ID complement).
               Explain the 5× CAR_TYPE startup init and the mandatory 2 ms inter-packet
               delay. Show a real packet hex dump for a MOTION command (FUNC=0x12).
            5. Engineering Tradeoffs — STM32 (full control, no docs needed) vs Yahboom
               (integrated, but opaque). When to choose each.
            6. Lessons Learned — How to structure protocol reverse-engineering:
               capture → decode → validate → unit test → integrate
            7. Future Directions — Hardware abstraction layers, protocol unit tests,
               why `confirmed_protocol.py` became OmniBot's serial source of truth
            8. Final Takeaways — 3 actionable rules for any engineer integrating
               undocumented hardware

            Style: technical, honest about wasted time, no hype, practical examples."""),
        "visuals": [
            "Terminal output of a captured hex dump from the Yahboom board (real or annotated)",
            "Annotated packet diagram: byte positions for HEAD, DEVICE_ID, LEN, FUNC, PAYLOAD, CHECKSUM",
            "Python snippet from confirmed_protocol.py showing the checksum formula",
            "Serial monitor screenshot showing CAR_TYPE init packets",
            "Before/after: STM32 wiring diagram vs Yahboom USB single-cable connection",
            "RViz showing `/odom` topic update rate at 20 Hz after successful integration",
        ],
        "cross_links": textwrap.dedent("""\
            Previous: (Day 0 — project kickoff)
            Future: DDS networking issues (cross-machine ROS_DOMAIN_ID), hardware abstraction
            layers, why robotics CI/CD needs hardware-in-loop tests
            YouTube: "Debugging a ROS 2 Serial Driver from Scratch" — live terminal walkthrough"""),
    }


def _content_dds(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why ROS 2 DDS Discovery Silently Fails Across Subnets (And the ROS_DOMAIN_ID Fix)",
        "core_idea": textwrap.dedent("""\
            OmniBot is a two-machine system: a Raspberry Pi 5 running drivers and nav,
            and a GPU desktop running VLA inference. Getting them to discover each other
            via ROS 2 DDS required fixing `ROS_DOMAIN_ID` to 30 on both machines and
            setting `ROS_STATIC_PEERS`. The failure mode was silent — nodes started fine
            locally but topics simply didn't appear on the remote machine. This is one
            of the most common "works on my machine" bugs in multi-host ROS 2 deployments,
            and it's invisible until you look at `ros2 topic list` from the other machine."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a Twitter/X thread (5–7 tweets) about why ROS 2 DDS discovery fails
            silently in multi-machine deployments and what it cost OmniBot's development.

            Context:
            - OmniBot splits compute across Raspberry Pi 5 (drivers/nav) and GPU desktop (VLA).
            - ROS 2 uses DDS for discovery; default multicast can fail across subnets or
              when firewall rules block UDP.
            - Fix required: ROS_DOMAIN_ID=30 on both machines + ROS_STATIC_PEERS set to
              peer IPs; exported in launch_teleop.sh.
            - Failure mode: nodes started cleanly on both machines but cross-machine topics
              were invisible — no error, just silence.
            - The bug was found by running `ros2 topic list` from the remote machine.

            Thread requirements:
            - Hook: "Your ROS 2 robot is silent and you don't know why — here's the
              most common reason"
            - Explain DDS multicast discovery and why it breaks across subnets/VLANs
            - Show the exact env vars needed: ROS_DOMAIN_ID, ROS_STATIC_PEERS
            - Include the controversial take: "ROS 2 networking documentation assumes
              a flat LAN — real robots don't live there"
            - Practical takeaway: always validate multi-machine topic visibility before
              writing any cross-machine code
            - No hype, no beginner explanations, credible to DDS/ROS 2 engineers"""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article titled:
            "ROS 2 DDS Discovery Is Silent When It Fails — and That's a Systems Problem"

            Article structure:
            1. Introduction — Multi-machine robots and why networking is the first failure
            2. Problem Statement — DDS multicast vs unicast, subnets, UDP firewall rules
            3. Why Existing Approaches Break — Default ROS_DOMAIN_ID collisions,
               missing STATIC_PEERS, no discovery validation step
            4. OmniBot Case Study — Pi 5 + GPU desktop split, the silent failure, the fix
            5. Engineering Tradeoffs — DDS vs ROSBridge, when to use each
            6. Lessons Learned — Network validation checklist before any multi-machine work
            7. Future Directions — Zenoh transport, ROS_LOCALHOST_ONLY for development
            8. Final Takeaways — 3 rules for multi-machine ROS 2 deployment

            Style: practical, frustrated-but-constructive tone, real terminal commands."""),
        "visuals": [
            "Architecture diagram: Pi 5 ↔ GPU desktop with DDS peers labeled",
            "Terminal: `ros2 topic list` from remote machine before and after fix",
            "network.env file showing IP config",
            "`ros2 daemon status` output showing discovery state",
        ],
        "cross_links": "Previous: Serial protocol reverse-engineering\nFuture: multi-machine deployment, ROSBridge for Android, DDS vs Zenoh",
    }


def _content_readme(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why Your Robot's README Is a Systems Architecture Document",
        "core_idea": textwrap.dedent("""\
            OmniBot's README rewrite forced a confrontation with every architectural
            decision made so far: hardware cost breakdown, two-machine split rationale,
            quickstart instructions that actually work. Writing documentation for a
            real robot is harder than writing it for software because the reader needs
            to reproduce hardware + software + networking simultaneously. The cost
            comparison section revealed that OmniBot's total BOM is under $600 while
            comparable research platforms cost $15,000+. That gap is the entire argument
            for the project's existence."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread arguing that a robot's README is actually a systems
            architecture document — not a marketing page.

            Context: OmniBot rewrote its README to include hardware cost comparison,
            two-machine architecture rationale, and a quickstart that covers both
            software install and hardware wiring. Total BOM under $600 vs $15,000+
            for comparable research platforms.

            Requirements: practical, opinionated, mention cost transparency as a
            differentiator for open-source robotics projects."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Your Robot's README Should Be a Systems Document"
            covering: what good robotics documentation includes (cost, architecture,
            failure modes), what most projects get wrong, OmniBot case study with
            the cost comparison, and actionable README template for robot projects."""),
        "visuals": [
            "Screenshot of README hardware cost table",
            "Architecture diagram from README",
            "Quick-start terminal output showing successful bringup",
        ],
        "cross_links": "Previous: DDS networking\nFuture: DevContainers, CI/CD, contributor onboarding",
    }


def _content_imu_sensors(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why IMU Integration Is Harder in Simulation Than on Hardware",
        "core_idea": textwrap.dedent("""\
            Adding IMU and camera sensors to the Gazebo simulation exposed a gap: the
            simulated IMU publishes at a different rate and with different noise
            characteristics than the Yahboom board's physical IMU. Nav2's EKF expects
            consistent covariance values. OmniBot's fix was to parameterize covariance
            in the simulation config separately from the hardware config — but this
            means two tuning surfaces diverge over time. The lesson: simulation sensor
            models need calibration against real hardware, not just plausible defaults."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread about why IMU simulation in Gazebo is deceptively
            hard for real Nav2 deployments. Context: OmniBot added Gazebo IMU + camera
            plugins and immediately hit EKF covariance mismatch issues. Include the
            specific failure: robot localization diverges in simulation because noise
            parameters don't match hardware. Practical takeaway: validate sensor noise
            models against real hardware before tuning Nav2."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Why Simulation Sensors Lie to Your Navigation Stack"
            covering: Gazebo plugin defaults vs real hardware, EKF covariance tuning,
            OmniBot's dual-config approach, and when sim-to-real sensor calibration
            matters more than model fidelity."""),
        "visuals": [
            "RViz: simulated vs real IMU topic data side-by-side",
            "robot_localization.yaml showing covariance parameters",
            "Gazebo sensor plugin XML configuration",
            "EKF output comparison: simulated vs hardware odom",
        ],
        "cross_links": "Previous: README/architecture\nFuture: SLAM tuning, 3D mapping, sim-to-real",
    }


def _content_bev(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why Mobile Manipulation Needs a Bird's-Eye-View Camera (And Why It's Hard)",
        "core_idea": textwrap.dedent("""\
            SmolVLA's unified 9-DOF policy requires a bird's-eye-view image of the
            workspace alongside a wrist camera feed. OmniBot's solution is a BEV
            stitcher that composites 4 base-mounted OV9732 cameras into a single
            top-down image at 30 fps. The engineering challenge: camera placement on
            a two-plate mecanum chassis, calibrating the stitch homographies, and
            maintaining <100 ms end-to-end latency to the policy. If the BEV stitcher
            goes down, SmolVLA loses a required input and silently degrades — there
            is no graceful fallback in the current design."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 6-tweet thread about why building a bird's-eye-view camera system
            for a mobile manipulation robot is a harder engineering problem than it
            looks. Context: OmniBot uses 4 OV9732 USB cameras stitched into a BEV
            image feed for SmolVLA. Key challenges: homography calibration, latency,
            USB bandwidth, and no graceful fallback if stitcher fails. Include the
            controversial take: "most VLA papers ignore the sensor pipeline complexity
            that real deployment requires." Credible to computer vision + robotics
            engineers."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Building a Bird's-Eye-View Camera System for
            Mobile Manipulation" covering: why VLA models need BEV, homography
            calibration on a mecanum chassis, USB bandwidth limits with 5 cameras,
            OmniBot's ros2_bev_stitcher design, latency budget analysis, and failure
            modes when the stitcher goes down."""),
        "visuals": [
            "BEV stitched image from 4 cameras showing workspace",
            "Camera placement diagram on two-plate chassis",
            "RViz: /camera/base/bev/image_raw topic",
            "USB camera topology diagram showing all 5 feeds",
            "Latency measurement from camera capture to SmolVLA input",
        ],
        "cross_links": "Previous: simulation sensors\nFuture: SmolVLA integration, data collection pipeline, imitation learning",
    }


def _content_packages(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why Robotics Monorepos Need Standalone Packages (And When to Extract Them)",
        "core_idea": textwrap.dedent("""\
            OmniBot extracted six reusable Python packages from the ROS workspace:
            yahboom_ros2 (protocol), vla_serve (FastAPI inference), robot_episode_dataset
            (LeRobot helpers), ros2_bev_stitcher, mecanum_drive_ros2 (kinematics), and
            a shared utilities package. The trigger was that the mecanum kinematics and
            Yahboom protocol were being duplicated across multiple nodes. Extracting them
            as pip-installable packages with their own tests created a clear boundary:
            ROS-independent logic that can be unit tested without launching a ROS graph.
            The tradeoff is version management — bumping a shared package now requires
            all consumers to update together."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread about why robotics monorepos should extract
            ROS-independent logic into standalone Python packages. Context: OmniBot
            extracted 6 packages (kinematics, protocol encoder, FastAPI VLA server,
            dataset helpers, BEV stitcher, drive library) from its ROS workspace.
            Key point: standalone packages can be unit tested without a running ROS
            graph, which dramatically improves test coverage and CI speed. Include
            the tradeoff: coordinated version bumps across all consumers."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Monorepo Boundaries in Robotics: When to
            Extract a Standalone Package" covering: the duplication trigger, how to
            identify ROS-independent logic, OmniBot's 6-package extraction, testing
            standalone vs node-level, version management tradeoffs, and a decision
            framework for future extractions."""),
        "visuals": [
            "packages/ directory tree with 6 packages listed",
            "Dependency graph: ROS nodes → standalone packages",
            "Test run output: pytest on yahboom_ros2 without ROS installed",
            "pip install -e command for each package",
        ],
        "cross_links": "Previous: BEV cameras\nFuture: CI/CD, test coverage, package publishing to PyPI",
    }


def _content_slam(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why 3D Mapping on a Budget Robot Requires Careful Sensor Selection",
        "core_idea": textwrap.dedent("""\
            OmniBot's first 3D mapping implementation used a RealSense D435, then
            switched mid-development to an Orbbec Astra Pro. The switch wasn't
            arbitrary: the RealSense driver's colcon build dependency on librealsense
            introduced a CI breakage, and the Astra Pro's ROS 2 driver was simpler
            to integrate without an NVIDIA-specific kernel module. The depth camera
            choice cascades into RTAB-Map configuration, point cloud topics, and
            Nav2 costmap 3D obstacle inflation. Switching sensors mid-project
            means touching 5+ config files simultaneously."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread about the hidden system-level cost of switching
            depth cameras mid-project in a ROS 2 robot. Context: OmniBot switched
            from RealSense D435 to Orbbec Astra Pro due to CI driver complexity.
            Show how one hardware decision touches RTAB-Map, Nav2 costmaps, point
            cloud topics, and launch files. Practical takeaway: lock sensor choices
            early and audit downstream config dependencies before switching."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Why Depth Camera Choice Cascades Through Your
            Entire Navigation Stack" covering: RealSense vs Orbbec driver complexity,
            RTAB-Map config dependencies, Nav2 3D costmap integration, OmniBot's
            sensor switch story, and a checklist for evaluating sensor changes."""),
        "visuals": [
            "RTAB-Map 3D map in RViz showing point cloud + occupancy grid",
            "OctoMap visualization in RViz",
            "Config diff: rtabmap_params.yaml before/after sensor switch",
            "Photo of Orbbec Astra Pro mounted on OmniBot",
        ],
        "cross_links": "Previous: packages\nFuture: SLAM tuning, localization, Nav2 costmaps, sim-to-real",
    }


def _content_hybrid(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why Real Robots Need a Command Arbitration Layer Between Nav2 and AI Policies",
        "core_idea": textwrap.dedent("""\
            OmniBot's cmd_vel_mux is a single node that decides which velocity source
            wins at any moment: Nav2, VLA inference, teleoperation, or RL policy.
            Without this arbitration layer, concurrent publishers on /cmd_vel would
            fight each other — the last writer wins in ROS 2, which produces erratic
            motion. The mission_planner state machine ensures only one mode is active
            at a time. The lesson: any robot with more than one autonomy source needs
            explicit command arbitration, and it needs to be designed before integrating
            the second source, not after."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 6-tweet thread arguing that command arbitration is the most
            under-discussed system component in mobile robotics. Context: OmniBot
            built cmd_vel_mux to arbitrate between Nav2, OpenVLA, teleoperation, and
            RL policies — all publishing to /cmd_vel. Explain why "last writer wins"
            in ROS 2 is dangerous, what explicit arbitration looks like, and include
            the controversial take: "most robotics demos avoid this problem by running
            only one policy at a time — that's not a product, that's a demo." """),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Why Every Robot with Multiple Autonomy Sources
            Needs a Command Arbitration Layer" covering: the /cmd_vel race condition,
            OmniBot's cmd_vel_mux design, the mission_planner state machine, valid
            mode strings and transitions, safety invariants, and how to extend this
            for priority-based arbitration."""),
        "visuals": [
            "Architecture diagram: 4 vel sources → cmd_vel_mux → yahboom_controller",
            "ROS graph showing cmd_vel topic with multiple publishers",
            "State machine diagram: idle → navigating → vla → done",
            "Terminal: ros2 topic pub /control_mode showing mode switch",
        ],
        "cross_links": "Previous: 3D mapping\nFuture: RL inference nodes, arm_cmd_mux, mission planner deep dive",
    }


def _content_urdf(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why URDF Accuracy Matters More Than It Looks in Mobile Manipulation",
        "core_idea": textwrap.dedent("""\
            OmniBot's URDF went through 6 rapid-fire fix commits in a single day —
            wheel orientation, mecanum roller visuals, camera placement, SO-101 arm
            mesh integration, and reverting arm visuals back to primitives when the
            mesh caused RViz crashes. Each URDF error isn't just cosmetic: wrong
            wheel orientation breaks the mecanum forward kinematics visualization,
            wrong camera placement corrupts the TF tree used by SmolVLA for image
            alignment, and wrong arm joint origins propagate into MoveIt and LeRobot
            teleoperation. The URDF is the single source of geometric truth for the
            entire software stack."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread about why URDF accuracy is a systems problem, not
            a visualization problem. Context: OmniBot's URDF had 6 fix commits in
            one day covering wheel orientation, mecanum rollers, camera placement,
            and SO-101 arm mesh. Explain how URDF errors cascade into TF, Nav2
            costmaps, MoveIt, and VLA image alignment. Include practical takeaway:
            validate TF tree before any sensor/manipulation integration."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "URDF Is Your Robot's Source of Truth — Treat
            It That Way" covering: how URDF errors cascade into TF, Nav2, MoveIt,
            and VLA pipelines; OmniBot's 6-commit URDF fix story; the specific
            failure modes (wheel orientation → bad IK, camera placement → TF mismatch);
            and a URDF validation checklist."""),
        "visuals": [
            "RViz: correct vs incorrect wheel orientation in URDF",
            "TF tree view showing all robot frames",
            "SO-101 arm mesh in RViz vs primitive fallback",
            "Camera link TF frame placement relative to base_link",
            "xacro snippet showing mecanum roller visual geometry",
        ],
        "cross_links": "Previous: hybrid control\nFuture: Gazebo simulation, SmolVLA camera alignment, MoveIt integration",
    }


def _content_android(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why ROSBridge WebSocket Is the Right Android-to-Robot Interface (With Caveats)",
        "core_idea": textwrap.dedent("""\
            OmniBot's Android controller uses ROSBridge v2 JSON over WebSocket at
            `ws://robot:9090`. The MVVM architecture with OkHttp3 and Kotlin coroutines
            means the UI stays responsive when the robot connection drops. The harder
            problem was MJPEG streaming: the `MjpegView` component needed Content-Length
            parsing with a JPEG-marker fallback because some web_video_server
            configurations don't send Content-Length headers reliably. The ROSBridge
            reconnect policy (5 attempts, exponential backoff) also had to be tuned
            — too aggressive and it hammers the Pi; too conservative and the operator
            loses control during navigation."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread about the engineering tradeoffs of using ROSBridge
            WebSocket as the Android-to-ROS interface for a real robot. Context:
            OmniBot's Android app uses OkHttp3 WebSocket + Kotlin coroutines +
            MVVM. Key issues: MJPEG parsing quirks, reconnect policy tuning, cmd_vel
            topic disambiguation (teleop vs nav2 slot), and the latency of JSON
            serialization vs native DDS. Include one controversial take: "ROSBridge
            is good enough for teleoperation but the latency ceiling is real." """),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Building a Real Robot Controller Android App
            with ROSBridge WebSocket" covering: ROSBridge v2 protocol, OkHttp3
            reconnect policy, MJPEG parsing challenges, cmd_vel topic routing,
            MVVM architecture for robot control UIs, and latency budget analysis."""),
        "visuals": [
            "Android app screenshot showing joystick + camera feed + telemetry",
            "Architecture diagram: Android → WebSocket → ROSBridge → ROS topics",
            "Kotlin code snippet: OkHttp3 WebSocket reconnect logic",
            "Wireshark capture of ROSBridge JSON message",
        ],
        "cross_links": "Previous: URDF\nFuture: emergency stop, arm control from Android, AI command interface",
    }


def _content_rl(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why RL Inference Nodes Need Their Own Command Arbitration Layer",
        "core_idea": textwrap.dedent("""\
            Adding RL navigation and arm policies to OmniBot required extending both
            the cmd_vel_mux (new rl_nav mode) and adding an entirely new arm_cmd_mux.
            The RL policies run ONNX models at 20 Hz on the GPU desktop and publish
            to separate topics (`/cmd_vel/rl`, `/arm/joint_commands/rl`). The
            arbitration nodes decide when these outputs reach the hardware. This design
            means you can hot-swap between SmolVLA and RL arm control without stopping
            any node — the mux switches the output, not the policy. The failure mode:
            if the RL policy diverges, there's no confidence score to gate the output,
            so the mux has no signal to fall back automatically."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 6-tweet thread about the systems design of adding RL inference
            to a robot that already has VLA and Nav2. Context: OmniBot added rl_nav
            (27D obs → 3D act) and rl_arm (30D obs → 6D act) ONNX nodes alongside
            SmolVLA and Nav2, using separate mux layers. Key insight: mux-based
            arbitration lets you hot-swap policies without stopping nodes. Include
            the failure mode: no confidence gating for RL outputs."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Integrating RL Policies Into a Running ROS 2
            Robot Without Breaking Everything Else" covering: ONNX inference nodes,
            observation space design, cmd_vel_mux rl_nav extension, arm_cmd_mux
            design, action delta vs absolute commands, and the missing confidence
            gating problem."""),
        "visuals": [
            "Architecture diagram: RL nodes → mux → hardware alongside VLA/Nav2",
            "ONNX model inference timing at 20 Hz",
            "Isaac Lab training reward curve",
            "Terminal: switching /control_mode between nav2 and rl_nav",
        ],
        "cross_links": "Previous: Android/ROSBridge\nFuture: Isaac Lab training details, sim-to-real gap, ONNX export",
    }


def _content_observability(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why Robotics Systems Need Observability Platforms, Not Just ROS Logs",
        "core_idea": textwrap.dedent("""\
            OmniBot's Prometheus + Grafana + Loki + Tempo stack exposes metrics that
            ROS logs cannot: P95 control loop latency, VLA inference time distribution,
            VRAM usage during inference, mission success rate over time. The critical
            alert that justified the whole setup: `ControlLoopCritical` fires when the
            yahboom_controller_node P95 exceeds 100 ms — indicating that the Pi 5 is
            CPU-throttling or the serial port is stalling. Without this metric, a
            software update that adds 80 ms of latency to the control loop would be
            invisible until the robot starts behaving erratically."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 6-tweet thread arguing that robotics systems need production-grade
            observability, not just `ros2 topic echo`. Context: OmniBot runs
            Prometheus + Grafana + Loki + Tempo, exposing P95 control loop latency,
            VLA inference time, GPU VRAM, and mission success rate. Key insight: a
            control loop regression that adds 80 ms latency is invisible in logs
            but visible as a metric alert. Include: what metrics matter most for
            mobile manipulation robots."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "Why Your Robot Needs Prometheus, Not Just
            ROS Logs" covering: what metrics matter in robotics (control loop P95,
            inference latency, VRAM, mission success rate), OmniBot's full stack,
            the ros2_prometheus_bridge design, alert rules that matter, and how to
            set this up on a Pi 5 + GPU desktop split."""),
        "visuals": [
            "Grafana dashboard screenshot: Robot Health panel",
            "Grafana: VLA inference latency histogram",
            "AlertManager alert for ControlLoopCritical",
            "Architecture diagram: Pi metrics bridge → Prometheus → Grafana",
            "W&B training run showing PPO reward curves",
        ],
        "cross_links": "Previous: RL policies\nFuture: CI/CD, W&B sweep results, runtime monitoring for RL nodes",
    }


def _content_ci(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why Robotics CI Is Harder Than Web CI (And What OmniBot Does About It)",
        "core_idea": textwrap.dedent("""\
            OmniBot's GitHub Actions CI builds the full ROS 2 Jazzy workspace on
            ubuntu-24.04, installs torch, transformers, and accelerate, and runs
            colcon test. The known gap: no hardware-in-loop tests, no Gazebo
            integration tests, and linting is disabled. The serial driver has 0%
            test coverage. The CI catches build breakage and import errors but
            cannot catch the most important robot failures: wrong checksum, bad
            odometry, VLA inference regression. This is the honest state of most
            robotics CI — it validates compilation, not behavior."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread being honest about the state of CI in real
            robotics projects. Context: OmniBot's CI builds ROS 2 Jazzy + runs
            colcon test but has 0% coverage on the serial driver, no HIL tests,
            and disabled linting. Argue that this is normal for early robotics
            projects but has a ceiling: CI that only checks compilation misses
            the most expensive bugs. Include what good robotics CI would add."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "The Honest State of Robotics CI: What It
            Catches and What It Misses" covering: OmniBot's current CI (build +
            colcon test), the known gaps, why HIL testing is expensive, what
            simulation tests can and can't replace, and a roadmap for improving
            robotics test coverage."""),
        "visuals": [
            "GitHub Actions CI passing screenshot",
            "colcon test-result output",
            "Coverage report showing 0% on yahboom_controller_node",
            "CI workflow YAML showing torch/transformers install step",
        ],
        "cross_links": "Previous: observability\nFuture: test coverage improvement, HIL testing, simulation-based tests",
    }


def _content_ota(commit: dict, stat: str) -> dict:
    return {
        "topic": "Why Mobile Robots Need OTA Updates (And Why It's a Safety Problem)",
        "core_idea": textwrap.dedent("""\
            OmniBot's OTA update system lets you push new ROS 2 package builds and
            ONNX model files to the Pi 5 without SSH access to the robot. The safety
            constraint: OTA updates must not apply while the robot is in motion or
            during an active VLA inference session. The implementation uses a
            pre-update health check that gates on /emergency_stop and /control_mode/active.
            Getting this wrong means a robot that restarts its driver node mid-navigation
            — which is worse than no update system at all."""),
        "twitter_prompt": textwrap.dedent("""\
            Write a 5-tweet thread about why OTA updates for real robots are a safety
            engineering problem, not just a DevOps problem. Context: OmniBot implements
            OTA updates for Pi 5 packages + ONNX models with a pre-update gate on
            emergency stop and control mode. Key insight: a failed OTA mid-navigation
            is more dangerous than running stale software. Include the gate conditions
            and rollback strategy."""),
        "substack_prompt": textwrap.dedent("""\
            Write a Substack article: "OTA Updates for Real Robots: Safety Gates,
            Rollback, and the Update Window Problem" covering: why robots need OTA,
            safety preconditions (estop, control mode), ONNX model update atomicity,
            rollback strategy, OmniBot's implementation, and what commercial robots
            do differently."""),
        "visuals": [
            "OTA update flow diagram: trigger → health check → apply → verify",
            "Terminal: OTA update applying to Pi 5",
            "/emergency_stop and /control_mode gate check in code",
            "Before/after: robot software version via ros2 node info",
        ],
        "cross_links": "Previous: CI/CD\nFuture: fleet management, deployment modes, configuration management",
    }


def _content_generic(commit: dict, stat: str, idx: int) -> dict:
    msg = commit["message"]
    return {
        "topic": f"Engineering Insights from OmniBot — Commit Day {idx + 1}",
        "core_idea": textwrap.dedent(f"""\
            Commit: "{msg}" ({stat}).
            This change continued OmniBot's incremental systems integration. Each commit
            in a real robotics project represents a resolved constraint: a hardware quirk
            fixed, a config parameter tuned, or an interface clarified. The cumulative
            effect of small, targeted commits is a robot that fails in fewer ways."""),
        "twitter_prompt": textwrap.dedent(f"""\
            Write a 4-tweet thread sharing a practical robotics engineering lesson
            from this OmniBot development milestone: "{msg}".
            Keep it grounded, practical, and credible to ROS 2 engineers.
            Include one concrete lesson and one systems-level takeaway."""),
        "substack_prompt": textwrap.dedent(f"""\
            Write a Substack post about the engineering lesson behind this OmniBot
            commit: "{msg}". Include the problem context, the fix, and what
            it reveals about real robotics systems integration."""),
        "visuals": [
            "Terminal output or ROS graph showing the change",
            "Relevant config file or code diff",
        ],
        "cross_links": "Continue from previous commit's topic.",
    }


# ── Discord sender ─────────────────────────────────────────────────────────────


def split_message(text: str, limit: int = DISCORD_MAX_CHARS) -> list[str]:
    """Split a string into chunks that fit within Discord's character limit."""
    if len(text) <= limit:
        return [text]

    chunks = []
    current = ""
    for line in text.splitlines(keepends=True):
        if len(current) + len(line) > limit:
            if current:
                chunks.append(current)
            current = line
        else:
            current += line
    if current:
        chunks.append(current)
    return chunks


def send_to_discord(content: str, retries: int = 4) -> bool:
    """POST a message to the Discord webhook, splitting if needed."""
    chunks = split_message(content)
    success = True
    for i, chunk in enumerate(chunks):
        payload = json.dumps({"content": chunk}).encode("utf-8")
        req = urllib.request.Request(
            DISCORD_WEBHOOK_URL,
            data=payload,
            headers={
                "Content-Type": "application/json",
                "User-Agent": "OmniBot-ContentBot/1.0",
            },
            method="POST",
        )
        delay = 2
        for attempt in range(retries):
            try:
                with urllib.request.urlopen(req) as resp:
                    if resp.status in (200, 204):
                        print(f"  Chunk {i + 1}/{len(chunks)} sent.")
                        break
                    else:
                        print(f"  Chunk {i + 1} HTTP {resp.status}, retrying…")
            except urllib.error.URLError as e:
                print(f"  Error: {e}. Retry {attempt + 1} in {delay}s…")
                time.sleep(delay)
                delay *= 2
        else:
            print(f"  Failed to send chunk {i + 1} after {retries} attempts.")
            success = False
        time.sleep(0.5)  # rate-limit between chunks
    return success


def format_discord_message(
    content: dict, commit: dict, commit_index: int, total_commits: int
) -> str:
    """Format content dict into a Markdown Discord message."""
    date_str = commit["date"][:10]
    msg = commit["message"]

    header = (
        f"# OmniBot Daily Content — Day {commit_index + 1} / {total_commits}\n"
        f"**Commit**: `{commit['hash'][:8]}` — {date_str}\n"
        f"**Message**: {msg}\n"
        f"---\n"
    )

    topic_section = (
        f"## Daily Topic\n"
        f"**{content['topic']}**\n\n"
    )

    core_section = (
        f"## Core Idea\n"
        f"{content['core_idea']}\n"
    )

    twitter_section = (
        f"## Twitter/X Post Prompt\n"
        f"```\n{content['twitter_prompt']}\n```\n"
    )

    substack_section = (
        f"## Substack Article Prompt\n"
        f"```\n{content['substack_prompt']}\n```\n"
    )

    visuals_section = (
        "## Suggested Visuals\n"
        + "".join(f"- {v}\n" for v in content["visuals"])
        + "\n"
    )

    cross_section = (
        f"## Cross-Link Opportunities\n"
        f"{content['cross_links']}\n"
    )

    return (
        header
        + topic_section
        + core_section
        + twitter_section
        + substack_section
        + visuals_section
        + cross_section
    )


# ── Main ──────────────────────────────────────────────────────────────────────


def main() -> None:
    print("OmniBot Daily Content Generator")
    print(f"  Repo root : {REPO_ROOT}")
    print(f"  State file: {STATE_FILE}")

    commits = get_commits_ascending()
    total = len(commits)
    print(f"  Total commits: {total}")

    state = load_state()
    next_index = state["last_commit_index"] + 1

    if next_index >= total:
        print("All commits have been covered. Resetting to the beginning.")
        next_index = 0

    commit = commits[next_index]
    print(f"\n  Processing commit {next_index + 1}/{total}: {commit['hash'][:8]}")
    print(f"  {commit['date'][:10]} — {commit['message']}")

    content = generate_content_for_commit(commit, next_index)

    print("\n  Generating Discord message…")
    discord_msg = format_discord_message(content, commit, next_index, total)

    print(f"  Message length: {len(discord_msg)} chars")
    print("\n  Sending to Discord…")

    ok = send_to_discord(discord_msg)

    if ok:
        state["last_commit_index"] = next_index
        state["last_run"] = datetime.utcnow().isoformat()
        state["last_commit_hash"] = commit["hash"]
        state["last_topic"] = content["topic"]
        save_state(state)
        print("\nDone. State saved.")
    else:
        print("\nDiscord send failed — state NOT advanced.")
        sys.exit(1)


if __name__ == "__main__":
    main()
