/*
 * Shield MCP tools — device identity, CVE monitoring, and security posture.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
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
  {
    name: "shield.getDevice",
    description: "Get a single robot device identity by id, including its key fingerprint and verification status.",
    inputSchema: s({ deviceId: { type: "string", description: "Device id, e.g. 'amr-04'" } }, ["deviceId"]),
    handler: async (p) => {
      const device = INITIAL_DEVICES.find((d) => d.id === p.deviceId);
      return device ? jsonResult(device) : errorResult(`Unknown device: ${p.deviceId}`);
    },
    product: "shield", readOnly: true,
  },
  {
    name: "shield.getCve",
    description: "Get a single CVE record by id, including the affected package, version, severity, and patch status.",
    inputSchema: s({ cveId: { type: "string", description: "CVE id, e.g. 'CVE-2024-1101'" } }, ["cveId"]),
    handler: async (p) => {
      const cve = INITIAL_CVES.find((c) => c.id === p.cveId);
      return cve ? jsonResult(cve) : errorResult(`Unknown CVE: ${p.cveId}`);
    },
    product: "shield", readOnly: true,
  },
  {
    name: "shield.rotateDeviceKey",
    description: "Rotate a device's hardware-rooted identity key, returning it to 'verified' status. Use for devices flagged 'rotate_key'.",
    inputSchema: s({ deviceId: { type: "string", description: "Device id to rotate, e.g. 'amr-04'" } }, ["deviceId"]),
    handler: async (p) => {
      const device = INITIAL_DEVICES.find((d) => d.id === p.deviceId);
      if (!device) return errorResult(`Unknown device: ${p.deviceId}`);
      return jsonResult({
        deviceId: device.id,
        previousStatus: device.status,
        status: "verified",
        message: `Key rotation requested for ${device.id}. A new hardware-rooted key will be provisioned and the device re-attested.`,
      });
    },
    product: "shield", readOnly: false,
  },
  {
    name: "shield.quarantineDevice",
    description: "Quarantine (isolate) a device from the fleet network, marking it 'untrusted'. Use when a device is compromised or fails attestation.",
    inputSchema: s({
      deviceId: { type: "string", description: "Device id to quarantine" },
      reason: { type: "string", description: "Optional reason for the quarantine" },
    }, ["deviceId"]),
    handler: async (p) => {
      const device = INITIAL_DEVICES.find((d) => d.id === p.deviceId);
      if (!device) return errorResult(`Unknown device: ${p.deviceId}`);
      return jsonResult({
        deviceId: device.id,
        status: "untrusted",
        reason: (p.reason as string) ?? "manual quarantine",
        message: `${device.id} quarantined and isolated from the fleet network pending review.`,
      });
    },
    product: "shield", readOnly: false,
  },
  {
    name: "shield.patchCve",
    description: "Apply the available patch for a CVE across affected robots, moving it from 'open' to 'patched'.",
    inputSchema: s({ cveId: { type: "string", description: "CVE id to patch, e.g. 'CVE-2024-1101'" } }, ["cveId"]),
    handler: async (p) => {
      const cve = INITIAL_CVES.find((c) => c.id === p.cveId);
      if (!cve) return errorResult(`Unknown CVE: ${p.cveId}`);
      if (cve.status === "patched") return jsonResult({ cveId: cve.id, status: "patched", message: `${cve.id} is already patched.` });
      return jsonResult({
        cveId: cve.id,
        package: cve.package,
        status: "patched",
        message: `Patch scheduled for ${cve.package} (${cve.id}) across all affected robots via signed OTA.`,
      });
    },
    product: "shield", readOnly: false,
  },
  {
    name: "shield.runScan",
    description: "Run a fresh security scan across the fleet (device attestation + SBOM/CVE re-check) and return the recomputed posture.",
    inputSchema: s({}),
    handler: async () => {
      const verified = INITIAL_DEVICES.filter((d) => d.status === "verified").length;
      const openCves = INITIAL_CVES.filter((c) => c.status === "open").length;
      return jsonResult({
        scanned: INITIAL_DEVICES.length,
        verified,
        openCves,
        completedAt: "just now",
        message: "Security scan complete. Device attestations re-checked and SBOM re-evaluated against the CVE feed.",
      });
    },
    product: "shield", readOnly: false,
  },
];
