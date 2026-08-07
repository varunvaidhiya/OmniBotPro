import Link from "next/link";
import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import GlassCard from "@/components/GlassCard";
import ProductDashboard from "@/components/products/ProductDashboard";
import { DOCS_HREF, GITHUB_HREF } from "@/lib/site";
import { pageSeo } from "@/lib/seo";

export const metadata = pageSeo({
  path: "/os",
  title: "OhhO OS v1.0.0 — the open robot engine",
  description:
    "OhhO OS v1.0.0 is the open-source, robot-agnostic engine that powers every OhhO product. Control any robot with or without ROS, agent brain, data collection, training, serving, skills — all from one pip install.",
});

// ── v1.0.0 Feature pillars ────────────────────────────────────────────────────
const FEATURE_PILLARS: { icon: string; title: string; body: string }[] = [
  {
    icon: "Control",
    title: "6 adapters · 2 runtimes",
    body: "sim, serial base, bus-servo arm, DDS-native, ROS 2 topics, and composite (base+arm merge). Native runtime or ROS 2 — auto-detects which is available.",
  },
  {
    icon: "Agent",
    title: "Real agent brain",
    body: "HarnessBrain wires agent_engine — perceive→reason→act→reflect with LLM tool-calling. Tools built automatically from robot capabilities. ScriptedBrain fallback needs zero deps.",
  },
  {
    icon: "Train",
    title: "Record → Train → Serve",
    body: "Recorder captures 9-D state+action to LeRobot v2.0. finetune() delegates to lerobot_engine (smolvla/act/diffusion/openvla). FastAPI server with /predict endpoint.",
  },
  {
    icon: "Skills",
    title: "Skill market",
    body: "@skill decorator, ohho market list/run, 4 built-in skills (patrol, wave, stop, status). Capability-gated — a skill that needs manipulation won't run on a base-only robot.",
  },
  {
    icon: "Profiles",
    title: "Hardware profiles",
    body: "5 built-in profiles (pi_workstation, jetson_single, workstation_single, mac_dev, edge_cpu). ohho profile detect auto-detects your machine. device=\"auto\" everywhere.",
  },
  {
    icon: "CLI",
    title: "10 CLI commands",
    body: "ohho doctor list version connect sim drive agent serve market profile — everything from diagnostics to serving a policy, from one command.",
  },
];

// ── Differentiators (deliberately competitor-agnostic) ───────────────────────
const DIFFERENTIATORS: { title: string; body: string }[] = [
  {
    title: "ROS optional — never required",
    body: "Most agentic robot stacks force a choice: go all-in on ROS, or abandon it entirely. OhhO OS runs both ways — a lightweight pure-Python runtime or the full ROS 2 stack — and you switch with a single argument. get_runtime(\"auto\") detects ROS 2 automatically.",
  },
  {
    title: "Truly robot-agnostic",
    body: "Write a behavior once and run it on a wheeled base, a quadruped, a humanoid or an arm. Capability-typed commands mean swapping hardware never means rewriting your application.",
  },
  {
    title: "The whole lifecycle, one engine",
    body: "Other frameworks stop at control. OhhO OS also carries the agent brain, data collection, training, serving, and skills — the same engine from first prototype to deployed fleet.",
  },
  {
    title: "Agent-native by design",
    body: "A continuous perceive → reason → act → reflect loop powered by agent_engine with an LLM tool-calling reasoner. Tools are built automatically from the robot's capabilities. ScriptedBrain fallback needs zero deps.",
  },
  {
    title: "Training built in",
    body: "Record demonstrations → fine-tune VLA policies (SmolVLA, ACT, diffusion, OpenVLA) → serve over REST → close the loop. All with device=\"auto\" hardware detection. Mock mode for sim loops — no GPU needed.",
  },
  {
    title: "Open source, zero lock-in",
    body: "Apache-2.0 licensed. 151 tests passing. Self-host every line, bring your own models and data, and move to OhhO Cloud only when you want managed GPUs, training and fleet operations.",
  },
];

// ── Runtime comparison ───────────────────────────────────────────────────────
const RUNTIME_ROWS: { feature: string; native: string; ros: string }[] = [
  { feature: "Install", native: "pip install — any OS", ros: "Ubuntu 24.04 + ROS 2 Jazzy" },
  { feature: "Drive · teleop · telemetry", native: "yes", ros: "yes" },
  { feature: "Agent (Mind) · Train · Data · Serve", native: "yes", ros: "yes" },
  { feature: "Direct DDS / MAVLink / firmware", native: "native", ros: "via bridge" },
  { feature: "Lightweight nav (built-in A*)", native: "yes", ros: "yes" },
  { feature: "Nav2 full navigation", native: "no", ros: "yes" },
  { feature: "SLAM (slam_toolbox / 3-D)", native: "basic", ros: "yes" },
  { feature: "MoveIt 2 manipulation", native: "no", ros: "yes" },
  { feature: "Multi-machine distribution", native: "partial", ros: "yes (DDS)" },
  { feature: "Tooling", native: "Foxglove", ros: "RViz + Foxglove + ecosystem" },
  { feature: "Footprint / cold start", native: "light / fast", ros: "heavy" },
  { feature: "OS support", native: "Windows · macOS · Linux", ros: "Linux" },
];

const ROBOT_CATEGORIES = [
  "Wheeled", "Legged", "Humanoid", "Industrial arm", "Mobile manipulator",
  "Drone", "Tracked", "Marine", "Delivery", "Inspection", "Agricultural", "+ more",
];

const ADAPTERS = [
  "Simulator", "Serial base", "Bus-servo arm", "DDS-native", "ROS 2 topics", "Composite",
  "MAVLink", "CANopen", "OPC UA", "PROFINET", "EtherNet/IP", "MQTT",
  "VDA 5050", "Modbus", "EtherCAT", "ROSBridge", "Web Serial", "+ more",
];

function cellColor(v: string): string {
  if (v === "yes" || v === "native") return "var(--cyan)";
  if (v === "no") return "rgba(255,255,255,0.3)";
  return "rgba(255,255,255,0.7)";
}

export default function OhhoOsPage() {
  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />

        <div className="max-w-4xl w-full mx-auto px-6 relative z-10">
          {/* ── Hero ── */}
          <div className="flex items-center gap-3 mb-6">
            <span className="badge-dot" />
            <span className="text-[13px] font-mono tracking-widest uppercase" style={{ color: "var(--cyan)" }}>
              OhhO OS · Open Source
            </span>
          </div>
          <h1 className="font-display font-bold text-[clamp(34px,6vw,68px)] tracking-tight leading-[1.05] mb-6 legible">
            The open engine<br />for <span className="text-cyan">any robot</span>.
          </h1>
          <p className="text-[clamp(16px,2.2vw,20px)] leading-[1.6] max-w-2xl mb-8 legible" style={{ color: "rgba(255,255,255,0.66)" }}>
            OhhO OS is the open-source, robot-agnostic engine the whole platform runs on. One runtime
            controls anything on wheels, legs or wings — with or without ROS — and carries the entire
            stack from perception to training. It is the engine under every OhhO product, and it is
            yours to install, self-host and extend.
          </p>

          {/* install snippet */}
          <div
            className="rounded-xl p-4 font-mono text-[13px] leading-[1.9] mb-7 max-w-xl"
            style={{ background: "rgba(0,0,0,0.4)", border: "1px solid rgba(255,255,255,0.10)", color: "rgba(255,255,255,0.82)" }}
          >
            <div><span style={{ color: "var(--cyan)" }}>$</span> pip install <span style={{ color: "var(--cyan)" }}>&apos;ohho-os[base]&apos;</span></div>
            <div style={{ color: "rgba(255,255,255,0.45)" }}>&gt;&gt;&gt; from ohho import Robot</div>
            <div style={{ color: "rgba(255,255,255,0.45)" }}>
              &gt;&gt;&gt; bot = Robot.connect(<span style={{ color: "#9ae6b4" }}>&quot;omnibot&quot;</span>)  <span style={{ color: "rgba(255,255,255,0.3)" }}># ROS or no-ROS</span>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3 mb-20">
            <Link
              href={`${DOCS_HREF}/ohho-os/setup`}
              className="inline-flex items-center gap-2 text-sm font-semibold px-6 py-3 rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Setup Guide →
            </Link>
            <Link
              href={`${DOCS_HREF}/ohho-os`}
              className="inline-flex items-center gap-2 text-sm font-medium px-6 py-3 rounded-lg transition-all duration-200"
              style={{ background: "rgba(255,255,255,0.04)", color: "var(--text)", border: "1px solid rgba(255,255,255,0.16)" }}
            >
              Read the docs
            </Link>
            <a
              href={GITHUB_HREF}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 text-sm font-medium px-6 py-3 rounded-lg transition-all duration-200"
              style={{ color: "rgba(255,255,255,0.6)" }}
            >
              View on GitHub
            </a>
          </div>

          <figure className="mb-24">
            <GlassCard accent="violet" interactive={false} padding="16px" radius={22}>
              <div className="rounded-[14px] overflow-hidden">
                <ProductDashboard slug="ohho-os" accent="violet" />
              </div>
            </GlassCard>
          </figure>

          {/* ── v1.0.0 Features ── */}
          <SectionHeading kicker="v1.0.0" title="What's in the box" />
          <p className="text-[15px] leading-[1.7] max-w-2xl mb-10" style={{ color: "rgba(255,255,255,0.6)" }}>
            Six adapters, two runtimes, a real agent brain, the record→train→serve loop,
            a skill market, hardware profiles, and ten CLI commands — 151 tests, Apache-2.0.
          </p>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-24">
            {FEATURE_PILLARS.map((f) => (
              <div
                key={f.title}
                className="rounded-2xl p-5 backdrop-blur-md"
                style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
              >
                <div className="font-mono text-[10px] tracking-widest uppercase mb-2" style={{ color: "var(--cyan)" }}>{f.icon}</div>
                <div className="font-display text-[16px] font-semibold mb-2 legible">{f.title}</div>
                <p className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.58)" }}>{f.body}</p>
              </div>
            ))}
          </div>

          {/* ── Why it is different ── */}
          <SectionHeading kicker="Why OhhO OS" title="Built differently from the rest" />
          <p className="text-[15px] leading-[1.7] max-w-2xl mb-10" style={{ color: "rgba(255,255,255,0.6)" }}>
            The market is full of robot frameworks and &quot;agentic OS&quot; projects. Here is what sets OhhO OS
            apart — no asterisks, no lock-in.
          </p>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-24">
            {DIFFERENTIATORS.map((d) => (
              <div
                key={d.title}
                className="rounded-2xl p-6 backdrop-blur-md"
                style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
              >
                <div className="font-display text-[17px] font-semibold mb-2 legible">{d.title}</div>
                <p className="text-[14px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.62)" }}>{d.body}</p>
              </div>
            ))}
          </div>

          {/* ── Choose your runtime ── */}
          <SectionHeading kicker="Choose your runtime" title="Run with ROS, or without it" />
          <p className="text-[15px] leading-[1.7] max-w-2xl mb-10" style={{ color: "rgba(255,255,255,0.6)" }}>
            OhhO OS ships two runtimes behind one identical API. Pick what fits — and switch later by
            changing a single argument. Your agent, training and application code never change.
          </p>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
            <RuntimeCard
              accent="cyan"
              name="No-ROS (native)"
              best="Getting started · single robot · non-ROS hardware · laptop / edge compute · Windows or macOS"
              advantages={[
                "Zero setup friction — pip install and go, no ROS distro",
                "Lightweight, fast cold start, fewer moving parts",
                "Pure Python — no workspaces, colcon or launch files",
                "Talks DDS, MAVLink and firmware directly",
                "Runs anywhere Python runs",
              ]}
              install="pip install 'ohho-os[base]'"
            />
            <RuntimeCard
              accent="violet"
              name="ROS 2"
              best="Production autonomy · Nav2 / SLAM / MoveIt · multi-robot or multi-machine · existing ROS fleets · Gazebo / Isaac sim"
              advantages={[
                "The entire mature ROS ecosystem — Nav2, SLAM, MoveIt 2",
                "ros2_control, RViz, and thousands of community packages",
                "Battle-tested multi-node / multi-machine over DDS",
                "Industry-standard integration with existing fleets",
                "The most capable navigation and manipulation stacks",
              ]}
              install="pip install 'ohho-os[ros2]'"
            />
          </div>

          <p className="text-[14px] leading-[1.7] max-w-2xl mb-8 rounded-xl p-4" style={{ color: "rgba(255,255,255,0.7)", background: "rgba(0,212,255,0.05)", border: "1px solid rgba(0,212,255,0.18)" }}>
            <strong className="text-white">No wrong choice.</strong> Start native today and graduate to ROS 2
            later — <span className="font-mono text-[13px]" style={{ color: "var(--cyan)" }}>Robot.connect(&quot;omnibot&quot;, runtime=&quot;ros2&quot;)</span> — without touching the rest of your code.
          </p>

          {/* comparison table */}
          <div className="overflow-x-auto rounded-2xl mb-24" style={{ border: "1px solid rgba(255,255,255,0.08)" }}>
            <table className="w-full text-left border-collapse">
              <thead>
                <tr style={{ background: "rgba(255,255,255,0.03)" }}>
                  <th className="text-[12px] font-mono uppercase tracking-wider p-3.5" style={{ color: "rgba(255,255,255,0.45)" }}>Capability</th>
                  <th className="text-[12px] font-mono uppercase tracking-wider p-3.5" style={{ color: "var(--cyan)" }}>No-ROS</th>
                  <th className="text-[12px] font-mono uppercase tracking-wider p-3.5" style={{ color: "var(--violet)" }}>ROS 2</th>
                </tr>
              </thead>
              <tbody>
                {RUNTIME_ROWS.map((r, i) => (
                  <tr key={r.feature} style={{ borderTop: "1px solid rgba(255,255,255,0.06)", background: i % 2 ? "rgba(255,255,255,0.01)" : "transparent" }}>
                    <td className="text-[13.5px] p-3.5" style={{ color: "rgba(255,255,255,0.78)" }}>{r.feature}</td>
                    <td className="text-[13.5px] p-3.5 font-medium" style={{ color: cellColor(r.native) }}>{r.native}</td>
                    <td className="text-[13.5px] p-3.5 font-medium" style={{ color: cellColor(r.ros) }}>{r.ros}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* ── One engine, every robot ── */}
          <SectionHeading kicker="Robot-agnostic" title="One engine, every robot" />
          <p className="text-[15px] leading-[1.7] max-w-2xl mb-8" style={{ color: "rgba(255,255,255,0.6)" }}>
            A capability model describes what a robot can do — not how it does it — so the same code runs
            across wildly different hardware, degrading gracefully when a capability is absent. Adapters
            translate each robot&apos;s native protocol underneath.
          </p>
          <div className="mb-6">
            <div className="font-mono text-[10px] tracking-[0.14em] uppercase mb-3" style={{ color: "rgba(255,255,255,0.4)" }}>Robot categories</div>
            <div className="flex flex-wrap gap-2 mb-8">
              {ROBOT_CATEGORIES.map((c) => (
                <span key={c} className="font-mono text-[12px] px-3 py-1.5 rounded-full" style={{ color: "var(--cyan)", background: "rgba(0,212,255,.08)", border: "1px solid rgba(0,212,255,.22)" }}>{c}</span>
              ))}
            </div>
            <div className="font-mono text-[10px] tracking-[0.14em] uppercase mb-3" style={{ color: "rgba(255,255,255,0.4)" }}>Protocol adapters</div>
            <div className="flex flex-wrap gap-2 mb-24">
              {ADAPTERS.map((a) => (
                <span key={a} className="font-mono text-[12px] px-3 py-1.5 rounded-full" style={{ color: "var(--violet)", background: "rgba(124,58,237,.10)", border: "1px solid rgba(124,58,237,.24)" }}>{a}</span>
              ))}
            </div>
          </div>

          {/* ── Powered by OhhO OS ── */}
          <SectionHeading kicker="The platform" title="Nineteen products. One engine." />
          <p className="text-[15px] leading-[1.7] max-w-2xl mb-6" style={{ color: "rgba(255,255,255,0.6)" }}>
            Every OhhO product — from Build and Train to Autonomy, Mind and Fleet — is a console that
            runs on OhhO OS. Use the open engine on its own, or sign in to OhhO Cloud for the managed,
            hosted experience on top.
          </p>
          <Link href="/#products" className="inline-flex items-center gap-2 text-[14px] font-mono tracking-wide text-cyan hover:text-white transition-colors mb-24">
            Explore all 19 products →
          </Link>

          {/* ── Get started ── */}
          <SectionHeading kicker="Get started" title="From install to a moving robot" />
          <ol className="space-y-4 mb-10">
            {[
              ["Install", "pip install 'ohho-os[base]' — add [serial], [arm], [unitree], [agent], [train], [serve] or [ros2] as you need them."],
              ["Connect", "Robot.connect(\"omnibot\") auto-detects the runtime, or pass transport=\"sim://\" / \"serial://\" / \"dds://\" / \"ros2://\"."],
              ["Control", "bot.drive(vx=0.2), bot.move_joints([...]), bot.telemetry() — same API on any robot. Capability-gated, safe no-ops."],
              ["Automate", "Agent(bot).run(\"explore the room\") — perceive→reason→act→reflect loop with LLM tool-calling."],
              ["Train & Serve", "Recorder → finetune() → ohho serve — record→train→serve loop with mock mode for sim (no GPU needed)."],
              ["Skills", "ohho market list / ohho market run omnibot patrol — 4 built-in skills, or write your own with @skill."],
            ].map(([t, b], i) => (
              <li key={t} className="flex gap-4">
                <span className="flex-shrink-0 w-7 h-7 rounded-full flex items-center justify-center font-mono text-[12px] font-semibold" style={{ background: "var(--cyan)", color: "var(--bg)" }}>{i + 1}</span>
                <div>
                  <div className="font-display text-[15px] font-semibold mb-0.5">{t}</div>
                  <p className="text-[14px] leading-[1.6] font-mono" style={{ color: "rgba(255,255,255,0.55)" }}>{b}</p>
                </div>
              </li>
            ))}
          </ol>
          <div className="flex flex-wrap gap-3">
            <Link
              href={`${DOCS_HREF}/ohho-os/setup`}
              className="inline-flex items-center gap-2 text-sm font-semibold px-6 py-3 rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Read the setup guide →
            </Link>
            <Link
              href={`${DOCS_HREF}/ohho-os`}
              className="inline-flex items-center gap-2 text-sm font-medium px-6 py-3 rounded-lg transition-all duration-200"
              style={{ background: "rgba(255,255,255,0.04)", color: "var(--text)", border: "1px solid rgba(255,255,255,0.16)" }}
            >
              Full documentation
            </Link>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}

function SectionHeading({ kicker, title }: { kicker: string; title: string }) {
  return (
    <div className="mb-2">
      <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-3" style={{ color: "var(--cyan)" }}>{kicker}</div>
      <h2 className="font-display font-bold text-[clamp(24px,3.5vw,38px)] tracking-tight leading-[1.12] mb-3 legible">{title}</h2>
    </div>
  );
}

function RuntimeCard({
  accent, name, best, advantages, install,
}: { accent: "cyan" | "violet"; name: string; best: string; advantages: string[]; install: string }) {
  const color = accent === "cyan" ? "var(--cyan)" : "var(--violet)";
  const border = accent === "cyan" ? "rgba(0,212,255,.22)" : "rgba(124,58,237,.26)";
  const bg = accent === "cyan" ? "rgba(0,212,255,.04)" : "rgba(124,58,237,.05)";
  return (
    <div className="rounded-2xl p-6 backdrop-blur-md" style={{ background: bg, border: `1px solid ${border}` }}>
      <div className="font-display text-[19px] font-bold mb-3" style={{ color }}>{name}</div>
      <div className="text-[12px] font-mono leading-[1.6] mb-5 pb-5" style={{ color: "rgba(255,255,255,0.5)", borderBottom: "1px solid rgba(255,255,255,0.08)" }}>
        <span style={{ color: "rgba(255,255,255,0.7)" }}>Best for:</span> {best}
      </div>
      <ul className="space-y-2.5 mb-5">
        {advantages.map((a) => (
          <li key={a} className="flex gap-2.5 text-[13.5px] leading-[1.5]" style={{ color: "rgba(255,255,255,0.72)" }}>
            <span style={{ color }} className="flex-shrink-0">▸</span>
            <span>{a}</span>
          </li>
        ))}
      </ul>
      <div className="rounded-lg p-2.5 font-mono text-[12.5px]" style={{ background: "rgba(0,0,0,0.35)", border: "1px solid rgba(255,255,255,0.08)", color: "rgba(255,255,255,0.8)" }}>
        <span style={{ color }}>$</span> {install}
      </div>
    </div>
  );
}
