/*
 * OhhO Pilot — Gazebo Harmonic provider (free tier).
 *
 * Connects to a running Gazebo simulation via ROSBridge WebSocket (port 9090).
 * This is the same protocol the Android app uses (rosbridge v2 JSON).
 *
 * Topics subscribed:
 *   /odom            → odometry
 *   /joint_states    → arm + wheel joints
 *   /camera/*        → compressed camera frames
 *
 * Topics published:
 *   /cmd_vel         → velocity commands
 *   /arm/joint_commands → joint position targets
 *   /emergency_stop  → e-stop
 */

import type { RobotProfile } from "@/lib/pilot/robots";
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
  id: "gazebo",
  name: "Gazebo Harmonic",
  desc: "Open-source 3-D robot simulator with physics. Connect via ROSBridge on port 9090.",
  tier: "free",
  requiresServer: true,
  defaultUrl: "ws://localhost:9090",
  setupHint: "Launch: ./launch_simulation.sh. Then: ./launch_rosbridge.sh",
  supportsRecording: true,
  available: true,
};

interface RosbridgeMessage {
  op: "publish" | "subscribe" | "unsubscribe" | "call_service";
  topic?: string;
  type?: string;
  msg?: Record<string, unknown>;
  id?: string;
  service?: string;
  args?: Record<string, unknown>;
}

export class GazeboProvider implements SimulatorProvider {
  readonly meta = META;

  private _ws: WebSocket | null = null;
  private _url = META.defaultUrl;
  private _status: SimulatorStatus = {
    connected: false,
    connecting: false,
    label: "Offline",
    latency: 0,
    fps: 0,
  };
  private _profile: RobotProfile | null = null;
  private _reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private _shouldReconnect = false;
  private _msgId = 0;

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

  connect(url?: string): Promise<SimulatorStatus> {
    if (url) this._url = url;

    return new Promise((resolve) => {
      this._status = { ...this._status, connecting: true, label: "Connecting to Gazebo…" };
      this._notifyStatus();

      try {
        this._ws = new WebSocket(this._url);
        this._shouldReconnect = true;

        this._ws.onopen = () => {
          this._status = {
            connected: true,
            connecting: false,
            label: "Gazebo · ROSBridge",
            latency: 0,
            fps: 0,
            error: undefined,
          };
          this._subscribeTopics();
          this._notifyStatus();
          resolve(this._status);
        };

        this._ws.onmessage = (event) => {
          try {
            const data = JSON.parse(event.data as string);
            this._handleMessage(data);
          } catch {
            /* malformed message — skip */
          }
        };

        this._ws.onerror = () => {
          this._status = {
            ...this._status,
            connecting: false,
            error: "WebSocket error — is rosbridge running?",
          };
          this._notifyStatus();
        };

        this._ws.onclose = () => {
          const wasConnected = this._status.connected;
          this._status = {
            connected: false,
            connecting: false,
            label: "Gazebo · Disconnected",
            latency: 0,
            fps: 0,
          };
          this._notifyStatus();

          // auto-reconnect
          if (wasConnected && this._shouldReconnect) {
            this._reconnectTimer = setTimeout(() => {
              if (this._shouldReconnect) this.connect(this._url);
            }, 3000);
          }

          if (!wasConnected && this._status.connecting) {
            this._status.error = `Unable to reach Gazebo at ${this._url}. Start rosbridge: ./launch_rosbridge.sh`;
            resolve(this._status);
          }
        };

        // timeout if no response in 5 seconds
        setTimeout(() => {
          if (this._status.connecting) {
            this._status = {
              connected: false,
              connecting: false,
              label: "Gazebo · Timed out",
              latency: 0,
              fps: 0,
              error: `Connection timed out. Is rosbridge running on ${this._url}?`,
            };
            this._ws?.close();
            this._notifyStatus();
            resolve(this._status);
          }
        }, 5000);

      } catch (err) {
        this._status = {
          connected: false,
          connecting: false,
          label: "Gazebo · Error",
          latency: 0,
          fps: 0,
          error: `WebSocket error: ${err instanceof Error ? err.message : String(err)}`,
        };
        this._notifyStatus();
        resolve(this._status);
      }
    });
  }

  disconnect(): void {
    this._shouldReconnect = false;
    if (this._reconnectTimer) clearTimeout(this._reconnectTimer);
    this._ws?.close();
    this._ws = null;
  }

  getStatus(): SimulatorStatus {
    return { ...this._status };
  }

  sendVelocity(vel: Velocity): void {
    this._publish("/cmd_vel", "geometry_msgs/Twist", {
      linear: { x: vel.linearX, y: vel.linearY, z: 0 },
      angular: { x: 0, y: 0, z: vel.angularZ },
    });
  }

  sendJointCommand(jointName: string, position: number): void {
    this._publish("/arm/joint_commands", "sensor_msgs/JointState", {
      name: [jointName],
      position: [position],
      velocity: [],
      effort: [],
    });
  }

  emergencyStop(): void {
    this._publish("/emergency_stop", "std_msgs/Bool", { data: true });
  }

  releaseStop(): void {
    this._publish("/emergency_stop", "std_msgs/Bool", { data: false });
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

  startRecording(instruction = "gazebo teleop episode"): void {
    this._recording = true;
    this._recordingInstruction = instruction;
    this._recordingFrames = [];
    this._recordingStartedAt = Date.now();
  }

  stopRecording(): RecordedEpisode | null {
    if (!this._recording) return null;
    this._recording = false;
    const episode: RecordedEpisode = {
      id: `gazebo_${Date.now()}`,
      instruction: this._recordingInstruction,
      provider: "gazebo",
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
    a.download = `${this._lastEpisode.id}_gazebo_episode.csv`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  // ── Private ──────────────────────────────────────────────────────────────

  private _subscribeTopics(): void {
    this._subscribe("/odom", "nav_msgs/Odometry");
    this._subscribe("/joint_states", "sensor_msgs/JointState");
    this._subscribe("/tf", "tf2_msgs/TFMessage");
  }

  private _subscribe(topic: string, type: string): void {
    this._send({ op: "subscribe", topic, type });
  }

  private _publish(topic: string, type: string, msg: Record<string, unknown>): void {
    this._send({ op: "publish", topic, type, msg });
  }

  private _send(msg: RosbridgeMessage): void {
    if (!this._ws || this._ws.readyState !== WebSocket.OPEN) return;
    msg.id = `pilot_${++this._msgId}`;
    this._ws.send(JSON.stringify(msg));
  }

  private _handleMessage(data: Record<string, unknown>): void {
    const topic = data.topic as string | undefined;
    const msg = data.msg as Record<string, unknown> | undefined;
    if (!topic || !msg) return;

    const now = Date.now();

    if (topic === "/odom") {
      const pose = msg.pose as Record<string, unknown> | undefined;
      const twist = msg.twist as Record<string, unknown> | undefined;
      const pos = (pose?.pose as Record<string, unknown>)?.position as Record<string, number> | undefined;
      const ori = (pose?.pose as Record<string, unknown>)?.orientation as Record<string, number> | undefined;
      const lin = (twist?.twist as Record<string, unknown>)?.linear as Record<string, number> | undefined;
      const ang = (twist?.twist as Record<string, unknown>)?.angular as Record<string, number> | undefined;

      // simple yaw from quaternion
      const qw = ori?.w ?? 1, qz = ori?.z ?? 0;
      const theta = Math.atan2(2 * qw * qz, 1 - 2 * qz * qz);

      const odom: Odometry = {
        x: pos?.x ?? 0,
        y: pos?.y ?? 0,
        theta,
        linearX: lin?.x ?? 0,
        linearY: lin?.y ?? 0,
        angularZ: ang?.z ?? 0,
        timestamp: now,
      };
      for (const cb of this._odomCbs) cb(odom);

      if (this._recording) {
        // append to latest frame or create new
        const frame = this._recordingFrames[this._recordingFrames.length - 1];
        if (frame) {
          frame.baseVelocity = [odom.linearX, odom.linearY, odom.angularZ];
        }
      }
    }

    if (topic === "/joint_states") {
      const names = msg.name as string[] | undefined;
      const positions = msg.position as number[] | undefined;
      if (names && positions) {
        const joints: JointState[] = names.map((name, i) => ({
          name,
          position: positions[i] ?? 0,
          velocity: (msg.velocity as number[])?.[i] ?? 0,
          effort: (msg.effort as number[])?.[i] ?? 0,
        }));
        for (const cb of this._jointCbs) cb(joints);

        if (this._recording) {
          const frame = this._recordingFrames[this._recordingFrames.length - 1];
          if (frame) {
            frame.armPositions = joints.slice(0, 6).map((j) => j.position);
          } else {
            this._recordingFrames.push({
              timestamp: now,
              armPositions: joints.slice(0, 6).map((j) => j.position),
              baseVelocity: [0, 0, 0],
              cameraFrames: {},
            });
          }
        }
      }
    }
  }

  private _buildExportCsv(): string {
    const header = [
      "# OhhO Pilot — Gazebo Episode",
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

  private _notifyStatus(): void {
    const status = this.getStatus();
    for (const cb of this._statusCbs) cb(status);
  }
}
