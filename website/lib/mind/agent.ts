/*
 * Simulated agent state for OhhO Mind — the deliberative brain console.
 *
 * A plain-language goal is turned into a transcript of the continuous agent
 * loop (perceive → reason → verify → act → monitor → … → reflect → remember).
 * Running it streams the loop's reasoning events, advances the fused world
 * state, gates unsafe actions, and — on success — stores an episode and writes
 * a fact back to memory. All client-side; no robot or network required.
 */

import { useCallback, useEffect, useRef, useState } from "react";

export type LoopPhase =
  | "perceive"
  | "reason"
  | "verify"
  | "act"
  | "monitor"
  | "reflect"
  | "remember";
export type AgentPhase = "idle" | "running" | "done";
export type Backend = "cloud" | "on_device" | "deepx";
export type Tone = "info" | "accent" | "good" | "warn" | "bad";

export const LOOP_PHASES: LoopPhase[] = [
  "perceive",
  "reason",
  "verify",
  "act",
  "monitor",
  "reflect",
  "remember",
];

export interface LoopEvent {
  id: number;
  phase: LoopPhase;
  text: string;
  tone: Tone;
}

export interface WorldState {
  basePose: string;
  arm: string;
  nearest: string;
  missionPhase: string;
}

interface Step {
  phase: LoopPhase;
  text: string;
  tone: Tone;
  world?: Partial<WorldState>;
  blocked?: boolean;
  learns?: string;
}

const NAMED = ["kitchen", "bench", "dock", "shelf-3", "lab", "home"];
const IDLE_WORLD: WorldState = {
  basePose: "dock",
  arm: "home · open",
  nearest: "—",
  missionPhase: "idle",
};

function parseGoal(text: string): { loc: string; obj: string } {
  const t = text.toLowerCase();
  const loc = NAMED.find((l) => t.includes(l)) ?? "kitchen";
  const m = t.match(/(red |blue |green |yellow )?(cup|box|bottle|tool|part|tray|mug)/);
  const obj = m ? `${m[1] ?? ""}${m[2]}`.trim() : "object";
  return { loc, obj };
}

/** Turn a goal into the loop transcript the console streams. */
function buildTranscript(goal: string, unsafe: boolean): Step[] {
  const { loc, obj } = parseGoal(goal);
  const objId = obj.replace(/\s+/g, "_");
  const steps: Step[] = [];

  const cycle = (
    rationale: string,
    tool: string,
    opts: { world?: Partial<WorldState>; blocked?: boolean } = {},
  ) => {
    steps.push({ phase: "perceive", text: "fused /agent/world_state snapshot", tone: "info" });
    steps.push({ phase: "reason", text: rationale, tone: "accent" });
    if (opts.blocked) {
      steps.push({
        phase: "verify",
        text: `safety gate BLOCKED ${tool} — exceeds hardware limits; re-planning`,
        tone: "bad",
      });
      return; // no act / monitor — the loop re-plans a safe action
    }
    steps.push({ phase: "verify", text: `${tool} → within hardware limits ✓`, tone: "good" });
    steps.push({ phase: "act", text: tool, tone: "info", world: opts.world });
    steps.push({ phase: "monitor", text: "watching /mission/status …", tone: "info" });
  };

  cycle(`Goal needs the ${obj}; navigate to the ${loc} first.`, `navigate_to("${loc}")`, {
    world: { basePose: loc, missionPhase: "navigating" },
  });
  cycle(`Arrived at ${loc}. Look for the ${obj}.`, `run_skill(vla, "detect the ${obj}")`, {
    world: { missionPhase: "perceiving", nearest: `${objId} 0.42 m` },
  });
  if (unsafe) {
    cycle(`Lunge straight at it to save time.`, `drive(vx=0.90)`, { blocked: true });
  }
  cycle(`${objId} is in reach; pick it up precisely.`, `run_skill(rl_arm, "pick up the ${obj}")`, {
    world: { arm: `holding ${objId}`, missionPhase: "manipulating" },
  });
  cycle(`Have the ${obj} — return home.`, `navigate_to("home")`, {
    world: { basePose: "home", missionPhase: "navigating" },
  });

  steps.push({
    phase: "reflect",
    text: "goal complete — language-goal judge: success (0.93)",
    tone: "good",
    world: { missionPhase: "done" },
  });
  steps.push({
    phase: "remember",
    text: `episode stored → OhhO Train · learned ${objId} @ ${loc}`,
    tone: "accent",
    learns: `${objId} → ${loc}`,
  });
  return steps;
}

const TICK_MS = 750;
const START_EPISODES = 127;

export function useAgentLoop() {
  const [goal, setGoal] = useState("find the red cup and bring it back");
  const [phase, setPhase] = useState<AgentPhase>("idle");
  const [events, setEvents] = useState<LoopEvent[]>([]);
  const [currentPhase, setCurrentPhase] = useState<LoopPhase | null>(null);
  const [world, setWorld] = useState<WorldState>(IDLE_WORLD);
  const [online, setOnline] = useState(true);
  const [unsafe, setUnsafe] = useState(false);
  const [episodes, setEpisodes] = useState(START_EPISODES);
  const [memory, setMemory] = useState<string[]>(["mug → lab", "tray → shelf-3"]);
  const [blocked, setBlocked] = useState(0);

  const transcript = useRef<Step[]>([]);
  const cursor = useRef(0);
  const idc = useRef(0);
  const timer = useRef<ReturnType<typeof setInterval> | null>(null);

  const stop = useCallback(() => {
    if (timer.current) clearInterval(timer.current);
    timer.current = null;
  }, []);
  useEffect(() => stop, [stop]);

  const reset = useCallback(() => {
    stop();
    setPhase("idle");
    setEvents([]);
    setCurrentPhase(null);
    setWorld(IDLE_WORLD);
    cursor.current = 0;
    transcript.current = [];
  }, [stop]);

  const start = useCallback(() => {
    stop();
    transcript.current = buildTranscript(goal, unsafe);
    cursor.current = 0;
    setEvents([]);
    setCurrentPhase(null);
    setWorld({ ...IDLE_WORLD, missionPhase: "active" });
    setPhase("running");
    timer.current = setInterval(() => {
      const i = cursor.current;
      const steps = transcript.current;
      if (i >= steps.length) {
        stop();
        setPhase("done");
        setCurrentPhase(null);
        setEpisodes((e) => e + 1);
        const last = steps[steps.length - 1];
        if (last?.learns) {
          const fact = last.learns;
          setMemory((m) => (m.includes(fact) ? m : [fact, ...m]));
        }
        return;
      }
      const s = steps[i];
      cursor.current = i + 1;
      setCurrentPhase(s.phase);
      setEvents((ev) => [...ev, { id: idc.current++, phase: s.phase, text: s.text, tone: s.tone }]);
      if (s.world) setWorld((w) => ({ ...w, ...s.world }));
      if (s.blocked) setBlocked((b) => b + 1);
    }, TICK_MS);
  }, [goal, unsafe, stop]);

  const submit = useCallback(
    (text: string) => {
      setGoal(text);
      reset();
    },
    [reset],
  );

  const activeBackend: Backend = online ? "cloud" : "deepx";

  return {
    goal,
    phase,
    events,
    currentPhase,
    world,
    online,
    setOnline,
    unsafe,
    setUnsafe,
    episodes,
    memory,
    blocked,
    activeBackend,
    start,
    reset,
    submit,
  };
}
