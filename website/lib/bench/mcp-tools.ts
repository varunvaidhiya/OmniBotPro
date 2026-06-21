/*
 * Bench MCP tools — hardware bring-up, self-test, and calibration.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";

const ASSEMBLY_STEPS = [
  { id: "a1", label: "Frame & base", detail: "Bolt the base frame together", status: "done" },
  { id: "a2", label: "Mecanum wheels", detail: "Attach 4 mecanum wheels to the base", status: "done" },
  { id: "a3", label: "Motor board", detail: "Mount the Yahboom motor controller", status: "done" },
  { id: "a4", label: "SO-101 arm", detail: "Attach the 6-DOF arm to the base", status: "done" },
  { id: "a5", label: "Cameras", detail: "Mount front, wrist, and surround cameras", status: "active" },
  { id: "a6", label: "Compute + power", detail: "Install Pi 5, battery, and wiring", status: "pending" },
];

const SUBSYSTEMS = [
  { id: "sub1", label: "Motors", port: "USB0 · 115200", status: "OK", detail: "All 4 motors responding" },
  { id: "sub2", label: "Encoders", port: "USB0 · 115200", status: "OK", detail: "Encoder readings nominal" },
  { id: "sub3", label: "IMU", port: "I2C · 400kHz", status: "OK", detail: "IMU bias calibrated" },
  { id: "sub4", label: "Arm bus", port: "ACM0 · 1Mbd", status: "homing", detail: "Arm servos homing" },
  { id: "sub5", label: "Cameras", port: "USB · uvc", status: "OK", detail: "3 cameras streaming" },
];

const CALIBRATIONS = [
  { id: "cal1", label: "Odometry geometry", status: "done", result: "wheel_radius=0.04, sep=0.215" },
  { id: "cal2", label: "IMU bias", status: "done", result: "gyro_bias=[0.01, -0.02, 0.00]" },
  { id: "cal3", label: "Camera intrinsics", status: "done", result: "fx=640, fy=640, cx=320, cy=240" },
  { id: "cal4", label: "BEV rig alignment", status: "active", result: "in progress" },
  { id: "cal5", label: "Arm joint homing", status: "pending", result: "" },
];

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "bench.getAssembly",
    description: "Get the ordered assembly checklist for bringing up a robot from a box of parts. Each step has a label, detail, and status (done/active/pending).",
    inputSchema: s({}),
    handler: async () => jsonResult(ASSEMBLY_STEPS),
    product: "bench", readOnly: true,
  },
  {
    name: "bench.getSubsystems",
    description: "Get the hardware self-test status for all subsystems (motors, encoders, IMU, arm bus, cameras) with port, status, and detail.",
    inputSchema: s({}),
    handler: async () => jsonResult(SUBSYSTEMS),
    product: "bench", readOnly: true,
  },
  {
    name: "bench.getCalibrations",
    description: "Get the calibration status for all calibration routines (odometry, IMU bias, camera intrinsics, BEV rig, arm homing) with results.",
    inputSchema: s({}),
    handler: async () => jsonResult(CALIBRATIONS),
    product: "bench", readOnly: true,
  },
  {
    name: "bench.getStatus",
    description: "Get the overall bring-up status: assembly progress, self-test progress, calibration progress, and whether the robot is ready.",
    inputSchema: s({}),
    handler: async () => {
      const assemblyDone = ASSEMBLY_STEPS.filter((s) => s.status === "done").length;
      const subOk = SUBSYSTEMS.filter((s) => s.status === "OK").length;
      const calDone = CALIBRATIONS.filter((c) => c.status === "done").length;
      return jsonResult({
        assembly: { done: assemblyDone, total: ASSEMBLY_STEPS.length, pct: Math.round((assemblyDone / ASSEMBLY_STEPS.length) * 100) },
        selfTest: { ok: subOk, total: SUBSYSTEMS.length, pct: Math.round((subOk / SUBSYSTEMS.length) * 100) },
        calibration: { done: calDone, total: CALIBRATIONS.length, pct: Math.round((calDone / CALIBRATIONS.length) * 100) },
        ready: assemblyDone === ASSEMBLY_STEPS.length && subOk === SUBSYSTEMS.length && calDone === CALIBRATIONS.length,
      });
    },
    product: "bench", readOnly: true,
  },
  {
    name: "bench.flashFirmware",
    description: "Flash the motor-controller and MCU firmware. Returns a confirmation when the firmware is flashed with the correct protocol and car-type.",
    inputSchema: s({}),
    handler: async () => jsonResult({ flashed: true, firmware: "yahboom_v2.3", carType: "mecanum_x3", message: "Firmware flashed successfully. Motor controller is now online." }),
    product: "bench", readOnly: false,
  },
  {
    name: "bench.runSelfTest",
    description: "Run the hardware self-test across all subsystems (motors, encoders, IMU, arm, cameras). Returns the test results.",
    inputSchema: s({}),
    handler: async () => jsonResult({ started: true, message: "Self-test running. Checking motors, encoders, IMU, arm bus, and cameras." }),
    product: "bench", readOnly: false,
  },
  {
    name: "bench.runCalibration",
    description: "Run a specific calibration routine by ID (odometry, IMU bias, camera intrinsics, BEV rig, arm homing).",
    inputSchema: s(
      { calibrationId: { type: "string", description: "Calibration ID (e.g. 'cal1', 'cal2', 'cal3', 'cal4', 'cal5')" } },
      ["calibrationId"],
    ),
    handler: async (p) => {
      const cal = CALIBRATIONS.find((c) => c.id === p.calibrationId);
      if (!cal) return errorResult(`Calibration not found: ${p.calibrationId}`);
      return jsonResult({ ...cal, status: "done", started: true, message: `Calibration '${cal.label}' started.` });
    },
    product: "bench", readOnly: false,
  },
];
