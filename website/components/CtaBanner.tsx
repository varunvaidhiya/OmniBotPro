"use client";

import GlassCard from "@/components/GlassCard";
import { ArrowRight } from "lucide-react";
import { SIGNUP_HREF } from "@/lib/site";

export default function CtaBanner() {
  return (
    <section className="relative overflow-hidden" style={{ padding: "112px 24px" }}>
      <div className="cta-bg" />
      <div className="max-w-content mx-auto">
        <GlassCard
          interactive={false}
          padding="64px 40px"
          radius={28}
          className="text-center max-w-[680px] mx-auto"
        >
          <h2
            className="font-display font-bold tracking-[-0.035em] leading-[1.08] mb-[18px] legible"
            style={{ fontSize: "clamp(40px, 7vw, 72px)" }}
          >
            Ready to<br />operate?
          </h2>
          <p className="text-[16px] mb-10 leading-[1.65]" style={{ color: "rgba(255,255,255,0.66)" }}>
            Start free. No hardware required — simulate first.
          </p>
          <a
            href={SIGNUP_HREF}
            className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg mx-auto transition-all duration-200 hover:-translate-y-0.5 hover:opacity-95"
            style={{ background: "var(--cyan)", color: "var(--bg)" }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLElement).style.boxShadow = "0 10px 32px rgba(0,212,255,0.32)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLElement).style.boxShadow = "";
            }}
          >
            Get Started with OhhO <ArrowRight size={15} strokeWidth={2.5} />
          </a>
        </GlassCard>
      </div>
    </section>
  );
}
