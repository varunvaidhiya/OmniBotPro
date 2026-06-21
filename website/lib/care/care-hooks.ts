/*
 * OhhO Care — degradation simulation hook (client-side only).
 *
 * Extracted from care.ts so the pure-data module (care-data.ts) can be
 * imported from server-side code (MCP tools, API routes) without React.
 */

import { useEffect, useRef, useState, useCallback } from "react";

export function useDegradationSimulation(running: boolean) {
  const [temps, setTemps] = useState<number[]>([]);
  const [elapsed, setElapsed] = useState(0);
  const rafRef = useRef<number>(0);
  const lastTime = useRef<number>(0);
  const tickRef = useRef<number>(0);

  useEffect(() => {
    if (temps.length === 0) {
      const seed: number[] = [];
      for (let i = 0; i < 40; i++) {
        seed.push(48 + Math.sin(i * 0.3) * 3 + Math.random() * 2);
      }
      setTemps(seed);
    }
  }, [temps.length]);

  useEffect(() => {
    if (!running) return;
    lastTime.current = performance.now();

    const loop = (time: number) => {
      const delta = (time - lastTime.current) / 1000;
      lastTime.current = time;
      tickRef.current += delta;

      if (tickRef.current > 0.5) {
        tickRef.current = 0;
        setElapsed((e) => e + 0.5);
        setTemps((prev) => {
          const last = prev[prev.length - 1] ?? 50;
          const next = Math.min(92, last + 0.4 + Math.random() * 0.6);
          return [...prev.slice(-59), next];
        });
      }

      rafRef.current = requestAnimationFrame(loop);
    };

    rafRef.current = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(rafRef.current);
  }, [running]);

  const reset = useCallback(() => {
    const seed: number[] = [];
    for (let i = 0; i < 40; i++) {
      seed.push(48 + Math.sin(i * 0.3) * 3 + Math.random() * 2);
    }
    setTemps(seed);
    setElapsed(0);
  }, []);

  const currentTemp = temps[temps.length - 1] ?? 50;
  const threshold = 85;
  const timeToThreshold = Math.max(0, (threshold - currentTemp) / 0.5);

  return {
    temps,
    currentTemp,
    threshold,
    elapsed,
    timeToThreshold,
    reset,
  };
}
