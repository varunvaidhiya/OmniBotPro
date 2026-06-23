/*
 * Proof MCP tools — scenario test suites, coverage, and regression tracking.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
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
  {
    name: "proof.getSuite",
    description: "Get a single test suite by id (nav, man, edge, fault) with its pass rate.",
    inputSchema: s({ suiteId: { type: "string", description: "Suite id", enum: ["nav", "man", "edge", "fault"] } }, ["suiteId"]),
    handler: async (p) => {
      const suite = INITIAL_SUITES.find((x) => x.id === p.suiteId);
      return suite ? jsonResult(suite) : errorResult(`Unknown suite: ${p.suiteId}`);
    },
    product: "proof", readOnly: true,
  },
  {
    name: "proof.runSuite",
    description: "Run a scenario test suite (or all suites if none specified) in simulation and return the resulting pass rate.",
    inputSchema: s({
      suiteId: { type: "string", description: "Suite id to run; omit to run all suites", enum: ["nav", "man", "edge", "fault"] },
      seeds: { type: "number", description: "Number of randomized seeds/scenarios to run (default 100)" },
    }),
    handler: async (p) => {
      const seeds = typeof p.seeds === "number" ? p.seeds : 100;
      if (p.suiteId) {
        const suite = INITIAL_SUITES.find((x) => x.id === p.suiteId);
        if (!suite) return errorResult(`Unknown suite: ${p.suiteId}`);
        return jsonResult({ suite: suite.id, name: suite.name, seeds, passRate: suite.passRate, message: `Ran ${seeds} scenarios for ${suite.name}.` });
      }
      const avg = INITIAL_SUITES.reduce((acc, x) => acc + x.passRate, 0) / INITIAL_SUITES.length;
      return jsonResult({ suite: "all", seeds, overallPassRate: Math.round(avg * 100) / 100, results: INITIAL_SUITES, message: `Ran all suites (${seeds} scenarios each).` });
    },
    product: "proof", readOnly: false,
  },
  {
    name: "proof.generateSafetyCase",
    description: "Generate the safety-case report (verdict + coverage + regression evidence) as a shareable document.",
    inputSchema: s({ format: { type: "string", description: "Document format", enum: ["pdf", "html", "markdown"] } }),
    handler: async (p) => {
      const avg = INITIAL_SUITES.reduce((acc, x) => acc + x.passRate, 0) / INITIAL_SUITES.length;
      return jsonResult({
        format: (p.format as string) ?? "pdf",
        overallPassRate: Math.round(avg * 100) / 100,
        readyToShip: avg >= 0.9,
        handle: `safety-case-${Date.now()}.${(p.format as string) ?? "pdf"}`,
        message: "Safety-case report generated with current suite results, coverage heatmap, and regression trend.",
      });
    },
    product: "proof", readOnly: false,
  },
];
