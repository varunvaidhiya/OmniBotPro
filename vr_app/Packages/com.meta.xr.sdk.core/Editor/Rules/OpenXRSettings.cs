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
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Meta.XR.Editor.Utils;
using UnityEditor.XR.OpenXR.Features;
using UnityEditor.Build;

#if USING_XR_SDK_OCULUS
using Unity.XR.Oculus;
#endif

namespace Meta.XR.Editor.Rules
{
    [InitializeOnLoad]
    internal static class OpenXRSettings
    {
        private static BuildTargetGroup[] BuildTargetsToCheck =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.Standalone
        };

        static OpenXRSettings()
        {
#if USING_XR_MANAGEMENT
            // [Required] OpenXR Loader
            OVRProjectSetup.AddTask(
                conditionalValidity: _ =>
                    PackageList.IsPackageInstalled(OVRProjectSetupXRTasks.XRPluginManagementPackageName),
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Packages,
                isDone: OVRProjectSetupXRTasks.IsActiveLoader<OpenXRLoader>,
                message: "OpenXR must be added to the XR Plugin active loaders",
                fix: buildTargetGroup =>
                {
#if USING_XR_SDK_OCULUS
                    OVRProjectSetupXRTasks.RemoveLoader<OculusLoader>(buildTargetGroup);
#endif
                    OVRProjectSetupXRTasks.AddLoader<OpenXRLoader>(buildTargetGroup);
                },
                fixMessage: "Add OpenXR to the XR Plugin active loaders"
            );
#endif

            // [Recommended] Enable Subsampled Layout
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                platform: BuildTargetGroup.Android,
                group: OVRProjectSetup.TaskGroup.Rendering,
                conditionalValidity: buildTargetGroup => GetSettings(buildTargetGroup) != null
                                                         && OVRProjectSetupRenderingTasks.GetGraphicsAPIs(buildTargetGroup).Any(item => item == GraphicsDeviceType.Vulkan),
                isDone: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);
                    var ext = settings.GetFeature<Meta.XR.MetaXRSubsampledLayout>();
                    return !ext || ext.enabled;
                },
                message: "Subsampled Layout should be enabled to improve GPU performance when foveation is enabled.",
                fix: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);
                    var ext = settings.GetFeature<Meta.XR.MetaXRSubsampledLayout>();
                    if (ext)
                        ext.enabled = true;
                    FeatureHelpers.RefreshFeatures(buildTargetGroup);
                },
                fixMessage: "OpenXRSettings.Instance.GetFeature<MetaXRSubsampledLayout>.enabled = true"
            );

            // [Recommended] Enable Optimize Buffer Discards
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                platform: BuildTargetGroup.Android,
                group: OVRProjectSetup.TaskGroup.Rendering,
                conditionalValidity: buildTargetGroup => GetSettings(buildTargetGroup) != null
                                                         && OVRProjectSetupRenderingTasks.GetGraphicsAPIs(buildTargetGroup).Any(item => item == GraphicsDeviceType.Vulkan),
                isDone: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);
                    var ext = settings.GetFeature<MetaQuestFeature>();
                    if (ext == null)
                        return true;
                    // Use reflection to access internal property
                    var property = ext.GetType().GetProperty("optimizeBufferDiscards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (property == null)
                        return true;

                    return (bool)property.GetValue(ext);
                },
                message: "Optimize Buffer Discards should be enabled on Vulkan to allow MSAA textures to be memoryless.",
                fix: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);
                    var ext = settings.GetFeature<MetaQuestFeature>();
                    if (ext != null)
                    {
                        var property = ext.GetType().GetProperty("optimizeBufferDiscards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (property != null)
                        {
                            property.SetValue(ext, true);
                        }
                        EditorUtility.SetDirty(ext);
                        AssetDatabase.SaveAssets();
                    }
                },
                fixMessage: "MetaQuestFeature.optimizeBufferDiscards = true"
            );

            // [Required] Include Oculus Touch Interaction Profile for full OVRInput support
            OVRProjectSetup.AddTask(
                conditionalValidity: buildTargetGroup => GetSettings(buildTargetGroup) != null &&
                    PackageList.IsPackageInstalled(OVRProjectSetupXRTasks.UnityXRPackage),
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Packages,
                isDone: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);

                    bool touchFeatureEnabled = false;
                    foreach(var feature in settings.GetFeatures<OpenXRInteractionFeature>())
                    {
                        if (feature.enabled)
                        {
                            if (feature is OculusTouchControllerProfile)
                            {
                                touchFeatureEnabled = true;
                            }
                        }
                    }
                    return touchFeatureEnabled;
                },
                message: "When using OpenXR Plugin, at least the Oculus Touch Interaction Profile should be included for full OVRInput support.",
                fix: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);

                    var touchFeature = settings.GetFeature<OculusTouchControllerProfile>();
                    if (touchFeature == null)
                    {
                        throw new InvalidOperationException("Could not find Oculus Touch Interaction Profile in OpenXR settings");
                    }
                    touchFeature.enabled = true;
                    FeatureHelpers.RefreshFeatures(buildTargetGroup);
                },
                fixMessage: "Add Oculus Touch Controller Interaction Profile"
            );

#if UNITY_OPENXR_PLUGIN_1_15_0_OR_NEWER && UNITY_6000_1_OR_NEWER
            OVRProjectSetup.AddTask(
                conditionalValidity: buildTargetGroup => GetSettings(buildTargetGroup) != null &&
                    PackageList.IsPackageInstalled(OVRProjectSetupXRTasks.XRPluginManagementPackageName),
                platform: BuildTargetGroup.Android,
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Packages,
                isDone: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);
                    var metaSpaceWarpFeature = settings.GetFeature<MetaXRSpaceWarp>();

                    if (metaSpaceWarpFeature == null)
                        throw new InvalidOperationException("Could not find the Meta XR Space Warp feature");

                    return !metaSpaceWarpFeature.enabled;
                },
                message: "It's recommended to use Application SpaceWarp feature over Meta XR Space Warp feature to avoid conflicts.",
                fix: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);

                    var metaSpaceWarpFeature = settings.GetFeature<MetaXRSpaceWarp>();
                    var spaceWarpFeature = settings.GetFeature<SpaceWarpFeature>();

                    if (metaSpaceWarpFeature == null)
                        throw new InvalidOperationException("Could not find the Meta XR Space Warp feature");
                    if (spaceWarpFeature == null)
                        throw new InvalidOperationException("Could not find the Application Space Warp feature");

                    metaSpaceWarpFeature.enabled = false;
                    spaceWarpFeature.enabled = true;

                    FeatureHelpers.RefreshFeatures(buildTargetGroup);
                },
                fixMessage: "Enable Application SpaceWarp and disable Meta XR Space Warp"
            );
#endif

#if UNITY_OPENXR_PLUGIN_1_17_0_OR_NEWER
            // [Recommended] Enable Oculus Touch Controller Proximity if the Oculus Touch Controller Interaction Profile is enabled
            OVRProjectSetup.AddTask(
                conditionalValidity: buildTargetGroup => GetSettings(buildTargetGroup) != null &&
                    PackageList.IsPackageInstalled(OVRProjectSetupXRTasks.XRPluginManagementPackageName),
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                isDone: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);

                    var touchFeatureEnabled = settings.GetFeatures<OpenXRInteractionFeature>()
                        .Any(f => f is OculusTouchControllerProfile && f.enabled);
                    var proximityFeatureEnabled = settings.GetFeatures<OpenXRInteractionFeature>()
                        .Any(f => f is OculusTouchControllerProximityProfile && f.enabled);
                    return touchFeatureEnabled == proximityFeatureEnabled;
                },
                message: "Recommended to enable Oculus Touch Controller Proximity Interaction when using the Oculus Touch Controller Interaction Profile and vice versa.",
                fix: buildTargetGroup =>
                {
                    var settings = GetSettings(buildTargetGroup);

                    var proximityFeature = settings.GetFeature<OculusTouchControllerProximityProfile>();
                    if (proximityFeature == null)
                    {
                        throw new InvalidOperationException("Could not find Oculus Touch Controller Proximity Profile in OpenXR settings");
                    }
                    proximityFeature.enabled = true;

                    var touchFeature = settings.GetFeature<OculusTouchControllerProfile>();
                    if (touchFeature == null)
                    {
                        throw new InvalidOperationException("Could not find Oculus Touch Interaction Profile in OpenXR settings");
                    }
                    touchFeature.enabled = true;
                    FeatureHelpers.RefreshFeatures(buildTargetGroup);
                },
                fixMessage: "Enable Oculus Touch Controller Proximity Profile"
            );
#endif
        }

        private static UnityEngine.XR.OpenXR.OpenXRSettings GetSettings(BuildTargetGroup buildTargetGroup)
            => UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);

        private static TFeature GetFeature<TFeature>(BuildTargetGroup buildTargetGroup) where TFeature : OpenXRFeature
        {
            var settings = GetSettings(buildTargetGroup);
            return !settings ? null : settings.GetFeature<TFeature>();
        }

        private static bool TryGetFeature<TFeature>(BuildTargetGroup buildTargetGroup, out TFeature feature)
            where TFeature : OpenXRFeature
            => feature = GetFeature<TFeature>(buildTargetGroup);
    }
}

#endif
