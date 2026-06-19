"use client";

/*
 * GarageView — displays the user's robots in an interactive grid.
 *
 * Shows each robot as a card with:
 *   - Category icon + color accent
 *   - Robot name + hardware model name
 *   - Status badge
 *   - Quick actions (open console, edit, delete)
 */

import { useState } from "react";
import {
  Bot,
  Trash2,
  MoreVertical,
  Settings2,
  ExternalLink,
  Plus,
} from "lucide-react";
import Link from "next/link";

import type { UserRobot, GarageRobot } from "@/lib/garage/types";
import { CATEGORIES } from "@/lib/garage/types";
import {
  getHardwareModel,
  getRobotTypeForHardware,
} from "@/lib/garage/robot-catalog";

interface Props {
  robots: UserRobot[];
  onAddRobot: () => void;
  onDeleteRobot: (id: string) => void;
  selectedRobotId?: string | null;
}

export default function GarageView({ robots, onAddRobot, onDeleteRobot, selectedRobotId }: Props) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const enriched: GarageRobot[] = robots
    .map((ur) => {
      const hw = getHardwareModel(ur.hardwareModelId);
      const type = getRobotTypeForHardware(ur.hardwareModelId);
      if (!hw || !type) return null;
      const cat = CATEGORIES.find((c) => c.id === type.category);
      return { userRobot: ur, robotType: type, hardwareModel: hw, category: cat! };
    })
    .filter(Boolean) as GarageRobot[];

  const handleDelete = async (id: string) => {
    setDeletingId(id);
    onDeleteRobot(id);
  };

  return (
    <div>
      {/* header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="font-display font-bold text-[clamp(22px,3vw,30px)] tracking-tight mb-1">
            Your Garage
          </h2>
          <p className="text-[13px]" style={{ color: "rgba(255,255,255,0.5)" }}>
            {robots.length} robot{robots.length !== 1 ? "s" : ""} in your fleet
          </p>
        </div>
        <button
          onClick={onAddRobot}
          className="flex items-center gap-2 text-[13px] font-semibold px-5 py-2.5 rounded-lg transition-all duration-200 hover:-translate-y-px"
          style={{
            background: "var(--cyan)",
            color: "var(--bg)",
            boxShadow: "0 0 20px rgba(0,212,255,0.15)",
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.boxShadow =
              "0 6px 24px rgba(0,212,255,0.3)";
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.boxShadow =
              "0 0 20px rgba(0,212,255,0.15)";
          }}
        >
          <Plus size={15} />
          Add Robot
        </button>
      </div>

      {/* empty state */}
      {enriched.length === 0 && (
        <EmptyGarage onAdd={onAddRobot} />
      )}

      {/* robot grid */}
      {enriched.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {/* add-new card (always first) */}
          <button
            onClick={onAddRobot}
            className="relative flex flex-col items-center justify-center gap-3 p-6 rounded-2xl min-h-[200px] transition-all duration-200 group"
            style={{
              background: "rgba(255,255,255,0.02)",
              border: "1.5px dashed rgba(255,255,255,0.1)",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = "rgba(0,212,255,0.04)";
              e.currentTarget.style.borderColor = "rgba(0,212,255,0.3)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = "rgba(255,255,255,0.02)";
              e.currentTarget.style.borderColor = "rgba(255,255,255,0.1)";
            }}
          >
            <div
              className="w-11 h-11 rounded-full flex items-center justify-center transition-transform group-hover:scale-110"
              style={{ background: "rgba(0,212,255,0.1)", color: "var(--cyan)" }}
            >
              <Plus size={22} />
            </div>
            <span className="text-[13px] font-semibold" style={{ color: "rgba(255,255,255,0.6)" }}>
              Add New Robot
            </span>
          </button>

          {/* robot cards */}
          {enriched.map((g) => (
              <RobotCard
                key={g.userRobot.id}
                garage={g}
                selected={g.userRobot.id === selectedRobotId}
                menuOpen={openMenuId === g.userRobot.id}
              onToggleMenu={() =>
                setOpenMenuId(
                  openMenuId === g.userRobot.id ? null : g.userRobot.id,
                )
              }
              onCloseMenu={() => setOpenMenuId(null)}
              onDelete={() => handleDelete(g.userRobot.id)}
              deleting={deletingId === g.userRobot.id}
            />
          ))}
        </div>
      )}
    </div>
  );
}

// ── Empty State ─────────────────────────────────────────────────────────────

function EmptyGarage({ onAdd }: { onAdd: () => void }) {
  return (
    <div className="text-center py-16">
      <div
        className="w-16 h-16 rounded-2xl flex items-center justify-center mx-auto mb-5"
        style={{ background: "rgba(0,212,255,0.06)", color: "var(--cyan)" }}
      >
        <Bot size={30} />
      </div>
      <h3 className="font-display font-semibold text-[18px] mb-2">
        No robots in your garage yet
      </h3>
      <p className="text-[13px] mb-6" style={{ color: "rgba(255,255,255,0.45)" }}>
        Add your first robot to get started. Choose from 15 categories and dozens of pre-loaded hardware models.
      </p>
      <div className="flex items-center justify-center gap-3">
        <button
          onClick={onAdd}
          className="flex items-center gap-2 text-[13px] font-semibold px-5 py-2.5 rounded-lg transition-all duration-200 hover:-translate-y-px"
          style={{
            background: "var(--cyan)",
            color: "var(--bg)",
          }}
        >
          <Plus size={15} />
          Add Your First Robot
        </button>
        <span className="text-[12px]" style={{ color: "rgba(255,255,255,0.3)" }}>
          or browse the catalog
        </span>
      </div>
    </div>
  );
}

// ── Robot Card ──────────────────────────────────────────────────────────────

function RobotCard({
  garage,
  selected,
  menuOpen,
  onToggleMenu,
  onCloseMenu,
  onDelete,
  deleting,
}: {
  garage: GarageRobot;
  selected: boolean;
  menuOpen: boolean;
  onToggleMenu: () => void;
  onCloseMenu: () => void;
  onDelete: () => void;
  deleting: boolean;
}) {
  const { userRobot, hardwareModel, category } = garage;
  const catColor = category?.color ?? "var(--cyan)";
  const status = userRobot.status;

  const statusBadge = {
    active: { label: "Active", bg: "rgba(52,211,153,0.12)", color: "#34d399" },
    draft: { label: "Draft", bg: "rgba(255,255,255,0.06)", color: "rgba(255,255,255,0.5)" },
    simulated: { label: "Sim", bg: "rgba(167,139,250,0.12)", color: "#a78bfa" },
    offline: { label: "Offline", bg: "rgba(251,113,133,0.12)", color: "#fb7185" },
  }[status];

  const IconForCategory = getCategoryIcon(category?.id ?? "wheeled");

  return (
    <div className="relative">
      <Link
        href={`/console?robot=${userRobot.id}`}
        className="block p-5 rounded-2xl transition-all duration-250 group/card"
        style={{
          background: selected
            ? catColor + "0d"
            : "rgba(255,255,255,0.03)",
          border: selected
            ? `1.5px solid ${catColor}55`
            : "1px solid rgba(255,255,255,0.06)",
          boxShadow: selected
            ? `0 0 20px ${catColor}11`
            : "none",
        }}
        onMouseEnter={(e) => {
          e.currentTarget.style.background = selected
            ? catColor + "14"
            : "rgba(255,255,255,0.05)";
          e.currentTarget.style.borderColor = selected
            ? catColor + "88"
            : catColor + "33";
          e.currentTarget.style.transform = "translateY(-2px)";
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.background = selected
            ? catColor + "0d"
            : "rgba(255,255,255,0.03)";
          e.currentTarget.style.borderColor = selected
            ? catColor + "55"
            : "rgba(255,255,255,0.06)";
          e.currentTarget.style.transform = "translateY(0)";
        }}
      >
        {/* top row: icon + status + menu */}
        <div className="flex items-start justify-between mb-3">
          <div
            className="w-10 h-10 rounded-xl flex items-center justify-center"
            style={{ background: catColor + "14", color: catColor }}
          >
            <IconForCategory size={20} />
          </div>
          <div className="flex items-center gap-2">
            <span
              className="text-[10px] font-mono px-2 py-0.5 rounded-full"
              style={{ background: statusBadge.bg, color: statusBadge.color }}
            >
              {statusBadge.label}
            </span>
            <button
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onToggleMenu();
              }}
              className="p-1 rounded-md hover:bg-white/5 transition-colors"
              style={{ color: "rgba(255,255,255,0.3)" }}
            >
              <MoreVertical size={14} />
            </button>
          </div>
        </div>

        {/* robot info */}
        <div className="mb-2">
          <div className="font-semibold text-[15px] leading-tight truncate">
            {userRobot.name}
          </div>
          <div className="text-[11px] font-mono mt-1" style={{ color: "rgba(255,255,255,0.4)" }}>
            {hardwareModel.name}
          </div>
        </div>

        {/* specs row */}
        <div className="flex flex-wrap gap-1.5 mt-3">
          {hardwareModel.price !== "—" && (
            <span
              className="text-[10px] font-mono px-1.5 py-0.5 rounded"
              style={{ background: "rgba(255,255,255,0.04)", color: "rgba(255,255,255,0.4)" }}
            >
              {hardwareModel.price}
            </span>
          )}
          <span
            className="text-[10px] font-mono px-1.5 py-0.5 rounded"
            style={{ background: "rgba(255,255,255,0.04)", color: "rgba(255,255,255,0.4)" }}
          >
            {hardwareModel.locomotion}
          </span>
          {hardwareModel.hasArm && (
            <span
              className="text-[10px] font-mono px-1.5 py-0.5 rounded"
              style={{ background: "rgba(167,139,250,0.1)", color: "#a78bfa" }}
            >
              arm
            </span>
          )}
        </div>

        {/* hover "Open Console" hint */}
        <div
          className="absolute bottom-4 right-4 flex items-center gap-1.5 text-[11px] font-semibold opacity-0 group-hover/card:opacity-100 transition-opacity duration-200"
          style={{ color: catColor }}
        >
          <ExternalLink size={12} />
          {selected ? "Deselect" : "Open"}
        </div>
      </Link>

      {/* context menu */}
      {menuOpen && (
        <div className="absolute top-12 right-2 z-30 w-44 py-1.5 rounded-xl overflow-hidden"
          style={{
            background: "rgba(15,22,40,0.98)",
            border: "1px solid rgba(255,255,255,0.1)",
            boxShadow: "0 12px 32px rgba(0,0,0,0.4)",
          }}
        >
          <button
            onClick={(e) => {
              e.stopPropagation();
              onCloseMenu();
            }}
            className="w-full flex items-center gap-2 px-4 py-2 text-[12px] hover:bg-white/5 transition-colors"
            style={{ color: "rgba(255,255,255,0.7)" }}
          >
            <Settings2 size={13} />
            Configure
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              onCloseMenu();
              if (confirm("Delete this robot from your garage?")) onDelete();
            }}
            className="w-full flex items-center gap-2 px-4 py-2 text-[12px] hover:bg-white/5 transition-colors"
            style={{ color: "#fb7185" }}
          >
            <Trash2 size={13} />
            {deleting ? "Deleting…" : "Delete"}
          </button>
        </div>
      )}

      {/* click-away to close menu */}
      {menuOpen && (
        <div
          className="fixed inset-0 z-20"
          onClick={(e) => {
            e.stopPropagation();
            onCloseMenu();
          }}
        />
      )}
    </div>
  );
}

// ── Category Icons ──────────────────────────────────────────────────────────

import {
  Car,
  Footprints,
  User,
  LayoutList,
  Ship,
  Armchair,
  Grid3x3,
  Leaf,
  Waves,
  Rocket,
  HeartPulse,
  Package,
  type LucideIcon,
} from "lucide-react";

const iconMap: Record<string, LucideIcon> = {
  drones: Bot,
  wheeled: Car,
  legged: Footprints,
  humanoid: User,
  tracked: LayoutList,
  marine: Ship,
  "industrial-arm": Armchair,
  "mobile-manipulator": Bot,
  swarm: Grid3x3,
  agricultural: Leaf,
  "underwater-rov": Waves,
  space: Rocket,
  medical: HeartPulse,
  delivery: Package,
  inspection: Package,
};

function getCategoryIcon(catId: string): LucideIcon {
  return iconMap[catId] ?? Bot;
}
