"use client";

/*
 * Catches OAuth / magic-link redirects that land on the site root with ?code=...
 * instead of /auth/callback (e.g. when Supabase redirect URLs are misconfigured).
 *
 * The Supabase client (detectSessionInUrl: true) may have already auto-exchanged
 * the code by the time this hook runs. When the session is already established we
 * forward directly to ?next=; otherwise we pass the code to /auth/callback which
 * knows how to wait for the session properly (no duplicate exchange, no race).
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

    // detectSessionInUrl may have already consumed the code — check first.
    if (supabase) {
      supabase.auth.getSession().then(({ data }) => {
        if (data.session) {
          window.location.replace(next);
          return;
        }
        // Code not exchanged yet — punt to the callback page.
        const search = new URLSearchParams();
        search.set("code", code);
        if (params.get("next")) search.set("next", params.get("next")!);
        window.location.replace(`/auth/callback?${search.toString()}`);
      });
    } else {
      window.location.replace(next);
    }
  }, []);

  return null;
}
