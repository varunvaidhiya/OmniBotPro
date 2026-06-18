"use client";

/*
 * Handles OAuth providers that return to the site root with ?code=...
 * instead of /auth/callback. This keeps stale Supabase/Google redirect settings
 * from stranding signed-in users on the marketing page.
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

    let cancelled = false;
    supabase.auth.exchangeCodeForSession(code).finally(() => {
      if (cancelled) return;
      window.history.replaceState(null, "", next);
      window.location.replace(next);
    });

    return () => {
      cancelled = true;
    };
  }, []);

  return null;
}
