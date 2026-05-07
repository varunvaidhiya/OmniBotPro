#!/usr/bin/env bash
# benchmarks/run_benchmarks.sh
# ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
# Machine-aware benchmark runner for OmniBot.
#
# Usage:
#   ./benchmarks/run_benchmarks.sh           # auto-detect machine type
#   ./benchmarks/run_benchmarks.sh ci        # pure Python, no hardware, no ROS
#   ./benchmarks/run_benchmarks.sh pi5       # Raspberry Pi 5 (ARM64)
#   ./benchmarks/run_benchmarks.sh gpu       # GPU desktop (CUDA)
#   ./benchmarks/run_benchmarks.sh ros       # ROS-required benchmarks (needs running nodes)
#
# Environment variables:
#   OMNIBOT_SERIAL_PORT   Set to /dev/ttyUSB0 to run hardware serial benchmarks
#   OMNIBOT_MACHINE       Override machine detection (ci/pi5/gpu)

set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# ---------------------------------------------------------------------------
# Machine detection
# ---------------------------------------------------------------------------
MACHINE="${1:-auto}"

if [[ "$MACHINE" == "auto" ]]; then
    if python3 -c "import torch; assert torch.cuda.is_available()" 2>/dev/null; then
        MACHINE="gpu"
    elif [[ "$(uname -m)" == "aarch64" ]]; then
        MACHINE="pi5"
    else
        MACHINE="ci"
    fi
fi

export OMNIBOT_MACHINE="$MACHINE"
echo "========================================================================"
echo "OmniBot Performance Benchmarks — machine: $MACHINE"
echo "========================================================================"

# ---------------------------------------------------------------------------
# Install packages (if not already installed)
# ---------------------------------------------------------------------------
echo ""
echo "[Setup] Installing Python packages..."
pip install -e packages/yahboom_ros2 -e packages/mecanum_drive_ros2 \
    -q --break-system-packages 2>/dev/null || \
pip install -e packages/yahboom_ros2 -e packages/mecanum_drive_ros2 -q

# ---------------------------------------------------------------------------
# Run benchmarks based on machine type
# ---------------------------------------------------------------------------

PYTEST_COMMON="-v --tb=short --no-header"

run_suite() {
    local label="$1"
    shift
    echo ""
    echo "--- $label ---"
    python3 -m pytest "$@" $PYTEST_COMMON || true
}

case "$MACHINE" in
  ci)
    # Pure Python, no hardware, no ROS — runs on any x86-64 Linux/macOS
    run_suite "Serial Protocol" \
        benchmarks/serial/bench_yahboom_protocol.py \
        -m "not hardware and not ros and not gpu"

    run_suite "Kinematics" \
        benchmarks/kinematics/bench_mecanum_kinematics.py \
        -m "not hardware and not ros and not gpu"

    run_suite "BEV Stitcher (CPU)" \
        benchmarks/vision/bench_bev_stitcher.py \
        -m "not hardware and not ros and not gpu" \
        -k "not gpu"

    run_suite "SmolVLA Preprocess (CPU)" \
        benchmarks/vision/bench_smolvla_preprocess.py \
        -m "not hardware and not ros and not gpu" \
        -k "not gpu_transfer"
    ;;

  pi5)
    # ARM64 Pi 5 — all CPU benchmarks, optional hardware serial
    run_suite "Serial Protocol" \
        benchmarks/serial/bench_yahboom_protocol.py \
        -m "not gpu"

    if [[ -n "${OMNIBOT_SERIAL_PORT:-}" ]]; then
        run_suite "Serial I/O (hardware)" \
            benchmarks/serial/bench_serial_io.py
    else
        echo "  INFO  Set OMNIBOT_SERIAL_PORT to run hardware serial benchmarks."
    fi

    run_suite "Kinematics" \
        benchmarks/kinematics/bench_mecanum_kinematics.py \
        -m "not gpu"

    run_suite "BEV Stitcher (Pi 5 CPU)" \
        benchmarks/vision/bench_bev_stitcher.py \
        -m "not gpu" -k "not gpu"

    run_suite "SmolVLA Preprocess (Pi 5 CPU)" \
        benchmarks/vision/bench_smolvla_preprocess.py \
        -m "not gpu" -k "not gpu_transfer"

    run_suite "Full Pipeline (Pi 5, no inference)" \
        benchmarks/system/bench_full_pipeline.py \
        -m "not gpu" -k "no_inference or stage_breakdown"
    ;;

  gpu)
    # CUDA GPU desktop — all benchmarks including inference
    run_suite "Serial Protocol" \
        benchmarks/serial/bench_yahboom_protocol.py \
        -m "not hardware and not ros"

    run_suite "Kinematics" \
        benchmarks/kinematics/bench_mecanum_kinematics.py \
        -m "not hardware and not ros"

    run_suite "BEV Stitcher" \
        benchmarks/vision/bench_bev_stitcher.py \
        -m "not hardware and not ros"

    run_suite "SmolVLA Preprocess" \
        benchmarks/vision/bench_smolvla_preprocess.py \
        -m "not hardware and not ros"

    run_suite "SmolVLA Inference" \
        benchmarks/inference/bench_smolvla_inference.py \
        -m "gpu" --timeout=300

    run_suite "Full Pipeline" \
        benchmarks/system/bench_full_pipeline.py \
        -m "not hardware and not ros"
    ;;

  ros)
    # ROS 2 benchmarks — requires sourced ROS Jazzy and running nodes
    if ! python3 -c "import rclpy" 2>/dev/null; then
        echo "ERROR: rclpy not importable. Source your ROS 2 workspace first:"
        echo "  source /opt/ros/jazzy/setup.bash"
        echo "  source robot_ws/install/setup.bash"
        exit 1
    fi

    run_suite "ROS Topic Latency (intra-process)" \
        benchmarks/ros/bench_topic_latency.py \
        -m "ros" -k "intraprocess"

    echo ""
    echo "  INFO  For live topic latency tests, start the relevant nodes first:"
    echo "    ros2 run omnibot_driver yahboom_controller_node"
    echo "    ros2 run omnibot_arm arm_driver_node"
    echo "    ros2 run ros2_bev_stitcher bev_stitcher_node"

    run_suite "ROS Topic Latency (live)" \
        benchmarks/ros/bench_topic_latency.py \
        -m "ros" -k "not intraprocess"

    run_suite "ROS Control Loop (mock serial)" \
        benchmarks/ros/bench_control_loop.py \
        -m "ros" -k "mock_serial or read_yahboom"
    ;;

  *)
    echo "Unknown machine type: $MACHINE"
    echo "Usage: $0 [ci|pi5|gpu|ros|auto]"
    exit 1
    ;;
esac

# ---------------------------------------------------------------------------
# Compare against baseline
# ---------------------------------------------------------------------------
echo ""
echo "--- Regression Check ---"
if ls benchmarks/results/*.json 2>/dev/null | head -1 > /dev/null; then
    python3 benchmarks/compare_baseline.py || \
        echo "  WARN  Regression check failed (see above). Run with --update-baseline to update."
else
    echo "  INFO  No results yet — run benchmarks first."
fi

echo ""
echo "========================================================================"
echo "Done. Results in benchmarks/results/"
echo "To update baseline: python3 benchmarks/compare_baseline.py --update-baseline"
echo "========================================================================"
