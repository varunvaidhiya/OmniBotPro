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
using System.Threading.Tasks;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.OVR.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    internal class InteractableInstallationRoutine : InstallationRoutine
    {
        protected override bool UsesPrefab => false;

        internal override IEnumerable<OVRConfigurationTask> GetAssociatedRules(BuildingBlock block)
            => new[] { ISDKBlocksRules.DuplicateHandVisuals };


        internal abstract class InteractableCreationDescriptionBase
        {
            public abstract string Name { get; }
            public Type InteractableType;
            public Type InteractorType;

            public abstract IEnumerable<GameObject> Create(InteractableInstallationRoutine routine, GameObject target);
        }

        internal class InteractableCreationDescription<TWizard> : InteractableCreationDescriptionBase
            where TWizard : QuickActionsWizard
        {
            public Type WizardType => typeof(TWizard);
            public override string Name => ObjectNames.NicifyVariableName(WizardType.Name);
            public Func<GameObject, bool, Action<TWizard>, IEnumerable<GameObject>> CreationDelegate;
            public Func<InteractableInstallationRoutine, Action<TWizard>> CreateInjectionsDelegate;

            public override IEnumerable<GameObject> Create(InteractableInstallationRoutine routine, GameObject target)
            {
                return CreationDelegate?.Invoke(target, false, CreateInjectionsDelegate?.Invoke(routine));
            }
        }

        private enum InteractableTypes
        {
            HandGrab,
            DistanceHandGrab,
            TouchHandGrab,
        }

        [SerializeField]
        [Variant(
            Behavior = VariantAttribute.VariantBehavior.Constant)]
        private InteractableTypes type;

        [SerializeField]
        [Variant(
            Behavior = VariantAttribute.VariantBehavior.Parameter,
            Description = "The behavior of the Distance Grab Interaction",
            Condition = nameof(NeedsDistanceGrabMode),
            Default = DistanceGrabWizard.Mode.InteractableToHand)]
        private DistanceGrabWizard.Mode mode;

        internal bool NeedsDistanceGrabMode()
        {
            return type == InteractableTypes.DistanceHandGrab;
        }

        internal InteractableCreationDescriptionBase CreationDescription
        {
            get
            {
                if (!InteractableCreationDescriptions.TryGetValue(type, out var creationDescription))
                {
                    throw new InvalidOperationException(
                        $"Undefined behavior for type {type}");
                }

                return creationDescription;
            }
        }

        private static readonly Dictionary<InteractableTypes, InteractableCreationDescriptionBase>
            InteractableCreationDescriptions = new Dictionary<InteractableTypes, InteractableCreationDescriptionBase>
            {
                {
                    InteractableTypes.HandGrab,
                    new InteractableCreationDescription<GrabWizard>()
                    {
                        InteractableType =  typeof(HandGrabInteractable),
                        InteractorType = typeof(HandGrabInteractor),
                        CreationDelegate = QuickActionsWizard.CreateWithDefaults
                    }
                },
                {
                    InteractableTypes.DistanceHandGrab,
                    new InteractableCreationDescription<DistanceGrabWizard>()
                    {
                        InteractableType =  typeof(DistanceHandGrabInteractable),
                        InteractorType = typeof(DistanceHandGrabInteractor),
                        CreationDelegate = QuickActionsWizard.CreateWithDefaults,
                        CreateInjectionsDelegate = (routine) => (wizard) => wizard.InjectMode(routine.mode)
                    }
                },
                {
                    InteractableTypes.TouchHandGrab,
                    new InteractableCreationDescription<TouchGrabWizard>()
                    {
                        InteractableType =  typeof(TouchHandGrabInteractable),
                        InteractorType = typeof(TouchHandGrabInteractor),
                        CreationDelegate = QuickActionsWizard.CreateWithDefaults
                    }
                }
            };

        public override async Task<List<GameObject>> InstallAsync(BlockData blockData, GameObject selectedObject)
        {
            if (selectedObject == null)
            {
                // Install Dummy Cube
                var cubeBlockData = Utils.GetBlockData(BlockDataIds.Cube);
                var cubeBlockObjects = await cubeBlockData.InstallWithDependencies();

                // Install on Dummy Cube
                selectedObject = cubeBlockObjects.First();
            }

            if (selectedObject == null)
            {
                throw new ArgumentNullException(nameof(selectedObject));
            }

            // Get corresponding wizard
            var creationDescription = CreationDescription;

            // Invoke Wizard with Default values
            var blocks = creationDescription.Create(this, selectedObject).ToList();
            selectedObject = blocks.FirstOrDefault(obj => obj.GetComponent(creationDescription.InteractableType) != null);
            if (selectedObject != null)
            {
                selectedObject.name = $"{Utils.BlockPublicTag} {name}";
            }
            Undo.RegisterFullObjectHierarchyUndo(selectedObject, $"Installing {creationDescription.InteractableType} on {selectedObject.name}");
            return blocks;
        }


        internal override IReadOnlyCollection<InstallationStepInfo> GetInstallationSteps(VariantsSelection selection)
        {
            var installationSteps = new List<InstallationStepInfo>();
            installationSteps.AddRange(base.GetInstallationSteps(selection));
            installationSteps.Add(new InstallationStepInfo(null, $"Run <b>{CreationDescription.Name}</b> on the target object."));
            return installationSteps;
        }
    }
}
