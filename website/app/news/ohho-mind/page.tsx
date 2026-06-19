import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import Link from "next/link";

export const metadata = {
  title: "OhhO Mind: Give Your Robot a Mind of Its Own | OhhO News",
  description:
    "Introducing OhhO Mind — the continuous agent brain that turns a command-taking robot into a goal-driven one, delivered to your fleet over the air.",
};

export default function OhhOMindNewsArticle() {
  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden flex flex-col items-center">
        {/* Background elements */}
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-50" />
        <div className="hero-orb-2 opacity-50" />

        <div className="max-w-3xl w-full px-6 relative z-10">
          <div className="mb-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
            <Link
              href="/"
              className="inline-flex items-center gap-2 text-[13px] font-mono tracking-widest uppercase text-violet-lite hover:text-white transition-colors"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="m15 18-6-6 6-6" />
              </svg>
              Back to Home
            </Link>
            <div className="flex items-center gap-3">
              <span className="px-3 py-1 bg-violet/10 border border-violet/20 text-violet-lite text-xs font-mono uppercase tracking-wider rounded-full">
                Product Release
              </span>
              <span className="text-sm text-white/50 font-mono">June 19, 2026</span>
            </div>
          </div>

          <h1 className="text-3xl md:text-5xl lg:text-[56px] font-bold tracking-tight mb-8 leading-[1.15]">
            OhhO Mind: Give Your Robot a Mind of Its Own
          </h1>

          {/* Hero Image Placeholder */}
          <div className="relative w-full aspect-[16/9] rounded-2xl border border-white/10 bg-white/[0.02] overflow-hidden flex items-center justify-center group mb-12">
            <div className="absolute inset-0 bg-gradient-to-tr from-violet/10 to-cyan/10 opacity-30 transition-opacity duration-500 group-hover:opacity-70" />
            <div className="text-white/40 font-mono text-sm flex flex-col items-center gap-3 relative z-10 transition-transform duration-300 group-hover:scale-105">
              <svg className="w-8 h-8 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <circle cx="12" cy="12" r="3" strokeWidth={1.5} />
                <circle cx="5" cy="6" r="2" strokeWidth={1.5} />
                <circle cx="19" cy="6" r="2" strokeWidth={1.5} />
                <circle cx="5" cy="18" r="2" strokeWidth={1.5} />
                <circle cx="19" cy="18" r="2" strokeWidth={1.5} />
                <path strokeLinecap="round" strokeWidth={1.5} d="m9.8 10.2-3.1-2.7m7.5 0 3.1-2.7m-7.5 9.4-3.1 2.7m7.5 0 3.1 2.7" />
              </svg>
              [ OhhO Mind Hero Image ]
            </div>
          </div>

          <div className="max-w-none text-white/70 text-lg">
            <p className="lead text-xl text-white/90 mb-8 font-medium">
              Today we&apos;re introducing OhhO Mind — the deliberative brain that turns a capable robot into one you give goals to, not scripts. It&apos;s the newest product in the OhhO Intelligence tier, and it ships to your robots the way software should: over the air.
            </p>

            <p className="mb-6">
              The robotics world just got incredible reflexes. Vision-Language-Action models can look at a cluttered counter and produce a sensible motion to pick up a cup. Navigation stacks plan across mapped buildings. Reinforcement-learning policies trained in simulation transfer to real motors. Each of these works — and yet you still can&apos;t simply hand a robot a goal and walk away. You have a very capable set of reflexes, waiting for very specific commands.
            </p>

            <h2 className="text-2xl font-bold mt-12 mb-6 text-white border-b border-white/10 pb-4">A reflex is not a mind</h2>
            <p className="mb-6">
              A VLA model is, fundamentally, a function: image and instruction in, action out. That&apos;s enormously useful — and profoundly limited as a basis for autonomy. It doesn&apos;t choose goals, remember what it saw, notice that it failed, or get better over time. Real physical intelligence is the loop around the reflex: deciding what to do, grounding that decision in the world and the past, acting safely, watching the outcome, and learning from it — continuously, for hours.
            </p>
            <p className="mb-6">
              OhhO Mind is that loop. It runs a continuous cycle on top of your robot&apos;s existing stack — perceive, reason, verify, act, monitor, reflect, remember — sitting above OhhO Autonomy, calling OhhO Serve models as skills, and feeding judged experience back into OhhO Train.
            </p>

            <h2 className="text-2xl font-bold mt-12 mb-6 text-white border-b border-white/10 pb-4">Two-speed cognition</h2>
            <p className="mb-6">
              The most important design decision in OhhO Mind is what it doesn&apos;t do: it never puts a large model in the motor-control loop. The fast reflexive layer — navigation, RL and VLA policies running at 10–20 Hz — stays exactly as it is. OhhO Mind adds the slow deliberative layer on top, at roughly 1 Hz, where the thinking happens. A fast body and a slow mind, wired together carefully. You give it an objective in plain language; it loops until the job is done, then idles.
            </p>

            <div className="bg-white/[0.03] border border-white/[0.07] rounded-2xl p-8 my-10">
              <h3 className="text-xl font-bold mb-4 text-violet-lite">What OhhO Mind does</h3>
              <ul className="space-y-4 list-none pl-0">
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-cyan shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">The agent loop</strong>
                    A continuous perceive → reason → verify → act → monitor → reflect → remember cycle. Give it a goal; it recovers from failure and asks for help only when genuinely stuck.
                  </div>
                </li>
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-cyan shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">Grounded world state</strong>
                    It fuses pose, arm state, detected objects, mission status and memory into one snapshot — so it reasons about the world the robot is actually in.
                  </div>
                </li>
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-cyan shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">A safety gate it can&apos;t skip</strong>
                    Every low-level action is checked against the robot&apos;s real hardware limits before it reaches a motor. Unsafe plans are rejected, not clipped.
                  </div>
                </li>
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-cyan shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">Hybrid reasoning</strong>
                    Cloud-class reasoning when online, an on-device model and an onboard NPU when offline — so the robot keeps thinking with no internet.
                  </div>
                </li>
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-cyan shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">Memory that compounds</strong>
                    It remembers objects, places and the outcomes of past attempts, and feeds judged episodes to OhhO Train — so your fleet gets better with use.
                  </div>
                </li>
              </ul>
            </div>

            <h2 className="text-2xl font-bold mt-12 mb-6 text-white border-b border-white/10 pb-4">Delivered over the air</h2>
            <p className="mb-6">
              Here&apos;s the part we&apos;re most excited about. OhhO Mind reaches your robots as an OhhO Fleet over-the-air update — no new hardware, no teardown. The robots you already have in the field receive an update and wake up able to take goals, recover from failure, operate unattended, and learn. That&apos;s how we think physical AI actually gets deployed: not as a monolithic model you swap your whole robot for, but as a mind you can ship to a fleet.
            </p>

            <p className="mb-6">
              A robot that can act is a tool. A robot with a mind is an agent. OhhO Mind is how you give your robots the second one.
            </p>

            <div className="mt-12 pt-8 border-t border-white/10 flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link
                href="/products/mind"
                className="inline-flex items-center gap-2 px-8 py-4 rounded-xl bg-violet text-white font-semibold transition-all duration-200 hover:scale-105 hover:shadow-[0_0_30px_rgba(124,58,237,0.35)]"
              >
                Explore OhhO Mind
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M5 12h14M12 5l7 7-7 7" />
                </svg>
              </Link>
              <Link
                href="/mind"
                className="inline-flex items-center gap-2 px-8 py-4 rounded-xl border border-white/18 text-white font-semibold transition-all duration-200 hover:bg-white/[0.06]"
              >
                Open the console
              </Link>
            </div>

            <div className="mt-12 text-center">
              <Link
                href="/news/omnivla-engine"
                className="inline-flex items-center gap-2 text-[13px] font-mono tracking-wider uppercase text-white/45 hover:text-white transition-colors"
              >
                More from OhhO News — OmniVLA Engine
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M5 12h14M12 5l7 7-7 7" />
                </svg>
              </Link>
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
