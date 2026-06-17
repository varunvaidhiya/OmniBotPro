/*
 * OhhO Pilot — MuJoCo provider (pro tier).
 *
 * Fast, accurate physics engine for robotics research. Excellent for RL
 * training and sim-to-real transfer. Requires a running MuJoCo simulation
 * with the ROS 2 bridge plugin enabled.
 *
 * Requires: Fleet or Forge plan.
 */

import type {
  SimulatorProvider,
  SimulatorStatus,
  SimProviderMeta,
  Velocity,
  Odometry,
  JointState,
  SimulatedFrame,
  RecordedEpisode,
} from "./types";

const META: SimProviderMeta = {
  id: "mujoco",
  name: "MuJoCo (DeepMind)",
  desc: "Fast, accurate physics engine. Excellent for RL training and sim-to-real transfer.",
  tier: "pro",
  requiresServer: true,
  defaultUrl: "ws://localhost:9090",
  setupHint: "Install mujoco and mujoco_ros2. Requires a running MuJoCo simulation with ROS 2 bridge enabled.",
  supportsRecording: true,
  available: false,
};

let _warned = false;

export class MuJoCoProvider implements SimulatorProvider {
  readonly meta = META;

  async connect(): Promise<SimulatorStatus> {
    if (!_warned) {
      console.info(
        "[OhhO Pilot] MuJoCo provider not yet bundled. Requires Fleet or Forge plan. " +
          "Install mujoco and mujoco_ros2. Start the simulation and enable the ROS 2 bridge.",
      );
      _warned = true;
    }
    return {
      connected: false,
      connecting: false,
      label: "MuJoCo · Unavailable",
      latency: 0,
      fps: 0,
      error: "MuJoCo provider requires Fleet or Forge plan. Contact sales to upgrade.",
    };
  }

  disconnect(): void {}
  getStatus(): SimulatorStatus {
    return { connected: false, connecting: false, label: "MuJoCo · Pro tier", latency: 0, fps: 0 };
  }
  sendVelocity(_vel: Velocity): void {}
  sendJointCommand(_name: string, _pos: number): void {}
  emergencyStop(): void {}
  releaseStop(): void {}
  onOdometry(_cb: (o: Odometry) => void): () => void { return () => {}; }
  onJointStates(_cb: (j: JointState[]) => void): () => void { return () => {}; }
  onCameraFrame(_camera: string, _cb: (f: SimulatedFrame) => void): () => void { return () => {}; }
  onStatusChange(_cb: (s: SimulatorStatus) => void): () => void { return () => {}; }
  startRecording(_instruction?: string): void {}
  stopRecording(): null { return null; }
  isRecording(): boolean { return false; }
  downloadLastEpisode(): void {}
}
