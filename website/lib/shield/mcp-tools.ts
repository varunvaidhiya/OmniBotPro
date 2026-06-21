/*
 * Shield MCP tools — device identity, CVE monitoring, and security posture.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult,
} from "@/lib/mcp/types";
import { INITIAL_DEVICES, INITIAL_CVES } from "@/lib/shield/security";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "shield.listDevices",
    description: "List all robot device identities with their hardware-rooted key fingerprint, verification status (verified/rotate_key/untrusted), and last-seen timestamp.",
    inputSchema: s({}),
    handler: async () => jsonResult(INITIAL_DEVICES),
    product: "shield", readOnly: true,
  },
  {
    name: "shield.listCves",
    description: "List all known CVEs (vulnerabilities) across the fleet's software bill of materials, with package, version, severity (critical/high/medium/low), and patch status.",
    inputSchema: s({
      severity: { type: "string", description: "Filter by severity", enum: ["critical", "high", "medium", "low"] },
      status: { type: "string", description: "Filter by status", enum: ["open", "patched"] },
    }),
    handler: async (p) => {
      let result = INITIAL_CVES;
      if (p.severity) result = result.filter((c) => c.severity === p.severity);
      if (p.status) result = result.filter((c) => c.status === p.status);
      return jsonResult(result);
    },
    product: "shield", readOnly: true,
  },
  {
    name: "shield.getSecurityPosture",
    description: "Get the overall fleet security posture: total devices, verified count, open CVEs by severity, and risk score.",
    inputSchema: s({}),
    handler: async () => {
      const verified = INITIAL_DEVICES.filter((d) => d.status === "verified").length;
      const rotateKey = INITIAL_DEVICES.filter((d) => d.status === "rotate_key").length;
      const untrusted = INITIAL_DEVICES.filter((d) => d.status === "untrusted").length;
      const openCves = INITIAL_CVES.filter((c) => c.status === "open");
      const critical = openCves.filter((c) => c.severity === "critical").length;
      const high = openCves.filter((c) => c.severity === "high").length;
      const riskScore = Math.max(0, 100 - critical * 20 - high * 10 - rotateKey * 5 - untrusted * 15);
      return jsonResult({
        totalDevices: INITIAL_DEVICES.length,
        verified, rotateKey, untrusted,
        openCves: openCves.length,
        criticalCves: critical,
        highCves: high,
        riskScore,
        riskGrade: riskScore >= 90 ? "A" : riskScore >= 80 ? "B" : riskScore >= 70 ? "C" : "D",
      });
    },
    product: "shield", readOnly: true,
  },
];
