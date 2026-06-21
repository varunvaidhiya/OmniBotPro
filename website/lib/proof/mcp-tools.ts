/*
 * Proof MCP tools — scenario test suites, coverage, and regression tracking.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult,
} from "@/lib/mcp/types";
import { INITIAL_SUITES, INITIAL_COVERAGE, REGRESSION_DATA } from "@/lib/proof/proof-data";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "proof.listSuites",
    description: "List all test suites (Navigation, Manipulation, Edge cases, Fault injection) with pass rates.",
    inputSchema: s({}),
    handler: async () => jsonResult(INITIAL_SUITES),
    product: "proof", readOnly: true,
  },
  {
    name: "proof.getCoverage",
    description: "Get the scenario coverage heatmap (5×8 grid of pass/partial/miss values) showing which condition combinations have been tested.",
    inputSchema: s({}),
    handler: async () => jsonResult({ coverage: INITIAL_COVERAGE, rows: INITIAL_COVERAGE.length, cols: INITIAL_COVERAGE[0]?.length ?? 0 }),
    product: "proof", readOnly: true,
  },
  {
    name: "proof.getRegression",
    description: "Get the sim-to-real regression trend data — pass rates across recent builds to detect regressions.",
    inputSchema: s({}),
    handler: async () => jsonResult({ data: REGRESSION_DATA, builds: REGRESSION_DATA.length, latest: REGRESSION_DATA[REGRESSION_DATA.length - 1], trend: "improving" }),
    product: "proof", readOnly: true,
  },
  {
    name: "proof.getVerdict",
    description: "Get the overall safety-case verdict: average pass rate across all suites and whether the robot is ready to ship.",
    inputSchema: s({}),
    handler: async () => {
      const avg = INITIAL_SUITES.reduce((acc, s) => acc + s.passRate, 0) / INITIAL_SUITES.length;
      return jsonResult({
        overallPassRate: Math.round(avg * 100) / 100,
        readyToShip: avg >= 0.9,
        suites: INITIAL_SUITES.length,
        regressions: 0,
        coverageGaps: INITIAL_COVERAGE.flat().filter((v) => v < 2).length,
      });
    },
    product: "proof", readOnly: true,
  },
];
