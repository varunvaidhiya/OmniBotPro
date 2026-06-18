import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import ProofConsole from "@/components/proof/ProofConsole";

export const metadata: Metadata = {
  title: "OhhO Proof — Prove the robot is safe.",
  description:
    "Run your robot through thousands of simulated scenarios, track regression and coverage, and assemble the evidence into a versioned safety case.",
};

export default function ProofPage() {
  return (
    <ConsoleGate product="proof">
      <ProofConsole />
    </ConsoleGate>
  );
}
