import { describe, it, expect } from "vitest";

import {
  buildBom,
  bomToCsv,
  buildUrdf,
  buildWorld,
  buildDeployProfile,
  designToJson,
  slug,
} from "./exporters";
import { DEFAULT_DESIGN } from "./design";

describe("buildBom", () => {
  it("lists one row per selected part with line totals", () => {
    const bom = buildBom(DEFAULT_DESIGN);
    const arm = bom.find((r) => r.category === "arm");
    expect(arm?.name).toBe("SO-101 6-DOF");
    expect(arm?.qty).toBe(1);
    expect(arm?.lineTotal).toBe(480);
    expect(arm?.supplierUrl).toBeTruthy();
  });

  it("would collapse repeated sensor ids into a quantity", () => {
    const design = {
      ...DEFAULT_DESIGN,
      selection: { ...DEFAULT_DESIGN.selection, sensor: ["sensor-imu", "sensor-imu"] },
    };
    const imu = buildBom(design).find((r) => r.category === "sensor" && r.name === "9-DOF IMU");
    expect(imu?.qty).toBe(2);
    expect(imu?.lineTotal).toBe(50);
  });
});

describe("bomToCsv", () => {
  it("emits a header, one row per part and a totals line", () => {
    const csv = bomToCsv(DEFAULT_DESIGN);
    const lines = csv.split("\n");
    expect(lines[0]).toMatch(/Category,Part,Brand,Qty/);
    expect(csv).toContain("SO-101 6-DOF");
    expect(csv).toContain(",1823,"); // total price lands in the totals line
  });
});

describe("buildUrdf", () => {
  it("produces a parseable robot with base_link and four wheels", () => {
    const urdf = buildUrdf(DEFAULT_DESIGN);
    expect(urdf).toContain('<?xml version="1.0"?>');
    expect(urdf).toContain('<robot name="warehouse_amr"');
    expect(urdf).toContain('<link name="base_link">');
    const wheelCount = (urdf.match(/<link name="wheel_(fl|fr|rl|rr)_link">/g) || []).length;
    expect(wheelCount).toBe(4);
  });

  it("chains arm joints by dof and mounts the gripper", () => {
    const urdf = buildUrdf(DEFAULT_DESIGN);
    expect(urdf).toContain('arm_link_1');
    expect(urdf).toContain('arm_link_6'); // SO-101 is 6-DOF
    expect(urdf).toContain('<link name="gripper_link">');
  });
});

describe("buildWorld", () => {
  it("emits an SDF world that includes the generated model", () => {
    const world = buildWorld(DEFAULT_DESIGN);
    expect(world).toContain("<sdf version=\"1.9\">");
    expect(world).toContain('model://warehouse_amr');
  });
});

describe("buildDeployProfile", () => {
  it("picks single-machine deploy when onboard TOPS >= 40 and embeds subsystem ids", () => {
    const prof = buildDeployProfile(DEFAULT_DESIGN);
    expect(prof).toContain("OMNIBOT_NAME=warehouse_amr");
    expect(prof).toContain("DEPLOY_MODE=single"); // Orin Nano = 40 TOPS
    expect(prof).toContain("COMPUTE_TOPS=40");
    expect(prof).toContain("ARM=arm-so101");
    expect(prof).toContain("ROS_DOMAIN_ID=30");
  });

  it("falls back to multi-machine deploy for a CPU-only brain", () => {
    const design = {
      ...DEFAULT_DESIGN,
      selection: { ...DEFAULT_DESIGN.selection, compute: ["compute-pi5"] },
    };
    expect(buildDeployProfile(design)).toContain("DEPLOY_MODE=multi");
  });
});

describe("designToJson", () => {
  it("serializes name, requirements, bom and metrics (without the parts array)", () => {
    const json = JSON.parse(designToJson(DEFAULT_DESIGN));
    expect(json.generatedBy).toBe("OhhO Build");
    expect(json.name).toBe(DEFAULT_DESIGN.name);
    expect(Array.isArray(json.bom)).toBe(true);
    expect(json.metrics.totalPrice).toBe(1823);
    expect(json.metrics.parts).toBeUndefined();
  });
});

describe("slug", () => {
  it("slugifies a design name and falls back to omnibot", () => {
    expect(slug("Warehouse AMR")).toBe("warehouse_amr");
    expect(slug("   ")).toBe("omnibot");
  });
});
