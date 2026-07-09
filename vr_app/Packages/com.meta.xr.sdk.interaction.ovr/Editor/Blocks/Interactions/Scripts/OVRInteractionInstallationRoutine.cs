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
using Oculus.Interaction.OVR.Editor.QuickActions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    internal class OVRInteractionInstallationRoutine : InstallationRoutine
    {
        protected override bool UsesPrefab => false;

        public override List<GameObject> Install(BlockData blockData, GameObject selectedObject)
        {
            IEnumerable<GameObject> createdObjects =
                QuickActionsWizard.CreateWithDefaults<OVRComprehensiveInteractionRigWizard>(null, true,
                (wizard) =>
                {
#if UNITY_2022_1_OR_NEWER
                    wizard.InjectOptionalGenerateAsEditableCopy(false);
#endif
                });

            List<GameObject> blocks = createdObjects.ToList();
            selectedObject = blocks.First();
            selectedObject.name = $"{Utils.BlockPublicTag} {selectedObject.name}";

            Undo.RegisterFullObjectHierarchyUndo(selectedObject, $"Installing {nameof(OVRComprehensiveInteractionRigWizard)} on {selectedObject.name}");

            return blocks;
        }
    }
}
