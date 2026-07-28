import { describe, it, expect } from "vitest";

import {
  REQUIRED_CONFIG_KEYS,
  missingFirebaseConfigKeys,
  getFirebaseApp,
  getFirebaseAnalytics,
  initFirebaseAnalytics,
  logFirebaseEvent,
} from "@/lib/firebase";
import { track, pageview, initAnalytics } from "@/lib/analytics";

const complete = {
  apiKey: "AIzaTest",
  projectId: "ohho-test",
  appId: "1:123:web:abc",
  measurementId: "G-TEST12345",
};

describe("missingFirebaseConfigKeys", () => {
  it("reports nothing missing for a complete config", () => {
    expect(missingFirebaseConfigKeys(complete)).toEqual([]);
  });

  it("names every key that is absent", () => {
    expect(missingFirebaseConfigKeys({})).toEqual([...REQUIRED_CONFIG_KEYS]);
  });

  it("treats a blank or whitespace-only value as missing", () => {
    // An env var set to "" in a deploy dashboard is a very common way to end up
    // with a half-configured project — it must not count as configured.
    expect(missingFirebaseConfigKeys({ ...complete, apiKey: "" })).toEqual(["apiKey"]);
    expect(missingFirebaseConfigKeys({ ...complete, measurementId: "  " })).toEqual([
      "measurementId",
    ]);
  });

  it("requires measurementId — without it events would report nowhere", () => {
    expect(missingFirebaseConfigKeys({ ...complete, measurementId: undefined })).toEqual([
      "measurementId",
    ]);
  });

  it("ignores config fields that Analytics does not need", () => {
    // authDomain / storageBucket / messagingSenderId belong to other Firebase
    // products; their absence must not disable analytics.
    expect(REQUIRED_CONFIG_KEYS).not.toContain("authDomain");
    expect(REQUIRED_CONFIG_KEYS).not.toContain("storageBucket");
    expect(REQUIRED_CONFIG_KEYS).not.toContain("messagingSenderId");
  });
});

describe("server-side safety", () => {
  // This suite runs under vitest's node environment, i.e. no `window` — the
  // same conditions as Next.js server rendering.
  it("has no window", () => {
    expect(typeof window).toBe("undefined");
  });

  it("resolves to null instead of loading the SDK", async () => {
    await expect(getFirebaseApp()).resolves.toBeNull();
    await expect(getFirebaseAnalytics()).resolves.toBeNull();
  });

  it("never throws from any event entry point", () => {
    expect(() => initFirebaseAnalytics()).not.toThrow();
    expect(() => logFirebaseEvent("hero_view", { variant: "affordable" })).not.toThrow();
    expect(() => initAnalytics()).not.toThrow();
    expect(() => track("hero_cta_click", { cta: "start_free" })).not.toThrow();
    // pageview() reads window.location/document — must bail out before that.
    expect(() => pageview("/pricing")).not.toThrow();
  });
});
