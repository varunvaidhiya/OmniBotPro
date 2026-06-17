/*
 * Mock simulation testing data for OhhO Proof.
 */

import { useState, useCallback, useMemo } from "react";

export interface TestSuite {
  id: string;
  name: string;
  passRate: number;
}

export type HeatmapValue = 0 | 1 | 2; // 0=miss, 1=partial, 2=pass

export const INITIAL_SUITES: TestSuite[] = [
  { id: "nav", name: "Navigation", passRate: 0.98 },
  { id: "man", name: "Manipulation", passRate: 0.94 },
  { id: "edge", name: "Edge cases", passRate: 0.87 },
  { id: "fault", name: "Fault injection", passRate: 0.76 },
];

export const INITIAL_COVERAGE: HeatmapValue[][] = [
  [2, 2, 2, 1, 2, 2, 2, 0],
  [2, 2, 1, 2, 2, 2, 1, 2],
  [2, 1, 2, 2, 0, 2, 2, 2],
  [1, 2, 2, 2, 2, 1, 2, 2],
  [2, 2, 2, 1, 2, 2, 2, 1],
];

export const REGRESSION_DATA = [0.9, 0.92, 0.88, 0.94, 0.93, 0.95, 0.91, 0.96, 0.94, 0.97];

export function useProofSimulation() {
  const [suites, setSuites] = useState<TestSuite[]>(INITIAL_SUITES);
  const [coverage, setCoverage] = useState<HeatmapValue[][]>(INITIAL_COVERAGE);
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
        let changed = false;
        for (let r = 0; r < next.length; r++) {
          for (let c = 0; c < next[r].length; c++) {
            if (next[r][c] < 2 && Math.random() < 0.1) {
              next[r][c] = (next[r][c] + 1) as HeatmapValue;
              changed = true;
              break;
            }
          }
          if (changed) break;
        }
        return next;
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
    regression: REGRESSION_DATA,
    isRunning,
    runFullSuite,
    overallVerdict,
  };
}
