import type { Metadata } from "next";
import { pageSeo } from "@/lib/seo";

export const metadata = pageSeo({
  path: "/team",
  title: "Team",
  description:
    "Meet Varun Vaidhiya, the founder building the future of embodied AI.",
});

export default function TeamLayout({ children }: { children: React.ReactNode }) {
  return children;
}
