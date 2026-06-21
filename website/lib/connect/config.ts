/*
 * Per-robot connection settings — persisted inside the existing
 * UserRobot.config JSONB under the `connection` key, so NO database migration
 * is required. Reads/writes round-trip through the garage client (Supabase with
 * localStorage fallback), so a user's last protocol + address follow them
 * across devices.
 */

import { updateUserRobot } from "@/lib/garage/client";
import type { UserRobot } from "@/lib/garage/types";
import { DEFAULT_ROSBRIDGE_ADDRESS } from "./protocols";
import type { ConnectionConfig } from "./types";

const CONNECTION_KEY = "connection";

/** Read a robot's saved connection settings, if any. */
export function readConnectionConfig(robot: UserRobot): ConnectionConfig | null {
  const raw = (robot.config as Record<string, unknown> | undefined)?.[CONNECTION_KEY];
  if (!raw || typeof raw !== "object") return null;
  const c = raw as Partial<ConnectionConfig>;
  if (!c.protocol) return null;
  return {
    protocol: c.protocol,
    address: c.address,
    baudRate: c.baudRate,
    autoReconnect: c.autoReconnect ?? true,
    lastConnectedAt: c.lastConnectedAt,
  };
}

/** Sensible defaults for a robot that has never been connected. */
export function defaultConnectionConfig(): ConnectionConfig {
  return {
    protocol: "rosbridge",
    address: DEFAULT_ROSBRIDGE_ADDRESS,
    baudRate: 115200,
    autoReconnect: true,
  };
}

/** The robot's saved config, or defaults — always returns something usable. */
export function effectiveConnectionConfig(robot: UserRobot): ConnectionConfig {
  return readConnectionConfig(robot) ?? defaultConnectionConfig();
}

/**
 * Merge the connection settings into the robot's full config and persist.
 * Other config keys are preserved (we spread the existing blob).
 */
export async function persistConnectionConfig(
  robot: UserRobot,
  cfg: ConnectionConfig,
): Promise<void> {
  const mergedConfig = {
    ...(robot.config ?? {}),
    [CONNECTION_KEY]: cfg,
  };
  try {
    await updateUserRobot(robot.id, { config: mergedConfig });
  } catch {
    // Persistence is best-effort — a failed save shouldn't block connecting.
  }
}
