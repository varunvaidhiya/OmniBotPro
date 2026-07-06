import Nav from "@/components/Nav";
import Footer from "@/components/Footer";

export const metadata = {
  title: "Industry Standards | OhhO — Robotics, Operated.",
  description:
    "Every industry-standard protocol OhhO supports — from CANopen to OPC UA to VDA 5050. The open robotics platform that speaks the standards your facility already runs on.",
};

interface StandardEntry {
  name: string;
  category: "Communication" | "Safety" | "Data" | "Simulation";
  what: string;
  useCase: string;
  product: string;
}

const STANDARDS: StandardEntry[] = [
  // ── Communication ────────────────────────────────────────────────────────────
  { name: "DDS (Cyclone)", category: "Communication", what: "Data Distribution Service — pub/sub middleware for real-time robotics", useCase: "Humanoids, legged robots, native SDK translation", product: "Bridge" },
  { name: "MAVLink", category: "Communication", what: "Micro Air Vehicle Link — drone autopilot protocol", useCase: "Aerial drones, survey, inspection", product: "Bridge" },
  { name: "CAN bus / CANopen (CiA 402)", category: "Communication", what: "Controller Area Network + CANopen device profile (CiA 402 motion)", useCase: "AGVs, AMRs, mobile robot motor controllers, embedded bases", product: "Bridge" },
  { name: "Modbus TCP/RTU", category: "Communication", what: "Serial communication protocol for PLC-driven industrial arms", useCase: "Industrial SCARA / 6-DOF arms", product: "Bridge" },
  { name: "EtherCAT", category: "Communication", what: "Ethernet for Control Automation Technology — real-time servo control", useCase: "High-performance industrial arms", product: "Bridge" },
  { name: "OPC UA", category: "Communication", what: "Open Platform Communications Unified Architecture — Industry 4.0 standard", useCase: "Factory cell integration, MES/SCADA, digital twin communication", product: "Bridge, Twin" },
  { name: "PROFINET", category: "Communication", what: "Process Field Net — real-time industrial Ethernet (Siemens ecosystem)", useCase: "European / German automotive manufacturing", product: "Bridge" },
  { name: "EtherNet/IP (CIP)", category: "Communication", what: "Common Industrial Protocol over Ethernet (Rockwell ecosystem)", useCase: "North American manufacturing", product: "Bridge" },
  { name: "MQTT", category: "Communication", what: "Message Queuing Telemetry Transport — lightweight IoT pub/sub", useCase: "Cloud IoT, fleet telemetry, VDA 5050 transport", product: "Bridge, Connect, Fleet" },
  { name: "VDA 5050", category: "Communication", what: "German Automotive AGV/AMR fleet communication standard (JSON over MQTT)", useCase: "Warehouse robotics, WMS integration, master control", product: "Bridge, Fleet" },
  { name: "ROS-Industrial", category: "Communication", what: "Consortium driver ecosystem for industrial arms on ROS", useCase: "Fanuc, ABB, KUKA, Yaskawa, Universal Robots", product: "Bridge" },
  { name: "IEC 61131-3 / PLCopen", category: "Communication", what: "PLC programming standard (ladder logic, structured text, function blocks)", useCase: "Factory PLC controller integration", product: "Bridge" },
  { name: "ROS 2 topics", category: "Communication", what: "ROS 2 pub/sub message bus — the lingua franca of the OhhO platform", useCase: "All ROS 2-compatible robots", product: "Frame, Connect, all" },
  { name: "ROSBridge WebSocket", category: "Communication", what: "JSON protocol bridging ROS 2 to WebSocket clients", useCase: "Browser-based robot control, OhhO Connect", product: "Connect" },
  { name: "Open-RMF", category: "Communication", what: "Open Robotics Middleware Framework — multi-robot fleet coordination", useCase: "Mixed-vendor fleet traffic management, task allocation", product: "Fleet" },

  // ── Safety ────────────────────────────────────────────────────────────────────
  { name: "EU Machinery Reg (CE)", category: "Safety", what: "Essential health & safety requirements for machinery in the EU", useCase: "EU market access for any robot", product: "Comply" },
  { name: "ISO 10218-1/2", category: "Safety", what: "Safety requirements for industrial robots and robot systems", useCase: "Industrial robot makers", product: "Comply" },
  { name: "ISO 15066", category: "Safety", what: "Collaborative robot safety — power and force limiting", useCase: "Cobot deployments", product: "Comply" },
  { name: "ISO 13849-1", category: "Safety", what: "Safety-related parts of control systems (Performance Level a-e)", useCase: "All safety-rated control functions", product: "Comply" },
  { name: "ISO 12100", category: "Safety", what: "Risk assessment — hazard identification and risk estimation", useCase: "All machinery (foundational)", product: "Comply" },
  { name: "ISO 3691-4", category: "Safety", what: "Safety of industrial trucks — driverless (AGVs/AMRs)", useCase: "Warehouse robotics, AGV/AMR safety", product: "Comply" },
  { name: "ISO 13482", category: "Safety", what: "Safety requirements for personal care robots", useCase: "Service & household robots", product: "Comply" },
  { name: "ANSI/RIA R15.06", category: "Safety", what: "US industrial robot safety (harmonized with ISO 10218)", useCase: "US industrial robot market", product: "Comply" },
  { name: "ANSI/ITSDF B56.5", category: "Safety", what: "Safety of guided industrial vehicles (AGVs) — US", useCase: "US warehouse / AGV safety", product: "Comply" },
  { name: "IEC 61508", category: "Safety", what: "Functional safety of E/E/PE systems (foundamental root standard)", useCase: "All safety-critical systems", product: "Comply" },
  { name: "UL / IEC 60204-1", category: "Safety", what: "Electrical equipment of machines", useCase: "US / electrical safety", product: "Comply" },

  // ── Data ──────────────────────────────────────────────────────────────────────
  { name: "LeRobot (Parquet + MP4)", category: "Data", what: "Hugging Face dataset format for imitation learning demonstrations", useCase: "VLA training data collection and sharing", product: "Data, Train" },
  { name: "MCAP", category: "Data", what: "Open-source ROS 2-native bag/recording format", useCase: "ROS 2 topic recording, ecosystem interoperability", product: "Frame, Data" },
  { name: "ONNX", category: "Data", what: "Open Neural Network Exchange — cross-platform model format", useCase: "RL policy export, Fleet OTA deployment", product: "Train, Fleet, Serve" },
  { name: "URDF", category: "Data", what: "Unified Robot Description Format — XML robot model", useCase: "Robot description for ROS 2, simulation", product: "Build, Frame" },
  { name: "SDF", category: "Data", what: "Simulation Description Format — Gazebo-native world/robot description", useCase: "Gazebo simulation worlds", product: "Build, Frame" },
  { name: "USD", category: "Data", what: "Universal Scene Description — Pixar/Omniverse 3D scene format", useCase: "Isaac Sim, Omniverse, digital twins", product: "Frame, Twin" },

  // ── Simulation ────────────────────────────────────────────────────────────────
  { name: "Gazebo Harmonic", category: "Simulation", what: "Open-source 3D robot simulator (ROS 2-native)", useCase: "Development, testing, digital twins", product: "Frame, Twin, Proof" },
  { name: "Isaac Sim", category: "Simulation", what: "NVIDIA Isaac Sim — high-fidelity GPU simulator", useCase: "VLA training data, sim-to-real, digital twins", product: "Frame, Twin, Train" },
  { name: "MuJoCo", category: "Simulation", what: "Multi-Joint dynamics with Contact — physics simulator", useCase: "RL training, research", product: "Train" },
];

const CATEGORIES: { label: StandardEntry["category"]; color: string; icon: string }[] = [
  { label: "Communication", color: "var(--cyan)", icon: "01" },
  { label: "Safety", color: "var(--violet-lite)", icon: "02" },
  { label: "Data", color: "#FBBF24", icon: "03" },
  { label: "Simulation", color: "#34D399", icon: "04" },
];

export default function StandardsPage() {
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
              Industry Standards
            </span>
          </div>
          <h1 className="font-display font-bold text-[clamp(34px,6vw,68px)] tracking-tight leading-[1.05] mb-6 legible">
            Speaks every protocol<br />your <span className="text-cyan">facility</span> already runs.
          </h1>
          <p className="text-[clamp(16px,2.2vw,20px)] leading-[1.6] max-w-2xl mb-12 legible" style={{ color: "rgba(255,255,255,0.66)" }}>
            OhhO is built on the industry standards the robotics world already uses — from CANopen
            and OPC UA to VDA 5050 and ISO 3691-4. No proprietary lock-in, no protocol parsers to
            write. Your robots plug into the systems your plant, warehouse and cloud already have.
          </p>

          {/* ── Stats bar ── */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-20">
            {[
              { n: String(STANDARDS.filter((s) => s.category === "Communication").length), label: "Communication protocols" },
              { n: String(STANDARDS.filter((s) => s.category === "Safety").length), label: "Safety standards" },
              { n: String(STANDARDS.filter((s) => s.category === "Data").length), label: "Data & description formats" },
              { n: String(STANDARDS.filter((s) => s.category === "Simulation").length), label: "Simulation engines" },
            ].map((s) => (
              <div
                key={s.label}
                className="rounded-2xl p-5 text-center backdrop-blur-md"
                style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
              >
                <div className="font-display font-bold text-[34px] tracking-tight mb-1" style={{ color: "var(--cyan)" }}>
                  {s.n}
                </div>
                <div className="text-[12px] leading-[1.5]" style={{ color: "rgba(255,255,255,0.6)" }}>
                  {s.label}
                </div>
              </div>
            ))}
          </div>

          {/* ── Standards by category ── */}
          {CATEGORIES.map((cat) => {
            const entries = STANDARDS.filter((s) => s.category === cat.label);
            return (
              <div key={cat.label} className="mb-20">
                <div className="flex items-center gap-3 mb-8">
                  <span className="font-mono text-[12px] font-bold" style={{ color: cat.color }}>
                    {cat.icon} //
                  </span>
                  <h2 className="font-display font-bold text-[clamp(22px,3vw,32px)] tracking-tight legible">
                    {cat.label}
                  </h2>
                  <span className="text-[13px] font-mono" style={{ color: "rgba(255,255,255,0.4)" }}>
                    {entries.length} standards
                  </span>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {entries.map((s) => (
                    <div
                      key={s.name}
                      className="rounded-2xl p-5 backdrop-blur-md"
                      style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.07)" }}
                    >
                      <div className="flex items-start justify-between gap-3 mb-2">
                        <div className="font-display text-[16px] font-semibold legible">{s.name}</div>
                        <span
                          className="font-mono text-[9px] font-semibold px-2 py-1 rounded-full whitespace-nowrap"
                          style={{ color: cat.color, background: "rgba(255,255,255,0.04)", border: `1px solid rgba(255,255,255,0.1)` }}
                        >
                          {s.product}
                        </span>
                      </div>
                      <p className="text-[13px] leading-[1.6] mb-2" style={{ color: "rgba(255,255,255,0.62)" }}>
                        {s.what}
                      </p>
                      <p className="text-[12px] leading-[1.5] font-mono" style={{ color: "rgba(255,255,255,0.4)" }}>
                        → {s.useCase}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}

          {/* ── CTA ── */}
          <div
            className="rounded-3xl p-10 text-center backdrop-blur-md"
            style={{ background: "rgba(0,212,255,0.04)", border: "1px solid rgba(0,212,255,0.18)" }}
          >
            <h2 className="font-display font-bold text-[clamp(24px,3.5vw,38px)] tracking-tight mb-4 legible">
              Missing a standard?
            </h2>
            <p className="text-[15px] leading-[1.7] max-w-xl mx-auto mb-8" style={{ color: "rgba(255,255,255,0.62)" }}>
              OhhO is open-source and community-extensible. Every bridge is a standalone adapter
              module — new protocols are added as a new adapter, no platform fork. On Forge, the
              OhhO team builds and maintains custom bridges for proprietary protocols.
            </p>
            <a
              href="/#products"
              className="inline-flex items-center gap-2 text-sm font-semibold px-6 py-3 rounded-lg transition-all duration-200 hover:-translate-y-0.5"
              style={{ background: "var(--cyan)", color: "var(--bg)" }}
            >
              Explore all 19 products →
            </a>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
