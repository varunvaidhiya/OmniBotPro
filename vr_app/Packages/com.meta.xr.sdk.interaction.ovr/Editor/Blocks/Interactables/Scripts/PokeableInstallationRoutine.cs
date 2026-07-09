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

using Oculus.Interaction.Editor.QuickActions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    internal class PokeableInstallationRoutine : InstallationRoutine
    {
        protected override bool UsesPrefab => false;

        public override List<GameObject> Install(BlockData blockData, GameObject selectedObject)
        {
            if (selectedObject == null)
            {
                // Install on empty GameObject
                selectedObject = new GameObject(blockData.BlockName);
            }

            if (selectedObject == null)
            {
                throw new ArgumentNullException(nameof(selectedObject));
            }

            IEnumerable<GameObject> createdObjects =
                QuickActionsWizard.CreateWithDefaults<PokeWizard>(selectedObject, true,
                (wizard) =>
                {
                });

            List<GameObject> blocks = createdObjects.ToList();
            selectedObject = blocks.First();
            selectedObject.name = $"{Utils.BlockPublicTag} {selectedObject.name}";
            Undo.RegisterFullObjectHierarchyUndo(selectedObject, $"Installing {nameof(PokeWizard)} on {selectedObject.name}");

            return blocks;
        }
    }
}
