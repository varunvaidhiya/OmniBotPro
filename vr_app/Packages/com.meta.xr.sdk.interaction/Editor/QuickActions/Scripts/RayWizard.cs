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

using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal class RayWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add Ray Interaction (Internal only)";

        private static void OpenWizard()
        {
            ShowWindow<RayWizard>(Selection.gameObjects[0]);
        }

        static bool Validate()
        {
            return Selection.gameObjects.Length == 1;
        }

        #region Fields

        [SerializeField]
        [DeviceType, WizardSetting]
        [InspectorName("Add Required Interactor(s)")]
        [Tooltip("The interactors required for the new interactable will be " +
            "added for the device types selected here, if not already present.")]
        private DeviceTypes _deviceTypes = DeviceTypes.All;

        [SerializeField]
        [Tooltip("Collider to use for detecting the Ray.")]
        [WizardDependency(FindMethod = nameof(FindCollider), FixMethod = nameof(FixCollider),
            Category = Category.Optional)]
        private Collider _collider;

        #endregion Fields

        private bool HasCollider()
        {
            return Target.GetComponentInChildren<Collider>() != null;
        }

        private void FindCollider()
        {
            _collider = Target.GetComponentInChildren<Collider>();
        }

        private void FixCollider()
        {
            Collider collider;
            GameObject teleportColliderGO = new GameObject("Collider");
            teleportColliderGO.transform.parent = Target.transform;
            teleportColliderGO.transform.SetPose(Pose.identity, Space.Self);

            if (Utils.TryEncapsulateRenderers(Target,
                out Bounds localBounds))
            {
                var boxCollider = teleportColliderGO.AddComponent<BoxCollider>();
                boxCollider.center = localBounds.center;
                boxCollider.size = localBounds.size;
                collider = boxCollider;
            }
            else
            {
                collider = teleportColliderGO.AddComponent<SphereCollider>();
            }
            collider.isTrigger = true;
            _collider = collider;
        }

        protected override void Create()
        {
            GameObject obj = Templates.CreateFromTemplate(
                Target.transform, Templates.RayInteractable);
            if (_collider != null)
            {
                ColliderSurface surface = obj.GetComponentInChildren<ColliderSurface>();
                surface.GetComponent<Collider>().enabled = false;
                surface.InjectCollider(_collider);
            }

            InteractorUtils.AddInteractorsToRig(
                InteractorTypes.Ray, _deviceTypes);
        }
    }
}
