import { describe, it, expect, beforeEach, afterEach } from "vitest";

import {
  DEFAULT_DESIGN,
  DEFAULT_REQUIREMENTS,
  TEMPLATES,
  reducer,
  sanitize,
  encodeDesign,
  decodeDesign,
  isSelected,
  selectedIds,
  singleSelected,
  type Design,
} from "./design";

// encodeDesign short-circuits when window is undefined (SSR guard). Stub a
// window so the base64 round-trip can run under the node test environment.
beforeEach(() => {
  (globalThis as Record<string, unknown>).window = {} as unknown;
});
afterEach(() => {
  delete (globalThis as Record<string, unknown>).window;
});

describe("reducer", () => {
  it("toggles a multi-select sensor part", () => {
    const d = reducer(DEFAULT_DESIGN, { type: "togglePart", category: "sensor", partId: "sensor-wrist-cam" });
    expect(isSelected(d, "sensor", "sensor-wrist-cam")).toBe(true);
    const d2 = reducer(d, { type: "togglePart", category: "sensor", partId: "sensor-wrist-cam" });
    expect(isSelected(d2, "sensor", "sensor-wrist-cam")).toBe(false);
  });

  it("selects a single part and clears it when clicked again", () => {
    const d = reducer(DEFAULT_DESIGN, { type: "selectSingle", category: "base", partId: "base-nano" });
    expect(singleSelected(d, "base")).toBe("base-nano");
    const d2 = reducer(d, { type: "selectSingle", category: "base", partId: "base-nano" });
    expect(singleSelected(d2, "base")).toBeUndefined();
  });

  it("prunes a gripper when the arm is removed", () => {
    // start from a design that has both arm + gripper
    const withGripper = DEFAULT_DESIGN;
    expect(selectedIds(withGripper, "gripper").length).toBe(1);
    const noArm = reducer(withGripper, { type: "selectSingle", category: "arm", partId: "arm-so101" });
    // clicking the selected arm again clears it → gripper must be pruned
    expect(singleSelected(noArm, "arm")).toBeUndefined();
    expect(selectedIds(noArm, "gripper").length).toBe(0);
  });

  it("blocks selecting a gripper with no arm present", () => {
    const noArm: Design = {
      name: "x",
      selection: { base: ["base-nano"], drive: ["drive-diff-2"], power: ["power-3s-5ah"], compute: ["compute-pi5"] },
      requirements: { ...DEFAULT_REQUIREMENTS },
    };
    const d = reducer(noArm, { type: "selectSingle", category: "gripper", partId: "gripper-parallel" });
    expect(selectedIds(d, "gripper").length).toBe(0);
  });

  it("updates a requirement and the name", () => {
    const d = reducer(DEFAULT_DESIGN, { type: "setRequirement", key: "payload", value: 12 });
    expect(d.requirements.payload).toBe(12);
    const d2 = reducer(d, { type: "setName", name: "My Bot" });
    expect(d2.name).toBe("My Bot");
  });

  it("loads a template by id", () => {
    const d = reducer(DEFAULT_DESIGN, { type: "loadTemplate", templateId: "inspection-rover" });
    expect(d.name).toBe("Inspection Rover");
    expect(singleSelected(d, "compute")).toBe("compute-agx-orin");
  });
});

describe("sanitize", () => {
  it("drops unknown part ids", () => {
    const d = sanitize({
      name: "x",
      selection: { base: ["does-not-exist"], sensor: ["sensor-imu", "nope"] },
      requirements: { ...DEFAULT_REQUIREMENTS },
    });
    expect(d.selection.base).toBeUndefined();
    expect(d.selection.sensor).toEqual(["sensor-imu"]);
  });

  it("drops ids placed in the wrong category", () => {
    const d = sanitize({
      name: "x",
      selection: { base: ["drive-mecanum-4"] },
      requirements: { ...DEFAULT_REQUIREMENTS },
    });
    expect(d.selection.base).toBeUndefined();
  });

  it("collapses a single-select category to one id", () => {
    const d = sanitize({
      name: "x",
      selection: { base: ["base-nano", "base-yahboom-x3"] },
      requirements: { ...DEFAULT_REQUIREMENTS },
    });
    expect(d.selection.base).toEqual(["base-nano"]);
  });

  it("fills missing requirements and a default name", () => {
    const d = sanitize({ name: "  ", selection: {}, requirements: {} });
    expect(d.name).toBe("Untitled Robot");
    expect(d.requirements).toEqual(DEFAULT_REQUIREMENTS);
  });
});

describe("encode/decode round-trip", () => {
  it("survives encode → decode for the reference design", () => {
    const enc = encodeDesign(DEFAULT_DESIGN);
    expect(enc.length).toBeGreaterThan(0);
    const back = decodeDesign(enc);
    expect(back).not.toBeNull();
    expect(back!.name).toBe(DEFAULT_DESIGN.name);
    expect(back!.selection).toEqual(DEFAULT_DESIGN.selection);
    expect(back!.requirements).toEqual(DEFAULT_DESIGN.requirements);
  });

  it("returns null for garbage input", () => {
    expect(decodeDesign("not-valid-base64!!")).toBeNull();
  });
});
