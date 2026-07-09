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

namespace Meta.XR.AI.MCPBridge.Telemetry
{
    internal static class McpBridgeTelemetryConstants
    {
        internal static class FalcoEventName
        {
            public const string ServerStarted = "mcp_bridge_server_started";
            public const string ServerStopped = "mcp_bridge_server_stopped";
            public const string RequestReceived = "mcp_bridge_request_received";
            public const string RequestCompleted = "mcp_bridge_request_completed";
            public const string ToolInvoked = "mcp_bridge_tool_invoked";
            public const string ToolCompleted = "mcp_bridge_tool_completed";
            public const string ProviderConnected = "mcp_bridge_provider_connected";
            public const string ProviderDisconnected = "mcp_bridge_provider_disconnected";
            public const string Error = "mcp_bridge_error";
        }

        internal static class AnnotationType
        {
            public const string Port = "port";
            public const string AutoStart = "auto_start";
            public const string Duration = "duration";
            public const string CleanShutdown = "clean_shutdown";
            public const string Method = "method";
            public const string Success = "success";
            public const string ToolName = "tool_name";
            public const string MethodName = "method_name";
            public const string ProviderId = "provider_id";
            public const string IsLocal = "is_local";
            public const string ToolCount = "tool_count";
            public const string ResourceCount = "resource_count";
            public const string PromptCount = "prompt_count";
            public const string ErrorType = "error_type";
            public const string ErrorMessage = "error_message";
        }

        internal static class ErrorType
        {
            public const string Authentication = "authentication";
            public const string Serialization = "serialization";
            public const string Execution = "execution";
            public const string Timeout = "timeout";
            public const string NoProvider = "no_provider";
            public const string Unknown = "unknown";
        }
    }
}
