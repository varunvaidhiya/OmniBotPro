/*
 * OhhO Bridge — adapter catalog, joint-index maps and topic definitions.
 *
 * Pure data (no React, no hooks) so it imports from server and client alike.
 * The Bridge console reads this to render the adapter list, the per-model
 * joint map, the ROS 2 topic table, and the impedance-gain defaults.
 */

export type AdapterStatus = "active" | "idle" | "error" | "unavailable";

export interface BridgeAdapter {
  id: string;
  brand: string;
  protocol: string;
  /** Display name, e.g. "Unitree DDS". */
  name: string;
  /** Which robot models this adapter covers. */
  models: string[];
  /** Native SDK / library the adapter wraps. */
  sdk: string;
  /** DDS / transport layer the native SDK uses. */
  transport: string;
  status: AdapterStatus;
  /** Live bridge latency in ms (0 when idle). */
  latencyMs: number;
  /** Live jitter in ms (0 when idle). */
  jitterMs: number;
  /** Messages/sec flowing through the bridge (0 when idle). */
  msgRate: number;
  accent: string;
  blurb: string;
}

export interface JointMapEntry {
  index: number;
  nativeName: string;
  rosName: string;
  /** Default position-control kp. */
  kp: number;
  /** Default position-control kd. */
  kd: number;
  /** Body region — leg, arm, waist, hand. */
  region: string;
}

export interface RosTopic {
  name: string;
  type: string;
  direction: "native-to-ros" | "ros-to-native";
  rate: string;
}

export const ADAPTERS: BridgeAdapter[] = [
  {
    id: "unitree-dds",
    brand: "Unitree",
    protocol: "DDS (Cyclone)",
    name: "Unitree DDS",
    models: ["G1", "G1-EDU", "H1", "H1-2", "H2", "R1", "Go2", "B2", "A2"],
    sdk: "unitree_sdk2 / unitree_sdk2_python",
    transport: "Cyclone DDS",
    status: "active",
    latencyMs: 2.1,
    jitterMs: 0.3,
    msgRate: 48,
    accent: "#00D4FF",
    blurb: "Translates LowCmd/LowState and HighCmd/HighState IDL to standard ROS 2 topics, with per-model joint-index maps.",
  },
  {
    id: "dji-mavlink",
    brand: "DJI",
    protocol: "MAVLink",
    name: "DJI MAVLink",
    models: ["Matrice 300", "Matrice 350", "Mavic 3 Enterprise", "Mini 3 Pro"],
    sdk: "mavsdk + pymavlink",
    transport: "MAVLink v2 over UART/UDP",
    status: "idle",
    latencyMs: 0,
    jitterMs: 0,
    msgRate: 0,
    accent: "#A78BFA",
    blurb: "Bridges MAVLink heartbeat, attitude, global position and manual control to ROS 2 Imu, Odometry and Twist.",
  },
  {
    id: "modbus-arm",
    brand: "Industrial",
    protocol: "Modbus TCP/RTU",
    name: "Modbus Arm",
    models: ["Generic SCARA", "Generic 6-DOF PLC arm"],
    sdk: "pymodbus",
    transport: "Modbus TCP / RTU over Ethernet",
    status: "idle",
    latencyMs: 0,
    jitterMs: 0,
    msgRate: 0,
    accent: "#FBBF24",
    blurb: "Bridges Modbus holding registers for joint positions and status to standard ROS 2 JointState and JointTrajectory.",
  },
  {
    id: "ethercat-arm",
    brand: "Industrial",
    protocol: "EtherCAT",
    name: "EtherCAT Arm",
    models: ["Universal Robots UR5e", "UR10e", "Franka Emika Panda"],
    sdk: "ros2_control / EtherCAT master",
    transport: "EtherCAT (SOEM)",
    status: "unavailable",
    latencyMs: 0,
    jitterMs: 0,
    msgRate: 0,
    accent: "#34D399",
    blurb: "Real-time EtherCAT bridge for ROS 2 ros2_control — hardware interface + joint trajectory controller.",
  },
  {
    id: "yahboom-serial",
    brand: "Yahboom",
    protocol: "Serial",
    name: "Yahboom Serial",
    models: ["OmniBot", "Rosmaster X3", "Raspbot"],
    sdk: "yahboom_ros2 (browser codec)",
    transport: "USB serial @ 115200 baud",
    status: "active",
    latencyMs: 4.2,
    jitterMs: 0.8,
    msgRate: 20,
    accent: "#00D4FF",
    blurb: "Browser-native Web Serial codec for the Yahboom packet protocol — no Pi, no ROS, talks firmware directly.",
  },
  {
    id: "canopen",
    brand: "Industrial",
    protocol: "CAN bus / CANopen",
    name: "CANopen (CiA 402)",
    models: ["Generic CANopen motor base", "AGV/AMR drives", "Embedded mobile robots"],
    sdk: "python-can + canopen",
    transport: "CAN bus (SocketCAN / CAN-USB)",
    status: "active",
    latencyMs: 3.5,
    jitterMs: 0.5,
    msgRate: 100,
    accent: "#00D4FF",
    blurb: "Translates CANopen object dictionaries (CiA 402 motion profile, SDO/PDO) to ROS 2 JointState, Twist and Odometry — the standard protocol for mobile robot motor controllers and AGVs.",
  },
  {
    id: "opcua",
    brand: "Industrial",
    protocol: "OPC UA",
    name: "OPC UA",
    models: ["Generic OPC UA server", "Siemens Simatic", "Factory cells", "MES/SCADA"],
    sdk: "asyncua (python-opcua)",
    transport: "OPC UA over TCP",
    status: "active",
    latencyMs: 8.0,
    jitterMs: 1.2,
    msgRate: 50,
    accent: "#A78BFA",
    blurb: "Browses an OPC UA server's address space and maps nodes to ROS 2 topics — Industry 4.0 factory integration, digital twin communication and MES/SCADA connectivity.",
  },
  {
    id: "profinet",
    brand: "Industrial",
    protocol: "PROFINET",
    name: "PROFINET",
    models: ["Siemens S7 PLCs", "PROFINET I/O devices", "German automotive cells"],
    sdk: "python-profinet + ros2_control",
    transport: "PROFINET IO (real-time Ethernet)",
    status: "idle",
    latencyMs: 0,
    jitterMs: 0,
    msgRate: 0,
    accent: "#FBBF24",
    blurb: "Real-time PROFINET bridge for European/Siemens manufacturing — maps I/O controller data to ROS 2 topics for factory floor integration.",
  },
  {
    id: "ethernet-ip",
    brand: "Industrial",
    protocol: "EtherNet/IP",
    name: "EtherNet/IP (CIP)",
    models: ["Rockwell/Allen-Bradley PLCs", "CIP devices", "North American factory cells"],
    sdk: "cppp + ros2_control",
    transport: "EtherNet/IP (CIP over TCP/UDP)",
    status: "idle",
    latencyMs: 0,
    jitterMs: 0,
    msgRate: 0,
    accent: "#34D399",
    blurb: "EtherNet/IP bridge for North American/Rockwell manufacturing — CIP message routing to ROS 2 topics for factory floor integration.",
  },
  {
    id: "mqtt",
    brand: "IoT",
    protocol: "MQTT",
    name: "MQTT",
    models: ["AWS IoT", "Azure IoT Hub", "Generic MQTT brokers", "Cloud fleet systems"],
    sdk: "paho-mqtt",
    transport: "MQTT over TCP/TLS",
    status: "active",
    latencyMs: 12.0,
    jitterMs: 2.0,
    msgRate: 30,
    accent: "#00D4FF",
    blurb: "Bridges MQTT pub/sub to ROS 2 topics — cloud IoT connectivity, fleet telemetry and the transport layer for VDA 5050.",
  },
  {
    id: "vda5050",
    brand: "Industrial",
    protocol: "VDA 5050",
    name: "VDA 5050 (AGV/AMR fleet)",
    models: ["Generic AGV/AMR", "Warehouse fleets", "Master control systems"],
    sdk: "vda5050-python + paho-mqtt",
    transport: "VDA 5050 JSON over MQTT/HTTP",
    status: "active",
    latencyMs: 15.0,
    jitterMs: 3.0,
    msgRate: 10,
    accent: "#A78BFA",
    blurb: "Speaks the VDA 5050 AGV/AMR fleet standard — interoperate with warehouse management systems and master control from Linde, Toyota, MiR, KION and the VDA 5050 ecosystem.",
  },
  {
    id: "ros-industrial",
    brand: "Industrial",
    protocol: "ROS-Industrial",
    name: "ROS-Industrial arms",
    models: ["Fanuc", "ABB", "KUKA", "Yaskawa Motoman", "Universal Robots"],
    sdk: "ros_industrial_clients",
    transport: "Vendor-specific over TCP/EtherCAT",
    status: "active",
    latencyMs: 5.0,
    jitterMs: 0.8,
    msgRate: 125,
    accent: "#FBBF24",
    blurb: "Compatibility with the ROS-Industrial consortium driver ecosystem — Fanuc, ABB, KUKA, Yaskawa and UR arms appear as standard ROS 2 robots with no vendor SDK lock-in.",
  },
];

// ── Joint-index maps (Unitree G1 29-DoF) ─────────────────────────────────────
// Derived from Unitree's published URDF / SDK headers. The index is the
// position in LowState.motor_state[] / LowCmd.motor_cmd[].

export const G1_JOINT_MAP: JointMapEntry[] = [
  { index: 0, nativeName: "L_hip_pitch", rosName: "left_hip_pitch", kp: 80, kd: 3, region: "leg" },
  { index: 1, nativeName: "R_hip_pitch", rosName: "right_hip_pitch", kp: 80, kd: 3, region: "leg" },
  { index: 2, nativeName: "L_hip_roll", rosName: "left_hip_roll", kp: 80, kd: 3, region: "leg" },
  { index: 3, nativeName: "R_hip_roll", rosName: "right_hip_roll", kp: 80, kd: 3, region: "leg" },
  { index: 4, nativeName: "L_hip_yaw", rosName: "left_hip_yaw", kp: 80, kd: 3, region: "leg" },
  { index: 5, nativeName: "R_hip_yaw", rosName: "right_hip_yaw", kp: 80, kd: 3, region: "leg" },
  { index: 6, nativeName: "L_knee", rosName: "left_knee", kp: 120, kd: 4, region: "leg" },
  { index: 7, nativeName: "R_knee", rosName: "right_knee", kp: 120, kd: 4, region: "leg" },
  { index: 8, nativeName: "L_ankle_pitch", rosName: "left_ankle_pitch", kp: 40, kd: 2, region: "leg" },
  { index: 9, nativeName: "R_ankle_pitch", rosName: "right_ankle_pitch", kp: 40, kd: 2, region: "leg" },
  { index: 10, nativeName: "L_ankle_roll", rosName: "left_ankle_roll", kp: 40, kd: 2, region: "leg" },
  { index: 11, nativeName: "R_ankle_roll", rosName: "right_ankle_roll", kp: 40, kd: 2, region: "leg" },
  { index: 12, nativeName: "waist_yaw", rosName: "waist_yaw", kp: 60, kd: 3, region: "waist" },
  { index: 13, nativeName: "L_shoulder_pitch", rosName: "left_shoulder_pitch", kp: 40, kd: 2, region: "arm" },
  { index: 14, nativeName: "R_shoulder_pitch", rosName: "right_shoulder_pitch", kp: 40, kd: 2, region: "arm" },
  { index: 15, nativeName: "L_shoulder_roll", rosName: "left_shoulder_roll", kp: 40, kd: 2, region: "arm" },
  { index: 16, nativeName: "R_shoulder_roll", rosName: "right_shoulder_roll", kp: 40, kd: 2, region: "arm" },
  { index: 17, nativeName: "L_shoulder_yaw", rosName: "left_shoulder_yaw", kp: 40, kd: 2, region: "arm" },
  { index: 18, nativeName: "R_shoulder_yaw", rosName: "right_shoulder_yaw", kp: 40, kd: 2, region: "arm" },
  { index: 19, nativeName: "L_elbow", rosName: "left_elbow", kp: 40, kd: 2, region: "arm" },
  { index: 20, nativeName: "R_elbow", rosName: "right_elbow", kp: 40, kd: 2, region: "arm" },
  { index: 21, nativeName: "L_wrist_roll", rosName: "left_wrist_roll", kp: 20, kd: 1, region: "arm" },
  { index: 22, nativeName: "R_wrist_roll", rosName: "right_wrist_roll", kp: 20, kd: 1, region: "arm" },
  { index: 23, nativeName: "L_wrist_pitch", rosName: "left_wrist_pitch", kp: 20, kd: 1, region: "arm" },
  { index: 24, nativeName: "R_wrist_pitch", rosName: "right_wrist_pitch", kp: 20, kd: 1, region: "arm" },
  { index: 25, nativeName: "L_wrist_yaw", rosName: "left_wrist_yaw", kp: 20, kd: 1, region: "arm" },
  { index: 26, nativeName: "R_wrist_yaw", rosName: "right_wrist_yaw", kp: 20, kd: 1, region: "arm" },
];

// ── ROS 2 topics the Unitree bridge publishes / subscribes ───────────────────

export const UNITREE_TOPICS: RosTopic[] = [
  { name: "/joint_states", type: "sensor_msgs/JointState", direction: "native-to-ros", rate: "500 Hz" },
  { name: "/imu/data", type: "sensor_msgs/Imu", direction: "native-to-ros", rate: "500 Hz" },
  { name: "/odom", type: "nav_msgs/Odometry", direction: "native-to-ros", rate: "100 Hz" },
  { name: "/cmd_vel", type: "geometry_msgs/Twist", direction: "ros-to-native", rate: "100 Hz" },
  { name: "/joint_trajectory", type: "trajectory_msgs/JointTrajectory", direction: "ros-to-native", rate: "on-demand" },
  { name: "/unitree/sport_mode", type: "std_srvs/SetBool", direction: "ros-to-native", rate: "on-demand" },
];

export function getAdapter(id: string): BridgeAdapter | undefined {
  return ADAPTERS.find((a) => a.id === id);
}
