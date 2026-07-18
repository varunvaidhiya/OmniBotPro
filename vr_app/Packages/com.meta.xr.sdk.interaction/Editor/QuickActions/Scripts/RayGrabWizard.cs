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
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal class RayGrabWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add Ray Grab Interaction";

        [MenuItem(MENU_NAME, priority = MenuOrder.RAY_GRAB)]
        private static void OpenWizard()
        {
            ShowWindow<RayGrabWizard>(Selection.gameObjects[0]);
        }

        [MenuItem(MENU_NAME, true)]
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

        [SerializeField]
        [InspectorName("Raycast Surface")]
        [Tooltip("This surface will be used for hit testing the interaction. " +
            "If a collider should be used for hit testing instead of an ISurface, " +
            "leave this null and assign a collider to the 'Raycast Collider' field.")]
        [WizardDependency(Category = Category.Optional,
            FindMethod = nameof(FindSurface))]
        [Interface(typeof(ISurface))]
        private UnityEngine.Object _surface;

        [SerializeField]
        [InspectorName("Raycast Collider")]
        [Tooltip("If present, this collider will be used for raycast hit tests.")]
        [WizardDependency(Category = Category.Optional,
            FindMethod = nameof(FindCollider), FixMethod = nameof(FixCollider))]
        [ConditionalHide(nameof(_surface), null)]
        private Collider _collider;

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
            Transform target = _rigidbody != null ? _rigidbody.transform : Target.transform;
            Grabbable grabbable = AddComponent<Grabbable>(Target);
            grabbable.InjectOptionalTargetTransform(target);
            FindGrabbable();
        }

        private void FixRigidbody()
        {
            _rigidbody = AddComponent<Rigidbody>(Target);
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
        }

        private void FindSurface()
        {
            _surface = Target.GetComponent<ISurface>() as UnityEngine.Object;
        }

        private void FindCollider()
        {
            _collider = Target.GetComponent<Collider>();
        }

        private void FixCollider()
        {
            _collider = Utils.GenerateCollider(Target);
        }

        protected override void Create()
        {
            GameObject obj = Templates.CreateFromTemplate(
                Target.transform, Templates.RayGrabInteractable);

            Transform transform = obj.transform;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            var rayInteractable = obj.GetComponent<RayInteractable>();
            rayInteractable.InjectOptionalPointableElement(Grabbable);

            ISurface surface = _surface as ISurface;
            if (surface == null)
            {
                var colliderSurface = AddComponent<ColliderSurface>(obj);
                colliderSurface.InjectCollider(_collider);
                surface = colliderSurface;
            }

            rayInteractable.InjectSurface(surface);

            var interactors = InteractorUtils.AddInteractorsToRig(
                InteractorTypes.Ray, _deviceTypes);
        }

        protected override IEnumerable<MessageData> GetMessages()
        {
            var result = Enumerable.Empty<MessageData>();

            if (_collider == null && _surface == null)
            {
                void FindOrFixCollider()
                {
                    FindCollider();
                    if (_collider == null)
                    {
                        FixCollider();
                    }
                }
                result = result.Append(new MessageData(MessageType.Error,
                    "A Collider or a Surface must be provided for raycast hit testing. " +
                    "Assign either an ISurface or a Collider to the appropriate fields, or " +
                    "press the Fix button to generate a collider.",
                    new ButtonData("Fix", FindOrFixCollider)));
            }

            return result;
        }

        #region Injects

        public void InjectOptionalDeviceTypes(DeviceTypes deviceTypes)
        {
            _deviceTypes = deviceTypes;
        }

        public void InjectOptionalTargetTransform(Transform targetTransform)
        {
            _targetTransform = targetTransform;
        }

        public void InjectOptionalRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalGrabbable(IPointableElement grabbable)
        {
            _grabbable = grabbable as UnityEngine.Object;
            Grabbable = grabbable;
        }

        public void InjectOptionalSurface(ISurface surface)
        {
            _surface = surface as UnityEngine.Object;
        }

        public void InjectOptionalCollider(Collider collider)
        {
            _collider = collider;
        }

        #endregion
    }
}
