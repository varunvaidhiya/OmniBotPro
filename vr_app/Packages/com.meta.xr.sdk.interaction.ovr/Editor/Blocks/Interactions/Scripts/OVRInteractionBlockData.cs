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
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.BuildingBlocks.Editor;
using Oculus.Interaction.Input;
using UnityEditor;

namespace Oculus.Interaction.Editor.BuildingBlocks
{
    public class OVRInteractionBlockData : BlockData
    {
        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
            // Early out if we can find a pre-existing non block version. It will get blockified
            if (TryGetPreexistingNonBlock(out var nonBlockObject)) return new List<GameObject>() { nonBlockObject };

            // Early out if we cannot find the camera rig block as it is required as a dependency
            var cameraRigBlock = Meta.XR.BuildingBlocks.Editor.Utils.GetBlock(Meta.XR.BuildingBlocks.Editor.BlockDataIds.CameraRig);
            if (cameraRigBlock == null)
            {
                throw new InvalidOperationException(
                    $"Cannot install block '{this.name}' : Cannot find block with type {cameraRigBlock.name} in the scene.");
            }

            var interaction = Instantiate(Prefab, cameraRigBlock.transform, true);
            Undo.RegisterCreatedObjectUndo(interaction, $"Create {BlockName}");
            interaction.SetActive(true);
            interaction.name = $"[BuildingBlock] {BlockName}";
            BlocksUtils.UpdateForAutoWiring(interaction);
            Undo.RegisterFullObjectHierarchyUndo(interaction, $"Auto-Wiring {BlockName}");

            return new List<GameObject>() { interaction };
        }

        private static bool TryGetPreexistingNonBlock(out GameObject nonBlockObject)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var hmd = FindObjectOfType<Hmd>();
#pragma warning restore CS0618 // Type or member is obsolete
            var interactions = hmd != null ? hmd.transform.parent : null;
            nonBlockObject = interactions != null ? interactions.gameObject : null;
            return nonBlockObject != null;
        }
    }
}
