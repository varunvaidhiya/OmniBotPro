"use client";

/*
 * RobotSelector — modal popup for browsing robot types → hardware models
 * and adding them to the user's garage.
 *
 * Two-step flow:
 *   1. Pick a robot category → see robot types → choose one
 *   2. See hardware models for that type → pick a model → name it → add to garage
 */

import { useState } from "react";
import { X, Search, ChevronRight, ChevronLeft, Check, Plus, Bot } from "lucide-react";

import {
  ROBOT_TYPES,
  searchRobots,
  robotTypesByCategory,
} from "@/lib/garage/robot-catalog";
import { CATEGORIES, type RobotCategoryId } from "@/lib/garage/types";
import type { RobotType, HardwareModel } from "@/lib/garage/types";

interface Props {
  onAdd: (name: string, robotTypeId: string, hardwareModelId: string) => void;
  onClose: () => void;
}

type Step = "categories" | "types" | "models" | "add";

export default function RobotSelector({ onAdd, onClose }: Props) {
  const [step, setStep] = useState<Step>("categories");
  const [selectedCategory, setSelectedCategory] = useState<RobotCategoryId | null>(null);
  const [selectedType, setSelectedType] = useState<RobotType | null>(null);
  const [selectedHw, setSelectedHw] = useState<HardwareModel | null>(null);
  const [robotName, setRobotName] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [adding, setAdding] = useState(false);

  const handleCategoryPick = (catId: RobotCategoryId) => {
    setSelectedCategory(catId);
    setStep("types");
  };

  const handleTypePick = (type: RobotType) => {
    setSelectedType(type);
    setStep("models");
  };

  const handleHwPick = (hw: HardwareModel) => {
    setSelectedHw(hw);
    setRobotName(hw.name);
    setStep("add");
  };

  const handleAdd = async () => {
    if (!robotName.trim() || !selectedHw || !selectedType) return;
    setAdding(true);
    onAdd(robotName.trim(), selectedType.id, selectedHw.id);
  };

  const goBack = () => {
    if (step === "types") {
      setStep("categories");
      setSelectedCategory(null);
    } else if (step === "models") {
      setStep("types");
      setSelectedType(null);
    } else if (step === "add") {
      setStep("models");
      setSelectedHw(null);
      setRobotName("");
    }
  };

  const searchResults = searchQuery.trim() ? searchRobots(searchQuery) : null;
  const showSearch = step === "categories" || searchQuery.trim().length > 0;

  return (
    <div className="fixed inset-0 z-[200] flex items-center justify-center">
      {/* backdrop */}
      <div className="absolute inset-0 bg-black/70 backdrop-blur-sm" onClick={onClose} />

      {/* modal */}
      <div
        className="relative w-full max-w-2xl max-h-[85vh] overflow-hidden flex flex-col"
        style={{
          background: "rgba(15, 22, 40, 0.95)",
          border: "1px solid rgba(255,255,255,0.08)",
          borderRadius: "20px",
          boxShadow: "0 25px 60px rgba(0,0,0,0.5)",
        }}
      >
        {/* header */}
        <div
          className="flex items-center justify-between px-6 py-4 shrink-0"
          style={{ borderBottom: "1px solid rgba(255,255,255,0.06)" }}
        >
          <div className="flex items-center gap-3">
            {(step !== "categories" || searchQuery) && (
              <button
                onClick={goBack}
                className="p-1.5 rounded-lg hover:bg-white/5 transition-colors"
                style={{ color: "rgba(255,255,255,0.6)" }}
              >
                <ChevronLeft size={18} />
              </button>
            )}
            <div>
              <h2 className="font-display font-semibold text-[15px]">
                {step === "categories" && "Add a Robot to Your Garage"}
                {step === "types" && "Choose Robot Type"}
                {step === "models" && (selectedType?.name ?? "Select Hardware")}
                {step === "add" && "Add to Garage"}
              </h2>
              <p className="text-[11px] font-mono mt-0.5" style={{ color: "rgba(255,255,255,0.4)" }}>
                {step === "categories" && "Select your robot category to get started"}
                {step === "types" && "Pick a robot type"}
                {step === "models" && "Choose a pre-configured hardware model"}
                {step === "add" && "Give your robot a name"}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg hover:bg-white/5 transition-colors"
            style={{ color: "rgba(255,255,255,0.5)" }}
          >
            <X size={18} />
          </button>
        </div>

        {/* body */}
        <div className="flex-1 overflow-y-auto px-6 py-4 custom-scrollbar">
          {/* search bar */}
          {showSearch && (
            <div className="relative mb-5">
              <Search
                size={14}
                className="absolute left-3 top-1/2 -translate-y-1/2"
                style={{ color: "rgba(255,255,255,0.3)" }}
              />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search robots by name, manufacturer, or type..."
                className="w-full pl-9 pr-4 py-2.5 rounded-lg text-[13px] outline-none transition-all"
                style={{
                  background: "rgba(255,255,255,0.04)",
                  border: "1px solid rgba(255,255,255,0.08)",
                  color: "#fff",
                }}
                onFocus={(e) =>
                  (e.target.style.borderColor = "rgba(0,212,255,0.3)")
                }
                onBlur={(e) =>
                  (e.target.style.borderColor = "rgba(255,255,255,0.08)")
                }
              />
            </div>
          )}

          {/* search results (overrides category view) */}
          {searchResults && searchQuery.trim() && (
            <SearchResultsList
              results={searchResults}
              query={searchQuery}
              onPick={(hw) => {
                const type = ROBOT_TYPES.find((t) =>
                  t.hardwareModels.some((m) => m.id === hw.id),
                );
                if (type) {
                  setSelectedType(type);
                  setSelectedHw(hw);
                  setRobotName(hw.name);
                  setStep("add");
                  setSearchQuery("");
                }
              }}
            />
          )}

          {/* categories grid */}
          {step === "categories" && !searchResults && (
            <div className="grid grid-cols-2 gap-2.5">
              {CATEGORIES.map((cat) => (
                <CategoryCard
                  key={cat.id}
                  category={cat}
                  onClick={() => handleCategoryPick(cat.id)}
                />
              ))}
            </div>
          )}

          {/* robot types list */}
          {step === "types" && selectedCategory && (
            <RobotTypesList
              categoryId={selectedCategory}
              onPick={handleTypePick}
            />
          )}

          {/* hardware models list */}
          {step === "models" && selectedType && (
            <HardwareModelsList type={selectedType} onPick={handleHwPick} />
          )}

          {/* add step */}
          {step === "add" && selectedHw && (
            <AddConfirmation
              name={robotName}
              onChangeName={setRobotName}
              hw={selectedHw}
              onAdd={handleAdd}
              adding={adding}
              onCancel={goBack}
            />
          )}
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ──────────────────────────────────────────────────────────

function CategoryCard({
  category,
  onClick,
}: {
  category: (typeof CATEGORIES)[number];
  onClick: () => void;
}) {
  const Icon = getIcon(category.icon);
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-3 p-4 rounded-xl text-left transition-all duration-200 group"
      style={{
        background: "rgba(255,255,255,0.03)",
        border: "1px solid rgba(255,255,255,0.05)",
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.background = "rgba(255,255,255,0.06)";
        e.currentTarget.style.borderColor = category.color + "44";
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.background = "rgba(255,255,255,0.03)";
        e.currentTarget.style.borderColor = "rgba(255,255,255,0.05)";
      }}
    >
      <div
        className="w-9 h-9 rounded-lg flex items-center justify-center shrink-0 transition-transform group-hover:scale-110"
        style={{ background: category.color + "18", color: category.color }}
      >
        <Icon size={18} />
      </div>
      <div className="min-w-0">
        <div className="font-semibold text-[13px]">{category.label}</div>
        <div className="text-[10px] mt-0.5 leading-relaxed" style={{ color: "rgba(255,255,255,0.4)" }}>
          {category.blurb.slice(0, 55)}…
        </div>
      </div>
      <ChevronRight size={14} className="ml-auto shrink-0" style={{ color: "rgba(255,255,255,0.25)" }} />
    </button>
  );
}

function RobotTypesList({
  categoryId,
  onPick,
}: {
  categoryId: RobotCategoryId;
  onPick: (type: RobotType) => void;
}) {
  const types = robotTypesByCategory(categoryId);
  if (types.length === 0) {
    return (
      <p className="text-center text-[13px] py-8" style={{ color: "rgba(255,255,255,0.4)" }}>
        No robot types available in this category yet.
      </p>
    );
  }
  return (
    <div className="space-y-2">
      {types.map((type) => (
        <button
          key={type.id}
          onClick={() => onPick(type)}
          className="w-full flex items-center gap-4 p-4 rounded-xl text-left transition-all duration-200 group"
          style={{
            background: "rgba(255,255,255,0.03)",
            border: "1px solid rgba(255,255,255,0.05)",
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = "rgba(255,255,255,0.06)";
            e.currentTarget.style.borderColor = "rgba(0,212,255,0.25)";
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = "rgba(255,255,255,0.03)";
            e.currentTarget.style.borderColor = "rgba(255,255,255,0.05)";
          }}
        >
          <Bot size={20} style={{ color: "var(--cyan)" }} />
          <div className="flex-1 min-w-0">
            <div className="font-semibold text-[14px]">{type.name}</div>
            <div className="text-[11px] mt-0.5 leading-relaxed" style={{ color: "rgba(255,255,255,0.4)" }}>
              {type.tagline}
            </div>
            <div className="text-[10px] mt-1 font-mono" style={{ color: "rgba(255,255,255,0.25)" }}>
              {type.hardwareModels.length} hardware model{type.hardwareModels.length !== 1 ? "s" : ""}
            </div>
          </div>
          <ChevronRight size={14} style={{ color: "rgba(255,255,255,0.25)" }} />
        </button>
      ))}
    </div>
  );
}

function HardwareModelsList({
  type,
  onPick,
}: {
  type: RobotType;
  onPick: (hw: HardwareModel) => void;
}) {
  return (
    <div className="space-y-2.5">
      <p className="text-[12px] mb-3" style={{ color: "rgba(255,255,255,0.5)" }}>
        {type.description.slice(0, 160)}…
      </p>
      {type.hardwareModels.map((hw) => (
        <button
          key={hw.id}
          onClick={() => onPick(hw)}
          className="w-full flex items-start gap-4 p-4 rounded-xl text-left transition-all duration-200 group"
          style={{
            background: "rgba(255,255,255,0.03)",
            border: "1px solid rgba(255,255,255,0.05)",
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = "rgba(255,255,255,0.06)";
            e.currentTarget.style.borderColor = "var(--cyan)40";
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = "rgba(255,255,255,0.03)";
            e.currentTarget.style.borderColor = "rgba(255,255,255,0.05)";
          }}
        >
          {/* robot avatar */}
          <div
            className="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style={{ background: "rgba(0,212,255,0.08)" }}
          >
            <svg viewBox="0 0 32 32" className="w-6 h-6" fill="none" stroke="var(--cyan)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <path d={hw.avatarPath} />
            </svg>
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <div className="font-semibold text-[14px]">{hw.name}</div>
              <span
                className="text-[10px] font-mono px-1.5 py-0.5 rounded"
                style={{
                  background: "rgba(0,212,255,0.1)",
                  color: "var(--cyan)",
                }}
              >
                {hw.manufacturer}
              </span>
            </div>
            <div className="text-[11px] mt-1 leading-relaxed" style={{ color: "rgba(255,255,255,0.45)" }}>
              {hw.desc}
            </div>
            <div className="flex flex-wrap gap-2 mt-2">
              {Object.entries(hw.specs).slice(0, 4).map(([key, val]) => (
                <span
                  key={key}
                  className="text-[10px] font-mono px-1.5 py-0.5 rounded"
                  style={{ background: "rgba(255,255,255,0.04)", color: "rgba(255,255,255,0.45)" }}
                >
                  {key}: {val}
                </span>
              ))}
              {hw.price !== "—" && (
                <span
                  className="text-[10px] font-mono px-1.5 py-0.5 rounded"
                  style={{ background: "rgba(34,211,238,0.1)", color: "var(--cyan)" }}
                >
                  {hw.price}
                </span>
              )}
            </div>
          </div>
          <ChevronRight size={14} className="mt-1 shrink-0" style={{ color: "rgba(255,255,255,0.25)" }} />
        </button>
      ))}
    </div>
  );
}

function AddConfirmation({
  name,
  onChangeName,
  hw,
  onAdd,
  adding,
  onCancel,
}: {
  name: string;
  onChangeName: (n: string) => void;
  hw: HardwareModel;
  onAdd: () => void;
  adding: boolean;
  onCancel: () => void;
}) {
  return (
    <div className="space-y-4">
      {/* selected hardware summary */}
      <div
        className="p-4 rounded-xl"
        style={{ background: "rgba(0,212,255,0.05)", border: "1px solid rgba(0,212,255,0.15)" }}
      >
        <div className="flex items-center gap-2 mb-1">
          <Check size={14} style={{ color: "var(--cyan)" }} />
          <span className="font-semibold text-[14px]">{hw.name}</span>
        </div>
        <div className="text-[11px]" style={{ color: "rgba(255,255,255,0.5)" }}>
          {hw.manufacturer} · {hw.price} · {hw.locomotion}
        </div>
      </div>

      {/* name input */}
      <div>
        <label className="text-[11px] font-mono uppercase tracking-wider mb-2 block" style={{ color: "rgba(255,255,255,0.4)" }}>
          Robot Name
        </label>
        <input
          type="text"
          value={name}
          onChange={(e) => onChangeName(e.target.value)}
          placeholder="My awesome robot..."
          className="w-full px-4 py-3 rounded-lg text-[14px] font-medium outline-none transition-all"
          style={{
            background: "rgba(255,255,255,0.05)",
            border: "1px solid rgba(255,255,255,0.1)",
            color: "#fff",
          }}
          onFocus={(e) => (e.target.style.borderColor = "var(--cyan)")}
          onBlur={(e) => (e.target.style.borderColor = "rgba(255,255,255,0.1)")}
        />
        <p className="text-[10px] mt-1.5" style={{ color: "rgba(255,255,255,0.3)" }}>
          This is how your robot will appear in your garage. You can change it later.
        </p>
      </div>

      {/* action buttons */}
      <div className="flex items-center gap-3 pt-2">
        <button
          onClick={onCancel}
          className="text-[13px] font-medium px-4 py-2.5 rounded-lg transition-colors"
          style={{ color: "rgba(255,255,255,0.5)", border: "1px solid rgba(255,255,255,0.1)" }}
        >
          Back
        </button>
        <button
          onClick={onAdd}
          disabled={!name.trim() || adding}
          className="flex-1 flex items-center justify-center gap-2 text-[13px] font-semibold px-4 py-2.5 rounded-lg transition-all duration-200 disabled:opacity-40"
          style={{
            background: "var(--cyan)",
            color: "var(--bg)",
          }}
          onMouseEnter={(e) => {
            if (!adding) e.currentTarget.style.opacity = "0.9";
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.opacity = "1";
          }}
        >
          {adding ? (
            <>
              <span className="animate-spin w-3.5 h-3.5 border-2 border-current border-t-transparent rounded-full" />
              Adding…
            </>
          ) : (
            <>
              <Plus size={14} />
              Add to Garage
            </>
          )}
        </button>
      </div>
    </div>
  );
}

function SearchResultsList({
  results,
  query,
  onPick,
}: {
  results: HardwareModel[];
  query: string;
  onPick: (hw: HardwareModel) => void;
}) {
  if (results.length === 0) {
    return (
      <div className="text-center py-10">
        <Bot size={32} className="mx-auto mb-3" style={{ color: "rgba(255,255,255,0.15)" }} />
        <p className="text-[13px]" style={{ color: "rgba(255,255,255,0.4)" }}>
          No robots found for &ldquo;{query}&rdquo;
        </p>
        <p className="text-[11px] mt-1" style={{ color: "rgba(255,255,255,0.25)" }}>
          Try a different search or browse by category
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <p className="text-[11px] font-mono mb-1" style={{ color: "rgba(255,255,255,0.3)" }}>
        {results.length} result{results.length !== 1 ? "s" : ""} for &ldquo;{query}&rdquo;
      </p>
      {results.slice(0, 15).map((hw) => (
        <button
          key={hw.id}
          onClick={() => onPick(hw)}
          className="w-full flex items-center gap-3 p-3 rounded-xl text-left transition-all duration-200"
          style={{
            background: "rgba(255,255,255,0.03)",
            border: "1px solid rgba(255,255,255,0.05)",
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = "rgba(255,255,255,0.06)";
            e.currentTarget.style.borderColor = "rgba(0,212,255,0.25)";
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = "rgba(255,255,255,0.03)";
            e.currentTarget.style.borderColor = "rgba(255,255,255,0.05)";
          }}
        >
          <Bot size={16} style={{ color: "var(--cyan)" }} />
          <div className="flex-1 min-w-0">
            <div className="text-[13px] font-semibold">{hw.name}</div>
            <div className="text-[10px]" style={{ color: "rgba(255,255,255,0.35)" }}>
              {hw.manufacturer} · {hw.locomotion}
            </div>
          </div>
          <div className="text-[10px] font-mono" style={{ color: "var(--cyan)" }}>{hw.price}</div>
        </button>
      ))}
    </div>
  );
}

// ── Icon map ────────────────────────────────────────────────────────────────

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
  Drone: Bot, // fallback to Bot for drone icon
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
  Search,
  Bot,
};

function getIcon(name: string): LucideIcon {
  return iconMap[name] ?? Bot;
}
