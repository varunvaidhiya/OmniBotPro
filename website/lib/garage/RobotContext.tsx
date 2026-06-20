"use client";

/*
 * RobotContext — provides the currently selected robot (from URL ?robot=<id>)
 * to all console pages so they can adapt their UI to the specific robot type.
 *
 * Two exports:
 *   <RobotProvider>      — full wrapper with Suspense (use in console page.tsx)
 *   useRobot()           — hook to read robot context from any child component
 *
 * Passes ?robot=<id> through the URL so individual consoles know which robot
 * they're operating. Falls back to null (no robot selected) gracefully.
 */

import {
  createContext,
  Suspense,
  useCallback,
  useContext,
  useEffect,
  useState,
} from "react";
import { useSearchParams } from "next/navigation";
import { Loader2 } from "lucide-react";

import { getUserRobots } from "@/lib/garage/client";
import { getHardwareModel, getRobotTypeForHardware } from "@/lib/garage/robot-catalog";
import { getCategory, type RobotCategory } from "@/lib/garage/types";
import type { UserRobot, HardwareModel, RobotType } from "@/lib/garage/types";
import { getRobotConfig, defaultRobotConfig, type RobotConfig } from "@/lib/garage/robot-config";

interface RobotContextValue {
  robot: UserRobot | null;
  hardwareModel: HardwareModel | null;
  robotType: RobotType | null;
  category: RobotCategory | null;
  /**
   * The structured capability profile the consoles read to pre-configure
   * themselves. Always present: falls back to the OmniBot reference config
   * when no robot is selected, so a console never renders without a config.
   */
  config: RobotConfig;
  loading: boolean;
}

const RobotContext = createContext<RobotContextValue>({
  robot: null,
  hardwareModel: null,
  robotType: null,
  category: null,
  config: defaultRobotConfig(),
  loading: false,
});

/**
 * Full provider with built-in Suspense boundary.
 * Wrap a console page's content with this to enable ?robot= query param support.
 */
export function RobotProvider({ children }: { children: React.ReactNode }) {
  return (
    <Suspense fallback={null}>
      <RobotResolver>{children}</RobotResolver>
    </Suspense>
  );
}

function RobotResolver({ children }: { children: React.ReactNode }) {
  const searchParams = useSearchParams();
  const robotId = searchParams.get("robot") ?? null;
  const [value, setValue] = useState<RobotContextValue>({
    robot: null,
    hardwareModel: null,
    robotType: null,
    category: null,
    config: defaultRobotConfig(),
    loading: false,
  });

  const resolve = useCallback(async () => {
    if (!robotId) return;
    setValue((prev) => ({ ...prev, loading: true }));
    try {
      const robots = await getUserRobots();
      const bot = robots.find((r) => r.id === robotId) ?? null;
      const hw = bot ? getHardwareModel(bot.hardwareModelId) ?? null : null;
      const type = bot ? getRobotTypeForHardware(bot.hardwareModelId) ?? null : null;
      const cat = type ? getCategory(type.category) : null;
      const config =
        (bot ? getRobotConfig(bot.hardwareModelId) : undefined) ?? defaultRobotConfig();
      setValue({ robot: bot, hardwareModel: hw, robotType: type, category: cat, config, loading: false });
    } catch {
      setValue((prev) => ({ ...prev, loading: false }));
    }
  }, [robotId]);

  useEffect(() => {
    resolve();
  }, [resolve]);

  return (
    <RobotContext.Provider value={value}>{children}</RobotContext.Provider>
  );
}

export function useRobot(): RobotContextValue {
  return useContext(RobotContext);
}

/** Compact inline robot badge — use in console headers. */
export function RobotBadge() {
  const { robot, hardwareModel, category, loading } = useRobot();
  if (!robot) return null;

  return (
    <div
      className="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg text-[12px] font-medium"
      style={{
        background: "rgba(255,255,255,0.04)",
        border: `1px solid ${category?.color ?? "var(--cyan)"}33`,
        color: category?.color ?? "var(--cyan)",
      }}
    >
      {loading ? (
        <Loader2 size={13} className="animate-spin" />
      ) : (
        <>
          <span className="w-1.5 h-1.5 rounded-full" style={{ background: category?.color ?? "var(--cyan)" }} />
          {robot.name}
          {hardwareModel && (
            <span className="font-mono text-[10px] opacity-60">
              · {hardwareModel.name}
            </span>
          )}
        </>
      )}
    </div>
  );
}
