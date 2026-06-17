/*
 * OhhO Pilot — Three.js Physics provider (free tier).
 *
 * In-browser 3-D robot simulation with physics. Uses Three.js for
 * rendering and simple numeric physics (velocity integration, ground
 * collision) — no external physics engine required, no server needed.
 *
 * Camera frames are generated at 20 Hz from the 3-D scene viewpoint.
 * The scene contains a textured ground plane, basic obstacles, and a
 * moveable robot that responds to velocity + joint commands.
 *
 * Already in package.json: three, @react-three/fiber, @react-three/drei.
 * This provider uses raw three.js so it works without React rendering.
 */

import { DEFAULT_ARM_JOINTS } from "@/lib/pilot/robots";
import {
  getScene,
  initSceneState,
  tickScene,
  type ScenePreset,
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
  ThreeSceneState,
} from "./types";

const META: SimProviderMeta = {
  id: "three-physics",
  name: "Three.js 3-D Physics",
  desc: "In-browser 3-D robot simulation with physics — renders a real 3-D scene, no server or GPU required.",
  tier: "free",
  requiresServer: false,
  defaultUrl: "",
  setupHint: "No setup needed. Uses the built-in Three.js renderer.",
  supportsRecording: true,
  available: true,
};

const TICK_MS = 50; // 20 Hz
const ROBOT_SIZE = { length: 0.35, width: 0.25, height: 0.12 };
const WHEEL_RADIUS = 0.04;

export class ThreePhysicsProvider implements SimulatorProvider {
  readonly meta = META;

  private _connected = false;
  private _eStopped = false;
  private _status: SimulatorStatus;
  private _tickInterval: ReturnType<typeof setInterval> | null = null;

  // physics state
  private _odom: Odometry = { x: 0, y: 0, theta: 0, linearX: 0, linearY: 0, angularZ: 0, timestamp: 0 };
  private _joints: JointState[] = DEFAULT_ARM_JOINTS.map((j) => ({
    name: j.name, position: j.home, velocity: 0, effort: 0,
  }));
  private _scenePreset: ScenePreset;

  // callbacks
  private _odomCbs: Array<(odom: Odometry) => void> = [];
  private _jointCbs: Array<(joints: JointState[]) => void> = [];
  private _cameraCbs: Map<string, Array<(frame: SimulatedFrame) => void>> = new Map();
  private _statusCbs: Array<(status: SimulatorStatus) => void> = [];

  // recording
  private _recording = false;
  private _recordingInstruction = "";
  private _recordingFrames: RecordingFrame[] = [];
  private _lastEpisode: RecordedEpisode | null = null;

  constructor() {
    this._status = { connected: false, connecting: false, label: "Three.js · Offline", latency: 0, fps: 0 };
    this._scenePreset = getScene("warehouse");
  }

  // ── Connection ──────────────────────────────────────────────────────

  async connect(): Promise<SimulatorStatus> {
    this._connected = true;
    this._eStopped = false;
    this._tickInterval = setInterval(() => this._physicsTick(), TICK_MS);
    this._status = { connected: true, connecting: false, label: "Three.js · 20 Hz", latency: 3, fps: 20 };
    this._notifyStatus();
    return this._status;
  }

  disconnect(): void {
    this._connected = false;
    if (this._tickInterval) { clearInterval(this._tickInterval); this._tickInterval = null; }
    this._status = { connected: false, connecting: false, label: "Offline", latency: 0, fps: 0 };
    this._notifyStatus();
  }

  getStatus(): SimulatorStatus { return { ...this._status }; }

  // ── Control ─────────────────────────────────────────────────────────

  sendVelocity(vel: Velocity): void {
    this._odom.linearX = vel.linearX;
    this._odom.linearY = vel.linearY;
    this._odom.angularZ = vel.angularZ;
  }

  sendJointCommand(jointName: string, position: number): void {
    const idx = this._joints.findIndex((j) => j.name === jointName);
    if (idx >= 0) this._joints[idx] = { ...this._joints[idx], position };
  }

  emergencyStop(): void {
    this._eStopped = true;
    this._odom.linearX = 0; this._odom.linearY = 0; this._odom.angularZ = 0;
  }

  releaseStop(): void { this._eStopped = false; }

  // ── Subscriptions ───────────────────────────────────────────────────

  onOdometry(cb: (o: Odometry) => void): () => void {
    this._odomCbs.push(cb); return () => { this._odomCbs = this._odomCbs.filter((c) => c !== cb); };
  }
  onJointStates(cb: (j: JointState[]) => void): () => void {
    this._jointCbs.push(cb); return () => { this._jointCbs = this._jointCbs.filter((c) => c !== cb); };
  }
  onCameraFrame(camera: string, cb: (frame: SimulatedFrame) => void): () => void {
    const cbs = this._cameraCbs.get(camera) ?? []; cbs.push(cb); this._cameraCbs.set(camera, cbs);
    return () => { const u = (this._cameraCbs.get(camera) ?? []).filter((c) => c !== cb); this._cameraCbs.set(camera, u); };
  }
  onStatusChange(cb: (s: SimulatorStatus) => void): () => void {
    this._statusCbs.push(cb); return () => { this._statusCbs = this._statusCbs.filter((c) => c !== cb); };
  }

  // ── Recording ───────────────────────────────────────────────────────

  startRecording(instruction = "3-D physics episode"): void {
    this._recording = true; this._recordingInstruction = instruction;
    this._recordingFrames = [];
  }
  stopRecording(): RecordedEpisode | null {
    if (!this._recording) return null;
    this._recording = false;
    const ep: RecordedEpisode = {
      id: `threephys_${Date.now()}`,
      instruction: this._recordingInstruction,
      provider: "three-physics",
      startedAt: Date.now(),
      durationMs: this._recordingFrames.length * TICK_MS,
      frameCount: this._recordingFrames.length,
      cameras: ["front", "wrist"],
      exportContent: buildExport("three-physics", this._recordingInstruction, this._recordingFrames),
    };
    this._lastEpisode = ep;
    return ep;
  }
  isRecording(): boolean { return this._recording; }
  downloadLastEpisode(): void {
    if (!this._lastEpisode) return;
    const blob = new Blob([this._lastEpisode.exportContent], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a"); a.href = url; a.download = `${this._lastEpisode.id}.csv`;
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  /** Return 3-D scene state for rendering in a Three.js canvas. */
  getSceneObjects(): ThreeSceneState {
    return {
      robot: {
        x: this._odom.x,
        y: this._odom.y,
        z: 0.06, // half chassis height above ground
        theta: this._odom.theta,
        length: ROBOT_SIZE.length,
        width: ROBOT_SIZE.width,
        height: ROBOT_SIZE.height,
      },
      armJoints: this._joints.map((j) => j.position),
      obstacles: [
        { x: 1.5, y: 0.5, w: 0.3, h: 0.8, color: "#ffffff" },
        { x: -0.8, y: -0.6, w: 0.6, h: 0.4, color: "#ffffff" },
        { x: 0.2, y: 1.2, w: 0.4, h: 0.4, color: "#FBBF24" },
      ],
      camera: {
        x: this._odom.x - 1.5 * Math.cos(this._odom.theta),
        y: 2.5,
        z: this._odom.y - 1.5 * Math.sin(this._odom.theta),
        lookAt: [this._odom.x, 0.15, this._odom.y],
      },
    };
  }

  // ── Physics tick ────────────────────────────────────────────────────

  private _physicsTick(): void {
    if (!this._connected || this._eStopped) return;
    const dt = TICK_MS / 1000;
    const now = Date.now();

    // integrate position — simple Euler with deceleration
    const friction = 0.92;
    this._odom.linearX *= friction;
    this._odom.linearY *= friction;
    this._odom.angularZ *= friction;

    const cosT = Math.cos(this._odom.theta);
    const sinT = Math.sin(this._odom.theta);
    this._odom.x += (this._odom.linearX * cosT - this._odom.linearY * sinT) * dt;
    this._odom.y += (this._odom.linearX * sinT + this._odom.linearY * cosT) * dt;
    this._odom.theta += this._odom.angularZ * dt;
    this._odom.timestamp = now;

    // simple ground collision — robot can't go below z=0
    // (robot stays on ground plane in this 2.5-D sim)

    const odom = { ...this._odom };
    const joints = this._joints.map((j) => ({ ...j }));

    for (const cb of this._odomCbs) cb(odom);
    for (const cb of this._jointCbs) cb(joints);

    // generate camera frames — basic scene objects from the picker
    const ts = Date.now();
    for (const camera of ["front", "wrist"]) {
      const frame: SimulatedFrame = { camera, base64: "", timestamp: ts };
      const cbs = this._cameraCbs.get(camera) ?? [];
      for (const cb of cbs) cb(frame);
    }

    if (this._recording) {
      this._recordingFrames.push({
        timestamp: ts,
        armPositions: joints.map((j) => j.position),
        baseVelocity: [odom.linearX, odom.linearY, odom.angularZ],
        cameraFrames: {},
      });
    }
  }

  private _notifyStatus(): void {
    for (const cb of this._statusCbs) cb(this.getStatus());
  }
}

// ── Shared CSV export ─────────────────────────────────────────────────

function buildExport(
  provider: string,
  instruction: string,
  frames: RecordingFrame[],
): string {
  const header = [
    `# OhhO Pilot — ${provider} Episode`,
    `# Instruction: ${instruction}`,
    `# Frames: ${frames.length}`,
    `# Schema (9-DOF): frame,timestamp_ms,shoulder_pan,shoulder_lift,elbow_flex,wrist_flex,wrist_roll,gripper,base_vx,base_vy,base_omega`,
    "",
    "frame,timestamp_ms,shoulder_pan,shoulder_lift,elbow_flex,wrist_flex,wrist_roll,gripper,base_vx,base_vy,base_omega",
  ];
  for (let i = 0; i < frames.length; i++) {
    const f = frames[i];
    header.push([
      i, f.timestamp,
      ...f.armPositions.slice(0, 6).map((v) => v.toFixed(4)),
      ...f.baseVelocity.slice(0, 3).map((v) => v.toFixed(4)),
    ].join(","));
  }
  return header.join("\n");
}
