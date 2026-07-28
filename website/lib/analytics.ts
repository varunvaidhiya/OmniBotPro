/*
 * Lightweight, privacy-friendly, provider-agnostic analytics.
 *
 * Gives the marketing site one event API to measure the funnel (page view →
 * hero exposure → CTA click) without the call sites knowing which vendor is
 * behind it. It no-ops on the server and fans each event out to whichever
 * providers are actually configured:
 *
 *   - Firebase Analytics (GA4) — the primary provider. Configured through
 *     NEXT_PUBLIC_FIREBASE_* env vars; see lib/firebase.ts and
 *     FIREBASE_ANALYTICS_SETUP.md.
 *   - Plausible / PostHog — optional, used only if their snippet has put a
 *     global on `window` (see the script tag in app/layout.tsx).
 *
 * Set NEXT_PUBLIC_ANALYTICS=off to disable every provider at once.
 *
 * Adding another vendor means adding one line to `track()` — no call site
 * changes.
 */

import { initFirebaseAnalytics, logFirebaseEvent } from "@/lib/firebase";

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

/**
 * Start any provider that needs warming up before the first event. Called once
 * from components/analytics/FirebaseAnalytics.tsx, which is mounted in the root
 * layout. Safe to call repeatedly.
 */
export function initAnalytics(): void {
  if (typeof window === "undefined" || DISABLED) return;
  initFirebaseAnalytics();
}

/** Record an event. Safe to call anywhere — never throws, never runs on the server. */
export function track(event: AnalyticsEvent, props: Props = {}): void {
  if (typeof window === "undefined" || DISABLED) return;
  try {
    logFirebaseEvent(event, props);
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

/**
 * Record a page view.
 *
 * `page` is our own label for the view — a route path ("/pricing") from the
 * automatic route tracker, or a hand-written name from a page component. The
 * GA4-reserved `page_location` / `page_path` / `page_title` params are filled
 * in from the current document so Firebase's built-in Pages reports work
 * whichever form the label takes.
 */
export function pageview(page: string, props: Props = {}): void {
  if (typeof window === "undefined" || DISABLED) return;
  track("page_view", {
    page,
    page_path: window.location.pathname,
    page_location: window.location.href,
    page_title: document.title,
    ...props,
  });
}
