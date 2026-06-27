import { fileURLToPath } from "node:url";

import { describe, it, expect } from "vitest";

import {
  buildVrManifest,
  vrProducts,
  VR_MANIFEST_VERSION,
  VR_THEME,
  VR_AUTH,
} from "./manifest";
import { PRODUCTS } from "@/lib/products";

describe("vrProducts", () => {
  it("includes Pilot (the teleoperation product)", () => {
    const slugs = vrProducts().map((p) => p.slug);
    expect(slugs).toContain("pilot");
  });

  it("excludes products that don't require a headset", () => {
    const slugs = vrProducts().map((p) => p.slug);
    // Plenty of non-VR products exist; spot-check a few.
    expect(slugs).not.toContain("build");
    expect(slugs).not.toContain("fleet");
    expect(slugs).not.toContain("serve");
  });

  it("matches exactly the products flagged vr === true", () => {
    const expected = PRODUCTS.filter((p) => p.vr === true).map((p) => p.slug);
    expect(vrProducts().map((p) => p.slug)).toEqual(expected);
    expect(expected.length).toBeGreaterThan(0);
  });

  it("drops the JSX icon and keeps only serializable fields", () => {
    for (const p of vrProducts()) {
      expect(p).not.toHaveProperty("icon");
      expect(typeof p.name).toBe("string");
      expect(Array.isArray(p.highlights)).toBe(true);
      expect(Array.isArray(p.specs)).toBe(true);
    }
  });
});

describe("buildVrManifest", () => {
  it("is fully JSON-serializable (no functions / JSX / undefined holes)", () => {
    const m = buildVrManifest();
    const roundTripped = JSON.parse(JSON.stringify(m));
    expect(roundTripped).toEqual(m);
  });

  it("carries the OhhO branding tokens", () => {
    const m = buildVrManifest();
    expect(m.theme.cyan).toBe("#00D4FF");
    expect(m.theme.violet).toBe("#7C3AED");
    expect(m.theme.fonts.display).toBe("Space Grotesk");
    expect(m.theme).toEqual(VR_THEME);
  });

  it("carries Supabase auth config for in-headset sign-in", () => {
    const m = buildVrManifest();
    expect(m.auth.provider).toBe("supabase");
    expect(m.auth.signIn).toBe("email_otp");
    expect(m.auth.url).toMatch(/^https:\/\/.*\.supabase\.co$/);
    expect(m.auth.anonKey.length).toBeGreaterThan(20);
    expect(m.auth).toEqual(VR_AUTH);
  });

  it("is deterministic (same input → identical output)", () => {
    expect(buildVrManifest()).toEqual(buildVrManifest());
    expect(buildVrManifest().version).toBe(VR_MANIFEST_VERSION);
  });
});

describe("public/vr/manifest.json (committed snapshot)", () => {
  it("is in sync with buildVrManifest() — run `npm run generate:vr` if this fails", async () => {
    // The VR app fetches this static file from the deployed site. The snapshot
    // is committed; `npm run generate:vr` (vitest -u) rewrites it after changes.
    const path = fileURLToPath(new URL("../../public/vr/manifest.json", import.meta.url));
    const json = JSON.stringify(buildVrManifest(), null, 2) + "\n";
    await expect(json).toMatchFileSnapshot(path);
  });
});
