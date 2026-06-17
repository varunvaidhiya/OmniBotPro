/*
 * Mock standards and compliance data for OhhO Comply.
 */

import { useState, useMemo, useCallback } from "react";

export type RequirementStatus = "done" | "review" | "open";

export interface Requirement {
  id: string;
  clause: string;
  description: string;
  status: RequirementStatus;
  owner: string;
  evidence: boolean;
}

export interface Standard {
  id: string;
  title: string;
  description: string;
  requirements: Requirement[];
}

const INITIAL_STANDARDS: Standard[] = [
  {
    id: "eu-machinery",
    title: "EU Machinery Reg · CE",
    description: "Essential health and safety requirements relating to the design and construction of machinery.",
    requirements: [
      { id: "r1", clause: "1.1.2", description: "Principles of safety integration (hazard elimination).", status: "done", owner: "Sarah J.", evidence: true },
      { id: "r2", clause: "1.2.2", description: "Control devices must be clearly visible and identifiable.", status: "done", owner: "Alex K.", evidence: true },
      { id: "r3", clause: "1.2.4.3", description: "Emergency stop device must have priority over all other commands.", status: "done", owner: "Alex K.", evidence: true },
      { id: "r4", clause: "1.3.7", description: "Risks related to moving parts must be mitigated by guards.", status: "review", owner: "Elena M.", evidence: true },
      { id: "r5", clause: "1.7.1", description: "Information and warnings on the machinery.", status: "open", owner: "Sarah J.", evidence: false },
    ],
  },
  {
    id: "iso-10218",
    title: "ISO 10218-1",
    description: "Safety requirements for industrial robots.",
    requirements: [
      { id: "r6", clause: "5.4.2", description: "Performance requirement for safety-related control systems.", status: "done", owner: "David L.", evidence: true },
      { id: "r7", clause: "5.5.3", description: "Reduced speed control (max 250 mm/s) in manual mode.", status: "done", owner: "David L.", evidence: true },
      { id: "r8", clause: "5.10.1", description: "Collaborative operation requirements (power and force limiting).", status: "review", owner: "Elena M.", evidence: false },
    ],
  },
  {
    id: "iso-13849",
    title: "ISO 13849-1 · PL d",
    description: "Safety-related parts of control systems.",
    requirements: [
      { id: "r9", clause: "4.2.1", description: "Determination of required performance level (PLr).", status: "done", owner: "Alex K.", evidence: true },
      { id: "r10", clause: "4.5", description: "Evaluation of the achieved performance level (PL).", status: "open", owner: "Alex K.", evidence: false },
      { id: "r11", clause: "4.6", description: "Software safety requirements (SRESW).", status: "open", owner: "David L.", evidence: false },
    ],
  },
  {
    id: "ul-60204",
    title: "UL / IEC 60204-1",
    description: "Electrical equipment of machines.",
    requirements: [
      { id: "r12", clause: "5.1", description: "Incoming supply circuit conductor terminations.", status: "review", owner: "Sarah J.", evidence: true },
      { id: "r13", clause: "6.2", description: "Protection against direct contact with live parts.", status: "open", owner: "Sarah J.", evidence: false },
    ],
  },
];

export function useCompliance() {
  const [standards, setStandards] = useState<Standard[]>(INITIAL_STANDARDS);

  const toggleRequirementStatus = useCallback((standardId: string, reqId: string) => {
    setStandards((prev) => 
      prev.map(std => {
        if (std.id !== standardId) return std;
        return {
          ...std,
          requirements: std.requirements.map(req => {
            if (req.id !== reqId) return req;
            const nextStatus: Record<RequirementStatus, RequirementStatus> = {
              "open": "review",
              "review": "done",
              "done": "open"
            };
            return { ...req, status: nextStatus[req.status], evidence: nextStatus[req.status] !== "open" };
          })
        };
      })
    );
  }, []);

  const stats = useMemo(() => {
    let total = 0;
    let done = 0;
    
    standards.forEach(std => {
      total += std.requirements.length;
      done += std.requirements.filter(r => r.status === "done").length;
    });

    return { total, done, percentage: total > 0 ? done / total : 0 };
  }, [standards]);

  return {
    standards,
    stats,
    toggleRequirementStatus
  };
}
