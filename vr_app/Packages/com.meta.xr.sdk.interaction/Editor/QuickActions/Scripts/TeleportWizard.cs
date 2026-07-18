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

using Oculus.Interaction.DistanceReticles;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Surfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal class TeleportWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add Teleport Interaction";

        internal enum TeleportInteractableType
        {
            Hotspot,
            Plane,
            NavMesh,
            PhysicsLayer
        }

        internal enum TeleportHotspotSnapType
        {
            None,
            SnapPosition,
            SnapPositionAndRotation,
        }

        [MenuItem(MENU_NAME, priority = MenuOrder.TELEPORT_LOCOMOTION)]
        private static void OpenWizard()
        {
            ShowWindow<TeleportWizard>(Selection.gameObjects[0]);
        }

        [MenuItem(MENU_NAME, true)]
        static bool Validate()
        {
            return Selection.gameObjects.Length == 1;
        }

        #region Fields

        [SerializeField]
        [DeviceType, WizardSetting]
        [InspectorName("Add Interactor(s)")]
        [Tooltip("The interactors required for the new interactable " +
            "will be added, if not already present.")]
        private DeviceTypes _deviceTypes = DeviceTypes.All;

        [SerializeField]
        [WizardSetting]
        [ConditionalHide(nameof(_hasOVRPackage), true,
            ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        [InspectorName("Use MicroGestures")]
        [Tooltip("If new Hand Teleport Interactors are needed, they will" +
            "use OVR Microgestures.")]
        [BooleanDropdown(True = "Use OVR Microgestures", False = "Use L gesture")]
        private bool _useMicrogestures = true;

        [SerializeField, HideInInspector]
        private bool _hasOVRPackage = false;

        [SerializeField, Interface(typeof(ILocomotionEventHandler))]
        [Tooltip("The Locomotor receives events from the Interactor and effectively " +
            "moves the player rig around the scene.")]
        [ConditionalHide(nameof(_deviceTypes), DeviceTypes.None,
            ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [WizardDependency(FindMethod = nameof(FindLocomotor), FixMethod = nameof(FixLocomotor), Category = Category.Optional)]
        private UnityEngine.Object _locomotor;
        private ILocomotionEventHandler Locomotor => _locomotor as ILocomotionEventHandler;

        [SerializeField]
        [Tooltip("Teleport Interactables are Surfaces the Interactor can teleport to.")]
        [WizardSetting]
        private TeleportInteractableType _interactableType = TeleportInteractableType.Hotspot;

        [SerializeField]
        [Tooltip("Hotspot only: Collider to use for detecting the hotspot with the teleport arc.")]
        [WizardDependency(FindMethod = nameof(FindHotspotCollider), FixMethod = nameof(FixHotspotCollider))]
        [ConditionalHide(nameof(_interactableType), TeleportInteractableType.Hotspot,
            ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        private Collider _hotspotCollider;

        [SerializeField]
        [Tooltip("Hotspot only: Moves the player to the referenced transform of the interactable.")]
        [WizardSetting]
        [ConditionalHide(nameof(_interactableType), TeleportInteractableType.Hotspot,
            ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        private TeleportHotspotSnapType _hotspotSnap = TeleportHotspotSnapType.SnapPosition;

        [SerializeField]
        [Tooltip("Hotspots only: when snapping, it aligns the player's head to this transform. " +
            "If left empty it will snap the feet.")]
        [WizardDependency(Category = Category.Optional, FindMethod = "", FixMethod = "", ReadOnly = false)]
        [ConditionalHide(nameof(_interactableType), TeleportInteractableType.Hotspot,
            ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        private Transform _snapHeadTransform = null;

        [SerializeField]
        [Tooltip("Navigation Mesh only: The area of the Navigation Mesh the teleport arc can query.")]
        [WizardSetting]
        [ConditionalHide(nameof(_interactableType), TeleportInteractableType.NavMesh,
            ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        private string _walkableAreaName = "Walkable";

        [SerializeField]
        [Tooltip("Physics Layer(s) only: The physics layer(s) the teleport arc can query.")]
        [WizardSetting]
        [ConditionalHide(nameof(_interactableType), TeleportInteractableType.PhysicsLayer,
            ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        private LayerMask _layerMask = -1;

        [SerializeField]
        [Tooltip("The teleport interactable will allow teleporting here or block the teleport ray. " +
            "You can take advantage of this with Plane or Physics Layers to easily block teleporting through obstacles.")]
        [WizardSetting]
        [BooleanDropdown(True = "Allows teleporting here", False = "Blocks the teleport ray")]
        private bool _allowsTeleport = true;

        [SerializeField]
        [Tooltip("The main transform where the Teleport Interactable will be placed. " +
            "In hotspot mode, the Ring for the feet will appear here.")]
        [WizardDependency(FindMethod = nameof(FindTransform), FixMethod = nameof(FixTransform))]
        private Transform _targetTransform;

        private const string _visualsName = "Visuals";
        private const string _headSnapTransformName = "Visuals/Arrow";
        private const string _feetSnapTransformName = "Visuals/Ring";

        #endregion Fields

        private void FindTransform()
        {
            _targetTransform = Target.GetComponent<Transform>();
        }

        private void FixTransform()
        {
            FindTransform();
        }

        private void FindHotspotCollider()
        {
            _hotspotCollider = Target.GetComponentInChildren<Collider>();
        }

        private void FixHotspotCollider()
        {
            if (_interactableType != TeleportInteractableType.Hotspot)
            {
                return;
            }

            Collider collider;
            GameObject teleportColliderGO = new GameObject("Teleport Collider");
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
            _hotspotCollider = collider;
        }

        private void FindLocomotor()
        {
            _locomotor = GameObject.FindAnyObjectByType<PlayerLocomotor>();
            if (_locomotor == null)
            {
                _locomotor = GameObject.FindAnyObjectByType<FirstPersonLocomotor>();
            }
        }

        private void FixLocomotor()
        {
            GameObject locomotorGO = new GameObject("Locomotor");

            //Try to make it a sibling of the Hmd, if it exists
            Hmd hmd = InteractorUtils.GetHmd();
            if (hmd != null)
            {
                locomotorGO.transform.SetParent(hmd.transform.parent);
            }

            _locomotor = locomotorGO.AddComponent<PlayerLocomotor>();
            UnityObjectAddedBroadcaster.HandleObjectWasAdded(locomotorGO);
        }

        private ISurface GenerateColliderSurface(GameObject root)
        {
            ColliderSurface colliderSurface = root.AddComponent<ColliderSurface>();
            colliderSurface.InjectAllColliderSurface(_hotspotCollider);
            return colliderSurface;
        }

        private ISurface GenerateFloorSurface(GameObject root)
        {
            GameObject floor = new GameObject("Plane");
            floor.transform.parent = root.transform;
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

            PlaneSurface surface = AddComponent<PlaneSurface>(floor);
            surface.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
            return surface;
        }

        private ISurface GenerateNavMeshSurface(GameObject root)
        {
            NavMeshSurface surface = AddComponent<NavMeshSurface>(root);
            surface.InjectOptionalAreaName(_walkableAreaName);
            return surface;
        }

        private ISurface GeneratePhysicsLayerSurface(GameObject root)
        {
            PhysicsLayerSurface surface = AddComponent<PhysicsLayerSurface>(root);
            surface.LayerMask = _layerMask;
            return surface;
        }

        protected override void Create()
        {
            GameObject instance = Templates.CreateFromTemplate(
                Target.transform, Templates.TeleportInteractable);

            instance.transform.SetPose(Pose.identity, Space.Self);

            TeleportInteractable interactable =
                instance.GetComponent<TeleportInteractable>();

            ReticleDataTeleport reticleData =
                instance.GetComponent<ReticleDataTeleport>();

            Transform visuals = instance.transform.Find(_visualsName);
            Transform headSnapPoint = instance.transform.Find(_headSnapTransformName);
            Transform feetSnapPoint = instance.transform.Find(_feetSnapTransformName);

            if (_interactableType == TeleportInteractableType.Hotspot)
            {
                visuals.SetPose(_targetTransform.GetPose());
                visuals.gameObject.SetActive(false);

                ISurface surface = GenerateColliderSurface(instance);
                interactable.InjectSurface(surface);

                if (_hotspotSnap == TeleportHotspotSnapType.None)
                {
                    interactable.InjectOptionalTargetPoint(null);

                    reticleData.HideReticle = false;
                    interactable.EyeLevel = false;
                    interactable.FaceTargetDirection = false;

                    DisableReticleVisuals(reticleData, visuals);
                }
                else
                {
                    bool snapHead = _snapHeadTransform != null;
                    bool snapRotation = _hotspotSnap == TeleportHotspotSnapType.SnapPositionAndRotation;

                    visuals.gameObject.SetActive(true);
                    if (snapHead)
                    {
                        headSnapPoint.transform.SetPose(_snapHeadTransform.GetPose());
                        headSnapPoint.gameObject.SetActive(true);
                    }
                    else
                    {
                        headSnapPoint.gameObject.SetActive(false);
                    }

                    if (!snapRotation || snapHead)
                    {
                        feetSnapPoint.transform.rotation = Quaternion.identity;
                    }

                    interactable.InjectOptionalTargetPoint(snapHead ? _snapHeadTransform : _targetTransform);
                    reticleData.InjectOptionalSnapPoint(_targetTransform);

                    reticleData.HideReticle = true;
                    interactable.EyeLevel = snapHead;
                    interactable.FaceTargetDirection = snapRotation;
                }

                interactable.TieBreakerScore = _allowsTeleport ? 10 : -100;
            }
            else if (_interactableType == TeleportInteractableType.Plane)
            {
                ISurface surface = GenerateFloorSurface(instance);
                interactable.InjectSurface(surface);
                interactable.TieBreakerScore = _allowsTeleport ? 0 : -100;
                DisableReticleVisuals(reticleData, visuals);
            }
            else if (_interactableType == TeleportInteractableType.NavMesh)
            {
                ISurface surface = GenerateNavMeshSurface(instance);
                interactable.InjectSurface(surface);
                interactable.TieBreakerScore = _allowsTeleport ? 0 : -100;
                DisableReticleVisuals(reticleData, visuals);
            }
            else if (_interactableType == TeleportInteractableType.PhysicsLayer)
            {
                ISurface surface = GeneratePhysicsLayerSurface(instance);
                interactable.InjectSurface(surface);
                interactable.TieBreakerScore = _allowsTeleport ? 0 : -100;
                DisableReticleVisuals(reticleData, visuals);
            }

            interactable.AllowTeleport = _allowsTeleport;

            if ((_deviceTypes & DeviceTypes.Controllers) != 0)
            {
                AddDefaultInteractors(DeviceTypes.Controllers);
            }
            if ((_deviceTypes & DeviceTypes.ControllerDrivenHands) != 0)
            {
                AddDefaultInteractors(DeviceTypes.ControllerDrivenHands);
            }
            if ((_deviceTypes & DeviceTypes.Hands) != 0)
            {
                if (HasOVRPackage() && _useMicrogestures)
                {
                    AddMicrogestureInteractors();
                }
                else
                {
                    AddDefaultInteractors(DeviceTypes.Hands);
                }
            }
        }

        protected override void InitializeFieldsExtra()
        {
            base.InitializeFieldsExtra();
            _useMicrogestures = _hasOVRPackage = HasOVRPackage();
        }

        private bool HasOVRPackage()
        {
            string packageName = "com.meta.xr.sdk.interaction.ovr";
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            bool found = packages.Any(pi => pi.name == packageName);

            return found;
        }

        private void AddMicrogestureInteractors()
        {
            List<GameObject> newInteractors = new List<GameObject>();
            if (InteractorUtils.CanAddHandInteractorsToRig())
            {
                foreach (Hand hand in InteractorUtils.GetHands())
                {
                    if (InteractorUtils.TryFindInteractorsGroup(hand, out InteractorGroup group, out Transform holder)
                        && !InteractorUtils.GetExistingHandInteractors(holder).HasFlag(InteractorTypes.Teleport))
                    {
                        InteractorTemplate template = Templates.MicrogestureTeleportInteractor;
                        GameObject interactors = InteractorUtils.AddInteractor(template,
                                InteractorUtils.GetHmd(), holder, group);
                        interactors.GetComponent<HandRef>().InjectHand(hand);
                        newInteractors.Add(interactors);
                        UnityObjectAddedBroadcaster.HandleObjectWasAdded(interactors);
                    }
                }
            }
            InjectLocomotionHandler(newInteractors);
        }

        private void AddDefaultInteractors(DeviceTypes deviceTypes)
        {
            var interactors = InteractorUtils.AddInteractorsToRig(
                InteractorTypes.Teleport, deviceTypes);
            InjectLocomotionHandler(interactors);
        }

        private void InjectLocomotionHandler(IEnumerable<GameObject> interactors)
        {
            foreach (var interactor in interactors)
            {
                if (interactor.TryGetComponent(out LocomotionEventsConnection locomotionEventconnection))
                {
                    locomotionEventconnection.InjectHandlers(new() { Locomotor });
                }
                UnityObjectAddedBroadcaster.HandleObjectWasAdded(interactor);
            }
        }

        private void DisableReticleVisuals(ReticleDataTeleport reticleData, Transform visuals)
        {
            reticleData.HideReticle = false;
            reticleData.InjectOptionalMaterialPropertyBlockEditor(null);
            DestroyImmediate(visuals.gameObject);
        }

        #region Injects

        public void InjectOptionalDeviceTypes(DeviceTypes deviceTypes)
        {
            _deviceTypes = deviceTypes;
        }

        public void InjectOptionalAllowTeleport(bool allowTeleport)
        {
            _allowsTeleport = allowTeleport;
        }

        public void InjectOptionalHotspotType(Collider collider, TeleportHotspotSnapType snapType, Transform snapHeadTransform)
        {
            _interactableType = TeleportInteractableType.Hotspot;
            _hotspotCollider = collider;
            _hotspotSnap = snapType;
            _snapHeadTransform = snapHeadTransform;
        }

        public void InjectOptionalNavMeshType(string walkableArea)
        {
            _interactableType = TeleportInteractableType.NavMesh;
            _walkableAreaName = walkableArea;
        }

        public void InjectOptionalPlaneType()
        {
            _interactableType = TeleportInteractableType.Plane;
        }

        public void InjectOptionalPhysicsLayerType(LayerMask layerMask)
        {
            _interactableType = TeleportInteractableType.PhysicsLayer;
            _layerMask = layerMask;
        }


        #endregion
    }
}
