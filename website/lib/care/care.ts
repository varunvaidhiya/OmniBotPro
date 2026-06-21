/*
 * OhhO Care — re-exports data from care-data.ts (pure TS, server-safe) and
 * the degradation hook from care-hooks.ts (React).
 */

export {
  type Urgency,
  type WorkOrderStatus,
  type WorkOrder,
  type RepairLogEntry,
  type FleetMetrics,
  WORK_ORDERS,
  REPAIR_LOG,
  FLEET_METRICS,
} from "./care-data";

export { useDegradationSimulation } from "./care-hooks";
