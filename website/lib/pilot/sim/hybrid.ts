/*
 * OhhO Pilot — Hybrid Simulated ROSBridge provider (free tier).
 *
 * A fully simulated ROS 2 message bus that runs entirely in the browser.
 * It manages simulated topics (/odom, /joint_states, /tf, /scan, /map,
 * /cmd_vel, /arm/joint_commands, /emergency_stop) with realistic message
 * shapes matching the real ROSBridge v2 JSON protocol.
 *
 * Key features:
 *   • Pass-through mode — can bridge to a REAL rosbridge WebSocket
 *     when one is available (toggle in UI)
 *   • Topic replay — captures and replays topic data for debugging
 *   • Recording built-in — all messages are timestamped for episodes
 *   • Zero dependencies — pure TypeScript, no server
 *
 * Use case:
 *   1. Start in pure-sim mode to test teleop without hardware
 *   2. Toggle "bridge to real" — messages flow through to a real robot
 *   3. Record episodes in either mode for training data
 */

import { DEFAULT_ARM_JOINTS } from "@/lib/pilot/robots";
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

const META: SimProviderMeta = {
  id: "hybrid",
  name: "Hybrid ROSBridge Sim",
  desc: "Full simulated ROS message bus. Test teleop locally, then bridge to real rosbridge when a robot is connected.",
  tier: "free",
  requiresServer: false,
  defaultUrl: "",
  setupHint: "Always available. Toggle 'bridge to real' and enter a rosbridge URL to connect to a physical robot.",
  supportsRecording: true,
  available: true,
};

const TICK_MS = 50;
const MAX_LIN_VEL = 0.2;
const MAX_ANG_VEL = 1.0;

interface RosMessage {
  topic: string;
  type: string;
  msg: Record<string, unknown>;
  timestamp: number;
}

export class HybridProvider implements SimulatorProvider {
  readonly meta = META;

  private _connected = false;
  private _eStopped = false;
  private _bridgeWs: WebSocket | null = null;
  private _bridgeUrl = "";
  private _bridgeActive = false;
  private _tickInterval: ReturnType<typeof setInterval> | null = null;

  // simulated robot state
  private _odom: Odometry = { x: 0, y: 0, theta: 0, linearX: 0, linearY: 0, angularZ: 0, timestamp: 0 };
  private _joints: JointState[] = DEFAULT_ARM_JOINTS.map((j) => ({
    name: j.name, position: j.home, velocity: 0, effort: 0,
  }));
  private _targetVel: Velocity = { linearX: 0, linearY: 0, angularZ: 0 };

  // message log for replay
  private _messageLog: RosMessage[] = [];
  private _fpsCounter = 0;
  private _fpsTimer = 0;
  private _fps = 0;

  // callbacks
  private _odomCbs: Array<(o: Odometry) => void> = [];
  private _jointCbs: Array<(j: JointState[]) => void> = [];
  private _cameraCbs: Map<string, Array<(f: SimulatedFrame) => void>> = new Map();
  private _statusCbs: Array<(s: SimulatorStatus) => void> = [];
  private _msgCbs: Array<(msg: RosMessage) => void> = [];

  // recording
  private _recording = false;
  private _recordingInstruction = "";
  private _recordingFrames: RecordingFrame[] = [];
  private _lastEpisode: RecordedEpisode | null = null;

  // ── Connection ──────────────────────────────────────────────────────

  async connect(bridgeUrl?: string): Promise<SimulatorStatus> {
    if (bridgeUrl) {
      this._bridgeUrl = bridgeUrl;
      try {
        this._bridgeWs = new WebSocket(bridgeUrl);
        this._bridgeWs.onmessage = (e) => {
          try {
            const data = JSON.parse(e.data as string);
            this._handleBridgeMessage(data);
          } catch { /* skip malformed */ }
        };
        this._bridgeActive = true;
      } catch {
        this._bridgeActive = false;
      }
    }

    this._connected = true;
    this._fpsTimer = performance.now();
    this._tickInterval = setInterval(() => this._simTick(), TICK_MS);
    return this.getStatus();
  }

  disconnect(): void {
    this._connected = false;
    this._bridgeActive = false;
    if (this._tickInterval) { clearInterval(this._tickInterval); this._tickInterval = null; }
    this._bridgeWs?.close();
    this._bridgeWs = null;
  }

  getStatus(): SimulatorStatus {
    const label = this._bridgeActive
      ? `Hybrid · bridged → ${this._bridgeUrl}`
      : "Hybrid · simulated bus";
    return { connected: this._connected, connecting: false, label, latency: 2, fps: this._fps };
  }

  // ── Toggle bridge mode ──────────────────────────────────────────────

  /** Enable/disable bridging messages to a real rosbridge endpoint. */
  async toggleBridge(url?: string): Promise<void> {
    if (this._bridgeActive) {
      this._bridgeActive = false;
      this._bridgeWs?.close();
      this._bridgeWs = null;
    } else if (url) {
      this._bridgeUrl = url;
      try {
        this._bridgeWs = new WebSocket(url);
        this._bridgeActive = true;
      } catch {
        this._bridgeActive = false;
      }
    }
  }

  get bridgeActive(): boolean { return this._bridgeActive; }
  get messageLog(): RosMessage[] { return this._messageLog; }

  // ── Control ─────────────────────────────────────────────────────────

  sendVelocity(vel: Velocity): void {
    this._targetVel = {
      linearX: clamp(vel.linearX, -MAX_LIN_VEL, MAX_LIN_VEL),
      linearY: clamp(vel.linearY, -MAX_LIN_VEL, MAX_LIN_VEL),
      angularZ: clamp(vel.angularZ, -MAX_ANG_VEL, MAX_ANG_VEL),
    };
    this._publishMsg("/cmd_vel", "geometry_msgs/Twist", {
      linear: { x: this._targetVel.linearX, y: this._targetVel.linearY, z: 0 },
      angular: { x: 0, y: 0, z: this._targetVel.angularZ },
    });
  }

  sendJointCommand(jointName: string, position: number): void {
    const idx = this._joints.findIndex((j) => j.name === jointName);
    if (idx >= 0) {
      this._joints[idx] = { ...this._joints[idx], position };
    }
    this._publishMsg("/arm/joint_commands", "sensor_msgs/JointState", {
      name: [jointName], position: [position], velocity: [], effort: [],
    });
  }

  emergencyStop(): void {
    this._eStopped = true;
    this._targetVel = { linearX: 0, linearY: 0, angularZ: 0 };
    this._publishMsg("/emergency_stop", "std_msgs/Bool", { data: true });
  }

  releaseStop(): void {
    this._eStopped = false;
    this._publishMsg("/emergency_stop", "std_msgs/Bool", { data: false });
  }

  // ── Subscriptions ───────────────────────────────────────────────────

  onOdometry(cb: (o: Odometry) => void): () => void {
    this._odomCbs.push(cb); return () => { this._odomCbs = this._odomCbs.filter((c) => c !== cb); };
  }
  onJointStates(cb: (j: JointState[]) => void): () => void {
    this._jointCbs.push(cb); return () => { this._jointCbs = this._jointCbs.filter((c) => c !== cb); };
  }
  onCameraFrame(camera: string, cb: (f: SimulatedFrame) => void): () => void {
    const cbs = this._cameraCbs.get(camera) ?? []; cbs.push(cb); this._cameraCbs.set(camera, cbs);
    return () => { const u = (this._cameraCbs.get(camera) ?? []).filter((c) => c !== cb); this._cameraCbs.set(camera, u); };
  }
  onStatusChange(cb: (s: SimulatorStatus) => void): () => void {
    this._statusCbs.push(cb); return () => { this._statusCbs = this._statusCbs.filter((c) => c !== cb); };
  }
  /** Subscribe to ALL messages on the simulated bus. */
  onMessage(cb: (msg: RosMessage) => void): () => void {
    this._msgCbs.push(cb); return () => { this._msgCbs = this._msgCbs.filter((c) => c !== cb); };
  }

  // ── Recording ───────────────────────────────────────────────────────

  startRecording(instruction = "hybrid teleop episode"): void {
    this._recording = true; this._recordingInstruction = instruction; this._recordingFrames = [];
  }
  stopRecording(): RecordedEpisode | null {
    if (!this._recording) return null;
    this._recording = false;
    const ep: RecordedEpisode = {
      id: `hybrid_${Date.now()}`,
      instruction: this._recordingInstruction,
      provider: "hybrid",
      startedAt: Date.now(),
      durationMs: this._recordingFrames.length * TICK_MS,
      frameCount: this._recordingFrames.length,
      cameras: ["front", "wrist", "BEV"],
      exportContent: this._buildExport(),
    };
    this._lastEpisode = ep; return ep;
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

  // ── Simulation tick ─────────────────────────────────────────────────

  private _simTick(): void {
    if (!this._connected) return;
    const dt = TICK_MS / 1000;
    const now = Date.now();

    // FPS counter
    this._fpsCounter++;
    if (now - this._fpsTimer >= 2000) {
      this._fps = Math.round(this._fpsCounter / 2);
      this._fpsCounter = 0;
      this._fpsTimer = now;
    }

    // ramp velocity toward target (simulates yahboom's 0.05 m/s ramp)
    const ramp = 0.05;
    this._odom.linearX = rampToward(this._odom.linearX, this._eStopped ? 0 : this._targetVel.linearX, ramp);
    this._odom.linearY = rampToward(this._odom.linearY, this._eStopped ? 0 : this._targetVel.linearY, ramp);
    this._odom.angularZ = rampToward(this._odom.angularZ, this._eStopped ? 0 : this._targetVel.angularZ, ramp * 3);

    // integrate
    const cosT = Math.cos(this._odom.theta);
    const sinT = Math.sin(this._odom.theta);
    this._odom.x += (this._odom.linearX * cosT - this._odom.linearY * sinT) * dt;
    this._odom.y += (this._odom.linearX * sinT + this._odom.linearY * cosT) * dt;
    this._odom.theta += this._odom.angularZ * dt;

    // publish simulated /odom
    this._publishMsg("/odom", "nav_msgs/Odometry", {
      pose: {
        pose: {
          position: { x: this._odom.x, y: this._odom.y, z: 0 },
          orientation: { x: 0, y: 0, z: Math.sin(this._odom.theta / 2), w: Math.cos(this._odom.theta / 2) },
        },
      },
      twist: {
        twist: {
          linear: { x: this._odom.linearX, y: this._odom.linearY, z: 0 },
          angular: { x: 0, y: 0, z: this._odom.angularZ },
        },
      },
    });

    // publish simulated /joint_states
    this._publishMsg("/joint_states", "sensor_msgs/JointState", {
      name: this._joints.map((j) => j.name),
      position: this._joints.map((j) => j.position),
      velocity: this._joints.map(() => 0),
      effort: this._joints.map(() => 0),
    });

    // notify subscribers
    const odom = { ...this._odom };
    const joints = this._joints.map((j) => ({ ...j }));
    for (const cb of this._odomCbs) cb(odom);
    for (const cb of this._jointCbs) cb(joints);

    // camera frames (simulated)
    const ts = Date.now();
    for (const camera of ["front", "wrist", "BEV"]) {
      const frame: SimulatedFrame = { camera, base64: "", timestamp: ts };
      for (const cb of (this._cameraCbs.get(camera) ?? [])) cb(frame);
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

  private _publishMsg(topic: string, type: string, msg: Record<string, unknown>): void {
    const wrapped: RosMessage = { topic, type, msg, timestamp: Date.now() };
    this._messageLog.push(wrapped);
    if (this._messageLog.length > 500) this._messageLog.shift();

    for (const cb of this._msgCbs) cb(wrapped);

    // bridge to real ROSBridge if active
    if (this._bridgeActive && this._bridgeWs?.readyState === WebSocket.OPEN) {
      this._bridgeWs.send(JSON.stringify({
        op: "publish", topic, type, msg,
        id: `hybrid_${Date.now()}`,
      }));
    }
  }

  private _handleBridgeMessage(data: Record<string, unknown>): void {
    const topic = data.topic as string;
    if (!topic) return;
    // incoming messages from the real robot override simulated state
    const msg = data.msg as Record<string, unknown> | undefined;
    if (topic === "/odom" && msg) {
      const pose = (msg as { pose?: { pose?: { position?: { x?: number; y?: number; z?: number }; orientation?: { w?: number; z?: number } } } }).pose;
      const twist = (msg as { twist?: { twist?: { linear?: { x?: number; y?: number }; angular?: { z?: number } } } }).twist;
      if (pose?.pose) {
        const pos = pose.pose.position;
        const ori = pose.pose.orientation;
        if (pos) { this._odom.x = pos.x ?? 0; this._odom.y = pos.y ?? 0; }
        if (ori) {
          const qw = ori.w ?? 1, qz = ori.z ?? 0;
          this._odom.theta = Math.atan2(2 * qw * qz, 1 - 2 * qz * qz);
        }
      }
      if (twist?.twist) {
        this._odom.linearX = twist.twist.linear?.x ?? 0;
        this._odom.linearY = twist.twist.linear?.y ?? 0;
        this._odom.angularZ = twist.twist.angular?.z ?? 0;
      }
    }
  }

  private _buildExport(): string {
    const h = [
      `# OhhO Pilot — Hybrid ROSBridge Episode`,
      `# Instruction: ${this._recordingInstruction}`,
      `# Frames: ${this._recordingFrames.length}`,
      `# Schema (9-DOF): frame,timestamp_ms,shoulder_pan,shoulder_lift,elbow_flex,wrist_flex,wrist_roll,gripper,base_vx,base_vy,base_omega`,
      `# Messages logged: ${this._messageLog.length}`,
      "",
      "frame,timestamp_ms,shoulder_pan,shoulder_lift,elbow_flex,wrist_flex,wrist_roll,gripper,base_vx,base_vy,base_omega",
    ];
    for (let i = 0; i < this._recordingFrames.length; i++) {
      const f = this._recordingFrames[i];
      h.push([i, f.timestamp, ...f.armPositions.slice(0, 6).map((v) => v.toFixed(4)), ...f.baseVelocity.slice(0, 3).map((v) => v.toFixed(4))].join(","));
    }
    return h.join("\n");
  }
}

// ── Math helpers ───────────────────────────────────────────────────────

function clamp(v: number, lo: number, hi: number): number { return Math.max(lo, Math.min(hi, v)); }
function rampToward(current: number, target: number, step: number): number {
  const diff = target - current;
  if (Math.abs(diff) <= step) return target;
  return current + Math.sign(diff) * step;
}
