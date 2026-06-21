/*
 * OhhO Link — AI agent integration catalog.
 *
 * Every AI agent that supports the Model Context Protocol (MCP) can connect
 * to the OhhO platform. This module defines the config snippet and setup
 * instructions for each known agent.
 *
 * To add a new agent: add an entry to AGENTS below. That's it.
 */

export type AgentTransport = "native-mcp" | "streamable-http" | "cli" | "vscode-extension";

export interface AgentConfig {
  id: string;
  name: string;
  /** Short tagline shown on the card. */
  tagline: string;
  /** What kind of MCP transport this agent uses. */
  transport: AgentTransport;
  /** Emoji or short text for the avatar. */
  avatar: string;
  /** Accent color. */
  accent: string;
  /** The config file path the user edits. */
  configFile: string;
  /** The config snippet to paste (JSON string, ready to copy). */
  configSnippet: string;
  /** Step-by-step setup instructions. */
  steps: string[];
  /** Link to the agent's MCP docs (if any). */
  docsUrl?: string;
  /** Is this agent free? */
  free: boolean;
}

const ENDPOINT = "https://ohho-robotics.com/api/mcp";

function configFor(endpoint: string, apiKey = "YOUR-API-KEY"): string {
  return JSON.stringify(
    {
      mcpServers: {
        ohho: {
          type: "streamable-http",
          url: endpoint,
          headers: { "X-API-Key": apiKey },
        },
      },
    },
    null,
    2,
  );
}

function claudeConfig(endpoint: string, apiKey = "YOUR-API-KEY"): string {
  return JSON.stringify(
    {
      mcpServers: {
        ohho: {
          command: "npx",
          args: ["-y", "mcp-remote", endpoint],
          env: { MCP_API_KEY: apiKey },
        },
      },
    },
    null,
    2,
  );
}

export const AGENTS: AgentConfig[] = [
  {
    id: "opencode",
    name: "OpenCode",
    tagline: "The CLI coding agent you're using right now.",
    transport: "streamable-http",
    avatar: "OC",
    accent: "#00D4FF",
    configFile: ".opencode/opencode.json or ~/.config/opencode/opencode.json",
    configSnippet: configFor(ENDPOINT),
    steps: [
      "Open your OpenCode config file.",
      "Add the mcpServers.ohho entry from the snippet.",
      "Restart OpenCode — the OhhO tools appear in /tools.",
    ],
    docsUrl: "https://opencode.ai/docs/mcp",
    free: true,
  },
  {
    id: "claude-desktop",
    name: "Claude Desktop",
    tagline: "Anthropic's desktop app — native MCP support.",
    transport: "native-mcp",
    avatar: "CD",
    accent: "#D97757",
    configFile: "~/Library/Application Support/Claude/claude_desktop_config.json (macOS) or %APPDATA%\\Claude\\claude_desktop_config.json (Windows)",
    configSnippet: claudeConfig(ENDPOINT),
    steps: [
      "Install Claude Desktop from claude.ai/download.",
      "Open the config file (or Settings → Developer → Edit Config).",
      "Add the mcpServers.ohho entry from the snippet.",
      "Restart Claude Desktop — OhhO tools appear in the tool menu.",
    ],
    docsUrl: "https://modelcontextprotocol.io/quickstart/user",
    free: true,
  },
  {
    id: "cursor",
    name: "Cursor",
    tagline: "The AI code editor with native MCP support.",
    transport: "streamable-http",
    avatar: "CU",
    accent: "#A78BFA",
    configFile: "~/.cursor/mcp.json",
    configSnippet: configFor(ENDPOINT),
    steps: [
      "Open Cursor Settings → MCP.",
      "Click 'Add new MCP server' or edit ~/.cursor/mcp.json.",
      "Paste the config snippet.",
      "OhhO tools are available in Cursor's chat and agent modes.",
    ],
    docsUrl: "https://docs.cursor.com/mcp",
    free: false,
  },
  {
    id: "cline",
    name: "Cline",
    tagline: "Autonomous coding agent for VS Code.",
    transport: "streamable-http",
    avatar: "CL",
    accent: "#34D399",
    configFile: "~/.cline/mcp_settings.json or VS Code Settings → Cline → MCP",
    configSnippet: configFor(ENDPOINT),
    steps: [
      "Install the Cline extension in VS Code.",
      "Open Cline's MCP settings.",
      "Add the ohho server from the config snippet.",
      "Cline can now query and control your robots.",
    ],
    docsUrl: "https://docs.cline.bot/mcp",
    free: true,
  },
  {
    id: "windsurf",
    name: "Windsurf",
    tagline: "AI-first IDE by Codeium with MCP support.",
    transport: "streamable-http",
    avatar: "WS",
    accent: "#60A5FA",
    configFile: "~/.codeium/windsurf/mcp_config.json",
    configSnippet: configFor(ENDPOINT),
    steps: [
      "Install Windsurf from codeium.com/windsurf.",
      "Open Settings → MCP Servers.",
      "Add the ohho entry from the snippet.",
      "Windsurf's Cascade agent can now control your robots.",
    ],
    docsUrl: "https://docs.codeium.com/mcp",
    free: true,
  },
  {
    id: "continue",
    name: "Continue",
    tagline: "Open-source AI coding assistant for VS Code & JetBrains.",
    transport: "streamable-http",
    avatar: "CO",
    accent: "#FBBF24",
    configFile: "~/.continue/config.json",
    configSnippet: configFor(ENDPOINT),
    steps: [
      "Install the Continue extension.",
      "Open config.json (Continue icon → Settings).",
      "Add the mcpServers.ohho entry.",
      "Continue can now access your robot fleet.",
    ],
    docsUrl: "https://docs.continue.dev/mcp",
    free: true,
  },
  {
    id: "zed",
    name: "Zed",
    tagline: "Blazing-fast editor with built-in MCP support.",
    transport: "streamable-http",
    avatar: "ZD",
    accent: "#F87171",
    configFile: "~/.config/zed/settings.json",
    configSnippet: configFor(ENDPOINT),
    steps: [
      "Install Zed from zed.dev.",
      "Open settings.json (cmd+,).",
      "Add the mcpServers.ohho entry under 'mcp_servers'.",
      "Zed's Assistant can now query your robots.",
    ],
    docsUrl: "https://zed.dev/docs/ai/mcp",
    free: true,
  },
  {
    id: "roo-code",
    name: "Roo Code",
    tagline: "AI coding extension for VS Code with MCP.",
    transport: "streamable-http",
    avatar: "RC",
    accent: "#A78BFA",
    configFile: "VS Code Settings → Roo Code → MCP",
    configSnippet: configFor(ENDPOINT),
    steps: [
      "Install the Roo Code extension in VS Code.",
      "Open Roo Code settings → MCP Servers.",
      "Add the ohho server from the snippet.",
      "Roo can now control your robots.",
    ],
    free: true,
  },
  {
    id: "codex",
    name: "Codex",
    tagline: "OpenAI's coding agent — connect via HTTP.",
    transport: "cli",
    avatar: "CX",
    accent: "#10B981",
    configFile: "Custom — pass the endpoint as an MCP URL",
    configSnippet: `# Codex MCP connection\n--mcp-url ${ENDPOINT}\n--mcp-header "X-API-Key: YOUR-API-KEY"`,
    steps: [
      "Install Codex from OpenAI.",
      "Pass the MCP endpoint URL and API key as flags.",
      "Codex can now query and control your robots.",
    ],
    free: false,
  },
  {
    id: "hermes",
    name: "Hermes",
    tagline: "AI agent framework — connect via standard HTTP.",
    transport: "cli",
    avatar: "HM",
    accent: "#00D4FF",
    configFile: "Custom — configure MCP endpoint in Hermes settings",
    configSnippet: `# Hermes MCP configuration\nendpoint: ${ENDPOINT}\nheaders:\n  X-API-Key: YOUR-API-KEY`,
    steps: [
      "Open your Hermes agent configuration.",
      "Set the MCP endpoint to the OhhO URL.",
      "Add the X-API-Key header.",
      "Hermes can now access all OhhO tools.",
    ],
    free: true,
  },
  {
    id: "antigravity",
    name: "Antigravity",
    tagline: "Google's AI agent — connect via MCP over HTTP.",
    transport: "cli",
    avatar: "AG",
    accent: "#4285F4",
    configFile: "Custom — configure MCP endpoint in agent settings",
    configSnippet: `# Antigravity MCP config\nmcp_endpoint: ${ENDPOINT}\nmcp_headers:\n  X-API-Key: YOUR-API-KEY`,
    steps: [
      "Open Antigravity's agent configuration.",
      "Set the MCP endpoint URL.",
      "Add the X-API-Key header.",
      "Antigravity can now control your robot fleet.",
    ],
    free: true,
  },
  {
    id: "generic",
    name: "Any MCP Client",
    tagline: "Any tool that speaks MCP — curl, custom scripts, etc.",
    transport: "streamable-http",
    avatar: "{}",
    accent: "rgba(255,255,255,0.4)",
    configFile: "Any MCP-compatible client",
    configSnippet: `# JSON-RPC 2.0 over HTTP\ncurl -X POST ${ENDPOINT} \\\n  -H "Content-Type: application/json" \\\n  -H "X-API-Key: YOUR-API-KEY" \\\n  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'`,
    steps: [
      "Any client that speaks JSON-RPC 2.0 can connect.",
      "POST to the endpoint with X-API-Key header.",
      'Call methods: initialize, tools/list, tools/call, resources/list, resources/read.',
      "See MCP_SETUP.md for the full protocol reference.",
    ],
    free: true,
  },
];

export function getAgent(id: string): AgentConfig | undefined {
  return AGENTS.find((a) => a.id === id);
}

export const ENDPOINT_URL = ENDPOINT;
