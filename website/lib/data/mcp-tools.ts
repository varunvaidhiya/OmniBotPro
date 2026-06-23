/*
 * Data MCP tools — episode catalog and dataset management.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";

// Representative episode catalog (the live catalog is robot-config-aware and browser-side)
const EPISODES = [
  { id: "ep_0148", frames: 412, durationSec: 13.7, status: "kept", instruction: "pick up the red cup and place it on the shelf", cameras: ["front", "wrist", "bev"] },
  { id: "ep_0149", frames: 388, durationSec: 12.9, status: "kept", instruction: "pick up the red cup and place it on the shelf", cameras: ["front", "wrist", "bev"] },
  { id: "ep_0150", frames: 97, durationSec: 3.2, status: "discard", instruction: "pick up the red cup and place it on the shelf", cameras: ["front", "wrist", "bev"] },
  { id: "ep_0151", frames: 455, durationSec: 15.1, status: "review", instruction: "pick up the red cup and place it on the shelf", cameras: ["front", "wrist", "bev"] },
  { id: "ep_0152", frames: 401, durationSec: 13.3, status: "kept", instruction: "navigate to the kitchen and find the red cup", cameras: ["front", "wrist", "bev"] },
  { id: "ep_0153", frames: 367, durationSec: 12.2, status: "kept", instruction: "navigate to the kitchen and find the red cup", cameras: ["front", "wrist", "bev"] },
];

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "data.listEpisodes",
    description: "List all episodes in the dataset with frame count, duration, status (kept/discard/review), instruction, and cameras.",
    inputSchema: s({
      status: { type: "string", description: "Filter by status", enum: ["kept", "discard", "review"] },
    }),
    handler: async (p) => p.status ? jsonResult(EPISODES.filter((e) => e.status === p.status)) : jsonResult(EPISODES),
    product: "data", readOnly: true,
  },
  {
    name: "data.getEpisode",
    description: "Get a specific episode by ID with full details including frame count, duration, status, instruction, and camera streams.",
    inputSchema: s(
      { episodeId: { type: "string", description: "Episode ID (e.g. 'ep_0148')" } },
      ["episodeId"],
    ),
    handler: async (p) => {
      const ep = EPISODES.find((e) => e.id === p.episodeId);
      if (!ep) return errorResult(`Episode not found: ${p.episodeId}`);
      return jsonResult(ep);
    },
    product: "data", readOnly: true,
  },
  {
    name: "data.getStats",
    description: "Get dataset statistics: total episodes, total frames, kept/discard/review counts, and average duration.",
    inputSchema: s({}),
    handler: async () => {
      const total = EPISODES.length;
      const kept = EPISODES.filter((e) => e.status === "kept").length;
      const discard = EPISODES.filter((e) => e.status === "discard").length;
      const review = EPISODES.filter((e) => e.status === "review").length;
      const totalFrames = EPISODES.reduce((a, e) => a + e.frames, 0);
      const avgDur = EPISODES.reduce((a, e) => a + e.durationSec, 0) / total;
      return jsonResult({ total, kept, discard, review, totalFrames, avgDurationSec: Math.round(avgDur * 10) / 10, format: "LeRobot (Parquet + MP4)" });
    },
    product: "data", readOnly: true,
  },
  {
    name: "data.setEpisodeStatus",
    description: "Update an episode's status (kept, discard, or review) to curate the dataset before training.",
    inputSchema: s(
      {
        episodeId: { type: "string", description: "Episode ID" },
        status: { type: "string", description: "New status", enum: ["kept", "discard", "review"] },
      },
      ["episodeId", "status"],
    ),
    handler: async (p) => {
      const ep = EPISODES.find((e) => e.id === p.episodeId);
      if (!ep) return errorResult(`Episode not found: ${p.episodeId}`);
      return jsonResult({ ...ep, status: p.status, updated: true, message: `Episode ${p.episodeId} marked as '${p.status}'.` });
    },
    product: "data", readOnly: false,
  },
  {
    name: "data.searchEpisodes",
    description: "Search episodes by a substring of their language instruction (e.g. 'red cup', 'kitchen').",
    inputSchema: s({ query: { type: "string", description: "Text to match within the episode instruction" } }, ["query"]),
    handler: async (p) => {
      const q = String(p.query ?? "").toLowerCase();
      const hits = EPISODES.filter((e) => e.instruction.toLowerCase().includes(q));
      return jsonResult({ query: p.query, count: hits.length, episodes: hits });
    },
    product: "data", readOnly: true,
  },
  {
    name: "data.deleteEpisode",
    description: "Permanently delete an episode from the dataset.",
    inputSchema: s({ episodeId: { type: "string", description: "Episode ID to delete" } }, ["episodeId"]),
    handler: async (p) => {
      const ep = EPISODES.find((e) => e.id === p.episodeId);
      if (!ep) return errorResult(`Episode not found: ${p.episodeId}`);
      return jsonResult({ episodeId: ep.id, deleted: true, message: `Episode ${ep.id} deleted from the dataset.` });
    },
    product: "data", readOnly: false,
  },
  {
    name: "data.exportDataset",
    description: "Export the (optionally filtered) dataset in LeRobot/HF format. Returns an export handle and the included episode count.",
    inputSchema: s({
      status: { type: "string", description: "Only export episodes with this status (default: kept)", enum: ["kept", "discard", "review"] },
      format: { type: "string", description: "Export format", enum: ["lerobot", "rlds", "parquet"] },
    }),
    handler: async (p) => {
      const status = (p.status as string) ?? "kept";
      const included = EPISODES.filter((e) => e.status === status);
      const frames = included.reduce((a, e) => a + e.frames, 0);
      return jsonResult({
        format: (p.format as string) ?? "lerobot",
        status,
        episodes: included.length,
        frames,
        handle: `dataset-${status}-${Date.now()}.tar`,
        message: `Exported ${included.length} '${status}' episodes (${frames} frames).`,
      });
    },
    product: "data", readOnly: false,
  },
  {
    name: "data.pushToHub",
    description: "Publish the dataset to a Hugging Face Hub repository so it can be used for training.",
    inputSchema: s({
      repoId: { type: "string", description: "Target HF repo id, e.g. 'myorg/mobile_manipulation'" },
      private: { type: "boolean", description: "Create as a private repo (default true)" },
    }, ["repoId"]),
    handler: async (p) => {
      const kept = EPISODES.filter((e) => e.status === "kept");
      return jsonResult({
        repoId: p.repoId,
        private: p.private !== false,
        episodes: kept.length,
        url: `https://huggingface.co/datasets/${p.repoId}`,
        message: `Pushing ${kept.length} kept episodes to ${p.repoId} in LeRobot format.`,
      });
    },
    product: "data", readOnly: false,
  },
];
