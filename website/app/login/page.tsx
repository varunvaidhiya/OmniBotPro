"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ArrowLeft, ArrowRight, Check, Loader2, Mail } from "lucide-react";

import Nav from "@/components/Nav";
import GlassCard from "@/components/GlassCard";
import OAuthButtons from "@/components/auth/OAuthButtons";
import { useAuth } from "@/lib/auth/AuthProvider";
import { enabledOAuthProviders } from "@/lib/auth/supabase";

const hasOAuth = enabledOAuthProviders.length > 0;

export default function LoginPage() {
  const { configured, user, signInWithEmail } = useAuth();
  const router = useRouter();
  const [next, setNext] = useState("/account");
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // read ?next= from the URL (avoids useSearchParams' suspense requirement)
  useEffect(() => {
    const n = new URLSearchParams(window.location.search).get("next");
    if (n) setNext(n);
  }, []);

  // already signed in → bounce to the destination
  useEffect(() => {
    if (configured && user) router.replace(next);
  }, [configured, user, next, router]);

  const submitEmail = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim() || busy) return;
    setBusy(true);
    setError(null);
    const res = await signInWithEmail(email.trim(), next);
    setBusy(false);
    if (res.error) setError(res.error);
    else setSent(true);
  };

  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden flex items-start justify-center">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-60" />
        <div className="hero-orb-2 opacity-60" />

        <div className="relative z-10 w-full max-w-[420px] px-6">
          <Link href="/" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-widest uppercase mb-8 transition-colors hover:text-white" style={{ color: "var(--cyan)" }}>
            <ArrowLeft size={14} /> Home
          </Link>

          <GlassCard accent="cyan" interactive={false} padding="32px" radius={22}>
            <h1 className="font-display font-bold text-[26px] tracking-tight mb-1.5">Sign in to OhhO</h1>
            <p className="text-[13.5px] mb-7" style={{ color: "rgba(255,255,255,0.6)" }}>
              Access your robot designer, inference console and fleet.
            </p>

            {!configured ? (
              <div className="p-4 rounded-xl text-[13px] leading-[1.6]" style={{ background: "rgba(251,191,36,.08)", border: "1px solid rgba(251,191,36,.4)", color: "rgba(255,255,255,.8)" }}>
                Authentication isn&apos;t configured on this deployment yet. Add your Supabase keys (see <span className="font-mono">AUTH_SETUP.md</span>) to enable email, Google and Apple sign-in.
              </div>
            ) : sent ? (
              <div className="p-4 rounded-xl flex items-start gap-3" style={{ background: "rgba(52,211,153,.08)", border: "1px solid rgba(52,211,153,.4)" }}>
                <Check size={18} style={{ color: "#34D399", flexShrink: 0, marginTop: 2 }} />
                <div className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,.82)" }}>
                  Check <span className="font-semibold">{email}</span> for a magic link. Open it on this device to finish signing in.
                </div>
              </div>
            ) : (
              <>
                <OAuthButtons next={next} />

                {hasOAuth && (
                  <div className="flex items-center gap-3 my-5">
                    <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
                    <span className="text-[11px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>or email</span>
                    <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
                  </div>
                )}

                <form onSubmit={submitEmail} className="flex flex-col gap-2.5">
                  <div className="relative">
                    <Mail size={15} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "var(--faint)" }} />
                    <input
                      type="email"
                      required
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="you@company.com"
                      className="w-full bg-transparent text-[13.5px] pl-9 pr-3 py-2.5 rounded-lg outline-none focus:border-cyan"
                      style={{ border: "1px solid var(--border-med)", color: "#fff" }}
                    />
                  </div>
                  {error && <p className="text-[12px]" style={{ color: "#F87171" }}>{error}</p>}
                  <button
                    type="submit"
                    disabled={busy}
                    className="inline-flex items-center justify-center gap-2 w-full py-2.5 rounded-lg text-[13.5px] font-semibold transition-all disabled:opacity-50 enabled:hover:-translate-y-px"
                    style={{ background: "var(--cyan)", color: "var(--bg)" }}
                  >
                    {busy ? <Loader2 size={15} className="animate-spin" /> : <>Send magic link <ArrowRight size={14} strokeWidth={2.5} /></>}
                  </button>
                </form>
              </>
            )}

            <p className="text-[11.5px] mt-6 leading-[1.6]" style={{ color: "var(--faint)" }}>
              By continuing you agree to OhhO&apos;s Terms and acknowledge the Privacy Policy.
            </p>
          </GlassCard>
        </div>
      </main>
    </>
  );
}
