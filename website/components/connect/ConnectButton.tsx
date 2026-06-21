"use client";

/*
 * ConnectButton — the per-robot connect/disconnect control used on garage cards.
 *
 * Reflects the live global connection: shows "Connect" normally, a spinner while
 * connecting, and a green "Connected · ✕" (click to disconnect) when THIS robot
 * is the live one. Stops event propagation so it works inside the card's Link.
 */

import { Loader2, Plug, Wifi, X } from "lucide-react";

import type { UserRobot } from "@/lib/garage/types";
import { useRobotConnection } from "@/lib/connect/RobotConnectionProvider";

interface Props {
  robot: UserRobot;
  /** Open the connect modal for this robot. */
  onOpen: () => void;
  className?: string;
}

export default function ConnectButton({ robot, onOpen, className = "" }: Props) {
  const conn = useRobotConnection();
  const isThis = conn.robot?.id === robot.id;
  const connected = isThis && conn.isConnected;
  const connecting = isThis && conn.isConnecting;

  const stop = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
  };

  if (connecting) {
    return (
      <button
        onClick={(e) => { stop(e); conn.disconnect(); }}
        className={`inline-flex items-center justify-center gap-1.5 text-[12px] font-semibold px-3 py-1.5 rounded-lg ${className}`}
        style={{ background: "rgba(251,191,36,0.12)", color: "#fbbf24", border: "1px solid rgba(251,191,36,0.4)" }}
      >
        <Loader2 size={13} className="animate-spin" />
        Connecting…
      </button>
    );
  }

  if (connected) {
    return (
      <button
        onClick={(e) => { stop(e); conn.disconnect(); }}
        title="Disconnect"
        className={`inline-flex items-center justify-center gap-1.5 text-[12px] font-semibold px-3 py-1.5 rounded-lg transition-all group/conn ${className}`}
        style={{ background: "rgba(52,211,153,0.14)", color: "#34d399", border: "1px solid rgba(52,211,153,0.45)" }}
      >
        <Wifi size={13} />
        <span className="group-hover/conn:hidden">Connected</span>
        <span className="hidden group-hover/conn:inline-flex items-center gap-1"><X size={12} /> Disconnect</span>
      </button>
    );
  }

  return (
    <button
      onClick={(e) => { stop(e); onOpen(); }}
      className={`inline-flex items-center justify-center gap-1.5 text-[12px] font-semibold px-3 py-1.5 rounded-lg transition-all hover:-translate-y-px ${className}`}
      style={{ background: "rgba(0,212,255,0.1)", color: "var(--cyan)", border: "1px solid rgba(0,212,255,0.35)" }}
    >
      <Plug size={13} />
      Connect
    </button>
  );
}
