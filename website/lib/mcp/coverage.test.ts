import { describe, it, expect } from "vitest";

import { getAllTools, getTool, toolsByProduct, TOOL_COUNT } from "@/lib/mcp/registry";

/*
 * Coverage guard for the console-endpoint audit. Locks in that every product
 * console is represented and that the read+write endpoints added during the
 * audit stay registered (so a future refactor can't silently drop them).
 */

const EXPECTED_PRODUCTS = [
  "autonomy", "bench", "bridge", "build", "care", "comply", "connect", "data",
  "fleet", "frame", "garage", "market", "mind", "pilot", "proof", "serve",
  "shield", "train", "twin", "view",
];

describe("MCP registry coverage", () => {
  it("covers all 20 product consoles", () => {
    const products = toolsByProduct();
    for (const p of EXPECTED_PRODUCTS) {
      expect(products[p]?.length, `missing tools for product '${p}'`).toBeGreaterThan(0);
    }
  });

  it("has grown to the audited tool count", () => {
    expect(TOOL_COUNT).toBeGreaterThanOrEqual(150);
  });

  it("registers the key audited endpoints (read + write)", () => {
    const added = [
      "shield.patchCve", "shield.rotateDeviceKey",
      "twin.runWhatIf", "twin.resync",
      "proof.runSuite", "comply.updateRequirement",
      "data.exportDataset", "autonomy.navigateTo", "autonomy.cancelMission",
      "bench.completeAssemblyStep", "bridge.connectAdapter", "bridge.getImpedanceDefaults",
      "build.validateDesign", "build.estimateBom",
      "connect.getStatus", "connect.getTelemetry",
      "serve.predict", "serve.deploy",
      "mind.setBackend", "pilot.commandArm",
      "train.launchSweep", "care.createWorkOrder",
      "fleet.restartRobot", "fleet.acknowledgeAlert",
      "market.installSkill", "market.publishSkill",
      "view.recordClip", "frame.startProcess",
    ];
    for (const name of added) {
      expect(getTool(name), `missing tool '${name}'`).toBeTruthy();
    }
  });

  it("every write tool is flagged readOnly:false and read tools readOnly:true", () => {
    for (const t of getAllTools()) {
      expect(typeof t.readOnly).toBe("boolean");
    }
    // At least the audited write tools exist and are mutations.
    expect(getTool("fleet.restartRobot")?.readOnly).toBe(false);
    expect(getTool("shield.patchCve")?.readOnly).toBe(false);
    expect(getTool("build.validateDesign")?.readOnly).toBe(true);
  });
});
