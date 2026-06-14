import Nav from "@/components/Nav";
import Footer from "@/components/Footer";

export const metadata = {
  title: "About Us | OhhO — Robotics, Operated.",
  description: "We're building the Multimodal Data Stack for embodied AI.",
};

export default function About() {
  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden flex flex-col items-center">
        {/* Background elements */}
        <div className="hero-grid" />
        <div className="hero-orb-1" />
        <div className="hero-orb-2" />
        
        <div className="max-w-3xl w-full px-6 relative z-10">
          <div className="flex items-center gap-3 mb-8 justify-center">
            <span className="badge-dot" />
            <span className="text-[13px] font-mono tracking-widest uppercase text-cyan">About Us</span>
          </div>

          <h1 className="text-4xl md:text-5xl lg:text-[64px] font-bold tracking-tight mb-8 text-center leading-[1.1]">
            Make the tools the future will be built with
          </h1>
          
          <p className="text-lg md:text-[21px] text-white/70 mb-20 text-center max-w-2xl mx-auto leading-relaxed">
            We're building the Multimodal Data Stack for embodied AI. Some of the best companies in the world are building the future of AI in the physical world with <span className="text-white font-medium">OhhO</span>. Joining us brings you into the front row of that revolution.
          </p>

          <div className="space-y-8">
            <section className="bg-white/[0.03] border border-white/[0.07] rounded-3xl p-8 md:p-12 backdrop-blur-md">
              <h2 className="text-2xl font-bold mb-6 text-white flex items-center gap-3">
                <span className="text-cyan text-xl">01 //</span> Who are we?
              </h2>
              <div className="space-y-6 text-[17px] text-white/70 leading-relaxed">
                <p>
                  We believe building great tools for others is a high form of empathy. We care for and understand what our users want to do, and how they want to do it. We are helpful, curious, honest, and never settle for something less than great.
                </p>
                <p>
                  We are very ambitious and are excited about how hard this task is, and the impact we can have. We want to create a workplace where you can do your life's best work with amazing colleagues, and also have a family and other things that are important in your life.
                </p>
              </div>
            </section>

            <section className="bg-white/[0.03] border border-white/[0.07] rounded-3xl p-8 md:p-12 backdrop-blur-md">
              <h2 className="text-2xl font-bold mb-6 text-white flex items-center gap-3">
                <span className="text-violet text-xl">02 //</span> Where are we?
              </h2>
              <div className="space-y-6 text-[17px] text-white/70 leading-relaxed mb-10">
                <p>
                  We help our users have an enormous impact on the world around us and believe our team should include a wide set of perspectives and experiences because of that.
                </p>
                <p>
                  We are a distributed company with a remote-first culture. We are based <em>nowhere</em>—meaning we hire the best talent regardless of geography. We get the entire team together for a full week about once a quarter.
                </p>
              </div>
              
              {/* Photo placeholder */}
              <div className="relative w-full aspect-[21/9] rounded-2xl border border-white/10 bg-white/[0.02] overflow-hidden flex items-center justify-center group">
                <div className="absolute inset-0 bg-gradient-to-tr from-cyan/10 to-violet/10 opacity-30 transition-opacity duration-500 group-hover:opacity-70" />
                <div className="text-white/40 font-mono text-sm flex flex-col items-center gap-3 relative z-10 transition-transform duration-300 group-hover:scale-105">
                  <svg className="w-8 h-8 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  [ Team Photo Placeholder ]
                </div>
              </div>
            </section>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
