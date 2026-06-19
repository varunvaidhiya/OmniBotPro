/*
 * OhhO Build — design state.
 *
 * A "design" is the user's robot: which parts are selected per category plus the
 * requirements they're targeting. This module owns the design shape, the starter
 * templates, the reducer that mutates it, and compact URL/localStorage
 * (de)serialization so a design is shareable on a statically-exported site.
 */

import { CATEGORIES, getPart, type Category, type Environment } from "./catalog";

export interface Requirements {
  payload: number; // kg of cargo to carry
  reach: number; // m manipulator reach (0 = no manipulation)
  topSpeed: number; // m/s
  runtime: number; // h between charges
  footprint: number; // m — max base dimension allowed
  environment: Environment;
}

/** Selected part ids per category. Single categories hold 0–1; sensors hold N. */
export type Selection = Partial<Record<Category, string[]>>;

export interface Design {
  name: string;
  selection: Selection;
  requirements: Requirements;
}

export const DEFAULT_REQUIREMENTS: Requirements = {
  payload: 5,
  reach: 0.6,
  topSpeed: 1.2,
  runtime: 6,
  footprint: 0.4,
  environment: "indoor",
};

// ── Starter templates ───────────────────────────────────────────────────────

export interface Template {
  id: string;
  name: string;
  blurb: string;
  design: Design;
}

export const TEMPLATES: Template[] = [
  {
    id: "warehouse-amr",
    name: "Warehouse AMR",
    blurb: "Mecanum mobile manipulator — the OmniBot reference build.",
    design: {
      name: "Warehouse AMR",
      selection: {
        base: ["base-yahboom-x3"],
        drive: ["drive-mecanum-4"],
        power: ["power-4s-10ah"],
        compute: ["compute-orin-nano"],
        arm: ["arm-so101"],
        gripper: ["gripper-parallel"],
        sensor: ["sensor-astra", "sensor-lidar-2d", "sensor-imu"],
      },
      requirements: { payload: 3, reach: 0.6, topSpeed: 1.2, runtime: 3, footprint: 0.45, environment: "indoor" },
    },
  },
  {
    id: "lab-automation",
    name: "Lab Automation",
    blurb: "Compact cleanroom arm-on-base for benchtop pick-and-place.",
    design: {
      name: "Lab Automation Cell",
      selection: {
        base: ["base-nano"],
        drive: ["drive-diff-2"],
        power: ["power-3s-5ah"],
        compute: ["compute-pi5"],
        arm: ["arm-so100-5dof"],
        gripper: ["gripper-soft"],
        sensor: ["sensor-wrist-cam", "sensor-rgb-front"],
      },
      requirements: { payload: 1, reach: 0.5, topSpeed: 0.5, runtime: 4, footprint: 0.25, environment: "cleanroom" },
    },
  },
  {
    id: "inspection-rover",
    name: "Inspection Rover",
    blurb: "Sealed outdoor rover with 3-D LiDAR and big-model compute.",
    design: {
      name: "Inspection Rover",
      selection: {
        base: ["base-ip65"],
        drive: ["drive-offroad-4"],
        power: ["power-lfp-48"],
        compute: ["compute-agx-orin"],
        sensor: ["sensor-lidar-3d", "sensor-rgb-front", "sensor-imu"],
      },
      requirements: { payload: 10, reach: 0, topSpeed: 2.0, runtime: 8, footprint: 0.6, environment: "outdoor" },
    },
  },
  {
    id: "education-kit",
    name: "Education Kit",
    blurb: "Affordable learn-to-build robot for the classroom.",
    design: {
      name: "Education Kit",
      selection: {
        base: ["base-nano"],
        drive: ["drive-diff-2"],
        power: ["power-3s-5ah"],
        compute: ["compute-pi5"],
        arm: ["arm-so100-5dof"],
        gripper: ["gripper-parallel"],
        sensor: ["sensor-astra", "sensor-imu"],
      },
      requirements: { payload: 1, reach: 0.5, topSpeed: 0.6, runtime: 3, footprint: 0.25, environment: "indoor" },
    },
  },
  {
    id: "blank",
    name: "Blank Canvas",
    blurb: "Start from nothing and assemble part by part.",
    design: {
      name: "Untitled Robot",
      selection: {},
      requirements: { ...DEFAULT_REQUIREMENTS },
    },
  },
];

export function templateById(id: string): Template | undefined {
  return TEMPLATES.find((t) => t.id === id);
}

export const DEFAULT_DESIGN: Design = clone(TEMPLATES[0].design);

function clone<T>(v: T): T {
  return JSON.parse(JSON.stringify(v)) as T;
}

// ── Selection helpers ───────────────────────────────────────────────────────

export function selectedIds(design: Design, cat: Category): string[] {
  return design.selection[cat] ?? [];
}

/** The single chosen part id for a single-select category, or undefined. */
export function singleSelected(design: Design, cat: Category): string | undefined {
  return selectedIds(design, cat)[0];
}

export function isSelected(design: Design, cat: Category, partId: string): boolean {
  return selectedIds(design, cat).includes(partId);
}

// ── Reducer ─────────────────────────────────────────────────────────────────

export type Action =
  | { type: "loadTemplate"; templateId: string }
  | { type: "loadDesign"; design: Design }
  | { type: "togglePart"; category: Category; partId: string }
  | { type: "selectSingle"; category: Category; partId: string }
  | { type: "removeCategory"; category: Category }
  | { type: "setRequirement"; key: keyof Requirements; value: Requirements[keyof Requirements] }
  | { type: "setName"; name: string };

export function reducer(design: Design, action: Action): Design {
  switch (action.type) {
    case "loadTemplate": {
      const t = templateById(action.templateId);
      return t ? clone(t.design) : design;
    }
    case "loadDesign":
      return clone(action.design);
    case "setName":
      return { ...design, name: action.name };
    case "setRequirement":
      return { ...design, requirements: { ...design.requirements, [action.key]: action.value } };
    case "removeCategory": {
      const selection = { ...design.selection };
      delete selection[action.category];
      return pruneGripper({ ...design, selection });
    }
    case "selectSingle": {
      const cur = selectedIds(design, action.category);
      // clicking the already-selected single part clears it (optional categories)
      const next = cur[0] === action.partId ? [] : [action.partId];
      return pruneGripper({ ...design, selection: { ...design.selection, [action.category]: next } });
    }
    case "togglePart": {
      const cur = selectedIds(design, action.category);
      const next = cur.includes(action.partId)
        ? cur.filter((id) => id !== action.partId)
        : [...cur, action.partId];
      return { ...design, selection: { ...design.selection, [action.category]: next } };
    }
    default:
      return design;
  }
}

/** A gripper without an arm is meaningless — drop it if the arm goes away. */
function pruneGripper(design: Design): Design {
  if (selectedIds(design, "gripper").length && !selectedIds(design, "arm").length) {
    const selection = { ...design.selection };
    delete selection.gripper;
    return { ...design, selection };
  }
  return design;
}

// ── (De)serialization for shareable URLs + autosave ─────────────────────────
//
// Designs are encoded to a compact, URL-safe base64 string so they can ride in
// a ?d= query param — no backend required on a static export.

export function encodeDesign(design: Design): string {
  const json = JSON.stringify(design);
  if (typeof window === "undefined") return "";
  return base64UrlEncode(json);
}

export function decodeDesign(encoded: string): Design | null {
  try {
    const json = base64UrlDecode(encoded);
    const parsed = JSON.parse(json) as Design;
    return sanitize(parsed);
  } catch {
    return null;
  }
}

/** Defensive: drop unknown part ids / fill missing requirement fields. */
export function sanitize(d: { name?: string; selection?: Selection; requirements?: Partial<Requirements> }): Design {
  const selection: Selection = {};
  for (const c of CATEGORIES) {
    const ids = (d.selection?.[c.key] ?? []).filter((id) => getPart(id)?.category === c.key);
    if (ids.length) selection[c.key] = c.single ? ids.slice(0, 1) : ids;
  }
  return {
    name: typeof d.name === "string" && d.name.trim() ? d.name : "Untitled Robot",
    selection,
    requirements: { ...DEFAULT_REQUIREMENTS, ...(d.requirements ?? {}) },
  };
}

function base64UrlEncode(s: string): string {
  const bytes = new TextEncoder().encode(s);
  const b64 = btoa(Array.from(bytes, (b) => String.fromCharCode(b)).join(""));
  return b64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function base64UrlDecode(s: string): string {
  const b64 = s.replace(/-/g, "+").replace(/_/g, "/");
  const bytes = Uint8Array.from(atob(b64), (c) => c.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}
