import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import BridgeConsole from "@/components/bridge/BridgeConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";

export const metadata: Metadata = {
  title: "OhhO Bridge — Connect any robot, even the ones that don't speak ROS.",
  description:
    "Protocol adapters that translate between any robot's native protocol and the OhhO platform. Bridge a DDS-native humanoid, a MAVLink drone, a CANopen mobile base, an OPC UA factory cell or a Modbus arm into standard ROS 2 topics.",
};

export default function BridgePage() {
  return (
    <ConsoleGate product="bridge">
      <RobotProvider>
        <BridgeConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
