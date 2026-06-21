"use client";

/*
 * RobotConnectionProvider — the ONE live robot connection for the whole app.
 *
 * Mounted once in app/layout.tsx (above the router), so a connection opened from
 * the garage stays alive as the user moves between consoles. Every console reads
 * it with useRobotConnection(); the global ConnectionBar renders from it; and
 * each console can send commands (velocity, joints, e-stop) through it.
 *
 * One robot is connected at a time — connecting a new robot tears down the old
 * link first. All transports are browser-side (WebSocket / Web Serial / Web
 * Bluetooth / simulated), so this works in the static export with no server.
 */

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";

import {
  getRobotConfig,
  defaultRobotConfig,
  type RobotConfig,
} from "@/lib/garage/robot-config";
import type { UserRobot } from "@/lib/garage/types";
import { createTransport } from "./factory";
import { persistConnectionConfig } from "./config";
import type {
  ConnectionConfig,
  ConnectionProtocol,
  RobotTelemetry,
  RobotTransport,
  TransportStatus,
  Velocity,
} from "./types";

interface RobotConnectionValue {
  /** The connected (or connecting) robot, else null. */
  robot: UserRobot | null;
  /** Its capability profile. */
  config: RobotConfig | null;
  protocol: ConnectionProtocol | null;
  status: TransportStatus | null;
  telemetry: RobotTelemetry | null;
  isConnected: boolean;
  isConnecting: boolean;
  eStopEngaged: boolean;
  connect: (robot: UserRobot, cfg: ConnectionConfig) => Promise<TransportStatus>;
  disconnect: () => void;
  sendVelocity: (v: Velocity) => void;
  sendJointCommand: (name: string, position: number) => void;
  emergencyStop: () => void;
  releaseStop: () => void;
}

const noop = () => {};
const RobotConnectionContext = createContext<RobotConnectionValue>({
  robot: null,
  config: null,
  protocol: null,
  status: null,
  telemetry: null,
  isConnected: false,
  isConnecting: false,
  eStopEngaged: false,
  connect: async () => ({ protocol: "simulated", state: "idle", label: "", latencyMs: 0, msgRate: 0 }),
  disconnect: noop,
  sendVelocity: noop,
  sendJointCommand: noop,
  emergencyStop: noop,
  releaseStop: noop,
});

export function RobotConnectionProvider({ children }: { children: React.ReactNode }) {
  const [robot, setRobot] = useState<UserRobot | null>(null);
  const [config, setConfig] = useState<RobotConfig | null>(null);
  const [protocol, setProtocol] = useState<ConnectionProtocol | null>(null);
  const [status, setStatus] = useState<TransportStatus | null>(null);
  const [telemetry, setTelemetry] = useState<RobotTelemetry | null>(null);
  const [eStopEngaged, setEStop] = useState(false);

  const transportRef = useRef<RobotTransport | null>(null);
  const unsubsRef = useRef<Array<() => void>>([]);

  const teardown = useCallback(() => {
    unsubsRef.current.forEach((u) => u());
    unsubsRef.current = [];
    try {
      transportRef.current?.disconnect();
    } catch {
      /* ignore */
    }
    transportRef.current = null;
  }, []);

  const connect = useCallback(
    async (target: UserRobot, cfg: ConnectionConfig): Promise<TransportStatus> => {
      teardown();
      const rc = getRobotConfig(target.hardwareModelId) ?? defaultRobotConfig();
      const transport = createTransport(rc, cfg);
      transportRef.current = transport;
      unsubsRef.current.push(transport.onStatus((s) => setStatus(s)));
      unsubsRef.current.push(transport.onTelemetry((t) => setTelemetry(t)));

      setRobot(target);
      setConfig(rc);
      setProtocol(cfg.protocol);
      setEStop(false);
      setTelemetry(null);
      setStatus(transport.getStatus());

      const result = await transport.connect();
      if (result.state === "connected") {
        void persistConnectionConfig(target, { ...cfg, lastConnectedAt: new Date().toISOString() });
      }
      return result;
    },
    [teardown],
  );

  const disconnect = useCallback(() => {
    teardown();
    setRobot(null);
    setConfig(null);
    setProtocol(null);
    setStatus(null);
    setTelemetry(null);
    setEStop(false);
  }, [teardown]);

  const sendVelocity = useCallback(
    (v: Velocity) => {
      if (eStopEngaged) {
        transportRef.current?.sendVelocity({ linearX: 0, linearY: 0, angularZ: 0 });
        return;
      }
      transportRef.current?.sendVelocity(v);
    },
    [eStopEngaged],
  );

  const sendJointCommand = useCallback((name: string, position: number) => {
    if (eStopEngaged) return;
    transportRef.current?.sendJointCommand(name, position);
  }, [eStopEngaged]);

  const emergencyStop = useCallback(() => {
    setEStop(true);
    transportRef.current?.emergencyStop();
  }, []);

  const releaseStop = useCallback(() => {
    setEStop(false);
    transportRef.current?.releaseStop();
  }, []);

  // Tear the link down if the whole app unmounts.
  useEffect(() => () => teardown(), [teardown]);

  const state = status?.state;
  const value: RobotConnectionValue = {
    robot,
    config,
    protocol,
    status,
    telemetry,
    isConnected: state === "connected",
    isConnecting: state === "connecting" || state === "reconnecting",
    eStopEngaged,
    connect,
    disconnect,
    sendVelocity,
    sendJointCommand,
    emergencyStop,
    releaseStop,
  };

  return (
    <RobotConnectionContext.Provider value={value}>
      {children}
    </RobotConnectionContext.Provider>
  );
}

export function useRobotConnection(): RobotConnectionValue {
  return useContext(RobotConnectionContext);
}
