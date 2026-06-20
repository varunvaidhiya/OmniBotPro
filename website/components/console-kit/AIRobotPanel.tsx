"use client";

/*
 * AIRobotPanel — the shared Kimi interface every OhhO console embeds.
 *
 * Shows the selected robot's deterministic baseline instantly (name, drive,
 * DOF, sensors) and offers a one-click "Generate with AI" that asks Kimi to
 * (a) enrich the robot with real facts and (b) build robot-specific console
 * panels on demand. Results are cached per robot, and everything degrades
 * gracefully: with no API key / offline, the baseline still renders in full.
 *
 * Drop `<AIRobotPanel consoleId="view" config={config} />` into any console.
 */

import { useState } from "react";
import { Sparkles, Loader2, ChevronDown, Bot, Cpu, Activity, Lightbulb } from "lucide-react";

import type { RobotConfig } from "@/lib/garage/robot-config";
import { useEnrichment, useConsoleSpec } from "@/lib/ai/useRobotAI";
import DynamicPanels from "./DynamicPanels";

const CYAN = "#00D4FF";

export default function AIRobotPanel({
  consoleId,
  config,
  defaultOpen = false,
}: {
  consoleId: string;
  config: RobotConfig;
  defaultOpen?: boolean;
}) {
  const [open, setOpen] = useState(defaultOpen);
  const { enrichment, loading: enriching, enrich } = useEnrichment(config);
  const { spec, loading: generating, generate } = useConsoleSpec(consoleId, config);
  const busy = enriching || generating;

  const run = () => {
    enrich();
    generate();
  };

  return (
    <div className="rounded-xl overflow-hidden" style={{ background: "var(--surf)", border: "1px solid var(--border)" }}>
      {/* header */}
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center gap-3 px-4 py-3 text-left"
        style={{ background: "rgba(0,212,255,0.04)" }}
      >
        <Bot size={16} style={{ color: CYAN }} />
        <div className="min-w-0 flex-1">
          <div className="text-[13px] font-semibold truncate">{config.name}</div>
          <div className="text-[10.5px] font-mono truncate" style={{ color: "var(--faint)" }}>
            {config.driveLabel} · {config.totalDof}-DOF · {config.sensors.length} sensors
            {config.enriched ? " · AI-enriched" : ""}
          </div>
        </div>
        <ChevronDown size={15} style={{ color: "var(--faint)", transform: open ? "rotate(180deg)" : "none", transition: "transform .2s" }} />
      </button>

      {open && (
        <div className="p-4 flex flex-col gap-4 border-t" style={{ borderColor: "var(--border)" }}>
          {/* baseline facts */}
          <div className="grid grid-cols-2 gap-x-4 gap-y-1.5">
            <Fact label="Drive" value={config.driveLabel} />
            <Fact label="DOF" value={`${config.totalDof} (${config.armDof} arm + ${config.baseDof} base)`} />
            <Fact label="Compute" value={config.compute.brain} />
            <Fact label="Payload" value={config.payloadKg ? `${config.payloadKg} kg` : "—"} />
          </div>

          {/* AI trigger */}
          <button
            onClick={run}
            disabled={busy}
            className="inline-flex items-center justify-center gap-2 text-[12.5px] font-semibold py-2.5 rounded-lg transition-all hover:-translate-y-px disabled:opacity-60"
            style={{ background: CYAN, color: "var(--bg)" }}
          >
            {busy ? <Loader2 size={14} className="animate-spin" /> : <Sparkles size={14} />}
            {busy ? "Asking Kimi…" : config.enriched || spec ? "Regenerate with AI" : "Generate with AI (Kimi)"}
          </button>

          {/* enrichment */}
          {enrichment && (
            <div className="flex flex-col gap-3">
              {enrichment.summary && (
                <p className="text-[12.5px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.82)" }}>{enrichment.summary}</p>
              )}
              {enrichment.notes.length > 0 && (
                <Section icon={<Activity size={12} />} title="Details">
                  <ul className="flex flex-col gap-1">
                    {enrichment.notes.map((n, i) => (
                      <li key={i} className="text-[11.5px] leading-[1.5] pl-3 relative" style={{ color: "rgba(255,255,255,0.7)" }}>
                        <span className="absolute left-0" style={{ color: CYAN }}>·</span>{n}
                      </li>
                    ))}
                  </ul>
                </Section>
              )}
              {enrichment.recommendedModels && enrichment.recommendedModels.length > 0 && (
                <Section icon={<Cpu size={12} />} title="Recommended models">
                  <Chips items={enrichment.recommendedModels} />
                </Section>
              )}
              {enrichment.suggestedTasks && enrichment.suggestedTasks.length > 0 && (
                <Section icon={<Lightbulb size={12} />} title="Suggested tasks">
                  <Chips items={enrichment.suggestedTasks} />
                </Section>
              )}
            </div>
          )}

          {/* dynamic console panels */}
          {spec && (
            <div className="flex flex-col gap-2">
              {spec.headline && (
                <p className="text-[11px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>{spec.headline}</p>
              )}
              <DynamicPanels spec={spec} config={config} />
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col">
      <span className="text-[9.5px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>{label}</span>
      <span className="text-[12px]" style={{ color: "rgba(255,255,255,0.85)" }}>{value}</span>
    </div>
  );
}

function Section({ icon, title, children }: { icon: React.ReactNode; title: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="flex items-center gap-1.5 mb-1.5" style={{ color: "var(--muted)" }}>
        {icon}
        <span className="text-[10px] font-mono uppercase tracking-wider">{title}</span>
      </div>
      {children}
    </div>
  );
}

function Chips({ items }: { items: string[] }) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {items.map((it, i) => (
        <span key={i} className="text-[10.5px] px-2 py-1 rounded-md" style={{ background: "rgba(255,255,255,0.04)", border: "1px solid var(--border)", color: "var(--muted)" }}>
          {it}
        </span>
      ))}
    </div>
  );
}
