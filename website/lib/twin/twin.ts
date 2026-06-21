/*
 * OhhO Twin — re-exports data from twin-data.ts (pure TS, server-safe) and
 * the simulation hook from twin-hooks.ts (React).
 *
 * This barrel keeps the existing import path (`@/lib/twin/twin`) working
 * for the TwinConsole component while allowing MCP tools to import only
 * the pure-data module (`@/lib/twin/twin-data`) without pulling in React.
 */

export {
  type TwinState,
  type Prediction,
  type WhatIfResult,
  PREDICTIONS,
  WHATIF_RESULTS,
  TWIN_STATS,
} from "./twin-data";

export { useTwinSimulation } from "./twin-hooks";
