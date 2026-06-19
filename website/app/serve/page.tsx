import type { Metadata } from "next";
import ServeConsole from "@/components/serve/ServeConsole";
import ConsoleGate from "@/components/auth/ConsoleGate";
import { RobotProvider } from "@/lib/garage/RobotContext";

export const metadata: Metadata = {
  title: "OhhO Serve — Robot AI inference, as an API",
  description:
    "Deploy a Vision-Language-Action model behind a REST endpoint, call /predict with an image and instruction, and watch latency, throughput and GPU metrics live — a working console for the OhhO Serve inference server.",
};

// Fully client-side console (live metrics, simulated inference, generated client
// code) — nothing to pre-render but its shell. Gated behind an active plan.
export default function ServePage() {
  return (
    <ConsoleGate product="serve">
      <RobotProvider>
        <ServeConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
