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

using Meta.XR.BuildingBlocks;
using System;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.BuildingBlocks.Editor;
using Oculus.Interaction.OVR.Editor;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor.BuildingBlocks
{
    internal class OVRSyntheticHandsBlockData : BlockData
    {
        internal override IEnumerable<OVRConfigurationTask> GetAssociatedRules(BuildingBlock block)
            => new[] { ISDKBlocksRules.DuplicateHandVisuals };

        public GameObject _leftHand;
        public GameObject _rightHand;

        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
            var syntheticHands = new List<GameObject>();
            foreach (var hand in BlocksUtils.GetHands())
            {
                var syntheticHand = InstantiateHand(hand);
                syntheticHands.Add(syntheticHand);
            }

            DisableUOIAssetsHandVisual();

            return syntheticHands;
        }

        private GameObject InstantiateHand(Hand hand)
        {
            var handedness = hand.Handedness;

            if (TryGetPreexistingNonBlock(handedness, out var syntheticHand)) return syntheticHand;

            var handTrackingBlock = Meta.XR.BuildingBlocks.Editor.Utils.GetBlock(BlockDataIds.InteractionHandTracking);
            if (handTrackingBlock == null)
            {
                throw new InvalidOperationException(
                    $"Cannot install block '{this.name}' : Cannot find block with type {handTrackingBlock.name} in the scene.");
            }

            var prefab = handedness == Handedness.Left ? _leftHand : _rightHand;
            syntheticHand = Instantiate(prefab, handTrackingBlock.transform, false);
            Undo.RegisterCreatedObjectUndo(syntheticHand, "Create Synthetic Hand");
            syntheticHand.GetComponent<SyntheticHand>().InjectModifyDataFromSource(hand);
            syntheticHand.SetActive(true);
            syntheticHand.name = $"[BuildingBlock] Synthetic {handedness} Hand";
            BlocksUtils.UpdateForAutoWiring(syntheticHand);
            Undo.RegisterFullObjectHierarchyUndo(syntheticHand, "Auto-Wiring Synthetic Hand");
            return syntheticHand;
        }

        private void DisableUOIAssetsHandVisual()
        {
            var handsBlocks =
                Meta.XR.BuildingBlocks.Editor.Utils.GetBlocks(Meta.XR.BuildingBlocks.Editor.BlockDataIds.HandTracking);
            foreach (var hand in handsBlocks)
            {
                var skeletonRenderer = hand.GetComponent<OVRSkeletonRenderer>();
                var meshRenderer = hand.GetComponent<OVRMeshRenderer>();
                var skinnedMeshRenderer = hand.GetComponent<SkinnedMeshRenderer>();
                if (skeletonRenderer && skeletonRenderer.enabled) skeletonRenderer.enabled = false;
                if (meshRenderer && meshRenderer.enabled) meshRenderer.enabled = false;
                if (skinnedMeshRenderer && skinnedMeshRenderer.enabled) skinnedMeshRenderer.enabled = false;
                Undo.RegisterCompleteObjectUndo(hand, "Disable Hand Visual");
            }
        }

        private bool TryGetPreexistingNonBlock(Handedness handedness, out GameObject syntheticHandObject)
        {
            syntheticHandObject = null;

            var handTrackingBlock = Meta.XR.BuildingBlocks.Editor.Utils.GetBlock(BlockDataIds.InteractionHandTracking);
            if (handTrackingBlock == null) return false;

            syntheticHandObject = handTrackingBlock.transform.GetComponentsInChildren<SyntheticHand>()
                .FirstOrDefault(syntheticHand => HasCorrectHandedness(syntheticHand, handedness)
                                                 && HasVisuals(syntheticHand)
                                                 && IsNotInsideInteractor(syntheticHand))?.gameObject;

            return syntheticHandObject != null;
        }

        private bool HasCorrectHandedness(SyntheticHand syntheticHand, Handedness handedness)
            => syntheticHand.Handedness == handedness;

        private bool HasVisuals(SyntheticHand syntheticHand)
            => syntheticHand.GetComponentInChildren<HandVisual>() != null;

        private bool IsNotInsideInteractor(SyntheticHand syntheticHand)
            => syntheticHand.GetComponentInParent<IInteractor>() == null;

        internal override IReadOnlyCollection<InstallationStepInfo> InstallationSteps
        {
            get
            {
                var handTrackingBlockData = Meta.XR.BuildingBlocks.Editor.Utils.GetBlockData(Meta.XR.BuildingBlocks.Editor.BlockDataIds.HandTracking);
                var interactionHandTrackingBlockData = Meta.XR.BuildingBlocks.Editor.Utils.GetBlockData(BlockDataIds.InteractionHandTracking);
                return new List<InstallationStepInfo>
                {
                    new(interactionHandTrackingBlockData, "Search for an instance of {0}."),
                    new(_leftHand, "Instantiates {0} prefab as a child of the instance found, and auto-wires it."),
                    new(null, $"Renames the instantiated prefab to <b>{Meta.XR.BuildingBlocks.Editor.Utils.BlockPublicTag} Synthetic Left Hand</b>."),
                    new(_rightHand, "Instantiates {0} prefab as a child of the instance found, and auto-wires it."),
                    new(null, $"Renames the instantiated prefab to <b>{Meta.XR.BuildingBlocks.Editor.Utils.BlockPublicTag} Synthetic Right Hand</b>."),
                    new(handTrackingBlockData, "Disable hand visuals from {0}")
                };
            }
        }
    }
}
