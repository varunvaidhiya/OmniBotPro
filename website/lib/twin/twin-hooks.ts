/*
 * OhhO Twin — React simulation hook (client-side only).
 *
 * Extracted from twin.ts so the pure-data module (twin-data.ts) can be
 * imported from server-side code (MCP tools, API routes) without pulling
 * in React.
 */

import { useEffect, useRef, useState, useCallback } from "react";
import type { TwinState } from "./twin-data";

const W = 220;
const H = 134;

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

        if (x < 10 || x > W - 10) { vx = -vx; x = Math.max(10, Math.min(W - 10, x)); }
        if (y < 10 || y > H - 10) { vy = -vy; y = Math.max(10, Math.min(H - 10, y)); }

        motorTemps = motorTemps.map((mt, i) =>
          Math.min(95, mt + delta * (0.5 + i * 0.1)),
        );

        battery = Math.max(0, battery - delta * 0.002);

        const next: TwinState = { x, y, theta, vx, vy, vtheta, motorTemps, battery, t: Date.now() };
        return next;
      });

      rafRef.current = requestAnimationFrame(loop);
    };

    rafRef.current = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(rafRef.current);
  }, [running]);

  const recordFrame = useCallback(() => {
    setFrames((prev) => {
      const next = [...prev, state];
      return next.length > 600 ? next.slice(next.length - 600) : next;
    });
  }, [state]);

  useEffect(() => {
    if (!running) return;
    const iv = setInterval(recordFrame, 100);
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
