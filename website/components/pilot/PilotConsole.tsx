"use client";

/*
 * PilotConsole — the OhhO Pilot application shell.
 *
 * A teleoperation cockpit for operating a robot remotely. Three surfaces:
 * robot profile picker + sim provider + connection (left), live camera view +
 * virtual joystick (centre), and arm joints + telemetry + e-stop + recording
 * (right).
 *
 * Provider model:
 *   Free tier:  Simulated (built-in), Gazebo Harmonic (ROSBridge)
 *   Pro tier:   NVIDIA Isaac Sim, MuJoCo (requires Fleet/Forge plan)
 *   Enterprise: Isaac Sim 4.0 cloud (requires Forge plan)
 *
 * Recording: any connected provider can record episodes to LeRobot-compatible
 * CSV — building training datasets from simulated or real teleop sessions.
 */

import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Activity,
  ArrowLeft,
  Check,
  Circle,
  Download,
  Gauge,
  Hand,
  Loader2,
  Plug,
  Power,
  Radio,
  Server,
  Video,
  Wifi,
  WifiOff,
  Zap,
} from "lucide-react";

import {
  PROFILES,
  jointsForProfile,
  profileFromConfig,
  type RobotProfile,
} from "@/lib/pilot/robots";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";
import type { RobotConfig } from "@/lib/garage/robot-config";
import {
  CONTROL_MODES,
  applyJoystick,
  initialTeleopState,
  zeroVelocity,
  type ControlMode,
  type TeleopState,
} from "@/lib/pilot/teleop";
import {
  SCENES,
  getScene,
  type ScenePreset,
} from "@/lib/pilot/scene";

import CameraView from "./CameraView";
import VirtualJoystick from "./VirtualJoystick";
import PilotThreeScene from "./PilotThreeScene";
import { SIM_PROVIDERS, tierAccessible, type SimulatorProvider, type SimulatorStatus, type SimProviderMeta, type RecordedEpisode, type ThreeSceneState } from "@/lib/pilot/sim/types";
import { SimulatedProvider } from "@/lib/pilot/sim/simulated";
import { ThreePhysicsProvider } from "@/lib/pilot/sim/three-physics";
import { HybridProvider } from "@/lib/pilot/sim/hybrid";
import { GazeboProvider } from "@/lib/pilot/sim/gazebo";
import { IsaacSimProvider } from "@/lib/pilot/sim/isaac";
import { MuJoCoProvider } from "@/lib/pilot/sim/mujoco";
import { createRecorder } from "@/lib/pilot/sim/recorder";

const VIOLET = "#A78BFA";
const VIOLET_DIM = "rgba(124,58,237,0.10)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

export default function PilotConsole() {
  const { config: robot } = useRobot();
  // The garage-selected robot becomes the default, pre-selected Pilot profile;
  // the built-in PROFILES remain available as manual alternatives.
  const garageProfile = useMemo(() => profileFromConfig(robot), [robot]);
  const [profileId, setProfileId] = useState(garageProfile.id);
  const [sceneId, setSceneId] = useState(SCENES[0].id);
  const [state, setState] = useState<TeleopState>(() =>
    initialTeleopState(garageProfile.armJoints),
  );
  const [activeCamera, setActiveCamera] = useState(garageProfile.cameras[0] ?? "front");

  // ── Sim provider state ──
  const [providerId, setProviderId] = useState("simulated");
  const [providerUrl, setProviderUrl] = useState("ws://localhost:9090");
  const [providerStatus, setProviderStatus] = useState<SimulatorStatus>({
    connected: false, connecting: false, label: "Offline", latency: 0, fps: 0,
  });

  // ── Recording state ──
  const [recording, setRecording] = useState(false);
  const [lastEpisode, setLastEpisode] = useState<RecordedEpisode | null>(null);

  const profiles = useMemo(() => [garageProfile, ...PROFILES], [garageProfile]);
  const profile = useMemo(
    () => profiles.find((p) => p.id === profileId) ?? garageProfile,
    [profiles, profileId, garageProfile],
  );
  const scene = useMemo(() => getScene(sceneId), [sceneId]);
  const joints = useMemo(() => jointsForProfile(profile), [profile]);

  // When the garage robot changes, snap Pilot back to that robot's profile.
  useEffect(() => {
    setProfileId(garageProfile.id);
  }, [garageProfile.id]);

  // ── Sim provider instance (recreated when providerId changes) ──
  const provider = useMemo((): SimulatorProvider => {
    switch (providerId) {
      case "three-physics": return new ThreePhysicsProvider();
      case "hybrid": return new HybridProvider();
      case "gazebo": return new GazeboProvider();
      case "isaac": return new IsaacSimProvider();
      case "mujoco": return new MuJoCoProvider();
      default: return new SimulatedProvider();
    }
  }, [providerId]);

  const providerMeta = useMemo(() => SIM_PROVIDERS.find((p) => p.id === providerId)!, [providerId]);
  const tierInfo = useMemo(() => tierAccessible(providerMeta.tier, "Builder"), [providerMeta.tier]);

  // ── Hybrid bridge state ──
  const [bridgeActive, setBridgeActive] = useState(false);
  const toggleBridge = useCallback(() => {
    if (provider instanceof HybridProvider) {
      provider.toggleBridge(bridgeActive ? undefined : providerUrl);
      setBridgeActive(!bridgeActive);
    }
  }, [provider, providerUrl, bridgeActive]);

  // ── 3-D scene state (for Three.js Physics provider) ──
  const [scene3d, setScene3d] = useState<ThreeSceneState | null>(null);
  useEffect(() => {
    if (!state.connected || providerId !== "three-physics") { setScene3d(null); return; }
    const iv = setInterval(() => {
      if (provider.getSceneObjects) setScene3d(provider.getSceneObjects());
    }, 50);
    return () => clearInterval(iv);
  }, [provider, providerId, state.connected]);

  // listen to provider status changes
  useEffect(() => {
    const unsub = provider.onStatusChange((s) => setProviderStatus(s));
    return unsub;
  }, [provider]);

  // ── Connection sim tick ──
  useEffect(() => {
    const iv = setInterval(() => {
      setState((s) => {
        if (!s.connected) return s;
        return {
          ...s,
          latency: providerStatus.latency,
          uptime: s.uptime + 1,
          msgRate: providerStatus.fps,
        };
      });
    }, 500);
    return () => clearInterval(iv);
  }, [providerStatus]);

  // Re-initialise state when profile changes
  useEffect(() => {
    setState(initialTeleopState(profile.armJoints));
    setActiveCamera(profile.cameras[0] ?? "front");
  }, [profile]);

  // ── Connect / disconnect ──
  const connect = useCallback(async () => {
    setState((s) => ({ ...s, connecting: true }));
    try {
      const status = await provider.connect(providerUrl);
      if (status.connected || providerId === "simulated") {
        setState((s) => ({ ...s, connecting: false, connected: true }));
      } else {
        setState((s) => ({ ...s, connecting: false, connected: false }));
      }
      setProviderStatus(status);
    } catch {
      setState((s) => ({ ...s, connecting: false, connected: false }));
    }
  }, [provider, providerUrl, providerId]);

  const disconnect = useCallback(() => {
    provider.disconnect();
    if (recording) {
      provider.stopRecording();
      setRecording(false);
    }
    setState((s) => ({
      ...s,
      connected: false,
      connecting: false,
      vel: zeroVelocity(),
      latency: 0,
      uptime: 0,
      msgRate: 0,
    }));
  }, [provider, recording]);

  // ── E-stop ──
  const toggleEStop = useCallback(() => {
    setState((s) => {
      const nextEStop = !s.eStop;
      if (nextEStop) provider.emergencyStop();
      else provider.releaseStop();
      return {
        ...s,
        eStop: nextEStop,
        vel: nextEStop ? zeroVelocity() : s.vel,
      };
    });
  }, [provider]);

  // ── Recording ──
  const toggleRecording = useCallback(() => {
    if (recording) {
      const ep = provider.stopRecording();
      if (ep) setLastEpisode(ep);
      setRecording(false);
    } else {
      provider.startRecording("pilot teleop episode");
      setRecording(true);
      setLastEpisode(null);
    }
  }, [provider, recording]);

  const downloadEpisode = useCallback(() => {
    if (!lastEpisode) return;
    provider.downloadLastEpisode();
  }, [provider, lastEpisode]);

  // ── Joystick ──
  const onJoystickMove = useCallback(
    (x: number, y: number) => {
      setState((s) => ({
        ...s,
        vel: applyJoystick(s.vel, x, y, profile.maxLinVel, profile.maxAngVel, s.eStop),
      }));
    },
    [profile],
  );

  const onJoystickRelease = useCallback(() => {
    setState((s) => ({ ...s, vel: zeroVelocity() }));
  }, []);

  // ── Arm sliders ──
  const setArmJoint = useCallback((idx: number, value: number) => {
    setState((s) => {
      const positions = [...s.armPositions];
      positions[idx] = value;
      return { ...s, armPositions: positions };
    });
  }, []);

  // ── Control mode ──
  const setControlMode = useCallback((mode: ControlMode) => {
    setState((s) => ({ ...s, controlMode: mode }));
  }, []);

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
          href="/products/pilot"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: VIOLET }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Pilot</span>
        </Link>

        <div
          className="h-5 w-px mx-1"
          style={{ background: "var(--border-med)" }}
        />

          <span
          className="text-[12.5px] font-mono truncate"
          style={{ color: "var(--muted)" }}
        >
          {providerMeta.name}
        </span>

        <span className="ml-auto flex items-center gap-2">
          {/* Latency */}
          {state.connected && (
            <span
              className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
              style={{
                background: "rgba(255,255,255,.04)",
                color: state.latency > 40 ? AMBER : GREEN,
                border: "1px solid var(--border)",
              }}
            >
              <Activity size={11} />
              {state.latency} ms
            </span>
          )}
          {/* Connection pill */}
          <ConnectionPill state={state} />
          {/* Mode pill */}
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{
              background: "rgba(255,255,255,.04)",
              color: VIOLET,
              border: "1px solid var(--border)",
            }}
          >
            <span
              className="badge-dot"
              style={{ background: VIOLET }}
            />
            {state.controlMode}
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[300px_1fr_320px] gap-px"
        style={{
          background: "var(--border)",
          minHeight: "calc(100vh - 56px)",
        }}
      >
        {/* LEFT — Robot profile + connection */}
        <ProfilePanel
          profiles={profiles}
          robot={robot}
          profileId={profileId}
          onProfile={setProfileId}
          sceneId={sceneId}
          onScene={setSceneId}
          controlMode={state.controlMode}
          onControlMode={setControlMode}
          connected={state.connected}
          connecting={state.connecting}
          onConnect={connect}
          onDisconnect={disconnect}
          providerId={providerId}
          providerMeta={providerMeta}
          onProviderId={setProviderId}
          providerUrl={providerUrl}
          onProviderUrl={setProviderUrl}
          providerStatus={providerStatus}
          recording={recording}
          onToggleRecording={toggleRecording}
          lastEpisode={lastEpisode}
          onDownloadEpisode={downloadEpisode}
          tierInfo={tierInfo}
          bridgeActive={bridgeActive}
          onToggleBridge={toggleBridge}
        />

        {/* CENTER — Camera + joystick */}
        <section
          className="flex flex-col"
          style={{ background: "var(--bg)" }}
        >
          {/* Camera tabs */}
          <div
            className="flex items-center gap-1.5 px-4 py-2 border-b"
            style={{ borderColor: "var(--border)" }}
          >
            {profile.cameras.map((cam) => (
              <button
                key={cam}
                onClick={() => setActiveCamera(cam)}
                className="text-[11px] font-mono uppercase tracking-wider px-2.5 py-1 rounded-lg transition-colors"
                style={{
                  background:
                    activeCamera === cam ? VIOLET_DIM : "transparent",
                  color: activeCamera === cam ? "#fff" : "var(--muted)",
                  border: `1px solid ${
                    activeCamera === cam
                      ? "rgba(124,58,237,.4)"
                      : "transparent"
                  }`,
                }}
              >
                {cam}
              </button>
            ))}
            <span
              className="ml-auto text-[10px] font-mono"
              style={{ color: "var(--faint)" }}
            >
              {profile.name}
            </span>
          </div>

          {/* Camera view */}
          <div className="relative flex-1 min-h-[240px] lg:min-h-0 p-3">
            {providerId === "three-physics" && scene3d ? (
              <div className="rounded-xl overflow-hidden" style={{ width: "100%", height: "100%" }}>
                <PilotThreeScene sceneState={scene3d} width={560} height={320} />
              </div>
            ) : (
              <CameraView
                scene={scene}
                activeCamera={activeCamera}
                connected={state.connected}
                eStop={state.eStop}
              />
            )}
          </div>

          {/* Joystick + velocity strip */}
          <div
            className="border-t px-4 py-4 flex flex-col sm:flex-row items-center gap-4"
            style={{ borderColor: "var(--border)", background: "var(--surf)" }}
          >
            <VirtualJoystick
              size={120}
              onMove={onJoystickMove}
              onRelease={onJoystickRelease}
              accent={VIOLET}
            />
            <VelocityStrip vel={state.vel} latency={state.latency} />
          </div>
        </section>

        {/* RIGHT — Arm + telemetry + e-stop */}
        <aside
          className="flex flex-col overflow-y-auto"
          style={{
            background: "var(--surf)",
            maxHeight: "calc(100vh - 56px)",
          }}
        >
          {/* Arm joints */}
          {joints.length > 0 && (
            <ArmPanel
              joints={joints}
              positions={state.armPositions}
              onSet={setArmJoint}
              connected={state.connected}
              eStop={state.eStop}
            />
          )}

          {/* Telemetry */}
          <TelemetryPanel state={state} />

          {/* E-STOP */}
          <div className="mt-auto p-4">
            <button
              onClick={toggleEStop}
              disabled={!state.connected}
              className="w-full py-4 rounded-xl text-[15px] font-display font-bold tracking-wide transition-all disabled:opacity-30 disabled:cursor-not-allowed"
              style={{
                background: state.eStop
                  ? "rgba(248,113,113,0.25)"
                  : "rgba(248,113,113,0.12)",
                color: RED,
                border: `2px solid ${state.eStop ? RED : "rgba(248,113,113,0.4)"}`,
              }}
            >
              <Power
                size={18}
                className="inline mr-2"
                style={{ verticalAlign: "text-bottom" }}
              />
              {state.eStop ? "RELEASE E-STOP" : "E-STOP"}
            </button>
          </div>
        </aside>
      </div>
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function ConnectionPill({ state }: { state: TeleopState }) {
  const color = state.connected ? GREEN : state.connecting ? AMBER : "var(--muted)";
  const label = state.connected
    ? "Connected"
    : state.connecting
      ? "Connecting…"
      : "Offline";
  const Icon = state.connected ? Wifi : state.connecting ? Loader2 : WifiOff;
  return (
    <span
      className="inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
      style={{
        background: state.connected
          ? "rgba(52,211,153,.12)"
          : "rgba(255,255,255,.04)",
        color,
        border: `1px solid ${state.connected ? GREEN + "44" : "var(--border)"}`,
      }}
    >
      <Icon
        size={12}
        className={state.connecting ? "animate-spin" : ""}
      />
      {label}
    </span>
  );
}

function SectionHead({
  icon,
  title,
  hint,
}: {
  icon: React.ReactNode;
  title: string;
  hint?: string;
}) {
  return (
    <div className="flex items-center gap-2 px-4 pt-4 pb-2">
      <span style={{ color: VIOLET }}>{icon}</span>
      <h2
        className="text-[12px] font-mono uppercase tracking-wider"
        style={{ color: "var(--muted)" }}
      >
        {title}
      </h2>
      {hint && (
        <span
          className="ml-auto text-[10.5px]"
          style={{ color: "var(--faint)" }}
        >
          {hint}
        </span>
      )}
    </div>
  );
}

// ── Left panel ────────────────────────────────────────────────────────────────

function ProfilePanel({
  profiles,
  robot,
  profileId,
  onProfile,
  sceneId,
  onScene,
  controlMode,
  onControlMode,
  connected,
  connecting,
  onConnect,
  onDisconnect,
  providerId,
  providerMeta,
  onProviderId,
  providerUrl,
  onProviderUrl,
  providerStatus,
  recording,
  onToggleRecording,
  lastEpisode,
  onDownloadEpisode,
  tierInfo,
  bridgeActive,
  onToggleBridge,
}: {
  profiles: RobotProfile[];
  robot: RobotConfig;
  profileId: string;
  onProfile: (id: string) => void;
  sceneId: string;
  onScene: (id: string) => void;
  controlMode: ControlMode;
  onControlMode: (m: ControlMode) => void;
  connected: boolean;
  connecting: boolean;
  onConnect: () => void;
  onDisconnect: () => void;
  providerId: string;
  providerMeta: SimProviderMeta;
  onProviderId: (id: string) => void;
  providerUrl: string;
  onProviderUrl: (url: string) => void;
  providerStatus: SimulatorStatus;
  recording: boolean;
  onToggleRecording: () => void;
  lastEpisode: RecordedEpisode | null;
  onDownloadEpisode: () => void;
  tierInfo: { allowed: boolean; badge: string; upgradeMessage?: string };
  bridgeActive: boolean;
  onToggleBridge: () => void;
}) {
  return (
    <aside
      className="flex flex-col overflow-y-auto"
      style={{
        background: "var(--surf)",
        maxHeight: "calc(100vh - 56px)",
      }}
    >
      {/* Sim provider picker */}
      <SectionHead icon={<Server size={15} />} title="Simulation provider" />
      <div className="px-3 flex flex-col gap-1.5">
        {SIM_PROVIDERS.map((p) => {
          const on = providerId === p.id;
          const ti = tierAccessible(p.tier, "Builder");
          const dimmed = !p.available || !ti.allowed;
          const tierColor = p.tier === "free" ? GREEN : p.tier === "pro" ? AMBER : "#A78BFA";
          return (
            <button
              key={p.id}
              onClick={() => {
                if (dimmed) return;
                if (connected) onDisconnect();
                onProviderId(p.id);
              }}
              disabled={dimmed}
              className="text-left p-2.5 rounded-xl transition-all disabled:opacity-45 disabled:cursor-not-allowed"
              style={{
                background: on ? "var(--violet-dim)" : "rgba(255,255,255,.02)",
                border: `1px solid ${on ? "rgba(124,58,237,.4)" : "var(--border)"}`,
              }}
            >
              <div className="flex items-center gap-2">
                <span className="text-[12.5px] font-semibold font-display flex-1">
                  {p.name}
                </span>
                <span className="text-[9px] font-mono px-1.5 py-0.5 rounded" style={{ background: `${tierColor}22`, color: tierColor }}>
                  {ti.badge}
                </span>
                {on && (
                  <span className="w-1.5 h-1.5 rounded-full" style={{ background: VIOLET }} />
                )}
              </div>
              <p className="text-[10.5px] leading-[1.45] mt-1" style={{ color: "var(--muted)" }}>
                {p.desc}
              </p>
              {!ti.allowed && (
                <p className="text-[10px] mt-1" style={{ color: AMBER }}>
                  {ti.upgradeMessage}
                </p>
              )}
              {!p.available && ti.allowed && (
                <p className="text-[10px] mt-1" style={{ color: AMBER }}>
                  Coming soon — {p.setupHint}
                </p>
              )}
            </button>
          );
        })}
      </div>

      {/* Provider URL + Bridge toggle (server-based or hybrid providers) */}
      {providerMeta.requiresServer && (
        <div className="px-3 pt-3">
          <div className="text-[10px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--faint)" }}>Connection URL</div>
          <input
            value={providerUrl}
            onChange={(e) => onProviderUrl(e.target.value)}
            className="w-full bg-transparent text-[11.5px] font-mono px-2 py-2 rounded-lg outline-none focus:border-violet"
            style={{ border: "1px solid var(--border)", color: "#fff" }}
          />
        </div>
      )}

      {/* Hybrid bridge toggle */}
      {providerId === "hybrid" && connected && (
        <div className="px-3 pt-2">
          <div className="text-[10px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--faint)" }}>Bridge to real robot</div>
          <div className="flex gap-1.5">
            <input
              value={providerUrl}
              onChange={(e) => onProviderUrl(e.target.value)}
              placeholder="ws://robot:9090"
              className="flex-1 bg-transparent text-[11px] font-mono px-2 py-2 rounded-lg outline-none focus:border-violet disabled:opacity-40"
              style={{ border: "1px solid var(--border)", color: "#fff" }}
              disabled={bridgeActive}
            />
            <button
              onClick={onToggleBridge}
              className="shrink-0 inline-flex items-center gap-1.5 text-[11px] font-semibold px-3 py-2 rounded-lg transition-all"
              style={{
                background: bridgeActive ? "rgba(52,211,153,.16)" : VIOLET,
                color: bridgeActive ? GREEN : "var(--bg)",
                border: bridgeActive ? `1px solid ${GREEN}66` : "none",
              }}
            >
              {bridgeActive ? "Disconnect" : "Bridge"}
            </button>
          </div>
          {bridgeActive && (
            <p className="text-[10px] mt-1" style={{ color: GREEN }}>
              Bridging messages to {providerUrl} — real robot state overrides simulated.
            </p>
          )}
        </div>
      )}

      {/* Status line */}
      {providerMeta.requiresServer && providerStatus.error && (
        <p className="px-3 pt-1 text-[10.5px] leading-[1.4]" style={{ color: RED }}>
          {providerStatus.error}
        </p>
      )}

      {/* Connect button */}
      <div className="px-3 py-3">
        <button
          onClick={connected ? onDisconnect : onConnect}
          disabled={connecting || (!tierInfo.allowed && providerId !== "simulated")}
          className="w-full inline-flex items-center justify-center gap-2 text-[13px] font-semibold py-2.5 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed enabled:hover:-translate-y-px"
          style={{
            background: connected
              ? "rgba(52,211,153,.16)"
              : VIOLET,
            color: connected ? GREEN : "var(--bg)",
            border: connected
              ? `1px solid ${GREEN}66`
              : "none",
          }}
        >
          {connecting ? (
            <Loader2 size={14} className="animate-spin" />
          ) : connected ? (
            <Check size={14} />
          ) : (
            <Plug size={14} />
          )}
          {connecting
            ? "Connecting…"
            : connected
              ? "Connected · Disconnect"
              : "Connect"}
        </button>
      </div>

      {/* Recording controls */}
      {connected && (
        <div className="border-t" style={{ borderColor: "var(--border)" }}>
          <SectionHead icon={<Video size={15} />} title="Recording" hint={recording ? "● REC" : undefined} />
          <div className="px-3 pb-3 flex flex-col gap-2">
            <button
              onClick={onToggleRecording}
              className="w-full inline-flex items-center justify-center gap-2 text-[12px] font-semibold py-2 rounded-lg transition-all hover:-translate-y-px"
              style={{
                background: recording ? "rgba(248,113,113,0.18)" : "rgba(52,211,153,0.12)",
                color: recording ? RED : GREEN,
                border: `1px solid ${recording ? RED + "55" : GREEN + "55"}`,
              }}
            >
              <Circle size={10} fill={recording ? RED : "transparent"} />
              {recording ? "Stop Recording" : "Start Recording"}
            </button>
            {recording && (
              <p className="text-[10px] text-center" style={{ color: RED }}>
                Recording episode data for training…
              </p>
            )}
            {lastEpisode && !recording && (
              <button
                onClick={onDownloadEpisode}
                className="w-full inline-flex items-center justify-center gap-1.5 text-[11px] font-medium py-2 rounded-lg transition-all hover:bg-white/5"
                style={{ color: GREEN, border: `1px solid ${GREEN}44` }}
              >
                <Download size={12} />
                Download {lastEpisode.id} ({lastEpisode.frameCount} frames)
              </button>
            )}
          </div>
        </div>
      )}

      {/* AI interface — robot enrichment + dynamic panels */}
      <div className="px-3 pb-2">
        <AIRobotPanel consoleId="pilot" config={robot} />
      </div>

      {/* Robot profiles */}
      <SectionHead icon={<Radio size={15} />} title="Robot profile" />
      <div className="px-3 flex flex-col gap-2">
        {profiles.map((p) => {
          const on = profileId === p.id;
          return (
            <button
              key={p.id}
              onClick={() => onProfile(p.id)}
              className="text-left p-2.5 rounded-xl transition-all"
              style={{
                background: on ? VIOLET_DIM : "rgba(255,255,255,.02)",
                border: `1px solid ${
                  on ? "rgba(124,58,237,.4)" : "var(--border)"
                }`,
              }}
            >
              <div className="flex items-center gap-2">
                <span className="text-[13px] font-semibold font-display flex-1">
                  {p.name}
                </span>
                <span
                  className="text-[10px] font-mono px-1.5 py-0.5 rounded"
                  style={{
                    background: "rgba(255,255,255,.05)",
                    color: "var(--muted)",
                  }}
                >
                  {p.baseType}
                </span>
              </div>
              <p
                className="text-[11px] leading-[1.45] mt-1"
                style={{ color: "var(--muted)" }}
              >
                {p.desc}
              </p>
              <div className="flex flex-wrap gap-1.5 mt-2">
                <Tag label={`${p.maxLinVel} m/s`} />
                {p.hasArm && <Tag label={`${p.armJoints}-DOF arm`} />}
                <Tag label={`${p.cameras.length} cam${p.cameras.length > 1 ? "s" : ""}`} />
              </div>
            </button>
          );
        })}
      </div>

      {/* Scene */}
      <SectionHead icon={<Gauge size={15} />} title="Scene" />
      <div className="px-3 flex flex-col gap-1.5">
        {SCENES.map((s) => {
          const on = sceneId === s.id;
          return (
            <button
              key={s.id}
              onClick={() => onScene(s.id)}
              className="flex items-center gap-2 px-2.5 py-2 rounded-lg text-[13px] font-medium transition-colors"
              style={{
                background: on ? VIOLET_DIM : "transparent",
                color: on ? "#fff" : "var(--muted)",
                border: `1px solid ${
                  on ? "rgba(124,58,237,.4)" : "transparent"
                }`,
              }}
            >
              <span className="font-display flex-1 text-left">{s.name}</span>
              <span
                className="text-[10px]"
                style={{ color: "var(--faint)" }}
              >
                {s.desc}
              </span>
            </button>
          );
        })}
      </div>

      {/* Control mode */}
      <SectionHead icon={<Zap size={15} />} title="Control mode" />
      <div className="px-3 flex gap-1.5">
        {CONTROL_MODES.map((m) => {
          const on = controlMode === m.id;
          return (
            <button
              key={m.id}
              onClick={() => onControlMode(m.id)}
              className="flex-1 text-[12px] font-medium py-1.5 rounded-lg transition-colors text-center"
              title={m.desc}
              style={{
                background: on ? VIOLET_DIM : "rgba(255,255,255,.02)",
                color: on ? "#fff" : "var(--muted)",
                border: `1px solid ${
                  on ? "rgba(124,58,237,.4)" : "var(--border)"
                }`,
              }}
            >
              {m.label}
            </button>
          );
        })}
      </div>
    </aside>
  );
}

// ── Velocity readout ──────────────────────────────────────────────────────────

function VelocityStrip({
  vel,
  latency,
}: {
  vel: TeleopState["vel"];
  latency: number;
}) {
  const items = [
    { label: "Lin X", value: `${vel.linearX.toFixed(3)} m/s` },
    { label: "Lin Y", value: `${vel.linearY.toFixed(3)} m/s` },
    { label: "Ang Z", value: `${vel.angularZ.toFixed(3)} rad/s` },
    { label: "Latency", value: latency > 0 ? `${latency} ms` : "—" },
  ];
  return (
    <div className="flex-1 grid grid-cols-2 sm:grid-cols-4 gap-px rounded-xl overflow-hidden" style={{ background: "var(--border)" }}>
      {items.map((it) => (
        <div
          key={it.label}
          className="px-3 py-2.5"
          style={{ background: "var(--bg)" }}
        >
          <div
            className="text-[9.5px] font-mono uppercase tracking-wider truncate"
            style={{ color: "var(--faint)" }}
          >
            {it.label}
          </div>
          <div className="text-[13px] font-display font-semibold mt-0.5 tabular-nums">
            {it.value}
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Right panel: arm joints ───────────────────────────────────────────────────

function ArmPanel({
  joints,
  positions,
  onSet,
  connected,
  eStop,
}: {
  joints: ReturnType<typeof jointsForProfile>;
  positions: number[];
  onSet: (idx: number, val: number) => void;
  connected: boolean;
  eStop: boolean;
}) {
  return (
    <div
      className="border-b"
      style={{ borderColor: "var(--border)" }}
    >
      <SectionHead
        icon={<Hand size={15} />}
        title="Arm · joint targets"
        hint={`${joints.length}-DOF`}
      />
      {!connected && (
        <p className="px-4 pb-2 text-[11px] flex items-center gap-1.5" style={{ color: "rgba(248,113,113,0.6)" }}>
          <span className="w-1.5 h-1.5 rounded-full" style={{ background: RED }} /> Connect to enable arm control
        </p>
      )}
      {connected && eStop && (
        <p className="px-4 pb-2 text-[11px] flex items-center gap-1.5" style={{ color: RED }}>
          <span className="w-1.5 h-1.5 rounded-full" style={{ background: RED }} /> Release E-STOP to move arm
        </p>
      )}
      <div className="px-4 pb-4 flex flex-col gap-3">
        {joints.map((j, i) => {
          const val = positions[i] ?? j.home;
          const disabled = !connected || eStop;
          return (
            <div key={j.name}>
              <div className="flex items-baseline justify-between mb-1">
                <label
                  className="text-[12px]"
                  style={{ color: "var(--muted)" }}
                >
                  {j.label}
                </label>
                <span
                  className="text-[11.5px] font-mono tabular-nums"
                  style={{ color: "#fff" }}
                >
                  {val.toFixed(2)}
                  <span style={{ color: "var(--faint)" }}> rad</span>
                </span>
              </div>
              <input
                type="range"
                min={j.min}
                max={j.max}
                step={0.01}
                value={val}
                disabled={disabled}
                onChange={(e) => onSet(i, parseFloat(e.target.value))}
                className="ohho-range w-full"
                style={{ opacity: disabled ? 0.3 : 1 }}
              />
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ── Right panel: telemetry ────────────────────────────────────────────────────

function TelemetryPanel({ state }: { state: TeleopState }) {
  const { latencyHistory, uptime, msgRate, connected, latency } = state;

  // Build a sparkline path from latency history
  const maxLat = Math.max(1, ...latencyHistory);
  const sparkW = 220;
  const sparkH = 40;
  const points = latencyHistory
    .map((v, i) => {
      const x = (i / (latencyHistory.length - 1)) * sparkW;
      const y = sparkH - (v / maxLat) * sparkH * 0.9;
      return `${i === 0 ? "M" : "L"}${x.toFixed(1)} ${y.toFixed(1)}`;
    })
    .join(" ");
  const areaPath =
    points +
    ` L${sparkW} ${sparkH} L0 ${sparkH} Z`;

  return (
    <div
      className="border-b"
      style={{ borderColor: "var(--border)" }}
    >
      <SectionHead icon={<Activity size={15} />} title="Telemetry" />
      <div className="px-4 pb-4">
        {/* Sparkline */}
        <div
          className="rounded-lg p-3 mb-3"
          style={{
            background: "rgba(255,255,255,.02)",
            border: "1px solid var(--border)",
          }}
        >
          <div className="flex justify-between items-baseline mb-2">
            <span
              className="text-[10px] font-mono uppercase tracking-wider"
              style={{ color: "var(--faint)" }}
            >
              Latency
            </span>
            <span
              className="text-[14px] font-display font-bold tabular-nums"
              style={{
                color: connected
                  ? latency > 40
                    ? AMBER
                    : GREEN
                  : "var(--faint)",
              }}
            >
              {connected ? `${latency} ms` : "—"}
            </span>
          </div>
          <svg
            viewBox={`0 0 ${sparkW} ${sparkH}`}
            width="100%"
            height={sparkH}
            className="block"
          >
            <path d={areaPath} fill={VIOLET} fillOpacity={0.08} />
            <path
              d={points}
              fill="none"
              stroke={VIOLET}
              strokeWidth={1.8}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 gap-2">
          <StatCard
            label="Uptime"
            value={connected ? formatUptime(uptime) : "—"}
          />
          <StatCard
            label="Msg/s"
            value={connected ? `${msgRate}` : "—"}
          />
          <StatCard
            label="Packet loss"
            value={connected ? "0.0%" : "—"}
          />
          <StatCard
            label="Protocol"
            value="ROSBridge"
          />
        </div>
      </div>
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div
      className="px-3 py-2.5 rounded-lg"
      style={{
        background: "rgba(255,255,255,.02)",
        border: "1px solid var(--border)",
      }}
    >
      <div
        className="text-[9px] font-mono uppercase tracking-wider"
        style={{ color: "var(--faint)" }}
      >
        {label}
      </div>
      <div className="text-[13px] font-display font-semibold mt-0.5 tabular-nums">
        {value}
      </div>
    </div>
  );
}

function Tag({ label }: { label: string }) {
  return (
    <span
      className="text-[10px] font-mono px-1.5 py-0.5 rounded"
      style={{
        background: "rgba(255,255,255,.04)",
        color: "var(--muted)",
      }}
    >
      {label}
    </span>
  );
}

function formatUptime(ticks: number): string {
  const totalSec = Math.round(ticks * 0.5);
  const m = Math.floor(totalSec / 60);
  const s = totalSec % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}
