import { describe, it, expect } from "vitest";

import { computeMetrics, validate, recommend, overallStatus } from "./engine";
import { DEFAULT_DESIGN, TEMPLATES, reducer, type Design } from "./design";

const warehouse = DEFAULT_DESIGN; // TEMPLATES[0].design — the OmniBot reference build

describe("computeMetrics", () => {
  it("sums mass, price and lead time across all selected parts", () => {
    const m = computeMetrics(warehouse);
    expect(m.count).toBe(9);
    expect(m.totalMass).toBeCloseTo(5.15, 2);
    expect(m.totalPrice).toBe(1823);
    expect(m.maxLeadTimeDays).toBe(14);
  });

  it("derives usable payload, reach, speed, runtime and tops from the real specs", () => {
    const m = computeMetrics(warehouse);
    expect(m.ratedPayload).toBe(5);
    expect(m.mountedMass).toBeCloseTo(1.97, 2);
    expect(m.usablePayload).toBeCloseTo(3.03, 2);
    expect(m.armPayload).toBeCloseTo(0.5, 2);
    expect(m.reach).toBeCloseTo(0.62, 2);
    expect(m.topSpeed).toBeCloseTo(1.2, 2);
    expect(m.avgPowerDraw).toBeCloseTo(44.3, 1);
    expect(m.runtime).toBeCloseTo(3.3, 1);
    expect(m.footprint).toBeCloseTo(0.265, 3);
    expect(m.tops).toBe(40);
  });

  it("returns zeroes for an empty design", () => {
    const blank = TEMPLATES.find((t) => t.id === "blank")!.design;
    const m = computeMetrics(blank);
    expect(m.count).toBe(0);
    expect(m.totalMass).toBe(0);
    expect(m.totalPrice).toBe(0);
    expect(m.runtime).toBe(0);
    expect(m.tops).toBe(0);
  });
});

describe("validate", () => {
  it("passes every check for the reference build", () => {
    const m = computeMetrics(warehouse);
    const checks = validate(warehouse, m);
    expect(checks.length).toBeGreaterThan(0);
    expect(overallStatus(checks)).toBe("pass");
  });

  it("fails on missing required subsystems", () => {
    const blank = TEMPLATES.find((t) => t.id === "blank")!.design;
    const checks = validate(blank, computeMetrics(blank));
    const required = checks.filter((c) => c.id.startsWith("req-"));
    expect(required.map((c) => c.id).sort()).toEqual(["req-base", "req-compute", "req-drive", "req-power"]);
    expect(overallStatus(checks)).toBe("fail");
  });

  it("flags a gripper with no arm", () => {
    const design: Design = {
      ...warehouse,
      selection: { ...warehouse.selection, arm: [] },
    };
    const m = computeMetrics(design);
    const checks = validate(design, m);
    expect(checks.find((c) => c.id === "gripper-arm")?.status).toBe("fail");
  });

  it("warns when a part is not rated for the chosen environment", () => {
    // outdoor requirement, but the reference build's base/drive are indoor-only
    const design: Design = {
      ...warehouse,
      requirements: { ...warehouse.requirements, environment: "outdoor" },
    };
    const checks = validate(design, computeMetrics(design));
    expect(checks.find((c) => c.id === "environment")?.status).toBe("warn");
  });
});

describe("recommend", () => {
  it("suggests adding the missing required subsystems for a blank design", () => {
    const blank = TEMPLATES.find((t) => t.id === "blank")!.design;
    const recs = recommend(blank, computeMetrics(blank));
    const fixes = recs.filter((r) => r.severity === "fix");
    expect(fixes.length).toBe(5); // base, drive, power, compute, arm (reach>0)
    expect(fixes.every((r) => r.action)).toBe(true);
    const addBase = recs.find((r) => r.id === "add-base");
    expect(addBase?.action?.payload).toEqual({ type: "selectSingle", category: "base", partId: "base-nano" });
  });

  it("applies an add-base recommendation and clears the fix", () => {
    const blank = TEMPLATES.find((t) => t.id === "blank")!.design;
    const recs = recommend(blank, computeMetrics(blank));
    const addBase = recs.find((r) => r.id === "add-base")!;
    const next = reducer(blank, addBase.action!.payload);
    expect(next.selection.base).toEqual(["base-nano"]);
    const recs2 = recommend(next, computeMetrics(next));
    expect(recs2.find((r) => r.id === "add-base")).toBeUndefined();
  });

  it("reports a valid design once all requirements are met", () => {
    const recs = recommend(warehouse, computeMetrics(warehouse));
    expect(recs.some((r) => r.severity === "ok")).toBe(true);
  });
});
