import type { Metadata } from "next";
import GaragePageClient from "./GaragePageClient";
import ConsoleGate from "@/components/auth/ConsoleGate";

export const metadata: Metadata = {
  title: "My Garage — OhhO Console",
  description:
    "Your robot garage — browse, add, and manage your fleet of robots. Choose from 15+ robot categories and dozens of pre-loaded hardware models.",
};

export default function GaragePage() {
  return (
    <ConsoleGate product="garage">
      <GaragePageClient />
    </ConsoleGate>
  );
}
