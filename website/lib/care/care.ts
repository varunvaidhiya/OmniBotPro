/*
 * OhhO Care — predictive maintenance data and types.
 *
 * Pure data + the useDegradationSimulation hook. The Care console uses
 * this to render work orders, motor-degradation charts, fleet metrics,
 * and the repair log.
 */

import { useEffect, useRef, useState, useCallback } from "react";

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

// ── Degradation simulation hook ──────────────────────────────────────────────
// Drives a rising motor-temperature chart for the high-urgency work order.

export function useDegradationSimulation(running: boolean) {
  const [temps, setTemps] = useState<number[]>([]);
  const [elapsed, setElapsed] = useState(0);
  const rafRef = useRef<number>(0);
  const lastTime = useRef<number>(0);
  const tickRef = useRef<number>(0);

  // Seed with 40 samples of baseline data
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
          // Trend upward with noise, approaching 85°C threshold
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
