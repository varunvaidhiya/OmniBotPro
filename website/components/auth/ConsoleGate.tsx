"use client";

/*
 * ConsoleGate — wraps a product console (e.g. /build, /serve) and enforces:
 *   not signed in        → /login?next=…
 *   signed in, no plan   → /upgrade?next=…
 *   signed in + active   → render the console
 */

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { CreditCard, Loader2, Lock } from "lucide-react";

import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

export default function ConsoleGate({ product, children }: { product: string; children: React.ReactNode }) {
  const { loading, user, subscription } = useAuth();
  const router = useRouter();
  const [allowed, setAllowed] = useState(false);
  const [redirecting, setRedirecting] = useState(false);

  useEffect(() => {
    if (loading) return;
    const here = typeof window !== "undefined" ? window.location.pathname : `/${product}`;
    if (!user) {
      setAllowed(false);
      setRedirecting(true);
      router.replace(`/login?next=${encodeURIComponent(here)}`);
      return;
    }
    if (!hasConsoleAccess(subscription)) {
      setAllowed(false);
      setRedirecting(true);
      router.replace(`/upgrade?next=${encodeURIComponent(here)}`);
      return;
    }
    setAllowed(true);
  }, [loading, user, subscription, product, router]);

  if (loading || !allowed) {
    const sub = hasConsoleAccess(subscription);
    const tag = !user ? "signing in…" : !sub ? "plan needed" : "checking access…";
    return <GateScreen tag={tag} redirecting={redirecting && !sub} />;
  }
  return <>{children}</>;
}

function GateScreen({ tag, redirecting }: { tag: string; redirecting: boolean }) {
  return (
    <div className="min-h-screen flex items-center justify-center" style={{ background: "var(--bg)" }}>
      <div className="flex flex-col items-center gap-3" style={{ color: "var(--faint)" }}>
        {redirecting ? <CreditCard size={24} /> : tag === "checking access…" ? <Loader2 size={26} className="animate-spin" /> : <Lock size={24} />}
        <span className="text-[12.5px] font-mono">{tag}{redirecting ? " — redirecting…" : ""}</span>
      </div>
    </div>
  );
}
