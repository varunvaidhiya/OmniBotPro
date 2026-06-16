/*
 * Single source of truth for the OhhO product catalog.
 *
 * Both the homepage Products grid (components/Products.tsx) and the per-product
 * "Learn more" detail pages (app/products/[slug]/page.tsx) read from here, so a
 * product is described in exactly one place.
 *
 * This module is plain data + presentational SVG icons (no hooks, no client
 * state), so it can be imported from both server and client components.
 */

import type { ReactNode } from "react";

export type Accent = "cyan" | "violet";
export type PlanName = "Spark" | "Builder" | "Fleet" | "Forge";
export type Category =
  | "Design"
  | "Foundation"
  | "Intelligence"
  | "Operations"
  | "Trust";

export interface Feature {
  title: string;
  body: string;
}

export interface Step {
  title: string;
  body: string;
}

export interface Spec {
  label: string;
  value: string;
}

export interface PlanAccess {
  plan: PlanName;
  /** Short description of what this plan gives you for THIS product. */
  level: string;
  included: boolean;
}

export interface Faq {
  q: string;
  a: string;
}

export interface Product {
  slug: string;
  name: string;
  /** One-line tagline shown on the card and detail hero. */
  tag: string;
  /** Short description used on the homepage card. */
  desc: string;
  accent: Accent;
  category: Category;
  icon: ReactNode;

  /** Detail-page hero paragraph (the "what is this" elevator pitch). */
  hero: string;
  /** Quick value bullets shown beside the hero. */
  highlights: string[];
  /** Longer "what you get" body paragraphs. */
  overview: string[];
  features: Feature[];
  how: Step[];
  specs: Spec[];
  plans: PlanAccess[];
  recommendedPlan: PlanName;
  planRationale: string;
  faq: Faq[];
  /** Related product slugs. */
  related: string[];
  /** Caption shown under the embedded dashboard mockup. */
  dashboardCaption: string;
}

const stroke = {
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

export const PRODUCTS: Product[] = [
  // ── DESIGN ────────────────────────────────────────────────────────────────
  {
    slug: "build",
    name: "OhhO Build",
    tag: "Design any robot. For any industry.",
    desc: "Browser-based 3-D robot designer. Drag in components, set your requirements, and get an AI-validated design with a sourced bill of materials — then bring it to life with the full OhhO stack.",
    accent: "cyan",
    category: "Design",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <rect x="3" y="3" width="18" height="18" rx="2" />
        <path d="M3 9h18" />
        <path d="M9 21V9" />
        <circle cx="6" cy="6" r=".9" fill="currentColor" stroke="none" />
        <path d="m14.5 14.5 2.5 2.5" />
        <path d="m17 14.5-2.5 2.5" />
      </svg>
    ),
    hero: "Design any robot in your browser. Drag parts onto a 3-D canvas, set your requirements — payload, reach, speed, environment — and Build's recommendation engine returns a validated design with a sourced bill of materials.",
    highlights: [
      "Drag-and-drop 3-D canvas",
      "Requirements-driven validation",
      "AI design recommendations",
      "One-click sourced bill of materials",
      "Exports URDF + sim world to the stack",
    ],
    overview: [
      "OhhO Build is the front door to the entire platform. It's a browser-based 3-D robot designer where anyone — an integrator, a factory engineer, a startup, or a hobbyist — can assemble a robot from a library of real, in-stock components. No CAD license, no mechanical-engineering degree, no blank-page paralysis.",
      "You describe what the robot needs to do. Build does the hard part: it checks reach envelopes, payload and torque budgets, wheelbase stability, power draw and battery runtime, and flags anything that won't physically work. When a combination is sub-optimal, it recommends a better one and explains why.",
      "When you're happy, Build turns your design into a sourced bill of materials with supplier links and lead times, and hands the whole thing off to the rest of the OhhO stack — a matching URDF, simulation world and deployment profile — so the robot you designed on screen is the robot you operate in the real world.",
    ],
    features: [
      { title: "Real parts, real constraints", body: "Every component carries real specs: mass, torque, voltage, payload, dimensions, price and supplier. Build validates your assembly against them as you go." },
      { title: "Requirements engine", body: "Set targets for payload, reach, top speed, runtime, footprint and operating environment — indoor, outdoor, cleanroom, cold-chain — and Build checks your design against them continuously." },
      { title: "AI design recommendations", body: "Describe the job in plain language. Build proposes a complete starting design — base, drivetrain, arm, sensors, compute — and suggests upgrades or swaps as you iterate." },
      { title: "Sourced bill of materials", body: "Export a costed BOM with supplier links, lead times and alternates. Hand it to procurement, or send it straight to a build partner." },
      { title: "Hands off to the whole stack", body: "Your design exports a URDF, a simulation world and a deployment profile — so OhhO Frame, View, Serve and Fleet pick it up with zero re-modeling." },
      { title: "Works for any industry", body: "Warehouse AMRs, lab automation, agriculture, inspection, education — start from an industry template or a blank canvas." },
    ],
    how: [
      { title: "Describe the job", body: "Tell Build what the robot needs to do and where — or start from an industry template." },
      { title: "Drag, drop, validate", body: "Assemble on the 3-D canvas. Build checks payload, reach, stability and power on every change." },
      { title: "Take the recommendation", body: "Accept Build's suggested design, or tune it part-by-part with live feedback." },
      { title: "Source and deploy", body: "Export the BOM to order parts, and the URDF + sim world to the OhhO stack to start building software immediately." },
    ],
    specs: [
      { label: "Interface", value: "Browser-based 3-D (WebGL), no install" },
      { label: "Parts library", value: "Bases, drives, arms, grippers, sensors, compute" },
      { label: "Validation", value: "Payload, torque, reach, stability, power, runtime" },
      { label: "Output", value: "Costed BOM + URDF + simulation world" },
      { label: "Sourcing", value: "Supplier links, lead times, alternates" },
      { label: "Handoff", value: "OhhO Frame, View, Serve, Fleet" },
    ],
    plans: [
      { plan: "Spark", level: "Basic parts library, 1 design", included: true },
      { plan: "Builder", level: "Full library, up to 10 designs", included: true },
      { plan: "Fleet", level: "Unlimited designs + live supplier sourcing", included: true },
      { plan: "Forge", level: "Custom / white-label catalog", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Most teams designing more than a throwaway prototype want the full parts library and multiple saved designs — that's Builder. Move up to Fleet when you need live supplier sourcing and unlimited designs.",
    faq: [
      { q: "Do I need CAD or mechanical-engineering experience?", a: "No. Build is drag-and-drop, and the requirements engine catches the mechanical mistakes for you — it's made for product people and engineers alike." },
      { q: "Are the parts real and buyable?", a: "Yes. The library is built from real components with live specs, pricing and supplier links, so your exported BOM is an order-ready document." },
      { q: "What happens after I design a robot?", a: "Build exports a URDF, a simulation world and a deployment profile that the rest of the OhhO platform consumes — so you can simulate, add AI and operate the exact robot you designed." },
    ],
    related: ["frame", "view", "serve"],
    dashboardCaption:
      "OhhO Build — drag parts onto the 3-D canvas; the requirements panel and AI recommendation update live.",
  },

  // ── FOUNDATION ──────────────────────────────────────────────────────────────
  {
    slug: "frame",
    name: "OhhO Frame",
    tag: "Your robot stack, ready in one afternoon.",
    desc: "A production-grade robot software foundation. Docker, ROS 2, simulation and CI/CD — pre-wired. Single or multi-machine deployments out of the box.",
    accent: "violet",
    category: "Foundation",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M4 4h4v4H4z" />
        <path d="M16 4h4v4h-4z" />
        <path d="M4 16h4v4H4z" />
        <path d="M16 16h4v4h-4z" />
        <path d="M8 6h8" />
        <path d="M6 8v8" />
        <path d="M18 8v8" />
        <path d="M8 18h8" />
      </svg>
    ),
    hero: "The robot software stack, pre-wired. Docker, ROS 2, simulation, CI/CD and multi-machine deployment — a production-grade foundation you launch in an afternoon instead of assembling over months.",
    highlights: [
      "ROS 2 Jazzy workspace, pre-structured",
      "Docker + DevContainer build",
      "Gazebo & Isaac simulation included",
      "CI/CD pipeline ready",
      "Single or multi-machine deploy",
    ],
    overview: [
      "Every robot project starts by rebuilding the same plumbing: a ROS 2 workspace, Docker images, a simulator, message buses, launch files, a CI pipeline. OhhO Frame gives you all of it, already wired together and known-good.",
      "Frame is the chassis your robot's software rides on. It ships the workspace layout, containerized build, Gazebo and Isaac simulation, and single- or multi-machine deployment — robot plus GPU workstation — that the rest of the OhhO products plug straight into.",
      "Built in the open on ROS 2 Jazzy, Frame is the same foundation that powers OhhO's own reference robot — so what you start from is a real, tested production stack, not a toy template.",
    ],
    features: [
      { title: "Batteries-included workspace", body: "Driver, description (URDF), navigation, perception and bringup packages laid out the way a production robot needs them." },
      { title: "Containerized everything", body: "Docker images and a DevContainer so 'works on my machine' becomes 'works on every machine' — including CI." },
      { title: "Simulate before you build", body: "Gazebo Harmonic and Isaac Sim worlds wired to the same topics as the real robot, so you develop with no hardware." },
      { title: "CI/CD out of the box", body: "A GitHub Actions pipeline builds the workspace and runs the test suite on every push." },
      { title: "Single or multi-machine", body: "One configurator switches between all-on-one-workstation and Pi-robot + GPU-desktop topologies, wiring DDS peers for you." },
    ],
    how: [
      { title: "Generate your stack", body: "Frame scaffolds the workspace, Docker, sim and CI from your robot profile — or directly from an OhhO Build export." },
      { title: "Develop in simulation", body: "Launch the simulator and iterate on navigation, perception and control with no hardware." },
      { title: "Deploy single or multi-machine", body: "Pick your topology; Frame wires DDS and the launch files." },
      { title: "Layer on the platform", body: "Add View, Serve, Data, Fleet and the rest on top of the same foundation." },
    ],
    specs: [
      { label: "ROS distro", value: "ROS 2 Jazzy (Ubuntu 24.04)" },
      { label: "Containers", value: "Docker + DevContainer" },
      { label: "Simulation", value: "Gazebo Harmonic + Isaac Sim" },
      { label: "CI", value: "GitHub Actions (build + colcon test)" },
      { label: "Deploy modes", value: "Single workstation / multi-machine" },
      { label: "Networking", value: "DDS peer auto-config (ROS_DOMAIN_ID 30)" },
    ],
    plans: [
      { plan: "Spark", level: "Simulation only", included: true },
      { plan: "Builder", level: "Single-machine + hardware", included: true },
      { plan: "Fleet", level: "Multi-machine deploy", included: true },
      { plan: "Forge", level: "On-prem + custom topologies", included: true },
    ],
    recommendedPlan: "Spark",
    planRationale:
      "Frame is included on every plan — even free. Start on Spark to build and simulate at no cost; you only need a paid plan once you're deploying to real hardware or multiple machines.",
    faq: [
      { q: "Do I need hardware to start?", a: "No. Frame's simulation runs entirely on your workstation — many teams build for weeks before touching a robot." },
      { q: "Is it locked to your hardware?", a: "No. Frame works with any ROS 2-compatible hardware; the reference drivers are a starting point you can swap." },
    ],
    related: ["build", "fleet", "data"],
    dashboardCaption:
      "OhhO Frame — workspace scaffold, containerized build and the node graph that ships ready to run.",
  },

  // ── INTELLIGENCE ────────────────────────────────────────────────────────────
  {
    slug: "serve",
    name: "OhhO Serve",
    tag: "Robot AI inference, as an API.",
    desc: "Deploy a Vision-Language-Action model behind a REST endpoint in one command. Pluggable model backends, batching and Prometheus metrics built in.",
    accent: "cyan",
    category: "Intelligence",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M13 2 3 14h9l-1 8 10-12h-9l1-8z" />
      </svg>
    ),
    hero: "Robot AI inference, as an API. Deploy a Vision-Language-Action model behind a REST endpoint in one command — pluggable model backends, batching, and Prometheus metrics built in.",
    highlights: [
      "One-command model deploy",
      "REST predict API",
      "Pluggable backends (OpenVLA, SmolVLA, ACT…)",
      "4-bit quantization option",
      "Prometheus metrics built in",
    ],
    overview: [
      "Modern robots run on large Vision-Language-Action (VLA) models, but getting one into production means GPU servers, model loading, batching, versioning and monitoring. OhhO Serve turns all of that into a single endpoint.",
      "Point Serve at a model — OpenVLA, SmolVLA, ACT, a diffusion policy, or your own fine-tune — and it exposes a clean REST API your robot calls with an image and an instruction, and gets back an action. Swap models without touching robot code.",
      "Serve is built for the real world: health checks, hot model loading, 4-bit quantization for tighter VRAM budgets, and first-class Prometheus metrics so you can see latency and throughput at a glance.",
    ],
    features: [
      { title: "Model-agnostic backend", body: "OpenVLA, SmolVLA, ACT, diffusion, or a custom class — selected by config, not code." },
      { title: "Clean predict API", body: "POST an image + instruction, get back an action vector. Your robot doesn't need to know which model is behind it." },
      { title: "Hot loading & health", body: "/health, /load_model and /predict endpoints let you swap or reload models with zero downtime." },
      { title: "Fits your VRAM", body: "Optional 4-bit loading runs 7B-class models on a single GPU with 16 GB or more." },
      { title: "Observability first", body: "Latency, throughput and GPU metrics export to Prometheus and the OhhO Fleet dashboards." },
    ],
    how: [
      { title: "Choose a model", body: "Set the model class and checkpoint via environment variables." },
      { title: "Launch the server", body: "One command starts the FastAPI inference server." },
      { title: "Call /predict", body: "Your robot sends camera + instruction and receives an action." },
      { title: "Watch it scale", body: "Metrics stream to Prometheus / Grafana; raise call limits as you grow." },
    ],
    specs: [
      { label: "API", value: "REST — /health, /load_model, /predict" },
      { label: "Backends", value: "OpenVLA, SmolVLA, ACT, diffusion, custom" },
      { label: "Quantization", value: "Optional 4-bit" },
      { label: "Hardware", value: "NVIDIA GPU, 16 GB+ VRAM" },
      { label: "Metrics", value: "Prometheus /metrics endpoint" },
      { label: "Deploy", value: "Docker, single command" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "500 API calls / day", included: true },
      { plan: "Fleet", level: "10K API calls / day", included: true },
      { plan: "Forge", level: "On-prem license, unlimited", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Builder's 500 calls/day is for prototyping. If real robots are calling the model in production, choose Fleet for 10K/day — or Forge for an on-prem license with no metering.",
    faq: [
      { q: "Can I run my own fine-tuned model?", a: "Yes. Serve loads any compatible checkpoint, including models you've fine-tuned with OhhO Data and the OmniVLA engine." },
      { q: "Where does inference run?", a: "On your GPU — desktop, server or cloud. Serve is software; you keep the model and the data." },
    ],
    related: ["data", "view", "pilot"],
    dashboardCaption:
      "OhhO Serve — endpoint console with live latency, throughput and GPU utilization.",
  },
  {
    slug: "view",
    name: "OhhO View",
    tag: "Four cameras. One smart view.",
    desc: "Fuse base-mounted cameras into a single calibrated bird's-eye-view your robot and your operators can both rely on. CPU-only, ROS 2-ready.",
    accent: "violet",
    category: "Intelligence",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z" />
        <circle cx="12" cy="12" r="3" />
      </svg>
    ),
    hero: "Four cameras, one smart view. Fuse base-mounted cameras into a single calibrated bird's-eye-view your robot and your operators can both rely on — CPU-only, ROS 2-ready.",
    highlights: [
      "4-camera surround BEV",
      "Geometric IPM, true top-down",
      "CPU-only — no GPU needed",
      "Calibration UI included",
      "ROS 2 topic out of the box",
    ],
    overview: [
      "A robot that can only see straight ahead is a robot that bumps into things. OhhO View stitches up to four base-mounted cameras into one top-down, surround bird's-eye-view (BEV) of everything around the robot.",
      "View computes geometric inverse-perspective-mapping from your robot's camera poses, producing a true fused top-down image — not a 2×2 tile. A calibration UI dials it in, and it runs on the CPU, so it doesn't compete with your GPU for VLA inference.",
      "The BEV feeds both humans and models: operators get instant situational awareness, and policies like SmolVLA consume the same surround view as an input — closing the loop between perception and action.",
    ],
    features: [
      { title: "True fused BEV", body: "Geometric inverse-perspective-mapping from URDF camera poses produces a single top-down image, not a tiled mosaic." },
      { title: "Calibration UI", body: "A bring-up wizard aligns the cameras; a calibration file takes priority when you want pixel-perfect." },
      { title: "CPU-only", body: "Runs on the robot's compute without touching the GPU your models need." },
      { title: "Model-ready output", body: "Publishes a standard ROS 2 image topic consumed by OhhO Serve policies and recorders alike." },
      { title: "Any camera layout", body: "Configure front / rear / left / right poses, field of view and mounting height for your specific robot." },
    ],
    how: [
      { title: "Mount & describe", body: "Add four cameras and their poses — or import them from an OhhO Build design." },
      { title: "Calibrate", body: "Run the calibration UI to align the surround view." },
      { title: "Publish the BEV", body: "View streams a fused top-down image on a ROS 2 topic." },
      { title: "Feed perception & policy", body: "Operators and models consume the same view." },
    ],
    specs: [
      { label: "Cameras", value: "Up to 4 (front / rear / left / right)" },
      { label: "Method", value: "Geometric IPM from URDF poses" },
      { label: "Compute", value: "CPU-only" },
      { label: "Calibration", value: "UI + optional calibration file" },
      { label: "Output", value: "ROS 2 Image topic (BEV)" },
      { label: "Consumers", value: "OhhO Serve, OhhO Data, operators" },
    ],
    plans: [
      { plan: "Spark", level: "Open source, included", included: true },
      { plan: "Builder", level: "Included", included: true },
      { plan: "Fleet", level: "Included + multi-robot calibration", included: true },
      { plan: "Forge", level: "Included + custom rigs", included: true },
    ],
    recommendedPlan: "Spark",
    planRationale:
      "View is open source and included on every plan, including free. There's nothing to buy to get the surround view running — pick the plan you need for the rest of the stack.",
    faq: [
      { q: "Do I need a depth camera?", a: "No. View works with standard RGB cameras; depth is optional and used elsewhere in the stack." },
      { q: "Will it slow down my AI?", a: "No — View is CPU-only by design, leaving the GPU free for OhhO Serve." },
    ],
    related: ["build", "serve", "pilot"],
    dashboardCaption:
      "OhhO View — four raw camera feeds fused into one calibrated bird's-eye-view.",
  },
  {
    slug: "data",
    name: "OhhO Data",
    tag: "Collect. Label. Ship.",
    desc: "An end-to-end pipeline for robot demonstration data. Teleop recording, an episode viewer and CLI tools — in LeRobot-compatible format, training-ready.",
    accent: "cyan",
    category: "Intelligence",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <ellipse cx="12" cy="5" rx="9" ry="3" />
        <path d="M3 5v14a9 3 0 0 0 18 0V5" />
        <path d="M3 12a9 3 0 0 0 18 0" />
      </svg>
    ),
    hero: "Collect, label and ship robot demonstration data. An end-to-end pipeline in LeRobot-compatible format, with teleop recording, an episode viewer and CLI tools — the fuel your policies learn from.",
    highlights: [
      "Teleop episode recording",
      "LeRobot dataset format (Parquet + MP4)",
      "Multi-camera time sync",
      "Episode viewer & curation",
      "CLI tools, training-ready",
    ],
    overview: [
      "Robot intelligence is only as good as the demonstrations it learns from. OhhO Data is the pipeline that turns teleoperation sessions into clean, training-ready datasets.",
      "Record episodes from a leader arm and mobile base, synchronized with multi-camera video, and store them in the standard LeRobot dataset format — Parquet plus MP4. Review and curate with the episode viewer, then ship straight to training.",
      "Data is designed for the whole loop: the same schema your robot records is the schema your model trains on and the policy runs in production — so what you collect is exactly what you deploy.",
    ],
    features: [
      { title: "Synchronized recording", body: "Leader arm, base velocity and multiple camera streams aligned to a tight sync tolerance, frame by frame." },
      { title: "Standard format", body: "LeRobot-compatible Hugging Face datasets (Parquet + MP4) — no bespoke converters." },
      { title: "Episode viewer", body: "Scrub, inspect and keep-or-discard episodes before they pollute a training run." },
      { title: "One schema, end to end", body: "A 9-DOF mobile-manipulation state/action that matches the recorder, the trainer and the policy." },
      { title: "CLI-first", body: "Scriptable record / inspect / push commands that fit into a data-ops workflow." },
    ],
    how: [
      { title: "Teleoperate", body: "Drive the robot — or the simulator — with a leader arm and joystick while Data records." },
      { title: "Review", body: "Open the episode viewer and discard the bad takes." },
      { title: "Ship", body: "Push the dataset in LeRobot format to training." },
      { title: "Close the loop", body: "Fine-tune with the OmniVLA engine and deploy via OhhO Serve." },
    ],
    specs: [
      { label: "Format", value: "LeRobot HF dataset (Parquet + MP4)" },
      { label: "State / action", value: "9-DOF (arm ×6 + base ×3)" },
      { label: "Cameras", value: "Front + wrist + BEV, time-synced" },
      { label: "Sync tolerance", value: "~50 ms" },
      { label: "Tools", value: "Recorder node, episode viewer, CLI" },
      { label: "Targets", value: "Real robot, Gazebo, Isaac Sim" },
    ],
    plans: [
      { plan: "Spark", level: "Local recording", included: true },
      { plan: "Builder", level: "Cloud sync, 1K episodes", included: true },
      { plan: "Fleet", level: "Unlimited + annotation", included: true },
      { plan: "Forge", level: "Unlimited + managed pipeline", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Builder adds cloud sync and 1,000 episodes — enough to train a first real policy. Teams collecting at scale or labeling heavily should move to Fleet for unlimited episodes and annotation.",
    faq: [
      { q: "Is my data portable?", a: "Completely. It's standard LeRobot format — train with OhhO's engine or any compatible toolchain." },
      { q: "Can I collect in simulation?", a: "Yes. Data records from Gazebo and Isaac Sim with the same schema as the real robot." },
    ],
    related: ["serve", "pilot", "proof"],
    dashboardCaption:
      "OhhO Data — dataset table, episode timeline and per-frame camera + label inspection.",
  },

  // ── OPERATIONS ──────────────────────────────────────────────────────────────
  {
    slug: "pilot",
    name: "OhhO Pilot",
    tag: "Operate any robot. From anywhere.",
    desc: "VR and mobile teleoperation in real time. Pre-loaded robot profiles, a custom robot builder, and arm control via hand tracking.",
    accent: "violet",
    category: "Operations",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <rect x="2" y="6" width="20" height="12" rx="3" />
        <path d="M12 10v4" />
        <path d="M10 12h4" />
        <circle cx="17" cy="12" r="1" fill="currentColor" stroke="none" />
        <circle cx="19.5" cy="10" r="1" fill="currentColor" stroke="none" />
      </svg>
    ),
    hero: "Operate any robot, from anywhere. VR and mobile teleoperation with pre-loaded robot profiles, a custom robot builder, and arm control via hand tracking — real-time and low-latency.",
    highlights: [
      "VR + mobile teleop",
      "Hand-tracking arm IK",
      "Pre-loaded robot profiles",
      "Live camera view (MJPEG)",
      "Emergency stop + safe clamps",
    ],
    overview: [
      "Sometimes the robot needs a human. OhhO Pilot is the teleoperation cockpit — drive a mobile base, command an arm, and see what the robot sees, from a phone or a VR headset, anywhere in the world.",
      "Pilot ships with pre-loaded profiles for common robots and a custom builder for your own. In VR, arm inverse-kinematics is driven by natural hand tracking; on mobile, an on-screen joystick and live camera view keep you in control.",
      "It connects over the same standard ROSBridge WebSocket your stack already speaks — with safe velocity clamping, an emergency stop, and a control-mode mux so teleop, navigation and AI never fight over the wheels.",
    ],
    features: [
      { title: "VR cockpit", body: "Drive and manipulate in immersive VR; arm IK follows your hands." },
      { title: "Mobile app", body: "On-screen joystick, live video and mission controls in your pocket." },
      { title: "Robot profiles", body: "Start from pre-loaded profiles or build your own to match your hardware." },
      { title: "Safe by construction", body: "Velocity clamps, an emergency stop and a control-mode mux prevent conflicting commands." },
      { title: "Standard transport", body: "Connects via ROSBridge WebSocket — no custom firmware required." },
    ],
    how: [
      { title: "Connect", body: "Point Pilot at your robot's ROSBridge endpoint." },
      { title: "Pick a profile", body: "Load a robot profile or build one for your hardware." },
      { title: "Take control", body: "Drive and manipulate from mobile or VR with live video." },
      { title: "Hand off to AI", body: "Switch control modes between teleop, navigation and AI policies." },
    ],
    specs: [
      { label: "Platforms", value: "VR headset + mobile (Android)" },
      { label: "Transport", value: "ROSBridge WebSocket (port 9090)" },
      { label: "Arm control", value: "Hand-tracking IK (VR), joint UI (mobile)" },
      { label: "Video", value: "MJPEG via web_video_server" },
      { label: "Safety", value: "Velocity clamps, e-stop, control-mode mux" },
      { label: "Robots", value: "Pre-loaded profiles + custom builder" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "Mobile, up to 3 robots", included: true },
      { plan: "Fleet", level: "VR + mobile, unlimited", included: true },
      { plan: "Forge", level: "White-label Pilot", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Casual mobile teleop of a few robots fits Builder. For VR, unlimited robots, or to put Pilot in front of your own operators, choose Fleet — or Forge to white-label it.",
    faq: [
      { q: "Do I need a VR headset?", a: "No. Pilot works fully on mobile; VR is an option for immersive arm control." },
      { q: "Is it safe over the internet?", a: "Pilot clamps velocities and offers an emergency stop; pair it with OhhO Shield for authenticated, encrypted links." },
    ],
    related: ["view", "fleet", "shield"],
    dashboardCaption:
      "OhhO Pilot — operator HUD with live robot view, hand-tracking arm IK and a latency readout.",
  },
  {
    slug: "fleet",
    name: "OhhO Fleet",
    tag: "Update 50 robots like you update an app.",
    desc: "Fleet management, signed over-the-air updates and a full observability stack — Prometheus, Grafana and alerting, pre-configured.",
    accent: "cyan",
    category: "Operations",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <rect x="16" y="16" width="6" height="6" rx="1" />
        <rect x="2" y="16" width="6" height="6" rx="1" />
        <rect x="9" y="2" width="6" height="6" rx="1" />
        <path d="M5 16v-3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3" />
        <path d="M12 12V8" />
      </svg>
    ),
    hero: "Update 50 robots like you update an app. Fleet management, signed over-the-air updates and a full observability stack — Prometheus, Grafana and alerting, pre-configured.",
    highlights: [
      "Fleet health dashboard",
      "Signed OTA updates (code + models)",
      "Staged / canary rollouts",
      "Prometheus + Grafana + alerts",
      "Per-robot telemetry",
    ],
    overview: [
      "One robot is a project; fifty is an operation. OhhO Fleet is mission control for a whole fleet — see every robot's health, push software and AI-model updates over the air, and get paged before a problem becomes an outage.",
      "Fleet ships the observability stack already wired: Prometheus scraping, Grafana dashboards, Loki, Tempo and AlertManager. Robot telemetry, GPU metrics and VLA latency all land in one place.",
      "Over-the-air updates cover both the ROS 2 workspace and ONNX policy models, with staged rollouts so you can canary a release to a few robots before it reaches the rest.",
    ],
    features: [
      { title: "Single pane of glass", body: "Every robot's status, version, battery and last-seen in one dashboard." },
      { title: "OTA for code and models", body: "Update the ROS 2 workspace and ONNX policies remotely — no field visits." },
      { title: "Staged rollouts", body: "Canary to a subset, watch the metrics, then roll forward or back." },
      { title: "Observability included", body: "Prometheus, Grafana, Loki, Tempo and AlertManager, pre-provisioned with dashboards." },
      { title: "Alerting that pages", body: "Rules for offline robots, latency spikes and resource exhaustion route to email or Slack." },
    ],
    how: [
      { title: "Enroll robots", body: "Each robot reports telemetry to the Fleet stack." },
      { title: "Watch the fleet", body: "Health, versions and metrics on pre-built dashboards." },
      { title: "Roll out updates", body: "Push signed OTA bundles; canary first." },
      { title: "Get alerted", body: "AlertManager pages you before users notice." },
    ],
    specs: [
      { label: "Metrics", value: "Prometheus (Pi, GPU, VLA, DCGM)" },
      { label: "Dashboards", value: "Grafana (auto-provisioned)" },
      { label: "Logs / traces", value: "Loki + Tempo" },
      { label: "Alerting", value: "AlertManager → email / Slack" },
      { label: "OTA", value: "Workspace + ONNX models, signed" },
      { label: "Rollouts", value: "Staged / canary" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "Not included", included: false },
      { plan: "Fleet", level: "Up to 100 robots, OTA", included: true },
      { plan: "Forge", level: "Unlimited robots, on-prem", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Fleet is the plan named for this product: up to 100 robots with OTA and the full observability stack. Above 100 robots, or to run it all on-prem, choose Forge.",
    faq: [
      { q: "Can I host the observability stack myself?", a: "Yes. It deploys via Docker Compose on your own infrastructure; Forge adds on-prem licensing and SSO." },
      { q: "Are OTA updates safe?", a: "Updates are signed and staged; pair with OhhO Shield for end-to-end supply-chain integrity." },
    ],
    related: ["serve", "shield", "proof"],
    dashboardCaption:
      "OhhO Fleet — fleet map, health donut, OTA rollout progress and the live alert feed.",
  },

  // ── TRUST (compliance / security / testing) ─────────────────────────────────
  {
    slug: "comply",
    name: "OhhO Comply",
    tag: "Ship robots the regulators will pass.",
    desc: "Turn robot safety standards into a guided checklist — CE, ISO 10218, ISO 13849, UL — and auto-generate the technical file and audit trail to prove conformity.",
    accent: "violet",
    category: "Trust",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
        <path d="M9 2h6a1 1 0 0 1 1 1v1a1 1 0 0 1-1 1H9a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z" />
        <path d="m9 14 2 2 4-4" />
      </svg>
    ),
    hero: "Ship robots the regulators will pass. OhhO Comply turns robot safety standards into a guided checklist — CE, ISO 10218, ISO 13849, UL — and generates the technical file and audit trail to prove conformity.",
    highlights: [
      "Standards mapped to your robot",
      "Guided requirement checklists",
      "Auto-generated technical file & DoC",
      "Immutable audit trail",
      "Risk-assessment templates",
    ],
    overview: [
      "A robot that can't be certified can't be sold. Compliance is where most hardware projects stall — a maze of directives, harmonized standards and documentation that nobody on the team trained for. OhhO Comply makes it a workflow.",
      "Comply maps your robot — pulled straight from its OhhO Build design and deployment profile — to the standards that actually apply: the EU Machinery Regulation and CE marking, ISO 10218 / ISO 15066 for industrial and collaborative robots, ISO 13849 functional safety, and regional electrical standards like UL and IEC 60204.",
      "For each requirement it tracks evidence, owners and status, generates the technical construction file and Declaration of Conformity, and keeps an immutable audit trail — so when an auditor or a customer asks, the proof is one click away.",
    ],
    features: [
      { title: "Applicability engine", body: "Comply reads your robot's design and use case and tells you which directives and standards actually apply — no guessing." },
      { title: "Guided checklists", body: "Each standard becomes a tracked list of requirements, each with evidence, an owner and a status." },
      { title: "Risk assessment", body: "Built-in templates for ISO 12100 hazard analysis and ISO 13849 performance-level determination." },
      { title: "Document generation", body: "Auto-assemble the technical construction file, risk assessment and Declaration of Conformity." },
      { title: "Audit-ready trail", body: "Every change is logged and timestamped, producing a defensible compliance history." },
      { title: "Stays in sync", body: "Change a part in OhhO Build and Comply flags the requirements that need re-review." },
    ],
    how: [
      { title: "Import your robot", body: "Comply pulls the design, sensors and use case from OhhO Build." },
      { title: "See what applies", body: "It maps the applicable directives and standards automatically." },
      { title: "Work the checklist", body: "Assign owners, attach evidence, run the risk assessment." },
      { title: "Generate the file", body: "Export the technical file, Declaration of Conformity and audit trail for certification." },
    ],
    specs: [
      { label: "Standards", value: "EU Machinery Reg, ISO 10218, ISO 15066, ISO 13849, ISO 12100, UL / IEC 60204" },
      { label: "Risk assessment", value: "ISO 12100 + ISO 13849 PL templates" },
      { label: "Outputs", value: "Technical file, risk assessment, Declaration of Conformity" },
      { label: "Audit trail", value: "Immutable, timestamped" },
      { label: "Sync", value: "Driven by OhhO Build design changes" },
      { label: "Regions", value: "EU (CE), North America (UL), extensible" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "Self-assessment checklists", included: true },
      { plan: "Fleet", level: "Full standards + document generation", included: true },
      { plan: "Forge", level: "Custom standards + certification partner", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "If you're heading toward a real certification, choose Fleet for the full standards library and document generation. Enterprises pursuing formal certification with a notified body should choose Forge for custom standards and partner support.",
    faq: [
      { q: "Does Comply certify my robot?", a: "Comply prepares everything a certification needs — the technical file, risk assessment and evidence — and on Forge connects you with certification partners. The certificate itself is issued by an accredited body, not OhhO." },
      { q: "Which standards are covered?", a: "The common machinery and robot-safety standards (CE / Machinery Regulation, ISO 10218, ISO 13849, UL), with custom standards available on Forge." },
    ],
    related: ["shield", "proof", "build"],
    dashboardCaption:
      "OhhO Comply — standards coverage, certification progress rings and the document / audit status board.",
  },
  {
    slug: "shield",
    name: "OhhO Shield",
    tag: "Security for robots that touch the real world.",
    desc: "Give every robot a hardware identity, encrypt its links, sign its updates, and watch its software bill of materials for vulnerabilities — across the whole fleet.",
    accent: "cyan",
    category: "Trust",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" />
        <path d="m9 12 2 2 4-4" />
      </svg>
    ),
    hero: "Security for robots that touch the real world. OhhO Shield gives every robot a hardware identity, encrypts its links, signs its updates, and watches its software bill of materials for vulnerabilities.",
    highlights: [
      "Hardware-rooted device identity",
      "Mutually-authenticated encrypted links",
      "Signed OTA / secure boot",
      "SBOM + CVE monitoring",
      "Fleet-wide risk posture",
    ],
    overview: [
      "A robot is a computer with wheels and an arm — and an attack surface to match. A compromised robot isn't a data breach; it's a physical-safety incident. OhhO Shield is the security layer for the whole fleet.",
      "Shield gives each robot a cryptographic identity rooted in hardware, establishes mutually-authenticated, encrypted channels for teleop and telemetry, and signs every over-the-air update so a robot only ever runs code you approved.",
      "It continuously inventories every robot's software bill of materials (SBOM), matches it against known CVEs, and surfaces a single risk posture across the fleet — turning security from a one-time audit into a live signal.",
    ],
    features: [
      { title: "Device identity", body: "Each robot gets a hardware-rooted key and certificate — no shared passwords, no anonymous nodes." },
      { title: "Encrypted by default", body: "Teleop, telemetry and ROS traffic run over mutually-authenticated, encrypted channels." },
      { title: "Trusted boot & updates", body: "Secure boot and signed OTA ensure a robot only runs code with a valid signature." },
      { title: "SBOM & CVE watch", body: "An automatic software bill-of-materials per robot, continuously checked against vulnerability feeds." },
      { title: "Zero-trust access", body: "Role-based, audited access to robots and the fleet console, with SSO on enterprise plans." },
      { title: "One risk posture", body: "A live security score per robot and across the fleet, integrated into OhhO Fleet." },
    ],
    how: [
      { title: "Enroll identity", body: "Each robot provisions a hardware-rooted key on first boot." },
      { title: "Encrypt the links", body: "Shield establishes authenticated channels for all traffic." },
      { title: "Sign the supply chain", body: "OTA bundles are signed; robots verify before applying." },
      { title: "Monitor continuously", body: "SBOM + CVE scanning feeds a live posture and alerts." },
    ],
    specs: [
      { label: "Identity", value: "Hardware-rooted keys + X.509 certs" },
      { label: "Transport", value: "Mutually-authenticated TLS / encrypted DDS" },
      { label: "Integrity", value: "Secure boot + signed OTA" },
      { label: "SBOM", value: "Per-robot, CVE-matched" },
      { label: "Access", value: "RBAC + audit log (SSO on Forge)" },
      { label: "Integrates", value: "OhhO Fleet, OhhO Pilot" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "Encrypted links + signed updates", included: true },
      { plan: "Fleet", level: "+ device identity, SBOM / CVE monitoring", included: true },
      { plan: "Forge", level: "+ secure boot, SSO, on-prem", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Any robot reachable over a network should at least be on Builder for encrypted links and signed updates. Fleets in production want Fleet for device identity and CVE monitoring; regulated or enterprise deployments choose Forge for secure boot and SSO.",
    faq: [
      { q: "Does Shield slow the robot down?", a: "Encryption and verification are designed for embedded targets; the security overhead is negligible next to perception and control." },
      { q: "Does it work with my existing ROS 2 stack?", a: "Yes. Shield layers onto standard ROS 2 / DDS and the ROSBridge transport OhhO Pilot uses." },
    ],
    related: ["fleet", "pilot", "comply"],
    dashboardCaption:
      "OhhO Shield — fleet risk score, device-identity roster and the live SBOM / CVE table.",
  },
  {
    slug: "proof",
    name: "OhhO Proof",
    tag: "Prove the robot is safe before it ships.",
    desc: "Run your robot through thousands of simulated scenarios, track regression and coverage, and assemble the evidence into a versioned safety case.",
    accent: "violet",
    category: "Trust",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M14 2v6a2 2 0 0 0 .245.96l5.51 10.08A2 2 0 0 1 18 22H6a2 2 0 0 1-1.755-2.96l5.51-10.08A2 2 0 0 0 10 8V2" />
        <path d="M6.453 15h11.094" />
        <path d="M8.5 2h7" />
      </svg>
    ),
    hero: "Prove the robot is safe before it ships. OhhO Proof runs your robot through thousands of simulated scenarios, tracks regression and coverage, and assembles the evidence into a safety case.",
    highlights: [
      "Scenario-based simulation testing",
      "Regression vs known-good builds",
      "Coverage map of tested conditions",
      "Failure / fault injection",
      "Versioned safety-case report",
    ],
    overview: [
      "You wouldn't ship software without tests; a robot deserves more. OhhO Proof is the testing and validation product — it puts your robot through scenario-based trials in simulation and on hardware, and tells you, with evidence, whether it's ready.",
      "Proof runs large batches of randomized scenarios — navigation, manipulation, edge cases and failure injection — across Gazebo and Isaac Sim, scoring each run against safety and task criteria. A coverage map shows what you've actually tested, not what you hope you have.",
      "Every release is checked for regressions against the last known-good build, and the results roll up into a versioned safety case you can hand to QA, to OhhO Comply for certification, or to a customer who needs assurance.",
    ],
    features: [
      { title: "Scenario suites", body: "Thousands of randomized navigation and manipulation scenarios across Gazebo and Isaac Sim." },
      { title: "Pass / fail criteria", body: "Score each run against task success, collisions, safety-zone and timing criteria." },
      { title: "Regression tracking", body: "Every build is compared to the last known-good; regressions are flagged before release." },
      { title: "Coverage map", body: "See which conditions — speeds, payloads, lighting, layouts — you've actually exercised." },
      { title: "Fault injection", body: "Inject sensor dropouts, latency and actuator faults to test failure handling." },
      { title: "Safety case", body: "Results assemble into a versioned report that feeds OhhO Comply and your QA sign-off." },
    ],
    how: [
      { title: "Define scenarios", body: "Pick suites or describe the conditions your robot must handle." },
      { title: "Run at scale", body: "Proof executes batches across simulators in parallel." },
      { title: "Read the coverage", body: "See pass rates, regressions and the coverage map." },
      { title: "Export the evidence", body: "Generate a safety-case report for QA and Comply." },
    ],
    specs: [
      { label: "Simulators", value: "Gazebo Harmonic + Isaac Sim" },
      { label: "Scenarios", value: "Navigation, manipulation, edge cases, faults" },
      { label: "Scoring", value: "Task success, collisions, safety zones, timing" },
      { label: "Regression", value: "Versus last known-good build" },
      { label: "Coverage", value: "Condition-space coverage map" },
      { label: "Output", value: "Versioned safety-case report" },
    ],
    plans: [
      { plan: "Spark", level: "Single-scenario sim tests", included: true },
      { plan: "Builder", level: "Scenario suites + regression", included: true },
      { plan: "Fleet", level: "Large-batch + coverage + fault injection", included: true },
      { plan: "Forge", level: "Custom scenarios + safety-case sign-off", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Hobby projects can validate single scenarios on Spark. Teams shipping to real users want Fleet for large-batch testing, coverage and fault injection; safety-critical programs choose Forge for custom scenarios and formal sign-off.",
    faq: [
      { q: "Do I need hardware to test?", a: "No. Proof runs primarily in simulation, with optional hardware-in-the-loop runs for final validation." },
      { q: "How does it relate to Comply?", a: "Proof produces the test evidence; Comply files it as part of the certification record." },
    ],
    related: ["comply", "data", "fleet"],
    dashboardCaption:
      "OhhO Proof — suite pass / fail, the scenario-coverage heatmap and the sim-to-real regression trend.",
  },
];

// ── Lookups & helpers ─────────────────────────────────────────────────────────

export const PRODUCT_SLUGS: string[] = PRODUCTS.map((p) => p.slug);

export function getProduct(slug: string): Product | undefined {
  return PRODUCTS.find((p) => p.slug === slug);
}

export function relatedProducts(slug: string): Product[] {
  const p = getProduct(slug);
  if (!p) return [];
  return p.related
    .map((s) => getProduct(s))
    .filter((x): x is Product => Boolean(x));
}

/** CSS color for an accent (matches globals.css tokens). */
export function accentColor(accent: Accent): string {
  return accent === "cyan" ? "var(--cyan)" : "var(--violet-lite)";
}
