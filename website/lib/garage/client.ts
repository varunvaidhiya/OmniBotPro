/*
 * Garage Supabase client — CRUD helpers for the user_robots table.
 *
 * All operations are client-side and respect Supabase RLS. The auth context
 * determines which user's garage is returned.
 */

import { getSupabase } from "@/lib/auth/supabase";
import type { UserRobot, RobotStatus } from "./types";

/** Fetch all robots in the current user's garage. */
export async function getUserRobots(): Promise<UserRobot[]> {
  const supabase = getSupabase();
  if (!supabase) return [];

  const { data, error } = await supabase
    .from("user_robots")
    .select("*")
    .order("created_at", { ascending: false });

  if (error) {
    console.error("getUserRobots error:", error);
    return [];
  }
  return (data as UserRobot[]) ?? [];
}

/** Add a new robot to the user's garage. */
export async function addUserRobot(
  name: string,
  robotTypeId: string,
  hardwareModelId: string,
): Promise<UserRobot | null> {
  const supabase = getSupabase();
  if (!supabase) return null;

  const { data, error } = await supabase
    .from("user_robots")
    .insert({
      name,
      robot_type_id: robotTypeId,
      hardware_model_id: hardwareModelId,
      status: "draft" as RobotStatus,
      config: {},
    })
    .select()
    .single();

  if (error) {
    console.error("addUserRobot error:", error);
    return null;
  }
  return data as UserRobot;
}

/** Update a robot's configuration or name. */
export async function updateUserRobot(
  id: string,
  updates: Partial<Pick<UserRobot, "name" | "status" | "config">>,
): Promise<UserRobot | null> {
  const supabase = getSupabase();
  if (!supabase) return null;

  const { data, error } = await supabase
    .from("user_robots")
    .update(updates)
    .eq("id", id)
    .select()
    .single();

  if (error) {
    console.error("updateUserRobot error:", error);
    return null;
  }
  return data as UserRobot;
}

/** Remove a robot from the user's garage. */
export async function deleteUserRobot(id: string): Promise<boolean> {
  const supabase = getSupabase();
  if (!supabase) return false;

  const { error } = await supabase
    .from("user_robots")
    .delete()
    .eq("id", id);

  if (error) {
    console.error("deleteUserRobot error:", error);
    return false;
  }
  return true;
}
