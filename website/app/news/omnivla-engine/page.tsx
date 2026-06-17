import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import Link from "next/link";
import { GITHUB_HREF } from "@/lib/site";

export const metadata = {
  title: "OhhO Open-Sources OmniVLA Engine | OhhO News",
  description: "A Standardized VLA Engineering Foundation Built for Embodied AI.",
};

export default function OmniVLANewsArticle() {
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
              className="inline-flex items-center gap-2 text-[13px] font-mono tracking-widest uppercase text-cyan hover:text-white transition-colors"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="m15 18-6-6 6-6"/>
              </svg>
              Back to Home
            </Link>
            <div className="flex items-center gap-3">
              <span className="px-3 py-1 bg-cyan/10 border border-cyan/20 text-cyan text-xs font-mono uppercase tracking-wider rounded-full">
                Product Release
              </span>
              <span className="text-sm text-white/50 font-mono">
                June 7, 2026
              </span>
            </div>
          </div>

          <h1 className="text-3xl md:text-5xl lg:text-[56px] font-bold tracking-tight mb-8 leading-[1.15]">
            OhhO Open-Sources OmniVLA Engine: A Standardized VLA Engineering Foundation Built for Embodied AI
          </h1>

          {/* Hero Image Placeholder */}
          <div className="relative w-full aspect-[16/9] rounded-2xl border border-white/10 bg-white/[0.02] overflow-hidden flex items-center justify-center group mb-12">
            <div className="absolute inset-0 bg-gradient-to-tr from-cyan/10 to-violet/10 opacity-30 transition-opacity duration-500 group-hover:opacity-70" />
            <div className="text-white/40 font-mono text-sm flex flex-col items-center gap-3 relative z-10 transition-transform duration-300 group-hover:scale-105">
              <svg className="w-8 h-8 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              [ OmniVLA Engine Hero Image ]
            </div>
          </div>
          
          <div className="max-w-none text-white/70 text-lg">
            <p className="lead text-xl text-white/90 mb-8 font-medium">
              We are excited to announce the open-sourcing of the OmniVLA Engine, a standardized Vision-Language-Action (VLA) engineering foundation specifically built for embodied AI and robotics.
            </p>

            <p className="mb-6">
              At OhhO, our mission has always been to make robotics operation seamless—from VR teleoperation to AI inference. Today, we take a massive step forward in accelerating the entire embodied AI ecosystem by providing developers with the tools they need to build the next generation of intelligent robots.
            </p>

            <h2 className="text-2xl font-bold mt-12 mb-6 text-white border-b border-white/10 pb-4">What is OmniVLA?</h2>
            <p className="mb-6">
              OmniVLA is a robust, highly scalable engine that bridges the gap between high-level multimodal reasoning and low-level robotic control. By integrating vision, language, and action into a single unified architecture, it allows robots to understand natural language instructions, visually perceive their environment, and execute complex physical tasks with unprecedented reliability.
            </p>
            
            <p className="mb-6">
              Previously, building a capable VLA model required stitching together fragmented tools and dealing with massive integration overhead. OmniVLA solves this by offering an end-to-end pipeline tailored for the physical world.
            </p>

            <h2 className="text-2xl font-bold mt-12 mb-6 text-white border-b border-white/10 pb-4">Why Open Source?</h2>
            <p className="mb-6">
              We believe that the future of the physical world will be built on shared, open foundations. Building great tools for others is a high form of empathy. By open-sourcing OmniVLA, we are empowering researchers, developers, and the best companies in the world to push the boundaries of what robots can do, without reinventing the wheel.
            </p>

            <div className="bg-white/[0.03] border border-white/[0.07] rounded-2xl p-8 my-10">
              <h3 className="text-xl font-bold mb-4 text-cyan">Key Features</h3>
              <ul className="space-y-4 list-none pl-0">
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-violet shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">Seamless ROS 2 Integration</strong>
                    Native compatibility with the ROS 2 ecosystem for effortless deployment across diverse robot hardware.
                  </div>
                </li>
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-violet shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">Real-Time Inference</strong>
                    Optimized for edge computing to ensure minimal latency during complex physical interactions.
                  </div>
                </li>
                <li className="flex items-start gap-3">
                  <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-violet shrink-0" />
                  <div>
                    <strong className="text-white block mb-1">Extensible Architecture</strong>
                    Designed to easily swap in the latest foundation models as the field of AI rapidly evolves.
                  </div>
                </li>
              </ul>
            </div>

            <p className="mb-6">
              Join us in building the tools the future will be built with. The OmniVLA Engine repository is now live and accepting contributions.
            </p>

            <div className="mt-12 pt-8 border-t border-white/10 flex items-center justify-center">
              <a 
                href={GITHUB_HREF}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-2 px-8 py-4 rounded-xl bg-cyan text-[#0A0E1A] font-semibold transition-all duration-200 hover:scale-105 hover:shadow-[0_0_30px_rgba(0,212,255,0.3)]"
              >
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4"/>
                  <path d="M9 18c-4.51 2-5-2-7-2"/>
                </svg>
                View on GitHub
              </a>
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
