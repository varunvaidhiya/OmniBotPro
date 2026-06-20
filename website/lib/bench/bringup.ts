/*
 * Simulated hardware bring-up state for OhhO Bench.
 *
 * Models the path from a Build BOM to a powered-on, calibrated robot:
 * an assembly checklist, a firmware flash, a hardware self-test that walks
 * each subsystem, and the calibration routines that write the deployment
 * profile. All client-side simulation — no real hardware.
 */

import { useCallback, useEffect, useState } from "react";

import type { RobotConfig, SensorKind } from "@/lib/garage/robot-config";

export type Status = "pending" | "active" | "done" | "fail";

export interface AssemblyStep {
  id: string;
  label: string;
  detail: string;
  status: Status;
}

export interface Subsystem {
  id: string;
  label: string;
  port: string;
  status: Status;
  detail: string;
}

export interface Calibration {
  id: string;
  label: string;
  status: Status;
  result: string;
}

// ── Bring-up plans, derived from the selected robot ──────────────────────────
// What you assemble, self-test and calibrate depends entirely on the robot:
// a drone gets props + ESCs + GPS, a fixed arm gets no wheels, an armless AMR
// gets no arm bus or joint homing.

const CAMERA_KINDS: SensorKind[] = ["rgb_camera", "depth_camera", "stereo_camera", "thermal_camera", "fpv_camera", "bev"];
const hasSensor = (config: RobotConfig, k: SensorKind) => config.sensors.some((s) => s.kind === k);
const cameraCount = (config: RobotConfig) => config.sensors.filter((s) => CAMERA_KINDS.includes(s.kind)).length;

function driveAssemblyStep(config: RobotConfig): AssemblyStep | null {
  const done: Status = "done";
  switch (config.drive) {
    case "mecanum": return { id: "a2", label: "Mecanum wheels", detail: "4× wheels, roller X-pattern", status: done };
    case "differential":
    case "skid-steer": return { id: "a2", label: "Drive wheels", detail: "geared motors + caster", status: done };
    case "ackermann": return { id: "a2", label: "Steering & drive", detail: "Ackermann linkage + drive", status: done };
    case "tracked": return { id: "a2", label: "Tracks", detail: "2× tracked drive units", status: done };
    case "rocker-bogie": return { id: "a2", label: "Rocker-bogie wheels", detail: "6× wheels + suspension", status: done };
    case "gantry": return { id: "a2", label: "Gantry axes", detail: "X/Y/Z lead screws", status: done };
    case "quadrotor":
    case "vtol": return { id: "a2", label: "Props & ESCs", detail: "4× motors + ESCs", status: done };
    case "hexacopter": return { id: "a2", label: "Props & ESCs", detail: "6× motors + ESCs", status: done };
    case "fixed-wing": return { id: "a2", label: "Wing & control surfaces", detail: "servos + prop", status: done };
    case "quadruped":
    case "hexapod":
    case "bipedal": return { id: "a2", label: "Leg actuators", detail: `${config.totalDof}× joint servos`, status: done };
    case "thruster": return { id: "a2", label: "Thrusters", detail: "vectored thruster array", status: done };
    default: return null; // fixed-base / wearable
  }
}

function assemblyFor(config: RobotConfig): AssemblyStep[] {
  const steps: AssemblyStep[] = [
    { id: "a1", label: config.capabilities.isStationary ? "Base mount" : "Frame & base", detail: `${config.driveLabel} chassis`, status: "done" },
  ];
  const drive = driveAssemblyStep(config);
  if (drive) steps.push(drive);
  steps.push({ id: "a3", label: "Controller board", detail: "motor controller + power rail", status: "done" });
  if (config.hasArm) steps.push({ id: "a4", label: "Manipulator arm", detail: `${config.armDof}× actuators on the bus`, status: "active" });
  const cams = cameraCount(config);
  if (cams > 0) steps.push({ id: "a5", label: "Cameras", detail: `${cams}× camera${cams > 1 ? "s" : ""}`, status: "pending" });
  steps.push({ id: "a6", label: "Compute + power", detail: `${config.compute.brain} + battery`, status: "pending" });
  return steps;
}

function subsystemsFor(config: RobotConfig): Subsystem[] {
  const subs: Subsystem[] = [];
  if (config.baseDof > 0) {
    subs.push({ id: "m", label: config.capabilities.isAerial ? "Motors / ESCs" : "Motors", port: "USB0 · 115200", status: "pending", detail: config.driveLabel });
    if (config.capabilities.canNavigate && !config.capabilities.isAerial) {
      subs.push({ id: "e", label: "Encoders", port: "USB0 · 115200", status: "pending", detail: "wheel odometry" });
    }
  }
  if (hasSensor(config, "imu")) subs.push({ id: "i", label: "IMU", port: "i2c-1 · 0x68", status: "pending", detail: "6-axis" });
  if (config.hasArm) subs.push({ id: "a", label: "Arm bus", port: "ACM0 · 1 Mbd", status: "pending", detail: `${config.armDof}× servos` });
  const cams = cameraCount(config);
  if (cams > 0) subs.push({ id: "c", label: "Cameras", port: "uvc · /dev/video*", status: "pending", detail: `${cams}× streams` });
  if (hasSensor(config, "gps")) subs.push({ id: "g", label: "GPS", port: "ttyACM1 · 9600", status: "pending", detail: "fix + RTK" });
  return subs;
}

function calibrationsFor(config: RobotConfig): Calibration[] {
  const cal: Calibration[] = [];
  if (config.capabilities.canNavigate) cal.push({ id: "odom", label: "Odometry geometry", status: "pending", result: config.driveLabel });
  if (hasSensor(config, "imu")) cal.push({ id: "imu", label: "IMU bias", status: "pending", result: "gyro/accel zero" });
  const cams = cameraCount(config);
  if (cams > 0) cal.push({ id: "cam", label: "Camera intrinsics", status: "pending", result: `${cams}× fx,fy,cx,cy` });
  if (hasSensor(config, "bev")) cal.push({ id: "bev", label: "Surround-view rig", status: "pending", result: "IPM homographies" });
  if (config.hasArm) cal.push({ id: "arm", label: "Arm joint homing", status: "pending", result: `${config.armDof}× home` });
  if (cal.length === 0) cal.push({ id: "sys", label: "System check", status: "pending", result: "baseline" });
  return cal;
}

export function useBringup(config: RobotConfig) {
  const [assembly, setAssembly] = useState<AssemblyStep[]>(() => assemblyFor(config));
  const [subsystems, setSubsystems] = useState<Subsystem[]>(() => subsystemsFor(config));
  const [calibrations, setCalibrations] = useState<Calibration[]>(() => calibrationsFor(config));
  const [firmware, setFirmware] = useState<Status>("pending");
  const [testing, setTesting] = useState(false);

  // Rebuild the bring-up plan when the selected robot changes.
  useEffect(() => {
    setAssembly(assemblyFor(config));
    setSubsystems(subsystemsFor(config));
    setCalibrations(calibrationsFor(config));
    setFirmware("pending");
    setTesting(false);
  }, [config]);

  const toggleStep = useCallback((id: string) => {
    setAssembly((prev) =>
      prev.map((s) =>
        s.id === id
          ? { ...s, status: s.status === "done" ? "pending" : "done" }
          : s,
      ),
    );
  }, []);

  const flashFirmware = useCallback(() => {
    setFirmware("active");
    setTimeout(() => setFirmware("done"), 1600);
  }, []);

  const runSelfTest = useCallback(() => {
    setTesting(true);
    setSubsystems((prev) => prev.map((s) => ({ ...s, status: "pending" })));
    subsystems.forEach((sub, i) => {
      setTimeout(() => {
        setSubsystems((prev) =>
          prev.map((s) => (s.id === sub.id ? { ...s, status: "active" } : s)),
        );
      }, i * 600);
      setTimeout(
        () => {
          setSubsystems((prev) =>
            prev.map((s) => (s.id === sub.id ? { ...s, status: "done" } : s)),
          );
          if (i === subsystems.length - 1) setTesting(false);
        },
        i * 600 + 450,
      );
    });
  }, [subsystems]);

  const runCalibration = useCallback((id: string) => {
    setCalibrations((prev) =>
      prev.map((c) => (c.id === id ? { ...c, status: "active" } : c)),
    );
    setTimeout(() => {
      setCalibrations((prev) =>
        prev.map((c) => (c.id === id ? { ...c, status: "done" } : c)),
      );
    }, 1200);
  }, []);

  const assemblyDone = assembly.filter((s) => s.status === "done").length;
  const testsDone = subsystems.filter((s) => s.status === "done").length;
  const calDone = calibrations.filter((c) => c.status === "done").length;

  const ready =
    assemblyDone === assembly.length &&
    firmware === "done" &&
    testsDone === subsystems.length &&
    calDone === calibrations.length;

  return {
    assembly,
    subsystems,
    calibrations,
    firmware,
    testing,
    ready,
    progress: {
      assembly: assemblyDone / assembly.length,
      tests: testsDone / subsystems.length,
      cal: calDone / calibrations.length,
    },
    toggleStep,
    flashFirmware,
    runSelfTest,
    runCalibration,
  };
}
