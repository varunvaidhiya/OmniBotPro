import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Team | OhhO — Robotics, Operated.",
  description: "Meet Varun Vaidhiya, the founder building the future of embodied AI.",
};

export default function TeamLayout({ children }: { children: React.ReactNode }) {
  return children;
}
