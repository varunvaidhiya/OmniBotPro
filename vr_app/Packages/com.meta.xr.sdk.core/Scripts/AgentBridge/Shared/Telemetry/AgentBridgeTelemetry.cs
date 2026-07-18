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

using System;
using Meta.XR.Telemetry;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge.Telemetry
{
    internal static class AgentBridgeTelemetry
    {
        /// <summary>
        /// Extension method to add standard AgentBridge context to events
        /// </summary>
        public static UnifiedEventData AddAgentBridgeContext(
            this UnifiedEventData eventData)
        {
            eventData.productType = TelemetryProductType.Editor;
            return eventData;
        }

        /// <summary>
        /// Send a telemetry event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="configureEvent">Optional callback to configure event metadata</param>
        /// <param name="isEssential">Whether this is an essential event (always sent)</param>
        public static void SendEvent(string eventName,
            Action<UnifiedEventData> configureEvent = null,
            bool isEssential = false)
        {
            try
            {
                var guid = Guid.NewGuid().ToString();

                var unifiedEvent = new UnifiedEventData(eventName)
                {
                    isEssential = isEssential
                };
                unifiedEvent.AddAgentBridgeContext();
                configureEvent?.Invoke(unifiedEvent);
                unifiedEvent.Send();
            }
            catch
            {
                // Silently fail - telemetry errors should never surface to users
            }
        }
    }
}
