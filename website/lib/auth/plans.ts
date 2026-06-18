/*
 * Billing plans + console-access rules.
 *
 * Mirrors the homepage Pricing component. Per the product decision, EVERY
 * product console requires an active paid subscription — any paid plan unlocks
 * all consoles. The Stripe price for each plan is resolved server-side in the
 * checkout Edge Function (by plan id), so no price ids live in the client.
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
      "All product consoles unlocked",
      "OhhO Build — full library, 10 designs",
      "OhhO Serve — 500 API calls / day",
      "OhhO Data — cloud sync, 1K episodes",
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
      "OhhO Fleet — up to 100 robots, OTA",
      "OhhO Serve — 10K API calls / day",
      "OhhO Comply + Shield",
      "Priority support + Slack",
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
      "Unlimited robots",
      "On-prem OhhO Serve license",
      "SSO + dedicated SLA",
      "Custom robot integration",
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
}

const ACTIVE_STATUSES = ["active", "trialing", "past_due"];

/** Any active paid subscription unlocks every console. */
export function hasConsoleAccess(subscription: Subscription | null): boolean {
  return Boolean(subscription && ACTIVE_STATUSES.includes(subscription.status));
}
