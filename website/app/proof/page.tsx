import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import ProofConsole from "@/components/proof/ProofConsole";
import { RobotProvider } from "@/lib/garage/RobotContext";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Proof — Prove the robot is safe.",
  description:
    "Run your robot through thousands of simulated scenarios, track regression and coverage, and assemble the evidence into a versioned safety case.",
  robots: NO_INDEX,
};

export default function ProofPage() {
  return (
    <ConsoleGate product="proof">
      <RobotProvider>
        <ProofConsole />
      </RobotProvider>
    </ConsoleGate>
  );
}
