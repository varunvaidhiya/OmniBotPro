/*
 * OhhO VR catalog — the robot catalog the headset uses to render a user's
 * garage and to add new robots, served as a second static file alongside the
 * manifest: website/lib/vr/catalog.ts → public/vr/catalog.json.
 *
 * Why separate from the manifest?
 *   The manifest (branding + auth + VR products) is small and loaded at launch.
 *   The catalog (every category + robot type + hardware model) is larger and
 *   only needed once the user opens the teleop garage, so the headset fetches it
 *   lazily. Both are the SAME data the website's garage uses — a user's robots,
 *   saved once on the website/Android app, render identically in VR with no
 *   re-entry (the connected-experience goal).
 *
 * A user's saved robot (Supabase `user_robots`) only stores `robot_type_id` +
 * `hardware_model_id`; this catalog is how the headset resolves those ids to a
 * displayable RobotType + HardwareModel (mirrors GarageRobot on the website).
 *
 * Pure data + types only, deterministic — drift-checked in catalog.test.ts.
 */

import { CATEGORIES, type RobotCategory, type RobotType } from "@/lib/garage/types";
import { ROBOT_TYPES } from "@/lib/garage/robot-catalog";

/** Schema version — bump when the catalog shape changes (the VR app reads it). */
export const VR_CATALOG_VERSION = 1 as const;

export interface VrCatalog {
  version: typeof VR_CATALOG_VERSION;
  /** Top-level robot categories (drones, wheeled, humanoid, …). */
  categories: RobotCategory[];
  /** Robot types grouped by category, each with its hardware models. */
  robotTypes: RobotType[];
}

/** Build the catalog bundle. Deterministic: same input → identical output. */
export function buildVrCatalog(): VrCatalog {
  return {
    version: VR_CATALOG_VERSION,
    categories: CATEGORIES,
    robotTypes: ROBOT_TYPES,
  };
}
