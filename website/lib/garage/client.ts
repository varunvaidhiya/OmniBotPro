/*
 * Garage client — CRUD for user robots with localStorage fallback.
 *
 * Strategy:
 *   1. When Supabase IS configured → use it as the primary store. Users get
 *      cloud-synced, cross-device garages.
 *   2. When Supabase is NOT configured → fall back to localStorage. Robots
 *      persist in the browser so the garage always works, even in demo mode
 *      or while waiting for Supabase setup.
 *   3. localStorage key is per-user (ohho_garage_<userId>) so different users
 *      on the same browser get separate garages.
 */

import { getSupabase, isSupabaseConfigured } from "@/lib/auth/supabase";
import type { UserRobot, RobotStatus } from "./types";

// ── Row mapping ───────────────────────────────────────────────────────────────
//
// The `user_robots` table stores snake_case columns; the app uses a camelCase
// UserRobot shape. These were previously cast directly (`data as UserRobot`),
// which produced objects with undefined camelCase fields. Always map through
// fromRow() so reads and writes round-trip correctly.

interface UserRobotRow {
  id: string;
  user_id: string;
  name: string;
  robot_type_id: string;
  hardware_model_id: string;
  status: RobotStatus;
  config: Record<string, unknown> | null;
  created_at: string;
  updated_at: string;
}

function fromRow(row: UserRobotRow): UserRobot {
  return {
    id: row.id,
    userId: row.user_id,
    name: row.name,
    robotTypeId: row.robot_type_id,
    hardwareModelId: row.hardware_model_id,
    status: row.status,
    config: row.config ?? {},
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

// ── localStorage helpers ────────────────────────────────────────────────────

const STORAGE_PREFIX = "ohho_garage_";
const ANON_KEY = "ohho_garage_anonymous";

function storageKey(userId?: string): string {
  if (userId) return `${STORAGE_PREFIX}${userId}`;
  return ANON_KEY;
}

function readLocal(userId?: string): UserRobot[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = localStorage.getItem(storageKey(userId));
    return raw ? (JSON.parse(raw) as UserRobot[]) : [];
  } catch {
    return [];
  }
}

function writeLocal(robots: UserRobot[], userId?: string): void {
  if (typeof window === "undefined") return;
  try {
    localStorage.setItem(storageKey(userId), JSON.stringify(robots));
  } catch {
    // storage full — silently ignore
  }
}

function generateId(): string {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

function now(): string {
  return new Date().toISOString();
}

// ── User ID resolver ────────────────────────────────────────────────────────

async function getCurrentUserId(): Promise<string | null> {
  const supabase = getSupabase();
  if (!supabase) return null;
  try {
    const { data } = await supabase.auth.getSession();
    return data?.session?.user?.id ?? null;
  } catch {
    return null;
  }
}

// ── Public API ──────────────────────────────────────────────────────────────

/** Fetch all robots in the current user's garage. */
export async function getUserRobots(): Promise<UserRobot[]> {
  const userId = await getCurrentUserId();

  // Try Supabase first
  if (isSupabaseConfigured) {
    const supabase = getSupabase();
    if (supabase) {
      const { data, error } = await supabase
        .from("user_robots")
        .select("*")
        .order("created_at", { ascending: false });

      if (!error && data) {
        const robots = (data as UserRobotRow[]).map(fromRow);
        // Sync to localStorage as an offline cache
        writeLocal(robots, userId ?? undefined);
        return robots;
      }
      if (error) console.warn("[garage] Supabase read failed, using local cache:", error.message);
      // Supabase failed — fall through to localStorage
    }
  }

  // localStorage fallback
  return readLocal(userId ?? undefined);
}

/** Add a new robot to the user's garage. */
export async function addUserRobot(
  name: string,
  robotTypeId: string,
  hardwareModelId: string,
): Promise<UserRobot | null> {
  const userId = await getCurrentUserId();
  const newRobot: UserRobot = {
    id: generateId(),
    userId: userId ?? "anonymous",
    name,
    robotTypeId,
    hardwareModelId,
    status: "draft" as RobotStatus,
    config: {},
    createdAt: now(),
    updatedAt: now(),
  };

  // Try Supabase first — only when we have an authenticated user, because the
  // table requires a non-null user_id and RLS enforces auth.uid() = user_id.
  // (The previous insert omitted user_id entirely, so every write failed and
  // silently fell back to localStorage — robots never reached the backend.)
  if (isSupabaseConfigured && userId) {
    const supabase = getSupabase();
    if (supabase) {
      const { data, error } = await supabase
        .from("user_robots")
        .insert({
          id: newRobot.id,
          user_id: userId,
          name,
          robot_type_id: robotTypeId,
          hardware_model_id: hardwareModelId,
          status: "draft",
          config: {},
        })
        .select()
        .single();

      if (!error && data) {
        return fromRow(data as UserRobotRow);
      }
      if (error) console.warn("[garage] Supabase insert failed, using local store:", error.message);
      // Supabase failed — fall through to localStorage
    }
  }

  // localStorage fallback
  const existing = readLocal(userId ?? undefined);
  existing.unshift(newRobot);
  writeLocal(existing, userId ?? undefined);
  return newRobot;
}

/** Remove a robot from the user's garage. */
export async function deleteUserRobot(id: string): Promise<boolean> {
  const userId = await getCurrentUserId();

  // Try Supabase first
  if (isSupabaseConfigured) {
    const supabase = getSupabase();
    if (supabase) {
      const { error } = await supabase
        .from("user_robots")
        .delete()
        .eq("id", id);

      if (!error) return true;
      console.warn("[garage] Supabase delete failed, using local store:", error.message);
      // Supabase failed — fall through to localStorage
    }
  }

  // localStorage fallback
  const existing = readLocal(userId ?? undefined);
  const filtered = existing.filter((r) => r.id !== id);
  if (filtered.length === existing.length) return false; // nothing deleted
  writeLocal(filtered, userId ?? undefined);
  return true;
}

/** Update a robot's configuration, name, or status. */
export async function updateUserRobot(
  id: string,
  updates: Partial<Pick<UserRobot, "name" | "status" | "config">>,
): Promise<UserRobot | null> {
  const userId = await getCurrentUserId();

  // Try Supabase first
  if (isSupabaseConfigured) {
    const supabase = getSupabase();
    if (supabase) {
      const { data, error } = await supabase
        .from("user_robots")
        .update(updates)
        .eq("id", id)
        .select()
        .single();

      if (!error && data) return fromRow(data as UserRobotRow);
      if (error) console.warn("[garage] Supabase update failed, using local store:", error.message);
      // Supabase failed — fall through to localStorage
    }
  }

  // localStorage fallback
  const existing = readLocal(userId ?? undefined);
  const idx = existing.findIndex((r) => r.id === id);
  if (idx === -1) return null;
  existing[idx] = { ...existing[idx], ...updates, updatedAt: now() };
  writeLocal(existing, userId ?? undefined);
  return existing[idx];
}

// ── Re-export ───────────────────────────────────────────────────────────────
//
// clearUserIdCache intentionally removed — getCurrentUserId always queries
// the live Supabase session to avoid stale user IDs across sign-in/out.
