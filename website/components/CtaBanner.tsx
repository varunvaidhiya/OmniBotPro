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
          <div
            className="inline-flex items-center gap-2 font-mono text-[11px] tracking-[0.08em] uppercase px-3 py-1.5 rounded-full mb-6"
            style={{ color: "var(--cyan)", background: "rgba(0,212,255,.08)", border: "1px solid rgba(0,212,255,.22)" }}
          >
            <span style={{ width: 6, height: 6, borderRadius: "50%", background: "var(--cyan)", display: "inline-block" }} />
            100% open source · MIT / Apache
          </div>
          <h2
            className="font-display font-bold tracking-[-0.035em] leading-[1.08] mb-[18px] legible"
            style={{ fontSize: "clamp(36px, 6vw, 66px)" }}
          >
            The brand-agnostic<br />robotics platform.
          </h2>
          <p className="text-[16px] mb-3 leading-[1.65] max-w-[480px] mx-auto" style={{ color: "rgba(255,255,255,0.66)" }}>
            One open umbrella to build, train, launch, deploy, manage and regulate any robot — any GPU, any brand, any model, any cloud.
          </p>
          <p className="text-[14px] mb-10 leading-[1.65] max-w-[440px] mx-auto" style={{ color: "rgba(255,255,255,0.42)" }}>
            No credit card. No vendor lock-in. Pay only when you run GPU training, AI inference, or cloud simulation on OhhO infrastructure.
          </p>
          <div className="flex items-center justify-center gap-3 flex-wrap">
            <a
              href={SIGNUP_HREF}
              className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5 hover:opacity-95"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLElement).style.boxShadow = "0 10px 32px rgba(0,212,255,0.32)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLElement).style.boxShadow = "";
              }}
            >
              Start Free — No Card Needed <ArrowRight size={15} strokeWidth={2.5} />
            </a>
            <a
              href="https://github.com/varunvaidhiya/OmniBotPro"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 text-sm font-medium px-[22px] py-[12px] rounded-lg transition-all duration-200"
              style={{ border: "1px solid rgba(255,255,255,0.18)", color: "rgba(255,255,255,0.75)" }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,.32)";
                (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,.04)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,0.18)";
                (e.currentTarget as HTMLElement).style.background = "";
              }}
            >
              View on GitHub
            </a>
          </div>
        </GlassCard>
      </div>
    </section>
  );
}
