"use client";

/*
 * DynamicPanels — renders an AI-generated ConsoleSpec safely.
 *
 * The spec only ever contains allowlisted panel kinds (sanitizeConsoleSpec in
 * lib/ai/types), so this maps each known kind to a known component and fills it
 * from the spec's bounded data or the robot's deterministic config. There is no
 * dangerouslySetInnerHTML anywhere — the model can pick panels and labels, never
 * markup. This is what makes a console's controls change per robot type.
 */

import {
  Camera,
  Cpu,
  Gauge,
  Sliders,
  Radio,
  Play,
  Activity,
} from "lucide-react";

import type { ConsoleSpec, ConsolePanel } from "@/lib/ai/types";
import type { RobotConfig } from "@/lib/garage/robot-config";

const CYAN = "#00D4FF";

export default function DynamicPanels({ spec, config }: { spec: ConsoleSpec; config: RobotConfig }) {
  if (!spec.panels.length) return null;
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
      {spec.panels.map((panel, i) => (
        <Panel key={i} panel={panel} config={config} />
      ))}
    </div>
  );
}

function Panel({ panel, config }: { panel: ConsolePanel; config: RobotConfig }) {
  switch (panel.kind) {
    case "joints":
      return <Shell title={panel.title} icon={<Sliders size={13} />}><JointsBody config={config} /></Shell>;
    case "cameras":
      return <Shell title={panel.title} icon={<Camera size={13} />}><CamerasBody config={config} /></Shell>;
    case "sensors":
      return <Shell title={panel.title} icon={<Radio size={13} />}><SensorsBody config={config} /></Shell>;
    case "actions":
      return <Shell title={panel.title} icon={<Play size={13} />}><ActionsBody actions={panel.actions ?? []} /></Shell>;
    case "metrics":
      return <Shell title={panel.title} icon={<Gauge size={13} />}><ItemsBody panel={panel} /></Shell>;
    case "status":
      return <Shell title={panel.title} icon={<Activity size={13} />}><ItemsBody panel={panel} /></Shell>;
    case "note":
      return <Shell title={panel.title} icon={<Cpu size={13} />}><ItemsBody panel={panel} note /></Shell>;
    default:
      return null;
  }
}

function Shell({ title, icon, children }: { title: string; icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <div className="rounded-xl p-4" style={{ background: "var(--surf)", border: "1px solid var(--border)" }}>
      <div className="flex items-center gap-2 mb-3" style={{ color: CYAN }}>
        {icon}
        <h3 className="text-[11px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>{title}</h3>
      </div>
      {children}
    </div>
  );
}

function ItemsBody({ panel, note }: { panel: ConsolePanel; note?: boolean }) {
  const items = panel.items ?? [];
  if (!items.length) return <p className="text-[12px]" style={{ color: "var(--faint)" }}>—</p>;
  if (note) {
    return (
      <ul className="flex flex-col gap-1.5">
        {items.map((it, i) => (
          <li key={i} className="text-[12px] leading-[1.5]" style={{ color: "rgba(255,255,255,0.7)" }}>
            <span className="font-medium text-white">{it.label}</span>
            {it.value ? <span style={{ color: "var(--muted)" }}> — {it.value}</span> : null}
          </li>
        ))}
      </ul>
    );
  }
  return (
    <div className="flex flex-col gap-2">
      {items.map((it, i) => (
        <div key={i} className="flex items-baseline justify-between gap-3">
          <span className="text-[11.5px]" style={{ color: "var(--muted)" }}>{it.label}</span>
          <span className="text-[11.5px] font-mono tabular-nums text-right" style={{ color: "#fff" }}>{it.value ?? "—"}</span>
        </div>
      ))}
    </div>
  );
}

function JointsBody({ config }: { config: RobotConfig }) {
  if (!config.joints.length) {
    return <p className="text-[12px]" style={{ color: "var(--faint)" }}>This robot has no manipulator joints.</p>;
  }
  return (
    <div className="flex flex-col gap-2">
      {config.joints.map((j) => (
        <div key={j.name} className="flex items-center gap-2">
          <span className="w-16 text-[11px]" style={{ color: "var(--muted)" }}>{j.label}</span>
          <div className="flex-1 h-1.5 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,0.07)" }}>
            <div className="h-full rounded-full" style={{ width: "50%", background: CYAN, opacity: 0.5 }} />
          </div>
          <span className="text-[9.5px] font-mono" style={{ color: "var(--faint)" }}>
            {j.min.toFixed(1)}…{j.max.toFixed(1)}
          </span>
        </div>
      ))}
    </div>
  );
}

function CamerasBody({ config }: { config: RobotConfig }) {
  const cams = config.sensors.filter((s) =>
    ["rgb_camera", "depth_camera", "stereo_camera", "thermal_camera", "fpv_camera", "bev"].includes(s.kind),
  );
  if (!cams.length) return <p className="text-[12px]" style={{ color: "var(--faint)" }}>No cameras on this robot.</p>;
  return (
    <div className="grid grid-cols-2 gap-2">
      {cams.map((c) => (
        <div key={c.topic} className="rounded-lg aspect-video flex flex-col items-center justify-center gap-1" style={{ background: "#05080f", border: "1px solid var(--border)" }}>
          <Camera size={16} style={{ color: "var(--faint)" }} />
          <span className="text-[9px] font-mono text-center px-1" style={{ color: "var(--muted)" }}>{c.label}</span>
        </div>
      ))}
    </div>
  );
}

function SensorsBody({ config }: { config: RobotConfig }) {
  return (
    <div className="flex flex-col gap-1.5">
      {config.sensors.map((s) => (
        <div key={s.topic} className="flex items-center justify-between gap-2">
          <span className="text-[11.5px]" style={{ color: "rgba(255,255,255,0.8)" }}>{s.label}</span>
          <span className="text-[9.5px] font-mono" style={{ color: "var(--faint)" }}>{s.topic} · {s.hz}Hz</span>
        </div>
      ))}
    </div>
  );
}

function ActionsBody({ actions }: { actions: string[] }) {
  if (!actions.length) return <p className="text-[12px]" style={{ color: "var(--faint)" }}>—</p>;
  return (
    <div className="flex flex-wrap gap-2">
      {actions.map((a, i) => (
        <span
          key={i}
          className="text-[11px] font-medium px-2.5 py-1.5 rounded-lg"
          style={{ background: "rgba(0,212,255,0.08)", border: "1px solid rgba(0,212,255,0.2)", color: CYAN }}
        >
          {a}
        </span>
      ))}
    </div>
  );
}
