"use client";

/*
 * ConnectModal — pick a protocol and open a live link to a garage robot.
 *
 * Launched from the garage's Connect button. Shows every protocol as a card
 * (Wi-Fi/ROSBridge, USB/Serial, Bluetooth/BLE, Simulated), feature-detects
 * each for the current browser, surfaces the HTTPS→ws:// mixed-content gotcha,
 * and drives the global RobotConnectionProvider on connect.
 */

import { useMemo, useState } from "react";
import {
  AlertTriangle,
  Bluetooth,
  Check,
  Loader2,
  MonitorPlay,
  Plug,
  Usb,
  Wifi,
  X,
} from "lucide-react";

import type { UserRobot } from "@/lib/garage/types";
import { getHardwareModel } from "@/lib/garage/robot-catalog";
import { useRobotConnection } from "@/lib/connect/RobotConnectionProvider";
import { effectiveConnectionConfig } from "@/lib/connect/config";
import {
  PROTOCOLS,
  isProtocolAvailable,
  isValidWsUrl,
  rosbridgeMixedContentWarning,
  type ProtocolMeta,
} from "@/lib/connect/protocols";
import type { ConnectionConfig, ConnectionProtocol } from "@/lib/connect/types";

const ICONS: Record<string, typeof Wifi> = {
  Wifi,
  Usb,
  Bluetooth,
  MonitorPlay,
};

interface Props {
  robot: UserRobot;
  onClose: () => void;
}

export default function ConnectModal({ robot, onClose }: Props) {
  const { connect } = useRobotConnection();
  const saved = useMemo(() => effectiveConnectionConfig(robot), [robot]);
  const hw = getHardwareModel(robot.hardwareModelId);

  const [protocol, setProtocol] = useState<ConnectionProtocol>(saved.protocol);
  const [address, setAddress] = useState(saved.address ?? PROTOCOLS[0].defaultAddress ?? "");
  const [baud, setBaud] = useState(saved.baudRate ?? 115200);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selected = PROTOCOLS.find((p) => p.id === protocol)!;
  const availability = isProtocolAvailable(protocol);
  const mixedContent = protocol === "rosbridge" ? rosbridgeMixedContentWarning(address) : null;
  const addressInvalid = protocol === "rosbridge" && !isValidWsUrl(address);

  const canConnect = availability.available && !addressInvalid && !busy;

  const handleConnect = async () => {
    setError(null);
    setBusy(true);
    const cfg: ConnectionConfig = {
      protocol,
      autoReconnect: true,
      address: protocol === "rosbridge" ? address.trim() : undefined,
      baudRate: protocol === "webserial" ? baud : undefined,
    };
    const result = await connect(robot, cfg);
    setBusy(false);
    if (result.state === "connected") {
      onClose();
    } else if (result.error) {
      setError(result.error);
    }
    // (a cancelled USB/BLE chooser resolves as "disconnected" with no error —
    //  keep the modal open silently so the user can try again)
  };

  return (
    <div className="fixed inset-0 z-[200] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/70 backdrop-blur-sm" onClick={onClose} />

      <div
        className="relative w-full max-w-lg max-h-[88vh] overflow-y-auto flex flex-col rounded-2xl"
        style={{ background: "var(--surf)", border: "1px solid var(--border-med)", boxShadow: "0 24px 64px rgba(0,0,0,0.5)" }}
      >
        {/* header */}
        <div className="flex items-center justify-between px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
          <div className="min-w-0">
            <h2 className="font-display font-bold text-[17px] tracking-tight truncate">
              Connect to {robot.name}
            </h2>
            <p className="text-[12px] font-mono mt-0.5 truncate" style={{ color: "var(--muted)" }}>
              {hw?.name ?? robot.hardwareModelId}
            </p>
          </div>
          <button onClick={onClose} className="p-1.5 rounded-lg hover:bg-white/5 transition-colors shrink-0" style={{ color: "var(--muted)" }}>
            <X size={18} />
          </button>
        </div>

        {/* protocol grid */}
        <div className="p-5">
          <div className="text-[11px] font-mono uppercase tracking-[0.1em] mb-3" style={{ color: "var(--faint)" }}>
            Connection method
          </div>
          <div className="grid grid-cols-2 gap-2.5">
            {PROTOCOLS.map((p) => (
              <ProtocolCard
                key={p.id}
                meta={p}
                selected={protocol === p.id}
                onSelect={() => {
                  setProtocol(p.id);
                  setError(null);
                  if (p.id === "rosbridge" && !isValidWsUrl(address)) {
                    setAddress(p.defaultAddress ?? "");
                  }
                }}
              />
            ))}
          </div>

          {/* protocol-specific settings */}
          <div className="mt-4 space-y-3">
            {protocol === "rosbridge" && (
              <div>
                <label className="text-[11px] font-mono uppercase tracking-wider block mb-1.5" style={{ color: "var(--faint)" }}>
                  Robot address
                </label>
                <input
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  placeholder="ws://192.168.1.100:9090"
                  className="w-full bg-transparent text-[13px] font-mono px-3 py-2.5 rounded-lg outline-none"
                  style={{ border: `1px solid ${addressInvalid ? "rgba(248,113,113,0.5)" : "var(--border-med)"}`, color: "#fff" }}
                />
                {addressInvalid && (
                  <p className="text-[11px] mt-1.5" style={{ color: "#fb7185" }}>
                    Enter a ws:// or wss:// URL.
                  </p>
                )}
              </div>
            )}

            {protocol === "webserial" && (
              <div>
                <label className="text-[11px] font-mono uppercase tracking-wider block mb-1.5" style={{ color: "var(--faint)" }}>
                  Baud rate
                </label>
                <input
                  type="number"
                  value={baud}
                  onChange={(e) => setBaud(parseInt(e.target.value || "0", 10) || 115200)}
                  className="w-full bg-transparent text-[13px] font-mono px-3 py-2.5 rounded-lg outline-none"
                  style={{ border: "1px solid var(--border-med)", color: "#fff" }}
                />
              </div>
            )}

            {/* help line */}
            <p className="text-[11.5px] leading-[1.5] flex items-start gap-1.5" style={{ color: "var(--muted)" }}>
              <Plug size={13} className="mt-0.5 shrink-0" style={{ color: selected.accent }} />
              {selected.help}
            </p>

            {/* availability / warnings / errors */}
            {!availability.available && (
              <Notice tone="warn" text={availability.reason ?? "Unavailable in this browser."} />
            )}
            {mixedContent && <Notice tone="warn" text={mixedContent} />}
            {error && <Notice tone="error" text={error} />}
          </div>
        </div>

        {/* footer */}
        <div className="mt-auto flex items-center gap-2 px-5 py-4 border-t" style={{ borderColor: "var(--border)" }}>
          <button
            onClick={onClose}
            className="text-[13px] font-medium px-4 py-2.5 rounded-lg transition-colors hover:bg-white/5"
            style={{ color: "var(--muted)" }}
          >
            Cancel
          </button>
          <button
            onClick={handleConnect}
            disabled={!canConnect}
            className="flex-1 flex items-center justify-center gap-2 text-[13px] font-semibold px-4 py-2.5 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed enabled:hover:-translate-y-px"
            style={{ background: selected.accent, color: "var(--bg)" }}
          >
            {busy ? <Loader2 size={15} className="animate-spin" /> : <Plug size={15} />}
            {busy ? "Connecting…" : `Connect via ${selected.shortName}`}
          </button>
        </div>
      </div>
    </div>
  );
}

function ProtocolCard({ meta, selected, onSelect }: { meta: ProtocolMeta; selected: boolean; onSelect: () => void }) {
  const Icon = ICONS[meta.icon] ?? Wifi;
  const avail = isProtocolAvailable(meta.id);
  return (
    <button
      onClick={onSelect}
      className="text-left p-3 rounded-xl transition-all"
      style={{
        background: selected ? `${meta.accent}14` : "rgba(255,255,255,0.02)",
        border: `1px solid ${selected ? `${meta.accent}88` : "var(--border)"}`,
        opacity: avail.available ? 1 : 0.55,
      }}
    >
      <div className="flex items-center gap-2 mb-1.5">
        <div className="w-7 h-7 rounded-lg flex items-center justify-center shrink-0" style={{ background: `${meta.accent}1a`, color: meta.accent }}>
          <Icon size={15} />
        </div>
        <span className="text-[12.5px] font-semibold font-display flex-1 leading-tight">{meta.name}</span>
        {selected && <Check size={13} style={{ color: meta.accent }} />}
      </div>
      <p className="text-[10.5px] leading-[1.45]" style={{ color: "var(--muted)" }}>
        {meta.blurb}
      </p>
      <div className="flex gap-1 mt-1.5">
        {meta.experimental && (
          <span className="text-[9px] font-mono px-1.5 py-0.5 rounded" style={{ background: "rgba(251,191,36,0.12)", color: "#fbbf24" }}>
            experimental
          </span>
        )}
        {!avail.available && (
          <span className="text-[9px] font-mono px-1.5 py-0.5 rounded" style={{ background: "rgba(255,255,255,0.05)", color: "var(--faint)" }}>
            unavailable
          </span>
        )}
      </div>
    </button>
  );
}

function Notice({ tone, text }: { tone: "warn" | "error"; text: string }) {
  const color = tone === "error" ? "#fb7185" : "#fbbf24";
  return (
    <div
      className="flex items-start gap-2 px-3 py-2.5 rounded-lg text-[11.5px] leading-[1.5]"
      style={{ background: `${color}12`, border: `1px solid ${color}33`, color }}
    >
      <AlertTriangle size={13} className="mt-0.5 shrink-0" />
      <span>{text}</span>
    </div>
  );
}
