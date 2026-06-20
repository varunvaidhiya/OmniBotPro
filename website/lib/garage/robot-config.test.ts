import { describe, it, expect } from "vitest";

import {
  getRobotConfig,
  defaultRobotConfig,
  deriveRobotConfig,
  parseDrive,
  genericJoints,
} from "./robot-config";
import { getHardwareModel, getRobotTypeForHardware } from "./robot-catalog";
import { getCategory } from "./types";

describe("parseDrive", () => {
  it("maps locomotion strings to drive kinds", () => {
    expect(parseDrive("mecanum", "wheeled")).toBe("mecanum");
    expect(parseDrive("ackermann / skid-steer", "wheeled")).toBe("ackermann");
    expect(parseDrive("quadrotor", "drones")).toBe("quadrotor");
    expect(parseDrive("underwater-8thruster", "underwater-rov")).toBe("thruster");
    expect(parseDrive("fixed-base", "industrial-arm")).toBe("fixed-base");
  });
  it("falls back to the category when locomotion is unhelpful", () => {
    expect(parseDrive("", "drones")).toBe("quadrotor");
    expect(parseDrive("", "humanoid")).toBe("bipedal");
    expect(parseDrive("", "industrial-arm")).toBe("fixed-base");
  });
});

describe("genericJoints", () => {
  it("produces n named joints", () => {
    expect(genericJoints(0)).toHaveLength(0);
    const j = genericJoints(7);
    expect(j).toHaveLength(7);
    expect(j[0].name).toBe("joint_1");
  });
});

describe("getRobotConfig — OmniBot reference (flagship)", () => {
  it("locks OmniBot to the 9-DOF mecanum mobile-manipulation stack", () => {
    const cfg = getRobotConfig("omnibot")!;
    expect(cfg.source).toBe("flagship");
    expect(cfg.drive).toBe("mecanum");
    expect(cfg.holonomic).toBe(true);
    expect(cfg.hasArm).toBe(true);
    expect(cfg.armDof).toBe(6);
    expect(cfg.baseDof).toBe(3);
    expect(cfg.totalDof).toBe(9);
    expect(cfg.stateDim).toBe(9);
    expect(cfg.actionDim).toBe(9);
    expect(cfg.joints[0].name).toBe("arm_shoulder_pan");
    expect(cfg.datasetName).toBe("local/mobile_manipulation");
    expect(cfg.rosTopics.images).toContain("/camera/front/image_raw");
  });
});

describe("getRobotConfig — divergent robots", () => {
  it("a drone is aerial, armless, 4-DOF base with FPV + GPS", () => {
    const cfg = getRobotConfig("px4-quad")!;
    expect(cfg.capabilities.isAerial).toBe(true);
    expect(cfg.hasArm).toBe(false);
    expect(cfg.armDof).toBe(0);
    expect(cfg.baseDof).toBe(4);
    expect(cfg.totalDof).toBe(4);
    expect(cfg.sensors.some((s) => s.kind === "fpv_camera")).toBe(true);
    expect(cfg.sensors.some((s) => s.kind === "gps")).toBe(true);
    expect(cfg.rosTopics.jointStates).toBeUndefined();
  });

  it("a fixed industrial arm has 6 joints, no base, no cmd_vel", () => {
    const cfg = getRobotConfig("ur5e")!;
    expect(cfg.drive).toBe("fixed-base");
    expect(cfg.capabilities.isStationary).toBe(true);
    expect(cfg.baseDof).toBe(0);
    expect(cfg.armDof).toBe(6);
    expect(cfg.totalDof).toBe(6);
    expect(cfg.joints[0].name).toBe("shoulder_pan_joint");
    expect(cfg.rosTopics.cmdVel).toBe("");
    expect(cfg.rosTopics.jointStates).toBe("/joint_states");
  });

  it("a quadruped is legged with 12 DOF and no arm", () => {
    const cfg = getRobotConfig("unitree-go2")!;
    expect(cfg.capabilities.isLegged).toBe(true);
    expect(cfg.hasArm).toBe(false);
    expect(cfg.totalDof).toBe(12);
    expect(cfg.sensors.some((s) => s.kind === "lidar_3d")).toBe(true);
  });

  it("a differential education base navigates but does not manipulate", () => {
    const cfg = getRobotConfig("turtlebot4")!;
    expect(cfg.drive).toBe("differential");
    expect(cfg.baseDof).toBe(2);
    expect(cfg.capabilities.canNavigate).toBe(true);
    expect(cfg.capabilities.canManipulate).toBe(false);
  });
});

describe("deriveRobotConfig — pure derivation (no override)", () => {
  it("derives a sane config straight from catalog fields", () => {
    const hw = getHardwareModel("yahboom-x3")!;
    const type = getRobotTypeForHardware("yahboom-x3")!;
    const cfg = deriveRobotConfig(hw, type, getCategory(type.category).id);
    expect(cfg.source).toBe("derived");
    expect(cfg.drive).toBe("mecanum");
    expect(cfg.baseDof).toBe(3);
  });
});

describe("defaultRobotConfig", () => {
  it("returns the OmniBot 9-DOF reference", () => {
    const cfg = defaultRobotConfig();
    expect(cfg.totalDof).toBe(9);
    expect(cfg.drive).toBe("mecanum");
  });
});
