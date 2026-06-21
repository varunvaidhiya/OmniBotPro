/*
 * OhhO Twin — digital twin simulation data and types.
 *
 * Pure data + the useTwinSimulation hook. The Twin console uses this to
 * render the live sim mirror, the replay timeline, the what-if branch
 * controls, and the prediction panel.
 */

import { useEffect, useRef, useState, useCallback } from "react";

export interface TwinState {
  x: number;
  y: number;
  theta: number;
  vx: number;
  vy: number;
  vtheta: number;
  /** Motor temperatures in °C (per-joint). */
  motorTemps: number[];
  /** Battery fraction 0..1. */
  battery: number;
  /** Timestamp (ms epoch). */
  t: number;
}

export interface Prediction {
  label: string;
  current: string;
  predicted: string;
  trend: "up" | "down" | "stable";
  urgency: "low" | "medium" | "high";
  /** Hours until predicted threshold crossing. */
  hoursToThreshold: number;
}

export interface WhatIfResult {
  scenario: string;
  change: string;
  outcome: string;
  successRate: number;
  /** Compared to the original run. */
  delta: number;
}

const W = 220;
const H = 134;
const N_MOTORS = 6;

const INITIAL_STATE: TwinState = {
  x: 120,
  y: 70,
  theta: 0,
  vx: 0.3,
  vy: 0,
  vtheta: 0.02,
  motorTemps: [48, 52, 45, 50, 47, 49],
  battery: 0.82,
  t: Date.now(),
};

export const PREDICTIONS: Prediction[] = [
  {
    label: "Motor temp (left knee)",
    current: "62°C",
    predicted: "78°C",
    trend: "up",
    urgency: "medium",
    hoursToThreshold: 4.2,
  },
  {
    label: "Battery",
    current: "82%",
    predicted: "61%",
    trend: "down",
    urgency: "low",
    hoursToThreshold: 2.8,
  },
  {
    label: "Joint wear (wrist)",
    current: "MTBF 412h",
    predicted: "MTBF 380h",
    trend: "down",
    urgency: "medium",
    hoursToThreshold: 72,
  },
];

export const WHATIF_RESULTS: WhatIfResult[] = [
  {
    scenario: "Different grasp angle",
    change: "+15° approach",
    outcome: "Pick succeeds with better clearance",
    successRate: 0.91,
    delta: 0.17,
  },
  {
    scenario: "Reduced speed",
    change: "0.15 m/s (was 0.3)",
    outcome: "Smoother trajectory, same success",
    successRate: 0.88,
    delta: 0.14,
  },
];

export function useTwinSimulation(running: boolean) {
  const [state, setState] = useState<TwinState>(INITIAL_STATE);
  const [frames, setFrames] = useState<TwinState[]>([]);
  const [replayIndex, setReplayIndex] = useState<number | null>(null);
  const rafRef = useRef<number>(0);
  const lastTime = useRef<number>(0);

  useEffect(() => {
    if (!running) return;
    lastTime.current = performance.now();

    const loop = (time: number) => {
      const delta = (time - lastTime.current) / 1000;
      lastTime.current = time;

      setState((prev) => {
        let { x, y, theta, vx, vy, vtheta, motorTemps, battery, t } = prev;

        x += vx * delta * 20;
        y += vy * delta * 20;
        theta += vtheta * delta * 10;

        // Bounce off bounds
        if (x < 10 || x > W - 10) { vx = -vx; x = Math.max(10, Math.min(W - 10, x)); }
        if (y < 10 || y > H - 10) { vy = -vy; y = Math.max(10, Math.min(H - 10, y)); }

        // Motor temps drift up slightly
        motorTemps = motorTemps.map((mt, i) =>
          Math.min(95, mt + delta * (0.5 + i * 0.1))
        );

        // Battery drains
        battery = Math.max(0, battery - delta * 0.002);

        const next: TwinState = { x, y, theta, vx, vy, vtheta, motorTemps, battery, t: Date.now() };
        return next;
      });

      rafRef.current = requestAnimationFrame(loop);
    };

    rafRef.current = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(rafRef.current);
  }, [running]);

  // Record frames for replay
  const recordFrame = useCallback(() => {
    setFrames((prev) => {
      const next = [...prev, state];
      // Keep last 600 frames (~10 seconds at 60fps)
      return next.length > 600 ? next.slice(next.length - 600) : next;
    });
  }, [state]);

  useEffect(() => {
    if (!running) return;
    const iv = setInterval(recordFrame, 100); // 10 Hz recording
    return () => clearInterval(iv);
  }, [running, recordFrame]);

  const scrubTo = useCallback((index: number) => {
    setReplayIndex(index);
  }, []);

  const clearReplay = useCallback(() => {
    setReplayIndex(null);
    setFrames([]);
  }, []);

  const displayState = replayIndex !== null && frames[replayIndex]
    ? frames[replayIndex]
    : state;

  return {
    state: displayState,
    liveState: state,
    frames,
    replayIndex,
    isRecording: running && replayIndex === null,
    scrubTo,
    clearReplay,
    frameCount: frames.length,
  };
}

export const TWIN_STATS = {
  simWorld: "Isaac Sim",
  matchScore: 0.97,
  driftCm: 3,
  replayStorageMb: 2.1,
};
