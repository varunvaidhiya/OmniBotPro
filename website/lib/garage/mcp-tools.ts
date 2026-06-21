/*
 * Garage MCP tools — robot CRUD and catalog queries.
 *
 * Tools:
 *   garage.listRobots       — list all robots in the user's garage
 *   garage.getRobot         — get a specific robot by ID
 *   garage.listCategories   — list all robot categories (humanoid, wheeled, etc.)
 *   garage.listHardwareModels — list hardware models for a robot type
 *   garage.getRobotConfig   — get the capability profile for a robot
 *
 * Resources:
 *   ohho://garage/robots     — all robots as JSON
 *   ohho://garage/categories — all categories as JSON
 */

import {
  type ToolDefinition,
  type ResourceDefinition,
  textResult,
  jsonResult,
  errorResult,
  type InputSchema, type JsonSchemaProperty,
} from "@/lib/mcp/types";
import { getUserRobots, addUserRobot, deleteUserRobot, updateUserRobot } from "@/lib/garage/client";
import { CATEGORIES, type RobotCategoryId } from "@/lib/garage/types";
import { getHardwareModel, getRobotTypeForHardware } from "@/lib/garage/robot-catalog";
import { getRobotConfig, defaultRobotConfig } from "@/lib/garage/robot-config";

const s = (
  props: Record<string, JsonSchemaProperty>,
  required: string[] = [],
): InputSchema => ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "garage.listRobots",
    description: "List all robots in the user's garage. Returns an array of robot objects with id, name, type, status, and config.",
    inputSchema: s({}),
    handler: async () => {
      const robots = await getUserRobots();
      return jsonResult(robots);
    },
    product: "garage",
    readOnly: true,
  },
  {
    name: "garage.getRobot",
    description: "Get a specific robot's full details by ID, including hardware model specs and capability config.",
    inputSchema: s(
      { robotId: { type: "string", description: "The robot's unique ID" } },
      ["robotId"],
    ),
    handler: async (params) => {
      const robots = await getUserRobots();
      const robot = robots.find((r) => r.id === params.robotId);
      if (!robot) return errorResult(`Robot not found: ${params.robotId}`);
      const hw = getHardwareModel(robot.hardwareModelId);
      const config = getRobotConfig(robot.hardwareModelId) ?? defaultRobotConfig();
      return jsonResult({ robot, hardwareModel: hw, config });
    },
    product: "garage",
    readOnly: true,
  },
  {
    name: "garage.listCategories",
    description: "List all robot categories (humanoid, wheeled, legged, drone, industrial-arm, mobile-manipulator, swarm, etc.). Returns 15 categories.",
    inputSchema: s({}),
    handler: async () => {
      return jsonResult(CATEGORIES);
    },
    product: "garage",
    readOnly: true,
  },
  {
    name: "garage.getRobotConfig",
    description: "Get the structured capability profile (DOF, joints, sensors, compute, drive) for a specific robot hardware model.",
    inputSchema: s(
      { hardwareModelId: { type: "string", description: "The hardware model ID (e.g. 'omnibot', 'unitree-g1')" } },
      ["hardwareModelId"],
    ),
    handler: async (params) => {
      const config = getRobotConfig(params.hardwareModelId as string) ?? defaultRobotConfig();
      return jsonResult(config);
    },
    product: "garage",
    readOnly: true,
  },
  {
    name: "garage.addRobot",
    description: "Add a new robot to the garage. Requires a name, robot type ID, and hardware model ID.",
    inputSchema: s(
      {
        name: { type: "string", description: "Display name for the robot" },
        robotTypeId: { type: "string", description: "Robot type ID from the catalog" },
        hardwareModelId: { type: "string", description: "Hardware model ID (e.g. 'omnibot', 'unitree-g1')" },
        status: { type: "string", description: "Initial status", enum: ["active", "draft", "simulated", "offline"] },
      },
      ["name", "robotTypeId", "hardwareModelId"],
    ),
    handler: async (params) => {
      const robot = await addUserRobot(
        params.name as string,
        params.robotTypeId as string,
        params.hardwareModelId as string,
      );
      return jsonResult(robot);
    },
    product: "garage",
    readOnly: false,
  },
  {
    name: "garage.deleteRobot",
    description: "Delete a robot from the garage by ID.",
    inputSchema: s(
      { robotId: { type: "string", description: "The robot's unique ID" } },
      ["robotId"],
    ),
    handler: async (params) => {
      await deleteUserRobot(params.robotId as string);
      return textResult(`Robot ${params.robotId} deleted.`);
    },
    product: "garage",
    readOnly: false,
  },
  {
    name: "garage.updateRobot",
    description: "Update a robot's name, status, or config. Only provided fields are updated.",
    inputSchema: s(
      {
        robotId: { type: "string", description: "The robot's unique ID" },
        name: { type: "string", description: "New display name (optional)" },
        status: { type: "string", description: "New status (optional)", enum: ["active", "draft", "simulated", "offline"] },
        config: { type: "object", description: "New config blob (optional, merged with existing)" },
      },
      ["robotId"],
    ),
    handler: async (params) => {
      const updates: Record<string, unknown> = {};
      if (params.name) updates.name = params.name;
      if (params.status) updates.status = params.status;
      if (params.config) updates.config = params.config;
      const robot = await updateUserRobot(params.robotId as string, updates);
      return jsonResult(robot);
    },
    product: "garage",
    readOnly: false,
  },
];

export const resources: ResourceDefinition[] = [
  {
    uri: "ohho://garage/robots",
    name: "All garage robots",
    description: "The complete list of robots in the user's garage as JSON.",
    mimeType: "application/json",
    product: "garage",
    read: async () => {
      const robots = await getUserRobots();
      return JSON.stringify(robots, null, 2);
    },
  },
  {
    uri: "ohho://garage/categories",
    name: "Robot categories",
    description: "All 15 robot categories with labels, icons, and descriptions.",
    mimeType: "application/json",
    product: "garage",
    read: async () => JSON.stringify(CATEGORIES, null, 2),
  },
];
