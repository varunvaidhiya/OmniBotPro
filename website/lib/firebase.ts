/*
 * Firebase (Google Analytics 4) provider for the OhhO website.
 *
 * Mirrors the lib/auth/supabase.ts convention: configuration comes from
 * NEXT_PUBLIC_* env vars inlined at build time, and when they're absent the
 * module degrades to a no-op so the site keeps building and deploying with or
 * without a Firebase project.
 *
 * Three properties this module guarantees:
 *
 *   1. Browser-only. Firebase Analytics needs `window`, cookies and (in most
 *      browsers) IndexedDB. Every entry point returns early on the server and
 *      `isSupported()` is consulted before the SDK is touched.
 *   2. Lazily loaded. The SDK is pulled in through dynamic `import()` so it is
 *      code-split into its own chunk and never downloaded by visitors when no
 *      measurement ID is configured. Only `import type` appears at the top
 *      level, and types are erased at compile time.
 *   3. Never throws. Analytics must not be able to break a page, so every
 *      promise is caught and every failure resolves to null.
 *
 * Automatic page_view collection is deliberately turned OFF (see
 * `send_page_view` below): this is a client-routed Next.js app, so gtag's
 * built-in page view would only fire on a hard load and miss every subsequent
 * client-side navigation. components/analytics/FirebaseAnalytics.tsx emits
 * page_view for every route instead, which keeps it the single source of truth
 * and avoids double-counting the landing page.
 */

import type { FirebaseApp, FirebaseOptions } from "firebase/app";
import type { Analytics } from "firebase/analytics";

/**
 * Read as individual static member expressions — Next.js only inlines
 * `process.env.NEXT_PUBLIC_*` when accessed this way, never via destructuring
 * or dynamic indexing.
 */
export const firebaseConfig: FirebaseOptions = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN,
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
  storageBucket: process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID,
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID,
  measurementId: process.env.NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID,
};

/**
 * The config keys Analytics actually needs. `measurementId` (G-XXXXXXXXXX) is
 * what binds the app to a GA4 data stream — without it the SDK initialises but
 * reports nowhere, so we treat it as required rather than let it fail silently.
 * The other Firebase config fields (authDomain, storageBucket,
 * messagingSenderId) belong to products we don't use here and stay optional.
 */
export const REQUIRED_CONFIG_KEYS = [
  "apiKey",
  "projectId",
  "appId",
  "measurementId",
] as const;

export type RequiredConfigKey = (typeof REQUIRED_CONFIG_KEYS)[number];

/**
 * Which required keys are missing or blank. Pure and exported so setup problems
 * can be reported precisely ("measurementId is missing") instead of as a bare
 * "not configured", and so the rule is unit-testable without env plumbing.
 */
export function missingFirebaseConfigKeys(
  config: Partial<Record<RequiredConfigKey, string | undefined>>,
): RequiredConfigKey[] {
  return REQUIRED_CONFIG_KEYS.filter((key) => {
    const value = config[key];
    return typeof value !== "string" || value.trim() === "";
  });
}

export const isFirebaseConfigured =
  missingFirebaseConfigKeys(firebaseConfig).length === 0;

/** Send events to GA4 DebugView instead of the normal (batched) pipeline. */
const DEBUG_MODE = process.env.NEXT_PUBLIC_FIREBASE_DEBUG === "true";

let appPromise: Promise<FirebaseApp | null> | null = null;
let analyticsPromise: Promise<Analytics | null> | null = null;

/**
 * The initialised Firebase app, or null when unconfigured / server-side.
 * Cached as a promise so concurrent callers share one initialisation.
 */
export function getFirebaseApp(): Promise<FirebaseApp | null> {
  if (!appPromise) {
    appPromise = (async () => {
      if (typeof window === "undefined" || !isFirebaseConfigured) return null;
      const { initializeApp, getApps, getApp } = await import("firebase/app");
      // Reuse an existing app if one was already created (Fast Refresh, or a
      // future module that also initialises Firebase) — initializeApp() with
      // the same name twice would otherwise throw.
      return getApps().length > 0 ? getApp() : initializeApp(firebaseConfig);
    })().catch(() => null);
  }
  return appPromise;
}

/**
 * The Analytics instance, or null when unconfigured, server-side, or running
 * in an environment the SDK doesn't support (no cookies/IndexedDB, some
 * in-app webviews, SSR-ish test runners).
 */
export function getFirebaseAnalytics(): Promise<Analytics | null> {
  if (!analyticsPromise) {
    analyticsPromise = (async () => {
      const app = await getFirebaseApp();
      if (!app) return null;
      const { initializeAnalytics, getAnalytics, isSupported } = await import(
        "firebase/analytics"
      );
      if (!(await isSupported())) return null;
      try {
        // initializeAnalytics (not getAnalytics) is what lets us pass the
        // initial gtag config.
        return initializeAnalytics(app, {
          config: {
            // We emit page_view ourselves, per route — see the file header.
            send_page_view: false,
            ...(DEBUG_MODE ? { debug_mode: true } : {}),
          },
        });
      } catch {
        // Throws "already-exists" if this app was already initialised for
        // analytics — happens in dev when Fast Refresh re-evaluates this module
        // and resets the promise cache below. Reuse the existing instance
        // rather than losing analytics until a full reload.
        return getAnalytics(app);
      }
    })().catch(() => null);
  }
  return analyticsPromise;
}

/**
 * Warm the Analytics instance up front so the first real event doesn't pay for
 * loading the SDK. Safe to call repeatedly; work happens only once.
 */
export function initFirebaseAnalytics(): void {
  if (typeof window === "undefined" || !isFirebaseConfigured) return;
  void getFirebaseAnalytics();
}

/**
 * Record a GA4 event. Fire-and-forget: resolves the SDK in the background and
 * swallows every failure, so callers never need to await or guard.
 */
export function logFirebaseEvent(
  event: string,
  params: Record<string, unknown> = {},
): void {
  if (typeof window === "undefined" || !isFirebaseConfigured) return;
  void getFirebaseAnalytics()
    .then(async (analytics) => {
      if (!analytics) return;
      const { logEvent } = await import("firebase/analytics");
      logEvent(analytics, event, params);
    })
    .catch(() => {
      /* analytics must never break the page */
    });
}
