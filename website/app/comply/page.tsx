import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import ComplyConsole from "@/components/comply/ComplyConsole";

export const metadata: Metadata = {
  title: "OhhO Comply — Ship robots the regulators will pass.",
  description:
    "Turn robot safety standards into a guided checklist and auto-generate the technical file and audit trail.",
};

export default function ComplyPage() {
  return (
    <ConsoleGate product="comply">
      <ComplyConsole />
    </ConsoleGate>
  );
}
