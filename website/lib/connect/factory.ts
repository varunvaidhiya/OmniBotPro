/*
 * Transport factory — turns a (RobotConfig, ConnectionConfig) pair into the
 * right RobotTransport. The single place that knows the protocol → class map.
 */

import type { RobotConfig } from "@/lib/garage/robot-config";
import { RosbridgeTransport } from "./rosbridge";
import { WebSerialTransport } from "./webserial";
import { WebBluetoothTransport } from "./webbluetooth";
import { SimulatedTransport } from "./simulated";
import type { ConnectionConfig, RobotTransport } from "./types";

export function createTransport(robot: RobotConfig, cfg: ConnectionConfig): RobotTransport {
  switch (cfg.protocol) {
    case "rosbridge":
      return new RosbridgeTransport(robot, cfg);
    case "webserial":
      return new WebSerialTransport(robot, cfg);
    case "webbluetooth":
      return new WebBluetoothTransport(robot, cfg);
    case "simulated":
      return new SimulatedTransport(robot, cfg);
    default:
      return new SimulatedTransport(robot, cfg);
  }
}
