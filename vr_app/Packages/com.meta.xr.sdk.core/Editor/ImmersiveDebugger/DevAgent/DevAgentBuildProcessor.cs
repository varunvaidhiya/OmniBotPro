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

using Meta.XR.AI.AgentBridge;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent.Editor
{
    /// <summary>
    /// Injects the editor machine's current local network IP and access token into
    /// the DevAgent RuntimeSettings asset at build time, so Quest builds automatically
    /// connect back to the correct editor instance without manual configuration.
    /// </summary>
    internal class DevAgentBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 1; // Run after RuntimeSettingsBuildProcessor (order 0)

        private static string _previousServerAddress;
        private static string _previousAccessToken;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = RuntimeSettings.Instance;
            if (settings == null)
                return;

            // Save current values to restore after build
            _previousServerAddress = settings.ServerAddress;
            _previousAccessToken = settings.AccessToken;

            // Inject the editor machine's local network IP so remote devices (Quest)
            // can connect over the local network instead of trying localhost.
            var localIp = NetworkUtilities.GetLocalNetworkAddress();
            settings.ServerAddress = localIp;

            // Inject the server's access token so the build can authenticate
            var serverToken = RemoteAgentSettings.EnsureToken();
            settings.SetAccessToken(serverToken);

            EditorUtility.SetDirty(settings);

            Debug.Log($"[DevAgent] Build: injected server address {localIp} and access token into RuntimeSettings");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var settings = RuntimeSettings.Instance;
            if (settings == null)
                return;

            // Restore the pre-build values so the editor asset isn't permanently changed
            settings.ServerAddress = _previousServerAddress;
            settings.SetAccessToken(_previousAccessToken);
            EditorUtility.SetDirty(settings);
        }
    }
}
