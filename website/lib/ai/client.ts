/*
 * Client-side fetchers + cache for the robot AI routes.
 *
 * Both calls are best-effort: any failure (no API key, offline, bad response)
 * resolves to null, so a console always falls back to its deterministic
 * baseline. Results are cached in localStorage per robot/console so a robot is
 * only enriched once (Kimi credits cost money) and re-opening a console is free.
 */

import type { RobotConfig } from "@/lib/garage/robot-config";
import type { RobotEnrichment, ConsoleSpec } from "./types";

const TTL_MS = 1000 * 60 * 60 * 24 * 7; // 7 days

function cacheGet<T>(key: string): T | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(key);
    if (!raw) return null;
    const { t, v } = JSON.parse(raw) as { t: number; v: T };
    if (Date.now() - t > TTL_MS) return null;
    return v;
  } catch {
    return null;
  }
}

function cacheSet(key: string, v: unknown): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(key, JSON.stringify({ t: Date.now(), v }));
  } catch {
    /* quota / private-mode — ignore */
  }
}

const enrichKey = (robotId: string) => `ohho:ai:enrich:${robotId}`;
const specKey = (consoleId: string, robotId: string) => `ohho:ai:cspec:${consoleId}:${robotId}`;

export function cachedEnrichment(robotId: string): RobotEnrichment | null {
  return cacheGet<RobotEnrichment>(enrichKey(robotId));
}

export function cachedConsoleSpec(consoleId: string, robotId: string): ConsoleSpec | null {
  return cacheGet<ConsoleSpec>(specKey(consoleId, robotId));
}

export async function fetchEnrichment(
  config: RobotConfig,
  signal?: AbortSignal,
): Promise<RobotEnrichment | null> {
  try {
    const res = await fetch("/api/robot/enrich", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ config }),
      signal,
    });
    const data = (await res.json().catch(() => null)) as { enrichment?: RobotEnrichment } | null;
    if (!res.ok || !data?.enrichment) return null;
    cacheSet(enrichKey(config.robotId), data.enrichment);
    return data.enrichment;
  } catch {
    return null;
  }
}

export async function fetchConsoleSpec(
  consoleId: string,
  config: RobotConfig,
  signal?: AbortSignal,
): Promise<ConsoleSpec | null> {
  try {
    const res = await fetch("/api/robot/console-spec", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ consoleId, config }),
      signal,
    });
    const data = (await res.json().catch(() => null)) as { spec?: ConsoleSpec } | null;
    if (!res.ok || !data?.spec) return null;
    cacheSet(specKey(consoleId, config.robotId), data.spec);
    return data.spec;
  } catch {
    return null;
  }
}

/** Merge informational enrichment into a config (compute + summary; structure stays derived). */
export function applyEnrichment(config: RobotConfig, enrichment: RobotEnrichment): RobotConfig {
  return {
    ...config,
    compute: enrichment.compute ?? config.compute,
    summary: enrichment.summary || config.summary,
    enriched: true,
    source: "ai",
  };
}
