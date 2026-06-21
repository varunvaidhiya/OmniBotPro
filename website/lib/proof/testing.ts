/*
 * Mock simulation testing data for OhhO Proof.
 *
 * Re-exports pure data from proof-data.ts (server-safe) and the
 * useProofSimulation hook (React, client-only).
 */

import { useState, useCallback, useMemo } from "react";

export { type TestSuite, type HeatmapValue, INITIAL_SUITES, INITIAL_COVERAGE, REGRESSION_DATA } from "./proof-data";
import { type TestSuite, type HeatmapValue, INITIAL_SUITES, INITIAL_COVERAGE, REGRESSION_DATA } from "./proof-data";

export function useProofSimulation() {
  const [suites, setSuites] = useState<TestSuite[]>(INITIAL_SUITES);
  const [coverage, setCoverage] = useState<HeatmapValue[][]>(INITIAL_COVERAGE);
  const [regression, setRegression] = useState<number[]>([...REGRESSION_DATA]);
  const [isRunning, setIsRunning] = useState(false);

  const runFullSuite = useCallback(() => {
    setIsRunning(true);
    
    // Simulate updating coverage and pass rates
    let step = 0;
    const interval = setInterval(() => {
      setSuites(prev => prev.map(s => {
        // slightly fluctuate pass rates
        const shift = (Math.random() - 0.4) * 0.02;
        return { ...s, passRate: Math.min(1, Math.max(0, s.passRate + shift)) };
      }));

      setCoverage(prev => {
        const next = [...prev].map(row => [...row]);
        // randomly turn 0s or 1s into 2s (improving coverage)
        for (let r = 0; r < next.length; r++) {
          for (let c = 0; c < next[r].length; c++) {
            if (next[r][c] < 2 && Math.random() < 0.15) {
              next[r][c] = (next[r][c] + 1) as HeatmapValue;
            }
          }
        }
        return next;
      });

      // append a new regression data point (trending slightly upward + jitter)
      setRegression((prev) => {
        const base = prev[prev.length - 1] || 0.9;
        const next = Math.min(1, Math.max(0, base + (Math.random() - 0.3) * 0.03));
        const updated = [...prev, Math.round(next * 1000) / 1000];
        return updated.slice(-14); // keep last 14 points
      });

      step++;
      if (step > 15) {
        clearInterval(interval);
        setIsRunning(false);
      }
    }, 400);
  }, []);

  const overallVerdict = useMemo(() => {
    const avg = suites.reduce((acc, s) => acc + s.passRate, 0) / suites.length;
    return avg;
  }, [suites]);

  return {
    suites,
    coverage,
    regression,
    isRunning,
    runFullSuite,
    overallVerdict,
  };
}
