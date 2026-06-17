/*
 * OhhO Pilot — Simulated Provider (free tier).
 *
 * Deterministic browser-side simulation. Always works with no external
 * dependencies. Uses the same scene presets and robot profiles as the
 * SVG camera view, but wraps them behind the SimulatorProvider interface
 * so the Pilot console doesn't know the difference between this and a
 * real Gazebo/Isaac connection.
 */

import { DEFAULT_ARM_JOINTS } from "@/lib/pilot/robots";
import {
  getScene,
  initSceneState,
  tickScene,
  type ScenePreset,
  type SceneState,
} from "@/lib/pilot/scene";
import type {
  SimulatorProvider,
  SimulatorStatus,
  SimProviderMeta,
  Velocity,
  Odometry,
  JointState,
  SimulatedFrame,
  RecordedEpisode,
  RecordingFrame,
} from "./types";
import { SCENES } from "@/lib/pilot/scene";

const META: SimProviderMeta = {
  id: "simulated",
  name: "Simulated (Built-in)",
  desc: "Deterministic browser-side simulation — always works, no external dependencies.",
  tier: "free",
  requiresServer: false,
  defaultUrl: "",
  setupHint: "Always available. No setup needed.",
  supportsRecording: true,
  available: true,
};

export class SimulatedProvider implements SimulatorProvider {
  readonly meta = META;

  private _connected = false;
  private _eStopped = false;
  private _odom: Odometry = { x: 0, y: 0, theta: 0, linearX: 0, linearY: 0, angularZ: 0, timestamp: 0 };
  private _joints: JointState[] = DEFAULT_ARM_JOINTS.map((j) => ({
    name: j.name,
    position: j.home,
    velocity: 0,
    effort: 0,
  }));
  private _sceneState: SceneState;
  private _scenePreset: ScenePreset;
  private _tickInterval: ReturnType<typeof setInterval> | null = null;
  private _fps = 0;
  private _frameCount = 0;
  private _fpsTimer = 0;
  private _latency = 0;

  // callbacks
  private _odomCbs: Array<(odom: Odometry) => void> = [];
  private _jointCbs: Array<(joints: JointState[]) => void> = [];
  private _cameraCbs: Map<string, Array<(frame: SimulatedFrame) => void>> = new Map();
  private _statusCbs: Array<(status: SimulatorStatus) => void> = [];

  // recording
  private _recording = false;
  private _recordingInstruction = "";
  private _recordingFrames: RecordingFrame[] = [];
  private _recordingStartedAt = 0;
  private _lastEpisode: RecordedEpisode | null = null;

  constructor() {
    this._scenePreset = getScene(SCENES?.[0]?.id ?? "warehouse");
    this._sceneState = initSceneState(this._scenePreset);
  }

  // ── Connection ───────────────────────────────────────────────────────────

  async connect(): Promise<SimulatorStatus> {
    this._connected = true;
    this._eStopped = false;
    this._frameCount = 0;
    this._fpsTimer = performance.now();

    this._tickInterval = setInterval(() => this._tick(), 50); // 20 Hz
    this._notifyStatus();
    return this.getStatus();
  }

  disconnect(): void {
    this._connected = false;
    if (this._tickInterval) {
      clearInterval(this._tickInterval);
      this._tickInterval = null;
    }
    this._notifyStatus();
  }

  getStatus(): SimulatorStatus {
    return {
      connected: this._connected,
      connecting: false,
      label: this._connected ? "Simulated · 20 Hz" : "Offline",
      latency: this._latency,
      fps: this._fps,
    };
  }

  // ── Control ──────────────────────────────────────────────────────────────

  sendVelocity(vel: Velocity): void {
    this._odom.linearX = vel.linearX;
    this._odom.linearY = vel.linearY;
    this._odom.angularZ = vel.angularZ;
  }

  sendJointCommand(jointName: string, position: number): void {
    const idx = this._joints.findIndex((j) => j.name === jointName);
    if (idx >= 0) {
      this._joints[idx] = { ...this._joints[idx], position };
    }
  }

  emergencyStop(): void {
    this._eStopped = true;
    this._odom.linearX = 0;
    this._odom.linearY = 0;
    this._odom.angularZ = 0;
  }

  releaseStop(): void {
    this._eStopped = false;
  }

  // ── Subscriptions ────────────────────────────────────────────────────────

  onOdometry(cb: (odom: Odometry) => void): () => void {
    this._odomCbs.push(cb);
    return () => { this._odomCbs = this._odomCbs.filter((c) => c !== cb); };
  }

  onJointStates(cb: (joints: JointState[]) => void): () => void {
    this._jointCbs.push(cb);
    return () => { this._jointCbs = this._jointCbs.filter((c) => c !== cb); };
  }

  onCameraFrame(camera: string, cb: (frame: SimulatedFrame) => void): () => void {
    const cbs = this._cameraCbs.get(camera) ?? [];
    cbs.push(cb);
    this._cameraCbs.set(camera, cbs);
    return () => {
      const updated = (this._cameraCbs.get(camera) ?? []).filter((c) => c !== cb);
      this._cameraCbs.set(camera, updated);
    };
  }

  onStatusChange(cb: (status: SimulatorStatus) => void): () => void {
    this._statusCbs.push(cb);
    return () => { this._statusCbs = this._statusCbs.filter((c) => c !== cb); };
  }

  // ── Recording ────────────────────────────────────────────────────────────

  startRecording(instruction = "manual teleop episode"): void {
    this._recording = true;
    this._recordingInstruction = instruction;
    this._recordingFrames = [];
    this._recordingStartedAt = Date.now();
  }

  stopRecording(): RecordedEpisode | null {
    if (!this._recording) return null;
    this._recording = false;

    const episode: RecordedEpisode = {
      id: `sim_${Date.now()}`,
      instruction: this._recordingInstruction,
      provider: "simulated",
      startedAt: this._recordingStartedAt,
      durationMs: Date.now() - this._recordingStartedAt,
      frameCount: this._recordingFrames.length,
      cameras: ["front", "wrist", "BEV"],
      exportContent: this._buildExportCsv(),
    };
    this._lastEpisode = episode;
    return episode;
  }

  isRecording(): boolean {
    return this._recording;
  }

  downloadLastEpisode(): void {
    if (!this._lastEpisode) return;
    const blob = new Blob([this._lastEpisode.exportContent], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${this._lastEpisode.id}_pilot_episode.csv`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  // ── Internal tick ────────────────────────────────────────────────────────

  private _tick(): void {
    if (!this._connected) return;

    // update scene (drives the simulated camera)
    this._sceneState = tickScene(this._sceneState, this._scenePreset);
    this._frameCount++;
    const now = performance.now();
    if (now - this._fpsTimer >= 2000) {
      this._fps = Math.round(this._frameCount / ((now - this._fpsTimer) / 1000));
      this._frameCount = 0;
      this._fpsTimer = now;
    }
    this._latency = Math.round(2 + Math.random() * 6); // 2-8 ms simulated

    // update odometry
    const dt = 0.05;
    this._odom.x += this._odom.linearX * dt;
    this._odom.y += this._odom.linearY * dt;
    this._odom.theta += this._odom.angularZ * dt;
    this._odom.timestamp = now;

    // notify subscribers
    const odom = { ...this._odom };
    const joints = this._joints.map((j) => ({ ...j }));

    for (const cb of this._odomCbs) cb(odom);
    for (const cb of this._jointCbs) cb(joints);

    // camera frames — generate a simulated frame per camera
    const ts = Date.now();
    for (const camera of ["front", "wrist", "BEV"]) {
      const frame: SimulatedFrame = {
        camera,
        base64: "", // no actual image in simulated mode — SVG renders directly
        timestamp: ts,
        objects: camera === "wrist"
          ? [
              { ...this._sceneState.primary },
              ...this._sceneState.secondary,
            ]
          : undefined,
      };
      const cbs = this._cameraCbs.get(camera) ?? [];
      for (const cb of cbs) cb(frame);
    }

    // recording
    if (this._recording) {
      this._recordingFrames.push({
        timestamp: ts,
        armPositions: joints.map((j) => j.position),
        baseVelocity: [odom.linearX, odom.linearY, odom.angularZ],
        cameraFrames: {},
      });
    }

    this._notifyStatus();
  }

  private _notifyStatus(): void {
    const status = this.getStatus();
    for (const cb of this._statusCbs) cb(status);
  }

  private _buildExportCsv(): string {
    const header = [
      "# OhhO Pilot — Simulated Episode",
      `# ID: ${this._lastEpisode?.id ?? "unknown"}`,
      `# Instruction: ${this._recordingInstruction}`,
      `# Frames: ${this._recordingFrames.length}`,
      `# Schema (9-DOF): frame,timestamp_ms,shoulder_pan,shoulder_lift,elbow_flex,wrist_flex,wrist_roll,gripper,base_vx,base_vy,base_omega`,
      "",
      "frame,timestamp_ms,shoulder_pan,shoulder_lift,elbow_flex,wrist_flex,wrist_roll,gripper,base_vx,base_vy,base_omega",
    ];
    for (let i = 0; i < this._recordingFrames.length; i++) {
      const f = this._recordingFrames[i];
      const row = [
        i,
        f.timestamp,
        ...f.armPositions.slice(0, 6).map((v) => v.toFixed(4)),
        ...f.baseVelocity.slice(0, 3).map((v) => v.toFixed(4)),
      ];
      header.push(row.join(","));
    }
    return header.join("\n");
  }
}
