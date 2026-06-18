"use client";

/*
 * ConsoleGate — wraps a product console (e.g. /build, /serve) and enforces:
 *   not signed in        → /login?next=…
 *   signed in, no plan   → /upgrade?next=…&product=…   (the payment page)
 *   active subscription  → render the console
 *
 * When Supabase isn't configured the gate is bypassed (children render) so the
 * site stays demoable before credentials are wired up.
 */

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Loader2, Lock } from "lucide-react";

import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

export default function ConsoleGate({ product, children }: { product: string; children: React.ReactNode }) {
  const { configured, loading, user, subscription } = useAuth();
  const router = useRouter();
  const [allowed, setAllowed] = useState(false);

  useEffect(() => {
    if (!configured || loading) return;
    const here = typeof window !== "undefined" ? window.location.pathname : `/${product}`;
    if (!user) {
      router.replace(`/login?next=${encodeURIComponent(here)}`);
      return;
    }
    if (!hasConsoleAccess(subscription)) {
      router.replace(`/upgrade?next=${encodeURIComponent(here)}&product=${product}`);
      return;
    }
    setAllowed(true);
  }, [configured, loading, user, subscription, product, router]);

  // Not configured → open (graceful degradation, keeps the live demo usable).
  if (!configured) return <>{children}</>;
  if (loading || !allowed) return <GateScreen checking={loading || !!user} />;
  return <>{children}</>;
}

function GateScreen({ checking }: { checking: boolean }) {
  return (
    <div className="min-h-screen flex items-center justify-center" style={{ background: "var(--bg)" }}>
      <div className="flex flex-col items-center gap-3" style={{ color: "var(--faint)" }}>
        {checking ? <Loader2 size={26} className="animate-spin" /> : <Lock size={24} />}
        <span className="text-[12.5px] font-mono">{checking ? "checking access…" : "redirecting…"}</span>
      </div>
    </div>
  );
}
