import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import { SIGNUP_HREF, START_HREF, GITHUB_HREF } from "@/lib/site";
import { pageSeo } from "@/lib/seo";

export const metadata = pageSeo({
  path: "/why",
  title: "Why OhhO — open is the wedge, data is the moat",
  description:
    "No vendor lock-in is why it's safe to adopt OhhO — not, by itself, a moat. Openness wins adoption; compounding data, operational excellence and open-standard interop are what keep you and block rivals.",
});

interface Pillar {
  n: string;
  title: string;
  body: string;
  color: string;
}

// The moat is everything openness is NOT: earned switching costs + speed + scale.
const MOAT: Pillar[] = [
  {
    n: "01",
    title: "Data gravity",
    body: "Every demonstration, skill and evaluation you capture on OhhO compounds — your models get better here, and the dataset is yours to take anywhere. It's the one switching cost that's earned, not imposed.",
    color: "var(--cyan)",
  },
  {
    n: "02",
    title: "Best-operated",
    body: "The canonical managed cloud, run by the people who build the engine. Like Red Hat with Linux or Vercel with Next.js: the code is free and portable — you stay because we run it best and ship fastest.",
    color: "var(--violet-lite)",
  },
  {
    n: "03",
    title: "Open-standard interop",
    body: "Built on LeRobot, ROS 2 and ONNX — we extend the open ecosystem, we don't wall it. Bring any brain, any robot, any runtime. Complementary to the giants, not a fragile clone of them.",
    color: "#FBBF24",
  },
  {
    n: "04",
    title: "Human-in-the-loop services",
    body: "Teleoperation data collection, labeling and evaluation at a cost structure most can't match. A business, not a repo — relationships and scale that code can't fork.",
    color: "#34D399",
  },
];

export default function WhyPage() {
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
              Why OhhO
            </span>
          </div>
          <h1 className="font-display font-bold text-[clamp(34px,6vw,68px)] tracking-tight leading-[1.05] mb-6 legible">
            Open gets you in.<br /><span className="text-cyan">Your data</span> keeps you.
          </h1>
          <p className="text-[clamp(16px,2.2vw,20px)] leading-[1.6] max-w-2xl mb-6 legible" style={{ color: "rgba(255,255,255,0.66)" }}>
            No vendor lock-in is why it&rsquo;s <strong className="text-white font-semibold">safe to adopt</strong> OhhO.
            It is not, by itself, a moat — a moat is what makes leaving costly and copying hard, and
            &ldquo;you can leave anytime&rdquo; is the opposite of that.
          </p>
          <p className="text-[clamp(16px,2.2vw,20px)] leading-[1.6] max-w-2xl mb-14 legible" style={{ color: "rgba(255,255,255,0.66)" }}>
            So here&rsquo;s the honest version: <strong className="text-white font-semibold">openness is the wedge; your
            compounding data is the moat.</strong> You should be able to walk away at any time — and choose
            not to, because it keeps getting better.
          </p>

          {/* ── The wedge ── */}
          <div className="mb-20">
            <div className="flex items-center gap-3 mb-6">
              <span className="font-mono text-[12px] font-bold" style={{ color: "var(--cyan)" }}>THE WEDGE //</span>
              <h2 className="font-display font-bold text-[clamp(22px,3vw,32px)] tracking-tight legible">
                Openness lowers the barrier to adopt
              </h2>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {[
                ["Apache-2.0 engine", "The core is open source. Self-host it, fork it, audit it — no black boxes."],
                ["Portable forever", "Standard LeRobot datasets, ONNX exports and ROS 2 topics. The work you do on OhhO moves to anything."],
                ["Runs anywhere", "A Raspberry Pi, a laptop, a GPU workstation, or your own cloud. No vendor's silicon required."],
                ["One argument to switch", "Swap simulator → real robot, native runtime → ROS 2, one robot → another. The behavior code doesn't change."],
              ].map(([t, b]) => (
                <div
                  key={t}
                  className="rounded-2xl p-5 backdrop-blur-md"
                  style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
                >
                  <div className="font-display text-[16px] font-semibold legible mb-1">{t}</div>
                  <div className="text-[13.5px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.62)" }}>{b}</div>
                </div>
              ))}
            </div>
            <p className="text-[14px] leading-[1.6] mt-6 max-w-2xl font-mono" style={{ color: "rgba(255,255,255,0.4)" }}>
              &ldquo;Come try it — it&rsquo;s safe, you can always leave.&rdquo; Great for getting in the door. Terrible as
              the whole strategy, because it lowers the barrier to leave, too.
            </p>
          </div>

          {/* ── The moat ── */}
          <div className="mb-20">
            <div className="flex items-center gap-3 mb-6">
              <span className="font-mono text-[12px] font-bold" style={{ color: "var(--violet-lite)" }}>THE MOAT //</span>
              <h2 className="font-display font-bold text-[clamp(22px,3vw,32px)] tracking-tight legible">
                What actually keeps you — and blocks rivals
              </h2>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {MOAT.map((p) => (
                <div
                  key={p.n}
                  className="rounded-2xl p-6 backdrop-blur-md"
                  style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
                >
                  <div className="flex items-baseline gap-3 mb-2">
                    <span className="font-mono text-[13px] font-bold" style={{ color: p.color }}>{p.n}</span>
                    <div className="font-display text-[18px] font-semibold legible">{p.title}</div>
                  </div>
                  <div className="text-[14px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.64)" }}>{p.body}</div>
                </div>
              ))}
            </div>
          </div>

          {/* ── What it means ── */}
          <div className="mb-20 rounded-2xl p-8 backdrop-blur-md" style={{ background: "rgba(0,212,255,0.04)", border: "1px solid rgba(0,212,255,0.14)" }}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              <div>
                <div className="font-mono text-[11px] tracking-widest uppercase mb-2" style={{ color: "var(--cyan)" }}>You can adopt freely</div>
                <p className="text-[15px] leading-[1.7]" style={{ color: "rgba(255,255,255,0.7)" }}>
                  No lock-in, fully portable, self-hostable. If OhhO ever stops earning your trust, your
                  robots, data and policies come with you. That promise is real and it&rsquo;s permanent.
                </p>
              </div>
              <div>
                <div className="font-mono text-[11px] tracking-widest uppercase mb-2" style={{ color: "var(--violet-lite)" }}>And you get better over time</div>
                <p className="text-[15px] leading-[1.7]" style={{ color: "rgba(255,255,255,0.7)" }}>
                  Because the data compounds and the platform is run by the people who build the engine,
                  staying is the rational choice — not the trapped one. We measure success as retention
                  we <em>earned</em>, never retention we <em>imposed</em>.
                </p>
              </div>
            </div>
          </div>

          {/* ── CTA ── */}
          <div className="flex items-center gap-4 flex-wrap">
            <a
              href={START_HREF}
              className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Start in 5 minutes →
            </a>
            <a
              href={SIGNUP_HREF}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "var(--text)", border: "1px solid rgba(255,255,255,0.16)" }}
            >
              Start free
            </a>
            <a
              href={GITHUB_HREF}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "rgba(255,255,255,0.6)", border: "1px solid rgba(255,255,255,0.1)" }}
            >
              View on GitHub
            </a>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
