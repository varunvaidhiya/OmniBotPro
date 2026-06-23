# OhhO MCP Server — Setup & Developer Guide

The OhhO platform exposes a **Model Context Protocol (MCP) server** at
`/api/mcp` that lets any AI agent (OpenCode, Claude Desktop, Cursor, or any
MCP-compatible client) query and control every console on the platform.

## Quick start

### 1. Set the API key (production)

```bash
# In your Vercel project settings → Environment Variables
MCP_API_KEY=your-secret-key-here
```

In development (no key set), the endpoint is open.

### 2. Configure your AI client

#### OpenCode

Add to `.opencode/opencode.json` or `~/.config/opencode/opencode.json`:

```json
{
  "mcpServers": {
    "ohho": {
      "type": "streamable-http",
      "url": "https://ohho-robotics.com/api/mcp",
      "headers": {
        "X-API-Key": "your-secret-key-here"
      }
    }
  }
}
```

#### Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "ohho": {
      "command": "npx",
      "args": ["mcp-remote", "https://ohho-robotics.com/api/mcp"],
      "env": {
        "MCP_API_KEY": "your-secret-key-here"
      }
    }
  }
}
```

#### Cursor

Add to `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "ohho": {
      "url": "https://ohho-robotics.com/api/mcp",
      "headers": {
        "X-API-Key": "your-secret-key-here"
      }
    }
  }
}
```

#### Any HTTP client (curl)

```bash
# List all tools
curl -X POST https://ohho-robotics.com/api/mcp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-key" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# Call a tool
curl -X POST https://ohho-robotics.com/api/mcp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-key" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"market.listSkills","arguments":{}}}'
```

### 3. Verify

```bash
curl https://ohho-robotics.com/api/mcp/health
# → {"status":"ok","server":"ohho-robotics-platform","tools":165,...}

curl https://ohho-robotics.com/api/mcp
# → {"server":{...},"endpoint":"/api/mcp","toolCount":165,...}
```

## Available tools

Tools are namespaced as `<product>.<action>`. Each has a JSON Schema for its
parameters that the AI reads to decide when to call it. Every one of the 20
product consoles is covered, with both read (query) and write (action) tools.
Call `tools/list` for the live, authoritative set with full schemas.

| Product | Tools | Highlights (read · write) |
|---|---:|---|
| **garage** | 7 + 2 res | `listRobots`, `getRobotConfig` · `addRobot`, `updateRobot`, `deleteRobot` |
| **connect** | 11 | `getStatus`, `getTelemetry`, `getProtocols` · `connect`, `sendVelocity`, `emergencyStop`, `beep` |
| **bridge** | 8 + 2 res | `listAdapters`, `getJointMap`, `getImpedanceDefaults` · `connectAdapter`, `setImpedance` |
| **build** | 8 | `listParts`, `listTemplates`, `validateDesign`, `estimateBom` |
| **frame** | 7 | `listProcesses`, `getLogs` · `startProcess`, `stopProcess`, `restartProcess` |
| **bench** | 9 | `getAssembly`, `getStatus` · `flashFirmware`, `runSelfTest`, `runCalibration` |
| **serve** | 9 | `listModels`, `estimate`, `listScenes` · `predict`, `deploy`, `stop` |
| **view** | 6 + res | `listLayers` · `toggleLayer`, `setLayerVisibility`, `snapshot`, `recordClip` |
| **data** | 8 | `listEpisodes`, `searchEpisodes` · `setEpisodeStatus`, `exportDataset`, `pushToHub` |
| **train** | 8 | `listMethods`, `getStatus`, `getCurve` · `start`, `stop`, `exportCheckpoint`, `launchSweep` |
| **autonomy** | 8 | `getMission`, `listLocations` · `submitMission`, `navigateTo`, `setMode`, `cancelMission` |
| **mind** | 9 | `getStatus`, `getMemory` · `submitGoal`, `setBackend`, `respondToHuman`, `addMemory` |
| **market** | 9 + res | `listSkills`, `searchSkills` · `purchaseSkill`, `installSkill`, `publishSkill`, `rateSkill` |
| **pilot** | 9 | `listProfiles`, `applyJoystick` · `setControlMode`, `commandArm`, `emergencyStop` |
| **fleet** | 10 | `listRobots`, `getHealth`, `getAlerts` · `triggerOTA`, `rollbackOTA`, `restartRobot`, `acknowledgeAlert` |
| **twin** | 7 | `getState`, `getPredictions` · `runWhatIf`, `resync`, `exportReplay` |
| **care** | 9 | `listWorkOrders`, `getDegradation` · `orderPart`, `createWorkOrder`, `updateWorkOrder` |
| **comply** | 7 | `listStandards`, `getProgress` · `updateRequirement`, `generateDocument`, `exportAuditPackage` |
| **shield** | 9 | `listDevices`, `listCves`, `getSecurityPosture` · `rotateDeviceKey`, `patchCve`, `quarantineDevice` |
| **proof** | 7 | `listSuites`, `getVerdict` · `runSuite`, `generateSafetyCase` |

> ~165 tools total. The in-console **OhhO Assistant** (the "Ask OhhO" launcher)
> calls this exact registry, so anything an external MCP client can do, the
> built-in assistant can do too — and new tools are exposed to both automatically.

## Available resources

Resources are URI-addressable data an AI can read directly:

| URI | Description |
|---|---|
| `ohho://garage/robots` | All robots in the user's garage |
| `ohho://garage/categories` | All 15 robot categories |
| `ohho://bridge/adapters` | All protocol adapters |
| `ohho://bridge/jointmap/g1` | Unitree G1 joint-index map |
| `ohho://market/skills` | All marketplace skills |

## Architecture

```
AI Client (OpenCode / Claude / Cursor / curl)
     │
     ▼  POST /api/mcp (JSON-RPC 2.0, X-API-Key header)
     │
  ┌──┴──────────┐
  │ MCP Server   │  lib/mcp/server.ts — JSON-RPC handler
  │ (stateless)  │  lib/mcp/auth.ts  — API key validation
  └──┬──────────┘
     │
  ┌──┴──────────┐
  │ Registry     │  lib/mcp/registry.ts — aggregates all tools
  └──┬──────────┘
     │
     ├── lib/garage/mcp-tools.ts     (tools + resources)
     ├── lib/connect/mcp-tools.ts
     ├── lib/bridge/mcp-tools.ts     (tools + resources)
     ├── lib/market/mcp-tools.ts     (tools + resource)
     ├── … one lib/<product>/mcp-tools.ts per console …
     └── lib/proof/mcp-tools.ts      (20 products, ~165 tools total)
```

The same registry also backs the in-app assistant
(`lib/assistant/agent.ts` → `app/api/assistant/route.ts`), so the "Ask OhhO"
chat on every console drives these tools directly.

No external SDK dependency — the MCP JSON-RPC protocol is implemented
directly, so it runs in Vercel serverless with zero install.

## Developer guide — adding MCP coverage for a new feature

When you add a new feature or option to any console, follow this pattern to
automatically expose it via MCP:

### Step 1: Create or update `lib/<product>/mcp-tools.ts`

```typescript
import {
  type ToolDefinition,
  type InputSchema,
  type JsonSchemaProperty,
  jsonResult,
  errorResult,
} from "@/lib/mcp/types";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "myProduct.myAction",        // dotted, unique
    description: "What this tool does.", // the LLM reads this
    inputSchema: s({
      param1: { type: "string", description: "..." },
      param2: { type: "number", description: "..." },
    }, ["param1"]),
    handler: async (params) => {
      // Your logic here
      return jsonResult({ result: "..." });
    },
    product: "myProduct",
    readOnly: true,  // false for write/mutation tools
  },
];
```

### Step 2: Register in `lib/mcp/registry.ts`

Add one import + one array entry:

```typescript
import { tools as myProductTools } from "@/lib/myProduct/mcp-tools";

const ALL_TOOLS: ToolDefinition[] = [
  // ...existing
  ...myProductTools,
];
```

### Step 3 (optional): Add resources

```typescript
import { type ResourceDefinition } from "@/lib/mcp/types";

export const resources: ResourceDefinition[] = [
  {
    uri: "ohho://myProduct/data",
    name: "My product data",
    description: "Read-only data for my product.",
    mimeType: "application/json",
    product: "myProduct",
    read: async () => JSON.stringify(myData, null, 2),
  },
];
```

Register in `registry.ts`:

```typescript
import { resources as myProductResources } from "@/lib/myProduct/mcp-tools";

const ALL_RESOURCES: ResourceDefinition[] = [
  // ...existing
  ...myProductResources,
];
```

### Step 4: Test

```bash
cd website && npm run test   # mcp.test.ts covers registry + server
```

That's it — the MCP server auto-exposes the new tools. No other file to touch.

## Protocol details

- **Transport**: JSON-RPC 2.0 over HTTP (stateless)
- **Endpoint**: `POST /api/mcp`
- **Auth**: `X-API-Key` header or `Authorization: Bearer` token
- **Methods**: `initialize`, `ping`, `tools/list`, `tools/call`,
  `resources/list`, `resources/read`, `prompts/list`, `logging/setLevel`
- **Batch**: supports JSON-RPC batch (array of requests in one POST)
- **CORS**: `OPTIONS` returns permissive headers for browser-based clients

## Testing

```bash
cd website && npm run test
# → 7 test files, 75 tests (17 MCP-specific)
```

The MCP test suite (`lib/mcp/mcp.test.ts`) covers:
- Registry: tool count, uniqueness, schema validity, product coverage
- Server: initialize, ping, tools/list, tools/call (with params),
  resources/list, resources/read, error handling
