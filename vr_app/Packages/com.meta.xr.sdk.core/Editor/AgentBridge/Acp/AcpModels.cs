/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meta.XR.AI.AgentBridge.Acp
{
    // -----------------------------------------------------------------------
    //  JSON-RPC 2.0 envelopes
    // -----------------------------------------------------------------------

    internal class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")] public string Jsonrpc = "2.0";
        [JsonProperty("id")] public int Id;
        [JsonProperty("method")] public string Method = "";
        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public object? Params;
    }

    internal class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")] public string Jsonrpc = "2.0";
        [JsonProperty("id")] public int? Id;
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public JToken? Result;
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcError? Error;
    }

    internal class JsonRpcNotification
    {
        [JsonProperty("jsonrpc")] public string Jsonrpc = "2.0";
        [JsonProperty("method")] public string Method = "";
        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public JToken? Params;
    }

    internal class JsonRpcError
    {
        [JsonProperty("code")] public int Code;
        [JsonProperty("message")] public string Message = "";
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public JToken? Data;
    }

    // -----------------------------------------------------------------------
    //  Client -> Agent requests
    // -----------------------------------------------------------------------

    internal class InitializeParams
    {
        [JsonProperty("protocolVersion")] public int ProtocolVersion = 1;
        [JsonProperty("clientCapabilities", NullValueHandling = NullValueHandling.Ignore)]
        public ClientCapabilities? ClientCapabilities;
        [JsonProperty("clientInfo", NullValueHandling = NullValueHandling.Ignore)]
        public ClientInfo? ClientInfo;
    }

    internal class ClientCapabilities
    {
        [JsonProperty("streaming", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Streaming;
    }

    internal class ClientInfo
    {
        [JsonProperty("name")] public string Name = "";
        [JsonProperty("version")] public string Version = "";
    }

    internal class InitializeResult
    {
        [JsonProperty("protocolVersion")] public int ProtocolVersion;
        [JsonProperty("agentCapabilities", NullValueHandling = NullValueHandling.Ignore)]
        public AgentCapabilities? AgentCapabilities;
        [JsonProperty("agentInfo", NullValueHandling = NullValueHandling.Ignore)]
        public AgentInfo? AgentInfo;
    }

    internal class AgentCapabilities
    {
        [JsonProperty("streaming", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Streaming;
    }

    internal class AgentInfo
    {
        [JsonProperty("name")] public string Name = "";
        [JsonProperty("version")] public string Version = "";
    }

    internal class NewSessionParams
    {
        [JsonProperty("cwd")] public string Cwd = "";
        [JsonProperty("mcpServers")] public List<object> McpServers = new();
    }

    internal class NewSessionResult
    {
        [JsonProperty("sessionId")] public string SessionId = "";
    }

    internal class PromptParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("prompt")] public List<ContentBlock> Prompt = new();
    }

    internal class PromptResult
    {
        [JsonProperty("stopReason", NullValueHandling = NullValueHandling.Ignore)]
        public string? StopReason;
    }

    internal class CancelNotificationParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
    }

    // -----------------------------------------------------------------------
    //  Content blocks
    // -----------------------------------------------------------------------

    [JsonConverter(typeof(ContentBlockConverter))]
    internal abstract class ContentBlock
    {
        [JsonProperty("type")] public string Type = "";
    }

    internal class TextContentBlock : ContentBlock
    {
        [JsonProperty("text")] public string Text = "";

        public TextContentBlock()
        {
            Type = "text";
        }
    }

    internal class ImageContentBlock : ContentBlock
    {
        [JsonProperty("data")] public string Data = "";
        [JsonProperty("mimeType")] public string MimeType = "";

        public ImageContentBlock()
        {
            Type = "image";
        }
    }

    /// <summary>
    /// JSON converter for ContentBlock polymorphism.
    /// </summary>
    internal class ContentBlockConverter : JsonConverter<ContentBlock>
    {
        public override ContentBlock? ReadJson(JsonReader reader, System.Type objectType,
            ContentBlock? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = jo["type"]?.ToString();
            ContentBlock block = type switch
            {
                "image" => new ImageContentBlock(),
                _ => new TextContentBlock()
            };
            serializer.Populate(jo.CreateReader(), block);
            return block;
        }

        public override void WriteJson(JsonWriter writer, ContentBlock? value, JsonSerializer serializer)
        {
            // Use default serialization but bypass converter to avoid infinite loop
            var jo = new JObject();
            if (value != null)
            {
                jo["type"] = value.Type;
                if (value is TextContentBlock text)
                {
                    jo["text"] = text.Text;
                }
                else if (value is ImageContentBlock img)
                {
                    jo["data"] = img.Data;
                    jo["mimeType"] = img.MimeType;
                }
            }
            jo.WriteTo(writer);
        }
    }

    // -----------------------------------------------------------------------
    //  Agent -> Client requests
    // -----------------------------------------------------------------------

    internal class ReadTextFileParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("path")] public string Path = "";
    }

    internal class ReadTextFileResult
    {
        [JsonProperty("content")] public string Content = "";
    }

    internal class WriteTextFileParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("path")] public string Path = "";
        [JsonProperty("content")] public string Content = "";
    }

    internal class RequestPermissionParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("toolCall", NullValueHandling = NullValueHandling.Ignore)]
        public JToken? ToolCall;
        [JsonProperty("options")] public List<PermissionOption> Options = new();
    }

    internal class PermissionOption
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("label")] public string Label = "";
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description;
    }

    internal class RequestPermissionResult
    {
        [JsonProperty("outcome")] public PermissionOutcome Outcome = new();
    }

    internal class PermissionOutcome
    {
        [JsonProperty("outcome")] public string OutcomeType = "selected";
        [JsonProperty("optionId")] public string OptionId = "";
    }

    internal class CreateTerminalParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("command")] public string Command = "";
        [JsonProperty("args", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Args;
        [JsonProperty("cwd", NullValueHandling = NullValueHandling.Ignore)]
        public string? Cwd;
    }

    internal class CreateTerminalResult
    {
        [JsonProperty("terminalId")] public string TerminalId = "";
    }

    internal class TerminalOutputParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("terminalId")] public string TerminalId = "";
    }

    internal class TerminalOutputResult
    {
        [JsonProperty("output")] public string Output = "";
        [JsonProperty("truncated")] public bool Truncated;
        [JsonProperty("exitStatus", NullValueHandling = NullValueHandling.Ignore)]
        public ExitStatus? ExitStatus;
    }

    internal class ExitStatus
    {
        [JsonProperty("type")] public string Type = "";
        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public int? Code;
    }

    internal class KillTerminalParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("terminalId")] public string TerminalId = "";
    }

    internal class ReleaseTerminalParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("terminalId")] public string TerminalId = "";
    }

    internal class WaitForTerminalExitParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("terminalId")] public string TerminalId = "";
        [JsonProperty("timeoutMs", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeoutMs;
    }

    internal class WaitForTerminalExitResult
    {
        [JsonProperty("exitStatus", NullValueHandling = NullValueHandling.Ignore)]
        public ExitStatus? ExitStatus;
        [JsonProperty("timedOut")] public bool TimedOut;
    }

    // -----------------------------------------------------------------------
    //  Session update notifications (agent -> client)
    // -----------------------------------------------------------------------

    internal class SessionUpdateParams
    {
        [JsonProperty("sessionId")] public string SessionId = "";
        [JsonProperty("update")] public JObject Update = new();

        /// <summary>
        /// Gets the session update type string from the update object.
        /// </summary>
        public string? UpdateType => Update["sessionUpdate"]?.ToString();
    }

    // -----------------------------------------------------------------------
    //  Typed update helpers (parsed from SessionUpdateParams.Update)
    // -----------------------------------------------------------------------

    internal class AgentMessageChunk
    {
        [JsonProperty("content")] public JToken? Content;
    }

    internal class AgentThoughtChunk
    {
        [JsonProperty("content")] public JToken? Content;
    }

    internal class ToolCallStart
    {
        [JsonProperty("toolCallId")] public string ToolCallId = "";
        [JsonProperty("title")] public string Title = "";
        [JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
        public string? Kind;
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string? Status;
    }

    internal class ToolCallProgress
    {
        [JsonProperty("toolCallId")] public string ToolCallId = "";
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string? Status;
        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public JArray? Content;
    }

    internal class ToolCallComplete
    {
        [JsonProperty("toolCallId")] public string ToolCallId = "";
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string? Status;
        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public JArray? Content;
    }

    internal class UsageUpdate
    {
        [JsonProperty("inputTokens", NullValueHandling = NullValueHandling.Ignore)]
        public int? InputTokens;
        [JsonProperty("outputTokens", NullValueHandling = NullValueHandling.Ignore)]
        public int? OutputTokens;
    }

    internal class PlanUpdate
    {
        [JsonProperty("content")] public JToken? Content;
    }
}
