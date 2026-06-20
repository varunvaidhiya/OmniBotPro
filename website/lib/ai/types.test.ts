import { describe, it, expect } from "vitest";

import { sanitizeConsoleSpec, sanitizeEnrichment } from "./types";

describe("sanitizeConsoleSpec", () => {
  it("keeps only allowlisted panel kinds and ignores junk", () => {
    const spec = sanitizeConsoleSpec({
      headline: "Aerial console",
      panels: [
        { kind: "metrics", title: "Flight", items: [{ label: "Altitude", value: "12 m" }] },
        { kind: "<script>alert(1)</script>", title: "evil" },
        { kind: "cameras", title: "Feeds" },
        { kind: "actions", title: "Controls", actions: ["Arm", "Takeoff"] },
      ],
    });
    expect(spec).not.toBeNull();
    expect(spec!.panels).toHaveLength(3);
    expect(spec!.panels.map((p) => p.kind)).toEqual(["metrics", "cameras", "actions"]);
    expect(spec!.panels[0].items![0].label).toBe("Altitude");
  });

  it("caps panels at 8", () => {
    const panels = Array.from({ length: 20 }, () => ({ kind: "metrics", title: "x" }));
    const spec = sanitizeConsoleSpec({ headline: "h", panels });
    expect(spec!.panels.length).toBeLessThanOrEqual(8);
  });

  it("returns null for non-objects / empty", () => {
    expect(sanitizeConsoleSpec(null)).toBeNull();
    expect(sanitizeConsoleSpec("nope")).toBeNull();
    expect(sanitizeConsoleSpec({ headline: "", panels: [] })).toBeNull();
  });
});

describe("sanitizeEnrichment", () => {
  it("extracts summary, notes and compute", () => {
    const e = sanitizeEnrichment({
      summary: "A 12-DOF quadruped.",
      notes: ["ROS 2 SDK", "4D LiDAR"],
      compute: { brain: "Jetson NX", accelerator: "NVIDIA Jetson", vramGb: 8 },
      recommendedModels: ["RL locomotion policy"],
      suggestedTasks: ["inspection", "patrol"],
    });
    expect(e!.summary).toContain("quadruped");
    expect(e!.notes).toHaveLength(2);
    expect(e!.compute!.brain).toBe("Jetson NX");
    expect(e!.recommendedModels).toContain("RL locomotion policy");
  });

  it("clamps absurd vram and returns null on garbage", () => {
    const e = sanitizeEnrichment({ summary: "x", compute: { brain: "b", vramGb: 99999 } });
    expect(e!.compute!.vramGb).toBe(512);
    expect(sanitizeEnrichment({})).toBeNull();
    expect(sanitizeEnrichment(42)).toBeNull();
  });
});
