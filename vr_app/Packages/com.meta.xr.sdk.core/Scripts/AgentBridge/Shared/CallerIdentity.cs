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

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Represents the identity of a caller for telemetry tracking.
    /// Acts as a bridge between Editor-only IIdentified and cross-platform API.
    /// </summary>
    public class CallerIdentity
    {
        /// <summary>
        /// Unique identifier for the caller (e.g., tool name, window name)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Create a caller identity with the specified ID
        /// </summary>
        public CallerIdentity(string id)
        {
            Id = id ?? string.Empty;
        }
    }
}
