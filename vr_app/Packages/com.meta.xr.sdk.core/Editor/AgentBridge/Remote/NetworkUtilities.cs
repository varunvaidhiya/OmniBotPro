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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Utilities for detecting local network addresses.
    /// Used to auto-populate the server address for remote clients (e.g., Quest headsets)
    /// that need to connect to this editor machine over the local network.
    /// </summary>
    internal static class NetworkUtilities
    {
        /// <summary>
        /// Returns the local network IP address of this machine (e.g., 192.168.x.x).
        /// Prefers IPv4 addresses from active, non-loopback network interfaces.
        /// Returns "127.0.0.1" if no suitable network address is found.
        /// </summary>
        internal static string GetLocalNetworkAddress()
        {
            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var properties = networkInterface.GetIPProperties();

                    // Skip interfaces without a default gateway (not connected to a network)
                    if (properties.GatewayAddresses.Count == 0)
                        continue;

                    foreach (var unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork
                            && !IPAddress.IsLoopback(unicast.Address))
                        {
                            return unicast.Address.ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to fallback
            }

            return "127.0.0.1";
        }
    }
}
