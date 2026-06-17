/*
 * Mock fleet data and alerts for OhhO Fleet.
 */

export type RobotStatus = "online" | "degraded" | "offline";
export type AlertSeverity = "critical" | "warning" | "info";

export interface Robot {
  id: string;
  status: RobotStatus;
  battery: number;
  version: string;
  x: number;
  y: number;
  targetX: number;
  targetY: number;
  speed: number;
}

export interface Alert {
  id: string;
  title: string;
  description: string;
  severity: AlertSeverity;
  time: string;
}

const W = 400;
const H = 300;

export const VERSIONS = {
  STABLE: "v2.3.1",
  CANARY: "v2.4.0-rc.1",
};

export function generateInitialFleet(count: number = 24): Robot[] {
  const robots: Robot[] = [];
  
  for (let i = 0; i < count; i++) {
    const isOffline = i === 3 || i === 14;
    const isDegraded = i === 7 || i === 20 || i === 11;
    
    let status: RobotStatus = "online";
    if (isOffline) status = "offline";
    if (isDegraded) status = "degraded";

    // 15% of online/degraded robots are on canary
    const version = (status !== "offline" && Math.random() < 0.15) ? VERSIONS.CANARY : VERSIONS.STABLE;
    
    robots.push({
      id: `amr-${(i + 1).toString().padStart(2, "0")}`,
      status,
      battery: isOffline ? 0 : Math.floor(40 + Math.random() * 60),
      version,
      x: Math.random() * W,
      y: Math.random() * H,
      targetX: Math.random() * W,
      targetY: Math.random() * H,
      speed: status === "online" ? 0.5 + Math.random() * 0.5 : (status === "degraded" ? 0.2 : 0),
    });
  }
  
  return robots;
}

export const INITIAL_ALERTS: Alert[] = [
  {
    id: "a1",
    title: "amr-04 · offline",
    description: "Robot dropped off network 4m ago.",
    severity: "critical",
    time: "4m ago",
  },
  {
    id: "a2",
    title: "amr-15 · offline",
    description: "Robot dropped off network 12m ago.",
    severity: "critical",
    time: "12m ago",
  },
  {
    id: "a3",
    title: "amr-08 · VLA latency ↑",
    description: "Inference time spiked above 150ms.",
    severity: "warning",
    time: "1h ago",
  },
  {
    id: "a4",
    title: "fleet · OTA canary",
    description: "15% of fleet successfully updated to v2.4.0-rc.1.",
    severity: "info",
    time: "2h ago",
  },
];
