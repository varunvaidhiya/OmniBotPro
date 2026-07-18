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

#if USING_XR_SDK_OPENXR

using System;
using static OVRPlugin;

namespace Meta.XR
{
    partial class MetaXRFeature
    {
        internal unsafe OVRPlugin.Result ovrp_GetSystemDisplayFrequency2(out float systemDisplayFrequency)
        {
            systemDisplayFrequency = 0.0f;

            if (Session == 0)
                return OVRPlugin.Result.Failure_NotInitialized;

            if (!_displayRefreshRateEnabled)
                return OVRPlugin.Result.Failure_Unsupported;

            if (Command.xrGetDisplayRefreshRateFB == null)
            {
                LogError("xrGetDisplayRefreshRateFB command was not loaded.");
                return OVRPlugin.Result.Failure_Unsupported;
            }

            var result = Command.xrGetDisplayRefreshRateFB(Session, out systemDisplayFrequency);
            return result.ToOVRPluginType();
        }

        internal unsafe OVRPlugin.Result ovrp_GetSystemDisplayAvailableFrequencies(
            IntPtr systemDisplayAvailableFrequencies,
            ref int numFrequencies)
        {
            if (Session == 0)
                return OVRPlugin.Result.Failure_NotInitialized;

            if (!_displayRefreshRateEnabled)
                return OVRPlugin.Result.Failure_Unsupported;

            if (Command.xrEnumerateDisplayRefreshRatesFB == null)
            {
                LogError("xrEnumerateDisplayRefreshRatesFB command was not loaded.");
                return OVRPlugin.Result.Failure_Unsupported;
            }

            uint inputArraySize = systemDisplayAvailableFrequencies == IntPtr.Zero ? 0 : (uint)numFrequencies;
            uint outputArraySize = 0;
            var result = Command.xrEnumerateDisplayRefreshRatesFB(
                Session,
                inputArraySize,
                out outputArraySize,
                (float*)systemDisplayAvailableFrequencies);

            numFrequencies = (int)outputArraySize;
            return result.ToOVRPluginType();
        }

        internal unsafe OVRPlugin.Result ovrp_SetSystemDisplayFrequency(float requestedFrequency)
        {
            if (Session == 0)
                return OVRPlugin.Result.Failure_NotInitialized;

            if (!_displayRefreshRateEnabled)
                return OVRPlugin.Result.Failure_Unsupported;

            if (Command.xrRequestDisplayRefreshRateFB == null)
            {
                LogError("xrRequestDisplayRefreshRateFB command was not loaded.");
                return OVRPlugin.Result.Failure_Unsupported;
            }

            var result = Command.xrRequestDisplayRefreshRateFB(Session, requestedFrequency);
            return result.ToOVRPluginType();
        }
    }
}
#endif
