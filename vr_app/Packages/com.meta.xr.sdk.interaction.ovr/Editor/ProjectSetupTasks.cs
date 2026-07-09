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
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Oculus.Interaction.OVR.Editor
{
    [InitializeOnLoad]
    internal static class ProjectSetupTasks
    {
        static ProjectSetupTasks()
        {
            CheckHandSkeletonVersion();
            CheckHandTrackingSupportMode();
        }

        private static void CheckHandSkeletonVersion()
        {

#if ISDK_OPENXR_HAND
            const OVRHandSkeletonVersion handSkeletonVersion = OVRHandSkeletonVersion.OpenXR;
#else
            const OVRHandSkeletonVersion handSkeletonVersion = OVRHandSkeletonVersion.OVR;
#endif
            const OVRProjectSetup.TaskGroup Group = OVRProjectSetup.TaskGroup.Compatibility;

            string message = $"When using Interaction SDK with the {handSkeletonVersion} hand skeleton, " +
                $"the Hand Skeleton Version in OVRManager must be set to {handSkeletonVersion}";

            OVRProjectSetup.AddTask(
                    level: OVRProjectSetup.TaskLevel.Required,
                    group: Group,
                    isDone: buildTargetGroup => OVRRuntimeSettings.Instance.HandSkeletonVersion == handSkeletonVersion,
                    message: message,
                    fix: buildTargetGroup =>
                    {
                        OVRRuntimeSettings.Instance.HandSkeletonVersion = handSkeletonVersion;
                        OVRRuntimeSettings.CommitRuntimeSettings(OVRRuntimeSettings.Instance);
                    },
                    fixMessage: $"Set the Hand Skeleton to the required version."
                );
        }

        private static void CheckHandTrackingSupportMode()
        {
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                isDone: _ =>
                {
                    OVRManager ovrManager = OVRProjectSetupUtils.FindComponentInScene<OVRManager>();
                    if (ovrManager == null)
                    {
                        return true;
                    }

                    var projectConfig = OVRProjectConfig.CachedProjectConfig;
                    return projectConfig.handTrackingSupport != OVRProjectConfig.HandTrackingSupport.ControllersOnly;

                },
                message: "Hand tracking support is set to \"Controllers Only\", hand tracking will not work in this mode.",
                fix: _ =>
                {
                    var projectConfig = OVRProjectConfig.CachedProjectConfig;
                    projectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
                    OVRProjectConfig.CommitProjectConfig(projectConfig);
                },
                fixMessage: "Set hand tracking support mode to \"Controllers And Hands\" in the OVR Manager."
            );
        }
    }
}
