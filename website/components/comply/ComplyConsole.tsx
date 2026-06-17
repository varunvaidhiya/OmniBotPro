"use client";

/*
 * ComplyConsole — the OhhO Comply application shell.
 *
 * A simulated compliance and certification workflow dashboard.
 * Three surfaces: standards list (left), checklist (centre),
 * and readiness/documents (right).
 */

import Link from "next/link";
import { useState } from "react";
import {
  ArrowLeft,
  BookOpen,
  CheckCircle2,
  Circle,
  FileCheck,
  FileText,
  History,
  ShieldCheck,
  DownloadCloud,
} from "lucide-react";

import { useCompliance, downloadText, type Standard } from "@/lib/comply/standards";

const VIOLET = "#A78BFA";
const VIOLET_DIM = "rgba(167,139,250,0.15)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";

export default function ComplyConsole() {
  const { standards, stats, toggleRequirementStatus, generateDocument } = useCompliance();
  const [activeStandardId, setActiveStandardId] = useState<string>(standards[0].id);

  const activeStandard = standards.find(s => s.id === activeStandardId) as Standard;

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
          href="/products/comply"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: VIOLET }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Comply</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <ShieldCheck size={14} /> warehouse-amr-v2
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}
          >
            <BookOpen size={11} />
            {standards.length} STANDARDS
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Standards List */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="px-4 pt-4 pb-2">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--muted)" }}>
              Applicable Standards
            </h2>
            <p className="text-[10px] text-gray-500 mb-4 leading-relaxed">Auto-mapped from OhhO Build profile.</p>
          </div>

          <div className="flex-1 overflow-y-auto px-2 pb-4 flex flex-col gap-1">
            {standards.map((std) => {
              const on = activeStandardId === std.id;
              const reqsDone = std.requirements.filter(r => r.status === "done").length;
              const totalReqs = std.requirements.length;
              const progress = reqsDone / totalReqs;
              const isComplete = progress === 1;

              return (
                <button
                  key={std.id}
                  onClick={() => setActiveStandardId(std.id)}
                  className="flex flex-col px-3 py-3 rounded-lg text-left transition-all group"
                  style={{
                    background: on ? VIOLET_DIM : "transparent",
                    border: `1px solid ${on ? "rgba(167,139,250,0.3)" : "transparent"}`,
                  }}
                >
                  <div className="flex items-center justify-between w-full mb-1">
                    <span className="text-[11.5px] font-mono" style={{ color: on ? "#fff" : "var(--muted)" }}>
                      {std.title}
                    </span>
                    {isComplete ? (
                      <CheckCircle2 size={13} color={GREEN} />
                    ) : (
                      <span className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>{reqsDone}/{totalReqs}</span>
                    )}
                  </div>
                  <div className="w-full h-1 bg-white/5 rounded-full mt-1 overflow-hidden">
                    <div className="h-full transition-all" style={{ width: `${progress * 100}%`, background: isComplete ? GREEN : VIOLET }} />
                  </div>
                </button>
              );
            })}
          </div>
        </aside>

        {/* CENTER — Checklist */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b" style={{ borderColor: "var(--border)" }}>
            <h1 className="text-xl font-medium text-white mb-2">{activeStandard.title}</h1>
            <p className="text-sm text-gray-400">{activeStandard.description}</p>
          </div>

          <div className="flex-1 overflow-y-auto p-6">
            <div className="flex flex-col gap-3">
              {activeStandard.requirements.map(req => {
                const isDone = req.status === "done";
                const isReview = req.status === "review";
                return (
                  <div key={req.id} className="flex gap-4 p-4 rounded-xl border transition-colors hover:bg-white/5" style={{ borderColor: "var(--border)", background: "rgba(255,255,255,0.01)" }}>
                    <button 
                      onClick={() => toggleRequirementStatus(activeStandard.id, req.id)}
                      className="shrink-0 mt-0.5"
                    >
                      {isDone ? <CheckCircle2 size={18} color={GREEN} /> : isReview ? <CheckCircle2 size={18} color={AMBER} /> : <Circle size={18} color="rgba(255,255,255,0.2)" />}
                    </button>
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-[11px] font-mono text-violet-300 bg-violet-500/10 px-1.5 rounded">{req.clause}</span>
                        <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>{req.owner}</span>
                        {req.evidence && <span className="text-[10px] font-mono text-emerald-400 border border-emerald-400/30 px-1.5 rounded ml-auto">EVIDENCE ATTACHED</span>}
                      </div>
                      <p className="text-[13px] text-gray-300 leading-relaxed">{req.description}</p>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </section>

        {/* RIGHT — Readiness & Documents */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          
          {/* Readiness Donut */}
          <div className="p-5 border-b flex flex-col items-center" style={{ borderColor: "var(--border)" }}>
            <h2 className="w-full text-left text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Certification Readiness</h2>
            
            <div className="relative w-32 h-32 mb-2">
              <svg viewBox="0 0 100 100" className="transform -rotate-90">
                <circle cx="50" cy="50" r="40" fill="none" stroke="rgba(255,255,255,0.05)" strokeWidth="10" />
                <circle cx="50" cy="50" r="40" fill="none" stroke={VIOLET} strokeWidth="10" strokeDasharray={`${stats.percentage * 251.2} 251.2`} className="transition-all duration-500" />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-[20px] font-medium text-white">{Math.round(stats.percentage * 100)}%</span>
                <span className="text-[9px] font-mono text-violet-400 tracking-wider">READY</span>
              </div>
            </div>
            
            <div className="text-center mt-2">
              <div className="text-[14px] text-white font-mono">{stats.done} / {stats.total}</div>
              <div className="text-[9px] uppercase text-gray-500 font-mono">Requirements Met</div>
            </div>
          </div>

          {/* Documents Export */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Documents</h2>
            <div className="flex flex-col gap-2">
              <button
                onClick={() => downloadText("technical_file.md", generateDocument("technicalFile"))}
                className="flex items-center justify-between p-3 rounded-lg border hover:bg-white/5 transition-colors cursor-pointer"
                style={{ borderColor: "var(--border)" }}
              >
                <div className="flex items-center gap-2">
                  <FileText size={14} color={GREEN} />
                  <span className="text-[12px] font-medium text-gray-300">Technical File</span>
                </div>
                <DownloadCloud size={14} color="var(--muted)" />
              </button>
              <button
                onClick={() => downloadText("risk_assessment.md", generateDocument("riskAssessment"))}
                className="flex items-center justify-between p-3 rounded-lg border hover:bg-white/5 transition-colors cursor-pointer"
                style={{ borderColor: "var(--border)" }}
              >
                <div className="flex items-center gap-2">
                  <FileText size={14} color={GREEN} />
                  <span className="text-[12px] font-medium text-gray-300">Risk Assessment</span>
                </div>
                <DownloadCloud size={14} color="var(--muted)" />
              </button>
              <button
                onClick={() => downloadText("declaration_of_conformity.md", generateDocument("declarationOfConformity"))}
                className="flex items-center justify-between p-3 rounded-lg border hover:bg-white/5 transition-colors cursor-pointer"
                style={{ borderColor: "var(--border)", opacity: stats.percentage === 1 ? 1 : 0.5 }}
              >
                <div className="flex items-center gap-2">
                  <FileCheck size={14} color={stats.percentage === 1 ? GREEN : AMBER} />
                  <span className="text-[12px] font-medium text-gray-300">Declaration of Conformity</span>
                </div>
                <DownloadCloud size={14} color="var(--muted)" />
              </button>
            </div>
          </div>

          {/* Audit Trail */}
          <div className="flex-1 p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Audit Trail</h2>
            <div className="flex items-start gap-3 mb-4">
              <History size={14} color="var(--faint)" className="mt-0.5 shrink-0" />
              <div>
                <p className="text-[11px] text-gray-400 mb-1">PLd evidence added to ISO 13849 by Alex K.</p>
                <p className="text-[9px] font-mono text-gray-600">2026-06-17 10:45 · signed</p>
              </div>
            </div>
            <div className="flex items-start gap-3 mb-4">
              <History size={14} color="var(--faint)" className="mt-0.5 shrink-0" />
              <div>
                <p className="text-[11px] text-gray-400 mb-1">Risk assessment finalized by Sarah J.</p>
                <p className="text-[9px] font-mono text-gray-600">2026-06-16 16:20 · signed</p>
              </div>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
