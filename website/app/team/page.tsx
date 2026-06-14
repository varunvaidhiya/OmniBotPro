import Nav from "@/components/Nav";
import Footer from "@/components/Footer";

export const metadata = {
  title: "Team | OhhO — Robotics, Operated.",
  description: "Meet the people building the future of embodied AI.",
};

const members = [
  { name: "Your Name", role: "Role", photo: null, bio: "Add your bio here." },
  { name: "Team Member", role: "Role", photo: null, bio: "Add your bio here." },
  { name: "Team Member", role: "Role", photo: null, bio: "Add your bio here." },
  { name: "Team Member", role: "Role", photo: null, bio: "Add your bio here." },
];

export default function Team() {
  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden flex flex-col items-center">
        <div className="hero-grid" />
        <div className="hero-orb-1" />
        <div className="hero-orb-2" />

        <div className="max-w-5xl w-full px-6 relative z-10">
          <div className="flex items-center gap-3 mb-8 justify-center">
            <span className="badge-dot" />
            <span className="text-[13px] font-mono tracking-widest uppercase text-cyan">Team</span>
          </div>

          <h1 className="text-4xl md:text-5xl lg:text-[64px] font-bold tracking-tight mb-6 text-center leading-[1.1]">
            Meet the team
          </h1>

          <p className="text-lg md:text-[21px] text-white/70 mb-20 text-center max-w-2xl mx-auto leading-relaxed">
            We're a small team of builders obsessed with making robots easier to operate.
          </p>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {members.map((m, i) => (
              <div
                key={m.name + i}
                className="bg-white/[0.03] border border-white/[0.07] rounded-2xl p-6 backdrop-blur-md flex flex-col items-center text-center group transition-all duration-300 hover:border-white/[0.14] hover:bg-white/[0.05]"
              >
                <div className="w-28 h-28 rounded-full bg-white/[0.04] border border-white/[0.08] mb-4 flex items-center justify-center overflow-hidden group-hover:border-white/[0.16] transition-colors">
                  {m.photo ? (
                    <img src={m.photo} alt={m.name} className="w-full h-full object-cover" />
                  ) : (
                    <svg className="w-10 h-10 text-white/25" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                    </svg>
                  )}
                </div>
                <h3 className="text-[17px] font-semibold text-white mb-1">{m.name}</h3>
                <p className="text-[13px] font-medium text-cyan mb-3">{m.role}</p>
                <p className="text-[13px] text-white/55 leading-relaxed">{m.bio}</p>
              </div>
            ))}
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
