import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import { WHY_HREF, DOCS_HREF, GITHUB_HREF } from "@/lib/site";
import { pageSeo } from "@/lib/seo";

export const metadata = pageSeo({
  path: "/start",
  title: "Quickstart — drive a robot in 5 minutes",
  description:
    "Install the open OhhO engine, drive a robot in simulation, then point the same code at real hardware with one argument. Works with or without ROS 2.",
});

// A static, server-rendered code block (no client JS needed).
function Code({ children, label }: { children: string; label?: string }) {
  return (
    <div className="rounded-xl overflow-hidden mb-4" style={{ border: "1px solid rgba(255,255,255,0.09)", background: "rgba(4,7,14,0.6)" }}>
      {label && (
        <div className="px-4 py-2 font-mono text-[11px] tracking-wide" style={{ color: "rgba(255,255,255,0.4)", borderBottom: "1px solid rgba(255,255,255,0.07)", background: "rgba(255,255,255,0.02)" }}>
          {label}
        </div>
      )}
      <pre className="px-4 py-4 overflow-x-auto text-[13px] leading-[1.7] font-mono" style={{ color: "rgba(255,255,255,0.82)" }}>
        <code>{children}</code>
      </pre>
    </div>
  );
}

const STEPS: { n: string; title: string; body: string; code: string; codeLabel: string }[] = [
  {
    n: "02",
    title: "Check your environment",
    body: "Doctor reports the runtimes, adapters and robots available on your machine — with or without ROS 2.",
    codeLabel: "shell",
    code: "ohho doctor",
  },
  {
    n: "03",
    title: "Drive a robot in simulation",
    body: "No hardware required — a deterministic simulator runs the same API as a real robot.",
    codeLabel: "shell",
    code: "ohho sim --robot omnibot --seconds 5",
  },
  {
    n: "04",
    title: "Give it a brain",
    body: "Run the agent loop — perceive → reason → act → reflect — against the simulated robot.",
    codeLabel: "shell",
    code: 'ohho agent omnibot "explore the room"',
  },
];

export default function StartPage() {
  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />

        <div className="max-w-3xl w-full mx-auto px-6 relative z-10">
          {/* ── Hero ── */}
          <div className="flex items-center gap-3 mb-6">
            <span className="badge-dot" />
            <span className="text-[13px] font-mono tracking-widest uppercase" style={{ color: "var(--cyan)" }}>
              Quickstart
            </span>
          </div>
          <h1 className="font-display font-bold text-[clamp(32px,5.5vw,60px)] tracking-tight leading-[1.06] mb-6 legible">
            Drive a robot in 5 minutes.<br /><span className="text-cyan">Swap to real hardware</span> with one argument.
          </h1>
          <p className="text-[clamp(15px,2.2vw,19px)] leading-[1.6] max-w-2xl mb-14 legible" style={{ color: "rgba(255,255,255,0.66)" }}>
            <strong className="text-white font-semibold">OhhO OS</strong> is the open engine underneath the platform.
            Install it, drive a robot in simulation, then point the same code at real hardware — native
            runtime or ROS 2, your choice.
          </p>

          {/* ── Step 1: install ── */}
          <div className="mb-14">
            <div className="flex items-baseline gap-3 mb-3">
              <span className="font-mono text-[13px] font-bold" style={{ color: "var(--cyan)" }}>01</span>
              <h2 className="font-display text-[20px] font-semibold legible">Install the engine</h2>
            </div>
            <p className="text-[14px] leading-[1.6] mb-4" style={{ color: "rgba(255,255,255,0.62)" }}>
              Install from source today — the dependency-free base needs only Python 3.10+.
            </p>
            <Code label="shell — from source (works today)">{`git clone https://github.com/varunvaidhiya/OmniBotPro
cd OmniBotPro
pip install -e sdk`}</Code>
            <div className="flex items-center gap-2 mt-3 mb-2 font-mono text-[11px] uppercase tracking-wide" style={{ color: "rgba(255,255,255,0.4)" }}>
              <span className="px-2 py-[3px] rounded-full" style={{ background: "rgba(240,179,74,.14)", color: "#f0b34a", border: "1px solid rgba(240,179,74,.3)" }}>coming soon</span>
              one-line install from PyPI
            </div>
            <Code label="shell — PyPI (not yet published)">{`pip install ohho-os   # coming soon`}</Code>
          </div>

          {/* ── Steps 2-4 ── */}
          {STEPS.map((s) => (
            <div key={s.n} className="mb-14">
              <div className="flex items-baseline gap-3 mb-3">
                <span className="font-mono text-[13px] font-bold" style={{ color: "var(--cyan)" }}>{s.n}</span>
                <h2 className="font-display text-[20px] font-semibold legible">{s.title}</h2>
              </div>
              <p className="text-[14px] leading-[1.6] mb-4" style={{ color: "rgba(255,255,255,0.62)" }}>{s.body}</p>
              <Code label={s.codeLabel}>{s.code}</Code>
            </div>
          ))}

          {/* ── One behavior, any robot ── */}
          <div className="mb-14 rounded-2xl p-6 md:p-8 backdrop-blur-md" style={{ background: "rgba(124,58,237,0.05)", border: "1px solid rgba(124,58,237,0.16)" }}>
            <div className="font-mono text-[11px] tracking-widest uppercase mb-3" style={{ color: "var(--violet-lite)" }}>
              One behavior · any robot
            </div>
            <p className="text-[14px] leading-[1.6] mb-4" style={{ color: "rgba(255,255,255,0.66)" }}>
              The same code drives a wheeled mobile-manipulator, a quadruped, or an arm. Change the
              connection string — nothing else.
            </p>
            <Code label="python">{`from ohho import Robot

# simulation — no hardware needed
bot = Robot.connect("omnibot", "sim://")

# the SAME code, real hardware — just one argument
bot = Robot.connect("omnibot", "serial:///dev/ttyUSB0,/dev/ttyACM0")

# a very different robot — a quadruped over DDS
bot = Robot.connect("unitree-go2", "dds://eth0")

bot.drive(vx=0.2, w=0.3)             # clamped to the robot's real limits
bot.move_joints([0, -0.5, 0.5, 0, 0, 0.2])   # no-op if the robot has no arm`}</Code>
            <p className="text-[13px] leading-[1.6] font-mono" style={{ color: "rgba(255,255,255,0.4)" }}>
              Native runtime or ROS 2 — <code>runtime=&quot;auto&quot;</code> prefers ROS 2 when it&rsquo;s installed, and
              falls back to the no-ROS runtime otherwise.
            </p>
          </div>

          {/* ── Close the loop ── */}
          <div className="mb-16">
            <div className="flex items-baseline gap-3 mb-3">
              <span className="font-mono text-[13px] font-bold" style={{ color: "var(--cyan)" }}>Next</span>
              <h2 className="font-display text-[20px] font-semibold legible">Close the loop: record → train → serve</h2>
            </div>
            <p className="text-[14px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.62)" }}>
              Record teleoperation demonstrations to a standard LeRobot dataset, fine-tune a policy, and
              serve it behind a REST API — all from the same engine. <code>ohho serve</code> launches the
              inference server; <code>ohho market</code> lists the built-in skills.
            </p>
          </div>

          {/* ── CTA ── */}
          <div className="flex items-center gap-4 flex-wrap">
            <a
              href={GITHUB_HREF}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 text-sm font-semibold px-[26px] py-[13px] rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              View on GitHub →
            </a>
            <a
              href={DOCS_HREF}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "var(--text)", border: "1px solid rgba(255,255,255,0.16)" }}
            >
              Read the docs
            </a>
            <a
              href={WHY_HREF}
              className="inline-flex items-center gap-2 text-sm font-medium px-[26px] py-[12px] rounded-lg transition-all duration-200"
              style={{ color: "rgba(255,255,255,0.6)", border: "1px solid rgba(255,255,255,0.1)" }}
            >
              Why OhhO?
            </a>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
