"use client";

/*
 * LogoMarquee — the homepage "Built on the open stack" logo wall.
 *
 * A two-row infinite scroller of brand wordmarks representing the OSS
 * projects, runtimes, standards and robots the OhhO platform is built
 * with. Each chip carries a small brand-color accent dot so the wall
 * reads as a color spectrum without becoming visually noisy (the site
 * theme is dark cyan/violet — colored text chips would clash).
 *
 * Inserted between StatsBar and BrandAgnostic on the homepage:
 *   hero → proof stats → "built with" logo wall → open-ecosystem moat.
 *
 * Accessibility + perf:
 *   - Honors prefers-reduced-motion (marquee-track animation gated in
 *     globals.css via the global reduce block).
 *   - Honors [data-perf="lite"] (animation dropped for low-power tiers).
 *   - Pauses on hover (.marquee-pause:hover) so users can read a brand.
 *   - Edge fade via mask-image so chips glide in/out instead of hard-cut.
 *
 * Brand context:
 *   Per website/AGENTS.md §2, marketing copy must stay brand-agnostic. This
 *   section is the ONE sanctioned surface that names real brands — it
 *   reads as "the open stack OhhO is built with," not "our partners."
 *   Only brands that appear as real dependencies in pyproject.toml /
 *   requirements.txt / package.json / infra/ are listed here.
 *
 * See docs/market analysis/11-partners.md for the aspirational partner
 * list — those are not yet signed and are deliberately NOT shown here.
 */

import { useScrollReveal } from "@/hooks/useScrollReveal";

interface Brand {
  name: string;
  accent: string;
  category: string;
}

// Row 1 — AI, simulation, training, standards (scrolls left)
const ROW_TOP: Brand[] = [
  { name: "NVIDIA Isaac", accent: "#76b900", category: "Simulation" },
  { name: "Gazebo", accent: "#f69321", category: "Simulation (ROS 2)" },
  { name: "PyTorch", accent: "#ee4c2c", category: "Deep learning" },
  { name: "Hugging Face LeRobot", accent: "#ffd21e", category: "VLA datasets" },
  { name: "ONNX", accent: "#00a1f1", category: "Model exchange" },
  { name: "Anthropic Claude", accent: "#cc785c", category: "Mind reasoner" },
  { name: "LangGraph", accent: "#4ade80", category: "Agent loop" },
  { name: "FastAPI", accent: "#05998b", category: "Serve API" },
  { name: "Weights & Biases", accent: "#ffb400", category: "Experiment tracking" },
  { name: "Cyclone DDS", accent: "#6d4aff", category: "Robot protocol" },
  { name: "MAVLink", accent: "#2a8fed", category: "Drone protocol" },
  { name: "Open-RMF", accent: "#f59e0b", category: "Fleet coordination" },
];

// Row 2 — Web platform, cloud, robot hardware (OEMs), VR, observability.
// International robot OEMs + open-software cobot arms joined below: OhhO's
// "any robot" claim covers any robot that publishes ROS 2 topics, speaks
// MAVLink, CANopen, Modbus, ROS-Industrial, or a serial protocol.
const ROW_BOTTOM: Brand[] = [
  { name: "Next.js + Vercel", accent: "#ffffff", category: "Web platform" },
  { name: "Supabase", accent: "#3ecf8e", category: "Auth + database" },
  { name: "Firebase", accent: "#ffca28", category: "Analytics" },
  { name: "Stripe", accent: "#635bff", category: "Billing" },
  { name: "Three.js", accent: "#e2e8f0", category: "3-D hero scene" },
  { name: "Unitree", accent: "#f87171", category: "G1 · H1 · Go2 · China" },
  { name: "Trossen / SO-101", accent: "#60a5fa", category: "Reference arm · US" },
  { name: "Unity", accent: "#cbd5e1", category: "VR engine" },
  { name: "OpenXR", accent: "#00a4e4", category: "XR standard" },
  { name: "Meta Quest", accent: "#1b69de", category: "MR headset · US" },
  { name: "Grafana", accent: "#f46800", category: "Observability" },
  { name: "Docker", accent: "#2496ed", category: "Containerization" },
  { name: "DJI", accent: "#3b82f6", category: "Drones · MAVLink · China" },
  { name: "Franka Emika", accent: "#8b5cf6", category: "7-DOF cobot · ROS-Industrial · Germany" },
  { name: "Universal Robots", accent: "#22c55e", category: "UR cobots · Denmark" },
  { name: "KUKA", accent: "#f97316", category: "Industrial arms · Germany" },
  { name: "UBTECH", accent: "#06b6d4", category: "Walker humanoid · China" },
  { name: "PAL Robotics", accent: "#e11d48", category: "TIAGo · Spain" },
];

// Row 3 — Compute hardware, microcontrollers, AI accelerators (scrolls left).
// Drawn from the built-in hardware profiles in sdk/ohho/profiles.py and the
// learning_engine/ARCHITECTURE.md edge-AI matrix. International compute added
// below — OhhO's `device="auto"` resolution picks the execution provider
// present, so any GPU/NPU/CPU runs the exported ONNX policy.
const ROW_HARDWARE: Brand[] = [
  { name: "Raspberry Pi", accent: "#c51c4c", category: "Pi 5 · robot brain · UK" },
  { name: "NVIDIA Jetson", accent: "#76b900", category: "Orin · edge AI · US" },
  { name: "DeepX NPU", accent: "#a855f7", category: "On-device AI accelerator · Korea" },
  { name: "Google Coral", accent: "#4285f4", category: "Edge TPU · US" },
  { name: "Hailo", accent: "#ff6b35", category: "AI accelerator · Israel" },
  { name: "Intel", accent: "#0071c5", category: "OpenVINO · x86 · US" },
  { name: "Apple Silicon", accent: "#a2aaad", category: "M-series · dev" },
  { name: "STM32", accent: "#03234b", category: "Microcontroller · France" },
  { name: "Arduino", accent: "#00979d", category: "Microcontroller · Italy" },
  { name: "ESP32", accent: "#e7352c", category: "Microcontroller · Espressif · China" },
  { name: "Feetech", accent: "#f59e0b", category: "STS3215 arm servos · China" },
  { name: "Yahboom", accent: "#10b981", category: "Motor board · China" },
  { name: "Rockchip RK3588", accent: "#ef4444", category: "ARM SoC · China" },
  { name: "Huawei Ascend", accent: "#dc2626", category: "Atlas NPU · China" },
  { name: "Orange Pi", accent: "#f97316", category: "ARM SBC · China" },
  { name: "Seeed Studio", accent: "#16a34a", category: "reComputer · China" },
  { name: "Renesas", accent: "#1e40af", category: "Robotics MCUs · Japan" },
  { name: "Qualcomm RB", accent: "#3253dc", category: "Snapdragon robotics · US-global" },
];

// Row 4 — Sensors, cameras, depth, LiDAR, IMU (scrolls right). OhhO's ROS 2
// perception stack consumes standard sensor_msgs topics, so any
// ROS-2-compatible sensor works. International sensor brands joined below —
// the robotics community buys LiDAR from China (Livox/RoboSense/Hesai),
// Japan (Hokuyo/Murata), Germany (ifm/Pepperl+Fuchs), and the US alike.
const ROW_SENSORS: Brand[] = [
  { name: "Intel RealSense", accent: "#0071c5", category: "Depth cameras · US" },
  { name: "Stereolabs ZED", accent: "#3b82f6", category: "3-D cameras · Italy" },
  { name: "Luxonis OAK-D", accent: "#10b981", category: "AI cameras · US" },
  { name: "Arducam", accent: "#ef4444", category: "Camera modules · China" },
  { name: "SICK", accent: "#ffcc00", category: "LiDAR · industrial · Germany" },
  { name: "Velodyne LiDAR", accent: "#fb923c", category: "LiDAR · US" },
  { name: "Ouster", accent: "#22d3ee", category: "LiDAR · US" },
  { name: "RPLIDAR", accent: "#84cc16", category: "Slamtec · 2-D LiDAR · China" },
  { name: "Bosch Sensortec", accent: "#ed1c24", category: "IMU · sensors · Germany" },
  { name: "InvenSense", accent: "#f97316", category: "IMU · Japan" },
  { name: "Garmin Lidar Lite", accent: "#0071c5", category: "Range · Taiwan" },
  { name: "FLIR", accent: "#facc15", category: "Thermal cameras · US" },
  { name: "Livox", accent: "#3b82f6", category: "Mid/Tele LiDAR · DJI · China" },
  { name: "RoboSense", accent: "#06b6d4", category: "Solid-state LiDAR · China" },
  { name: "Hesai", accent: "#a855f7", category: "Hybrid LiDAR · China" },
  { name: "Orbbec", accent: "#f59e0b", category: "Depth cameras · China" },
  { name: "Hokuyo", accent: "#dc2626", category: "2-D LiDAR · Japan" },
  { name: "ifm electronic", accent: "#1e40af", category: "Proximity + 3-D · Germany" },
];

export default function LogoMarquee() {
  const { ref, inView } = useScrollReveal();

  return (
    <section id="stack" style={{ padding: "72px 0 88px" }}>
      <div className="max-w-content mx-auto px-6">
        <div
          ref={ref}
          className="text-center max-w-[620px] mx-auto mb-[48px] transition-all duration-[650ms]"
          style={{ opacity: inView ? 1 : 0, transform: inView ? "none" : "translateY(22px)" }}
        >
          <div
            className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]"
            style={{ color: "var(--cyan)" }}
          >
            Built on the open stack
          </div>
          <h2 className="font-display font-bold text-[clamp(26px,3.6vw,38px)] tracking-tight leading-[1.12] mb-4 legible">
            The same open tools you<br />
            <span className="text-cyan">already know and trust.</span>
          </h2>
          <p className="text-[15px] leading-[1.7] mx-auto legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            OhhO is built on the open-source foundation the robotics world
            already runs on — no proprietary lock-in, no black boxes. We extend
            the ecosystem; we don&rsquo;t wall it.
          </p>
        </div>
      </div>

      <div
        className="marquee-pause"
        style={{ display: "flex", flexDirection: "column", gap: "16px" }}
      >
        <MarqueeRow brands={ROW_TOP} reverse={false} />
        <MarqueeRow brands={ROW_BOTTOM} reverse={true} />
      </div>

      <div className="max-w-content mx-auto px-6" style={{ marginTop: "40px" }}>
        <div
          className="text-center mb-[24px]"
          style={{ opacity: inView ? 1 : 0 }}
        >
          <div
            className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[6px]"
            style={{ color: "var(--violet-lite)" }}
          >
            Runs on any compute · sees with any sensor
          </div>
          <p className="text-[14px] leading-[1.6] max-w-[520px] mx-auto legible" style={{ color: "rgba(255,255,255,0.56)" }}>
            The same robot-agnostic runtime deploys to a Raspberry Pi, a Jetson,
            a Rockchip SoC, a DeepX NPU, or a workstation GPU — and perceives
            through any ROS&nbsp;2-compatible sensor, anywhere in the world.
          </p>
        </div>
      </div>

      <div
        className="marquee-pause"
        style={{ display: "flex", flexDirection: "column", gap: "16px" }}
      >
        <MarqueeRow brands={ROW_HARDWARE} reverse={false} />
        <MarqueeRow brands={ROW_SENSORS} reverse={true} />
      </div>
    </section>
  );
}

function MarqueeRow({ brands, reverse }: { brands: Brand[]; reverse: boolean }) {
  // Duplicate the brand list once per row so the track is 2× wide; the
  // marquee-scroll keyframe translates the track by -50%, which lands
  // on the exact start of the second copy — a seamless infinite loop.
  const doubled = [...brands, ...brands];
  return (
    <div className="marquee-viewport">
      <div className={`marquee-track${reverse ? " reverse" : ""}`}>
        {doubled.map((b, i) => (
          <BrandChip key={`${b.name}-${i}`} brand={b} />
        ))}
      </div>
    </div>
  );
}

function BrandChip({ brand }: { brand: Brand }) {
  return (
    <div
      className="flex flex-col gap-1 flex-shrink-0"
      style={{
        padding: "14px 22px",
        margin: "0 6px",
        borderRadius: "14px",
        background: "rgba(255,255,255,0.025)",
        border: "1px solid rgba(255,255,255,0.08)",
        minWidth: "168px",
        minHeight: "72px",
        justifyContent: "center",
        alignItems: "center",
        textAlign: "center",
      }}
    >
      <div className="flex items-center gap-2">
        <span
          className="inline-block rounded-full flex-shrink-0"
          style={{ width: "6px", height: "6px", background: brand.accent }}
          aria-hidden
        />
        <span
          className="font-display text-[14.5px] font-semibold legible whitespace-nowrap"
          style={{ color: "rgba(255,255,255,0.88)" }}
        >
          {brand.name}
        </span>
      </div>
      <div
        className="font-mono text-[10px] tracking-[0.04em] uppercase"
        style={{ color: "rgba(255,255,255,0.42)" }}
      >
        {brand.category}
      </div>
    </div>
  );
}