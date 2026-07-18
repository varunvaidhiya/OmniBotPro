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
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal class TouchGrabWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add TouchGrab Interaction (Internal only)";

        private static void OpenWizard()
        {
            ShowWindow<TouchGrabWizard>(Selection.gameObjects[0]);
        }

        private static bool Validate()
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
        [WizardSetting]
        [BooleanDropdown(False = "Do Not Generate Collider", True = "Generate Collider")]
        [Tooltip("If a collider is not present under the provided RigidBody, " +
            "a collider will be auto-generated.")]
        private bool _autoGenerateCollider = true;

        [SerializeField]
        [Tooltip("The transform to be moved when grabbing the object.")]
        [WizardDependency(FindMethod = nameof(FindTransform), FixMethod = nameof(FixTransform))]
        private Transform _targetTransform;

        [SerializeField]
        [Tooltip("The rigidbody representing the physics object that will be moved.")]
        [WizardDependency(FindMethod = nameof(FindRigidbody), FixMethod = nameof(FixRigidbody))]
        private Rigidbody _rigidbody;

        [SerializeField, Interface(typeof(IPointableElement))]
        [Tooltip("The grabbable that will receive the Interactable events and move the object.")]
        [WizardDependency(FindMethod = nameof(FindGrabbable), FixMethod = nameof(FixGrabbable))]
        private UnityEngine.Object _grabbable;
        private IPointableElement Grabbable { get; set; }

        #endregion Fields

        private void FindTransform()
        {
            _targetTransform = Target.GetComponent<Transform>();
        }

        private void FixTransform()
        {
            FindTransform();
        }

        private void FindRigidbody()
        {
            _rigidbody = Target.GetComponent<Rigidbody>();
        }

        private void FixRigidbody()
        {
            GameObject target = _targetTransform != null ? _targetTransform.gameObject : Target;
            _rigidbody = AddComponent<Rigidbody>(target);
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
        }

        private void FindGrabbable()
        {
            Grabbable = Target.GetComponent<Grabbable>();
            if (Grabbable == null)
            {
                Grabbable = Target.GetComponent<IPointableElement>();
            }
            _grabbable = Grabbable as UnityEngine.Object;
        }

        private void FixGrabbable()
        {
            Grabbable grabbable = AddComponent<Grabbable>(Target);
            grabbable.InjectOptionalTargetTransform(_targetTransform);
            FindGrabbable();
        }

        private bool HasCollider()
        {
            return _rigidbody != null && _rigidbody.gameObject.
                GetComponentInChildren<Collider>() != null;
        }
        protected override void Create()
        {
            GameObject obj = Templates.CreateFromTemplate(
                Target.transform, Templates.TouchHandGrabInteractable);

            Transform transform = obj.transform;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            TouchHandGrabInteractable interactable =
                obj.GetComponent<TouchHandGrabInteractable>();

            Collider collider = _rigidbody.gameObject.GetComponentInChildren<Collider>();
            if (!HasCollider() && _autoGenerateCollider)
            {
                collider = Utils.GenerateCollider(_rigidbody.gameObject);
            }

            if (collider != null)
            {
                interactable.InjectBoundsCollider(collider);
                interactable.InjectColliders(new List<Collider>() { collider });
            }
            interactable.InjectOptionalPointableElement(Grabbable);

            var interactors = InteractorUtils.AddInteractorsToRig(
                InteractorTypes.TouchGrab, _deviceTypes);

            foreach (var interactor in interactors)
            {
                UnityObjectAddedBroadcaster.HandleObjectWasAdded(interactor);
            }
        }
    }
}
