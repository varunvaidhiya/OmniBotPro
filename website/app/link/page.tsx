import type { Metadata } from "next";
import ConsoleGate from "@/components/auth/ConsoleGate";
import LinkConsole from "@/components/link/LinkConsole";
import { NO_INDEX } from "@/lib/seo";

export const metadata: Metadata = {
  title: "OhhO Link — Connect any AI agent to your robots",
  description:
    "Connect Claude Desktop, OpenCode, Cursor, Cline, Windsurf, Continue, Zed, or any MCP-compatible AI agent to the OhhO platform. 91 tools spanning every console.",
  robots: NO_INDEX,
};

export default function LinkPage() {
  return (
    <ConsoleGate product="link">
      <LinkConsole />
    </ConsoleGate>
  );
}
