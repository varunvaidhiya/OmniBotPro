/*
 * OhhO Connect — the robot connection layer.
 *
 * A single, robot-agnostic transport abstraction so any robot in the garage
 * (OmniBot, a drone, a UR5e arm, a quadruped…) can be connected from the
 * website over whatever link the hardware exposes — Wi-Fi (ROSBridge), USB
 * (Web Serial), Bluetooth (Web BLE), or a built-in simulator.
 *
 * Every console reads the live connection through `useRobotConnection()` and
 * never needs to know which protocol is underneath. This file is pure
 * types + interfaces (no React, no DOM) so it imports anywhere.
 */

// ── Protocols ─────────────────────────────────────────────────────────────────

export type ConnectionProtocol =
  | "rosbridge" // Wi-Fi / Ethernet → rosbridge_suite WebSocket (the robot's own server)
  | "webserial" // USB cable → Web Serial API (raw firmware protocol, no ROS needed)
  | "webbluetooth" // BLE → Web Bluetooth API (Nordic UART serial-over-BLE)
  | "simulated"; // no hardware — deterministic in-browser telemetry

// ── Commands the consoles can send to any robot ───────────────────────────────

export interface Velocity {
  /** Forward/back, m/s. */
  linearX: number;
  /** Strafe (holonomic bases only), m/s. */
  linearY: number;
  /** Yaw, rad/s. */
  angularZ: number;
}

// ── Telemetry every transport emits (all fields optional / best-effort) ───────

export interface Odometry {
  x: number;
  y: number;
  theta: number;
  vx: number;
  vy: number;
  omega: number;
}

export interface JointReading {
  name: string;
  position: number;
  velocity?: number;
}

export interface RobotTelemetry {
  odom?: Odometry;
  joints?: JointReading[];
  /** Battery, 0..1 (fraction) when known. */
  battery?: number;
  /** Any extra robot-specific scalars/strings a transport wants to surface. */
  custom?: Record<string, number | string>;
  /** When this snapshot was produced, ms epoch. */
  timestamp: number;
}

// ── Live status of a transport ────────────────────────────────────────────────

export type ConnectionState =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "error"
  | "disconnected";

export interface TransportStatus {
  protocol: ConnectionProtocol;
  state: ConnectionState;
  /** Human-readable one-liner, e.g. "ROSBridge · 192.168.1.100". */
  label: string;
  /** Round-trip latency, ms (0 when unknown). */
  latencyMs: number;
  /** Inbound messages per second. */
  msgRate: number;
  /** Last error, if any. */
  error?: string;
  /** ms epoch when the link came up (for uptime). */
  connectedSince?: number;
}

// ── Persisted, per-robot connection settings (stored in UserRobot.config) ─────

export interface ConnectionConfig {
  protocol: ConnectionProtocol;
  /** ws:// or wss:// URL — rosbridge only. */
  address?: string;
  /** Serial baud rate — webserial only. */
  baudRate?: number;
  /** Reconnect automatically if the link drops. */
  autoReconnect?: boolean;
  /** ISO timestamp of the last successful connection (persisted for display). */
  lastConnectedAt?: string;
}

// ── The transport interface every protocol implements ─────────────────────────

export interface RobotTransport {
  readonly protocol: ConnectionProtocol;

  /** Open the link. Resolves once connected (or failed — check status.state). */
  connect(): Promise<TransportStatus>;

  /** Close the link and release resources. */
  disconnect(): Promise<void> | void;

  /** Current snapshot. */
  getStatus(): TransportStatus;

  /** Drive the base. */
  sendVelocity(vel: Velocity): void;

  /** Command a single joint to an absolute position (radians). */
  sendJointCommand(name: string, position: number): void;

  /** Halt everything immediately. */
  emergencyStop(): void;

  /** Clear the e-stop latch. */
  releaseStop(): void;

  /** Subscribe to status changes. Returns an unsubscribe fn. */
  onStatus(cb: (status: TransportStatus) => void): () => void;

  /** Subscribe to telemetry. Returns an unsubscribe fn. */
  onTelemetry(cb: (telemetry: RobotTelemetry) => void): () => void;
}
