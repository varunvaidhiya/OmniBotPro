"use client";

/*
 * Catches OAuth / magic-link redirects that land on the site root with ?code=...
 * instead of /auth/callback (e.g. when Supabase redirect URLs are misconfigured).
 *
 * The Supabase client (detectSessionInUrl: true) auto-exchanges the code
 * asynchronously during init. We stay on the page and wait for SIGNED_IN
 * (or an existing session) rather than navigating away mid-exchange.
 * A hard timeout fallback forwards to the callback page as a last resort.
 */

import { useEffect } from "react";

import { safeNextPath } from "@/lib/auth/AuthProvider";
import { getSupabase } from "@/lib/auth/supabase";

export default function AuthRedirectHandler() {
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get("code");
    const isCallback = window.location.pathname === "/auth/callback";
    if (!code || isCallback) return;

    const next = safeNextPath(params.get("next"));
    const supabase = getSupabase();
    if (!supabase) {
      window.location.replace(next);
      return;
    }

    let resolved = false;

    const go = (dest: string) => {
      if (resolved) return;
      resolved = true;
      window.history.replaceState(null, "", dest);
      window.location.replace(dest);
    };

    // Fast path: session already established (detectSessionInUrl won the race).
    supabase.auth.getSession().then(({ data }) => {
      if (data.session) go(next);
    });

    // Normal path: wait for detectSessionInUrl to finish exchanging the code.
    const { data: sub } = supabase.auth.onAuthStateChange((event, session) => {
      if (event === "SIGNED_IN" && session) go(next);
    });

    // Fallback: if nothing happens after 8 s, punt to the callback page which
    // will try a fresh exchange (the code may have been consumed by now, but
    // persistSession will have the session in local storage).
    const fallback = setTimeout(() => {
      if (resolved) return;
      const search = new URLSearchParams();
      search.set("code", code);
      if (params.get("next")) search.set("next", params.get("next")!);
      go(`/auth/callback?${search.toString()}`);
    }, 8000);

    return () => {
      sub.subscription.unsubscribe();
      clearTimeout(fallback);
    };
  }, []);

  return null;
}
