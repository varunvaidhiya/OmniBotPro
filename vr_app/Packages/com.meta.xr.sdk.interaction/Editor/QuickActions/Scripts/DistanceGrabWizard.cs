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

using UnityEditor;
using UnityEngine;
using System;
using System.ComponentModel;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.DistanceReticles;
using System.Collections.Generic;
using System.Linq;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal class DistanceGrabWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add Distance Grab Interaction";

        [MenuItem(MENU_NAME, priority = MenuOrder.DISTANCE_GRAB)]
        private static void OpenWizard()
        {
            ShowWindow<DistanceGrabWizard>(Selection.gameObjects[0]);
        }

        [MenuItem(MENU_NAME, true)]
        static bool Validate()
        {
            return Selection.gameObjects.Length == 1;
        }

        internal enum Mode
        {
            [InspectorName("Grab Relative To Hand")]
            AnchorAtHand,

            [InspectorName("Pull Interactable To Hand")]
            InteractableToHand,

            [InspectorName("Manipulate In Place")]
            HandToInteractable,
        }

        #region Fields

        [SerializeField]
        [DeviceType, WizardSetting]
        [InspectorName("Add Required Interactor(s)")]
        [Tooltip("The interactors required for the new interactable will be " +
            "added for the device types selected here, if not already present.")]
        private DeviceTypes _deviceTypes = DeviceTypes.All;

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
        [InspectorName("Time Out Snap Zone")]
        [Tooltip("If provided, the object will snap back to this location once released.")]
        [WizardDependency(Category = Category.Optional, FixMethod = nameof(FixSnapZone))]
        private SnapInteractable _timeOutSnapZone;

        [SerializeField]
        [InspectorName("Hologram Mesh")]
        [Tooltip("If provided, a hologram of the object moving toward the hand " +
            "will be animated before the object is grabbed.")]
        [ConditionalHide(nameof(_mode), Mode.InteractableToHand)]
        [WizardDependency(Category = Category.Optional, FindMethod = nameof(FindMeshFilter))]
        private MeshFilter _hologramMesh;

        [SerializeField]
        [InspectorName("Distance Grab Type")]
        [Tooltip("The behavior of the Distance Grab interaction")]
        [WizardSetting]
        private Mode _mode = Mode.InteractableToHand;

        [SerializeField]
        [InspectorName("Supported Grab Types")]
        [Tooltip("The grab types that will be supported by this interactable.")]
        [WizardSetting]
        [DefaultValue(GrabTypeFlags.All)]
        private GrabTypeFlags _grabTypeFlags;

        [SerializeField]
        [WizardSetting]
        [BooleanDropdown(False = "Do Not Generate Collider", True = "Generate Collider")]
        [Tooltip("If a collider is not present under the provided RigidBody, " +
            "a collider will be auto-generated.")]
        private bool _autoGenerateCollider = true;

        #endregion Fields

        private void FindRigidbody()
        {
            _rigidbody = Target.GetComponent<Rigidbody>();
        }

        private void FixRigidbody()
        {
            _rigidbody = AddComponent<Rigidbody>(Target);
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
            Transform target = _rigidbody != null ? _rigidbody.transform : Target.transform;
            Grabbable grabbable = AddComponent<Grabbable>(Target);
            grabbable.InjectOptionalTargetTransform(target);
            FindGrabbable();
        }

        private bool HasCollider()
        {
            return _rigidbody != null && _rigidbody.gameObject.
                GetComponentInChildren<Collider>() != null;
        }

        private void GenerateCollider(GameObject root)
        {
            Collider collider;
            if (Utils.TryEncapsulateRenderers(root,
                out Bounds localBounds))
            {
                var boxCollider = AddComponent<BoxCollider>(root);
                boxCollider.center = localBounds.center;
                boxCollider.size = localBounds.size;
                collider = boxCollider;
            }
            else
            {
                collider = AddComponent<SphereCollider>(root);
            }
            collider.isTrigger = true;
        }

        private void FixSnapZone()
        {
            var snapZone = Templates.CreateFromTemplate(Target.transform.parent,
                Templates.DistanceGrabInteractable_SnapZone);
            _timeOutSnapZone = snapZone.GetComponent<SnapInteractable>();
            snapZone.name = $"ISDK_SnapZone ({Target.gameObject.name})";
            var transform = snapZone.transform;
            transform.position = Target.transform.position;
            transform.rotation = Target.transform.rotation;
            transform.localScale = Vector3.zero;
        }

        private void FindMeshFilter()
        {
            _hologramMesh = Target.GetComponentInChildren<MeshFilter>();
        }

        #region Inject

        internal void InjectMode(Mode mode) => _mode = mode;

        #endregion

        protected override void Create()
        {
            Template template;
            switch (_mode)
            {
                case Mode.AnchorAtHand:
                    template = Templates.DistanceGrabInteractable_AnchorAtHand;
                    break;
                case Mode.InteractableToHand:
                    template = Templates.DistanceGrabInteractable_ToHand;
                    break;
                case Mode.HandToInteractable:
                    template = Templates.DistanceGrabInteractable_HandTo;
                    break;
                default:
                    throw new NotSupportedException("Distance Hand Grab Type Unsupported");
            }

            GameObject obj = Templates.CreateFromTemplate(Target.transform, template);

            Transform transform = obj.transform;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            DistanceHandGrabInteractable distanceHandInteractable =
                obj.GetComponentInChildren<DistanceHandGrabInteractable>();

            distanceHandInteractable.InjectRigidbody(_rigidbody);
            distanceHandInteractable.InjectSupportedGrabTypes(_grabTypeFlags);
            distanceHandInteractable.InjectOptionalPointableElement(Grabbable);

            DistanceGrabInteractable distanceInteractable =
                obj.GetComponentInChildren<DistanceGrabInteractable>();

            distanceInteractable.InjectRigidbody(_rigidbody);
            distanceInteractable.InjectOptionalPointableElement(Grabbable);

            SnapInteractor snapInteractor =
                obj.GetComponent<SnapInteractor>();

            if (_timeOutSnapZone != null)
            {
                snapInteractor.InjectRigidbody(_rigidbody);
                snapInteractor.InjectOptionalTimeOutInteractable(_timeOutSnapZone);
                snapInteractor.InjectPointableElement(Grabbable as PointableElement);
            }
            else
            {
                DestroyImmediate(snapInteractor);
            }

            ReticleDataMesh reticleDataMesh =
                obj.GetComponentInChildren<ReticleDataMesh>();

            if (reticleDataMesh != null && _hologramMesh != null)
            {
                reticleDataMesh.Filter = _hologramMesh;
            }
            else
            {
                DestroyImmediate(reticleDataMesh);
            }

            if (!HasCollider() && _autoGenerateCollider)
            {
                GenerateCollider(_rigidbody.gameObject);
            }

            InteractorUtils.AddInteractorsToRig(
                InteractorTypes.DistanceGrab, _deviceTypes);
        }

        protected override IEnumerable<MessageData> GetMessages()
        {
            IEnumerable<MessageData> messages = Enumerable.Empty<MessageData>();
            if (_rigidbody != null && !HasCollider() && !_autoGenerateCollider)
            {
                messages = messages.Append(new MessageData(MessageType.Warning,
                    "No collider(s) detected in the target rigidbody hierarchy. Distance Grab " +
                    "interaction uses physics collisions to find interactables in the scene, " +
                    "and will not work without a collider(s) present.",
                    new ButtonData("Enable Auto\nGeneration", () => _autoGenerateCollider = true)));
            }
            return messages;
        }
    }
}
