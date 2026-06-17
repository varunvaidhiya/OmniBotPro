/*
 * Mock system telemetry for OhhO Frame.
 */

import { useState, useEffect } from "react";

export interface TelemetryPoint {
  time: string;
  cpu: number;
  ram: number;
  temp: number;
}

export function useSystemTelemetry() {
  const [history, setHistory] = useState<TelemetryPoint[]>([]);
  const [current, setCurrent] = useState<TelemetryPoint>({
    time: new Date().toLocaleTimeString(),
    cpu: 12,
    ram: 45,
    temp: 42,
  });

  useEffect(() => {
    // Generate initial history
    const initial: TelemetryPoint[] = [];
    const now = new Date();
    for (let i = 20; i > 0; i--) {
      const d = new Date(now.getTime() - i * 2000);
      initial.push({
        time: d.toLocaleTimeString([], { hour12: false, hour: '2-digit', minute:'2-digit', second:'2-digit' }),
        cpu: 10 + Math.random() * 20,
        ram: 40 + Math.random() * 10,
        temp: 40 + Math.random() * 5,
      });
    }
    setHistory(initial);

    // Live updates
    const interval = setInterval(() => {
      setCurrent(prev => {
        const next = {
          time: new Date().toLocaleTimeString([], { hour12: false, hour: '2-digit', minute:'2-digit', second:'2-digit' }),
          cpu: Math.max(0, Math.min(100, prev.cpu + (Math.random() - 0.5) * 15)),
          ram: Math.max(0, Math.min(100, prev.ram + (Math.random() - 0.5) * 2)),
          temp: Math.max(20, Math.min(90, prev.temp + (Math.random() - 0.5) * 1)),
        };
        
        setHistory(h => {
          const newH = [...h, next];
          if (newH.length > 20) newH.shift();
          return newH;
        });

        return next;
      });
    }, 2000);

    return () => clearInterval(interval);
  }, []);

  return { current, history };
}
