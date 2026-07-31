"use client";

/*
 * LogoMarquee — the homepage "Built on the open stack" logo wall.
 *
 * A multi-row infinite scroller of brand wordmarks across every layer of the
 * OhhO stack: simulation, training, models, web platform, robot OEMs, compute
 * hardware, sensors, cloud providers, data engineering, AI coding tools, and
 * insurance for the physical world. Each chip carries a small brand-color
 * accent dot so the wall reads as a color spectrum without becoming visually
 * noisy.
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
 *   section is the ONE sanctioned surface that names real brands — each row
 *   is framed as the stack OhhO is built on / runs on / deployed on /
 *   engineered with / trusted by, not as a "partner" relationship. Brands
 *   listed are drawn from real dependencies (pyproject.toml /
 *   requirements.txt / package.json / infra/), supported runtime targets
 *   described in product copy, or industry-standard vendors in adjacent
 *   categories. Aspirational signed partners live in
 *   docs/market analysis/11-partners.md and are deliberately NOT shown here.
 */

import { useScrollReveal } from "@/hooks/useScrollReveal";

interface Brand {
  name: string;
  accent: string;
  category: string;
}

// ── Section 1 — Built on the open stack ────────────────────────────────
// Three rows: simulation engines, training libraries + VLA models + agent
// loop, and the web/robot-OEM/VR surface.

// Row 1 — Simulation engines (scrolls left). Drawn from
// website/AGENTS.md §3 (simulation-engine standards list) + rl_engine Isaac
// Lab + lerobot_engine MuJoCo references.
const ROW_SIMULATION: Brand[] = [
  { name: "NVIDIA Isaac Sim", accent: "#76b900", category: "High-fidelity sim" },
  { name: "NVIDIA Isaac Lab", accent: "#76b900", category: "RL training env" },
  { name: "Gazebo Harmonic", accent: "#f69321", category: "ROS 2-native sim" },
  { name: "MuJoCo", accent: "#22d3ee", category: "Physics sim · DeepMind" },
  { name: "Webots", accent: "#3b82f6", category: "Open-source sim · Cyberbotics" },
  { name: "PyBullet", accent: "#facc15", category: "Bullet physics" },
  { name: "CARLA", accent: "#06b6d4", category: "Autonomous-vehicle sim" },
  { name: "ManiSkill", accent: "#a855f7", category: "Manipulation benchmark sim" },
];

// Row 2 — Training libraries, VLA models, agent loop, protocols (scrolls right).
// Every entry here appears in a requirements.txt or pyproject.toml in this
// repo, or is named as a model/method supported by the OmniVLA engine.
const ROW_TRAINING: Brand[] = [
  { name: "PyTorch", accent: "#ee4c2c", category: "Deep learning" },
  { name: "Hugging Face LeRobot", accent: "#ffd21e", category: "VLA datasets + backbones" },
  { name: "HF Transformers", accent: "#ffd21e", category: "Model library" },
  { name: "HF Datasets", accent: "#ffd21e", category: "Dataset hub" },
  { name: "Accelerate", accent: "#ffd21e", category: "Distributed training" },
  { name: "bitsandbytes", accent: "#f97316", category: "4-bit quantization" },
  { name: "OpenVLA", accent: "#3b82f6", category: "VLA backbone · 7B" },
  { name: "SmolVLA", accent: "#22d3ee", category: "9-DOF mobile-manip policy" },
  { name: "ACT", accent: "#4ade80", category: "Action Chunking Transformer" },
  { name: "Diffusion Policy", accent: "#a855f7", category: "Diffusion-based policy" },
  { name: "ONNX", accent: "#00a1f1", category: "Model exchange + Fleet OTA" },
  { name: "TensorRT", accent: "#76b900", category: "NVIDIA inference acceleration" },
  { name: "OpenCV", accent: "#ee4c2c", category: "Computer vision" },
  { name: "Pillow", accent: "#facc15", category: "Image processing" },
  { name: "einops", accent: "#06b6d4", category: "Tensor reshape" },
  { name: "Weights & Biases", accent: "#ffb400", category: "Experiment tracking + sweeps" },
  { name: "TensorBoard", accent: "#ff8c00", category: "Training visualization" },
  { name: "Anthropic Claude", accent: "#cc785c", category: "Mind reasoner · LLM" },
  { name: "LangGraph", accent: "#4ade80", category: "Agent loop orchestration" },
  { name: "FastAPI", accent: "#05998b", category: "Serve REST endpoint" },
  { name: "Uvicorn", accent: "#06b6d4", category: "ASGI server" },
  { name: "Pydantic", accent: "#e34f26", category: "API validation" },
  { name: "Cyclone DDS", accent: "#6d4aff", category: "Robot pub/sub protocol" },
  { name: "MAVLink", accent: "#2a8fed", category: "Drone protocol" },
  { name: "Open-RMF", accent: "#f59e0b", category: "Fleet coordination" },
];

// Row 3 — Web platform, auth, billing, 3-D, robot OEMs, VR, observability
// (scrolls left). Brand-agnostic OEM brands are listed here because they
// speak the open protocols Bridge translates — VDA 5050, ROS-Industrial,
// MAVLink, CANopen, or a serial protocol — so OhhO's "any robot" claim
// applies by construction.
const ROW_OEMS: Brand[] = [
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

// ── Section 2 — Runs on any compute · sees with any sensor ─────────────
// Two rows: compute hardware + sensors.

// Row 4 — Compute (scrolls left). Drawn from built-in hardware profiles in
// sdk/ohho/profiles.py + learning_engine edge-AI matrix. OhhO's
// `device="auto"` resolution picks the execution provider present, so any
// GPU/NPU/CPU runs the exported ONNX policy.
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

// Row 5 — Sensors (scrolls right). OhhO's ROS 2 perception stack consumes
// standard sensor_msgs topics, so any ROS-2-compatible sensor works.
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

// ── Section 3 — Deployed on any cloud · engineered with AI ────────────
// New section: cloud + GPU providers, data engineering + labeling tools,
// and AI coding + workflow-automation tools used to ship the platform.

// Row 6 — Cloud providers + GPU compute + CI/CD workflows (scrolls left).
// OhhO's marketing explicitly states "any cloud — or none." Cloud providers
// below are deploy targets for Train / Serve / Fleet, not software deps.
// GitHub + GitHub Actions are real CI/CD deps (.github/workflows/ros2_ci.yml).
const ROW_CLOUD: Brand[] = [
  { name: "Vercel", accent: "#ffffff", category: "Web hosting · deploys" },
  { name: "GitHub", accent: "#ffffff", category: "Source + repo + releases" },
  { name: "GitHub Actions", accent: "#2088ff", category: "CI / CD pipeline" },
  { name: "AWS", accent: "#ff9900", category: "Amazon Web Services" },
  { name: "Google Cloud", accent: "#4285f4", category: "GCP · GPUs" },
  { name: "Microsoft Azure", accent: "#0078d4", category: "Azure · GPUs" },
  { name: "Modal", accent: "#facc15", category: "Serverless GPU compute" },
  { name: "RunPod", accent: "#6366f1", category: "GPU on demand" },
  { name: "Lambda Labs", accent: "#22d3ee", category: "GPU cloud" },
  { name: "CoreWeave", accent: "#3b82f6", category: "GPU cloud" },
  { name: "Replicate", accent: "#a855f7", category: "Model hosting API" },
  { name: "HuggingFace Hub", accent: "#ffd21e", category: "Model + dataset hub" },
  { name: "Docker Hub", accent: "#2496ed", category: "Container registry" },
  { name: "Colab", accent: "#f9a825", category: "Free GPU notebooks · Google" },
];

// Row 7 — Data engineering, collection, cleaning, labeling tools
// (scrolls right). Apache Arrow + Parquet + MCAP are the real dataset
// formats named in the repo. OpenCV/Pillow read frames. Roboflow / Labelbox
// / Encord / V7 / Dataloop are the labeling stack OhhO Data interoperates
// with via LeRobot format (no hard dep — LeRobot is portable).
const ROW_DATA: Brand[] = [
  { name: "Apache Arrow / Parquet", accent: "#7b3cea", category: "Columnar dataset format" },
  { name: "MCAP", accent: "#1e40af", category: "ROS 2-native bag format" },
  { name: "LeRobot HF", accent: "#ffd21e", category: "Episode dataset format" },
  { name: "Pandas", accent: "#150458", category: "DataFrame processing" },
  { name: "NumPy", accent: "#4dabf7", category: "Numeric core" },
  { name: "DuckDB", accent: "#fff000", category: "In-process OLAP" },
  { name: "OpenCV", accent: "#ee4c2c", category: "Frame capture + vision" },
  { name: "Roboflow", accent: "#f28c28", category: "Data labeling + pipelining" },
  { name: "Labelbox", accent: "#2496ed", category: "Data labeling platform" },
  { name: "Encord", accent: "#3b0764", category: "Annotation tooling" },
  { name: "V7 Labs", accent: "#ff5c00", category: "Data labeling + cv" },
  { name: "Dataloop", accent: "#0077b6", category: "Data engine + curation" },
  { name: "Sky Alliance", accent: "#facc15", category: "Teleop data workforce" },
  { name: "Apache Iceberg", accent: "#22c55e", category: "Open table format" },
  { name: "DVC", accent: "#c7254e", category: "Data version control" },
  { name: "GitHub LFS", accent: "#2088ff", category: "Large-asset storage" },
];

// Row 8 — AI coding + workflow automation tools (scrolls left). These are
// the tools OhhO is engineered with — assistants used to design, write and
// review this codebase. Honest framing is "engineered with AI," not "built
// by AI" — humans are still in the loop.
const ROW_TOOLS: Brand[] = [
  { name: "Claude Code", accent: "#cc785c", category: "Anthropic AI coder" },
  { name: "Anthropic Claude", accent: "#cc785c", category: "Frontier reasoning" },
  { name: "GitHub Copilot", accent: "#2088ff", category: "AI pair-programming" },
  { name: "Cursor", accent: "#000000", category: "AI-native code editor" },
  { name: "Continue.dev", accent: "#22c55e", category: "Open-source AI code assist" },
  { name: "Aider", accent: "#dc2626", category: "CLI AI pair-programmer" },
  { name: "Codeium", accent: "#09b6a2", category: "AI code completion" },
  { name: "Tabnine", accent: "#f97316", category: "AI code completion" },
  { name: "Sourcegraph Cody", accent: "#ff5028", category: "Code + AI search" },
  { name: "OpenAI Codex", accent: "#10a37f", category: "Code LLM backend" },
  { name: "OpenCode", accent: "#06b6d4", category: "OSS AI coding agent" },
  { name: "Graphify", accent: "#facc15", category: "Code → knowledge graph" },
  { name: "Notion", accent: "#ffffff", category: "Docs + spec writing" },
  { name: "Linear", accent: "#5e6ad2", category: "Issue tracking" },
  { name: "Figma", accent: "#a259ff", category: "Design + brand" },
];

// Row 9 — Insurance & risk for physical AI (scrolls right). OhhO's Comply
// + Proof + Shield are designed to feed evidence into cert and
// insurance workflows. These are the providers that insure physical-AI
// fleets — the trust stack's closing layer.
const ROW_INSURANCE: Brand[] = [
  { name: "Chubb", accent: "#cc0000", category: "Cyber + robotics insurance · US" },
  { name: "Munich Re", accent: "#003c71", category: "AI risk underwriting · Germany" },
  { name: "AXA", accent: "#0000ff", category: "Robotics insurance · France" },
  { name: "Allianz", accent: "#003781", category: "AI + robotics insurance · Germany" },
  { name: "Swiss Re", accent: "#e4002d", category: "AI risk modeling · Switzerland" },
  { name: "Hiscox", accent: "#c8102e", category: "Specialty insurance · UK" },
  { name: "Lloyd's of London", accent: "#9b1c24", category: "Specialty risk market · UK" },
  { name: "Zurich Insurance", accent: "#0066b3", category: "Robotics insurance · Switzerland" },
  { name: "AIG", accent: "#003972", category: "Cyber + AI liability · US-global" },
  { name: "Marsh", accent: "#1e3a8a", category: "Risk + insurance brokerage · US-global" },
  { name: "Safety National", accent: "#0f766e", category: "Surplus-lines specialty · US" },
  { name: "CNA", accent: "#c8102e", category: "Commercial robotics insurance · US" },
  { name: "Beazley", accent: "#e52b50", category: "Specialty cyber · UK" },
  { name: "HDI Global", accent: "#005baa", category: "Industrial robotics insurance · Germany" },
  { name: "Cap Speciality", accent: "#003087", category: "Tech + robotics specialty · US" },
  { name: "Tokio Marine", accent: "#c80000", category: "Robotics insurance · Japan" },
];

export default function LogoMarquee() {
  const { ref, inView } = useScrollReveal();

  return (
    <section id="stack" style={{ padding: "72px 0 96px" }}>
      {/* ── Section 1 — "Built on the open stack" ── */}
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
            already runs on — no proprietary lock-in, no black boxes. We
            extend the ecosystem; we don&rsquo;t wall it.
          </p>
        </div>
      </div>

      <div
        className="marquee-pause"
        style={{ display: "flex", flexDirection: "column", gap: "16px" }}
      >
        <MarqueeRow brands={ROW_SIMULATION} reverse={false} />
        <MarqueeRow brands={ROW_TRAINING} reverse={true} />
        <MarqueeRow brands={ROW_OEMS} reverse={false} />
      </div>

      {/* ── Section 2 — "Runs on any compute · sees with any sensor" ── */}
      <div className="max-w-content mx-auto px-6" style={{ marginTop: "48px" }}>
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
            The same robot-agnostic runtime deploys to a Raspberry Pi, a
            Jetson, a Rockchip SoC, a DeepX NPU, or a workstation GPU —
            and perceives through any ROS&nbsp;2-compatible sensor, anywhere
            in the world.
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

      {/* ── Section 3 — "Deployed on any cloud · engineered with AI" ── */}
      <div className="max-w-content mx-auto px-6" style={{ marginTop: "48px" }}>
        <div
          className="text-center mb-[24px]"
          style={{ opacity: inView ? 1 : 0 }}
        >
          <div
            className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[6px]"
            style={{ color: "var(--cyan)" }}
          >
            Deployed on any cloud · engineered with AI
          </div>
          <p className="text-[14px] leading-[1.6] max-w-[540px] mx-auto legible" style={{ color: "rgba(255,255,255,0.56)" }}>
            Train on AWS, GCP, Azure, Modal or your own cluster. Build with
            AI-assisted dev tools. Label with Roboflow, Encord or your own
            stack. Every format is portable: LeRobot HF, Parquet, MCAP, ONNX.
            And when fleet fleets enter the physical world, OhhO&rsquo;s
            Comply + Proof + Shield feed evidence straight into the carriers
            that insure physical-AI risk.
          </p>
        </div>
      </div>

      <div
        className="marquee-pause"
        style={{ display: "flex", flexDirection: "column", gap: "16px" }}
      >
        <MarqueeRow brands={ROW_CLOUD} reverse={false} />
        <MarqueeRow brands={ROW_DATA} reverse={true} />
        <MarqueeRow brands={ROW_TOOLS} reverse={false} />
        <MarqueeRow brands={ROW_INSURANCE} reverse={true} />
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