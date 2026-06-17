/*
 * OhhO Pilot — NVIDIA Isaac Sim provider (pro tier).
 *
 * Connects to Isaac Sim via ROS 2 Bridge (ROS_DOMAIN_ID 30) + ROSBridge
 * WebSocket. Provides photorealistic rendering, PhysX physics, domain
 * randomization, and synthetic data generation.
 *
 * Requires: Fleet or Forge plan. NVIDIA GPU (RTX 3060+).
 *
 * Setup:
 *   1. Launch Isaac Sim with the Omnibot USD scene
 *   2. Enable Isaac ROS 2 Bridge extension
 *   3. Start rosbridge_server (port 9090)
 *   4. Select "NVIDIA Isaac Sim" in Pilot
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
  id: "isaac",
  name: "NVIDIA Isaac Sim",
  desc: "Photorealistic simulation — PhysX 5, RTX rendering, domain randomization, synthetic data generation.",
  tier: "pro",
  requiresServer: true,
  defaultUrl: "ws://localhost:9090",
  setupHint: "Launch Isaac Sim with the OmniBot USD scene. Requires NVIDIA GPU (RTX 3060+). Enable ROS 2 Bridge + rosbridge.",
  supportsRecording: true,
  available: false,
};

let _warned = false;

export class IsaacSimProvider implements SimulatorProvider {
  readonly meta = META;

  async connect(): Promise<SimulatorStatus> {
    if (!_warned) {
      console.info(
        "[OhhO Pilot] Isaac Sim provider not yet bundled. Requires Fleet or Forge plan. " +
          "Launch Isaac Sim with the OmniBot USD scene and enable the Isaac ROS 2 Bridge. " +
          "Then connect via rosbridge on ws://localhost:9090.",
      );
      _warned = true;
    }
    return {
      connected: false,
      connecting: false,
      label: "Isaac Sim · Unavailable",
      latency: 0,
      fps: 0,
      error: "Isaac Sim provider requires Fleet or Forge plan. Contact sales to upgrade.",
    };
  }

  disconnect(): void {}
  getStatus(): SimulatorStatus {
    return { connected: false, connecting: false, label: "Isaac Sim · Pro tier", latency: 0, fps: 0 };
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
