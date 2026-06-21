/*
 * ROSBridge transport (Wi-Fi / Ethernet) — the primary, recommended link.
 *
 * Speaks the rosbridge v2 JSON protocol over a WebSocket (the same server the
 * Android app uses, default port 9090). Robot-agnostic: topic names come from
 * the robot's RobotConfig, so a drone, an arm and OmniBot all work unchanged.
 *
 *   subscribes : <odom>, <jointStates>, /battery_state   (per RobotConfig)
 *   publishes  : <cmdVel> (Twist), <jointCommands> (JointState), /emergency_stop
 *   latency    : measured via the /rosapi/get_time service round-trip
 */

import type { RobotConfig } from "@/lib/garage/robot-config";
import type {
  ConnectionConfig,
  JointReading,
  Odometry,
  RobotTelemetry,
  RobotTransport,
  TransportStatus,
  Velocity,
} from "./types";

interface RosbridgeMessage {
  op: "publish" | "subscribe" | "unsubscribe" | "call_service";
  topic?: string;
  type?: string;
  msg?: Record<string, unknown>;
  id?: string;
  service?: string;
  args?: Record<string, unknown>;
}

/** Where joint position commands go, derived from the state topic. */
function jointCommandTopic(robot: RobotConfig): string {
  const states = robot.rosTopics.jointStates ?? "/joint_states";
  return states.includes("/arm/") ? "/arm/joint_commands" : "/joint_commands";
}

export class RosbridgeTransport implements RobotTransport {
  readonly protocol = "rosbridge" as const;

  private _ws: WebSocket | null = null;
  private _robot: RobotConfig;
  private _url: string;
  private _autoReconnect: boolean;
  private _shouldReconnect = false;
  private _reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private _pingTimer: ReturnType<typeof setInterval> | null = null;
  private _rateTimer: ReturnType<typeof setInterval> | null = null;
  private _msgId = 0;
  private _msgCount = 0;
  private _pendingPings = new Map<string, number>();

  private _status: TransportStatus;
  private _statusCbs: Array<(s: TransportStatus) => void> = [];
  private _telemetryCbs: Array<(t: RobotTelemetry) => void> = [];

  constructor(robot: RobotConfig, cfg: ConnectionConfig) {
    this._robot = robot;
    this._url = cfg.address ?? "ws://192.168.1.100:9090";
    this._autoReconnect = cfg.autoReconnect ?? true;
    this._status = {
      protocol: "rosbridge",
      state: "idle",
      label: "Offline",
      latencyMs: 0,
      msgRate: 0,
    };
  }

  connect(): Promise<TransportStatus> {
    return new Promise((resolve) => {
      this._set({ state: "connecting", label: `Connecting to ${this._host()}…`, error: undefined });

      let settled = false;
      const finish = () => {
        if (!settled) {
          settled = true;
          resolve(this.getStatus());
        }
      };

      try {
        this._ws = new WebSocket(this._url);
        this._shouldReconnect = this._autoReconnect;

        this._ws.onopen = () => {
          this._set({
            state: "connected",
            label: `ROSBridge · ${this._host()}`,
            connectedSince: Date.now(),
            error: undefined,
          });
          this._subscribeTopics();
          this._startTimers();
          finish();
        };

        this._ws.onmessage = (event) => {
          this._msgCount++;
          try {
            this._handleMessage(JSON.parse(event.data as string));
          } catch {
            /* malformed frame — ignore */
          }
        };

        this._ws.onerror = () => {
          this._set({ state: "error", error: "WebSocket error — is rosbridge running and reachable?" });
        };

        this._ws.onclose = () => {
          const wasConnected = this._status.state === "connected";
          this._stopTimers();
          if (wasConnected && this._shouldReconnect) {
            this._set({ state: "reconnecting", label: "Reconnecting…" });
            this._reconnectTimer = setTimeout(() => {
              if (this._shouldReconnect) this.connect();
            }, 3000);
          } else {
            this._set({ state: "disconnected", label: "Disconnected", latencyMs: 0, msgRate: 0 });
          }
          finish();
        };

        // connection timeout
        setTimeout(() => {
          if (this._status.state === "connecting") {
            this._set({
              state: "error",
              label: "Timed out",
              error: `Connection timed out. Is rosbridge running at ${this._url}?`,
            });
            this._ws?.close();
            finish();
          }
        }, 6000);
      } catch (err) {
        this._set({
          state: "error",
          label: "Error",
          error: `Could not open socket: ${err instanceof Error ? err.message : String(err)}`,
        });
        finish();
      }
    });
  }

  disconnect(): void {
    this._shouldReconnect = false;
    if (this._reconnectTimer) clearTimeout(this._reconnectTimer);
    this._stopTimers();
    this._ws?.close();
    this._ws = null;
    this._set({ state: "disconnected", label: "Offline", latencyMs: 0, msgRate: 0, connectedSince: undefined });
  }

  getStatus(): TransportStatus {
    return { ...this._status };
  }

  sendVelocity(vel: Velocity): void {
    const topic = this._robot.rosTopics.cmdVel || "/cmd_vel";
    this._publish(topic, "geometry_msgs/Twist", {
      linear: { x: vel.linearX, y: vel.linearY, z: 0 },
      angular: { x: 0, y: 0, z: vel.angularZ },
    });
  }

  sendJointCommand(name: string, position: number): void {
    this._publish(jointCommandTopic(this._robot), "sensor_msgs/JointState", {
      name: [name],
      position: [position],
      velocity: [],
      effort: [],
    });
  }

  emergencyStop(): void {
    this._publish("/emergency_stop", "std_msgs/Bool", { data: true });
    this.sendVelocity({ linearX: 0, linearY: 0, angularZ: 0 });
  }

  releaseStop(): void {
    this._publish("/emergency_stop", "std_msgs/Bool", { data: false });
  }

  onStatus(cb: (s: TransportStatus) => void): () => void {
    this._statusCbs.push(cb);
    return () => {
      this._statusCbs = this._statusCbs.filter((c) => c !== cb);
    };
  }

  onTelemetry(cb: (t: RobotTelemetry) => void): () => void {
    this._telemetryCbs.push(cb);
    return () => {
      this._telemetryCbs = this._telemetryCbs.filter((c) => c !== cb);
    };
  }

  // ── internals ──────────────────────────────────────────────────────────────

  private _host(): string {
    return this._url.replace(/^wss?:\/\//, "").replace(/\/.*$/, "");
  }

  private _subscribeTopics(): void {
    const t = this._robot.rosTopics;
    if (t.odom) this._send({ op: "subscribe", topic: t.odom, type: "nav_msgs/Odometry" });
    if (t.jointStates) this._send({ op: "subscribe", topic: t.jointStates, type: "sensor_msgs/JointState" });
    this._send({ op: "subscribe", topic: "/battery_state", type: "sensor_msgs/BatteryState" });
  }

  private _startTimers(): void {
    // latency ping via rosapi
    this._pingTimer = setInterval(() => {
      if (this._ws?.readyState !== WebSocket.OPEN) return;
      const id = `ping_${Date.now()}`;
      this._pendingPings.set(id, performance.now());
      this._send({ op: "call_service", service: "/rosapi/get_time", args: {}, id });
      // drop stale pings
      this._pendingPings.forEach((t0, k) => {
        if (performance.now() - t0 > 5000) this._pendingPings.delete(k);
      });
    }, 2000);

    // message-rate sampler
    this._rateTimer = setInterval(() => {
      this._set({ msgRate: this._msgCount });
      this._msgCount = 0;
    }, 1000);
  }

  private _stopTimers(): void {
    if (this._pingTimer) clearInterval(this._pingTimer);
    if (this._rateTimer) clearInterval(this._rateTimer);
    this._pingTimer = null;
    this._rateTimer = null;
  }

  private _send(msg: RosbridgeMessage): void {
    if (!this._ws || this._ws.readyState !== WebSocket.OPEN) return;
    if (!msg.id) msg.id = `ohho_${++this._msgId}`;
    this._ws.send(JSON.stringify(msg));
  }

  private _publish(topic: string, type: string, msg: Record<string, unknown>): void {
    this._send({ op: "publish", topic, type, msg });
  }

  private _handleMessage(data: Record<string, unknown>): void {
    // service_response → latency ping
    if (data.op === "service_response" && typeof data.id === "string") {
      const t0 = this._pendingPings.get(data.id);
      if (t0 != null) {
        this._pendingPings.delete(data.id);
        this._set({ latencyMs: Math.round(performance.now() - t0) });
      }
      return;
    }

    if (data.op !== "publish") return;
    const topic = data.topic as string | undefined;
    const msg = data.msg as Record<string, unknown> | undefined;
    if (!topic || !msg) return;

    const t = this._robot.rosTopics;
    const telemetry: RobotTelemetry = { timestamp: Date.now() };
    let emit = false;

    if (topic === t.odom) {
      telemetry.odom = parseOdom(msg);
      emit = true;
    } else if (topic === t.jointStates) {
      telemetry.joints = parseJoints(msg);
      emit = true;
    } else if (topic === "/battery_state") {
      const pct = msg.percentage as number | undefined;
      if (typeof pct === "number") {
        telemetry.battery = pct > 1 ? pct / 100 : pct;
        emit = true;
      }
    }

    if (emit) for (const cb of this._telemetryCbs) cb(telemetry);
  }

  private _set(patch: Partial<TransportStatus>): void {
    this._status = { ...this._status, ...patch };
    for (const cb of this._statusCbs) cb(this.getStatus());
  }
}

// ── message parsers ─────────────────────────────────────────────────────────

function parseOdom(msg: Record<string, unknown>): Odometry {
  const pose = (msg.pose as Record<string, unknown>)?.pose as Record<string, unknown> | undefined;
  const twist = (msg.twist as Record<string, unknown>)?.twist as Record<string, unknown> | undefined;
  const pos = pose?.position as Record<string, number> | undefined;
  const ori = pose?.orientation as Record<string, number> | undefined;
  const lin = twist?.linear as Record<string, number> | undefined;
  const ang = twist?.angular as Record<string, number> | undefined;
  const qw = ori?.w ?? 1;
  const qz = ori?.z ?? 0;
  return {
    x: pos?.x ?? 0,
    y: pos?.y ?? 0,
    theta: Math.atan2(2 * qw * qz, 1 - 2 * qz * qz),
    vx: lin?.x ?? 0,
    vy: lin?.y ?? 0,
    omega: ang?.z ?? 0,
  };
}

function parseJoints(msg: Record<string, unknown>): JointReading[] {
  const names = msg.name as string[] | undefined;
  const positions = msg.position as number[] | undefined;
  const velocities = msg.velocity as number[] | undefined;
  if (!names || !positions) return [];
  return names.map((name, i) => ({
    name,
    position: positions[i] ?? 0,
    velocity: velocities?.[i],
  }));
}
