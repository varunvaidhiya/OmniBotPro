import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import TrainConsole from "@/components/train/TrainConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";

export const metadata: Metadata = {
  title: "OhhO Train — Turn demonstrations into policies.",
  description:
    "Fine-tune VLA, imitation and reinforcement-learning policies with live loss/success curves, then export straight to Serve and Fleet.",
};

export default function TrainPage() {
  return (
    <ConsoleGate product="train">
      <RobotProvider>
        <TrainConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
