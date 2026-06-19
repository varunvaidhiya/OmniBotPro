/*
 * OhhO Garage — complete robot catalog.
 *
 * Every robot type and pre-loaded hardware model the platform supports.
 * Organized by category, each robot type has a list of real hardware models
 * the user can import into their garage with one click.
 *
 * Pure data only — importable from any component, server or client.
 */

import type { RobotType, HardwareModel } from "./types";
import { type RobotCategoryId } from "./types";

// ── Helpers ──────────────────────────────────────────────────────────────────

function hw(
  id: string,
  overrides: Partial<HardwareModel> & { name: string; manufacturer: string; desc: string },
): HardwareModel {
  return {
    id,
    specs: {},
    imageUrl: "",
    price: "—",
    supportedProducts: ["build", "serve", "pilot", "autonomy", "fleet"],
    ros: "ros2",
    locomotion: "—",
    hasArm: false,
    payloadKg: 0,
    weightKg: 0,
    ...overrides,
  };
}

// ── THE CATALOG ──────────────────────────────────────────────────────────────

export const ROBOT_TYPES: RobotType[] = [
  // ═══ DRONES ═══════════════════════════════════════════════════════════════
  {
    id: "drone-quadcopter",
    name: "Quadcopter",
    category: "drones" as RobotCategoryId,
    tagline: "4-rotor aerial platform — the standard for aerial robotics.",
    description:
      "Quadcopters are the most common multirotor configuration. Four independent rotors provide stable hover, agile maneuvering, and payload capacity from grams to kilograms. Used for inspection, surveying, cinematography, delivery, and research.",
    hardwareModels: [
      hw("dji-matrice-300", {
        name: "DJI Matrice 300 RTK",
        manufacturer: "DJI",
        desc: "Enterprise flagship with 55-min flight, IP45 rating, and multi-payload support. 15 km transmission range.",
        specs: { flightTime: "55 min", maxPayload: "2.7 kg", range: "15 km", weight: "6.3 kg" },
        price: "~$13,000",
        ros: "custom",
        locomotion: "quadrotor",
        weightKg: 6.3,
        payloadKg: 2.7,
        supportedProducts: ["pilot", "view", "data", "fleet"],
      }),
      hw("dji-mavic-3e", {
        name: "DJI Mavic 3 Enterprise",
        manufacturer: "DJI",
        desc: "Compact enterprise drone with mechanical shutter, RTK module, and 45-min flight. Ideal for mapping and inspection.",
        specs: { flightTime: "45 min", camera: "20 MP, 4/3 CMOS", range: "15 km", weight: "915 g" },
        price: "~$4,200",
        ros: "custom",
        locomotion: "quadrotor",
        weightKg: 0.915,
        payloadKg: 0.2,
        supportedProducts: ["pilot", "view", "data"],
      }),
      hw("px4-quad", {
        name: "PX4 Custom Quad",
        manufacturer: "Custom / Open Source",
        desc: "Fully open-source PX4/ArduPilot quadcopter. Build your own frame, stack companion computer (RPi/Jetson), and run ROS 2 on board.",
        specs: { flightTime: "20-40 min", frame: "450 mm", fc: "Pixhawk", comp: "RPi 5 / Jetson" },
        price: "~$800–3,000",
        ros: "ros2",
        locomotion: "quadrotor",
        weightKg: 1.5,
        payloadKg: 1.0,
        supportedProducts: ["build", "frame", "serve", "pilot", "autonomy", "data", "fleet"],
      }),
      hw("skydio-x10", {
        name: "Skydio X10",
        manufacturer: "Skydio",
        desc: "AI-powered autonomous drone with 360° obstacle avoidance, FLIR thermal, and 40-min flight. Best-in-class autonomous navigation.",
        specs: { flightTime: "40 min", sensors: "6× 4K nav cameras", thermal: "FLIR Boson+", weight: "3.1 kg" },
        price: "~$18,000",
        ros: "custom",
        locomotion: "quadrotor",
        weightKg: 3.1,
        payloadKg: 0.8,
        supportedProducts: ["pilot", "view", "data", "fleet"],
      }),
    ],
  },
  {
    id: "drone-hexacopter",
    name: "Hexacopter",
    category: "drones" as RobotCategoryId,
    tagline: "6-rotor heavy-lift platform — redundancy and payload.",
    description:
      "Six rotors provide motor-out redundancy and higher payload capacity. Used for heavy-lift cinematography, LiDAR surveying, agricultural spraying, and industrial cargo.",
    hardwareModels: [
      hw("dji-agras-t40", {
        name: "DJI Agras T40",
        manufacturer: "DJI",
        desc: "Agricultural spraying drone with 40 kg payload, 50 L tank, dual radar, and terrain-following. Covers 21 ha/hr.",
        specs: { payload: "40 kg", tank: "50 L", sprayWidth: "11 m", flightTime: "18 min loaded" },
        price: "~$25,000",
        ros: "custom",
        locomotion: "hexacopter",
        weightKg: 38,
        payloadKg: 40,
        supportedProducts: ["pilot", "fleet"],
      }),
      hw("freefly-alta-x", {
        name: "Freefly Alta X",
        manufacturer: "Freefly Systems",
        desc: "Heavy-lift cinema hexacopter with 16 kg payload, folding arms, and open-source PX4 autopilot.",
        specs: { payload: "16 kg", flightTime: "25–45 min", fc: "PX4", weight: "9.5 kg" },
        price: "~$17,500",
        ros: "custom",
        locomotion: "hexacopter",
        weightKg: 9.5,
        payloadKg: 16,
        supportedProducts: ["pilot", "view"],
      }),
    ],
  },
  {
    id: "drone-fixed-wing",
    name: "Fixed-Wing UAV",
    category: "drones" as RobotCategoryId,
    tagline: "Long-endurance fixed-wing for mapping and surveying vast areas.",
    description:
      "Fixed-wing UAVs offer flight times of 1-4+ hours, covering hundreds of square kilometers per flight. Used for aerial surveying, agriculture monitoring, pipeline inspection, and defense.",
    hardwareModels: [
      hw("wingtra-one", {
        name: "WingtraOne Gen II",
        manufacturer: "Wingtra",
        desc: "VTOL fixed-wing survey drone. Takes off and lands vertically, flies like a plane. 54-min endurance, 42 MP Sony RX1R II camera.",
        specs: { endurance: "54 min", coverage: "110 ha/flight", camera: "42 MP full-frame", weight: "4.5 kg" },
        price: "~$25,000",
        ros: "custom",
        locomotion: "vtol-fixed-wing",
        weightKg: 4.5,
        payloadKg: 0.5,
        supportedProducts: ["pilot", "data"],
      }),
      hw("sensefly-ebee-x", {
        name: "senseFly eBee X",
        manufacturer: "AgEagle (senseFly)",
        desc: "Fixed-wing mapping drone with swappable payloads (RGB, multispectral, thermal). Covers 500 ha/flight.",
        specs: { endurance: "90 min", coverage: "500 ha", payloads: "RGB, NIR, thermal", weight: "1.6 kg" },
        price: "~$15,000",
        ros: "custom",
        locomotion: "fixed-wing",
        weightKg: 1.6,
        payloadKg: 0.3,
        supportedProducts: ["pilot", "data"],
      }),
    ],
  },

  // ═══ WHEELED ROBOTS ═══════════════════════════════════════════════════════
  {
    id: "wheeled-differential",
    name: "Differential Drive",
    category: "wheeled" as RobotCategoryId,
    tagline: "Two-wheel differential base — the classic mobile robot.",
    description:
      "The simplest and most common wheeled robot configuration. Two independently driven wheels plus a caster for balance. Used in education, research, warehouse logistics, and indoor service robots.",
    hardwareModels: [
      hw("turtlebot4", {
        name: "TurtleBot 4",
        manufacturer: "Clearpath / Open Robotics",
        desc: "The standard ROS 2 education/research platform. iRobot Create 3 base, RPi 4, OAK-D camera, 2-D LiDAR. Runs ROS 2 Humble/Jazzy.",
        specs: { speed: "0.31 m/s", payload: "9 kg", sensors: "OAK-D, RPLIDAR", compute: "RPi 4 4 GB" },
        price: "~$1,200",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 3.0,
        payloadKg: 9,
        supportedProducts: ["build", "frame", "bench", "serve", "view", "autonomy", "pilot", "data", "train", "fleet"],
      }),
      hw("turtlebot3", {
        name: "TurtleBot 3 Burger / Waffle",
        manufacturer: "ROBOTIS",
        desc: "Compact, affordable ROS platform. Burger has 360° LiDAR; Waffle adds an Intel RealSense camera.",
        specs: { speed: "0.22 m/s", payload: "15 kg (Burger)", sensors: "LDS-01 LiDAR", compute: "RPi 3/4" },
        price: "~$550–1,000",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 1.0,
        payloadKg: 15,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data"],
      }),
      hw("clearpath-dingo", {
        name: "Clearpath Dingo",
        manufacturer: "Clearpath Robotics",
        desc: "Indoor research robot for manipulation. Modular payload mounts, 5/12 V power rails, ROS 2-native.",
        specs: { speed: "1.0 m/s", payload: "20 kg", drive: "diff-drive", compute: "Up to Jetson AGX" },
        price: "~$12,000+",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 20,
        payloadKg: 20,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data", "fleet"],
      }),
    ],
  },
  {
    id: "wheeled-mecanum",
    name: "Mecanum Wheel",
    category: "wheeled" as RobotCategoryId,
    tagline: "Omnidirectional mecanum base — full holonomic motion.",
    description:
      "Four mecanum rollers enable strafing, rotation, and diagonal motion — full 3-DOF planar movement. Used in tight-space logistics, mobile manipulation, and any application needing omnidirectional capability.",
    hardwareModels: [
      hw("omnibot", {
        name: "OmniBot Pro (Reference)",
        manufacturer: "OhhO / Open Source",
        desc: "OhhO's reference mecanum mobile manipulator. Yahboom X3 base, SO-101 6-DOF arm, Pi 5 brain, full ROS 2 stack. The platform the entire OhhO suite is built on.",
        specs: { speed: "0.2 m/s", payload: "5 kg", arm: "SO-101, 6-DOF", compute: "RPi 5 8 GB" },
        price: "~$1,500",
        ros: "ros2",
        locomotion: "mecanum",
        weightKg: 5,
        payloadKg: 5,
        hasArm: true,
        supportedProducts: ["build", "frame", "bench", "serve", "view", "autonomy", "pilot", "data", "train", "fleet", "comply", "proof", "shield"],
      }),
      hw("yahboom-x3", {
        name: "Yahboom Rosmaster X3",
        manufacturer: "Yahboom",
        desc: "Popular mecanum ROS robot kit. 4 STS servos, encoder odometry, IMU. Add your own SBC and sensors.",
        specs: { speed: "1.2 m/s", payload: "5 kg", odometry: "encoder + IMU", size: "265×250×130 mm" },
        price: "~$300–500",
        ros: "ros2",
        locomotion: "mecanum",
        weightKg: 2.5,
        payloadKg: 5,
        supportedProducts: ["build", "frame", "bench", "view", "autonomy", "pilot", "data", "fleet"],
      }),
      hw("nexus-4wd", {
        name: "Nexus 4WD Mecanum",
        manufacturer: "Nexus Robot",
        desc: "Heavy-duty mecanum base with 100 mm wheels, BLDC motors, and IP54 rating for light outdoor use.",
        specs: { speed: "1.8 m/s", payload: "25 kg", drive: "BLDC ×4", ip: "IP54" },
        price: "~$2,500",
        ros: "ros2",
        locomotion: "mecanum",
        weightKg: 12,
        payloadKg: 25,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "fleet"],
      }),
    ],
  },
  {
    id: "wheeled-ackermann",
    name: "Ackermann Steering",
    category: "wheeled" as RobotCategoryId,
    tagline: "Car-like steering — for outdoor autonomy and delivery.",
    description:
      "Car-like steering geometry for outdoor, higher-speed applications. Used in autonomous delivery, agriculture, and outdoor logistics where mecanum or differential bases are limited.",
    hardwareModels: [
      hw("clearpath-husky", {
        name: "Clearpath Husky A300",
        manufacturer: "Clearpath Robotics",
        desc: "Rugged outdoor UGV with Ackermann steering. IP65, 75 kg payload, 20 km range. ROS 2-native with full sensor integration.",
        specs: { speed: "1.5 m/s", payload: "75 kg", drive: "4×4 skid-steer", rating: "IP65" },
        price: "~$35,000+",
        ros: "ros2",
        locomotion: "ackermann / skid-steer",
        weightKg: 90,
        payloadKg: 75,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data", "fleet"],
      }),
      hw("racecar-j", {
        name: "MIT Racecar/J",
        manufacturer: "MIT / Open Source",
        desc: "Open-source 1/10 scale autonomous vehicle platform. NVIDIA Jetson + VESC motor controller. Used in F1Tenth racing.",
        specs: { speed: "7 m/s", scale: "1:10", compute: "Jetson Orin NX", lidar: "2-D LiDAR" },
        price: "~$4,000",
        ros: "ros2",
        locomotion: "ackermann",
        weightKg: 3.5,
        payloadKg: 5,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "data", "train"],
      }),
    ],
  },

  // ═══ LEGGED ROBOTS ════════════════════════════════════════════════════════
  {
    id: "legged-quadruped",
    name: "Quadruped (Dog)",
    category: "legged" as RobotCategoryId,
    tagline: "Four-legged robots — walk, climb, and navigate complex terrain.",
    description:
      "Quadruped robots traverse stairs, gravel, grass, and rubble that wheeled robots cannot. Used for inspection, security, construction monitoring, and research on dynamic locomotion.",
    hardwareModels: [
      hw("unitree-go2", {
        name: "Unitree Go2",
        manufacturer: "Unitree Robotics",
        desc: "The most popular quadruped. 12-DOF, 8 kg payload, 5 m/s top speed, 4-D LiDAR. ROS 2 SDK with full walking/trotting gaits.",
        specs: { dof: "12", speed: "5 m/s", payload: "8 kg", runtime: "1 hr", weight: "15 kg" },
        price: "~$1,600–$3,750",
        ros: "ros2",
        locomotion: "quadruped",
        weightKg: 15,
        payloadKg: 8,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data", "fleet"],
      }),
      hw("unitree-b2", {
        name: "Unitree B2",
        manufacturer: "Unitree Robotics",
        desc: "High-performance quadruped. 6 m/s, 20 kg payload, 2 hr runtime, IP67 rating. Industrial inspection and security.",
        specs: { dof: "12", speed: "6 m/s", payload: "20 kg", runtime: "2 hr", rating: "IP67" },
        price: "~$100,000",
        ros: "ros2",
        locomotion: "quadruped",
        weightKg: 60,
        payloadKg: 20,
        supportedProducts: ["serve", "view", "autonomy", "pilot", "data", "fleet"],
      }),
      hw("boston-dynamics-spot", {
        name: "Boston Dynamics Spot",
        manufacturer: "Boston Dynamics",
        desc: "The industrial-standard quadruped. Dynamic sensing, autonomous navigation, dock charging, and a rich payload ecosystem. API + SDK for custom autonomy.",
        specs: { dof: "12", speed: "1.6 m/s", payload: "14 kg", runtime: "90 min", rating: "IP54" },
        price: "~$75,000+",
        ros: "custom",
        locomotion: "quadruped",
        weightKg: 32.5,
        payloadKg: 14,
        supportedProducts: ["pilot", "view", "data", "fleet"],
      }),
      hw("anymal-d", {
        name: "ANYmal D",
        manufacturer: "ANYbotics",
        desc: "Swiss-engineered quadruped for industrial inspection. Full autonomy with 360° perception, stair climbing, and ATEX certification option.",
        specs: { dof: "12", speed: "1.3 m/s", payload: "10 kg", runtime: "90 min", rating: "IP67" },
        price: "~$150,000+",
        ros: "ros2",
        locomotion: "quadruped",
        weightKg: 35,
        payloadKg: 10,
        supportedProducts: ["view", "autonomy", "data", "fleet"],
      }),
    ],
  },
  {
    id: "legged-hexapod",
    name: "Hexapod",
    category: "legged" as RobotCategoryId,
    tagline: "Six-legged robots — extreme stability and payload.",
    description:
      "Six legs provide static stability — the robot can walk while keeping 3+ feet on the ground. Used for heavy-duty inspection, search and rescue, and terrain that defeats even quadrupeds.",
    hardwareModels: [
      hw("phantomx-ax", {
        name: "PhantomX AX Hexapod",
        manufacturer: "Trossen Robotics / Interbotix",
        desc: "Open-source 18-DOF hexapod. Dynamixel AX-12A servos, ROS-compatible. Research platform for gait generation and terrain adaptation.",
        specs: { dof: "18", servos: "AX-12A", weight: "2.5 kg", speed: "0.3 m/s" },
        price: "~$1,500",
        ros: "ros2",
        locomotion: "hexapod",
        weightKg: 2.5,
        payloadKg: 1.5,
        supportedProducts: ["build", "serve", "pilot"],
      }),
    ],
  },

  // ═══ HUMANOID ROBOTS ══════════════════════════════════════════════════════
  {
    id: "humanoid-full",
    name: "Full Humanoid",
    category: "humanoid" as RobotCategoryId,
    tagline: "Bipedal humanoid robots — the frontier of embodied AI.",
    description:
      "Full-scale humanoid robots with two legs, two arms, and dexterous hands. The ultimate platform for general-purpose embodied AI — manipulation, navigation, and human-robot interaction in human-designed environments.",
    hardwareModels: [
      hw("unitree-h1", {
        name: "Unitree H1",
        manufacturer: "Unitree Robotics",
        desc: "General-purpose humanoid. 51-DOF, 19-DOF dexterous hands, 5 km/h walking, 3-D LiDAR + depth cameras. Open SDK.",
        specs: { dof: "51", height: "180 cm", weight: "47 kg", payload: "10 kg/arm", speed: "5 km/h" },
        price: "~$90,000",
        ros: "custom",
        locomotion: "bipedal",
        weightKg: 47,
        payloadKg: 10,
        hasArm: true,
        supportedProducts: ["serve", "view", "pilot", "data", "fleet"],
      }),
      hw("unitree-g1", {
        name: "Unitree G1",
        manufacturer: "Unitree Robotics",
        desc: "Compact humanoid. 23-43 DOF, 3-finger dexterous hands, 14 cm depth camera. Targeted at home and service use.",
        specs: { dof: "23–43", height: "127 cm", weight: "35 kg", payload: "2 kg/arm" },
        price: "~$16,000",
        ros: "custom",
        locomotion: "bipedal",
        weightKg: 35,
        payloadKg: 2,
        hasArm: true,
        supportedProducts: ["serve", "view", "pilot", "data"],
      }),
      hw("figure-02", {
        name: "Figure 02",
        manufacturer: "Figure AI",
        desc: "Electric humanoid with human-scale dexterity. Built for industrial work — 16-DOF hands, 2.5-hr runtime, integrated AI with speech.",
        specs: { dof: "30+", height: "170 cm", weight: "60 kg", payload: "20 kg", runtime: "5 hr" },
        price: "Contact manufacturer",
        ros: "custom",
        locomotion: "bipedal",
        weightKg: 60,
        payloadKg: 20,
        hasArm: true,
        supportedProducts: ["serve", "pilot", "data"],
      }),
      hw("fourier-gr2", {
        name: "Fourier GR-2",
        manufacturer: "Fourier Intelligence",
        desc: "Humanoid with 53-DOF, 12-DOF dexterous hands with tactile sensing. 1.75 m tall, 63 kg, designed for healthcare and service.",
        specs: { dof: "53", height: "175 cm", weight: "63 kg", payload: "5 kg/arm" },
        price: "Contact manufacturer",
        ros: "ros2",
        locomotion: "bipedal",
        weightKg: 63,
        payloadKg: 5,
        hasArm: true,
        supportedProducts: ["serve", "pilot", "data"],
      }),
    ],
  },

  // ═══ TRACKED ROBOTS ═══════════════════════════════════════════════════════
  {
    id: "tracked-ugv",
    name: "Tracked UGV",
    category: "tracked" as RobotCategoryId,
    tagline: "Tank-tread ground vehicles — conquer any terrain.",
    description:
      "Tracked platforms for rough terrain, stairs, mud, and snow. Higher traction and lower ground pressure than wheeled robots. Used for military, search and rescue, mining, and heavy outdoor logistics.",
    hardwareModels: [
      hw("clearpath-warthog", {
        name: "Clearpath Warthog",
        manufacturer: "Clearpath Robotics",
        desc: "Heavy-duty amphibious tracked UGV. 272 kg payload, 18 kW drivetrain, IP65. Built for mining, agriculture, and defense.",
        specs: { payload: "272 kg", power: "18 kW", speed: "18 km/h", rating: "IP65" },
        price: "~$80,000+",
        ros: "ros2",
        locomotion: "tracked",
        weightKg: 280,
        payloadKg: 272,
        supportedProducts: ["serve", "view", "autonomy", "fleet"],
      }),
      hw("clearpath-jackal", {
        name: "Clearpath Jackal",
        manufacturer: "Clearpath Robotics",
        desc: "Compact, weatherproof tracked UGV. 20 kg payload, IP62. ROS 2-native. Used for field robotics research.",
        specs: { speed: "2 m/s", payload: "20 kg", rating: "IP62", runtime: "4 hr" },
        price: "~$20,000+",
        ros: "ros2",
        locomotion: "tracked",
        weightKg: 17,
        payloadKg: 20,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data", "fleet"],
      }),
    ],
  },

  // ═══ MARINE ROBOTS ════════════════════════════════════════════════════════
  {
    id: "marine-asv",
    name: "Autonomous Surface Vessel",
    category: "marine" as RobotCategoryId,
    tagline: "Unmanned surface vessels for ocean and inland water operations.",
    description:
      "Autonomous boats for hydrographic surveying, environmental monitoring, bathymetry, and maritime security. Operate on lakes, rivers, harbors, and coastal waters.",
    hardwareModels: [
      hw("heron-asv", {
        name: "Clearpath Heron",
        manufacturer: "Clearpath Robotics",
        desc: "ASV for bathymetric and environmental survey. Differential thrust, 3-hr endurance, payload bay with plug-and-play sensors.",
        specs: { endurance: "3 hr", speed: "1.7 m/s", payload: "10 kg", sensors: "GPS, IMU, sonar" },
        price: "~$30,000+",
        ros: "ros2",
        locomotion: "marine-thruster",
        weightKg: 27,
        payloadKg: 10,
        supportedProducts: ["view", "autonomy", "data", "fleet"],
      }),
    ],
  },

  // ═══ INDUSTRIAL ARMS ══════════════════════════════════════════════════════
  {
    id: "arm-collaborative",
    name: "Collaborative Robot Arm",
    category: "industrial-arm" as RobotCategoryId,
    tagline: "Cobots — safe, teachable robot arms for manufacturing and labs.",
    description:
      "Collaborative robots (cobots) are designed to work alongside humans without safety cages. Used for pick-and-place, assembly, quality inspection, lab automation, and CNC tending.",
    hardwareModels: [
      hw("ur5e", {
        name: "Universal Robots UR5e",
        manufacturer: "Universal Robots",
        desc: "6-DOF cobot. 5 kg payload, 850 mm reach, ±0.03 mm repeatability. ROS 2 driver via ur_robot_driver. The industry-standard cobot.",
        specs: { dof: "6", payload: "5 kg", reach: "850 mm", repeatability: "±0.03 mm", weight: "20.6 kg" },
        price: "~$35,000",
        ros: "ros2",
        locomotion: "fixed-base",
        weightKg: 20.6,
        payloadKg: 5,
        hasArm: true,
        supportedProducts: ["build", "frame", "bench", "serve", "view", "pilot", "data", "train"],
      }),
      hw("ur10e", {
        name: "Universal Robots UR10e",
        manufacturer: "Universal Robots",
        desc: "Heavy-payload cobot. 12.5 kg payload, 1300 mm reach. ROS 2 driver available. Ideal for palletizing and machine tending.",
        specs: { dof: "6", payload: "12.5 kg", reach: "1300 mm", repeatability: "±0.05 mm", weight: "33.5 kg" },
        price: "~$45,000",
        ros: "ros2",
        locomotion: "fixed-base",
        weightKg: 33.5,
        payloadKg: 12.5,
        hasArm: true,
        supportedProducts: ["build", "frame", "bench", "serve", "view", "pilot", "data"],
      }),
      hw("franka-panda", {
        name: "Franka Research 3",
        manufacturer: "Franka Robotics",
        desc: "7-DOF torque-controlled research arm. 3 kg payload, 855 mm reach, joint torque sensing. Gold-standard for manipulation research.",
        specs: { dof: "7", payload: "3 kg", reach: "855 mm", torqueSensing: "All 7 joints" },
        price: "~$27,000",
        ros: "ros2",
        locomotion: "fixed-base",
        weightKg: 18,
        payloadKg: 3,
        hasArm: true,
        supportedProducts: ["build", "frame", "serve", "pilot", "data", "train"],
      }),
      hw("kinova-gen3", {
        name: "Kinova Gen3",
        manufacturer: "Kinova Robotics",
        desc: "6 or 7-DOF lightweight arm. 4 kg payload, 902 mm reach. Full ROS 2 support, open architecture. Designed for research and assistive tech.",
        specs: { dof: "6 / 7", payload: "4 kg", reach: "902 mm", weight: "7.2 kg" },
        price: "~$35,000",
        ros: "ros2",
        locomotion: "fixed-base",
        weightKg: 7.2,
        payloadKg: 4,
        hasArm: true,
        supportedProducts: ["build", "frame", "serve", "pilot", "data", "train"],
      }),
      hw("dobot-cr10", {
        name: "Dobot CR10",
        manufacturer: "Dobot",
        desc: "6-DOF cobot. 10 kg payload, 1525 mm reach. Cost-effective alternative for education, light industry, and robotics labs.",
        specs: { dof: "6", payload: "10 kg", reach: "1525 mm", repeatability: "±0.03 mm" },
        price: "~$14,000",
        ros: "ros2",
        locomotion: "fixed-base",
        weightKg: 40,
        payloadKg: 10,
        hasArm: true,
        supportedProducts: ["build", "frame", "serve", "pilot", "data"],
      }),
      hw("so101-arm", {
        name: "SO-101 (LeRobot 6-DOF)",
        manufacturer: "LeRobot / Open Source",
        desc: "Open-source 3D-printed 6-DOF arm with Feetech STS3215 servos. 0.62 m reach, 0.5 kg payload. Designed for imitation-learning research.",
        specs: { dof: "6", payload: "0.5 kg", reach: "0.62 m", servos: "STS3215 ×6" },
        price: "~$500",
        ros: "ros2",
        locomotion: "fixed-base",
        weightKg: 1.1,
        payloadKg: 0.5,
        hasArm: true,
        supportedProducts: ["build", "frame", "bench", "serve", "view", "pilot", "data", "train"],
      }),
    ],
  },
  {
    id: "arm-delta",
    name: "Delta Robot",
    category: "industrial-arm" as RobotCategoryId,
    tagline: "High-speed parallel robot for pick-and-place and sorting.",
    description:
      "Delta robots use three parallelograms for extreme speed in lightweight pick-and-place. Used in food packaging, pharmaceutical sorting, and electronics assembly.",
    hardwareModels: [
      hw("abb-irb360", {
        name: "ABB IRB 360 FlexPicker",
        manufacturer: "ABB Robotics",
        desc: "Industry-standard delta robot. 1-8 kg payload, up to 200 picks/min. Hygienic design for food handling.",
        specs: { payload: "1–8 kg", speed: "200 picks/min", reach: "1130 mm", ip: "IP69K option" },
        price: "~$40,000+",
        ros: "custom",
        locomotion: "fixed-base",
        weightKg: 120,
        payloadKg: 8,
        hasArm: true,
        supportedProducts: ["pilot", "fleet"],
      }),
    ],
  },

  // ═══ MOBILE MANIPULATORS ══════════════════════════════════════════════════
  {
    id: "mobile-manip-full",
    name: "Full Mobile Manipulator",
    category: "mobile-manipulator" as RobotCategoryId,
    tagline: "Wheels + arm — the complete embodied-AI platform.",
    description:
      "A mobile base with a mounted manipulator arm. These are the fully-capable robots — they navigate to a location, then use the arm to interact with the world. The reference platform for embodied AI research and deployment.",
    hardwareModels: [
      hw("omnibot-pro", {
        name: "OmniBot Pro (Reference)",
        manufacturer: "OhhO / Open Source",
        desc: "Yahboom X3 mecanum base + SO-101 6-DOF arm. RPi 5 brain, depth + wrist cameras, full ROS 2 + VLA stack. The complete reference platform.",
        specs: { base: "mecanum", arm: "SO-101, 6-DOF", speed: "0.2 m/s", payload: "0.5 kg", compute: "RPi 5" },
        price: "~$1,500",
        ros: "ros2",
        locomotion: "mecanum",
        weightKg: 5,
        payloadKg: 0.5,
        hasArm: true,
        supportedProducts: ["build", "frame", "bench", "serve", "view", "autonomy", "pilot", "data", "train", "fleet", "comply", "proof", "shield"],
      }),
      hw("stretch-re2", {
        name: "Hello Robot Stretch RE2",
        manufacturer: "Hello Robot Inc.",
        desc: "Low-cost mobile manipulator for home/office research. Telescoping arm, compliant gripper, Intel RealSense, ROS 2-native.",
        specs: { base: "differential", arm: "telescoping, 3-DOF", reach: "1.15 m", payload: "1.5 kg" },
        price: "~$20,000",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 24.5,
        payloadKg: 1.5,
        hasArm: true,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data", "train"],
      }),
      hw("fetch-mp", {
        name: "Fetch Mobile Manipulator",
        manufacturer: "Zebra / Fetch Robotics",
        desc: "Warehouse mobile manipulator. Differential base + 7-DOF arm, 150 kg payload on base, 6 kg on arm. Designed for logistics AMR research.",
        specs: { base: "differential", arm: "7-DOF", reach: "940 mm", payload: "6 kg (arm)", speed: "1 m/s" },
        price: "~$100,000+",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 113,
        payloadKg: 6,
        hasArm: true,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data", "fleet"],
      }),
      hw("tracer-arm", {
        name: "AgileX Tracer + Arm",
        manufacturer: "AgileX Robotics",
        desc: "Mini differential base with optional 4/6-DOF arm. ROS 2 drivers, modular payload, compact for labs and classrooms.",
        specs: { base: "differential", arm: "4–6 DOF option", speed: "2.6 m/s", payload: "100 kg (base)" },
        price: "~$8,000–20,000",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 25,
        payloadKg: 2,
        hasArm: true,
        supportedProducts: ["build", "frame", "serve", "view", "autonomy", "pilot", "data", "fleet"],
      }),
    ],
  },

  // ═══ SWARM ROBOTS ═════════════════════════════════════════════════════════
  {
    id: "swarm-small",
    name: "Small Swarm Robot",
    category: "swarm" as RobotCategoryId,
    tagline: "Tiny robots that collaborate — collective intelligence at scale.",
    description:
      "Small, inexpensive robots that work together as a collective. Used for warehouse inventory, search and rescue, educational robotics, and research on emergent behavior and distributed algorithms.",
    hardwareModels: [
      hw("khepera-iv", {
        name: "Khepera IV",
        manufacturer: "K-Team / GCtronic",
        desc: "Classic swarm research robot. 2 wheels, 12 IR sensors, camera, Linux onboard. ROS-compatible. Used in hundreds of swarm research papers.",
        specs: { sensors: "12 IR, 5 ultrasonic", camera: "VGA", compute: "ARM Linux", weight: "540 g" },
        price: "~$3,500/unit",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 0.54,
        payloadKg: 0.3,
        supportedProducts: ["serve", "pilot", "data", "fleet"],
      }),
      hw("e-puck2", {
        name: "E-Puck2",
        manufacturer: "GCtronic",
        desc: "Desktop swarm robot. Differential drive, 8 IR proximity, ToF sensor, microphone, speaker, WiFi. Huge open-source ecosystem.",
        specs: { sensors: "8 IR, ToF, IMU", camera: "VGA", compute: "STM32F4", weight: "130 g" },
        price: "~$1,000/unit",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 0.13,
        payloadKg: 0,
        supportedProducts: ["serve", "pilot", "data"],
      }),
    ],
  },

  // ═══ AGRICULTURAL ROBOTS ══════════════════════════════════════════════════
  {
    id: "agri-precision",
    name: "Precision Agriculture",
    category: "agricultural" as RobotCategoryId,
    tagline: "Farming robots — plant, monitor, weed, and harvest autonomously.",
    description:
      "Autonomous platforms for precision agriculture — targeted weeding, selective harvesting, soil analysis, and crop monitoring. Reduce chemical use and labor costs through robotic precision.",
    hardwareModels: [
      hw("farmbot", {
        name: "FarmBot Genesis",
        manufacturer: "FarmBot Inc.",
        desc: "Open-source CNC farming robot. Cartesian gantry plants, waters, and weeds a garden bed. Fully open hardware + software.",
        specs: { area: "1.5 m² – 18 m²", precision: "±0.08 cm", tools: "seeder, weeder, waterer, sensor", weight: "—" },
        price: "~$3,000",
        ros: "custom",
        locomotion: "cartesian-gantry",
        weightKg: 10,
        payloadKg: 1,
        supportedProducts: ["build", "pilot", "data"],
      }),
      hw("ecorobotix-avo", {
        name: "ecoRobotix AVO",
        manufacturer: "ecoRobotix / CNH",
        desc: "Autonomous weeding robot. Solar-powered, vision-based precision spraying. Treats only weeds, reducing herbicide by 90%.",
        specs: { coverage: "10 ha/day", precision: "6 cm", power: "solar", width: "1.5 m" },
        price: "~$50,000+",
        ros: "custom",
        locomotion: "differential",
        weightKg: 130,
        payloadKg: 60,
        supportedProducts: ["view", "data", "fleet"],
      }),
    ],
  },

  // ═══ UNDERWATER ROVs ══════════════════════════════════════════════════════
  {
    id: "underwater-rov-inspection",
    name: "Inspection ROV",
    category: "underwater-rov" as RobotCategoryId,
    tagline: "Subsea robots — inspect, maintain, and explore underwater.",
    description:
      "Remotely operated underwater vehicles for dam inspection, hull cleaning, aquaculture monitoring, pipeline survey, and marine research. Depth ratings from 100 m to 6,000 m.",
    hardwareModels: [
      hw("bluerov2", {
        name: "BlueROV2",
        manufacturer: "Blue Robotics",
        desc: "Open-source ROV. 6 thrusters, HD camera, 100 m depth rating. Full ROS 2 integration via ArduSub. The standard for affordable underwater robotics.",
        specs: { depth: "100 m", thrusters: "T200 ×6", camera: "1080p HD", weight: "10 kg" },
        price: "~$5,000",
        ros: "ros2",
        locomotion: "underwater-6thruster",
        weightKg: 10,
        payloadKg: 2,
        supportedProducts: ["build", "frame", "serve", "view", "pilot", "data", "fleet"],
      }),
      hw("bluerov2-heavy", {
        name: "BlueROV2 Heavy",
        manufacturer: "Blue Robotics",
        desc: "Heavy-configuration ROV with 8 T200 thrusters, gripper, scanning sonar, and 300 m depth rating for inspection missions.",
        specs: { depth: "300 m", thrusters: "T200 ×8", gripper: "electric 2-jaw", sonar: "Ping360 scanning" },
        price: "~$15,000",
        ros: "ros2",
        locomotion: "underwater-8thruster",
        weightKg: 15,
        payloadKg: 4,
        hasArm: true,
        supportedProducts: ["serve", "view", "pilot", "data", "fleet"],
      }),
    ],
  },

  // ═══ SPACE ROBOTICS ═══════════════════════════════════════════════════════
  {
    id: "space-rover",
    name: "Planetary Rover",
    category: "space" as RobotCategoryId,
    tagline: "Exploring other worlds — robotic rovers for planetary science.",
    description:
      "Autonomous rovers for planetary exploration, lunar construction, and orbital servicing. Designed for extreme environments with radiation hardening, thermal management, and multi-year autonomy.",
    hardwareModels: [
      hw("exomy", {
        name: "ExoMy Rover",
        manufacturer: "ESA / Open Source",
        desc: "3D-printed Mars rover replica by ESA. 6 wheels with rocker-bogie suspension, RPi compute, stereo cameras. Educational + research platform.",
        specs: { wheels: "6, rocker-bogie", compute: "RPi 4", camera: "stereo", weight: "2.5 kg" },
        price: "~$800 (DIY)",
        ros: "ros2",
        locomotion: "rocker-bogie",
        weightKg: 2.5,
        payloadKg: 0.5,
        supportedProducts: ["build", "frame", "serve", "view", "pilot", "data"],
      }),
      hw("nasa-jpl-open-rover", {
        name: "JPL Open Source Rover",
        manufacturer: "NASA JPL / Open Source",
        desc: "NASA JPL's open-source 6-wheel rover design. Rocker-bogie suspension, corner steering. Used for education and outreach.",
        specs: { wheels: "6, rocker-bogie", compute: "RPi", steering: "4-corner", weight: "11 kg" },
        price: "~$2,500 (DIY)",
        ros: "ros2",
        locomotion: "rocker-bogie",
        weightKg: 11,
        payloadKg: 5,
        supportedProducts: ["build", "frame", "serve", "view", "pilot", "data"],
      }),
    ],
  },

  // ═══ MEDICAL ROBOTS ═══════════════════════════════════════════════════════
  {
    id: "medical-surgical",
    name: "Surgical Robot",
    category: "medical" as RobotCategoryId,
    tagline: "Precision surgical platforms — teleoperated and autonomous.",
    description:
      "Robotic surgical systems that extend the surgeon's capabilities — tremor filtering, motion scaling, and 3-D HD visualization. Used across urology, gynecology, orthopedics, and general surgery.",
    hardwareModels: [
      hw("dvrk", {
        name: "da Vinci Research Kit (dVRK)",
        manufacturer: "Intuitive / Open Source",
        desc: "Open-source research platform based on the da Vinci Surgical System. ROS-based control, stereo endoscope, 7-DOF manipulators. Used in 100+ research labs worldwide.",
        specs: { arms: "2-4 PSM + ECM", dof: "7 per arm", stereo: "HD 3-D", control: "ROS" },
        price: "Research grant / loan",
        ros: "ros1",
        locomotion: "fixed-base",
        weightKg: 200,
        payloadKg: 2,
        hasArm: true,
        supportedProducts: ["serve", "pilot", "data", "comply"],
      }),
    ],
  },
  {
    id: "medical-rehab",
    name: "Rehabilitation Robot",
    category: "medical" as RobotCategoryId,
    tagline: "Assistive robotics — exoskeletons and therapy platforms.",
    description:
      "Robots for physical rehabilitation, mobility assistance, and prosthetics. Used in post-stroke gait training, spinal cord injury recovery, and powered exoskeleton mobility.",
    hardwareModels: [
      hw("myomo-myopro", {
        name: "MyoPro Orthosis",
        manufacturer: "Myomo Inc.",
        desc: "Powered arm orthosis that reads surface EMG signals to assist arm movement. FDA-cleared for stroke, SCI, and neuromuscular conditions.",
        specs: { dof: "elbow + hand", sensors: "sEMG", control: "intent-based", weight: "1.8 kg" },
        price: "Insurance-billed",
        ros: "custom",
        locomotion: "wearable",
        weightKg: 1.8,
        payloadKg: 0.5,
        hasArm: true,
        supportedProducts: ["serve", "data"],
      }),
    ],
  },

  // ═══ DELIVERY ROBOTS ══════════════════════════════════════════════════════
  {
    id: "delivery-sidewalk",
    name: "Sidewalk Delivery",
    category: "delivery" as RobotCategoryId,
    tagline: "Last-mile autonomous delivery — sidewalks and pedestrian zones.",
    description:
      "Compact, sidewalk-navigating delivery robots for food, groceries, and parcels. Speed-limited for pedestrian safety, with curb-dropping capability and remote human oversight.",
    hardwareModels: [
      hw("starship-robot", {
        name: "Starship Delivery Robot",
        manufacturer: "Starship Technologies",
        desc: "The most deployed autonomous delivery robot. 6 wheels, curb-climbing, insulated cargo hold. Millions of commercial deliveries completed.",
        specs: { speed: "6 km/h", range: "6 km", cargo: "10 kg", sensors: "12 cameras, ultrasonic, radar" },
        price: "Contact manufacturer",
        ros: "custom",
        locomotion: "differential-6wheel",
        weightKg: 35,
        payloadKg: 10,
        supportedProducts: ["view", "autonomy", "fleet"],
      }),
      hw("coco-robot", {
        name: "Coco Delivery Robot",
        manufacturer: "Coco (formerly Kiwibot)",
        desc: "Compact pink sidewalk delivery robot. Emphasizes safe pedestrian interaction with expressive displays and remote-assist capability.",
        specs: { speed: "walking pace", cargo: "5 kg", sensors: "cameras + LiDAR", display: "interactive screen" },
        price: "Contact manufacturer",
        ros: "ros2",
        locomotion: "differential",
        weightKg: 20,
        payloadKg: 5,
        supportedProducts: ["view", "autonomy", "fleet"],
      }),
    ],
  },

  // ═══ INSPECTION ROBOTS ════════════════════════════════════════════════════
  {
    id: "inspection-pipe",
    name: "Pipeline Inspection",
    category: "inspection" as RobotCategoryId,
    tagline: "Confined-space inspection — pipes, tanks, and infrastructure.",
    description:
      "Robots designed to crawl inside pipes, ducts, tanks, and confined spaces for inspection and maintenance. Used in oil & gas, water utilities, HVAC, and nuclear decommissioning.",
    hardwareModels: [
      hw("inyo-crawler", {
        name: "Inyodrive Pipe Crawler",
        manufacturer: "Inyodrive / Custom",
        desc: "Modular pipe inspection robot. Tracks or magnetic wheels, PTZ camera, gas sensors. Customizable diameter and length for different pipe sizes.",
        specs: { pipeDiameter: "100–1000 mm", drive: "tracked / magnetic", camera: "PTZ HD", sensors: "gas, thermal" },
        price: "~$15,000–50,000",
        ros: "ros2",
        locomotion: "tracked",
        weightKg: 8,
        payloadKg: 3,
        supportedProducts: ["view", "pilot", "data", "fleet"],
      }),
      hw("flyability-elios3", {
        name: "Flyability Elios 3",
        manufacturer: "Flyability",
        desc: "Caged indoor inspection drone. Collision-tolerant protective cage, 4K PTZ camera, LiDAR payload, SLAM-based stabilization. For confined-space inspection without human entry.",
        specs: { flightTime: "10 min", camera: "4K, thermal option", lighting: "10K lumens", rating: "collision-tolerant" },
        price: "~$45,000+",
        ros: "custom",
        locomotion: "quadrotor-caged",
        weightKg: 1.8,
        payloadKg: 0.3,
        supportedProducts: ["view", "pilot", "data", "fleet"],
      }),
    ],
  },
];

// ── Lookups ──────────────────────────────────────────────────────────────────

const TYPE_BY_ID: Record<string, RobotType> = Object.fromEntries(
  ROBOT_TYPES.map((t) => [t.id, t]),
);

const HW_BY_ID: Record<string, { hw: HardwareModel; type: RobotType }> = {};
for (const t of ROBOT_TYPES) {
  for (const hw of t.hardwareModels) {
    HW_BY_ID[hw.id] = { hw, type: t };
  }
}

export function getRobotType(id: string): RobotType | undefined {
  return TYPE_BY_ID[id];
}

export function getHardwareModel(id: string): HardwareModel | undefined {
  return HW_BY_ID[id]?.hw;
}

export function getRobotTypeForHardware(hwId: string): RobotType | undefined {
  return HW_BY_ID[hwId]?.type;
}

export function robotTypesByCategory(categoryId: string): RobotType[] {
  return ROBOT_TYPES.filter((t) => t.category === categoryId);
}

export function searchRobots(query: string): HardwareModel[] {
  const q = query.toLowerCase();
  const results: HardwareModel[] = [];
  for (const t of ROBOT_TYPES) {
    for (const hw of t.hardwareModels) {
      if (
        hw.name.toLowerCase().includes(q) ||
        hw.manufacturer.toLowerCase().includes(q) ||
        hw.desc.toLowerCase().includes(q) ||
        t.name.toLowerCase().includes(q)
      ) {
        results.push(hw);
      }
    }
  }
  return results;
}
