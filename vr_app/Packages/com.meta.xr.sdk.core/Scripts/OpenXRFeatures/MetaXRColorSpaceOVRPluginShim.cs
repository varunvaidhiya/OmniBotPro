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
using UnityEngine;
using Meta.XR;

namespace Meta.XR
{
    public partial class MetaXRFeature
    {
        internal unsafe OVRPlugin.Result ovrp_SetClientColorDesc(OVRPlugin.ColorSpace colorSpace)
        {
            if (Session == 0)
                return OVRPlugin.Result.Failure_NotInitialized;

            if (!_colorSpaceEnabled)
                return OVRPlugin.Result.Failure_Unsupported;

            if (Command.xrSetColorSpaceFB == null)
            {
                LogError("xrSetColorSpaceFB command was not loaded.");
                return OVRPlugin.Result.Failure_Unsupported;
            }

            if (colorSpace < OVRPlugin.ColorSpace.Unknown)
            {
                LogError("Unsupported color space.");
                return OVRPlugin.Result.Failure_InvalidParameter;
            }

            var result = Command.xrSetColorSpaceFB(Session, colorSpace.ToXrColorSpaceFB())
                .OrLogFormat(LogPrefix + nameof(Command.xrSetColorSpaceFB));
            return result.ToOVRPluginType();
        }

        internal OVRPlugin.Result ovrp_GetHmdColorDesc(ref OVRPlugin.ColorSpace colorSpace)
        {
            if (!_colorSpaceEnabled)
                return OVRPlugin.Result.Failure_Unsupported;

            colorSpace = _nativeColorSpace.ToOVRPluginColorSpace();
            return OVRPlugin.Result.Success;
        }
    }

    public static partial class Extensions
    {
        public static XrColorSpaceFB ToXrColorSpaceFB(this OVRPlugin.ColorSpace colorSpace) => colorSpace switch
        {
            OVRPlugin.ColorSpace.Unmanaged => XrColorSpaceFB.Unmanaged,
            OVRPlugin.ColorSpace.Rec_2020 => XrColorSpaceFB.Rec2020,
            OVRPlugin.ColorSpace.Rec_709 => XrColorSpaceFB.Rec709,
            OVRPlugin.ColorSpace.Rift_CV1 => XrColorSpaceFB.RiftCv1,
            OVRPlugin.ColorSpace.Rift_S => XrColorSpaceFB.RiftS,
            OVRPlugin.ColorSpace.Quest => XrColorSpaceFB.Quest,
            OVRPlugin.ColorSpace.P3 => XrColorSpaceFB.P3,
            OVRPlugin.ColorSpace.Adobe_RGB => XrColorSpaceFB.AdobeRgb,
            _ => XrColorSpaceFB.P3, // Unknown and any other values default to P3
        };

        public static OVRPlugin.ColorSpace ToOVRPluginColorSpace(this XrColorSpaceFB colorSpace) => colorSpace switch
        {
            XrColorSpaceFB.Unmanaged => OVRPlugin.ColorSpace.Unmanaged,
            XrColorSpaceFB.Rec2020 => OVRPlugin.ColorSpace.Rec_2020,
            XrColorSpaceFB.Rec709 => OVRPlugin.ColorSpace.Rec_709,
            XrColorSpaceFB.RiftCv1 => OVRPlugin.ColorSpace.Rift_CV1,
            XrColorSpaceFB.RiftS => OVRPlugin.ColorSpace.Rift_S,
            XrColorSpaceFB.Quest => OVRPlugin.ColorSpace.Quest,
            XrColorSpaceFB.P3 => OVRPlugin.ColorSpace.P3,
            XrColorSpaceFB.AdobeRgb => OVRPlugin.ColorSpace.Adobe_RGB,
            _ => OVRPlugin.ColorSpace.Unknown,
        };
    }
}
#endif
