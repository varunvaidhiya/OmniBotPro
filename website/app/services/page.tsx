import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import { contactMailto, START_HREF, productHref } from "@/lib/site";
import { pageSeo } from "@/lib/seo";

export const metadata = pageSeo({
  path: "/services",
  title: "Robot data services — demonstrations, labeling & eval",
  description:
    "Data is the bottleneck in embodied AI. OhhO collects teleoperation demonstrations, labels and curates episodes, runs evaluation suites, and fine-tunes policies — delivered in open LeRobot format, so the data is always yours.",
});

interface Offer {
  n: string;
  title: string;
  body: string;
  color: string;
}

const OFFERS: Offer[] = [
  {
    n: "01",
    title: "Teleoperation data collection",
    body: "Human demonstrations across embodiments — mobile bases, arms, mobile-manipulators — captured via leader-arm and VR teleop. The fuel imitation-learning and VLA policies actually need.",
    color: "var(--cyan)",
  },
  {
    n: "02",
    title: "Annotation & curation",
    body: "Episode review, keep-or-discard curation, task and success/failure labels, and language annotations — so a training run learns from clean data, not noise.",
    color: "var(--violet-lite)",
  },
  {
    n: "03",
    title: "Evaluation suites",
    body: "Benchmark a policy against real tasks: success rate, safety violations, reachability, latency. An honest scorecard before anything ships to a robot.",
    color: "#FBBF24",
  },
  {
    n: "04",
    title: "Fine-tuning as a service",
    body: "Turn your demonstrations into a deployable policy — SmolVLA, ACT, diffusion or RL — delivered as a checkpoint you own, ready for OhhO Serve or any compatible runtime.",
    color: "#34D399",
  },
];

export default function ServicesPage() {
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
              Data Services
            </span>
          </div>
          <h1 className="font-display font-bold text-[clamp(34px,6vw,68px)] tracking-tight leading-[1.05] mb-6 legible">
            Robot data,<br /><span className="text-cyan">done for you.</span>
          </h1>
          <p className="text-[clamp(16px,2.2vw,20px)] leading-[1.6] max-w-2xl mb-6 legible" style={{ color: "rgba(255,255,255,0.66)" }}>
            Data is the bottleneck in embodied AI — collecting and labeling demonstrations is slow,
            expensive, and the thing every robot-learning team is short on. We do it for you, at a cost
            structure most can&rsquo;t match.
          </p>
          <p className="text-[clamp(15px,2vw,18px)] leading-[1.6] max-w-2xl mb-14 legible" style={{ color: "rgba(255,255,255,0.55)" }}>
            Everything ships in open <strong className="text-white font-semibold">LeRobot format</strong> with ONNX
            exports — so the data and policies are always yours to take anywhere. No lock-in, ever.
          </p>

          {/* ── What we deliver ── */}
          <div className="mb-20">
            <div className="flex items-center gap-3 mb-6">
              <span className="font-mono text-[12px] font-bold" style={{ color: "var(--cyan)" }}>WHAT WE DELIVER //</span>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {OFFERS.map((o) => (
                <div
                  key={o.n}
                  className="rounded-2xl p-6 backdrop-blur-md"
                  style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
                >
                  <div className="flex items-baseline gap-3 mb-2">
                    <span className="font-mono text-[13px] font-bold" style={{ color: o.color }}>{o.n}</span>
                    <div className="font-display text-[18px] font-semibold legible">{o.title}</div>
                  </div>
                  <div className="text-[14px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.64)" }}>{o.body}</div>
                </div>
              ))}
            </div>
          </div>

          {/* ── How it works ── */}
          <div className="mb-20">
            <div className="flex items-center gap-3 mb-8">
              <span className="font-mono text-[12px] font-bold" style={{ color: "var(--violet-lite)" }}>HOW IT WORKS //</span>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {[
                ["Scope", "Tell us the tasks, embodiments and volume you need. We agree a spec, a format and a timeline."],
                ["Collect & label", "Our operators capture and curate the demonstrations — reviewed, labeled and evaluated against your spec."],
                ["Deliver", "You receive a training-ready LeRobot dataset (and, optionally, a fine-tuned checkpoint) that you own outright."],
              ].map(([t, b], i) => (
                <div key={t} className="rounded-2xl p-6 backdrop-blur-md" style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}>
                  <div className="font-mono text-[12px] font-bold mb-2" style={{ color: "var(--cyan)" }}>{`0${i + 1}`}</div>
                  <div className="font-display text-[16px] font-semibold legible mb-1">{t}</div>
                  <div className="text-[13.5px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.62)" }}>{b}</div>
                </div>
              ))}
            </div>
          </div>

          {/* ── Why us ── */}
          <div className="mb-16 rounded-2xl p-8 backdrop-blur-md" style={{ background: "rgba(0,212,255,0.04)", border: "1px solid rgba(0,212,255,0.14)" }}>
            <div className="font-mono text-[11px] tracking-widest uppercase mb-4" style={{ color: "var(--cyan)" }}>Why OhhO for data</div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {[
                ["Open by default", "LeRobot + ONNX + ROS 2. You're never locked to us — the deliverable is portable to any toolchain."],
                ["Cost-efficient delivery", "A cost structure built for volume, so you can afford the data scale real policies need."],
                ["Built on the OhhO stack", "The same engine that records, trains and serves powers the pipeline — collection to checkpoint, one loop."],
              ].map(([t, b]) => (
                <div key={t}>
                  <div className="font-display text-[15px] font-semibold legible mb-1">{t}</div>
                  <div className="text-[13.5px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.62)" }}>{b}</div>
                </div>
              ))}
            </div>
          </div>

          {/* ── CTA ── */}
          <div className="flex items-center gap-4 flex-wrap">
            <a
              href={contactMailto("OhhO data services — request a quote")}
              className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Talk to us — get a quote →
            </a>
            <a
              href={productHref("data")}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "var(--text)", border: "1px solid rgba(255,255,255,0.16)" }}
            >
              See the Data console
            </a>
            <a
              href={START_HREF}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "rgba(255,255,255,0.6)", border: "1px solid rgba(255,255,255,0.1)" }}
            >
              Or collect it yourself
            </a>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
