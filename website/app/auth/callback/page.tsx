"use client";

/*
 * OAuth / magic-link return URL. Supabase (detectSessionInUrl) exchanges the
 * code for a session automatically on load; we just wait for SIGNED_IN and then
 * forward to ?next=. Hard timeout fallback so we never hang.
 */

import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import { getSupabase } from "@/lib/auth/supabase";

export default function AuthCallbackPage() {
  const [msg, setMsg] = useState("Completing sign-in…");

  useEffect(() => {
    const next = new URLSearchParams(window.location.search).get("next") || "/account";
    const supabase = getSupabase();
    if (!supabase) {
      window.location.replace(next);
      return;
    }

    const go = () => window.location.replace(next);
    const { data: sub } = supabase.auth.onAuthStateChange((event) => {
      if (event === "SIGNED_IN") go();
    });
    supabase.auth.getSession().then(({ data }) => {
      if (data.session) go();
    });
    const timeout = setTimeout(() => {
      setMsg("Taking longer than expected — redirecting…");
      go();
    }, 3000);

    return () => {
      sub.subscription.unsubscribe();
      clearTimeout(timeout);
    };
  }, []);

  return (
    <main className="min-h-screen flex items-center justify-center" style={{ background: "var(--bg)" }}>
      <div className="flex flex-col items-center gap-3" style={{ color: "var(--faint)" }}>
        <Loader2 size={26} className="animate-spin" style={{ color: "var(--cyan)" }} />
        <span className="text-[12.5px] font-mono">{msg}</span>
      </div>
    </main>
  );
}
