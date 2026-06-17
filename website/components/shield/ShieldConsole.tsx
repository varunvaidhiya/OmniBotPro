"use client";

/*
 * ShieldConsole — the OhhO Shield application shell.
 *
 * A simulated fleet security dashboard.
 * Three surfaces: risk posture & policies (left), device identity (centre),
 * and SBOM/CVE watch (right).
 */

import Link from "next/link";
import {
  ArrowLeft,
  ShieldCheck,
  ShieldAlert,
  Fingerprint,
  RefreshCw,
  Search,
  Lock,
  Unlock,
  AlertOctagon,
} from "lucide-react";

import { useSecuritySimulation } from "@/lib/shield/simulation";

const CYAN = "#00D4FF";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

export default function ShieldConsole() {
  const { devices, cves, policies, togglePolicy, rotateKey, triggerScan, isScanning, riskScore } = useSecuritySimulation();

  const getRiskColor = (score: number) => {
    if (score >= 85) return GREEN;
    if (score >= 60) return AMBER;
    return RED;
  };

  const getRiskGrade = (score: number) => {
    if (score >= 90) return "A";
    if (score >= 80) return "B";
    if (score >= 70) return "C";
    if (score >= 60) return "D";
    return "F";
  };

  const riskColor = getRiskColor(riskScore);

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── Top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{
          background: "rgba(10,14,26,.86)",
          backdropFilter: "blur(18px)",
          borderColor: "var(--border)",
        }}
      >
        <Link
          href="/products/shield"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: CYAN }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Shield</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <ShieldCheck size={14} /> fleet-security
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}
          >
            <Lock size={11} />
            ZERO-TRUST
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Risk Posture & Policies */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          
          <div className="p-5 border-b flex flex-col items-center" style={{ borderColor: "var(--border)" }}>
            <h2 className="w-full text-left text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Risk Posture</h2>
            
            <div className="relative w-32 h-32 mb-2">
              <svg viewBox="0 0 100 100" className="transform -rotate-90 transition-all duration-1000">
                <circle cx="50" cy="50" r="40" fill="none" stroke="rgba(255,255,255,0.05)" strokeWidth="10" />
                <circle cx="50" cy="50" r="40" fill="none" stroke={riskColor} strokeWidth="10" strokeDasharray={`${(riskScore / 100) * 251.2} 251.2`} className="transition-all duration-500" />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-[28px] font-medium text-white">{getRiskGrade(riskScore)}</span>
                <span className="text-[9px] font-mono tracking-wider" style={{ color: riskColor }}>{riskScore} / 100</span>
              </div>
            </div>
          </div>

          <div className="flex-1 p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Security Policies</h2>
            
            <div className="flex flex-col gap-3">
              <PolicyToggle label="Secure boot" enabled={policies.secureBoot} onToggle={() => togglePolicy("secureBoot")} />
              <PolicyToggle label="Signed OTA updates" enabled={policies.signedOta} onToggle={() => togglePolicy("signedOta")} />
              <PolicyToggle label="Encrypted DDS (fastrtps)" enabled={policies.encryptedDds} onToggle={() => togglePolicy("encryptedDds")} />
            </div>
          </div>
        </aside>

        {/* CENTER — Device Identity */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b flex items-center justify-between" style={{ borderColor: "var(--border)" }}>
            <div>
              <h1 className="text-xl font-medium text-white mb-2 flex items-center gap-2">
                <Fingerprint size={20} color={CYAN} />
                Device Identity
              </h1>
              <p className="text-sm text-gray-400">Hardware-rooted keys and X.509 certificates.</p>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-6">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b" style={{ borderColor: "var(--border)" }}>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">Node</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">Fingerprint</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">Last Seen</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal text-right">Status</th>
                </tr>
              </thead>
              <tbody className="text-[13px]">
                {devices.map(d => (
                  <tr key={d.id} className="border-b transition-colors hover:bg-white/5" style={{ borderColor: "var(--border-med)" }}>
                    <td className="py-4 font-mono text-gray-300">{d.id}</td>
                    <td className="py-4 font-mono text-gray-500">{d.fingerprint}</td>
                    <td className="py-4 text-gray-400 text-[11px]">{d.lastSeen}</td>
                    <td className="py-4 text-right">
                      {d.status === "verified" && <span className="inline-flex items-center gap-1.5 text-[10px] font-mono text-emerald-400 bg-emerald-400/10 px-2 py-1 rounded"><ShieldCheck size={12} /> VERIFIED</span>}
                      {d.status === "untrusted" && <span className="inline-flex items-center gap-1.5 text-[10px] font-mono text-red-400 bg-red-400/10 px-2 py-1 rounded"><ShieldAlert size={12} /> UNTRUSTED</span>}
                      {(d.status === "rotate_key" || d.status === "untrusted") && (
                        <button onClick={() => rotateKey(d.id)} className="inline-flex items-center gap-1.5 text-[10px] font-mono text-amber-400 border border-amber-400/30 hover:bg-amber-400/10 px-2 py-1 rounded transition-colors">
                          <RefreshCw size={10} /> {d.status === "untrusted" ? "RE-ENROLL" : "ROTATE KEY"}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {/* RIGHT — SBOM & CVE */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>SBOM · CVE Watch</h2>
              <button 
                onClick={triggerScan}
                disabled={isScanning}
                className="flex items-center justify-center w-7 h-7 rounded bg-white/5 hover:bg-white/10 transition-colors"
              >
                <Search size={12} color={CYAN} className={isScanning ? "animate-spin" : ""} />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-2">
              <div className="p-3 rounded-lg border bg-black/20" style={{ borderColor: "var(--border)" }}>
                <div className="text-[18px] text-white font-mono">312</div>
                <div className="text-[9px] uppercase text-gray-500 font-mono">Packages Tracked</div>
              </div>
              <div className="p-3 rounded-lg border bg-black/20" style={{ borderColor: "var(--border)" }}>
                <div className="text-[18px] text-amber-400 font-mono">{cves.filter(c => c.status === "open").length}</div>
                <div className="text-[9px] uppercase text-gray-500 font-mono">Open CVEs</div>
              </div>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-2">
            {cves.map(c => {
              const isOpen = c.status === "open";
              const sevColor = c.severity === "high" || c.severity === "critical" ? RED : c.severity === "medium" ? AMBER : GREEN;
              return (
                <div key={c.id} className="p-3 rounded-lg border flex flex-col gap-1.5" style={{ borderColor: "var(--border)", background: "rgba(255,255,255,0.01)", opacity: isOpen ? 1 : 0.6 }}>
                  <div className="flex justify-between items-start">
                    <span className="text-[12px] font-mono text-gray-200">
                      {c.package} <span className="text-gray-600 text-[10px]">{c.version}</span>
                    </span>
                    <span className="text-[9px] font-mono uppercase px-1.5 py-0.5 rounded" style={{ color: sevColor, background: `${sevColor}20` }}>{c.severity}</span>
                  </div>
                  <div className="flex items-center justify-between mt-1">
                    <span className="text-[10px] font-mono text-gray-500">{c.id}</span>
                    {isOpen ? (
                      <span className="text-[10px] font-mono text-amber-400 flex items-center gap-1"><AlertOctagon size={10} /> OPEN</span>
                    ) : (
                      <span className="text-[10px] font-mono text-emerald-500 flex items-center gap-1"><ShieldCheck size={10} /> PATCHED</span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </aside>
      </div>
    </div>
  );
}

function PolicyToggle({ label, enabled, onToggle }: { label: string; enabled: boolean; onToggle: () => void }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-[12px] text-gray-300">{label}</span>
      <button 
        onClick={onToggle}
        className="relative w-10 h-5 rounded-full transition-colors"
        style={{ background: enabled ? GREEN : "var(--border)" }}
      >
        <span 
          className="absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white transition-transform" 
          style={{ transform: enabled ? "translateX(20px)" : "translateX(0)" }}
        />
      </button>
    </div>
  );
}
