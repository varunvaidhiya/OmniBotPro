"use client";

/*
 * PaidFeatureGate — inline gate for cloud-cost features.
 *
 * Use this inside a console to wrap a specific paid action (GPU training,
 * AI inference, MCP connection, cloud simulation, etc.). Unlike ConsoleGate,
 * this does NOT redirect — it renders an inline upgrade prompt so the user
 * can still browse the rest of the console freely.
 *
 *   signed in, active plan  → render children normally
 *   signed in, no plan      → render inline upgrade prompt
 */

import Link from "next/link";
import { Lock, Zap } from "lucide-react";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasConsoleAccess } from "@/lib/auth/plans";

interface PaidFeatureGateProps {
  /** Short slug used in the upgrade URL, e.g. "gpu-training" */
  feature: string;
  /** Human-readable feature name shown in the prompt, e.g. "GPU Training" */
  label: string;
  /** One line explaining the cloud cost, e.g. "Runs on OhhO cloud GPUs" */
  description?: string;
  children: React.ReactNode;
}

export default function PaidFeatureGate({ feature, label, description, children }: PaidFeatureGateProps) {
  const { subscription } = useAuth();

  if (hasConsoleAccess(subscription)) return <>{children}</>;

  return <UpgradePrompt feature={feature} label={label} description={description} />;
}

function UpgradePrompt({ feature, label, description }: Omit<PaidFeatureGateProps, "children">) {
  const next = typeof window !== "undefined" ? window.location.pathname : "/";
  const href = `/upgrade?product=${encodeURIComponent(feature)}&next=${encodeURIComponent(next)}`;

  return (
    <div
      className="flex items-center gap-3 px-4 py-3 rounded-xl"
      style={{
        background: "rgba(0,212,255,0.05)",
        border: "1px solid rgba(0,212,255,0.22)",
      }}
    >
      <span
        className="shrink-0 flex items-center justify-center w-8 h-8 rounded-lg"
        style={{ background: "rgba(0,212,255,0.1)", color: "var(--cyan)" }}
      >
        <Lock size={15} />
      </span>

      <div className="flex-1 min-w-0">
        <div className="text-[12.5px] font-semibold text-white leading-tight">{label}</div>
        {description && (
          <div className="text-[11.5px] mt-0.5 leading-snug" style={{ color: "rgba(255,255,255,0.5)" }}>
            {description}
          </div>
        )}
      </div>

      <Link
        href={href}
        className="shrink-0 inline-flex items-center gap-1.5 text-[12px] font-semibold px-3 py-1.5 rounded-lg transition-all hover:-translate-y-px"
        style={{ background: "var(--cyan)", color: "var(--bg)" }}
      >
        <Zap size={12} /> Upgrade
      </Link>
    </div>
  );
}
