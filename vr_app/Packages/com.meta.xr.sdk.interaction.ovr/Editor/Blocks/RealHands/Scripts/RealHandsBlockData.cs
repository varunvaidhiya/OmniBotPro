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

using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    public class RealHandsBlockData : BlockData
    {
        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
#pragma warning disable CS0618 // Flexible layering is being deprecated
            if (!OVRPassthroughHelper.IsAnyPassthroughLayerUnderlay())
            {
                var pt = new GameObject("OVRPassthroughLayer").AddComponent<OVRPassthroughLayer>();

                pt.overlayType = OVROverlay.OverlayType.Underlay;
                Undo.RegisterCreatedObjectUndo(pt.gameObject, "Instantiate PT layer.");
            }
#pragma warning restore CS0618
            var handMeshRenderers = new List<SkinnedMeshRenderer>();
            Utils.GetBlocksWithType<SyntheticHand>().ForEach(block =>
                handMeshRenderers.AddRange(block.GetComponentsInChildren<SkinnedMeshRenderer>(true)));

            foreach (var renderer in handMeshRenderers)
            {
                Undo.RegisterFullObjectHierarchyUndo(renderer.gameObject, "Process real hand.");
                renderer.sharedMaterial = Prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            }

            var blockPrefab = base.InstallRoutine(selectedGameObject).First();
            if (!Utils.FindComponentInScene<EnableUnpremultipliedAlpha>())
            {
                blockPrefab.AddComponent<EnableUnpremultipliedAlpha>();
            }

            return new List<GameObject> { blockPrefab };
        }
    }
}
