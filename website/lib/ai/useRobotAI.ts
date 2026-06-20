"use client";

/*
 * React hooks for the robot AI layer. Cache-first + lazy:
 *   - on robot change, any cached result is applied immediately (free, instant);
 *   - the network call only runs when the console explicitly triggers it
 *     (enrich() / generate()), so Kimi credits are spent on demand, not on
 *     every page load.
 * Both degrade silently to the deterministic baseline when AI is unavailable.
 */

import { useCallback, useEffect, useState } from "react";

import type { RobotConfig } from "@/lib/garage/robot-config";
import type { RobotEnrichment, ConsoleSpec } from "./types";
import {
  cachedEnrichment,
  cachedConsoleSpec,
  fetchEnrichment,
  fetchConsoleSpec,
} from "./client";

export function useEnrichment(config: RobotConfig) {
  const [enrichment, setEnrichment] = useState<RobotEnrichment | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setEnrichment(cachedEnrichment(config.robotId));
    setError(null);
  }, [config.robotId]);

  const enrich = useCallback(async () => {
    setLoading(true);
    setError(null);
    const r = await fetchEnrichment(config);
    setLoading(false);
    if (r) setEnrichment(r);
    else setError("AI enrichment is unavailable right now.");
  }, [config]);

  return { enrichment, loading, error, enrich };
}

export function useConsoleSpec(consoleId: string, config: RobotConfig) {
  const [spec, setSpec] = useState<ConsoleSpec | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setSpec(cachedConsoleSpec(consoleId, config.robotId));
    setError(null);
  }, [consoleId, config.robotId]);

  const generate = useCallback(async () => {
    setLoading(true);
    setError(null);
    const s = await fetchConsoleSpec(consoleId, config);
    setLoading(false);
    if (s) setSpec(s);
    else setError("AI console generation is unavailable right now.");
  }, [consoleId, config]);

  return { spec, loading, error, generate };
}
