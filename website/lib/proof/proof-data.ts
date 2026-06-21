/*
 * OhhO Proof — pure data types and constants (no React hooks).
 *
 * Safe to import from server-side code (MCP tools, API routes).
 * The simulation hook lives in testing.ts (React).
 */

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
