/*
 * USB transport (Web Serial API) — connect a robot with a plain USB cable, no
 * ROS or network required. Chromium-only (navigator.serial).
 *
 *   OmniBot-class (Yahboom): velocity commands are encoded as Yahboom motion
 *   packets (see ./yahboom). Best-effort RX decode of the velocity feedback
 *   packet for live telemetry.
 *
 *   Any other robot: a generic line protocol — commands are sent as JSON lines
 *   ({"cmd_vel":{...}}), and incoming JSON lines are surfaced as telemetry.
 */

import type { RobotConfig } from "@/lib/garage/robot-config";
import {
  HEAD_RX,
  TYPE_VELOCITY,
  packetMotion,
  packetSetCarType,
} from "./yahboom";
import type {
  ConnectionConfig,
  RobotTelemetry,
  RobotTransport,
  TransportStatus,
  Velocity,
} from "./types";

// Minimal Web Serial typings (not in the standard TS lib).
interface SerialPortLike {
  open(options: { baudRate: number }): Promise<void>;
  close(): Promise<void>;
  readable: ReadableStream<Uint8Array> | null;
  writable: WritableStream<Uint8Array> | null;
}
interface SerialLike {
  requestPort(): Promise<SerialPortLike>;
}
function getSerial(): SerialLike | null {
  if (typeof navigator === "undefined") return null;
  return (navigator as unknown as { serial?: SerialLike }).serial ?? null;
}

/** OmniBot-class robots use the Yahboom protocol; others get the generic one. */
function usesYahboom(robot: RobotConfig): boolean {
  const id = robot.robotId.toLowerCase();
  const mfr = robot.manufacturer.toLowerCase();
  return id.startsWith("omnibot") || mfr.includes("yahboom") || robot.drive === "mecanum";
}

export class WebSerialTransport implements RobotTransport {
  readonly protocol = "webserial" as const;

  private _robot: RobotConfig;
  private _baud: number;
  private _yahboom: boolean;
  private _port: SerialPortLike | null = null;
  private _writer: WritableStreamDefaultWriter<Uint8Array> | null = null;
  private _reader: ReadableStreamDefaultReader<Uint8Array> | null = null;
  private _reading = false;
  private _rxBuf: number[] = [];
  private _textBuf = "";
  private _msgCount = 0;
  private _rateTimer: ReturnType<typeof setInterval> | null = null;

  private _status: TransportStatus;
  private _statusCbs: Array<(s: TransportStatus) => void> = [];
  private _telemetryCbs: Array<(t: RobotTelemetry) => void> = [];

  constructor(robot: RobotConfig, cfg: ConnectionConfig) {
    this._robot = robot;
    this._baud = cfg.baudRate ?? 115200;
    this._yahboom = usesYahboom(robot);
    this._status = {
      protocol: "webserial",
      state: "idle",
      label: "Offline",
      latencyMs: 0,
      msgRate: 0,
    };
  }

  async connect(): Promise<TransportStatus> {
    const serial = getSerial();
    if (!serial) {
      this._set({ state: "error", error: "Web Serial isn't available. Use Chrome or Edge." });
      return this.getStatus();
    }
    this._set({ state: "connecting", label: "Requesting serial port…", error: undefined });
    try {
      // Must be triggered by a user gesture — the modal's Connect button is one.
      this._port = await serial.requestPort();
      await this._port.open({ baudRate: this._baud });

      this._writer = this._port.writable?.getWriter() ?? null;
      if (this._yahboom) await this._write(packetSetCarType());

      this._set({
        state: "connected",
        label: `USB · ${this._baud} baud${this._yahboom ? " · Yahboom" : ""}`,
        connectedSince: Date.now(),
        error: undefined,
      });
      this._startReadLoop();
      this._rateTimer = setInterval(() => {
        this._set({ msgRate: this._msgCount });
        this._msgCount = 0;
      }, 1000);
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      // requestPort throws if the user dismisses the chooser — treat as idle.
      this._set({
        state: /cancell|no port selected|chooser/i.test(msg) ? "disconnected" : "error",
        label: "Offline",
        error: /cancell|no port selected|chooser/i.test(msg) ? undefined : msg,
      });
    }
    return this.getStatus();
  }

  async disconnect(): Promise<void> {
    this._reading = false;
    if (this._rateTimer) clearInterval(this._rateTimer);
    this._rateTimer = null;
    try {
      await this._reader?.cancel();
      this._reader?.releaseLock();
    } catch {
      /* ignore */
    }
    try {
      this._writer?.releaseLock();
    } catch {
      /* ignore */
    }
    try {
      await this._port?.close();
    } catch {
      /* ignore */
    }
    this._reader = null;
    this._writer = null;
    this._port = null;
    this._set({ state: "disconnected", label: "Offline", latencyMs: 0, msgRate: 0, connectedSince: undefined });
  }

  getStatus(): TransportStatus {
    return { ...this._status };
  }

  sendVelocity(vel: Velocity): void {
    if (this._yahboom) {
      void this._write(packetMotion(vel.linearX, vel.linearY, vel.angularZ));
    } else {
      void this._writeLine(JSON.stringify({ cmd_vel: { x: vel.linearX, y: vel.linearY, w: vel.angularZ } }));
    }
  }

  sendJointCommand(name: string, position: number): void {
    // No Yahboom arm packet over this bus; emit the generic line form so a
    // custom firmware can act on it.
    void this._writeLine(JSON.stringify({ joint: { name, position } }));
  }

  emergencyStop(): void {
    this.sendVelocity({ linearX: 0, linearY: 0, angularZ: 0 });
    if (!this._yahboom) void this._writeLine(JSON.stringify({ estop: true }));
  }

  releaseStop(): void {
    if (!this._yahboom) void this._writeLine(JSON.stringify({ estop: false }));
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

  private async _write(bytes: Uint8Array): Promise<void> {
    try {
      await this._writer?.write(bytes);
    } catch {
      /* link dropped — read loop will surface it */
    }
  }

  private _writeLine(line: string): Promise<void> {
    return this._write(new TextEncoder().encode(line + "\n"));
  }

  private async _startReadLoop(): Promise<void> {
    if (!this._port?.readable) return;
    this._reading = true;
    this._reader = this._port.readable.getReader();
    try {
      while (this._reading) {
        const { value, done } = await this._reader.read();
        if (done) break;
        if (value) {
          this._msgCount++;
          this._yahboom ? this._consumeYahboom(value) : this._consumeText(value);
        }
      }
    } catch {
      if (this._reading) this._set({ state: "error", error: "Serial read error — cable unplugged?" });
    }
  }

  /** Best-effort decode of Yahboom RX velocity feedback frames (0xFB …). */
  private _consumeYahboom(chunk: Uint8Array): void {
    for (let i = 0; i < chunk.length; i++) this._rxBuf.push(chunk[i]);
    // Scan for frames headed by 0xFB; byte[1] is the frame length.
    while (this._rxBuf.length >= 2) {
      if (this._rxBuf[0] !== HEAD_RX) {
        this._rxBuf.shift();
        continue;
      }
      const len = this._rxBuf[1];
      const total = len + 2; // head + len byte + (len-? ) — total frame bytes
      if (this._rxBuf.length < total) break;
      const frame = this._rxBuf.splice(0, total);
      try {
        if (frame[3] === TYPE_VELOCITY && frame.length >= 10) {
          const vx = int16(frame[4], frame[5]) / 1000;
          const vy = int16(frame[6], frame[7]) / 1000;
          const omega = int16(frame[8], frame[9]) / 1000;
          this._emit({ odom: { x: 0, y: 0, theta: 0, vx, vy, omega }, timestamp: Date.now() });
        }
      } catch {
        /* skip malformed frame */
      }
    }
    if (this._rxBuf.length > 512) this._rxBuf = this._rxBuf.slice(-256);
  }

  /** Generic line protocol — JSON per line. */
  private _consumeText(chunk: Uint8Array): void {
    this._textBuf += new TextDecoder().decode(chunk);
    let nl: number;
    while ((nl = this._textBuf.indexOf("\n")) >= 0) {
      const line = this._textBuf.slice(0, nl).trim();
      this._textBuf = this._textBuf.slice(nl + 1);
      if (!line) continue;
      try {
        const obj = JSON.parse(line) as Record<string, unknown>;
        this._emit({ custom: obj as Record<string, number | string>, timestamp: Date.now() });
      } catch {
        /* not JSON — ignore */
      }
    }
    if (this._textBuf.length > 4096) this._textBuf = this._textBuf.slice(-1024);
  }

  private _emit(t: RobotTelemetry): void {
    for (const cb of this._telemetryCbs) cb(t);
  }

  private _set(patch: Partial<TransportStatus>): void {
    this._status = { ...this._status, ...patch };
    for (const cb of this._statusCbs) cb(this.getStatus());
  }
}

function int16(lo: number, hi: number): number {
  const v = (hi << 8) | lo;
  return v >= 0x8000 ? v - 0x10000 : v;
}
