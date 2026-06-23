/*
 * Care MCP tools — predictive maintenance and service workflow.
 *
 * Tools:
 *   care.listWorkOrders    — list maintenance work orders
 *   care.getWorkOrder      — get work order detail
 *   care.getRepairLog      — get repair history log
 *   care.getFleetMetrics   — get fleet maintenance metrics (MTBF, MTTR, downtime)
 *   care.getDegradation    — get motor degradation trend data
 *   care.orderPart         — order a replacement part from the Build BOM
 *   care.updateWorkOrder   — update work order status
 */

import {
  type ToolDefinition,
  jsonResult,
  errorResult,
  type InputSchema,
  type JsonSchemaProperty,
} from "@/lib/mcp/types";
import { WORK_ORDERS, REPAIR_LOG, FLEET_METRICS, type WorkOrderStatus } from "@/lib/care/care-data";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema => ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "care.listWorkOrders",
    description: "List all maintenance work orders with robot ID, component, urgency, status, and predicted failure window.",
    inputSchema: s({
      urgency: { type: "string", description: "Filter by urgency", enum: ["high", "medium", "low"] },
    }),
    handler: async (params) => {
      let result = WORK_ORDERS;
      if (params.urgency) result = result.filter((w) => w.urgency === params.urgency);
      return jsonResult(result);
    },
    product: "care",
    readOnly: true,
  },
  {
    name: "care.getWorkOrder",
    description: "Get full detail for a maintenance work order, including the replacement part from the Build BOM.",
    inputSchema: s(
      { workOrderId: { type: "string", description: "Work order ID (e.g. 'wo-001')" } },
      ["workOrderId"],
    ),
    handler: async (params) => {
      const wo = WORK_ORDERS.find((w) => w.id === params.workOrderId);
      if (!wo) return errorResult(`Work order not found: ${params.workOrderId}`);
      return jsonResult(wo);
    },
    product: "care",
    readOnly: true,
  },
  {
    name: "care.getRepairLog",
    description: "Get the repair history log with technician, part batch, and Comply audit-trail status for each repair.",
    inputSchema: s({}),
    handler: async () => jsonResult(REPAIR_LOG),
    product: "care",
    readOnly: true,
  },
  {
    name: "care.getFleetMetrics",
    description: "Get fleet maintenance metrics: MTBF (mean time between failures), MTTR (mean time to repair), monthly downtime, open work orders, and predictive alert count.",
    inputSchema: s({}),
    handler: async () => jsonResult(FLEET_METRICS),
    product: "care",
    readOnly: true,
  },
  {
    name: "care.getDegradation",
    description: "Get the motor degradation temperature trend for the highest-urgency work order. Returns an array of temperature readings trending toward the failure threshold.",
    inputSchema: s({}),
    handler: async () => {
      const wo = WORK_ORDERS.find((w) => w.urgency === "high");
      if (!wo) return jsonResult({ temps: [], threshold: 85, note: "No high-urgency work orders." });
      // Generate a representative degradation series
      const temps: number[] = [];
      for (let i = 0; i < 60; i++) temps.push(48 + i * 0.5 + Math.random() * 2);
      return jsonResult({ robotId: wo.robotId, component: wo.component, temps, threshold: 85, currentTemp: temps[temps.length - 1], daysToFailure: wo.daysToFailure });
    },
    product: "care",
    readOnly: true,
  },
  {
    name: "care.orderPart",
    description: "Order the replacement part for a work order from the Build BOM. Returns the part details, cost, and lead time.",
    inputSchema: s(
      { workOrderId: { type: "string", description: "Work order ID to order the part for" } },
      ["workOrderId"],
    ),
    handler: async (params) => {
      const wo = WORK_ORDERS.find((w) => w.id === params.workOrderId);
      if (!wo) return errorResult(`Work order not found: ${params.workOrderId}`);
      return jsonResult({
        ordered: true,
        partName: wo.partName,
        cost: wo.partCost,
        leadTimeDays: wo.partLeadTimeDays,
        inStock: wo.partInStock,
        message: `Ordered ${wo.partName} for ${wo.robotId}. Expected delivery in ${wo.partLeadTimeDays} days.`,
      });
    },
    product: "care",
    readOnly: false,
  },
  {
    name: "care.updateWorkOrder",
    description: "Update a work order's status (e.g. mark as 'ordered', 'dispatched', or 'repaired').",
    inputSchema: s(
      {
        workOrderId: { type: "string", description: "Work order ID" },
        status: { type: "string", description: "New status", enum: ["open", "ordered", "dispatched", "repaired"] },
      },
      ["workOrderId", "status"],
    ),
    handler: async (params) => {
      const wo = WORK_ORDERS.find((w) => w.id === params.workOrderId);
      if (!wo) return errorResult(`Work order not found: ${params.workOrderId}`);
      return jsonResult({ ...wo, status: params.status as WorkOrderStatus, updated: true, message: `Work order ${params.workOrderId} status updated to '${params.status}'.` });
    },
    product: "care",
    readOnly: false,
  },
  {
    name: "care.createWorkOrder",
    description: "Create a maintenance work order for a robot component (e.g. after a predictive alert). Returns the new work order.",
    inputSchema: s({
      robotId: { type: "string", description: "Robot id, e.g. 'amr-04'" },
      component: { type: "string", description: "Component needing service, e.g. 'left knee motor'" },
      urgency: { type: "string", description: "Urgency", enum: ["high", "medium", "low"] },
      note: { type: "string", description: "Optional description of the issue" },
    }, ["robotId", "component"]),
    handler: async (params) => {
      const id = `wo-${String(WORK_ORDERS.length + 1).padStart(3, "0")}`;
      return jsonResult({
        id,
        robotId: params.robotId,
        component: params.component,
        urgency: (params.urgency as string) ?? "medium",
        status: "open" as WorkOrderStatus,
        note: (params.note as string) ?? "",
        created: true,
        message: `Work order ${id} created for ${params.robotId} (${params.component}).`,
      });
    },
    product: "care",
    readOnly: false,
  },
  {
    name: "care.getComponentDegradation",
    description: "Get the degradation trend (temperature toward the failure threshold) for a specific work order's component.",
    inputSchema: s({ workOrderId: { type: "string", description: "Work order id" } }, ["workOrderId"]),
    handler: async (params) => {
      const wo = WORK_ORDERS.find((w) => w.id === params.workOrderId);
      if (!wo) return errorResult(`Work order not found: ${params.workOrderId}`);
      const temps: number[] = [];
      for (let i = 0; i < 60; i++) temps.push(48 + i * 0.5 + Math.random() * 2);
      return jsonResult({ robotId: wo.robotId, component: wo.component, temps, threshold: 85, currentTemp: temps[temps.length - 1], daysToFailure: wo.daysToFailure });
    },
    product: "care",
    readOnly: true,
  },
];
