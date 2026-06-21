"use client";

/*
 * ConsoleGate — wraps a product console (e.g. /build, /serve) and enforces
 * only that the user is signed in. No paid plan required to access consoles.
 *
 *   not signed in  → /login?next=…
 *   signed in      → render the console
 *
 * Cloud-cost features within a console (GPU training, AI inference, MCP,
 * cloud simulation) are individually gated by PaidFeatureGate.
 */

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Loader2, Lock } from "lucide-react";

import { useAuth } from "@/lib/auth/AuthProvider";

export default function ConsoleGate({ product, children }: { product: string; children: React.ReactNode }) {
  const { loading, user } = useAuth();
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
    setAllowed(true);
  }, [loading, user, product, router]);

  if (loading || !allowed) {
    const tag = redirecting ? "signing in…" : "checking access…";
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ background: "var(--bg)" }}>
        <div className="flex flex-col items-center gap-3" style={{ color: "var(--faint)" }}>
          {tag === "checking access…" ? <Loader2 size={26} className="animate-spin" /> : <Lock size={24} />}
          <span className="text-[12.5px] font-mono">{tag}{redirecting ? " — redirecting…" : ""}</span>
        </div>
      </div>
    );
  }
  return <>{children}</>;
}
