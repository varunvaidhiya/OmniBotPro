import { describe, it, expect } from "vitest";

import {
  buildPacket,
  packetMotion,
  packetSetCarType,
  HEAD_TX,
  DEVICE_ID,
  FUNC_MOTION,
  FUNC_SET_CAR_TYPE,
} from "./yahboom";
import {
  isValidWsUrl,
  rosbridgeMixedContentWarning,
  isProtocolAvailable,
  getProtocol,
  PROTOCOLS,
} from "./protocols";
import {
  defaultConnectionConfig,
  readConnectionConfig,
  effectiveConnectionConfig,
} from "./config";
import { createTransport } from "./factory";
import { defaultRobotConfig } from "@/lib/garage/robot-config";
import type { UserRobot } from "@/lib/garage/types";
import type { ConnectionConfig } from "./types";

// ── Yahboom protocol encoder ──────────────────────────────────────────────────

describe("yahboom encoder", () => {
  it("frames set-car-type with the documented checksum", () => {
    // [0xFF,0xFC,LEN=4,FUNC=0x15,1] → checksum (Σ+5)&0xFF = 0x1A
    const pkt = packetSetCarType(1);
    expect(Array.from(pkt)).toEqual([HEAD_TX, DEVICE_ID, 0x04, FUNC_SET_CAR_TYPE, 0x01, 0x1a]);
  });

  it("recomputed checksum always matches the trailing byte", () => {
    const pkt = buildPacket(FUNC_SET_CAR_TYPE, [1, 2, 3]);
    const before = Array.from(pkt.slice(0, -1));
    const sum = (before.reduce((a, b) => a + b, 0) + 5) & 0xff;
    expect(pkt[pkt.length - 1]).toBe(sum);
  });

  it("encodes motion velocities as little-endian int16 × 1000", () => {
    const pkt = packetMotion(0.5, 0, 0); // vx → 500 = 0x01F4
    expect(pkt[2]).toBe(0x0a); // LEN = payload(7) + 3
    expect(pkt[3]).toBe(FUNC_MOTION);
    expect(pkt[4]).toBe(1); // car type
    expect(pkt[5]).toBe(0xf4); // vx lo
    expect(pkt[6]).toBe(0x01); // vx hi
  });

  it("clamps out-of-range velocities to int16", () => {
    const pkt = packetMotion(100, 0, 0); // 100000 → clamp 32767 = 0x7FFF
    expect(pkt[5]).toBe(0xff);
    expect(pkt[6]).toBe(0x7f);
  });
});

// ── Protocol metadata + capability detection ──────────────────────────────────

describe("protocols", () => {
  it("validates rosbridge URLs", () => {
    expect(isValidWsUrl("ws://192.168.1.100:9090")).toBe(true);
    expect(isValidWsUrl("wss://robot.example.com")).toBe(true);
    expect(isValidWsUrl("http://192.168.1.100")).toBe(false);
    expect(isValidWsUrl("not a url")).toBe(false);
  });

  it("has the four expected protocols", () => {
    expect(PROTOCOLS.map((p) => p.id).sort()).toEqual([
      "rosbridge",
      "simulated",
      "webbluetooth",
      "webserial",
    ]);
    expect(getProtocol("rosbridge").id).toBe("rosbridge");
  });

  it("guards against SSR (no window) in capability detection", () => {
    // vitest runs in the node environment, so window is undefined.
    const a = isProtocolAvailable("webserial");
    expect(a.available).toBe(false);
    expect(rosbridgeMixedContentWarning("ws://x:9090")).toBeNull();
  });
});

// ── Persisted connection config ──────────────────────────────────────────────

function fakeRobot(config: Record<string, unknown>): UserRobot {
  return {
    id: "r1",
    userId: "u1",
    name: "Test",
    robotTypeId: "t1",
    hardwareModelId: "omnibot",
    status: "draft",
    config,
    createdAt: "",
    updatedAt: "",
  };
}

describe("connection config", () => {
  it("defaults to rosbridge with an address", () => {
    const d = defaultConnectionConfig();
    expect(d.protocol).toBe("rosbridge");
    expect(d.address).toBeTruthy();
  });

  it("reads a saved connection blob, or null when absent", () => {
    const saved: ConnectionConfig = { protocol: "webserial", baudRate: 9600 };
    expect(readConnectionConfig(fakeRobot({ connection: saved }))?.protocol).toBe("webserial");
    expect(readConnectionConfig(fakeRobot({}))).toBeNull();
  });

  it("falls back to defaults when nothing is saved", () => {
    expect(effectiveConnectionConfig(fakeRobot({})).protocol).toBe("rosbridge");
  });
});

// ── Transport factory ─────────────────────────────────────────────────────────

describe("transport factory", () => {
  const rc = defaultRobotConfig();
  it("maps each protocol to its transport", () => {
    for (const p of ["rosbridge", "webserial", "webbluetooth", "simulated"] as const) {
      const t = createTransport(rc, { protocol: p });
      expect(t.protocol).toBe(p);
      expect(t.getStatus().state).toBe("idle");
    }
  });
});
