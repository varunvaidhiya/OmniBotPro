/*
 * Simulated hardware bring-up state for OhhO Bench.
 *
 * Models the path from a Build BOM to a powered-on, calibrated robot:
 * an assembly checklist, a firmware flash, a hardware self-test that walks
 * each subsystem, and the calibration routines that write the deployment
 * profile. All client-side simulation — no real hardware.
 */

import { useCallback, useState } from "react";

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

const ASSEMBLY: AssemblyStep[] = [
  { id: "a1", label: "Frame & base", detail: "Mount chassis plates, M3×8 @ 0.5 Nm", status: "done" },
  { id: "a2", label: "Mecanum wheels", detail: "4× wheels, roller orientation X-pattern", status: "done" },
  { id: "a3", label: "Motor board", detail: "Yahboom expansion board + power rail", status: "done" },
  { id: "a4", label: "SO-101 arm", detail: "6× Feetech STS3215 on the bus", status: "active" },
  { id: "a5", label: "Cameras", detail: "Front + wrist + 4× base surround", status: "pending" },
  { id: "a6", label: "Compute + power", detail: "Pi 5 8 GB + 12 V Li-ion pack", status: "pending" },
];

const SUBSYSTEMS: Subsystem[] = [
  { id: "m", label: "Motors", port: "USB0 · 115200", status: "pending", detail: "4× mecanum drive" },
  { id: "e", label: "Encoders", port: "USB0 · 115200", status: "pending", detail: "quadrature × 4" },
  { id: "i", label: "IMU", port: "i2c-1 · 0x68", status: "pending", detail: "6-axis MPU" },
  { id: "a", label: "Arm bus", port: "ACM0 · 1 Mbd", status: "pending", detail: "6× STS3215" },
  { id: "c", label: "Cameras", port: "uvc · /dev/video*", status: "pending", detail: "6× streams" },
];

const CALIBRATIONS: Calibration[] = [
  { id: "odom", label: "Odometry geometry", status: "pending", result: "wheel r=0.04 m" },
  { id: "imu", label: "IMU bias", status: "pending", result: "gyro/accel zero" },
  { id: "cam", label: "Camera intrinsics", status: "pending", result: "6× fx,fy,cx,cy" },
  { id: "bev", label: "Surround-view rig", status: "pending", result: "IPM homographies" },
  { id: "arm", label: "Arm joint homing", status: "pending", result: "6× home @ 2048" },
];

export function useBringup() {
  const [assembly, setAssembly] = useState<AssemblyStep[]>(ASSEMBLY);
  const [subsystems, setSubsystems] = useState<Subsystem[]>(SUBSYSTEMS);
  const [calibrations, setCalibrations] = useState<Calibration[]>(CALIBRATIONS);
  const [firmware, setFirmware] = useState<Status>("pending");
  const [testing, setTesting] = useState(false);

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
    SUBSYSTEMS.forEach((sub, i) => {
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
          if (i === SUBSYSTEMS.length - 1) setTesting(false);
        },
        i * 600 + 450,
      );
    });
  }, []);

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
