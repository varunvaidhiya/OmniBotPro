/*
 * Simulated autonomy state for OhhO Autonomy.
 *
 * A natural-language instruction is turned into a mission (a small state
 * machine of navigate/perceive/manipulate steps); starting it animates the
 * robot along a planned Nav2 path on the SLAM map, completing steps as it
 * goes. A control-mode mux arbitrates nav / AI / teleop. All client-side.
 */

import { useCallback, useEffect, useRef, useState } from "react";

import type { RobotConfig } from "@/lib/garage/robot-config";

export type ControlMode = "nav2" | "vla" | "rl_nav" | "teleop";
export type StepKind = "navigate" | "perceive" | "manipulate";
export type Phase = "idle" | "running" | "done";

export interface MissionStep {
  id: string;
  label: string;
  kind: StepKind;
  status: "queued" | "running" | "done";
}

export interface Pt {
  x: number;
  y: number;
}

/** Planned path through the mapped space, normalised to [0,1]. */
export const PATH: Pt[] = [
  { x: 0.13, y: 0.85 },
  { x: 0.22, y: 0.62 },
  { x: 0.36, y: 0.5 },
  { x: 0.52, y: 0.43 },
  { x: 0.68, y: 0.31 },
  { x: 0.82, y: 0.2 },
];

export const NAMED_LOCATIONS = ["kitchen", "bench", "dock", "shelf-3", "lab"];

/** A sensible starting instruction for the robot's capabilities. */
function defaultInstruction(config: RobotConfig): string {
  if (config.capabilities.canManipulate) return "take the red cup from the kitchen to the bench";
  if (config.capabilities.isAerial) return "survey the lab then return to the dock";
  return "navigate from the kitchen to the bench";
}

const TOTAL_DIST_M = 6.2;

function interpolate(p: number): Pt {
  if (p <= 0) return PATH[0];
  if (p >= 1) return PATH[PATH.length - 1];
  // cumulative segment lengths
  const segs: number[] = [];
  let total = 0;
  for (let i = 1; i < PATH.length; i++) {
    const d = Math.hypot(PATH[i].x - PATH[i - 1].x, PATH[i].y - PATH[i - 1].y);
    segs.push(d);
    total += d;
  }
  let target = p * total;
  for (let i = 0; i < segs.length; i++) {
    if (target <= segs[i]) {
      const f = segs[i] === 0 ? 0 : target / segs[i];
      return {
        x: PATH[i].x + (PATH[i + 1].x - PATH[i].x) * f,
        y: PATH[i].y + (PATH[i + 1].y - PATH[i].y) * f,
      };
    }
    target -= segs[i];
  }
  return PATH[PATH.length - 1];
}

/** Build a mission from a plain-language instruction (toy parser). */
function planFrom(text: string, canManipulate: boolean): MissionStep[] {
  const t = text.toLowerCase();
  const from = NAMED_LOCATIONS.find((l) => t.includes(l)) ?? "kitchen";
  const to = NAMED_LOCATIONS.filter((l) => l !== from).find((l) => t.includes(l)) ?? "bench";
  const objMatch = t.match(/(?:the\s+)?([a-z]+\s)?(cup|box|bottle|tool|part|tray)/);
  const obj = objMatch ? objMatch[0].replace(/^the\s+/, "").trim() : "object";
  const steps: MissionStep[] = [
    { id: "s1", label: `navigate · ${from}`, kind: "navigate", status: "queued" },
    { id: "s2", label: `${canManipulate ? "detect" : "inspect"} · ${obj}`, kind: "perceive", status: "queued" },
  ];
  // Only robots with a manipulator get a pick/place step.
  if (canManipulate && /pick|grab|take|fetch|bring|move/.test(t)) {
    steps.push({ id: "s3", label: `pick · ${obj}`, kind: "manipulate", status: "queued" });
  }
  steps.push({ id: "s4", label: `navigate · ${to}`, kind: "navigate", status: "queued" });
  return steps;
}

export function useMission(config: RobotConfig) {
  const canManipulate = config.capabilities.canManipulate;
  const [instruction, setInstruction] = useState(() => defaultInstruction(config));
  const [steps, setSteps] = useState<MissionStep[]>(() => planFrom(defaultInstruction(config), canManipulate));
  const [mode, setMode] = useState<ControlMode>("nav2");
  const [phase, setPhase] = useState<Phase>("idle");
  const [progress, setProgress] = useState(0);
  const timer = useRef<ReturnType<typeof setInterval> | null>(null);

  const stop = useCallback(() => {
    if (timer.current) clearInterval(timer.current);
    timer.current = null;
  }, []);
  useEffect(() => stop, [stop]);

  // Re-plan the default mission when the selected robot changes.
  useEffect(() => {
    const di = defaultInstruction(config);
    setInstruction(di);
    setSteps(planFrom(di, canManipulate));
    setProgress(0);
    setPhase("idle");
    stop();
  }, [config, canManipulate, stop]);

  // mark steps complete as progress crosses their slice
  useEffect(() => {
    setSteps((prev) => {
      const n = prev.length;
      return prev.map((s, i) => {
        const startF = i / n;
        const endF = (i + 1) / n;
        let status: MissionStep["status"] = "queued";
        if (progress >= endF - 0.001) status = "done";
        else if (progress >= startF) status = "running";
        return { ...s, status };
      });
    });
  }, [progress]);

  const start = useCallback(() => {
    if (phase === "done") setProgress(0);
    setPhase("running");
    setMode("nav2");
    stop();
    timer.current = setInterval(() => {
      setProgress((p) => {
        const next = Math.min(1, p + 0.012 + Math.random() * 0.006);
        if (next >= 1) {
          stop();
          setPhase("done");
        }
        return next;
      });
    }, 120);
  }, [phase, stop]);

  const submit = useCallback(
    (text: string) => {
      stop();
      setInstruction(text);
      setSteps(planFrom(text, canManipulate));
      setProgress(0);
      setPhase("idle");
    },
    [stop, canManipulate],
  );

  const reset = useCallback(() => {
    stop();
    setProgress(0);
    setPhase("idle");
  }, [stop]);

  const robot = interpolate(progress);
  const distToGoal = Math.max(0, (1 - progress) * TOTAL_DIST_M);
  const eta = Math.ceil((1 - progress) * 14);

  return {
    instruction,
    steps,
    mode,
    setMode,
    phase,
    progress,
    robot,
    distToGoal,
    eta,
    start,
    submit,
    reset,
  };
}
