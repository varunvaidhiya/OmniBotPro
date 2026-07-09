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
using System.Linq;
using UnityEngine;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;

namespace Oculus.Interaction.Editor.BuildingBlocks
{
    public abstract class OVRInteractionBaseBlockData : BlockData
    {
        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
            // Early out if we can find a pre-existing non block version. It will get blockified
            if (TryGetPreexistingNonBlock(out var nonBlockObject)) return new List<GameObject>() { nonBlockObject };

            // Early out if we cannot find the interaction block as it is required as a dependency
            var interactionBlockData = Meta.XR.BuildingBlocks.Editor.Utils.GetBlockData(BlockDataIds.Interaction);
            var interactionBlock = interactionBlockData.GetBlock();
            if (interactionBlock == null)
            {
                throw new InvalidOperationException(
                    $"Cannot install block '{this.name}' : Cannot find block {interactionBlockData.name} in the scene.");
            }

            var interaction = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            interaction.SetActive(true);
            interaction.name = $"{Meta.XR.BuildingBlocks.Editor.Utils.BlockPublicTag} {BlockName}";
            interaction.transform.parent = interactionBlock.transform;
            ApplyAdditionalHooks(interaction);
            BlocksUtils.UpdateForAutoWiring(interaction);

            Undo.RegisterCreatedObjectUndo(interaction, $"Instantiate {interaction.name}");
            Undo.SetTransformParent(interaction.transform, interactionBlock.transform, true,
                $"Parent {interaction.name} to {interactionBlock.name}");

            return new List<GameObject>() { interaction };
        }

        protected abstract bool TryGetPreexistingNonBlock(out GameObject nonBlockObject);
        protected abstract void ApplyAdditionalHooks(GameObject blockObject);

        protected bool TryGetInteractorBase<T>(string interactorGroupName, out GameObject interactorBaseObject)
            where T : Component
        {
            var interactorBaseTransform = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(component => HasInteractorGroupChild(component, interactorGroupName))?.transform.parent;
            interactorBaseObject = interactorBaseTransform != null ? interactorBaseTransform.gameObject : null;
            return interactorBaseObject != null;
        }

        private bool HasInteractorGroupChild<T>(T component, string interactorGroupName)
            where T : Component
            => component.transform.Cast<Transform>()
                .Any(child => child.gameObject.name.StartsWith(interactorGroupName));
    }
}
