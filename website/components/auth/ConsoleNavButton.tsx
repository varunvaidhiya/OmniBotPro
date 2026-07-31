"use client";

/*
 * ConsoleNavButton — the "Console" button in the top Nav.
 *
 * Consoles are free with a sign-in (no plan required), so any signed-in user
 * goes straight to the console hub.
 *
 *   not signed in       → /login?next=/console  (prompt to sign in, then straight to console)
 *   signed in           → /console               (the console hub)
 *
 * Styling: filled cyan for subscribed users; subtle for signed-in free users
 * so the highlighted "Upgrade" button stays the visual lead.
 */

import Link from "next/link";
import { Monitor } from "lucide-react";

import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

export default function ConsoleNavButton() {
  const { loading, user, subscription } = useAuth();

  // During the initial auth-loading window (Supabase auth.getSession() hasn't
  // resolved yet) `user` is null and `loading` is true. We used to `return
  // null` here, which made the Console button disappear on slow networks —
  // a "sometimes the nav is missing sign-in options" bug on mobile. Instead,
  // render the signed-out state during loading so the button is always
  // visible; it just points to /login until the session resolves.
  const signedIn = Boolean(user) && !loading;
  const subscribed = signedIn && hasConsoleAccess(subscription);

  const href = signedIn ? "/console" : "/login?next=/console";

  return (
    <Link
      href={href}
      className="inline-flex items-center gap-1.5 text-[13px] font-semibold px-4 py-2 rounded-lg transition-all duration-200 hover:opacity-90 hover:-translate-y-px"
      style={{
        background: subscribed ? "var(--cyan)" : "rgba(255,255,255,0.06)",
        color: subscribed ? "var(--bg)" : "var(--text)",
        border: subscribed ? "none" : "1px solid rgba(255,255,255,0.14)",
      }}
    >
      <Monitor size={14} />
      Console
    </Link>
  );
}
