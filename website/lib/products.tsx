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
  /**
   * Live, shipped app for this product (if any). When set, the detail page
   * surfaces a "launch" CTA and the dashboard mockup becomes a link into it.
   */
  app?: { href: string; label: string };
  /**
   * Requires a VR/MR headset. When true, the product is surfaced inside the
   * OhhO VR console (vr_app) — the headset shows only `vr` products. Today
   * that's just Pilot (teleoperation); more VR products will set this later.
   * Consumed by lib/vr/manifest.ts → public/vr/manifest.json.
   */
  vr?: boolean;
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
    app: { href: "/build", label: "Launch the studio" },
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
      "Gazebo & Isaac simulation (USD + SDF)",
      "MCAP recording format (ROS 2 bags)",
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
      { title: "Simulate before you build", body: "Gazebo Harmonic and Isaac Sim worlds wired to the same topics as the real robot, so you develop with no hardware. Robot description uses URDF and SDF; Isaac Sim scenes use USD (Universal Scene Description) — the same 3D standard Pixar, Omniverse and the digital twin industry settled on." },
      { title: "MCAP recording", body: "Frame records ROS 2 data in MCAP — the open-source, ROS 2-native bag format — so your logs interoperate with the wider ROS 2 ecosystem's tooling, not a proprietary format." },
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
      { label: "Scene formats", value: "URDF, SDF, USD (Isaac Sim / Omniverse)" },
      { label: "Recording", value: "MCAP (ROS 2-native bag format)" },
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
    related: ["build", "bench", "fleet", "connect"],
    app: { href: "/frame", label: "Open device console" },
    dashboardCaption:
      "OhhO Frame — workspace scaffold, containerized build and the node graph that ships ready to run.",
  },
  {
    slug: "bench",
    name: "OhhO Bench",
    tag: "From a box of parts to a robot that powers on.",
    desc: "Guided assembly, wiring and firmware bring-up. Turn an OhhO Build bill of materials into a wired, flashed and calibrated robot — with step-by-step instructions and hardware self-tests.",
    accent: "cyan",
    category: "Foundation",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M14.7 6.3a4 4 0 0 0-5.4 5.4L3 18l3 3 6.3-6.3a4 4 0 0 0 5.4-5.4l-2.6 2.6-2.4-.6-.6-2.4z" />
        <circle cx="6.5" cy="17.5" r=".9" fill="currentColor" stroke="none" />
      </svg>
    ),
    hero: "Design is the easy half. OhhO Bench takes the bill of materials you exported from Build and walks you all the way to a robot that powers on — guided mechanical assembly, a wiring map, firmware flashing, and hardware self-tests that prove every motor, sensor and servo actually works.",
    highlights: [
      "Step-by-step assembly from your BOM",
      "Wiring & port map (serial / power / bus)",
      "One-click firmware flashing",
      "Hardware self-test & diagnostics",
      "Sensor, camera and arm calibration",
    ],
    overview: [
      "Between a sourced bill of materials and a working software stack lies the part nobody writes documentation for: bolting the robot together, wiring the motor board, flashing firmware, and discovering — usually the hard way — which connector is in backwards. OhhO Bench turns that into a guided, checked workflow.",
      "Bench reads your OhhO Build design and generates the exact assembly order, a wiring and port map (which controller talks over which serial port at which baud rate), and a firmware bring-up sequence. As you go, it runs hardware self-tests — spin each motor, read the encoders and IMU, sweep every arm servo — so a mistake is caught at the bench, not in the field.",
      "When the mechanics are sound, Bench runs the calibration routines: motor direction and odometry geometry, IMU bias, camera intrinsics and the surround-view rig, and arm joint homing. It writes the deployment profile the rest of the stack consumes — so the moment Bench turns green, OhhO Frame, View and Autonomy come up on real, calibrated hardware.",
    ],
    features: [
      { title: "Assembly from your design", body: "Bench expands your Build BOM into an ordered, illustrated assembly sequence — what bolts to what, in what order, with torque and orientation called out." },
      { title: "Wiring & port map", body: "A generated harness diagram: motor board, arm bus, cameras and compute, with the serial ports, baud rates and power budget each one needs." },
      { title: "Firmware flashing", body: "Flash the motor-controller and microcontroller firmware from the browser, with the right protocol and configuration for your base — no hand-edited config." },
      { title: "Hardware self-test", body: "Spin each wheel, read encoders and IMU, and sweep every arm joint to confirm wiring and direction before any autonomy runs." },
      { title: "Guided calibration", body: "Walk through odometry geometry, IMU bias, camera intrinsics, the surround-view rig and arm homing — and write them into the deployment profile." },
      { title: "Hands off to the stack", body: "A green Bench produces the deployment profile and calibration files that OhhO Frame, View and Autonomy pick up with zero re-entry." },
    ],
    how: [
      { title: "Import the build", body: "Bench pulls the BOM, wiring and component specs from your OhhO Build design." },
      { title: "Assemble & wire", body: "Follow the ordered assembly steps and the generated wiring / port map." },
      { title: "Flash & self-test", body: "Flash firmware and run the hardware self-tests until every subsystem reports healthy." },
      { title: "Calibrate & hand off", body: "Run the calibration routines; Bench writes the deployment profile for the rest of the stack." },
    ],
    specs: [
      { label: "Input", value: "OhhO Build BOM + wiring + component specs" },
      { label: "Firmware", value: "Motor board + MCU, protocol-aware flashing" },
      { label: "Self-test", value: "Motors, encoders, IMU, arm servos, cameras" },
      { label: "Calibration", value: "Odometry, IMU bias, camera intrinsics, BEV rig, arm homing" },
      { label: "Output", value: "Deployment profile + calibration files" },
      { label: "Handoff", value: "OhhO Frame, View, Autonomy" },
    ],
    plans: [
      { plan: "Spark", level: "Assembly guide + self-test", included: true },
      { plan: "Builder", level: "+ firmware flashing & calibration", included: true },
      { plan: "Fleet", level: "+ batch bring-up across many units", included: true },
      { plan: "Forge", level: "Contract-manufacturing handoff pack", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Anyone building their first unit wants Builder for firmware flashing and the guided calibration routines. Teams bringing up many identical robots move to Fleet for batch bring-up, and manufacturing partners use Forge for a handoff pack.",
    faq: [
      { q: "Do I have to use OhhO Build?", a: "It's smoothest end-to-end, but Bench also works from a manually-entered parts list — you just fill in the wiring and ports it would otherwise infer." },
      { q: "Does Bench need the real hardware?", a: "Yes — Bench is the step where software meets metal. The self-tests and calibration run against the physical robot over its serial / USB buses." },
    ],
    related: ["build", "frame", "view"],
    dashboardCaption:
      "OhhO Bench — assembly checklist, wiring map and the hardware self-test board going green subsystem by subsystem.",
    app: { href: "/bench", label: "Open bring-up console" },
  },
  {
    slug: "connect",
    name: "OhhO Connect",
    tag: "One link. Any robot. Any transport.",
    desc: "A robot-agnostic connection layer that bridges your browser to any robot over Wi-Fi, USB, Bluetooth or a built-in simulator — so every OhhO console works with any hardware, no install required.",
    accent: "cyan",
    category: "Foundation",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M5 12.55a11 11 0 0 1 14 0" />
        <path d="M8.5 16.1a6 6 0 0 1 7 0" />
        <path d="M12 20h.01" />
        <circle cx="12" cy="20" r=".9" fill="currentColor" stroke="none" />
      </svg>
    ),
    hero: "One link to any robot. OhhO Connect is the transport abstraction that lets every OhhO console — Pilot, Autonomy, Fleet, Mind — talk to any robot over whatever link the hardware exposes: Wi-Fi via ROSBridge, USB via Web Serial, Bluetooth Low Energy, or a built-in simulator. No SDK install, no Python, no robot-specific setup.",
    highlights: [
      "Wi-Fi · ROSBridge WebSocket",
      "USB · Web Serial (firmware-direct)",
      "Bluetooth · BLE (Nordic UART)",
      "Built-in simulator (no hardware)",
      "Robot-agnostic — one console, any robot",
    ],
    overview: [
      "Every robot speaks a different language on a different wire. A DDS-native humanoid talks Cyclone, a mecanum base speaks a serial protocol over USB, a drone speaks MAVLink, an industrial arm speaks Modbus. OhhO Connect is the layer that makes all of them look the same to every OhhO product.",
      "Connect exposes a single transport interface — connect, send velocity, send joint commands, emergency stop, subscribe to telemetry — and implements it for each protocol. A console built against Connect works on a mecanum manipulator over Wi-Fi today and a humanoid over USB tomorrow, with zero code changes.",
      "Because Connect runs in the browser, there's nothing to install on the operator's machine. Web Serial and Web Bluetooth are feature-detected at runtime, and a deterministic simulator is always available — so you can explore every console before you ever wire up real hardware.",
    ],
    features: [
      { title: "Four transports, one API", body: "ROSBridge WebSocket (Wi-Fi), Web Serial (USB), Web Bluetooth (BLE), and a built-in simulator — all behind the same RobotTransport interface, feature-detected at runtime." },
      { title: "Robot-agnostic by design", body: "Connect speaks generic velocity and joint commands, not robot-specific SDK calls. A console built against Connect works on any robot with a transport implementation." },
      { title: "Browser-native, no install", body: "Web Serial and Web Bluetooth run in Chromium browsers over a secure context — no driver install, no Python SDK, no desktop app. Open a URL and drive." },
      { title: "Mixed-content aware", body: "Connect detects the HTTPS-to-ws mismatch and guides the operator to a secure rosbridge or a LAN connection, so the link just works instead of failing silently." },
      { title: "Persistent per-robot config", body: "Each robot in the garage remembers its last protocol, address and baud rate — so reconnecting is one click, not a setup wizard every time." },
      { title: "Simulator always on", body: "A deterministic in-browser robot streams telemetry on every protocol slot, so you can demo, develop and test consoles without hardware on the bench." },
    ],
    how: [
      { title: "Pick a robot", body: "Select a robot from your OhhO Garage — Connect loads its saved transport settings." },
      { title: "Choose a protocol", body: "Connect shows the protocols available in your browser and on your robot — Wi-Fi, USB, BLE or Sim." },
      { title: "Open the link", body: "Connect establishes the transport, runs capability checks and streams live telemetry to every console." },
      { title: "Operate", body: "Every OhhO console reads the live connection and never needs to know which protocol is underneath." },
    ],
    specs: [
      { label: "Transports", value: "ROSBridge (WS), Web Serial (USB), Web BLE, Simulator" },
      { label: "Interface", value: "RobotTransport — connect, velocity, joints, e-stop, telemetry" },
      { label: "Browser", value: "Chromium (Chrome / Edge) for Serial + BLE; any for WS" },
      { label: "Persistence", value: "Per-robot config saved to garage (Supabase + localStorage)" },
      { label: "Simulator", value: "Deterministic in-browser telemetry stream" },
      { label: "Integrates", value: "OhhO Pilot, Autonomy, Fleet, Mind, all consoles" },
    ],
    plans: [
      { plan: "Spark", level: "Included — Wi-Fi + simulator", included: true },
      { plan: "Builder", level: "Included + USB + BLE", included: true },
      { plan: "Fleet", level: "Included + multi-robot sessions", included: true },
      { plan: "Forge", level: "Included + custom transports", included: true },
    ],
    recommendedPlan: "Spark",
    planRationale:
      "Connect is foundational and included on every plan, including free. You only need a paid plan for the consoles that sit on top of Connect — Pilot, Autonomy, Fleet and Mind.",
    faq: [
      { q: "Do I need to install anything on my computer?", a: "No. Connect runs entirely in the browser. Web Serial and Web Bluetooth are built into Chromium browsers (Chrome, Edge) — no driver, no SDK, no desktop app." },
      { q: "Does Connect work with non-ROS robots?", a: "Yes. Web Serial talks the firmware protocol directly — no ROS needed. For DDS-native robots, pair Connect with OhhO Bridge to translate between DDS and ROS topics." },
      { q: "What if my browser doesn't support Web Serial?", a: "Connect feature-detects each protocol at runtime and shows only the ones your browser supports. Wi-Fi (ROSBridge) and the simulator work in any modern browser." },
    ],
    related: ["bridge", "frame", "pilot"],
    dashboardCaption:
      "OhhO Connect — protocol picker, live link status with latency, and the telemetry stream flowing to every console.",
    app: { href: "/garage", label: "Open the garage" },
  },
  {
    slug: "bridge",
    name: "OhhO Bridge",
    tag: "Connect any robot. Even the ones that don't speak ROS.",
    desc: "Protocol adapters that translate between any robot's native protocol and the OhhO platform. Bridge a DDS-native humanoid, a MAVLink drone, a CANopen mobile base, an OPC UA factory cell or a Modbus arm into standard ROS 2 topics — no fork, no rewrite.",
    accent: "violet",
    category: "Foundation",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M3 12h18" />
        <path d="M3 8v8" />
        <path d="M21 8v8" />
        <path d="M7 12v4" />
        <path d="M11 12v4" />
        <path d="M13 12v4" />
        <path d="M17 12v4" />
      </svg>
    ),
    hero: "OhhO Connect handles the transport; OhhO Bridge handles the language. Bridge is a library of protocol adapters that translate between a robot's native protocol — a DDS LowCmd/LowState interface, a MAVLink autopilot, a CANopen motor bus, an OPC UA factory cell, a Modbus PLC arm — and the standard ROS 2 topics every OhhO product already speaks. One adapter per protocol family, and any robot joins the platform.",
    highlights: [
      "DDS-native ↔ ROS 2 topics",
      "MAVLink ↔ ROS 2 topics",
      "CAN bus / CANopen (CiA 402) ↔ ROS 2",
      "OPC UA ↔ ROS 2 (Industry 4.0)",
      "PROFINET · EtherNet/IP ↔ ROS 2",
      "MQTT · VDA 5050 (AGV/AMR fleets)",
      "Modbus · EtherCAT (industrial arms)",
      "ROS-Industrial arm driver compatibility",
      "Joint-index maps per robot model",
      "Impedance-gain defaults included",
    ],
    overview: [
      "ROS 2 is the lingua franca of the OhhO platform — but most commercial robots don't speak it natively. Humanoids talk Cyclone DDS through a native SDK with custom LowCmd/LowState IDL. Drones speak MAVLink. Mobile bases and AGVs speak CANopen over a CAN bus. Factory cells speak OPC UA. Industrial arms speak Modbus, EtherCAT, PROFINET or EtherNet/IP. Warehouse fleets speak VDA 5050 over MQTT. OhhO Bridge is the layer that translates each one into the standard ROS 2 topics the rest of the platform expects.",
      "Each bridge is a thin ROS 2 node — or a browser-side codec for Web Serial — that subscribes to the robot's native protocol and republishes as standard topics: Twist on /cmd_vel, JointState on /joint_states, Imu on /imu/data, Odometry on /odom. In the other direction, it takes your ROS 2 commands and calls the robot's native protocol. For a DDS-native humanoid, that means mapping Twist to HighCmd velocity fields and JointState to LowCmd motor commands with sensible default impedance gains (kp/kd). For a CANopen base, it means SDO/PDO object dictionary translation. For an OPC UA cell, it means browsing the server's address space and mapping nodes to topics.",
      "Bridge is what makes 'any robot' literally true. Without it, OhhO's intelligence and operations products work on any ROS 2-compatible robot — which is a lot, but not everything. With Bridge, a DDS-native humanoid, a MAVLink survey drone, a CANopen AGV, an OPC UA-integrated factory arm and a Modbus-controlled SCARA all appear to the platform as standard ROS 2 robots, and every console works unchanged. And because Bridge speaks the industry standards the factory floor already runs on — PROFINET for German automotive, EtherNet/IP for North American manufacturing, VDA 5050 for warehouse fleets — a robot on OhhO plugs into the systems your facility already has, not the other way around.",
    ],
    features: [
      { title: "DDS-native humanoid adapter", body: "Translates a native DDS LowCmd/LowState and HighCmd/HighState interface to and from standard ROS 2 topics, with per-model joint-index maps for the humanoid family you're driving." },
      { title: "MAVLink drone adapter", body: "Bridges MAVLink heartbeat, attitude, global position and manual control to ROS 2 Imu, Odometry and Twist — so a drone appears in the platform like any other robot." },
      { title: "CAN bus / CANopen adapter", body: "Translates CANopen object dictionaries (CiA 402 motion profile, SDO/PDO) to ROS 2 JointState, Twist and Odometry — the standard protocol for mobile robot motor controllers, AGVs and embedded bases." },
      { title: "OPC UA adapter", body: "Browses an OPC UA server's address space and maps nodes to ROS 2 topics — so a robot integrates with Industry 4.0 factory cells, MES/SCADA systems and digital twin platforms that already speak OPC UA." },
      { title: "PROFINET & EtherNet/IP adapters", body: "Real-time industrial Ethernet bridges for the two dominant factory-network ecosystems — PROFINET for European/Siemens manufacturing, EtherNet/IP for North American/Rockwell manufacturing. A robot on OhhO speaks the network your plant already runs." },
      { title: "MQTT & VDA 5050 adapter", body: "Bridges the VDA 5050 AGV/AMR fleet standard over MQTT — so OhhO Fleet interoperates with warehouse management systems and master control software from Linde, Toyota, MiR, KION and the rest of the VDA 5050 ecosystem." },
      { title: "Industrial arm adapters", body: "Modbus TCP/RTU and EtherCAT bridges for PLC-driven arms, plus ROS-Industrial compatibility for the major industrial arm families — exposing joint state and joint commands as standard ROS 2 topics." },
      { title: "PLC integration (IEC 61131-3)", body: "Bridge maps ROS 2 topics to PLC-readable signals, so a robot exchanges data with controllers programmed in ladder logic, structured text or function blocks — the languages every factory PLC already speaks." },
      { title: "Impedance-gain defaults", body: "When translating ROS joint commands into a native DDS motor command, Bridge applies sensible default kp/kd profiles per joint — so position control works out of the box without per-servo tuning." },
      { title: "Browser-side codecs", body: "For Web Serial connections, Bridge ships browser-native protocol codecs for common serial bases — so Connect can talk firmware-direct with no onboard PC in the loop." },
      { title: "Community-extensible", body: "Each bridge is a standalone adapter module. New protocols are added as a new adapter — no platform fork, no core rewrite." },
    ],
    how: [
      { title: "Install the bridge", body: "Select the bridge for your robot's protocol — DDS, MAVLink, CANopen, OPC UA, PROFINET, EtherNet/IP, MQTT, VDA 5050, Modbus, serial, or a community adapter — and install it alongside OhhO Frame." },
      { title: "Map the joints / signals", body: "Bridge loads the joint-index map for a humanoid, the object dictionary for a CANopen base, or the OPC UA address space for a factory cell — and applies sensible defaults." },
      { title: "Run the node", body: "The bridge node connects to the robot's native protocol and starts republishing standard ROS 2 topics." },
      { title: "Use every console", body: "Pilot, Autonomy, Fleet, Mind and every other OhhO product now work on your robot unchanged — and your plant's existing systems keep speaking their own protocols." },
    ],
    specs: [
      { label: "Robot protocols", value: "DDS, MAVLink, CANopen, Modbus, EtherCAT" },
      { label: "Factory protocols", value: "OPC UA, PROFINET, EtherNet/IP, IEC 61131-3" },
      { label: "Fleet protocols", value: "MQTT, VDA 5050" },
      { label: "Industrial arms", value: "ROS-Industrial compatible, Modbus, EtherCAT" },
      { label: "Humanoid models", value: "Per-model joint maps, configurable for your robot" },
      { label: "ROS 2 topics", value: "/cmd_vel, /joint_states, /imu/data, /odom" },
      { label: "Gain profiles", value: "Default kp/kd per joint per model" },
      { label: "Runtime", value: "ROS 2 node (onboard PC) + browser codecs" },
      { label: "Integrates", value: "OhhO Connect, Frame, all consoles" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "1 brand adapter", included: true },
      { plan: "Fleet", level: "All brand adapters", included: true },
      { plan: "Forge", level: "Custom protocol adapters", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Most teams only need one bridge — the one for their robot. Builder includes a single brand adapter, which is enough to bring one robot family onto the platform. Teams running mixed fleets choose Fleet for all adapters; enterprises with proprietary protocols choose Forge for custom bridges.",
    faq: [
      { q: "Do I need Bridge if my robot already speaks ROS 2?", a: "No. If your robot publishes standard ROS 2 topics natively, Connect + Frame are enough. Bridge is for robots that speak a native non-ROS protocol — DDS, MAVLink, CANopen, OPC UA, PROFINET, EtherNet/IP, MQTT, VDA 5050, Modbus, and so on." },
      { q: "Which humanoid models are supported?", a: "The DDS bridge covers any humanoid with a published URDF and SDK header — Bridge builds the joint-index map from those, so a new model is one config away." },
      { q: "Can Bridge integrate with my factory's PLC and SCADA systems?", a: "Yes. The OPC UA adapter browses your server's address space and maps nodes to ROS 2 topics; the PROFINET and EtherNet/IP adapters speak the industrial Ethernet your plant already runs; and IEC 61131-3 mapping lets PLCs exchange signals with robots over standard topics." },
      { q: "Does Bridge support warehouse AGV/AMR fleet standards?", a: "Yes. The VDA 5050 adapter over MQTT lets OhhO Fleet interoperate with warehouse management systems and master control software that speak the VDA 5050 standard — so your robots integrate with the fleet infrastructure your warehouse already has." },
      { q: "Can I write my own bridge?", a: "Yes. Each bridge is a standalone adapter module. On Forge, the OhhO team builds and maintains custom bridges for proprietary protocols." },
    ],
    related: ["connect", "frame", "pilot"],
    app: { href: "/bridge", label: "Open bridge console" },
    dashboardCaption:
      "OhhO Bridge — protocol adapters for DDS, MAVLink, CANopen, OPC UA, PROFINET, EtherNet/IP, MQTT, VDA 5050 and Modbus, all translating to standard ROS 2 topics.",
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
      { title: "Fits your hardware", body: "Optional 4-bit quantization runs large VLA models on modest GPUs. Bring your own — we help you size it, or recommend hardware that fits your budget." },
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
      { label: "Hardware", value: "Your GPU (desktop, server or cloud) — we help you size it" },
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
    related: ["train", "data", "view"],
    dashboardCaption:
      "OhhO Serve — endpoint console with live latency, throughput and GPU utilization.",
    app: { href: "/serve", label: "Open the console" },
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
    app: { href: "/view", label: "Open perception viewer" },
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
      "MCAP recording (ROS 2 bags)",
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
      { title: "Standard format", body: "LeRobot-compatible Hugging Face datasets (Parquet + MP4) for imitation learning, plus MCAP — the ROS 2-native bag format — for raw ROS 2 topic recording. No bespoke converters, no lock-in." },
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
      { label: "Training format", value: "LeRobot HF dataset (Parquet + MP4)" },
      { label: "ROS 2 recording", value: "MCAP (ROS 2-native bag format)" },
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
    related: ["train", "serve", "pilot"],
    app: { href: "/data", label: "Open episode viewer" },
    dashboardCaption:
      "OhhO Data — dataset table, episode timeline and per-frame camera + label inspection.",
  },
  {
    slug: "train",
    name: "OhhO Train",
    tag: "Turn demonstrations into policies.",
    desc: "The training engine behind the stack. Fine-tune VLA, imitation and reinforcement-learning policies from your OhhO Data episodes or in simulation — then export straight to Serve and Fleet.",
    accent: "cyan",
    category: "Intelligence",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M3 17l5-5 4 4 7-8" />
        <path d="M16 4h5v5" />
        <circle cx="8" cy="12" r=".9" fill="currentColor" stroke="none" />
        <circle cx="12" cy="16" r=".9" fill="currentColor" stroke="none" />
      </svg>
    ),
    hero: "OhhO Data collects the demonstrations; OhhO Serve runs the model — Train is the engine in between. Fine-tune Vision-Language-Action, imitation-learning and reinforcement-learning policies from your own episodes or in simulation, track every run, and export a deployment-ready checkpoint.",
    highlights: [
      "Fine-tune VLA, ACT, diffusion & RL policies",
      "Trains from OhhO Data or in simulation",
      "Built on the open OmniVLA engine",
      "Experiment tracking & sweeps (W&B)",
      "Exports to Serve + ONNX for Fleet OTA",
    ],
    overview: [
      "A policy is only as good as the loop that produced it. OhhO Train is the standardized training engine for embodied AI — the productized OmniVLA engine — so you fine-tune real models without stitching together a different toolchain for every method.",
      "Train covers the methods that matter: behavior cloning and VLA fine-tuning (SmolVLA, ACT, diffusion, OpenVLA) on the LeRobot datasets you record with OhhO Data, plus reinforcement learning in Isaac Lab with domain randomization for sim-to-real. A continual-learning loop can re-train as new episodes and tasks arrive, with replay, multi-objective rewards and AI-judged self-evaluation.",
      "Every run is tracked — losses, success rate, evaluation — with Weights & Biases sweeps to find good hyperparameters. When a checkpoint passes verification, Train exports it in the format the rest of the stack expects: a checkpoint OhhO Serve loads directly, or an ONNX policy OhhO Fleet ships over the air.",
    ],
    features: [
      { title: "Many methods, one engine", body: "Behavior cloning, VLA fine-tuning (SmolVLA / ACT / diffusion / OpenVLA), offline RL and on-policy RL — selected by config, not a rewrite." },
      { title: "Trains from your data", body: "Point Train at a LeRobot dataset from OhhO Data, or generate experience in Gazebo and Isaac Sim with domain randomization for sim-to-real." },
      { title: "Continual learning", body: "A post-training loop re-trains as new episodes and tasks arrive, with prioritized replay and outcome-stratified episodic memory." },
      { title: "Rewards & self-evaluation", body: "Multi-objective rewards (task, safety, efficiency, smoothness) plus vision rewards and AI judges score behavior, not just loss." },
      { title: "Tracked & reproducible", body: "Losses, success rate and eval stream to Weights & Biases; Bayesian sweeps search hyperparameters for you." },
      { title: "Export-ready", body: "Verified checkpoints export to OhhO Serve and to ONNX for OhhO Fleet OTA, with hardware-aware execution providers baked in." },
    ],
    how: [
      { title: "Choose method & data", body: "Pick a policy type and point Train at an OhhO Data dataset — or a simulation task." },
      { title: "Train & track", body: "Launch the run; losses, success rate and eval stream to W&B in real time." },
      { title: "Verify", body: "Best-of-N evaluation with hard safety and reachability checks gates what's allowed to ship." },
      { title: "Export & deploy", body: "Push the checkpoint to OhhO Serve, or export ONNX for OhhO Fleet to roll out." },
    ],
    specs: [
      { label: "Methods", value: "BC, VLA fine-tune, ACT, diffusion, offline + online RL" },
      { label: "Data sources", value: "OhhO Data (LeRobot), Gazebo, Isaac Sim" },
      { label: "Foundation", value: "Open-source OmniVLA engine" },
      { label: "Tracking", value: "Weights & Biases + Bayesian sweeps" },
      { label: "Hardware", value: "Your GPU — we help you size it or recommend a rig" },
      { label: "Export", value: "Serve checkpoint + ONNX for Fleet OTA" },
    ],
    plans: [
      { plan: "Spark", level: "Local training, single run", included: true },
      { plan: "Builder", level: "Cloud training + experiment tracking", included: true },
      { plan: "Fleet", level: "+ sweeps & continual-learning loop", included: true },
      { plan: "Forge", level: "On-prem cluster + managed training", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Builder gives you cloud training with full experiment tracking — enough to fine-tune a first real policy. Teams running sweeps or standing up a continual-learning loop want Fleet; enterprises training on their own cluster choose Forge.",
    faq: [
      { q: "Is the training engine open?", a: "Yes. Train is built on the open-source OmniVLA engine, so your training code and checkpoints are portable — you're never locked in." },
      { q: "Do I need real-robot data to start?", a: "No. You can train entirely in simulation with domain randomization, then fine-tune on real OhhO Data episodes for sim-to-real transfer." },
    ],
    related: ["data", "serve", "proof", "market"],
    dashboardCaption:
      "OhhO Train — run config, the live loss / success-rate curves and the export-to-Serve step on a passing checkpoint.",
    app: { href: "/train", label: "Open training console" },
  },
  {
    slug: "autonomy",
    name: "OhhO Autonomy",
    tag: "Map it, navigate it, command it in plain language.",
    desc: "The robot's autonomy stack — SLAM mapping, Nav2 navigation and a mission planner — driven by a natural-language agent that turns 'tidy the kitchen' into a sequence of robot actions.",
    accent: "violet",
    category: "Intelligence",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <circle cx="5" cy="19" r="2" />
        <circle cx="19" cy="5" r="2" />
        <path d="M7 19h6a4 4 0 0 0 4-4V7" />
        <path d="M5 17V9a4 4 0 0 1 4-4h2" />
      </svg>
    ),
    hero: "Teleop is for when a human drives; Autonomy is for when the robot drives itself. It maps a space with SLAM, navigates it with Nav2, and runs a mission planner that sequences navigation and manipulation — all orchestrated by a natural-language agent that turns an instruction into a plan.",
    highlights: [
      "2-D & 3-D SLAM mapping",
      "Nav2 path planning + obstacle avoidance",
      "Mission planner (navigate → act sequences)",
      "Natural-language agent (LLM-backed, your choice of model)",
      "Safe control-mode mux: nav / AI / teleop",
    ],
    overview: [
      "Perception tells a robot what's around it; Autonomy decides what to do about it. OhhO Autonomy is the behavior layer that turns a powered-on robot into one that moves through the world and completes tasks on its own.",
      "It builds and localizes against a map with SLAM (2-D and 3-D), plans collision-free paths with Nav2 over a fused costmap, and fuses wheel odometry and IMU through an EKF for reliable pose. A mission planner sequences higher-level jobs — navigate to a named location, then run a manipulation policy — and a control-mode mux arbitrates cleanly between navigation, AI policies and human teleop so they never fight over the wheels.",
      "On top sits a natural-language agent, backed by the LLM of your choice, that turns 'take the red cup from the kitchen to the bench' into a structured mission: it knows your named locations, can describe what it sees, and asks for clarification when an instruction is ambiguous — then hands the mission to the planner to execute.",
    ],
    features: [
      { title: "SLAM mapping", body: "Build and localize against 2-D and 3-D maps; switch between mapping and localization modes against a saved map." },
      { title: "Nav2 navigation", body: "Collision-free path planning and obstacle avoidance over a fused costmap, tuned to the robot's real velocity and acceleration limits." },
      { title: "Robust localization", body: "An EKF fuses wheel odometry and IMU so pose stays trustworthy even when mecanum wheels slip." },
      { title: "Mission planner", body: "Sequence navigation and manipulation into a mission — 'go to the kitchen, then pick up the cup' — as a tracked state machine." },
      { title: "Natural-language agent", body: "An LLM-backed agent maps plain-language instructions to missions, with named locations, scene description and clarification when it's unsure." },
      { title: "Safe arbitration", body: "A control-mode mux switches between navigation, AI policies and teleop so commands never conflict — and a human can always take over." },
    ],
    how: [
      { title: "Map the space", body: "Drive once while SLAM builds a map, then save it for localization." },
      { title: "Name the places", body: "Tag locations — kitchen, dock, bench — the planner and agent can refer to." },
      { title: "Give an instruction", body: "Type or speak a task; the agent turns it into a structured mission." },
      { title: "Watch it execute", body: "Nav2 and the policies run the mission; the mux keeps teleop override one tap away." },
    ],
    specs: [
      { label: "Mapping", value: "2-D & 3-D SLAM (mapping / localization)" },
      { label: "Navigation", value: "Nav2 planner + costmap obstacle avoidance" },
      { label: "Localization", value: "EKF fusing wheel odometry + IMU" },
      { label: "Missions", value: "Navigate → manipulate state machine" },
      { label: "Agent", value: "Natural language → mission (LLM-backed)" },
      { label: "Arbitration", value: "Control-mode mux: nav / AI / teleop" },
    ],
    plans: [
      { plan: "Spark", level: "SLAM + navigation in simulation", included: true },
      { plan: "Builder", level: "+ mission planner on real hardware", included: true },
      { plan: "Fleet", level: "+ natural-language agent & named missions", included: true },
      { plan: "Forge", level: "Custom behaviors + on-prem agent", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Builder gives you mapping, navigation and the mission planner on real hardware — the core of autonomy. Add Fleet when you want the natural-language agent and reusable named missions; Forge is for custom behaviors and running the agent on-prem.",
    faq: [
      { q: "How is this different from OhhO Pilot?", a: "Pilot is for a human operating the robot; Autonomy is for the robot operating itself. They share the same safe control-mode mux, so you can hand control back and forth instantly." },
      { q: "Does the language agent need the cloud?", a: "By default it calls a cloud LLM, but the agent layer is optional and model-agnostic — navigation and the mission planner run fully on-robot, and Forge can run the agent on-prem with your own model." },
    ],
    related: ["serve", "view", "pilot"],
    dashboardCaption:
      "OhhO Autonomy — the live SLAM map with a planned Nav2 path, the mission state machine and the natural-language agent's plan.",
    app: { href: "/autonomy", label: "Open mission control" },
  },
  {
    slug: "mind",
    name: "OhhO Mind",
    tag: "Give your robot a mind of its own.",
    desc: "The continuous agent brain that turns a command-taking robot into a goal-driven one. It perceives, reasons, verifies, acts, reflects and learns — on a loop — and ships to your fleet as an over-the-air update.",
    accent: "violet",
    category: "Intelligence",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <circle cx="12" cy="12" r="3" />
        <circle cx="5" cy="6" r="2" />
        <circle cx="19" cy="6" r="2" />
        <circle cx="5" cy="18" r="2" />
        <circle cx="19" cy="18" r="2" />
        <path d="m9.8 10.2-3.1-2.7" />
        <path d="m14.2 10.2 3.1-2.7" />
        <path d="m9.8 13.8-3.1 2.7" />
        <path d="m14.2 13.8 3.1 2.7" />
      </svg>
    ),
    hero: "OhhO Autonomy lets a robot follow a mission; OhhO Mind lets it decide on one. Mind is the deliberative layer that runs continuously on top of your stack — perceive → reason → verify → act → monitor → reflect → remember — turning a capable robot into an agent you give goals to, not scripts.",
    highlights: [
      "Continuous perceive→reason→act→reflect loop",
      "Goals in plain language, not scripted commands",
      "Hardware-safety gate on every action",
      "Hybrid reasoning: cloud + on-device + NPU",
      "Memory + learns from its own experience",
    ],
    overview: [
      "Foundation models gave robots brilliant reflexes — a Vision-Language-Action model turns an image and an instruction into an action. But a reflex isn't a mind. A robot still can't choose a goal, remember what it saw, notice that it failed, or get better over time. OhhO Mind is the layer that closes that gap.",
      "Mind runs a continuous decision loop on top of the robot's existing fast control stack. It fuses everything the robot knows into one world-state snapshot, reasons over it to pick the next action, verifies that action against the robot's real hardware limits, executes it through OhhO Autonomy and OhhO Serve, watches the outcome, and reflects on whether the goal was met — then remembers what it learned and does it again. You hand it an objective in plain language; it loops until the job is done, then idles.",
      "Crucially, Mind is built for the real world, not the demo table. Its reasoning runs wherever it can — cloud models when connected, an on-device model and an onboard NPU when not — so the robot keeps thinking with no internet. And because it judges and stores every attempt, that experience feeds OhhO Train to improve the underlying policies. Mind reaches your robots the way software should: as an OhhO Fleet over-the-air update, so a deployed fleet wakes up smarter with no new hardware.",
    ],
    features: [
      { title: "The agent loop", body: "A continuous perceive → reason → verify → act → monitor → reflect → remember cycle. Give it a goal; it runs until done, recovers from failure, and idles when there's nothing to do." },
      { title: "Grounded world state", body: "Mind fuses pose, arm state, detected objects, mission status and memory into one snapshot — so it reasons about the world the robot is actually in, not a guessed one." },
      { title: "A safety gate it can't skip", body: "Every low-level action is checked against the robot's real limits — velocity, joint deltas, reach — before it reaches a motor. Unsafe plans are rejected, not clipped." },
      { title: "Hybrid reasoning", body: "Cloud-class reasoning when online, an on-device model and NPU when offline. The robot degrades gracefully instead of going dark when the network does." },
      { title: "Memory that compounds", body: "Mind remembers objects, places and the outcomes of past attempts, and feeds them into every decision — so the robot builds a model of its own environment." },
      { title: "Learns from experience", body: "Each attempt is judged, labelled and stored, then fed to OhhO Train to improve the policies — closing the loop from operation back to capability." },
    ],
    how: [
      { title: "Give it a goal", body: "Send a plain-language objective — 'find the red cup and bring it back' — instead of a step-by-step script." },
      { title: "It reasons & verifies", body: "Mind grounds the goal in the live world state, picks the next action, and passes it through the safety gate." },
      { title: "It acts & reflects", body: "It runs the action through Autonomy and Serve, watches the result, and re-plans until the goal is met." },
      { title: "It learns & updates", body: "Outcomes feed OhhO Train; improved intelligence ships back to the fleet as an OhhO Fleet OTA update." },
    ],
    specs: [
      { label: "Loop", value: "perceive → reason → verify → act → monitor → reflect → remember" },
      { label: "Reasoning", value: "Cloud LLM + on-device LLM + NPU (hybrid router)" },
      { label: "Safety", value: "Hardware-limit + reachability gate on every action" },
      { label: "Memory", value: "Object / place memory + episodic recall" },
      { label: "Learning", value: "Judged episodes → OhhO Train continual learning" },
      { label: "Delivery", value: "OhhO Fleet over-the-air update" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "Single-robot agent loop", included: true },
      { plan: "Fleet", level: "+ hybrid reasoning, memory & continual learning", included: true },
      { plan: "Forge", level: "On-prem brain + custom skills", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Builder runs the agent loop on a single robot — enough to give one machine goals instead of scripts. Choose Fleet for the full hybrid (offline) reasoning, long-term memory, continual learning and OTA delivery across a fleet; Forge runs the brain entirely on-prem with your own custom skills.",
    faq: [
      { q: "How is this different from OhhO Autonomy?", a: "Autonomy executes a mission — map, navigate, run a skill. Mind decides the mission: it chooses goals, sequences skills, recovers from failure and learns. Mind sits on top of Autonomy and commands it." },
      { q: "Does it need the cloud to think?", a: "No. Mind prefers cloud-class reasoning when connected, but falls back to an on-device model and an onboard NPU when offline — so the robot keeps operating with no internet." },
      { q: "Is it safe to let a robot decide for itself?", a: "Every action Mind emits passes a safety gate checked against the robot's real hardware limits before it reaches a motor, and a human can take over or e-stop at any moment." },
      { q: "How does it reach robots already in the field?", a: "Mind is delivered as an OhhO Fleet over-the-air update — your deployed robots receive it like an app update, with no new hardware and no teardown." },
    ],
    related: ["autonomy", "serve", "train"],
    dashboardCaption:
      "OhhO Mind — the live agent loop, the fused world state, the verified next action, and the hybrid reasoning router.",
    app: { href: "/mind", label: "Open the console" },
  },
  {
    slug: "market",
    name: "OhhO Market",
    tag: "Download a skill. Or sell one.",
    desc: "A cross-brand marketplace for trained robot skills and policies. Download a verified pick-and-place policy for your G1, or publish one you trained with OhhO Train — signed, safety-checked and robot-ready.",
    accent: "cyan",
    category: "Intelligence",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M3 9l1.5-5h15L21 9" />
        <path d="M3 9v11a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1V9" />
        <path d="M3 9h18" />
        <path d="M9 21V13h6v8" />
      </svg>
    ),
    hero: "OEM app stores are per-robot. OhhO Market is the cross-brand equivalent for trained behaviors — a marketplace where you download a verified policy for your specific robot, or publish one you trained with OhhO Train. Every skill is signed, safety-checked through OhhO Proof, and tagged by robot model, task and success rate.",
    highlights: [
      "Cross-brand skill marketplace",
      "Verified, signed policies",
      "Tagged by robot + task",
      "One-click deploy via Serve",
      "Sell skills, take-rate model",
    ],
    overview: [
      "A trained policy is the most valuable artifact in robotics — and today, every team trains their own from scratch. OhhO Market changes that. It's a marketplace where a verified pick-and-place policy for a humanoid, a patrol skill for a quadruped, or a welding trajectory for an industrial arm can be downloaded, deployed and monetized.",
      "Every skill on Market is produced through the OhhO pipeline: trained with OhhO Train, validated through OhhO Proof's scenario suites, signed with OhhO Shield's supply-chain keys, and tagged with the robot models it runs on, the task it performs, and its measured success rate. You know what you're buying before you download it.",
      "Market creates a network effect that compounds: more robots on the platform attract more skill authors, more skills attract more robot owners, and the take-rate model rewards both. For a startup that just bought a G1, Market means deploying a working skill on day one instead of spending three months collecting data and training.",
    ],
    features: [
      { title: "Cross-brand, not per-robot", body: "Unlike OEM app stores, Market spans every robot the platform supports. A skill tagged for 'any mecanum base' works on a mecanum manipulator, a research robot and a custom AMR alike." },
      { title: "Verified, not posted", body: "Every published skill passes through OhhO Proof's scenario suites before it's listed — so the success rate on the listing is the measured rate, not a marketing claim." },
      { title: "Signed and tamper-proof", body: "Each skill package is signed with OhhO Shield's supply-chain keys, so a robot verifies the skill's integrity before loading it via OhhO Serve." },
      { title: "One-click deploy", body: "Download a skill and Serve loads it — no manual checkpoint conversion, no model-class mismatch. The skill package carries its model class and config." },
      { title: "Sell or share", body: "Authors set a price or publish for free. Market handles licensing, versioning and robot-model compatibility checks. The take-rate funds the platform." },
      { title: "Training-to-market loop", body: "Train a skill with OhhO Train, validate with OhhO Proof, sign with OhhO Shield, publish to Market — the whole pipeline is one platform." },
    ],
    how: [
      { title: "Browse skills", body: "Filter by robot model, task type, success rate and price — or search 'pick and place' for your G1." },
      { title: "Verify the claim", body: "Each listing shows the Proof scenario results, the training data size and the measured success rate." },
      { title: "Deploy", body: "Download the signed skill package and OhhO Serve loads it — or push it to your fleet via OhhO Fleet OTA." },
      { title: "Publish your own", body: "Train with OhhO Train, pass OhhO Proof, sign with OhhO Shield, and list on Market — free or priced." },
    ],
    specs: [
      { label: "Listing format", value: "Signed skill package (checkpoint + config + Proof report)" },
      { label: "Compatibility", value: "Tagged by robot model, brand and locomotion type" },
      { label: "Verification", value: "OhhO Proof scenario suites (pass rate published)" },
      { label: "Signing", value: "OhhO Shield supply-chain signatures" },
      { label: "Deploy", value: "OhhO Serve load or OhhO Fleet OTA" },
      { label: "Model", value: "Free + paid listings, platform take-rate" },
    ],
    plans: [
      { plan: "Spark", level: "Browse + free skills", included: true },
      { plan: "Builder", level: "Download paid skills", included: true },
      { plan: "Fleet", level: "Sell skills + team licenses", included: true },
      { plan: "Forge", level: "Private marketplace + white-label", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Anyone can browse and download free skills on Spark. Builder adds paid skills — most teams want at least one commercial policy to skip months of training. Teams selling skills or buying team licenses choose Fleet; enterprises running a private marketplace choose Forge.",
    faq: [
      { q: "How is this different from an OEM app store?", a: "OEM stores are per-robot apps for one brand's hardware only. Market is cross-brand — a skill tagged 'any mecanum base' works on any compatible robot, not just one OEM's. And every skill is verified through OhhO Proof, not just posted." },
      { q: "Can I sell a skill I trained?", a: "Yes. Train with OhhO Train, pass OhhO Proof's scenario suites, sign with OhhO Shield, and list it on Market at any price. The platform take-rate funds verification and hosting." },
      { q: "What if a skill doesn't work on my robot?", a: "Every listing is tagged with compatible robot models. Market checks compatibility before download, and the Proof report shows the exact scenarios the skill was tested in." },
    ],
    related: ["train", "serve", "proof"],
    app: { href: "/market", label: "Open the marketplace" },
    dashboardCaption:
      "OhhO Market — skill listings tagged by robot and task, with Proof-verified success rates and one-click deploy to Serve.",
  },

  // ── OPERATIONS ──────────────────────────────────────────────────────────────
  {
    slug: "pilot",
    name: "OhhO Pilot",
    tag: "Operate any robot. From anywhere. In mixed reality.",
    desc: "Mixed-reality teleoperation on any OpenXR headset. Catalog-driven robot profiles, hand-tracking arm IK, WebRTC video, dual-arm humanoid control, and per-robot calibration — any robot in your garage, one tap to drive.",
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
    hero: "Put on an OpenXR mixed-reality headset, see your real room through passthrough, pick any robot from your OhhO garage — a mecanum mobile manipulator, a quadrotor drone, an underwater ROV, a dual-arm humanoid — and teleoperate it with controls appropriate to that robot. Hand-tracking arm IK, WebRTC telepresence video, per-robot calibration, and a catalog-driven spatial add-robot flow that mirrors the website. One shared OhhO account across web, Android and VR.",
    highlights: [
      "Mixed-reality passthrough (OpenXR headset)",
      "Catalog-driven — any robot, one tap to drive",
      "Hand-tracking arm IK (analytic + FABRIK)",
      "WebRTC video with MJPEG fallback",
      "Dual-arm humanoid control",
      "Per-robot calibration + quick-resume",
    ],
    overview: [
      "OhhO Pilot is the mixed-reality teleoperation cockpit. Put on an OpenXR mixed-reality headset and your real room is the background — passthrough MR means no vection and minimal sim-sickness, even during fast base driving. Floating glass panels show your robot's camera feed, telemetry and HUD, all themed to match the OhhO website.",
      "Pilot is catalog-driven: the robot list, capability model and branding all mirror the OhhO website through one shared Supabase account. Pick a robot from your garage — or add a new one in-headset with the spatial categories → types → models → name flow — and Pilot builds a RobotProfile from the catalog entry (drive kind, arm DOF, joint limits, ROS topics, max velocities). A control-scheme factory picks the right drive scheme (mecanum, differential, Ackermann, aerial mode-2 RC, thruster vectoring) and manipulation scheme (6-DOF hand-IK, dual-arm FABRIK, gripper-only, or robot-side MoveIt Servo) for that robot automatically.",
      "Every robot category gets a correct, distinct control mapping. A drone uses mode-2 RC sticks (left = throttle + yaw, right = translate). An underwater ROV gets surge/sway/heave/yaw. A humanoid drives two arms with two hands via independent FABRIK IK chains. A mecanum mobile manipulator gets analytic arm IK + mecanum strafe. Safety is non-negotiable: deadman grip to drive, both-grips e-stop, velocity/joint clamping from the profile, and a connection-loss watchdog that auto-stops.",
    ],
    features: [
      { title: "Mixed-reality passthrough", body: "Always-on headset passthrough as the scene background. Frosted-glass panels float in your real room — see the robot and your surroundings simultaneously. Spatial anchors pin the virtual arm workspace to a real surface so it stays put as you walk." },
      { title: "Catalog-driven, any robot", body: "The robot list, capability model and branding mirror the OhhO website through one shared Supabase account. A drone, an industrial arm, a quadruped and a mobile manipulator each get a correct, distinct control scheme — automatically, from the catalog profile. Add robots in-headset or on the website; they appear everywhere." },
      { title: "Hand-tracking arm IK", body: "The headset reports a 6-DOF wrist pose; Pilot retargets it to the arm's end-effector and solves IK in real time. Analytic IK for the reference 6-DOF arm, generic FABRIK for arbitrary chains, dual-arm FABRIK for humanoids. Pinch drives the gripper. Optionally stream the Cartesian target to MoveIt 2 Servo on the robot for collision-aware solving." },
      { title: "WebRTC telepresence video", body: "Sub-100 ms live video via WebRTC when available, with automatic MJPEG fallback per-camera. The camera list comes from the robot's profile — a drone shows FPV + down cameras, a mobile manipulator shows front + wrist + BEV, all automatically." },
      { title: "Per-robot calibration + quick-resume", body: "Each robot gets its own calibration (arm base height, workspace scale, velocity caps, IK location) persisted in the headset. The last-driven robot is saved for one-tap quick-resume on the next session. First-run onboarding shows control hints for the selected robot." },
      { title: "Profile-driven demo recording", body: "Record imitation-learning demonstrations at 30 Hz in LeRobot-compatible JSONL. The recorder reads ROS topics and state/action dimensions from the robot's profile, so a drone records base-only obs/actions, a humanoid records dual-arm + base, and an industrial arm records arm-only — all correctly, automatically." },
    ],
    how: [
      { title: "Sign in", body: "Put on the headset and sign in with the same OhhO account you use on the website — email + 6-digit OTP code. Your saved robots are already there." },
      { title: "Pick a robot", body: "Open your garage and pick a robot — or add a new one with the spatial categories → types → models → name flow. Quick-resume jumps straight to the last robot you drove." },
      { title: "Drive + manipulate", body: "Thumbsticks drive the base (mecanum strafe, drone throttle/yaw, ROV surge/sway/heave). Right-hand pose retargets to the arm via IK; pinch drives the gripper. Both grips = emergency stop." },
      { title: "Record + hand off", body: "Record teleop demonstrations for training, or switch control modes between teleop, navigation and AI policies. Export episodes to the robot for the OhhO Data pipeline." },
    ],
    specs: [
      { label: "Headset", value: "Any OpenXR mixed-reality headset (passthrough)" },
      { label: "Engine", value: "Unity 2023.3 LTS + OpenXR" },
      { label: "Transport", value: "ROSBridge WebSocket (port 9090)" },
      { label: "Video", value: "WebRTC (sub-100 ms) + MJPEG fallback" },
      { label: "Drive schemes", value: "Mecanum, differential, Ackermann, aerial, thruster, legged" },
      { label: "Arm IK", value: "Analytic (reference 6-DOF), FABRIK (generic), dual-arm, MoveIt Servo (robot-side)" },
      { label: "Safety", value: "Deadman grip, both-grips e-stop, velocity/joint clamps, connection-loss watchdog" },
      { label: "Recording", value: "30 Hz JSONL, profile-driven topics + dims (LeRobot-compatible)" },
      { label: "Account", value: "Shared Supabase identity across web, Android and VR" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "Mobile, up to 3 robots", included: true },
      { plan: "Fleet", level: "VR + mobile, unlimited robots", included: true },
      { plan: "Forge", level: "White-label Pilot", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Casual mobile teleop of a few robots fits Builder. For the full VR experience — mixed-reality passthrough, WebRTC video, unlimited robots, dual-arm humanoid control, and per-robot calibration — choose Fleet. White-label Pilot for your own operators with Forge.",
    faq: [
      { q: "Do I need a VR headset?", a: "No. Pilot works fully on mobile; VR is the immersive option for mixed-reality arm control with hand tracking. The VR app targets any OpenXR mixed-reality headset." },
      { q: "Which robots can I teleoperate in VR?", a: "Any robot in the OhhO catalog. The headset derives a control scheme from the robot's profile — a mecanum base gets strafe sticks, a drone gets mode-2 RC, an ROV gets thruster vectoring, a humanoid gets dual-arm hand IK. Add robots on the website, Android app, or in-headset." },
      { q: "What about video latency?", a: "Pilot supports WebRTC for sub-100 ms telepresence when the package and signaling server are available, with automatic MJPEG fallback per-camera. The camera list adapts to the robot — a drone shows FPV + down, a mobile manipulator shows front + wrist + BEV." },
      { q: "Can I solve IK on the robot instead of the headset?", a: "Yes. Pilot supports robot-side IK via MoveIt 2 Servo — stream the Cartesian end-effector target and let the robot solve with collision awareness. This is the default for industrial-arm profiles; mobile manipulators use headset-side IK for lowest latency." },
      { q: "Is it safe over the internet?", a: "Pilot clamps velocities and joint angles from the robot's profile, offers a deadman grip and both-grips emergency stop, and auto-stops on connection loss. Pair it with OhhO Shield for authenticated, encrypted links." },
    ],
    related: ["autonomy", "view", "fleet", "connect"],
    app: { href: "/pilot", label: "Open the cockpit" },
    vr: true,
    dashboardCaption:
      "OhhO Pilot — mixed-reality teleoperation on an OpenXR headset: passthrough room, floating glass HUD, hand-tracking arm IK, and WebRTC camera feed.",
  },
  {
    slug: "fleet",
    name: "OhhO Fleet",
    tag: "Update 50 robots like you update an app.",
    desc: "Fleet management, signed over-the-air updates, VDA 5050 warehouse integration, Open-RMF multi-robot coordination, and a full observability stack — Prometheus, Grafana and alerting, pre-configured.",
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
    hero: "Update 50 robots like you update an app. Fleet management, signed over-the-air updates, VDA 5050 warehouse integration, Open-RMF multi-robot coordination, and a full observability stack — Prometheus, Grafana and alerting, pre-configured.",
    highlights: [
      "Fleet health dashboard",
      "Signed OTA updates (code + models)",
      "Staged / canary rollouts",
      "VDA 5050 compatible (AGV/AMR fleet standard)",
      "Open-RMF multi-robot coordination",
      "Prometheus + Grafana + alerts",
      "Per-robot telemetry",
    ],
    overview: [
      "One robot is a project; fifty is an operation. OhhO Fleet is mission control for a whole fleet — see every robot's health, push software and AI-model updates over the air, and get paged before a problem becomes an outage.",
      "Fleet ships the observability stack already wired: Prometheus scraping, Grafana dashboards, Loki, Tempo and AlertManager. Robot telemetry, GPU metrics and VLA latency all land in one place.",
      "Over-the-air updates cover both the ROS 2 workspace and ONNX policy models, with staged rollouts so you can canary a release to a few robots before it reaches the rest. And because Fleet speaks the VDA 5050 AGV/AMR standard over MQTT, your robots integrate with the warehouse management systems and master control software your facility already runs — while Open-RMF handles multi-robot traffic management, task allocation and conflict-free navigation across mixed fleets.",
    ],
    features: [
      { title: "Single pane of glass", body: "Every robot's status, version, battery and last-seen in one dashboard." },
      { title: "OTA for code and models", body: "Update the ROS 2 workspace and ONNX policies remotely — no field visits." },
      { title: "Staged rollouts", body: "Canary to a subset, watch the metrics, then roll forward or back." },
      { title: "VDA 5050 warehouse integration", body: "Speaks the VDA 5050 AGV/AMR fleet standard over MQTT — so OhhO Fleet interoperates with warehouse management systems and master control software from Linde, Toyota, MiR, KION and the rest of the VDA 5050 ecosystem. Your robots join the fleet your warehouse already runs." },
      { title: "Open-RMF multi-robot coordination", body: "Open Robotics Middleware Framework integration for multi-robot traffic management, task allocation and conflict-free navigation — so mixed fleets from different vendors share the same floor without collisions or deadlocks." },
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
      { label: "Metrics", value: "Prometheus (robot compute, GPU, VLA, DCGM)" },
      { label: "Dashboards", value: "Grafana (auto-provisioned)" },
      { label: "Logs / traces", value: "Loki + Tempo" },
      { label: "Alerting", value: "AlertManager → email / Slack" },
      { label: "OTA", value: "Workspace + ONNX models, signed" },
      { label: "Rollouts", value: "Staged / canary" },
      { label: "Fleet protocol", value: "VDA 5050 over MQTT" },
      { label: "Coordination", value: "Open-RMF compatible (multi-robot)" },
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
      { q: "Does Fleet integrate with my warehouse management system?", a: "Yes. Fleet speaks the VDA 5050 AGV/AMR fleet standard over MQTT — the same standard used by Linde, Toyota, MiR, KION and other major warehouse robotics vendors. Your robots appear in your WMS as standard VDA 5050 vehicles, and master control software can dispatch them alongside any other compliant AGV." },
      { q: "Can Fleet coordinate mixed fleets from different vendors?", a: "Yes. Open-RMF integration provides multi-robot traffic management, task allocation and conflict-free navigation across mixed fleets — so robots from different vendors share the same floor without collisions or deadlocks." },
    ],
    related: ["serve", "shield", "proof", "twin", "care"],
    app: { href: "/fleet", label: "Open mission control" },
    dashboardCaption:
      "OhhO Fleet — fleet map, health donut, OTA rollout progress and the live alert feed.",
  },
  {
    slug: "twin",
    name: "OhhO Twin",
    tag: "Your real robot. Mirrored in simulation. Live.",
    desc: "A live digital twin that streams real robot telemetry into a persistent simulation — replay, scrub, what-if and predict, side by side with the physical robot. Powered by Gazebo and Isaac Sim.",
    accent: "violet",
    category: "Operations",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <rect x="3" y="4" width="8" height="16" rx="1.5" />
        <rect x="13" y="4" width="8" height="16" rx="1.5" />
        <path d="M11 12h2" />
        <circle cx="7" cy="8" r=".9" fill="currentColor" stroke="none" />
        <circle cx="17" cy="8" r=".9" fill="currentColor" stroke="none" />
      </svg>
    ),
    hero: "OhhO Frame gives you a simulator to develop against; OhhO Twin gives you a live mirror of the robot you're already running. Real telemetry streams into a persistent Gazebo or Isaac Sim world — so you can replay the last hour, scrub to the moment something went wrong, run a what-if with a different policy, and predict what happens next.",
    highlights: [
      "Live telemetry → persistent sim",
      "Replay + scrub any moment",
      "What-if with different policies",
      "Prediction from observed state",
      "Gazebo + Isaac Sim (USD) backed",
      "OPC UA factory integration",
    ],
    overview: [
      "A digital twin is the bridge between 'it worked in simulation' and 'it's working right now on the factory floor.' OhhO Twin streams a real robot's telemetry — pose, joints, sensors, camera frames — into a persistent simulation world that stays in sync, so the sim always reflects what the robot is actually doing.",
      "Twin is not just a live view. It's a time machine: every telemetry frame is recorded, so you can replay the last shift, scrub to the moment a pick failed, and see exactly what the robot saw and felt at that instant. Run a what-if — 'what if the policy had chosen a different grasp angle?' — and Twin simulates the alternative from the same starting state.",
      "For fleet operators, Twin is the difference between reactive and predictive. A motor's temperature curve in the twin predicts a failure before it happens. A near-miss in the twin becomes a training scenario for OhhO Proof. And because Twin runs on the same Gazebo and Isaac Sim worlds as Frame, what you learn in the twin transfers directly to the simulation you develop in.",
    ],
    features: [
      { title: "Live mirror", body: "Real robot telemetry streams into a persistent Gazebo or Isaac Sim world, kept in sync frame by frame — so the sim always reflects reality." },
      { title: "Replay + scrub", body: "Every telemetry frame is recorded. Replay the last hour, scrub to any instant, and inspect pose, joints, sensors and camera frames at that exact moment." },
      { title: "What-if simulation", body: "Branch from any recorded state and simulate a different outcome — a different policy, a different grasp, a different speed — without touching the real robot." },
      { title: "Prediction", body: "From the observed state, Twin can project forward — motor temperature trends, battery depletion, trajectory completion — so you see problems before they happen." },
      { title: "Shared sim world", body: "Twin runs on the same Gazebo and Isaac Sim worlds as OhhO Frame (URDF, SDF and USD scene formats), so what you learn in the twin transfers directly to the simulation you develop and test in." },
      { title: "OPC UA factory integration", body: "Twin speaks OPC UA — the Industry 4.0 standard — so your digital twin exchanges data with factory cells, MES/SCADA systems and enterprise digital twin platforms that already speak OPC UA. A robot on OhhO plugs into the digital twin infrastructure your plant already has." },
      { title: "Fleet-scale", body: "Mirror one robot or a hundred. Each twin streams independently and is replayable from the Fleet dashboard." },
    ],
    how: [
      { title: "Connect the robot", body: "Twin uses the live telemetry stream from OhhO Connect — no extra wiring." },
      { title: "Mirror in sim", body: "Real telemetry flows into a persistent Gazebo or Isaac Sim world that stays in sync." },
      { title: "Replay + what-if", body: "Scrub to any moment, inspect what the robot saw, and branch into a what-if simulation." },
      { title: "Predict + feed back", body: "Twin projects trends forward — and near-misses become OhhO Proof scenarios, failures become OhhO Care tickets." },
    ],
    specs: [
      { label: "Simulators", value: "Gazebo Harmonic + Isaac Sim" },
      { label: "Scene formats", value: "USD, SDF, URDF" },
      { label: "Factory integration", value: "OPC UA (Industry 4.0)" },
      { label: "Telemetry", value: "Pose, joints, IMU, cameras (via OhhO Connect)" },
      { label: "Replay", value: "Full timeline scrub, per-frame inspection" },
      { label: "What-if", value: "Branch from any recorded state" },
      { label: "Prediction", value: "Motor, battery, trajectory projection" },
      { label: "Scale", value: "Single robot (Builder) to fleet (Fleet)" },
    ],
    plans: [
      { plan: "Spark", level: "Simulated twin (no live data)", included: true },
      { plan: "Builder", level: "Single live twin + replay", included: true },
      { plan: "Fleet", level: "Multi-robot twins + what-if", included: true },
      { plan: "Forge", level: "Enterprise twin + prediction APIs", included: true },
    ],
    recommendedPlan: "Builder",
    planRationale:
      "Spark gives you a simulated twin to explore the concept. Most teams want Builder for a single live twin with replay — enough to diagnose what happened on the real robot. Fleet operators choose Fleet for multi-robot twins and what-if; enterprises choose Forge for prediction APIs and custom models.",
    faq: [
      { q: "How is Twin different from Frame's simulation?", a: "Frame gives you a simulator to develop against — a clean world you launch and iterate in. Twin mirrors a real robot that's already running, streaming live telemetry and recording every frame for replay and what-if. They share the same Gazebo and Isaac Sim worlds." },
      { q: "Do I need hardware to use Twin?", a: "No. Spark includes a simulated twin with synthetic telemetry. But the real value — replay, what-if, prediction — comes from streaming a real robot's data, which starts on Builder." },
      { q: "Can Twin predict failures?", a: "Yes. By tracking motor temperature, current draw and vibration trends in the recorded telemetry, Twin projects degradation forward — and feeds OhhO Care to schedule maintenance before a failure." },
    ],
    related: ["frame", "fleet", "care"],
    app: { href: "/twin", label: "Open the twin" },
    dashboardCaption:
      "OhhO Twin — live robot mirrored in Isaac Sim, the replay timeline, and a what-if branch from the selected moment.",
  },
  {
    slug: "care",
    name: "OhhO Care",
    tag: "Fix it before it breaks.",
    desc: "Predictive maintenance and service workflow for robot fleets. Turn motor-degradation signals from OhhO Fleet into scheduled service, ordered parts and logged repairs — closing the loop from monitoring to maintenance.",
    accent: "cyan",
    category: "Operations",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M3 12h4l2-6 4 12 2-6h6" />
      </svg>
    ),
    hero: "OhhO Fleet tells you a motor is degrading; OhhO Care turns that signal into a workflow. Predictive maintenance, parts ordering from the OhhO Build bill of materials, technician dispatch, and a repair log that feeds back into OhhO Comply's audit trail — closing the loop from 'something is wearing' to 'it's fixed and documented.'",
    highlights: [
      "Predictive maintenance alerts",
      "Auto-sourced parts from Build BOM",
      "Technician dispatch + scheduling",
      "Repair log → Comply audit trail",
      "Downtime tracking + MTBF",
    ],
    overview: [
      "Monitoring tells you a robot is about to fail. Maintenance is what you do about it. OhhO Care is the product that bridges that gap — turning the degradation signals OhhO Fleet and OhhO Twin surface into a structured service workflow that ends with a fixed, documented robot.",
      "When a motor's temperature trend crosses a threshold or a joint's torque ripple changes, Care raises a predictive maintenance ticket — not a generic alert, but a structured work order with the affected robot, the suspect component, the predicted failure window, and the replacement part pulled from the robot's OhhO Build bill of materials. One click orders the part; another schedules a technician.",
      "When the repair is done, Care logs it: what was replaced, when, by whom, with what part batch — and that record flows straight into OhhO Comply's audit trail, so the robot's maintenance history is part of its certification evidence. Downtime, MTBF and mean-time-to-repair metrics roll up into the Fleet dashboard, so you see the operational cost of maintenance, not just the technical signals.",
    ],
    features: [
      { title: "Predictive alerts", body: "Degradation signals from Fleet and Twin — motor temperature, torque ripple, vibration — become structured work orders, not just alerts." },
      { title: "Parts from your BOM", body: "The replacement part is pulled from the robot's OhhO Build bill of materials, with supplier links and lead times — so ordering is one click, not a scavenger hunt." },
      { title: "Technician dispatch", body: "Schedule a field service visit, assign a technician and block the robot's calendar — all from the work order." },
      { title: "Repair logging", body: "Every repair is logged with the part replaced, the timestamp, the technician and the part batch — and flows into OhhO Comply's audit trail." },
      { title: "Downtime + MTBF", body: "Care tracks downtime, mean-time-between-failures and mean-time-to-repair per robot and across the fleet, surfaced in the Fleet dashboard." },
      { title: "Closes the loop", body: "Fleet detects, Care schedules, Bench calibrates the replacement, Comply records it. The whole maintenance lifecycle, one platform." },
    ],
    how: [
      { title: "Detect", body: "Fleet and Twin surface a degradation signal — a motor running hot, a joint drifting." },
      { title: "Triage", body: "Care raises a work order with the affected robot, the suspect component and the predicted failure window." },
      { title: "Order + dispatch", body: "One click orders the replacement part from the Build BOM; another schedules the technician." },
      { title: "Repair + log", body: "The repair is logged and flows into Comply's audit trail; MTBF and downtime update in Fleet." },
    ],
    specs: [
      { label: "Signals", value: "Motor temp, torque ripple, vibration (from Fleet + Twin)" },
      { label: "Parts", value: "Auto-sourced from OhhO Build BOM" },
      { label: "Workflow", value: "Work order → dispatch → repair → log" },
      { label: "Metrics", value: "Downtime, MTBF, MTTR per robot + fleet" },
      { label: "Audit", value: "Repair log → OhhO Comply audit trail" },
      { label: "Integrates", value: "OhhO Fleet, Twin, Build, Comply" },
    ],
    plans: [
      { plan: "Spark", level: "Not included", included: false },
      { plan: "Builder", level: "Basic maintenance scheduling", included: true },
      { plan: "Fleet", level: "Predictive maintenance + parts ordering", included: true },
      { plan: "Forge", level: "Full service workflow + SLA tracking", included: true },
    ],
    recommendedPlan: "Fleet",
    planRationale:
      "Care is most valuable when you're running a real fleet and need predictive maintenance, not just a calendar reminder. Builder gives you basic scheduling for a few robots; Fleet adds predictive alerts, auto-sourced parts and MTBF metrics. Enterprises with field service teams choose Forge for SLA tracking and custom workflows.",
    faq: [
      { q: "How does Care know a part is about to fail?", a: "Care reads degradation signals from OhhO Fleet (motor temperature, current draw, torque limits) and OhhO Twin (vibration trends, trajectory deviation). When a signal crosses a learned threshold, Care raises a predictive work order." },
      { q: "Does Care order parts for me?", a: "Care pulls the replacement part from the robot's OhhO Build bill of materials — with supplier links and lead times. One click takes you to the supplier's checkout. On Forge, ordering can be fully automated." },
      { q: "How does Care relate to Comply?", a: "Every repair Care logs — what was replaced, when, by whom, with what batch — flows into OhhO Comply's audit trail, so the robot's maintenance history is part of its certification evidence." },
    ],
    related: ["fleet", "twin", "comply"],
    app: { href: "/care", label: "Open maintenance" },
    dashboardCaption:
      "OhhO Care — predictive maintenance work orders, motor-degradation trend, parts from the Build BOM, and the repair log feeding Comply.",
  },

  // ── TRUST (compliance / security / testing) ─────────────────────────────────
  {
    slug: "comply",
    name: "OhhO Comply",
    tag: "Ship robots the regulators will pass.",
    desc: "Turn robot safety standards into a guided checklist — CE, ISO 10218, ISO 3691-4, ISO 13482, ISO 13849, ANSI/RIA R15.06, UL — and auto-generate the technical file and audit trail to prove conformity.",
    accent: "violet",
    category: "Trust",
    icon: (
      <svg viewBox="0 0 24 24" {...stroke}>
        <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
        <path d="M9 2h6a1 1 0 0 1 1 1v1a1 1 0 0 1-1 1H9a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z" />
        <path d="m9 14 2 2 4-4" />
      </svg>
    ),
    hero: "Ship robots the regulators will pass. OhhO Comply turns robot safety standards into a guided checklist — CE, ISO 10218, ISO 3691-4, ISO 13482, ISO 13849, ANSI/RIA R15.06, UL — and generates the technical file and audit trail to prove conformity.",
    highlights: [
      "Standards mapped to your robot",
      "AGV/AMR safety (ISO 3691-4)",
      "Service robot safety (ISO 13482)",
      "US industrial robot safety (ANSI/RIA R15.06)",
      "Guided requirement checklists",
      "Auto-generated technical file & DoC",
      "Immutable audit trail",
      "Risk-assessment templates",
    ],
    overview: [
      "A robot that can't be certified can't be sold. Compliance is where most hardware projects stall — a maze of directives, harmonized standards and documentation that nobody on the team trained for. OhhO Comply makes it a workflow.",
      "Comply maps your robot — pulled straight from its OhhO Build design and deployment profile — to the standards that actually apply. For industrial robots: the EU Machinery Regulation and CE marking, ISO 10218 / ISO 15066, ISO 13849 functional safety, and regional electrical standards like UL and IEC 60204. For AGVs and AMRs: ISO 3691-4, the warehouse robotics safety standard, plus ANSI/ITSDF B56.5 in the US. For service and personal care robots: ISO 13482. For the North American market: ANSI/RIA R15.06, the US equivalent of ISO 10218. And the foundational IEC 61508 functional safety standard underpins them all.",
      "For each requirement it tracks evidence, owners and status, generates the technical construction file and Declaration of Conformity, and keeps an immutable audit trail — so when an auditor or a customer asks, the proof is one click away.",
    ],
    features: [
      { title: "Applicability engine", body: "Comply reads your robot's design and use case and tells you which directives and standards actually apply — industrial, AGV/AMR, service, medical, agricultural — no guessing." },
      { title: "AGV/AMR safety (ISO 3691-4)", body: "The warehouse robotics safety standard — covering driverless industrial trucks, speed control, safety zones, obstacle detection and emergency stops. Plus ANSI/ITSDF B56.5 for the US market." },
      { title: "Service robot safety (ISO 13482)", body: "Safety requirements for personal care robots and mobile servant robots — directly relevant to household and service robots heading to market." },
      { title: "US industrial safety (ANSI/RIA R15.06)", body: "The North American equivalent of ISO 10218 — industrial robot and robot system safety requirements for the US market, harmonized with the international standard." },
      { title: "Foundational functional safety (IEC 61508)", body: "The root functional safety standard that ISO 13849 and other domain standards derive from — referenced throughout the compliance chain." },
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
      { label: "Industrial robot standards", value: "EU Machinery Reg (CE), ISO 10218, ISO 15066, ISO 13849, ISO 12100, UL / IEC 60204" },
      { label: "AGV/AMR standards", value: "ISO 3691-4, ANSI/ITSDF B56.5" },
      { label: "Service robot standards", value: "ISO 13482 (personal care robots)" },
      { label: "US safety standards", value: "ANSI/RIA R15.06, ANSI/ITSDF B56.5" },
      { label: "Functional safety", value: "IEC 61508 (foundational)" },
      { label: "Risk assessment", value: "ISO 12100 + ISO 13849 PL templates" },
      { label: "Outputs", value: "Technical file, risk assessment, Declaration of Conformity" },
      { label: "Audit trail", value: "Immutable, timestamped" },
      { label: "Sync", value: "Driven by OhhO Build design changes" },
      { label: "Regions", value: "EU (CE), North America (UL/ANSI), extensible" },
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
      { q: "Which standards are covered?", a: "Industrial robot safety (CE / Machinery Regulation, ISO 10218, ISO 15066, ISO 13849, UL/IEC 60204), AGV/AMR safety (ISO 3691-4, ANSI/ITSDF B56.5), service robot safety (ISO 13482), US industrial safety (ANSI/RIA R15.06), and foundational functional safety (IEC 61508) — with custom standards available on Forge." },
      { q: "Does Comply cover warehouse AGV/AMR safety?", a: "Yes. ISO 3691-4 is the safety standard for driverless industrial trucks — covering speed control, safety zones, obstacle detection and emergency stops. Comply maps it to your AGV/AMR design automatically, plus ANSI/ITSDF B56.5 for the US market." },
      { q: "What about service and household robots?", a: "ISO 13482 covers personal care robots — mobile servants, physical assistant robots and person carrier robots. If you're building a household or service robot, Comply maps the requirements that apply to your category." },
    ],
    related: ["shield", "proof", "build", "care"],
    app: { href: "/comply", label: "Open compliance center" },
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
    app: { href: "/shield", label: "Open security dashboard" },
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
    app: { href: "/proof", label: "Open validation suite" },
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
