import type { Metadata } from "next";
import GaragePageClient from "./GaragePageClient";
import AuthGate from "@/components/auth/AuthGate";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "My Garage — OhhO Console",
  description:
    "Your robot garage — browse, add, and manage your fleet of robots. Choose from 15+ robot categories and dozens of pre-loaded hardware models.",
  robots: NO_INDEX,
};

export default function GaragePage() {
  return (
    <AuthGate>
      <GaragePageClient />
    </AuthGate>
  );
}
