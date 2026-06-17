/*
 * Mock process manager for OhhO Frame.
 */

import { useState, useCallback } from "react";

export interface Process {
  id: string;
  name: string;
  status: "running" | "stopped" | "failed";
  cpu: number;
  ram: number;
  uptime: string;
}

const INITIAL_PROCESSES: Process[] = [
  { id: "p1", name: "omnivla_node", status: "running", cpu: 45.2, ram: 2048, uptime: "4h 12m" },
  { id: "p2", name: "nav2_stack", status: "running", cpu: 12.4, ram: 512, uptime: "4h 12m" },
  { id: "p3", name: "yahboom_driver", status: "running", cpu: 3.1, ram: 128, uptime: "4h 12m" },
  { id: "p4", name: "rosbridge_ws", status: "running", cpu: 2.0, ram: 256, uptime: "4h 12m" },
  { id: "p5", name: "bev_stitcher", status: "running", cpu: 18.5, ram: 1024, uptime: "4h 12m" },
  { id: "p6", name: "slam_toolbox", status: "stopped", cpu: 0, ram: 0, uptime: "-" },
  { id: "p7", name: "teleop_recorder", status: "failed", cpu: 0, ram: 0, uptime: "-" },
];

export function useProcessManager() {
  const [processes, setProcesses] = useState<Process[]>(INITIAL_PROCESSES);

  const toggleProcess = useCallback((id: string) => {
    setProcesses(prev => prev.map(p => {
      if (p.id === id) {
        const nextStatus = p.status === "running" ? "stopped" : "running";
        return {
          ...p,
          status: nextStatus,
          cpu: nextStatus === "running" ? Math.random() * 10 : 0,
          ram: nextStatus === "running" ? 100 + Math.random() * 500 : 0,
          uptime: nextStatus === "running" ? "0m 1s" : "-",
        };
      }
      return p;
    }));
  }, []);

  const restartProcess = useCallback((id: string) => {
    setProcesses(prev => prev.map(p => {
      if (p.id === id) {
        return { ...p, status: "stopped", cpu: 0, ram: 0, uptime: "-" };
      }
      return p;
    }));

    setTimeout(() => {
      setProcesses(prev => prev.map(p => {
        if (p.id === id) {
          return { ...p, status: "running", cpu: Math.random() * 20, ram: 200 + Math.random() * 400, uptime: "0m 1s" };
        }
        return p;
      }));
    }, 1000);
  }, []);

  return { processes, toggleProcess, restartProcess };
}
