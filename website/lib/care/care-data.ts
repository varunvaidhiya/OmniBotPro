/*
 * OhhO Care — pure data types and constants (no React hooks).
 *
 * Safe to import from server-side code (MCP tools, API routes).
 * The degradation hook lives in care-hooks.ts.
 */

export type Urgency = "high" | "medium" | "low";
export type WorkOrderStatus = "open" | "ordered" | "dispatched" | "repaired";

export interface WorkOrder {
  id: string;
  robotId: string;
  component: string;
  symptom: string;
  urgency: Urgency;
  status: WorkOrderStatus;
  /** Estimated days until failure. */
  daysToFailure: number;
  /** Replacement part from the Build BOM. */
  partName: string;
  partCost: number;
  partLeadTimeDays: number;
  partInStock: boolean;
  createdAt: string;
}

export interface RepairLogEntry {
  id: string;
  robotId: string;
  component: string;
  technician: string;
  partBatch: string;
  timestamp: string;
  /** Flows into OhhO Comply audit trail. */
  auditLogged: boolean;
}

export interface FleetMetrics {
  mtbfHours: number;
  mttrHours: number;
  downtimeMonthHours: number;
  openWorkOrders: number;
  predictiveAlerts: number;
  robotsMonitored: number;
}

export const WORK_ORDERS: WorkOrder[] = [
  {
    id: "wo-001",
    robotId: "amr-17",
    component: "Left knee motor",
    symptom: "Temperature trending above threshold — 78°C and rising",
    urgency: "high",
    status: "open",
    daysToFailure: 3,
    partName: "GO-M8010-6 knee motor",
    partCost: 89,
    partLeadTimeDays: 2,
    partInStock: true,
    createdAt: "2h ago",
  },
  {
    id: "wo-002",
    robotId: "amr-05",
    component: "Wrist servo #3",
    symptom: "Torque ripple increased 18% over baseline",
    urgency: "medium",
    status: "ordered",
    daysToFailure: 12,
    partName: "STS3215 servo",
    partCost: 45,
    partLeadTimeDays: 5,
    partInStock: false,
    createdAt: "1d ago",
  },
  {
    id: "wo-003",
    robotId: "amr-22",
    component: "Battery pack",
    symptom: "Capacity degradation — 82% of rated, cycle count 412",
    urgency: "low",
    status: "open",
    daysToFailure: 30,
    partName: "9000mAh smart battery",
    partCost: 220,
    partLeadTimeDays: 7,
    partInStock: true,
    createdAt: "3d ago",
  },
];

export const REPAIR_LOG: RepairLogEntry[] = [
  {
    id: "r-001",
    robotId: "amr-03",
    component: "Front-right wheel motor",
    technician: "J. Martinez",
    partBatch: "BOM-2024-W42",
    timestamp: "2d ago",
    auditLogged: true,
  },
  {
    id: "r-002",
    robotId: "amr-11",
    component: "IMU sensor",
    technician: "S. Chen",
    partBatch: "BOM-2024-W41",
    timestamp: "5d ago",
    auditLogged: true,
  },
  {
    id: "r-003",
    robotId: "amr-07",
    component: "Arm shoulder servo",
    technician: "J. Martinez",
    partBatch: "BOM-2024-W40",
    timestamp: "1w ago",
    auditLogged: true,
  },
];

export const FLEET_METRICS: FleetMetrics = {
  mtbfHours: 412,
  mttrHours: 3.4,
  downtimeMonthHours: 18.2,
  openWorkOrders: 3,
  predictiveAlerts: 1,
  robotsMonitored: 48,
};
