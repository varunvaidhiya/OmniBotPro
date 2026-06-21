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
# → {"status":"ok","server":"ohho-robotics-platform","tools":30,...}

curl https://ohho-robotics.com/api/mcp
# → {"server":{...},"endpoint":"/api/mcp","toolCount":30,...}
```

## Available tools

Tools are namespaced as `<product>.<action>`. Each has a JSON Schema for its
parameters that the AI reads to decide when to call it.

| Product | Tools | Read-only | Write |
|---|---|---|---|
| **garage** | `listRobots`, `getRobot`, `listCategories`, `getRobotConfig` | `addRobot`, `deleteRobot`, `updateRobot` |
| **connect** | `getProtocols`, `getDefaultConfig`, `getProtocol` | `sendVelocity`, `emergencyStop`, `releaseStop` |
| **bridge** | `listAdapters`, `getAdapter`, `getJointMap`, `getTopics` | — |
| **market** | `listSkills`, `searchSkills`, `getSkill`, `listAuthors`, `getStats` | — |
| **twin** | `getState`, `getPredictions`, `getWhatIf`, `getSyncMetrics` | — |
| **care** | `listWorkOrders`, `getWorkOrder`, `getRepairLog`, `getFleetMetrics`, `getDegradation` | `orderPart`, `updateWorkOrder` |
| **fleet** | `listRobots`, `getHealth`, `getAlerts`, `getVersions` | `triggerOTA`, `rollbackOTA` |

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
     ├── lib/garage/mcp-tools.ts     (7 tools, 2 resources)
     ├── lib/connect/mcp-tools.ts    (6 tools)
     ├── lib/bridge/mcp-tools.ts     (4 tools, 2 resources)
     ├── lib/market/mcp-tools.ts     (5 tools, 1 resource)
     ├── lib/twin/mcp-tools.ts       (4 tools)
     ├── lib/care/mcp-tools.ts       (7 tools)
     └── lib/fleet/mcp-tools.ts      (6 tools)
```

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
