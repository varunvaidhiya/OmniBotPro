/*
 * Simulated transport — no hardware. Streams deterministic-ish telemetry so the
 * whole connection UI (bar, telemetry, e-stop) and every console can be explored
 * without a robot. Velocity commands move the simulated pose.
 */

import type { RobotConfig } from "@/lib/garage/robot-config";
import type {
  ConnectionConfig,
  RobotTelemetry,
  RobotTransport,
  TransportStatus,
  Velocity,
} from "./types";

export class SimulatedTransport implements RobotTransport {
  readonly protocol = "simulated" as const;

  private _robot: RobotConfig;
  private _vel: Velocity = { linearX: 0, linearY: 0, angularZ: 0 };
  private _pose = { x: 0, y: 0, theta: 0 };
  private _estop = false;
  private _timer: ReturnType<typeof setInterval> | null = null;
  private _joints: number[];

  private _status: TransportStatus;
  private _statusCbs: Array<(s: TransportStatus) => void> = [];
  private _telemetryCbs: Array<(t: RobotTelemetry) => void> = [];

  constructor(robot: RobotConfig, _cfg: ConnectionConfig) {
    this._robot = robot;
    this._joints = robot.joints.map((j) => j.home);
    this._status = {
      protocol: "simulated",
      state: "idle",
      label: "Offline",
      latencyMs: 0,
      msgRate: 0,
    };
  }

  connect(): Promise<TransportStatus> {
    this._set({ state: "connecting", label: "Starting simulator…" });
    return new Promise((resolve) => {
      setTimeout(() => {
        this._set({
          state: "connected",
          label: "Simulated robot",
          connectedSince: Date.now(),
          latencyMs: 2,
          msgRate: 20,
        });
        this._timer = setInterval(() => this._tick(), 100);
        resolve(this.getStatus());
      }, 350);
    });
  }

  disconnect(): void {
    if (this._timer) clearInterval(this._timer);
    this._timer = null;
    this._set({ state: "disconnected", label: "Offline", latencyMs: 0, msgRate: 0, connectedSince: undefined });
  }

  getStatus(): TransportStatus {
    return { ...this._status };
  }

  sendVelocity(vel: Velocity): void {
    this._vel = this._estop ? { linearX: 0, linearY: 0, angularZ: 0 } : vel;
  }

  sendJointCommand(name: string, position: number): void {
    const idx = this._robot.joints.findIndex((j) => j.name === name);
    if (idx >= 0) this._joints[idx] = position;
  }

  emergencyStop(): void {
    this._estop = true;
    this._vel = { linearX: 0, linearY: 0, angularZ: 0 };
  }

  releaseStop(): void {
    this._estop = false;
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

  private _tick(): void {
    const dt = 0.1;
    // integrate the (holonomic) pose from the current velocity command
    this._pose.theta += this._vel.angularZ * dt;
    this._pose.x += (this._vel.linearX * Math.cos(this._pose.theta) - this._vel.linearY * Math.sin(this._pose.theta)) * dt;
    this._pose.y += (this._vel.linearX * Math.sin(this._pose.theta) + this._vel.linearY * Math.cos(this._pose.theta)) * dt;
    this._set({ latencyMs: 2 + Math.round(Math.random() * 3) });
    const telemetry: RobotTelemetry = {
      odom: {
        x: this._pose.x,
        y: this._pose.y,
        theta: this._pose.theta,
        vx: this._vel.linearX,
        vy: this._vel.linearY,
        omega: this._vel.angularZ,
      },
      joints: this._robot.joints.map((j, i) => ({ name: j.name, position: this._joints[i] ?? j.home })),
      battery: 0.87,
      timestamp: Date.now(),
    };
    for (const cb of this._telemetryCbs) cb(telemetry);
  }

  private _set(patch: Partial<TransportStatus>): void {
    this._status = { ...this._status, ...patch };
    for (const cb of this._statusCbs) cb(this.getStatus());
  }
}
