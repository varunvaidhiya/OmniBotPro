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

using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction.OVR.Editor.QuickActions;
using UnityEditor;

namespace Oculus.Interaction.OVR.Editor
{
    [InitializeOnLoad]
    internal static class ISDKBlocksRules
    {
        // Constructor is still needed to have InitializeOnLoad work
        static ISDKBlocksRules() { }

        static OVRConfigurationTask _duplicateHandVisuals;

        public static OVRConfigurationTask DuplicateHandVisuals
        {
            get
            {
                _duplicateHandVisuals ??= OVRProjectSetup.RegisterTask(
                    level: OVRProjectSetup.TaskLevel.Recommended,
                    group: OVRProjectSetup.TaskGroup.Compatibility,
                    // Should rule be visible
                    conditionalValidity: _ => !SceneIsCompliant(),
                    // Has rule been satisfied
                    isDone: _ => SceneIsCompliant(),
                    message: "Conflicting Hand Visuals are present in the scene",
                    fix: _ =>
                    {
                        OVRCameraRig cameraRig = OVRComprehensiveInteractionRigWizard.FindExistingCameraRig();
                        if (cameraRig == null)
                            return;

                        OVRHand[] cameraRigHands = cameraRig.trackingSpace.GetComponentsInChildren<OVRHand>();
                        foreach (OVRHand hand in cameraRigHands)
                        {
                            OVRComprehensiveInteractionRigWizard.DisableDuplicateVisuals(hand);
                        }
                    },
                    fixMessage: "Disable Hand Visuals in OVR Camera Rig"
                );

                return _duplicateHandVisuals;
            }
        }

        private static bool SceneIsCompliant()
        {
            // OVRInteraction* exists, and has hands
            OVRCameraRigRef interactionRig = OVRComprehensiveInteractionRigWizard.FindExistingInteractionRig();
            if (interactionRig == null)
                return true;

            if (interactionRig.GetComponentsInChildren<Hand>().Length == 0)
                return true;

            // A Camera Rig exists
            OVRCameraRig cameraRig = OVRComprehensiveInteractionRigWizard.FindExistingCameraRig();
            if (cameraRig == null)
                return true;

            // The Camera Rig has hands and enabled hand visuals
            OVRHand[] cameraRigHands = cameraRig.trackingSpace.GetComponentsInChildren<OVRHand>();
            if (cameraRigHands.Length == 0)
                return true;

            // Camera Rig has hands, but visuals already disabled
            foreach (OVRHand hand in cameraRigHands)
            {
                if (HandVisualsEnabled(hand))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HandVisualsEnabled(OVRHand hand)
        {
            if (hand.TryGetComponent<OVRSkeletonRenderer>(out var skeletonRenderer))
            {
                if (skeletonRenderer.enabled)
                    return true;
            }

            if (hand.TryGetComponent<OVRMesh>(out var mesh))
            {
                if (mesh.enabled)
                    return true;
            }

            if (hand.TryGetComponent<OVRMeshRenderer>(out var meshRenderer))
            {
                if (meshRenderer.enabled)
                    return true;
            }

            if (hand.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
            {
                if (skinnedMeshRenderer.enabled)
                    return true;
            }

            return false;
        }
    }

}
