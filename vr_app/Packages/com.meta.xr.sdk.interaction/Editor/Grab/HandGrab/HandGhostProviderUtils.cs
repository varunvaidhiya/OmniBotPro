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

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.HandGrab.Visuals;

namespace Oculus.Interaction.HandGrab.Editor
{
    public static class HandGhostProviderUtils
    {
#if ISDK_OPENXR_HAND
        private readonly static string PrefKey = "HandGhostProviderOpenXR";
        private readonly static string DefaultGhostProviderPath = "Packages/com.meta.xr.sdk.interaction/Runtime/Prefabs/HandGrab/OpenXRGhostProvider.asset";
#else
        private readonly static string PrefKey = "HandGhostProvider";
        private readonly static string DefaultGhostProviderPath = "Packages/com.meta.xr.sdk.interaction/Runtime/Prefabs/HandGrab/GhostProvider.asset";
#endif

        public static bool TryGetDefaultProvider(out HandGhostProvider provider)
        {
            var providerPath = EditorPrefs.GetString(PrefKey, DefaultGhostProviderPath);
            provider = AssetDatabase.LoadAssetAtPath<HandGhostProvider>(providerPath);
            if (provider == null)
            {
                provider = AssetDatabase.LoadAssetAtPath<HandGhostProvider>(DefaultGhostProviderPath);
            }
            return provider != null;
        }

        public static void SetLastDefaultProvider(HandGhostProvider provider)
        {
            EditorPrefs.SetString(PrefKey, AssetDatabase.GetAssetPath(provider));
        }
    }
}
