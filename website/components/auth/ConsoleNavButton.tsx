"use client";

/*
 * ConsoleNavButton — the "Console" button in the top Nav.
 *
 *   not signed in      → /login?next=/console  (prompt to sign in, then straight to console)
 *   signed in, no plan  → /upgrade?next=/console (payment / subscription options)
 *   signed in + active  → /console               (the console hub)
 */

import Link from "next/link";
import { Monitor } from "lucide-react";

import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

export default function ConsoleNavButton() {
  const { loading, user, subscription } = useAuth();

  if (loading) return null;

  const active = user && hasConsoleAccess(subscription);

  let href = "/login?next=/console";
  if (active) href = "/console";
  else if (user) href = "/upgrade?next=/console";

  return (
    <Link
      href={href}
      className="inline-flex items-center gap-1.5 text-[13px] font-semibold px-4 py-2 rounded-lg transition-all duration-200 hover:opacity-90 hover:-translate-y-px"
      style={{
        background: active ? "var(--cyan)" : "rgba(255,255,255,0.06)",
        color: active ? "var(--bg)" : "var(--text)",
        border: active ? "none" : "1px solid rgba(255,255,255,0.14)",
      }}
    >
      <Monitor size={14} />
      Console
    </Link>
  );
}
