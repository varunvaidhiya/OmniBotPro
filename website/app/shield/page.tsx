import type { Metadata } from "next";
import ShieldConsole from "@/components/shield/ShieldConsole";

export const metadata: Metadata = {
  title: "OhhO Shield — Security for robots.",
  description:
    "Give every robot a hardware identity, encrypt its links, sign its updates, and watch its software bill of materials.",
};

export default function ShieldPage() {
  return <ShieldConsole />;
}
