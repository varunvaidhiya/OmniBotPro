"use client";

/*
 * Garage page — full-page view of the user's robot fleet with add/delete.
 * Equivalent to the garage section on /console, but as its own page.
 */

import { useCallback, useEffect, useState } from "react";
import { Loader2 } from "lucide-react";

import Nav from "@/components/Nav";
import GarageView from "@/components/garage/GarageView";
import RobotSelector from "@/components/garage/RobotSelector";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";
import { getUserRobots, addUserRobot, deleteUserRobot } from "@/lib/garage/client";
import type { UserRobot } from "@/lib/garage/types";

export default function GaragePageClient() {
  const { user, subscription } = useAuth();
  const active = hasConsoleAccess(subscription);

  const [robots, setRobots] = useState<UserRobot[]>([]);
  const [loading, setLoading] = useState(true);
  const [showSelector, setShowSelector] = useState(false);

  const loadRobots = useCallback(async () => {
    setLoading(true);
    const data = await getUserRobots();
    setRobots(data);
    setLoading(false);
  }, []);

  useEffect(() => {
    loadRobots();
  }, [loadRobots]);

  const handleAdd = async (name: string, robotTypeId: string, hardwareModelId: string) => {
    const newRobot = await addUserRobot(name, robotTypeId, hardwareModelId);
    if (newRobot) {
      setRobots((prev) => [newRobot, ...prev]);
    }
    setShowSelector(false);
  };

  const handleDelete = async (id: string) => {
    const ok = await deleteUserRobot(id);
    if (ok) {
      setRobots((prev) => prev.filter((r) => r.id !== id));
    }
  };

  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />

        <div className="relative z-10 max-w-6xl mx-auto px-6">
          {/* user info header */}
          <div className="mb-8">
            <p className="text-[13px]" style={{ color: "rgba(255,255,255,0.45)" }}>
              {user?.email}
              {active && (
                <span
                  className="ml-2 inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-mono"
                  style={{ background: "var(--cyan)", color: "var(--bg)" }}
                >
                  {subscription?.plan ?? ""} plan
                </span>
              )}
            </p>
          </div>

          {/* garage content */}
          {loading ? (
            <div className="flex items-center gap-2 py-12" style={{ color: "rgba(255,255,255,0.4)" }}>
              <Loader2 size={18} className="animate-spin" />
              <span className="text-[14px] font-mono">Loading your garage…</span>
            </div>
          ) : (
            <GarageView
              robots={robots}
              onAddRobot={() => setShowSelector(true)}
              onDeleteRobot={handleDelete}
            />
          )}
        </div>
      </main>

      {showSelector && (
        <RobotSelector
          onAdd={handleAdd}
          onClose={() => setShowSelector(false)}
        />
      )}
    </>
  );
}
