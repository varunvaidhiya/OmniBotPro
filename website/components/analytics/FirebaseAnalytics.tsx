"use client";

/*
 * Firebase Analytics mount point — rendered once from the root layout.
 *
 * Does two things:
 *
 *   1. Warms the Analytics SDK on first mount, so the first real event isn't
 *      delayed by loading the chunk.
 *   2. Emits a page_view on every route, including client-side navigations.
 *      gtag's automatic page_view is switched off in lib/firebase.ts precisely
 *      so this component is the only source of page views — the built-in one
 *      fires on hard loads only and would both miss in-app navigation and
 *      double-count the landing page.
 *
 * Renders nothing. When Firebase isn't configured (or NEXT_PUBLIC_ANALYTICS=off)
 * every call below is an inert no-op and no SDK is ever downloaded.
 */

import { Suspense, useEffect } from "react";
import { usePathname, useSearchParams } from "next/navigation";

import { initAnalytics, pageview } from "@/lib/analytics";

function RouteTracker() {
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    if (!pathname) return;
    const query = searchParams?.toString();
    pageview(query ? `${pathname}?${query}` : pathname);
  }, [pathname, searchParams]);

  return null;
}

export default function FirebaseAnalytics() {
  useEffect(() => {
    initAnalytics();
  }, []);

  // useSearchParams() opts its subtree out of static rendering, so it must sit
  // behind a Suspense boundary — without one, every page in the app would be
  // forced to render dynamically at request time.
  return (
    <Suspense fallback={null}>
      <RouteTracker />
    </Suspense>
  );
}
