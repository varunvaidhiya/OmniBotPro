"use client";

/*
 * MarketConsole — the OhhO Market application shell.
 *
 * Three surfaces: skill listings with filters (left), skill detail with
 * Proof verification + deploy (centre), and author profile + marketplace
 * stats (right). Browsing and deploying skills is the core flow.
 */

import Link from "next/link";
import { useMemo, useState } from "react";
import {
  ArrowLeft,
  Search,
  Download,
  Star,
  ShieldCheck,
  Tag,
  TrendingUp,
  Sparkles,
  Upload,
} from "lucide-react";

import { SKILLS, AUTHORS, MARKETPLACE_STATS, getAuthor, type SkillListing } from "@/lib/market/skills";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";

const CYAN = "#00D4FF";
const CYAN_DIM = "rgba(0,212,255,0.10)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";

const CATEGORIES = ["all", "humanoid", "quadruped", "wheeled", "arm"] as const;

export default function MarketConsole() {
  const { config } = useRobot();
  const [activeSkillId, setActiveSkillId] = useState("pick-place-cup");
  const [filter, setFilter] = useState<(typeof CATEGORIES)[number]>("all");
  const [search, setSearch] = useState("");

  const filteredSkills = useMemo(() => {
    let result = SKILLS;
    if (filter !== "all") result = result.filter((s) => s.category === filter);
    if (search.trim()) {
      const q = search.toLowerCase();
      result = result.filter(
        (s) =>
          s.name.toLowerCase().includes(q) ||
          s.description.toLowerCase().includes(q) ||
          s.tags.some((t) => t.includes(q)),
      );
    }
    return result;
  }, [filter, search]);

  const activeSkill = SKILLS.find((s) => s.id === activeSkillId) ?? filteredSkills[0] ?? SKILLS[0];
  const author = getAuthor(activeSkill.authorId);

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
          href="/products/market"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: CYAN }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Market</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Tag size={14} /> skill-marketplace
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: CYAN_DIM, color: CYAN, border: `1px solid ${CYAN}40` }}
          >
            <Download size={11} />
            {MARKETPLACE_STATS.totalSkills} SKILLS
          </span>
          <Link
            href="#"
            className="inline-flex items-center gap-1.5 text-[11px] font-semibold px-3 py-1.5 rounded-lg"
            style={{ background: CYAN, color: "var(--bg)" }}
          >
            <Upload size={12} />
            Publish
          </Link>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_300px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Skill listings */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="market" config={config} />
          </div>

          {/* Search */}
          <div className="px-4 pt-4 pb-2">
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "var(--faint)" }} />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search skills…"
                className="w-full text-[12.5px] pl-9 pr-3 py-2 rounded-lg outline-none"
                style={{ background: "rgba(255,255,255,0.03)", border: "1px solid var(--border)", color: "var(--text)" }}
              />
            </div>
          </div>

          {/* Category filter */}
          <div className="px-4 pb-2">
            <div className="flex flex-wrap gap-1.5">
              {CATEGORIES.map((f) => (
                <button
                  key={f}
                  onClick={() => setFilter(f)}
                  className="text-[10.5px] font-mono uppercase px-2 py-1.5 rounded-md transition-colors"
                  style={{
                    background: filter === f ? CYAN_DIM : "rgba(255,255,255,0.02)",
                    color: filter === f ? "#fff" : "var(--muted)",
                    border: `1px solid ${filter === f ? "rgba(0,212,255,0.3)" : "var(--border)"}`,
                  }}
                >
                  {f}
                </button>
              ))}
            </div>
          </div>

          {/* Listings */}
          <div className="flex-1 overflow-y-auto px-2 pb-4">
            <div className="flex flex-col gap-1 mt-2">
              {filteredSkills.map((s) => {
                const on = activeSkillId === s.id;
                return (
                  <button
                    key={s.id}
                    onClick={() => setActiveSkillId(s.id)}
                    className="flex items-start px-3 py-3 rounded-lg text-left transition-all"
                    style={{
                      background: on ? CYAN_DIM : "transparent",
                      border: `1px solid ${on ? "rgba(0,212,255,0.3)" : "transparent"}`,
                    }}
                  >
                    <span className="w-2 h-2 rounded-full mr-3 mt-1.5 shrink-0" style={{ background: s.accent }} />
                    <span className="flex-1 min-w-0">
                      <span className="text-[12px] font-mono block truncate" style={{ color: on ? "#fff" : "var(--muted)" }}>
                        {s.name}
                      </span>
                      <span className="text-[10px] font-mono block" style={{ color: "var(--faint)" }}>
                        {s.robots.join(", ")} · {Math.round(s.successRate * 100)}% pass
                      </span>
                      <span className="text-[10px] font-mono block" style={{ color: s.price === 0 ? GREEN : CYAN }}>
                        {s.price === 0 ? "FREE" : `$${s.price}`}
                      </span>
                    </span>
                  </button>
                );
              })}
              {filteredSkills.length === 0 && (
                <p className="text-[12px] px-3 py-4" style={{ color: "var(--faint)" }}>
                  No skills match your filters.
                </p>
              )}
            </div>
          </div>
        </aside>

        {/* CENTER — Skill detail */}
 <section className="flex flex-col overflow-y-auto" style={{ background: "var(--bg)" }}>
          <SkillDetail skill={activeSkill} authorName={author?.name ?? ""} />
        </section>

        {/* RIGHT — Author + marketplace stats */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          {author && <AuthorPanel skill={activeSkill} authorId={author.id} />}
          <MarketplaceStats />
        </aside>
      </div>
    </div>
  );
}

function SkillDetail({ skill, authorName }: { skill: SkillListing; authorName: string }) {
  return (
    <div className="p-5">
      {/* Header */}
      <div className="flex items-start gap-3 mb-4">
        <span
          className="w-12 h-12 rounded-xl flex items-center justify-center text-[18px] font-display font-bold shrink-0"
          style={{ background: `${skill.accent}14`, border: `1px solid ${skill.accent}40`, color: skill.accent }}
        >
          {skill.name.charAt(0).toUpperCase()}
        </span>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <h1 className="text-[18px] font-semibold truncate">{skill.name}</h1>
            {skill.signed && (
              <span className="inline-flex items-center gap-1 text-[10px] font-mono px-2 py-0.5 rounded-full" style={{ background: "rgba(52,211,153,0.1)", color: GREEN, border: `1px solid ${GREEN}30` }}>
                <ShieldCheck size={10} /> SIGNED
              </span>
            )}
          </div>
          <div className="flex items-center gap-3 mt-1">
            <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>by {authorName}</span>
            <span className="text-[11px] font-mono" style={{ color: AMBER }}>
              <Star size={10} className="inline mr-0.5" />{skill.rating}
            </span>
            <span className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>{skill.downloads} downloads</span>
          </div>
        </div>
        <div className="text-right shrink-0">
          <div className="text-[22px] font-display font-bold" style={{ color: skill.price === 0 ? GREEN : CYAN }}>
            {skill.price === 0 ? "FREE" : `$${skill.price}`}
          </div>
          <div className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>{skill.version}</div>
        </div>
      </div>

      <p className="text-[14px] leading-[1.7] mb-5" style={{ color: "rgba(255,255,255,0.75)" }}>
        {skill.description}
      </p>

      {/* Tags */}
      <div className="flex flex-wrap gap-1.5 mb-5">
        {skill.tags.map((t) => (
          <span key={t} className="text-[10px] font-mono px-2 py-1 rounded-md" style={{ background: "rgba(255,255,255,0.04)", border: "1px solid var(--border)", color: "var(--muted)" }}>
            #{t}
          </span>
        ))}
      </div>

      {/* Metadata grid */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-5">
        <MetaCard label="Method" value={skill.method} />
        <MetaCard label="Episodes" value={skill.episodes > 0 ? skill.episodes.toLocaleString() : "RL (sim)"} />
        <MetaCard label="Success rate" value={`${Math.round(skill.successRate * 100)}%`} color={skill.successRate > 0.9 ? GREEN : AMBER} />
        <MetaCard label="Robots" value={skill.robots.join(", ")} />
      </div>

      {/* Proof verification */}
      <div className="mb-5">
        <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
          Proof verification
        </h2>
        <div className="flex flex-col gap-2">
          {skill.proof.map((p) => (
            <div key={p.suite} className="flex items-center gap-3 px-4 py-3 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
              <span className="text-[12px] flex-1">{p.suite}</span>
              <div className="w-32 h-2 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,0.05)" }}>
                <div className="h-full rounded-full" style={{ width: `${p.passRate * 100}%`, background: p.passRate > 0.9 ? GREEN : p.passRate > 0.8 ? AMBER : "#F87171" }} />
              </div>
              <span className="text-[11px] font-mono w-12 text-right" style={{ color: p.passRate > 0.9 ? GREEN : AMBER }}>
                {Math.round(p.passRate * 100)}%
              </span>
              <span className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>{p.runs} runs</span>
            </div>
          ))}
        </div>
      </div>

      {/* Deploy button */}
      <div className="flex items-center gap-3">
        <button
          className="inline-flex items-center gap-2 text-[13px] font-semibold px-5 py-2.5 rounded-lg transition-all hover:-translate-y-px"
          style={{ background: CYAN, color: "var(--bg)" }}
        >
          <Download size={15} />
          {skill.price === 0 ? "Download & Deploy" : `Buy & Deploy — $${skill.price}`}
        </button>
        <span className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>
          Deploy via OhhO Serve or Fleet OTA
        </span>
      </div>
    </div>
  );
}

function MetaCard({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="px-3 py-2.5 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
      <div className="text-[9px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--faint)" }}>{label}</div>
      <div className="text-[13px] font-semibold" style={{ color: color ?? "var(--text)" }}>{value}</div>
    </div>
  );
}

function AuthorPanel({ skill, authorId }: { skill: SkillListing; authorId: string }) {
  const author = getAuthor(authorId);
  if (!author) return null;

  return (
    <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
      <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
        Author
      </h2>
      <div className="flex items-center gap-3 mb-4">
        <span
          className="w-12 h-12 rounded-full flex items-center justify-center text-[14px] font-display font-bold"
          style={{ background: `${skill.accent}14`, border: `1px solid ${skill.accent}40`, color: skill.accent }}
        >
          {author.avatar}
        </span>
        <div>
          <div className="flex items-center gap-1.5">
            <span className="text-[14px] font-semibold">{author.name}</span>
            {author.verified && <ShieldCheck size={13} style={{ color: GREEN }} />}
          </div>
          <div className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>
            {author.skillCount} skills · {author.rating} ★
          </div>
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div>
          <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Downloads</div>
          <div className="text-[16px] font-display font-semibold mt-1">{author.totalDownloads.toLocaleString()}</div>
        </div>
        <div>
          <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Revenue</div>
          <div className="text-[16px] font-display font-semibold mt-1" style={{ color: GREEN }}>${author.revenue.toLocaleString()}</div>
        </div>
      </div>
    </div>
  );
}

function MarketplaceStats() {
  return (
    <div className="p-5">
      <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
        Marketplace
      </h2>
      <div className="flex flex-col gap-3">
        <StatRow icon={<Tag size={13} />} label="Total skills" value={`${MARKETPLACE_STATS.totalSkills}`} />
        <StatRow icon={<Download size={13} />} label="Total downloads" value={MARKETPLACE_STATS.totalDownloads.toLocaleString()} />
        <StatRow icon={<Sparkles size={13} />} label="Authors" value={`${MARKETPLACE_STATS.totalAuthors}`} />
        <StatRow icon={<TrendingUp size={13} />} label="Platform take-rate" value={`${Math.round(MARKETPLACE_STATS.takeRate * 100)}%`} />
      </div>
    </div>
  );
}

function StatRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3">
      <span style={{ color: "var(--faint)" }}>{icon}</span>
      <span className="text-[12px] flex-1" style={{ color: "var(--muted)" }}>{label}</span>
      <span className="text-[13px] font-mono font-semibold">{value}</span>
    </div>
  );
}
