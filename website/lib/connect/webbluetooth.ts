/*
 * Bluetooth transport (Web Bluetooth API) — pair over BLE using the Nordic
 * UART Service (serial-over-BLE). Chromium-only and experimental: it requires
 * the robot to expose a NUS GATT service. Commands use the same encoding as the
 * USB transport (Yahboom packets for OmniBot, JSON lines otherwise).
 */

import type { RobotConfig } from "@/lib/garage/robot-config";
import { TYPE_VELOCITY, HEAD_RX, packetMotion, packetSetCarType } from "./yahboom";
import type {
  ConnectionConfig,
  RobotTelemetry,
  RobotTransport,
  TransportStatus,
  Velocity,
} from "./types";

// Nordic UART Service UUIDs.
const NUS_SERVICE = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
const NUS_RX = "6e400002-b5a3-f393-e0a9-e50e24dcca9e"; // write (app → device)
const NUS_TX = "6e400003-b5a3-f393-e0a9-e50e24dcca9e"; // notify (device → app)

// Minimal Web Bluetooth typings (not in the standard TS lib).
interface BTChar {
  writeValue(data: Uint8Array): Promise<void>;
  startNotifications(): Promise<BTChar>;
  addEventListener(type: "characteristicvaluechanged", cb: (e: Event) => void): void;
  value?: DataView;
}
interface BTService {
  getCharacteristic(uuid: string): Promise<BTChar>;
}
interface BTGatt {
  connected: boolean;
  connect(): Promise<BTGatt>;
  disconnect(): void;
  getPrimaryService(uuid: string): Promise<BTService>;
}
interface BTDevice {
  gatt?: BTGatt;
  addEventListener(type: "gattserverdisconnected", cb: () => void): void;
}
interface BluetoothLike {
  requestDevice(opts: {
    filters?: Array<{ services?: string[] }>;
    optionalServices?: string[];
    acceptAllDevices?: boolean;
  }): Promise<BTDevice>;
}
function getBluetooth(): BluetoothLike | null {
  if (typeof navigator === "undefined") return null;
  return (navigator as unknown as { bluetooth?: BluetoothLike }).bluetooth ?? null;
}

function usesYahboom(robot: RobotConfig): boolean {
  const id = robot.robotId.toLowerCase();
  return id.startsWith("omnibot") || robot.manufacturer.toLowerCase().includes("yahboom") || robot.drive === "mecanum";
}

export class WebBluetoothTransport implements RobotTransport {
  readonly protocol = "webbluetooth" as const;

  private _robot: RobotConfig;
  private _yahboom: boolean;
  private _device: BTDevice | null = null;
  private _rx: BTChar | null = null; // write characteristic
  private _rxBuf: number[] = [];
  private _msgCount = 0;
  private _rateTimer: ReturnType<typeof setInterval> | null = null;

  private _status: TransportStatus;
  private _statusCbs: Array<(s: TransportStatus) => void> = [];
  private _telemetryCbs: Array<(t: RobotTelemetry) => void> = [];

  constructor(robot: RobotConfig, _cfg: ConnectionConfig) {
    this._robot = robot;
    this._yahboom = usesYahboom(robot);
    this._status = {
      protocol: "webbluetooth",
      state: "idle",
      label: "Offline",
      latencyMs: 0,
      msgRate: 0,
    };
  }

  async connect(): Promise<TransportStatus> {
    const bt = getBluetooth();
    if (!bt) {
      this._set({ state: "error", error: "Web Bluetooth isn't available. Use Chrome or Edge." });
      return this.getStatus();
    }
    this._set({ state: "connecting", label: "Pairing over BLE…", error: undefined });
    try {
      this._device = await bt.requestDevice({
        filters: [{ services: [NUS_SERVICE] }],
        optionalServices: [NUS_SERVICE],
      });
      this._device.addEventListener("gattserverdisconnected", () => {
        this._set({ state: "disconnected", label: "Offline", latencyMs: 0, msgRate: 0 });
      });
      const gatt = await this._device.gatt?.connect();
      if (!gatt) throw new Error("No GATT server.");
      const service = await gatt.getPrimaryService(NUS_SERVICE);
      this._rx = await service.getCharacteristic(NUS_RX);
      const tx = await service.getCharacteristic(NUS_TX);
      await tx.startNotifications();
      tx.addEventListener("characteristicvaluechanged", (e) => this._onNotify(e));

      if (this._yahboom) await this._write(packetSetCarType());

      this._set({ state: "connected", label: "Bluetooth · BLE", connectedSince: Date.now(), error: undefined });
      this._rateTimer = setInterval(() => {
        this._set({ msgRate: this._msgCount });
        this._msgCount = 0;
      }, 1000);
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      const cancelled = /cancell|user|chooser/i.test(msg);
      this._set({
        state: cancelled ? "disconnected" : "error",
        label: "Offline",
        error: cancelled ? undefined : msg,
      });
    }
    return this.getStatus();
  }

  disconnect(): void {
    if (this._rateTimer) clearInterval(this._rateTimer);
    this._rateTimer = null;
    try {
      this._device?.gatt?.disconnect();
    } catch {
      /* ignore */
    }
    this._device = null;
    this._rx = null;
    this._set({ state: "disconnected", label: "Offline", latencyMs: 0, msgRate: 0, connectedSince: undefined });
  }

  getStatus(): TransportStatus {
    return { ...this._status };
  }

  sendVelocity(vel: Velocity): void {
    if (this._yahboom) {
      void this._write(packetMotion(vel.linearX, vel.linearY, vel.angularZ));
    } else {
      void this._write(new TextEncoder().encode(JSON.stringify({ cmd_vel: { x: vel.linearX, y: vel.linearY, w: vel.angularZ } }) + "\n"));
    }
  }

  sendJointCommand(name: string, position: number): void {
    void this._write(new TextEncoder().encode(JSON.stringify({ joint: { name, position } }) + "\n"));
  }

  emergencyStop(): void {
    this.sendVelocity({ linearX: 0, linearY: 0, angularZ: 0 });
  }

  releaseStop(): void {
    /* no-op for BLE */
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
      await this._rx?.writeValue(bytes);
    } catch {
      /* link dropped */
    }
  }

  private _onNotify(e: Event): void {
    const dv = (e.target as unknown as { value?: DataView }).value;
    if (!dv) return;
    this._msgCount++;
    if (!this._yahboom) return; // generic NUS telemetry is robot-specific; skip
    for (let i = 0; i < dv.byteLength; i++) this._rxBuf.push(dv.getUint8(i));
    while (this._rxBuf.length >= 2) {
      if (this._rxBuf[0] !== HEAD_RX) {
        this._rxBuf.shift();
        continue;
      }
      const total = this._rxBuf[1] + 2;
      if (this._rxBuf.length < total) break;
      const frame = this._rxBuf.splice(0, total);
      if (frame[3] === TYPE_VELOCITY && frame.length >= 10) {
        const vx = i16(frame[4], frame[5]) / 1000;
        const vy = i16(frame[6], frame[7]) / 1000;
        const omega = i16(frame[8], frame[9]) / 1000;
        for (const cb of this._telemetryCbs) cb({ odom: { x: 0, y: 0, theta: 0, vx, vy, omega }, timestamp: Date.now() });
      }
    }
  }

  private _set(patch: Partial<TransportStatus>): void {
    this._status = { ...this._status, ...patch };
    for (const cb of this._statusCbs) cb(this.getStatus());
  }
}

function i16(lo: number, hi: number): number {
  const v = (hi << 8) | lo;
  return v >= 0x8000 ? v - 0x10000 : v;
}
