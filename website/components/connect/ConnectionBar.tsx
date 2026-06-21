"use client";

/*
 * ConnectionBar — the global "connected robot" HUD.
 *
 * Mounted once in app/layout.tsx, it floats at the top of every operational
 * console (suppressed on the marketing site + garage/hub, where robot cards
 * already show their own connect state). Collapsed it's a compact status pill;
 * expanded it shows live telemetry and exposes Disconnect + E-STOP — so the
 * connected robot, and control of it, is one click away from any console.
 */

import { useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import {
  Activity,
  Battery,
  Bluetooth,
  ChevronDown,
  Loader2,
  MonitorPlay,
  Navigation,
  Power,
  Usb,
  Wifi,
  X,
} from "lucide-react";

import { useRobotConnection } from "@/lib/connect/RobotConnectionProvider";
import { getProtocol } from "@/lib/connect/protocols";

// Routes where the HUD should appear (the operational consoles). Everything
// else (home, /console hub, /garage, marketing) is intentionally excluded.
const CONSOLE_ROUTES = [
  "/pilot", "/fleet", "/autonomy", "/bench", "/build", "/comply", "/data",
  "/frame", "/mind", "/proof", "/serve", "/shield", "/train", "/view",
  "/bridge", "/market", "/twin", "/care", "/link",
];

const PROTOCOL_ICONS: Record<string, typeof Wifi> = {
  Wifi, Usb, Bluetooth, MonitorPlay,
};

const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

export default function ConnectionBar() {
  const pathname = usePathname();
  const conn = useRobotConnection();
  const [expanded, setExpanded] = useState(false);
  const [uptime, setUptime] = useState("0:00");

  const onConsole = CONSOLE_ROUTES.some((r) => pathname === r || pathname.startsWith(r + "/"));

  // live uptime ticker
  useEffect(() => {
    const since = conn.status?.connectedSince;
    if (!since || !conn.isConnected) {
      setUptime("0:00");
      return;
    }
    const tick = () => {
      const s = Math.floor((Date.now() - since) / 1000);
      setUptime(`${Math.floor(s / 60)}:${(s % 60).toString().padStart(2, "0")}`);
    };
    tick();
    const iv = setInterval(tick, 1000);
    return () => clearInterval(iv);
  }, [conn.status?.connectedSince, conn.isConnected]);

  // Nothing to show unless a robot is connected/connecting and we're on a console.
  if (!onConsole || !conn.robot || !conn.status) return null;

  const { robot, status, telemetry, isConnected, isConnecting, eStopEngaged } = conn;
  const protoMeta = getProtocol(status.protocol);
  const ProtoIcon = PROTOCOL_ICONS[protoMeta.icon] ?? Wifi;

  const dotColor = isConnected ? GREEN : isConnecting ? AMBER : RED;
  const stateLabel = isConnected ? "Connected" : isConnecting ? status.label : "Disconnected";

  return (
    <div className="fixed top-3 left-1/2 -translate-x-1/2 z-[90] w-[min(94vw,520px)]" style={{ pointerEvents: "none" }}>
      <div
        className="rounded-xl overflow-hidden"
        style={{
          background: "rgba(10,14,26,0.92)",
          backdropFilter: "blur(18px)",
          border: `1px solid ${eStopEngaged ? RED + "88" : "var(--border-med)"}`,
          boxShadow: "0 10px 40px rgba(0,0,0,0.45)",
          pointerEvents: "auto",
        }}
      >
        {/* collapsed pill row */}
        <button
          onClick={() => setExpanded((v) => !v)}
          className="w-full flex items-center gap-2.5 px-3.5 py-2"
        >
          <span className="relative flex h-2 w-2 shrink-0">
            {isConnected && (
              <span className="absolute inline-flex h-full w-full rounded-full opacity-60 animate-ping" style={{ background: dotColor }} />
            )}
            <span className="relative inline-flex rounded-full h-2 w-2" style={{ background: dotColor }} />
          </span>

          <span className="text-[13px] font-semibold truncate max-w-[140px]">{robot.name}</span>

          <span className="inline-flex items-center gap-1 text-[11px] font-mono px-1.5 py-0.5 rounded" style={{ background: `${protoMeta.accent}1a`, color: protoMeta.accent }}>
            <ProtoIcon size={11} />
            {protoMeta.shortName}
          </span>

          <span className="hidden sm:inline text-[11px] font-mono" style={{ color: "var(--muted)" }}>
            {stateLabel}
          </span>

          <span className="ml-auto flex items-center gap-2">
            {isConnected && (
              <span className="text-[11px] font-mono tabular-nums" style={{ color: status.latencyMs > 80 ? AMBER : GREEN }}>
                {status.latencyMs} ms
              </span>
            )}
            {isConnecting && <Loader2 size={13} className="animate-spin" style={{ color: AMBER }} />}
            <ChevronDown size={14} className="transition-transform" style={{ color: "var(--faint)", transform: expanded ? "rotate(180deg)" : "none" }} />
          </span>
        </button>

        {/* expanded panel */}
        {expanded && (
          <div className="border-t px-3.5 py-3" style={{ borderColor: "var(--border)" }}>
            {status.error && (
              <p className="text-[11px] mb-2.5 px-2 py-1.5 rounded" style={{ background: `${RED}12`, color: RED }}>
                {status.error}
              </p>
            )}

            {/* telemetry grid */}
            <div className="grid grid-cols-3 gap-1.5 mb-3">
              <Metric icon={<Navigation size={12} />} label="Pose" value={
                telemetry?.odom
                  ? `${telemetry.odom.x.toFixed(1)},${telemetry.odom.y.toFixed(1)}`
                  : "—"
              } />
              <Metric icon={<Activity size={12} />} label="Msg/s" value={isConnected ? `${status.msgRate}` : "—"} />
              <Metric icon={<Battery size={12} />} label="Battery" value={
                telemetry?.battery != null ? `${Math.round(telemetry.battery * 100)}%` : "—"
              } />
              <Metric label="Uptime" value={isConnected ? uptime : "—"} />
              <Metric label="Joints" value={telemetry?.joints ? `${telemetry.joints.length}` : "—"} />
              <Metric label="Link" value={protoMeta.shortName} />
            </div>

            {/* address line */}
            <p className="text-[10.5px] font-mono mb-3 truncate" style={{ color: "var(--faint)" }}>
              {status.label}
            </p>

            {/* controls */}
            <div className="flex items-center gap-2">
              <button
                onClick={eStopEngaged ? conn.releaseStop : conn.emergencyStop}
                disabled={!isConnected}
                className="flex-1 inline-flex items-center justify-center gap-1.5 text-[12px] font-bold py-2 rounded-lg transition-all disabled:opacity-30"
                style={{
                  background: eStopEngaged ? "rgba(248,113,113,0.28)" : "rgba(248,113,113,0.12)",
                  color: RED,
                  border: `1.5px solid ${eStopEngaged ? RED : "rgba(248,113,113,0.4)"}`,
                }}
              >
                <Power size={13} />
                {eStopEngaged ? "RELEASE E-STOP" : "E-STOP"}
              </button>
              <button
                onClick={conn.disconnect}
                className="inline-flex items-center justify-center gap-1.5 text-[12px] font-semibold px-3 py-2 rounded-lg transition-colors hover:bg-white/5"
                style={{ color: "var(--muted)", border: "1px solid var(--border-med)" }}
              >
                <X size={13} />
                Disconnect
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function Metric({ icon, label, value }: { icon?: React.ReactNode; label: string; value: string }) {
  return (
    <div className="px-2 py-1.5 rounded-lg" style={{ background: "rgba(255,255,255,0.03)", border: "1px solid var(--border)" }}>
      <div className="flex items-center gap-1 text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>
        {icon}
        {label}
      </div>
      <div className="text-[12px] font-display font-semibold mt-0.5 tabular-nums truncate">{value}</div>
    </div>
  );
}
