import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import BridgeConsole from "@/components/bridge/BridgeConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";

export const metadata: Metadata = {
  title: "OhhO Bridge — Connect any robot, even the ones that don't speak ROS.",
  description:
    "Per-brand protocol adapters that translate between native robot SDKs and the OhhO platform. Bridge a Unitree DDS humanoid, a DJI drone or a Modbus arm into standard ROS 2 topics.",
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
