"""
collect_episodes.py
-------------------
Automated episode collection for OmniBot inside NVIDIA Isaac Sim.

This script runs INSIDE Isaac Sim's Python environment (not in a standard
Python venv).  It uses:
  - omni.replicator.core    — domain randomisation (lighting, objects, textures)
  - omni.isaac.core         — World / ArticulationView
  - omni.isaac.ros2_bridge  — publishes ROS 2 topics from the simulation

Usage
-----
    cd <isaac_sim_root>
    ./python.sh <repo>/data_engine/isaac_sim/collect_episodes.py \
        --episodes 500 \
        --task "pick up the red cup and place it on the tray" \
        --output ~/datasets/isaac_bags \
        --dataset ~/datasets/omnibot_isaac \
        --config <repo>/data_engine/isaac_sim/randomization_config.yaml

What it does per episode
------------------------
1. Trigger Replicator randomisation (lighting, target-object pose, distractors).
2. Start `ros2 bag record` capturing all OmniBot topics.
3. Run a scripted pick-and-place primitive via /cmd_vel + /arm/joint_commands.
4. Stop the bag recorder.
5. Reset the world for the next episode.

After all episodes are collected, run the ingestion pipeline manually:
    python -m data_engine.ingestion.bag_to_omnibot \
        --bag ~/datasets/isaac_bags/episode_00000 \
        --dataset ~/datasets/omnibot_isaac \
        --task "pick up the red cup and place it on the tray"

Or use the batch helper included at the bottom of this file:
    python data_engine/scripts/ingest_dataset.py \
        --input ~/datasets/isaac_bags \
        --output ~/datasets/omnibot_isaac \
        --task "pick up the red cup and place it on the tray"

Requirements
------------
- Isaac Sim 4.x with omni.replicator.core + omni.isaac.ros2_bridge extensions.
- ROS 2 Jazzy sourced in the same shell:
      source /opt/ros/jazzy/setup.bash
- numpy, pyyaml available in the Isaac Sim Python env.
"""

from __future__ import annotations

import argparse
import subprocess
import time
import yaml
from pathlib import Path

# ── Isaac Sim imports (only available inside ./python.sh) ────────────────────
try:
    import omni  # noqa: F401
    import omni.replicator.core as rep
    from omni.isaac.core import World
    from omni.isaac.core.utils.stage import add_reference_to_stage
    from omni.isaac.core.utils.nucleus import get_assets_root_path

    _ISAAC_AVAILABLE = True
except ImportError:
    _ISAAC_AVAILABLE = False

# ── ROS 2 imports ────────────────────────────────────────────────────────────
try:
    import rclpy
    from geometry_msgs.msg import Twist
    from sensor_msgs.msg import JointState

    _ROS_AVAILABLE = True
except ImportError:
    _ROS_AVAILABLE = False

# ── Paths ────────────────────────────────────────────────────────────────────
_REPO_ROOT = Path(__file__).parents[2]
_USD_ROBOT = _REPO_ROOT / "robot_ws/src/omnibot_description/usd/omnibot.usd"
_DEFAULT_CONFIG = Path(__file__).parent / "randomization_config.yaml"

# ARM joint names must match arm_params.yaml (prefixed form)
_ARM_JOINT_NAMES = [
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
]

# Topics recorded into each ROS 2 bag
_BAG_TOPICS = [
    "/cmd_vel",
    "/odom",
    "/tf",
    "/imu/data",
    "/joint_states",
    "/arm/joint_states",
    "/camera/image_raw",
    "/camera/wrist/image_raw",
    "/camera/front/image_raw",
    "/camera/rear/image_raw",
    "/camera/left/image_raw",
    "/camera/right/image_raw",
    "/camera/depth/image_raw",
    "/camera/depth/points",
]


# ── Scene setup ──────────────────────────────────────────────────────────────


def _load_config(config_path: Path) -> dict:
    with open(config_path) as f:
        return yaml.safe_load(f)


def _setup_scene(cfg: dict, usd_robot: Path) -> "World":
    """Load the robot USD into Isaac Sim and configure Replicator randomisers."""
    world = World(stage_units_in_meters=1.0)
    world.scene.add_default_ground_plane()

    if not usd_robot.exists():
        raise FileNotFoundError(
            f"Robot USD not found: {usd_robot}\n"
            "Generate it first:\n"
            "  xacro robot_ws/src/omnibot_description/urdf/omnibot.urdf.xacro > /tmp/omnibot.urdf\n"
            "  Then import into Isaac Sim: File → Import → URDF"
        )
    add_reference_to_stage(str(usd_robot), "/World/OmniBot")

    # Optional: add a simple table prim
    assets_root = get_assets_root_path()
    if assets_root:
        table_usd = f"{assets_root}/Isaac/Props/KLT_Bin/small_KLT.usd"
        add_reference_to_stage(table_usd, "/World/Table")

    # ── Replicator domain randomisation ──────────────────────────────────
    with rep.new_layer():
        # Randomise lighting
        lights = rep.create.light(
            light_type="Sphere",
            count=cfg.get("num_lights", 3),
            position=rep.distribution.uniform((-3, -3, 1.5), (3, 3, 3.5)),
            intensity=rep.distribution.uniform(
                cfg.get("light_intensity_min", 500),
                cfg.get("light_intensity_max", 4000),
            ),
            color=rep.distribution.uniform((0.8, 0.8, 0.8), (1.0, 1.0, 1.0)),
        )
        rep.randomizer.register(lights)

        # Randomise target object position on the table surface
        with rep.trigger.on_custom_event(event_name="randomize_scene"):
            with rep.get.prims(path_pattern="/World/TargetObject"):
                rep.randomizer.scatter_2d(
                    surface_prims=["/World/Table"],
                    check_for_collisions=True,
                )

            # Randomise distractor objects
            distractors = rep.get.prims(path_pattern="/World/Distractor_*")
            with distractors:
                rep.randomizer.scatter_2d(
                    surface_prims=["/World/Table"],
                    check_for_collisions=True,
                )

    world.reset()
    return world


# ── Scripted policy primitives ───────────────────────────────────────────────


def _publish_base_vel(
    pub: "rclpy.publisher.Publisher", vx: float, vy: float, w: float
) -> None:
    msg = Twist()
    msg.linear.x = vx
    msg.linear.y = vy
    msg.angular.z = w
    pub.publish(msg)


def _publish_arm_cmd(pub: "rclpy.publisher.Publisher", positions: list[float]) -> None:
    msg = JointState()
    msg.name = _ARM_JOINT_NAMES
    msg.position = positions
    pub.publish(msg)


def _run_scripted_policy(
    world: "World",
    node: "rclpy.Node",
    base_pub: "rclpy.publisher.Publisher",
    arm_pub: "rclpy.publisher.Publisher",
    cfg: dict,
) -> None:
    """
    Simple scripted pick-and-place primitive.

    Stages:
      1. Approach: drive forward toward the table.
      2. Pre-grasp: extend arm above target.
      3. Grasp: lower arm + close gripper.
      4. Lift: raise arm with object.
      5. Retreat: drive back to drop zone.
      6. Place: lower arm + open gripper.
      7. Home: return arm to home pose.
    """
    step_dt = 1.0 / cfg.get("policy_hz", 10.0)

    # Home pose (all zeros except gripper slightly open)
    home_pos = [0.0, 0.0, 0.0, 0.0, 0.0, 0.1]
    # Pre-grasp: arm extended forward
    pregrasp = [0.0, -0.8, 0.6, -0.5, 0.0, 0.1]
    # Grasp: lower end-effector
    grasp_pos = [0.0, -1.0, 0.8, -0.6, 0.0, 0.1]
    # Closed gripper
    grasped = [0.0, -1.0, 0.8, -0.6, 0.0, 0.7]
    # Lifted
    lifted = [0.0, -0.6, 0.5, -0.4, 0.0, 0.7]

    def step_world(
        duration_s: float,
        base_vx: float = 0.0,
        base_vy: float = 0.0,
        arm_pos: list[float] | None = None,
    ) -> None:
        deadline = time.time() + duration_s
        while time.time() < deadline:
            _publish_base_vel(base_pub, base_vx, base_vy, 0.0)
            if arm_pos is not None:
                _publish_arm_cmd(arm_pub, arm_pos)
            world.step(render=True)
            time.sleep(step_dt)

    step_world(1.5, base_vx=0.12, arm_pos=home_pos)  # approach
    step_world(0.5, arm_pos=pregrasp)  # pre-grasp
    step_world(0.8, arm_pos=grasp_pos)  # lower
    step_world(0.5, arm_pos=grasped)  # close gripper
    step_world(0.8, arm_pos=lifted)  # lift
    step_world(1.5, base_vx=-0.12, arm_pos=lifted)  # retreat
    step_world(0.8, arm_pos=grasp_pos)  # lower to place
    step_world(0.5, arm_pos=grasped[:5] + [0.1])  # open gripper
    step_world(0.5, arm_pos=home_pos)  # home


# ── Main collection loop ─────────────────────────────────────────────────────


def collect_episodes(
    num_episodes: int,
    task_description: str,
    output_dir: Path,
    config_path: Path,
    usd_robot: Path,
) -> None:
    if not _ISAAC_AVAILABLE:
        raise RuntimeError(
            "Isaac Sim Python modules not found.\n"
            "Run this script with Isaac Sim's Python interpreter:\n"
            "  <isaac_sim_root>/python.sh data_engine/isaac_sim/collect_episodes.py"
        )
    if not _ROS_AVAILABLE:
        raise RuntimeError(
            "rclpy not found. Source ROS 2 Jazzy before running:\n"
            "  source /opt/ros/jazzy/setup.bash"
        )

    output_dir.mkdir(parents=True, exist_ok=True)
    cfg = _load_config(config_path)
    world = _setup_scene(cfg, usd_robot)

    rclpy.init()
    node = rclpy.create_node("isaac_episode_collector")
    base_pub = node.create_publisher(Twist, "/cmd_vel", 10)
    arm_pub = node.create_publisher(JointState, "/arm/joint_commands", 10)

    print(f"[collect_episodes] Starting collection of {num_episodes} episodes.")
    print(f"[collect_episodes] Task: '{task_description}'")
    print(f"[collect_episodes] Output: {output_dir}")

    for ep_idx in range(num_episodes):
        bag_path = output_dir / f"episode_{ep_idx:05d}"
        print(f"\n[collect_episodes] Episode {ep_idx + 1}/{num_episodes} → {bag_path}")

        # Trigger Replicator randomisation
        rep.orchestrator.step(rt_subframes=4)

        # Start ROS 2 bag recorder
        bag_proc = subprocess.Popen(
            ["ros2", "bag", "record", "-o", str(bag_path)] + _BAG_TOPICS,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )

        # Allow the bag recorder to initialise
        time.sleep(0.5)

        try:
            _run_scripted_policy(world, node, base_pub, arm_pub, cfg)
        except Exception as exc:  # noqa: BLE001
            print(f"[collect_episodes] Policy error on episode {ep_idx}: {exc}")
        finally:
            # Stop bag recorder
            bag_proc.terminate()
            bag_proc.wait()

        # Reset simulation for next episode
        world.reset()

    rclpy.shutdown()
    print(f"\n[collect_episodes] Done. {num_episodes} bags saved to {output_dir}")
    print(
        "\nNext step — ingest bags into LeRobot format:\n"
        f"  python -m data_engine.ingestion.bag_to_omnibot \\\n"
        f"      --bag {output_dir}/episode_00000 \\\n"
        f"      --dataset ~/datasets/omnibot_isaac \\\n"
        f'      --task "{task_description}"\n'
        "\nOr batch-ingest all bags:\n"
        f"  python data_engine/scripts/ingest_dataset.py \\\n"
        f"      --input {output_dir} \\\n"
        f"      --output ~/datasets/omnibot_isaac \\\n"
        f'      --task "{task_description}"'
    )


# ── CLI ──────────────────────────────────────────────────────────────────────


def _parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Collect OmniBot episodes in Isaac Sim with domain randomisation."
    )
    p.add_argument(
        "--episodes",
        type=int,
        default=100,
        help="Number of episodes to collect (default: 100)",
    )
    p.add_argument(
        "--task",
        default="pick up the red cup and place it on the tray",
        help="Natural language task description written to dataset metadata",
    )
    p.add_argument(
        "--output",
        type=Path,
        default=Path.home() / "datasets/isaac_bags",
        help="Directory to save ROS 2 bags (default: ~/datasets/isaac_bags)",
    )
    p.add_argument(
        "--dataset",
        type=Path,
        default=Path.home() / "datasets/omnibot_isaac",
        help="LeRobot dataset root for direct ingestion after collection",
    )
    p.add_argument(
        "--config",
        type=Path,
        default=_DEFAULT_CONFIG,
        help="Path to randomization_config.yaml",
    )
    p.add_argument(
        "--usd",
        type=Path,
        default=_USD_ROBOT,
        help="Path to omnibot.usd (generated from URDF importer)",
    )
    return p.parse_args()


if __name__ == "__main__":
    args = _parse_args()
    collect_episodes(
        num_episodes=args.episodes,
        task_description=args.task,
        output_dir=args.output,
        config_path=args.config,
        usd_robot=args.usd,
    )
