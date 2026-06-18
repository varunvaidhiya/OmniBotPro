"use client";

/*
 * UserMenu — the auth control in the top Nav.
 *   not configured  → Sign in link (the login page explains setup)
 *   signed out       → "Sign in" link
 *   signed in        → avatar + dropdown (Account, plan status, Sign out)
 */

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { LogOut, User as UserIcon } from "lucide-react";

import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

export default function UserMenu() {
  const { loading, user, subscription, signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  if (loading) return null;

  if (!user) {
    return (
      <Link
        href="/login?next=%2F%23products"
        className="text-[13px] font-semibold px-4 py-2 rounded-lg transition-colors hover:bg-white/[0.06]"
        style={{ border: "1px solid rgba(255,255,255,0.18)", color: "var(--text)" }}
      >
        Sign in
      </Link>
    );
  }

  const initial = (user.email ?? "?").charAt(0).toUpperCase();
  const active = hasConsoleAccess(subscription);

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-9 h-9 rounded-full flex items-center justify-center font-semibold text-[13px] transition-transform hover:scale-105"
        style={{ background: "var(--cyan)", color: "var(--bg)" }}
        aria-label="Account menu"
      >
        {initial}
      </button>
      {open && (
        <div
          className="absolute right-0 top-[calc(100%+8px)] w-[230px] p-1.5 rounded-xl z-50"
          style={{ background: "var(--surf-hi)", border: "1px solid var(--border-med)", boxShadow: "0 18px 40px -16px rgba(0,0,0,.7)" }}
        >
          <div className="px-3 py-2">
            <div className="text-[12.5px] font-semibold truncate">{user.email}</div>
            <div className="text-[11px] mt-0.5" style={{ color: active ? "var(--cyan)" : "var(--muted)" }}>
              {active ? `${capitalize(subscription?.plan ?? "")} plan · active` : "No active plan"}
            </div>
          </div>
          <div className="h-px my-1" style={{ background: "var(--border)" }} />
          <Link href="/account" onClick={() => setOpen(false)} className="flex items-center gap-2.5 px-3 py-2 rounded-lg text-[12.5px] transition-colors hover:bg-white/[0.06]">
            <UserIcon size={14} style={{ color: "var(--muted)" }} /> Account & billing
          </Link>
          <button onClick={() => { setOpen(false); signOut(); }} className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-[12.5px] text-left transition-colors hover:bg-white/[0.06]">
            <LogOut size={14} style={{ color: "var(--muted)" }} /> Sign out
          </button>
        </div>
      )}
    </div>
  );
}

function capitalize(s: string): string {
  return s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
}
