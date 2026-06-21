/*
 * Pilot MCP tools — robot profiles, teleop safety, and control modes.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";
import { PROFILES, getProfile, DEFAULT_ARM_JOINTS, type RobotProfile } from "@/lib/pilot/robots";
import { CONTROL_MODES, applyJoystick, zeroVelocity, type Velocity } from "@/lib/pilot/teleop";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "pilot.listProfiles",
    description: "List all pre-loaded robot profiles for teleop (OmniBot mecanum, Diff-Drive Scout, Ackermann Rover) with base type, arm joints, max velocity, and cameras.",
    inputSchema: s({}),
    handler: async () => jsonResult(PROFILES),
    product: "pilot", readOnly: true,
  },
  {
    name: "pilot.getProfile",
    description: "Get a robot profile by ID with full teleop configuration (base type, arm joints, velocity limits, cameras).",
    inputSchema: s(
      { profileId: { type: "string", description: "Profile ID (e.g. 'omnibot', 'diff-scout', 'ackermann-rover')" } },
      ["profileId"],
    ),
    handler: async (p) => jsonResult(getProfile(p.profileId as string)),
    product: "pilot", readOnly: true,
  },
  {
    name: "pilot.listControlModes",
    description: "List the available control modes (teleop, nav2, vla) that the Pilot control-mode mux can switch between.",
    inputSchema: s({}),
    handler: async () => jsonResult(CONTROL_MODES),
    product: "pilot", readOnly: true,
  },
  {
    name: "pilot.applyJoystick",
    description: "Apply joystick input to produce a safe velocity command. Clamps to hardware limits and applies ramping. Returns the resulting velocity (linearX, linearY, angularZ).",
    inputSchema: s({
      joystickX: { type: "number", description: "Joystick X axis (-1 to 1)" },
      joystickY: { type: "number", description: "Joystick Y axis (-1 to 1)" },
      maxLin: { type: "number", description: "Max linear velocity m/s (default 0.2)" },
      maxAng: { type: "number", description: "Max angular velocity rad/s (default 1.0)" },
      eStop: { type: "boolean", description: "Emergency stop engaged (zeroes output)" },
    }),
    handler: async (p) => {
      const vel = applyJoystick(
        zeroVelocity(),
        Number(p.joystickX ?? 0), Number(p.joystickY ?? 0),
        Number(p.maxLin ?? 0.2), Number(p.maxAng ?? 1.0),
        Boolean(p.eStop),
      );
      return jsonResult(vel);
    },
    product: "pilot", readOnly: true,
  },
  {
    name: "pilot.getArmJoints",
    description: "Get the default arm joint definitions (SO-101 6-DOF: shoulder pan/lift, elbow, wrist flex/roll, gripper) with min/max/home positions in radians.",
    inputSchema: s({}),
    handler: async () => jsonResult(DEFAULT_ARM_JOINTS),
    product: "pilot", readOnly: true,
  },
];
