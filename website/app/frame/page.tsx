import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import FrameConsole from "@/components/frame/FrameConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Frame — Edge computation OS.",
  description:
    "A lean, real-time Linux container optimized for high-frequency control loops and neural network inference.",
  robots: NO_INDEX,
};

export default function FramePage() {
  return (
    <ConsoleGate product="frame">
      <RobotProvider>
        <FrameConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
