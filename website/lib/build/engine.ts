/*
 * OhhO Build — validation & recommendation engine.
 *
 * Pure, deterministic functions that turn a Design into:
 *   • computeMetrics()   — derived physical numbers (mass, payload, runtime…)
 *   • validate()         — pass / warn / fail checks against the requirements
 *   • recommend()        — AI-style suggestions, each with an applicable action
 *
 * No React, no I/O — everything here is testable in isolation and drives both
 * the UI panels and the export headers.
 */

import {
  getPart,
  partsByCategory,
  type Category,
  type Part,
} from "./catalog";
import {
  selectedIds,
  singleSelected,
  type Action,
  type Design,
  type Requirements,
} from "./design";

// Average-power duty factors: components rarely all draw peak at once.
const DUTY = { drive: 0.5, arm: 0.3, gripper: 0.1, compute: 1, sensor: 1 } as const;
const GROUND_CLEARANCE = 0.05; // m — assumed chassis ride height for CoM maths

export interface Metrics {
  parts: Part[];
  count: number;
  totalMass: number; // kg
  totalPrice: number; // USD
  maxLeadTimeDays: number;
  /** Cargo capacity left after mounting everything on the chassis. */
  usablePayload: number; // kg
  ratedPayload: number; // kg — base rating
  mountedMass: number; // kg — arm + gripper + sensors + compute on the deck
  armPayload: number; // kg — lift capacity at the gripper
  reach: number; // m
  topSpeed: number; // m/s
  avgPowerDraw: number; // W
  runtime: number; // h (Infinity-safe → 0 when no draw/power)
  footprint: number; // m — max base dimension
  tops: number; // AI TOPS available
  comHeight: number; // m — estimated centre-of-mass height
  stability: number; // 0..1 (1 = rock solid)
}

function resolve(design: Design): Part[] {
  const out: Part[] = [];
  for (const cat of Object.keys(design.selection) as Category[]) {
    for (const id of selectedIds(design, cat)) {
      const p = getPart(id);
      if (p) out.push(p);
    }
  }
  return out;
}

export function computeMetrics(design: Design): Metrics {
  const parts = resolve(design);
  const base = part(design, "base");
  const drive = part(design, "drive");
  const power = part(design, "power");
  const compute = part(design, "compute");
  const arm = part(design, "arm");
  const gripper = part(design, "gripper");
  const sensors = selectedIds(design, "sensor").map(getPart).filter(Boolean) as Part[];

  const totalMass = round(parts.reduce((s, p) => s + p.mass, 0), 2);
  const totalPrice = parts.reduce((s, p) => s + p.price, 0);
  const maxLeadTimeDays = parts.reduce((m, p) => Math.max(m, p.leadTimeDays), 0);

  // mounted = everything that loads the deck (not the chassis/drive/battery)
  const mountedMass =
    (arm?.mass ?? 0) + (gripper?.mass ?? 0) + (compute?.mass ?? 0) + sensors.reduce((s, p) => s + p.mass, 0);
  const ratedPayload = base?.payload ?? 0;
  const usablePayload = round(ratedPayload - mountedMass, 2);

  const armPayload = arm ? Math.min(arm.payload ?? 0, gripper?.payload ?? arm.payload ?? 0) : 0;

  const reach = arm?.reach ?? 0;
  const topSpeed = drive?.topSpeed ?? 0;

  const avgPowerDraw = round(
    (drive?.powerDraw ?? 0) * DUTY.drive +
      (arm?.powerDraw ?? 0) * DUTY.arm +
      (gripper?.powerDraw ?? 0) * DUTY.gripper +
      (compute?.powerDraw ?? 0) * DUTY.compute +
      sensors.reduce((s, p) => s + (p.powerDraw ?? 0), 0) * DUTY.sensor,
    1,
  );

  const capacityWh = power?.capacityWh ?? 0;
  const runtime = avgPowerDraw > 0 && capacityWh > 0 ? round(capacityWh / avgPowerDraw, 1) : 0;

  const footprint = base ? Math.max(base.size[0], base.size[1]) : 0;
  const tops = compute?.tops ?? 0;

  const { comHeight, stability } = stabilityOf(base, parts);

  return {
    parts,
    count: parts.length,
    totalMass,
    totalPrice,
    maxLeadTimeDays,
    usablePayload,
    ratedPayload,
    mountedMass: round(mountedMass, 2),
    armPayload,
    reach,
    topSpeed,
    avgPowerDraw,
    runtime,
    footprint,
    tops,
    comHeight: round(comHeight, 3),
    stability: round(stability, 2),
  };
}

/** Mass-weighted centre-of-mass height → a 0..1 tip-over stability score. */
function stabilityOf(base: Part | undefined, parts: Part[]) {
  if (!base) return { comHeight: 0, stability: 0 };
  const baseTop = GROUND_CLEARANCE + base.size[2];
  const armReach = parts.find((p) => p.category === "arm")?.reach ?? 0.3;
  let massSum = 0;
  let momentSum = 0;
  const add = (m: number, z: number) => {
    massSum += m;
    momentSum += m * z;
  };
  for (const p of parts) {
    switch (p.category) {
      case "base":
        add(p.mass, GROUND_CLEARANCE + base.size[2] / 2);
        break;
      case "drive":
        add(p.mass, GROUND_CLEARANCE / 2);
        break;
      case "power":
        add(p.mass, GROUND_CLEARANCE + p.size[2] / 2); // batteries ride low
        break;
      case "compute":
        add(p.mass, baseTop + p.size[2] / 2);
        break;
      case "arm":
        // stowed/travel pose: the arm's mass sits well below its full extension
        add(p.mass, baseTop + (p.size[2] ?? 0.3) * 0.3); // tall + heavy → raises CoM
        break;
      case "gripper":
        add(p.mass, baseTop + armReach * 0.5);
        break;
      case "sensor":
        add(p.mass, baseTop + 0.06);
        break;
    }
  }
  const comHeight = massSum > 0 ? momentSum / massSum : baseTop;
  const footprintMin = Math.min(base.size[0], base.size[1]);
  // stable while CoM stays inside ~0.4×min-footprint; degrades to 0 by 1.2×
  const ratio = comHeight / Math.max(footprintMin, 1e-3);
  const stability = clamp(1 - (ratio - 0.4) / 0.8, 0, 1);
  return { comHeight, stability };
}

// ── Validation ──────────────────────────────────────────────────────────────

export type Status = "pass" | "warn" | "fail";

export interface Check {
  id: string;
  label: string;
  status: Status;
  detail: string;
}

export function validate(design: Design, m: Metrics): Check[] {
  const r = design.requirements;
  const checks: Check[] = [];
  const arm = part(design, "arm");
  const gripper = part(design, "gripper");

  // 1. required subsystems present
  for (const cat of ["base", "drive", "power", "compute"] as Category[]) {
    if (!singleSelected(design, cat)) {
      checks.push({ id: `req-${cat}`, label: `${cap(cat)} fitted`, status: "fail", detail: `Every robot needs a ${cat}. Add one from the palette.` });
    }
  }

  // 2. payload (cargo capacity)
  if (m.ratedPayload > 0) {
    checks.push(meets("payload", "Payload", m.usablePayload, r.payload, "kg", 0.8));
  }

  // 3. reach / manipulation
  if (r.reach > 0) {
    if (!arm) {
      checks.push({ id: "reach", label: "Reach", status: "fail", detail: `Target reach is ${r.reach} m but no arm is fitted.` });
    } else {
      checks.push(meets("reach", "Reach", m.reach, r.reach, "m", 0.9));
    }
  }

  // 4. top speed
  if (r.topSpeed > 0) checks.push(meets("speed", "Top speed", m.topSpeed, r.topSpeed, "m/s", 0.85));

  // 5. runtime
  if (r.runtime > 0) checks.push(meets("runtime", "Runtime", m.runtime, r.runtime, "h", 0.85));

  // 6. footprint (smaller is better → fits the requirement)
  if (m.footprint > 0) {
    const status: Status = m.footprint <= r.footprint ? "pass" : m.footprint <= r.footprint * 1.15 ? "warn" : "fail";
    checks.push({ id: "footprint", label: "Footprint", status, detail: `Base is ${m.footprint.toFixed(2)} m across; budget is ${r.footprint.toFixed(2)} m.` });
  }

  // 7. stability
  if (m.parts.length) {
    const status: Status = m.stability >= 0.6 ? "pass" : m.stability >= 0.35 ? "warn" : "fail";
    checks.push({ id: "stability", label: "Stability", status, detail: `Centre of mass at ${(m.comHeight * 100).toFixed(0)} cm — stability score ${(m.stability * 100).toFixed(0)}%.` });
  }

  // 8. gripper needs an arm
  if (gripper && !arm) {
    checks.push({ id: "gripper-arm", label: "Gripper mount", status: "fail", detail: "A gripper needs an arm to mount on." });
  }

  // 9. arm vs base payload headroom
  if (arm && m.usablePayload < 0) {
    checks.push({ id: "overloaded", label: "Deck load", status: "fail", detail: `Mounted gear (${m.mountedMass} kg) exceeds the base's ${m.ratedPayload} kg rating.` });
  }

  // 10. environment rating
  const offenders = m.parts.filter((p) => !p.env.includes(r.environment));
  if (offenders.length) {
    checks.push({
      id: "environment",
      label: "Environment",
      status: "warn",
      detail: `${offenders.length} part${offenders.length > 1 ? "s are" : " is"} not rated for ${r.environment}: ${offenders.map((p) => p.name).join(", ")}.`,
    });
  } else if (m.parts.length) {
    checks.push({ id: "environment", label: "Environment", status: "pass", detail: `All parts rated for ${r.environment} operation.` });
  }

  return checks;
}

function meets(id: string, label: string, value: number, target: number, unit: string, warnFrac: number): Check {
  const status: Status = value >= target ? "pass" : value >= target * warnFrac ? "warn" : "fail";
  return { id, label, status, detail: `${value}${unit} available vs ${target}${unit} required.` };
}

export function overallStatus(checks: Check[]): Status {
  if (checks.some((c) => c.status === "fail")) return "fail";
  if (checks.some((c) => c.status === "warn")) return "warn";
  return "pass";
}

// ── Recommendations ─────────────────────────────────────────────────────────

export interface Recommendation {
  id: string;
  severity: "fix" | "improve" | "ok";
  title: string;
  detail: string;
  /** A reducer action that applies the suggestion, plus a button label. */
  action?: { label: string; payload: Action };
}

export function recommend(design: Design, m: Metrics): Recommendation[] {
  const r = design.requirements;
  const recs: Recommendation[] = [];

  // ── fixes: missing required subsystems ──
  for (const cat of ["base", "drive", "power", "compute"] as Category[]) {
    if (!singleSelected(design, cat)) {
      const pick = cheapest(partsByCategory(cat));
      if (pick) {
        recs.push({
          id: `add-${cat}`,
          severity: "fix",
          title: `Add a ${cat}`,
          detail: `A robot can't run without a ${cat}. ${pick.name} is a solid starting point.`,
          action: { label: `Add ${pick.name}`, payload: { type: "selectSingle", category: cat, partId: pick.id } },
        });
      }
    }
  }

  // ── fix: reach required but no arm ──
  if (r.reach > 0 && !part(design, "arm")) {
    const pick = partsByCategory("arm").find((p) => (p.reach ?? 0) >= r.reach) ?? bestBy(partsByCategory("arm"), (p) => p.reach ?? 0);
    if (pick) {
      recs.push({
        id: "add-arm",
        severity: "fix",
        title: "Add a manipulator",
        detail: `You set a ${r.reach} m reach target. ${pick.name} reaches ${pick.reach} m.`,
        action: { label: `Add ${pick.name}`, payload: { type: "selectSingle", category: "arm", partId: pick.id } },
      });
    }
  }

  // ── improve: runtime short → bigger battery ──
  if (r.runtime > 0 && m.runtime < r.runtime && m.avgPowerDraw > 0) {
    const cur = part(design, "power");
    const need = r.runtime * m.avgPowerDraw; // Wh required
    const upgrade = partsByCategory("power")
      .filter((p) => (p.capacityWh ?? 0) > (cur?.capacityWh ?? 0))
      .sort((a, b) => (a.capacityWh ?? 0) - (b.capacityWh ?? 0))
      .find((p) => (p.capacityWh ?? 0) >= need);
    const pick = upgrade ?? bestBy(partsByCategory("power"), (p) => p.capacityWh ?? 0);
    if (pick && pick.id !== cur?.id) {
      const newRuntime = round((pick.capacityWh ?? 0) / m.avgPowerDraw, 1);
      const dMass = round((pick.mass - (cur?.mass ?? 0)) * 1000, 0);
      recs.push({
        id: "swap-power",
        severity: "improve",
        title: "Swap to a bigger pack",
        detail: `${pick.name} lifts runtime to ${newRuntime} h (${pct(newRuntime, m.runtime)}), ${dMass > 0 ? `+${dMass} g` : `${dMass} g`}.`,
        action: { label: "Apply & re-check", payload: { type: "selectSingle", category: "power", partId: pick.id } },
      });
    }
  }

  // ── improve: payload short → stronger base ──
  if (m.ratedPayload > 0 && m.usablePayload < r.payload) {
    const cur = part(design, "base");
    const need = r.payload + m.mountedMass;
    const pick = partsByCategory("base")
      .filter((p) => (p.payload ?? 0) >= need && p.id !== cur?.id)
      .sort((a, b) => a.price - b.price)[0];
    if (pick) {
      recs.push({
        id: "swap-base",
        severity: "improve",
        title: "Use a stronger base",
        detail: `${pick.name} rates ${pick.payload} kg — enough for ${r.payload} kg of cargo plus your mounted gear.`,
        action: { label: "Apply & re-check", payload: { type: "selectSingle", category: "base", partId: pick.id } },
      });
    }
  }

  // ── improve: top speed short → faster drive ──
  if (r.topSpeed > 0 && m.topSpeed < r.topSpeed) {
    const cur = part(design, "drive");
    const pick = partsByCategory("drive")
      .filter((p) => (p.topSpeed ?? 0) >= r.topSpeed && p.id !== cur?.id)
      .sort((a, b) => a.price - b.price)[0];
    if (pick) {
      recs.push({
        id: "swap-drive",
        severity: "improve",
        title: "Fit a faster drivetrain",
        detail: `${pick.name} tops out at ${pick.topSpeed} m/s, clearing your ${r.topSpeed} m/s target.`,
        action: { label: "Apply & re-check", payload: { type: "selectSingle", category: "drive", partId: pick.id } },
      });
    }
  }

  // ── improve: environment mismatch → rated alternative for the worst offender ──
  const offender = m.parts.find((p) => !p.env.includes(r.environment));
  if (offender) {
    const alt = partsByCategory(offender.category).find((p) => p.env.includes(r.environment) && p.id !== offender.id);
    if (alt) {
      recs.push({
        id: `env-${offender.category}`,
        severity: "improve",
        title: `Rate the ${offender.category} for ${r.environment}`,
        detail: `${offender.name} isn't rated for ${r.environment}. ${alt.name} is.`,
        action: { label: `Swap to ${alt.name}`, payload: { type: "selectSingle", category: offender.category, partId: alt.id } },
      });
    }
  }

  // ── improve: low stability → lighter compute ──
  if (m.stability < 0.45 && m.parts.length) {
    const cur = part(design, "compute");
    const lighter = cur && partsByCategory("compute").filter((p) => p.mass < cur.mass).sort((a, b) => (b.tops ?? 0) - (a.tops ?? 0))[0];
    if (lighter) {
      recs.push({
        id: "stability-compute",
        severity: "improve",
        title: "Lower the centre of mass",
        detail: `Stability is ${(m.stability * 100).toFixed(0)}%. A lighter ${lighter.name} on the deck helps it stop tipping.`,
        action: { label: `Swap to ${lighter.name}`, payload: { type: "selectSingle", category: "compute", partId: lighter.id } },
      });
    }
  }

  // ── improve: heavily over-spec'd battery → save cost/weight ──
  if (r.runtime > 0 && m.runtime > r.runtime * 2 && m.avgPowerDraw > 0) {
    const cur = part(design, "power");
    const need = r.runtime * m.avgPowerDraw * 1.25; // keep 25% headroom
    const smaller = partsByCategory("power")
      .filter((p) => (p.capacityWh ?? 0) >= need && (p.capacityWh ?? 0) < (cur?.capacityWh ?? Infinity))
      .sort((a, b) => (b.capacityWh ?? 0) - (a.capacityWh ?? 0))[0];
    if (smaller && cur) {
      recs.push({
        id: "downsize-power",
        severity: "improve",
        title: "Trim an over-sized battery",
        detail: `Runtime is ${m.runtime} h for a ${r.runtime} h target. ${smaller.name} still beats it and saves $${cur.price - smaller.price} and ${round((cur.mass - smaller.mass) * 1000, 0)} g.`,
        action: { label: "Apply & re-check", payload: { type: "selectSingle", category: "power", partId: smaller.id } },
      });
    }
  }

  if (recs.length === 0) {
    recs.push({
      id: "ok",
      severity: "ok",
      title: "Design is valid",
      detail: "Every requirement is met with headroom. Export the BOM and hand it to the OhhO stack.",
    });
  }

  // fixes first, then improvements, cap to keep the panel readable
  const order = { fix: 0, improve: 1, ok: 2 } as const;
  return recs.sort((a, b) => order[a.severity] - order[b.severity]).slice(0, 5);
}

// ── small helpers ─────────────────────────────────────────────────────────

function part(design: Design, cat: Category): Part | undefined {
  const id = singleSelected(design, cat);
  return id ? getPart(id) : undefined;
}
function cheapest(parts: Part[]): Part | undefined {
  return [...parts].sort((a, b) => a.price - b.price)[0];
}
function bestBy(parts: Part[], score: (p: Part) => number): Part | undefined {
  return [...parts].sort((a, b) => score(b) - score(a))[0];
}
function clamp(v: number, lo: number, hi: number): number {
  return Math.max(lo, Math.min(hi, v));
}
function round(v: number, dp: number): number {
  const f = 10 ** dp;
  return Math.round(v * f) / f;
}
function pct(now: number, before: number): string {
  if (before <= 0) return "new";
  const d = Math.round(((now - before) / before) * 100);
  return `${d >= 0 ? "+" : ""}${d}%`;
}
function cap(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

export type { Requirements };
