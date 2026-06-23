import { describe, it, expect } from "vitest";

import { hasConsoleAccess, isCanceling, type Subscription } from "@/lib/auth/plans";

const sub = (over: Partial<Subscription>): Subscription => ({
  plan: "builder",
  status: "active",
  current_period_end: "2026-12-01T00:00:00.000Z",
  cancel_at_period_end: false,
  ...over,
});

describe("hasConsoleAccess", () => {
  it("is true for active/trialing/past_due, false otherwise and for null", () => {
    expect(hasConsoleAccess(sub({ status: "active" }))).toBe(true);
    expect(hasConsoleAccess(sub({ status: "trialing" }))).toBe(true);
    expect(hasConsoleAccess(sub({ status: "past_due" }))).toBe(true);
    expect(hasConsoleAccess(sub({ status: "canceled" }))).toBe(false);
    expect(hasConsoleAccess(null)).toBe(false);
  });

  it("keeps access while a cancelled plan is still in its paid period", () => {
    // cancel_at_period_end=true but status still active → access retained
    expect(hasConsoleAccess(sub({ status: "active", cancel_at_period_end: true }))).toBe(true);
  });
});

describe("isCanceling", () => {
  it("is true only when active AND set to cancel at period end", () => {
    expect(isCanceling(sub({ status: "active", cancel_at_period_end: true }))).toBe(true);
    expect(isCanceling(sub({ status: "active", cancel_at_period_end: false }))).toBe(false);
    // already fully cancelled → not "canceling" (it's done)
    expect(isCanceling(sub({ status: "canceled", cancel_at_period_end: true }))).toBe(false);
    expect(isCanceling(null)).toBe(false);
  });

  it("treats a missing/undefined flag as not canceling (back-compat)", () => {
    expect(isCanceling(sub({ cancel_at_period_end: undefined }))).toBe(false);
    expect(isCanceling(sub({ cancel_at_period_end: null }))).toBe(false);
  });
});
