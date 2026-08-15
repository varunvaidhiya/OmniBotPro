"use client";

import { useState } from "react";
import Link from "next/link";
import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import ContactModal from "@/components/team/ContactModal";
import { GITHUB_HREF } from "@/lib/site";

const EMAIL = "varun.vaidhiya@gmail.com";
const PHONE = "+44 7587 815427";

export default function Team() {
  const [contactOpen, setContactOpen] = useState(false);

  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden flex flex-col items-center">
        <div className="hero-grid" />
        <div className="hero-orb-1" />
        <div className="hero-orb-2" />

        <div className="max-w-4xl w-full px-6 relative z-10">
          <div className="flex items-center gap-3 mb-8 justify-center">
            <span className="badge-dot" />
            <span className="text-[13px] font-mono tracking-widest uppercase text-cyan">Team</span>
          </div>

          <h1 className="text-4xl md:text-5xl lg:text-[64px] font-bold tracking-tight mb-6 text-center leading-[1.1]">
            Meet the founder
          </h1>

          <p className="text-lg md:text-[21px] text-white/70 mb-20 text-center max-w-2xl mx-auto leading-relaxed">
            OhhO is built by a single founder — on purpose. Small, fast, and obsessed with making robots easier to operate.
          </p>

          {/* ── Founder card ─────────────────────────────────────────────── */}
          <section className="bg-white/[0.03] border border-white/[0.07] rounded-3xl p-8 md:p-12 backdrop-blur-md flex flex-col md:flex-row items-center md:items-start gap-8 md:gap-10">
            {/* monogram avatar (no portrait photo on file yet) */}
            <div className="shrink-0 w-32 h-32 md:w-40 md:h-40 rounded-full flex items-center justify-center"
              style={{
                background: "linear-gradient(135deg, rgba(0,212,255,.18), rgba(124,58,237,.18))",
                border: "1px solid rgba(0,212,255,.28)",
                boxShadow: "0 0 40px -12px rgba(0,212,255,.35)",
              }}
              aria-hidden
            >
              <span className="text-[44px] md:text-[56px] font-bold tracking-tight"
                style={{ background: "linear-gradient(135deg, var(--cyan), var(--violet))", WebkitBackgroundClip: "text", WebkitTextFillColor: "transparent" }}>
                VV
              </span>
            </div>

            <div className="flex-1 text-center md:text-left">
              <h2 className="text-[26px] md:text-[30px] font-bold text-white mb-1">Varun Vaidhiya</h2>
              <p className="text-[14px] font-medium text-cyan mb-5">Founder &amp; CEO</p>

              <div className="space-y-4 text-[16px] md:text-[17px] text-white/70 leading-relaxed">
                <p>
                  Varun is the solo founder behind OhhO. He designs the robots, writes the software, trains the policies, ships the product, and answers the support tickets — because the best way to build tools for builders is to be one.
                </p>
                <p>
                  He started OhhO after years of rebuilding the same robot plumbing on every project: a ROS workspace, a Docker image, a simulator, a data pipeline, a model server. OhhO is that stack, productized — so the next person can skip the plumbing and start at the interesting part.
                </p>
                <p>
                  The company stays small on purpose. One founder, one focus, no committee — which is exactly why it ships faster than teams ten times the size.
                </p>
              </div>

              <div className="flex flex-wrap gap-2.5 mt-7 justify-center md:justify-start">
                <Link
                  href={GITHUB_HREF}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 text-[12.5px] font-medium px-3.5 py-2 rounded-lg transition-all hover:-translate-y-px"
                  style={{ border: "1px solid rgba(255,255,255,.12)", color: "var(--text)" }}
                >
                  GitHub
                  <svg className="w-3 h-3 opacity-60" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 17L17 7M17 7H7M17 7v10" /></svg>
                </Link>
              </div>
            </div>
          </section>

          {/* ── Join the mission ─────────────────────────────────────────── */}
          <section className="mt-8 bg-white/[0.03] border border-white/[0.07] rounded-3xl p-8 md:p-12 backdrop-blur-md text-center">
            <h2 className="text-2xl font-bold mb-4 text-white flex items-center justify-center gap-3">
              <span className="text-violet text-xl">02 //</span> Join the mission
            </h2>
            <p className="text-[17px] text-white/70 leading-relaxed max-w-2xl mx-auto mb-8">
              OhhO is a one-person company today, but not for long. If you care about embodied AI, robot tooling, and shipping real software for real hardware — this is ground floor. Reach out and tell us what you&apos;d build.
            </p>
            <button
              onClick={() => setContactOpen(true)}
              className="inline-flex items-center gap-2 text-[14px] font-semibold px-5 py-3 rounded-xl transition-all hover:-translate-y-px"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Get in touch
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 17L17 7M17 7H7M17 7v10" /></svg>
            </button>
          </section>

          {/* ── Volunteer with OhhO ───────────────────────────────────────── */}
          <section className="mt-8 bg-white/[0.03] border border-white/[0.07] rounded-3xl p-8 md:p-12 backdrop-blur-md">
            <h2 className="text-2xl font-bold mb-5 text-white flex items-center gap-3">
              <span className="text-violet text-xl">03 //</span> Volunteer with OhhO
            </h2>

            <div className="space-y-4 text-[16px] md:text-[17px] text-white/70 leading-relaxed max-w-2xl mx-auto">
              <p>
                OhhO is a bootstrapped, pre-revenue company built by a single founder. We&apos;re a passionate, dedicated volunteer workforce, united by a vision to make robots easier to operate — and we&apos;re looking for talented tech specialists with at least <span className="text-white font-medium">6 hours of free time each week</span> to contribute and drive real impact.
              </p>
              <p>
                Your contributions will be instrumental in advancing a platform with enormous global potential, and in shaping our future as a team. Once we secure the necessary investment and runway, our intent is to reward our volunteers and bring them on as full-time, foundational team members.
              </p>
              <p>
                If you&apos;re an innovative thinker with a commitment to seeing OhhO lead the next frontier in embodied AI and robot tooling, we encourage you to reach out. Your effort could build the future.
              </p>
              <p className="pt-1">
                Please forward your CV to{" "}
                <a
                  href={`mailto:${EMAIL}?subject=${encodeURIComponent("Volunteer with OhhO")}`}
                  className="text-cyan font-medium underline decoration-cyan/40 underline-offset-4 hover:decoration-cyan transition-colors"
                  style={{ color: "var(--cyan)" }}
                >
                  {EMAIL}
                </a>
                .
              </p>
            </div>
          </section>
        </div>
      </main>
      <Footer />

      {contactOpen && <ContactModal email={EMAIL} phone={PHONE} onClose={() => setContactOpen(false)} />}
    </>
  );
}
