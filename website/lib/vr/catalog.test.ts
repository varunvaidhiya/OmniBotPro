import { fileURLToPath } from "node:url";

import { describe, it, expect } from "vitest";

import { buildVrCatalog, VR_CATALOG_VERSION } from "./catalog";
import { CATEGORIES } from "@/lib/garage/types";
import { ROBOT_TYPES } from "@/lib/garage/robot-catalog";

describe("buildVrCatalog", () => {
  it("carries every category and robot type from the website catalog", () => {
    const c = buildVrCatalog();
    expect(c.categories.length).toBe(CATEGORIES.length);
    expect(c.robotTypes.length).toBe(ROBOT_TYPES.length);
    expect(c.version).toBe(VR_CATALOG_VERSION);
  });

  it("includes the mobile-manipulator category and the OmniBot reference", () => {
    const c = buildVrCatalog();
    expect(c.categories.map((cat) => cat.id)).toContain("mobile-manipulator");

    const mecanum = c.robotTypes.find((t) => t.id === "wheeled-mecanum");
    expect(mecanum).toBeDefined();
    const omnibot = mecanum!.hardwareModels.find((m) => m.name.includes("OmniBot"));
    expect(omnibot).toBeDefined();
    expect(omnibot!.hasArm).toBe(true);
  });

  it("every robot type references a known category (resolvable in the headset)", () => {
    const c = buildVrCatalog();
    const ids = new Set(c.categories.map((cat) => cat.id));
    for (const t of c.robotTypes) expect(ids.has(t.category)).toBe(true);
  });

  it("is fully JSON-serializable (no functions / JSX / undefined holes)", () => {
    const c = buildVrCatalog();
    expect(JSON.parse(JSON.stringify(c))).toEqual(c);
  });
});

describe("public/vr/catalog.json (committed snapshot)", () => {
  it("is in sync with buildVrCatalog() — run `npm run generate:vr` if this fails", async () => {
    const path = fileURLToPath(new URL("../../public/vr/catalog.json", import.meta.url));
    const json = JSON.stringify(buildVrCatalog(), null, 2) + "\n";
    await expect(json).toMatchFileSnapshot(path);
  });
});
