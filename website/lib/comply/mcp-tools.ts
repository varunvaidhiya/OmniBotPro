/*
 * Comply MCP tools — safety standards, requirements, and document generation.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";
import { type RequirementStatus } from "@/lib/comply/standards";

// The standards data is defined inside the useCompliance hook as INITIAL_STANDARDS.
// We replicate the static data here so MCP tools can serve it without React.
const STANDARDS = [
  {
    id: "eu-machinery",
    title: "EU Machinery Regulation · CE",
    description: "European Machinery Regulation and CE marking requirements.",
    requirements: [
      { id: "r1", clause: "Annex III", description: "Technical documentation", status: "done" as RequirementStatus, owner: "Engineering", evidence: "Technical file v2.1" },
      { id: "r2", clause: "Annex V", description: "Risk assessment", status: "done" as RequirementStatus, owner: "Safety", evidence: "ISO 12100 hazard analysis" },
      { id: "r3", clause: "Art. 12", description: "Declaration of Conformity", status: "review" as RequirementStatus, owner: "Legal", evidence: "Draft DoC" },
      { id: "r4", clause: "Annex I", description: "Essential health and safety requirements", status: "done" as RequirementStatus, owner: "Engineering", evidence: "EHSR checklist" },
      { id: "r5", clause: "Art. 17", description: "EU declaration of conformity", status: "open" as RequirementStatus, owner: "Legal", evidence: "" },
    ],
  },
  {
    id: "iso-10218",
    title: "ISO 10218-1/2",
    description: "Safety requirements for industrial robots and robot systems.",
    requirements: [
      { id: "r1", clause: "5.2", description: "Protective stop", status: "done" as RequirementStatus, owner: "Engineering", evidence: "E-stop tested" },
      { id: "r2", clause: "5.7", description: "Speed monitoring", status: "review" as RequirementStatus, owner: "Engineering", evidence: "Speed limits verified" },
      { id: "r3", clause: "5.10", description: "Collision avoidance", status: "done" as RequirementStatus, owner: "Safety", evidence: "Nav2 costmap tested" },
    ],
  },
  {
    id: "iso-13849",
    title: "ISO 13849-1 · PL d",
    description: "Functional safety of machine controls — Performance Level d.",
    requirements: [
      { id: "r1", clause: "4.3", description: "Safety-related control functions", status: "review" as RequirementStatus, owner: "Engineering", evidence: "SRP/CS analysis" },
      { id: "r2", clause: "4.5", description: "Performance level determination", status: "review" as RequirementStatus, owner: "Safety", evidence: "MTTFd calculation" },
      { id: "r3", clause: "5.2", description: "Category 3 architecture", status: "open" as RequirementStatus, owner: "Engineering", evidence: "" },
    ],
  },
  {
    id: "ul-60204",
    title: "UL / IEC 60204-1",
    description: "Electrical safety of machinery.",
    requirements: [
      { id: "r1", clause: "7.2", description: "Emergency stop category", status: "done" as RequirementStatus, owner: "Engineering", evidence: "Category 0 stop" },
      { id: "r2", clause: "9.4", description: "Equipment protection", status: "open" as RequirementStatus, owner: "Engineering", evidence: "" },
    ],
  },
];

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "comply.listStandards",
    description: "List all applicable safety standards (EU Machinery Reg, ISO 10218, ISO 13849, UL/IEC 60204) with their requirements and completion status.",
    inputSchema: s({}),
    handler: async () => jsonResult(STANDARDS),
    product: "comply", readOnly: true,
  },
  {
    name: "comply.getStandard",
    description: "Get a specific standard with all its requirements, owners, and evidence.",
    inputSchema: s(
      { standardId: { type: "string", description: "Standard ID", enum: ["eu-machinery", "iso-10218", "iso-13849", "ul-60204"] } },
      ["standardId"],
    ),
    handler: async (p) => {
      const std = STANDARDS.find((s) => s.id === p.standardId);
      if (!std) return errorResult(`Standard not found: ${p.standardId}`);
      return jsonResult(std);
    },
    product: "comply", readOnly: true,
  },
  {
    name: "comply.getProgress",
    description: "Get the overall compliance progress: total requirements, completed, percentage, and per-standard breakdown.",
    inputSchema: s({}),
    handler: async () => {
      const allReqs = STANDARDS.flatMap((s) => s.requirements);
      const done = allReqs.filter((r) => r.status === "done").length;
      const review = allReqs.filter((r) => r.status === "review").length;
      const open = allReqs.filter((r) => r.status === "open").length;
      return jsonResult({
        total: allReqs.length,
        done, review, open,
        percentage: Math.round((done / allReqs.length) * 100),
        perStandard: STANDARDS.map((s) => ({
          id: s.id, title: s.title,
          total: s.requirements.length,
          done: s.requirements.filter((r) => r.status === "done").length,
        })),
      });
    },
    product: "comply", readOnly: true,
  },
  {
    name: "comply.generateDocument",
    description: "Generate a compliance document (technical file, risk assessment, or Declaration of Conformity) as a markdown string. Returns the document text.",
    inputSchema: s(
      { docType: { type: "string", description: "Document type to generate", enum: ["technicalFile", "riskAssessment", "declarationOfConformity"] } },
      ["docType"],
    ),
    handler: async (p) => {
      const docType = p.docType as string;
      const titles: Record<string, string> = {
        technicalFile: "Technical Construction File",
        riskAssessment: "Risk Assessment (ISO 12100)",
        declarationOfConformity: "Declaration of Conformity",
      };
      const content = `# ${titles[docType] ?? docType}\n\nGenerated: ${new Date().toISOString()}\n\n## Applicable Standards\n${STANDARDS.map((s) => `- ${s.title} (${s.requirements.length} requirements)`).join("\n")}\n\n## Requirements Summary\n${STANDARDS.flatMap((s) => s.requirements.map((r) => `- [${r.status.toUpperCase()}] ${s.title} §${r.clause}: ${r.description}`)).join("\n")}\n`;
      return jsonResult({ docType, content, standards: STANDARDS.length });
    },
    product: "comply", readOnly: true,
  },
  {
    name: "comply.getRequirement",
    description: "Get a single requirement within a standard, including its clause, owner, evidence, and status.",
    inputSchema: s({
      standardId: { type: "string", description: "Standard ID", enum: ["eu-machinery", "iso-10218", "iso-13849", "ul-60204"] },
      requirementId: { type: "string", description: "Requirement ID within the standard, e.g. 'r3'" },
    }, ["standardId", "requirementId"]),
    handler: async (p) => {
      const std = STANDARDS.find((x) => x.id === p.standardId);
      if (!std) return errorResult(`Standard not found: ${p.standardId}`);
      const req = std.requirements.find((r) => r.id === p.requirementId);
      if (!req) return errorResult(`Requirement not found: ${p.requirementId} in ${p.standardId}`);
      return jsonResult({ standardId: std.id, ...req });
    },
    product: "comply", readOnly: true,
  },
  {
    name: "comply.updateRequirement",
    description: "Update a compliance requirement's status (open/review/done), and optionally its owner and evidence. Use to record progress against a standard.",
    inputSchema: s({
      standardId: { type: "string", description: "Standard ID", enum: ["eu-machinery", "iso-10218", "iso-13849", "ul-60204"] },
      requirementId: { type: "string", description: "Requirement ID within the standard, e.g. 'r3'" },
      status: { type: "string", description: "New status", enum: ["open", "review", "done"] },
      owner: { type: "string", description: "New owner (optional)" },
      evidence: { type: "string", description: "Evidence reference/description (optional)" },
    }, ["standardId", "requirementId", "status"]),
    handler: async (p) => {
      const std = STANDARDS.find((x) => x.id === p.standardId);
      if (!std) return errorResult(`Standard not found: ${p.standardId}`);
      const req = std.requirements.find((r) => r.id === p.requirementId);
      if (!req) return errorResult(`Requirement not found: ${p.requirementId} in ${p.standardId}`);
      return jsonResult({
        standardId: std.id,
        requirementId: req.id,
        clause: req.clause,
        previousStatus: req.status,
        status: p.status,
        owner: (p.owner as string) ?? req.owner,
        evidence: (p.evidence as string) ?? req.evidence,
        message: `${std.title} §${req.clause} updated to '${p.status}'.`,
      });
    },
    product: "comply", readOnly: false,
  },
  {
    name: "comply.exportAuditPackage",
    description: "Bundle all compliance documents and evidence for every standard into a single audit package for a notified body or auditor.",
    inputSchema: s({ format: { type: "string", description: "Package format", enum: ["zip", "pdf"] } }),
    handler: async (p) => {
      const allReqs = STANDARDS.flatMap((x) => x.requirements);
      const done = allReqs.filter((r) => r.status === "done").length;
      return jsonResult({
        format: (p.format as string) ?? "zip",
        standards: STANDARDS.length,
        requirements: allReqs.length,
        completionPct: Math.round((done / allReqs.length) * 100),
        handle: `audit-package-${Date.now()}.${(p.format as string) ?? "zip"}`,
        message: "Audit package compiled with all technical files, risk assessments, and the conformity declaration.",
      });
    },
    product: "comply", readOnly: false,
  },
];
