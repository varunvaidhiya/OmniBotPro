/*
 * Billing plans + subscription status helpers.
 *
 * Access model:
 *   - All product consoles are free with a sign-in (AuthGate / ConsoleGate).
 *   - Cloud-cost features are gated inline by PaidFeatureGate:
 *       • GPU training (OhhO Train)
 *       • AI inference API (OhhO Serve)
 *       • Cloud AI agent / Claude API (OhhO Mind)
 *       • MCP server connection (OhhO Link)
 *       • Cloud simulation & what-if (OhhO Twin)
 *       • Cloud data sync (OhhO Data)
 *
 * The Stripe price for each plan is resolved server-side in the checkout Edge
 * Function (by plan id), so no price ids live in the client.
 */

export type PlanId = "builder" | "fleet" | "forge";

export interface Plan {
  id: PlanId;
  name: string;
  price: string;
  period: string;
  blurb: string;
  features: string[];
  /** Forge is sales-led (no self-serve Stripe checkout). */
  checkout: boolean;
  popular?: boolean;
  accent: "cyan" | "violet" | "none";
}

export const PLANS: Plan[] = [
  {
    id: "builder",
    name: "Builder",
    price: "$49",
    period: "/ month",
    blurb: "For indie devs and researchers.",
    features: [
      "All 19 consoles free with sign-in",
      "OhhO Train — GPU cloud training (20 hrs/mo)",
      "OhhO Serve — AI inference API (500 calls/day)",
      "OhhO Mind — Cloud AI agent, LLM-backed (1K calls/mo)",
      "OhhO Link — MCP server + API key",
      "OhhO Twin — Cloud simulation & what-if scenarios",
      "OhhO Data — Cloud sync (1K episodes)",
      "Email support",
    ],
    checkout: true,
    popular: true,
    accent: "cyan",
  },
  {
    id: "fleet",
    name: "Fleet",
    price: "$199",
    period: "/ month",
    blurb: "For teams running real hardware.",
    features: [
      "Everything in Builder",
      "OhhO Train — 200 GPU hrs/mo",
      "OhhO Serve — 10K API calls/day",
      "OhhO Mind — 20K calls/mo + team memory",
      "OhhO Fleet — OTA updates, up to 100 robots",
      "OhhO Data — Unlimited episodes + annotation",
      "OhhO Comply + Shield — compliance & security",
      "Priority support + Slack channel",
    ],
    checkout: true,
    accent: "violet",
  },
  {
    id: "forge",
    name: "Forge",
    price: "Custom",
    period: "contact us",
    blurb: "Enterprise / white-label.",
    features: [
      "Everything in Fleet",
      "Unlimited GPU & API usage",
      "On-prem OhhO Serve license",
      "SSO + dedicated SLA",
      "Custom robot integration",
      "White-label OhhO Pilot",
    ],
    checkout: false,
    accent: "none",
  },
];

export function getPlan(id: string): Plan | undefined {
  return PLANS.find((p) => p.id === id);
}

export interface Subscription {
  plan: string;
  status: string;
  current_period_end: string | null;
  /** Set when the user cancelled in the Stripe portal: the plan stays active
   *  until current_period_end but will not renew. */
  cancel_at_period_end?: boolean | null;
}

const ACTIVE_STATUSES = ["active", "trialing", "past_due"];

/** Whether the user has an active paid subscription for billing/account UI. */
export function hasConsoleAccess(subscription: Subscription | null): boolean {
  return Boolean(subscription && ACTIVE_STATUSES.includes(subscription.status));
}

/**
 * Active, but cancelled — the plan is still usable until current_period_end and
 * then stops. The account UI shows "ends on <date>" instead of "renews on …".
 */
export function isCanceling(subscription: Subscription | null): boolean {
  return Boolean(
    subscription &&
      subscription.cancel_at_period_end &&
      ACTIVE_STATUSES.includes(subscription.status),
  );
}
