/*
 * Lightweight, privacy-friendly, provider-agnostic analytics.
 *
 * Phase 0 scaffolding: gives the marketing site an event API to measure the
 * funnel (page view → hero exposure → CTA click) without committing to a vendor
 * or shipping a heavy SDK. It no-ops on the server and when no provider is
 * present, and calls a privacy-friendly provider (Plausible or PostHog) only if
 * one has been loaded on `window`. Set NEXT_PUBLIC_ANALYTICS=off to disable.
 *
 * Wire a provider later by adding its snippet to app/layout — the events below
 * already flow to it. No code here needs to change.
 */

export type AnalyticsEvent =
  | "page_view"
  | "hero_view"
  | "hero_cta_click"
  | "experiment_exposure"
  | "github_click";

type Props = Record<string, string | number | boolean | undefined>;

declare global {
  interface Window {
    plausible?: (event: string, opts?: { props?: Props }) => void;
    posthog?: { capture?: (event: string, props?: Props) => void };
  }
}

const DISABLED =
  typeof process !== "undefined" && process.env.NEXT_PUBLIC_ANALYTICS === "off";

/** Record an event. Safe to call anywhere — never throws, never runs on the server. */
export function track(event: AnalyticsEvent, props: Props = {}): void {
  if (typeof window === "undefined" || DISABLED) return;
  try {
    window.plausible?.(event, { props });
    window.posthog?.capture?.(event, props);
    if (process.env.NODE_ENV !== "production") {
      // eslint-disable-next-line no-console
      console.debug("[analytics]", event, props);
    }
  } catch {
    /* analytics must never break the page */
  }
}

export function pageview(page: string): void {
  track("page_view", { page });
}
