import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import { SIGNUP_HREF, START_HREF, SERVICES_HREF, contactMailto } from "@/lib/site";
import { pageSeo } from "@/lib/seo";

export const metadata = pageSeo({
  path: "/for-builders",
  title: "For Builders — robotics for the real world's budget",
  description:
    "Where labor is cheap, robots don't replace workers — they add quality, consistency, safety and 24/7 scale. OhhO is the open, vendor-neutral stack that runs on low-cost hardware and is priced for local budgets.",
});

interface Fit {
  n: string;
  title: string;
  body: string;
  color: string;
}

const FITS: Fit[] = [
  {
    n: "01",
    title: "Runs on cheap hardware",
    body: "A Raspberry Pi, a laptop, a low-cost NPU — no mandatory GPU and no vendor's silicon. The engine is designed for the edge, so your bill of materials stays small.",
    color: "var(--cyan)",
  },
  {
    n: "02",
    title: "Priced for local budgets",
    body: "The open engine is free and self-hostable. Cloud is pay-per-use for what you actually run — not a flat per-robot-per-month fee that only pencils out in a Western cost base.",
    color: "var(--violet-lite)",
  },
  {
    n: "03",
    title: "Vendor-neutral & open",
    body: "LeRobot, ROS 2, ONNX and Apache-2.0. Your data, robots and policies are portable forever — adopt without betting your company on a single supplier.",
    color: "#FBBF24",
  },
  {
    n: "04",
    title: "Support & services on tap",
    body: "When you need demonstrations collected, episodes labeled or a policy fine-tuned, our data services deliver it — so a small team can ship real autonomy.",
    color: "#34D399",
  },
];

export default function ForBuildersPage() {
  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />

        <div className="max-w-5xl w-full mx-auto px-6 relative z-10">
          {/* ── Hero ── */}
          <div className="flex items-center gap-3 mb-6">
            <span className="badge-dot" />
            <span className="text-[13px] font-mono tracking-widest uppercase" style={{ color: "var(--cyan)" }}>
              For Builders
            </span>
          </div>
          <h1 className="font-display font-bold text-[clamp(34px,6vw,64px)] tracking-tight leading-[1.05] mb-6 legible">
            Robotics for the<br />real world&rsquo;s <span className="text-cyan">budget.</span>
          </h1>
          <p className="text-[clamp(16px,2.2vw,20px)] leading-[1.6] max-w-2xl mb-14 legible" style={{ color: "rgba(255,255,255,0.66)" }}>
            Most of the world doesn&rsquo;t buy robots to replace cheap labor — it buys them for
            <strong className="text-white font-semibold"> quality, consistency, safety in hazardous work,
            precision, and 24/7 scale.</strong> OhhO is built for that reality: open, vendor-neutral, and
            priced for the budgets robots actually have to earn against.
          </p>

          {/* ── The different math ── */}
          <div className="mb-20 rounded-2xl p-8 backdrop-blur-md" style={{ background: "rgba(124,58,237,0.05)", border: "1px solid rgba(124,58,237,0.16)" }}>
            <div className="font-mono text-[11px] tracking-widest uppercase mb-3" style={{ color: "var(--violet-lite)" }}>The different math</div>
            <p className="text-[15px] leading-[1.75] max-w-3xl" style={{ color: "rgba(255,255,255,0.72)" }}>
              A robot that has to justify replacing a $50,000 worker is a very different product from one
              that has to add reliability to a job a person already does cheaply. Priced and designed for
              the first, most platforms simply don&rsquo;t fit the second. OhhO does — because the value is
              in the outcome (fewer defects, safer work, round-the-clock throughput), not in the payroll it
              removes.
            </p>
          </div>

          {/* ── Built for it ── */}
          <div className="mb-20">
            <div className="flex items-center gap-3 mb-6">
              <span className="font-mono text-[12px] font-bold" style={{ color: "var(--cyan)" }}>BUILT FOR IT //</span>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {FITS.map((f) => (
                <div
                  key={f.n}
                  className="rounded-2xl p-6 backdrop-blur-md"
                  style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
                >
                  <div className="flex items-baseline gap-3 mb-2">
                    <span className="font-mono text-[13px] font-bold" style={{ color: f.color }}>{f.n}</span>
                    <div className="font-display text-[18px] font-semibold legible">{f.title}</div>
                  </div>
                  <div className="text-[14px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.64)" }}>{f.body}</div>
                </div>
              ))}
            </div>
          </div>

          {/* ── Who it's for ── */}
          <div className="mb-16">
            <div className="flex items-center gap-3 mb-8">
              <span className="font-mono text-[12px] font-bold" style={{ color: "#FBBF24" }}>WHO IT&rsquo;S FOR //</span>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {[
                ["SMEs & industry", "Warehousing, agritech, manufacturing and inspection teams that need affordable automation, not enterprise price tags."],
                ["Labs & education", "Universities and training programs building on low-cost, open, standards-based hardware and software."],
                ["Global-South builders", "Robot startups and integrators serving markets the expensive Western platforms don't reach — the seam OhhO is built for."],
              ].map(([t, b]) => (
                <div key={t} className="rounded-2xl p-6 backdrop-blur-md" style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}>
                  <div className="font-display text-[16px] font-semibold legible mb-1">{t}</div>
                  <div className="text-[13.5px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.62)" }}>{b}</div>
                </div>
              ))}
            </div>
          </div>

          {/* ── CTA ── */}
          <div className="flex items-center gap-4 flex-wrap">
            <a
              href={SIGNUP_HREF}
              className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Start free →
            </a>
            <a
              href={START_HREF}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "var(--text)", border: "1px solid rgba(255,255,255,0.16)" }}
            >
              Quickstart
            </a>
            <a
              href={SERVICES_HREF}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "rgba(255,255,255,0.6)", border: "1px solid rgba(255,255,255,0.1)" }}
            >
              Data services
            </a>
            <a
              href={contactMailto("OhhO for builders — let's talk")}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "rgba(255,255,255,0.6)", border: "1px solid rgba(255,255,255,0.1)" }}
            >
              Talk to us
            </a>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
