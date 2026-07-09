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

using Meta.XR.Util;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    interface IUsage
    {
        void Apply(ControllerDataAsset controllerDataAsset, OVRInput.Controller controllerMask);
    }

    class UsageTouchMapping : IUsage
    {
        public ControllerButtonUsage Usage { get; }
        public OVRInput.Touch Touch { get; }

        public UsageTouchMapping(ControllerButtonUsage usage, OVRInput.Touch touch)
        {
            Usage = usage;
            Touch = touch;
        }

        public void Apply(ControllerDataAsset controllerDataAsset, OVRInput.Controller controllerMask)
        {
            bool value = OVRInput.Get(Touch, controllerMask);
            controllerDataAsset.Input.SetButton(Usage, value);
        }
    }

    class UsageButtonMapping : IUsage
    {
        public ControllerButtonUsage Usage { get; }
        public OVRInput.Button Button { get; }

        public UsageButtonMapping(ControllerButtonUsage usage, OVRInput.Button button)
        {
            Usage = usage;
            Button = button;
        }

        public void Apply(ControllerDataAsset controllerDataAsset, OVRInput.Controller controllerMask)
        {
            bool value = OVRInput.Get(Button, controllerMask);
            controllerDataAsset.Input.SetButton(Usage, value);
        }
    }

    class UsageAxis1DMapping : IUsage
    {
        public ControllerAxis1DUsage Usage { get; }
        public OVRInput.Axis1D Axis1D { get; }

        public UsageAxis1DMapping(ControllerAxis1DUsage usage, OVRInput.Axis1D axis1D)
        {
            Usage = usage;
            Axis1D = axis1D;
        }

        public void Apply(ControllerDataAsset controllerDataAsset, OVRInput.Controller controllerMask)
        {
            float value = OVRInput.Get(Axis1D, controllerMask);
            controllerDataAsset.Input.SetAxis1D(Usage, value);
        }
    }

    class UsageAxis2DMapping : IUsage
    {
        public ControllerAxis2DUsage Usage { get; }
        public OVRInput.Axis2D Axis2D { get; }

        public UsageAxis2DMapping(ControllerAxis2DUsage usage, OVRInput.Axis2D axis2D)
        {
            Usage = usage;
            Axis2D = axis2D;
        }

        public void Apply(ControllerDataAsset controllerDataAsset, OVRInput.Controller controllerMask)
        {
            Vector2 value = OVRInput.Get(Axis2D, controllerMask);
            controllerDataAsset.Input.SetAxis2D(Usage, value);
        }
    }

    /// <summary>
    /// Returns the Pointer Pose for the active controller model
    /// as found in the official prefabs.
    /// This point is usually located at the front tip of the controller.
    /// </summary>
    struct OVRPointerPoseSelector
    {
        private static readonly Pose[] QUEST1_POINTERS = new Pose[2]
        {
            new Pose(new Vector3(-0.00779999979f,-0.00410000002f,0.0375000015f),
                Quaternion.Euler(359.209534f, 6.45196056f, 6.95544577f)),
            new Pose(new Vector3(0.00779999979f,-0.00410000002f,0.0375000015f),
                Quaternion.Euler(359.209534f, 353.548035f, 353.044556f))
        };

        private static readonly Pose[] TOUCH_POINTERS = new Pose[2]
        {
            new Pose(new Vector3(0.00899999961f, -0.00321028521f, 0.030869998f),
                Quaternion.Euler(359.209534f, 6.45196056f, 6.95544577f)),
            new Pose(new Vector3(-0.00899999961f, -0.00321028521f, 0.030869998f),
                Quaternion.Euler(359.209534f, 353.548035f, 353.044556f))
        };

        private static readonly Pose[] TOUCHPRO_POINTERS = new Pose[2]
        {
            new Pose(new Vector3(0.008f, 0f, 0.03f),
                Quaternion.AngleAxis(5f, new Vector3(0f, 1f, 0f))),
            new Pose(new Vector3(-0.008f, 0f, 0.03f),
                Quaternion.AngleAxis(-5f, new Vector3(0f, 1f, 0f)))
        };

        private static readonly Pose[] TOUCHPLUS_POINTERS = new Pose[2]
        {
            new Pose(new Vector3(0.008f, 0.00014f, 0.03f),
                Quaternion.AngleAxis(5f, new Vector3(0f, 1f, 0f))),
            new Pose(new Vector3(-0.008f, 0.00014f, 0.03f),
                Quaternion.AngleAxis(-5f, new Vector3(0f, 1f, 0f)))
        };

        private OVRPlugin.SystemHeadset _headset;
        private OVRPlugin.InteractionProfile _controller;
        private bool _found;
        private Pose _cachedPose;
        private OVRPlugin.Hand _handedness;
        private int _handIndex;

        public OVRPointerPoseSelector(Handedness handedness)
        {
            _handedness = handedness == Handedness.Left ? OVRPlugin.Hand.HandLeft : OVRPlugin.Hand.HandRight;
            _handIndex = (int)handedness;
            _controller = OVRPlugin.InteractionProfile.None;
            _headset = OVRPlugin.SystemHeadset.None;
            _cachedPose = Pose.identity;
            _found = false;
        }

        public Pose GetPointerPose()
        {
            if (_found)
            {
                return _cachedPose;
            }

            TryRetrieveCachedController(_handedness);
            TryRetrieveCachedHeadset();

            switch (_controller)
            {
                case OVRPlugin.InteractionProfile.Touch:
                    _cachedPose = TOUCH_POINTERS[_handIndex]; _found = true; break;
                case OVRPlugin.InteractionProfile.TouchPro:
                    _cachedPose = TOUCHPRO_POINTERS[_handIndex]; _found = true; break;
                case OVRPlugin.InteractionProfile.TouchPlus:
                    _cachedPose = TOUCHPLUS_POINTERS[_handIndex]; _found = true; break;
                case OVRPlugin.InteractionProfile.None:
                    switch (_headset)
                    {
                        case OVRPlugin.SystemHeadset.Rift_DK1:
                        case OVRPlugin.SystemHeadset.Rift_DK2:
                        case OVRPlugin.SystemHeadset.Rift_CB:
                        case OVRPlugin.SystemHeadset.Rift_CV1:
                        case OVRPlugin.SystemHeadset.Rift_S:
                        case OVRPlugin.SystemHeadset.Oculus_Quest:
                        case OVRPlugin.SystemHeadset.Oculus_Link_Quest:
                            _cachedPose = QUEST1_POINTERS[_handIndex]; _found = true; break;
                        //Beyond this point, we will assume the default controller until
                        //we find the real interaction profile.
                        case OVRPlugin.SystemHeadset.Oculus_Quest_2:
                        case OVRPlugin.SystemHeadset.Oculus_Link_Quest_2:
                            _cachedPose = TOUCH_POINTERS[_handIndex]; break;
                        case OVRPlugin.SystemHeadset.Meta_Quest_Pro:
                        case OVRPlugin.SystemHeadset.Meta_Link_Quest_Pro:
                            _cachedPose = TOUCHPRO_POINTERS[_handIndex]; break;
                        case OVRPlugin.SystemHeadset.Meta_Quest_3:
                        case OVRPlugin.SystemHeadset.Meta_Quest_3S:
                        case OVRPlugin.SystemHeadset.Meta_Link_Quest_3:
                        case OVRPlugin.SystemHeadset.Meta_Link_Quest_3S:
                            _cachedPose = TOUCHPLUS_POINTERS[_handIndex]; break;
                        case OVRPlugin.SystemHeadset.None:
                        default:
                            _cachedPose = TOUCHPLUS_POINTERS[_handIndex]; break;
                    }
                    break;
                default:
                    _cachedPose = TOUCHPLUS_POINTERS[_handIndex]; break;
            }
            return _cachedPose;
        }

        private bool TryRetrieveCachedController(OVRPlugin.Hand hand)
        {
            if (_controller == OVRPlugin.InteractionProfile.None)
            {
                _controller = OVRPlugin.GetCurrentInteractionProfile(hand);
                if (_controller == OVRPlugin.InteractionProfile.None
                    && OVRPlugin.IsMultimodalHandsControllersSupported())
                {
                    _controller = OVRPlugin.GetCurrentDetachedInteractionProfile(hand);
                }
            }

            return _controller != OVRPlugin.InteractionProfile.None;
        }


        private bool TryRetrieveCachedHeadset()
        {
            if (_headset == OVRPlugin.SystemHeadset.None)
            {
                _headset = OVRPlugin.GetSystemHeadsetType();
            }

            return _headset != OVRPlugin.SystemHeadset.None;
        }
    }

    [Feature(Feature.Interaction)]
    public class FromOVRControllerDataSource : DataSource<ControllerDataAsset>
    {
        [Header("OVR Data Source")]
        [SerializeField, Interface(typeof(IOVRCameraRigRef))]
        private UnityEngine.Object _cameraRigRef;
        public IOVRCameraRigRef CameraRigRef { get; private set; }

        [SerializeField]
        private bool _processLateUpdates = false;

        [Header("Shared Configuration")]
        [SerializeField]
        private Handedness _handedness;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private UnityEngine.Object _trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer;

        public bool ProcessLateUpdates
        {
            get
            {
                return _processLateUpdates;
            }
            set
            {
                _processLateUpdates = value;
            }
        }

        private readonly ControllerDataAsset _controllerDataAsset = new ControllerDataAsset();
        private OVRInput.Controller _ovrController;
        private ControllerDataSourceConfig _config;

        private OVRPointerPoseSelector _pointerPoseSelector;

        #region OVR Controller Mappings

        // Mappings from Unity XR CommonUsage to Oculus Button/Touch.
        private static readonly IUsage[] ControllerUsageMappings =
        {
            new UsageButtonMapping(ControllerButtonUsage.PrimaryButton, OVRInput.Button.One),
            new UsageTouchMapping(ControllerButtonUsage.PrimaryTouch, OVRInput.Touch.One),
            new UsageButtonMapping(ControllerButtonUsage.SecondaryButton, OVRInput.Button.Two),
            new UsageTouchMapping(ControllerButtonUsage.SecondaryTouch, OVRInput.Touch.Two),
            new UsageButtonMapping(ControllerButtonUsage.GripButton, OVRInput.Button.PrimaryHandTrigger),
            new UsageButtonMapping(ControllerButtonUsage.TriggerButton, OVRInput.Button.PrimaryIndexTrigger),
            new UsageButtonMapping(ControllerButtonUsage.MenuButton, OVRInput.Button.Start),
            new UsageButtonMapping(ControllerButtonUsage.Primary2DAxisClick, OVRInput.Button.PrimaryThumbstick),
            new UsageTouchMapping(ControllerButtonUsage.Primary2DAxisTouch, OVRInput.Touch.PrimaryThumbstick),
            new UsageTouchMapping(ControllerButtonUsage.Thumbrest, OVRInput.Touch.PrimaryThumbRest),

            new UsageAxis1DMapping(ControllerAxis1DUsage.Trigger, OVRInput.Axis1D.PrimaryIndexTrigger),
            new UsageAxis1DMapping(ControllerAxis1DUsage.Grip, OVRInput.Axis1D.PrimaryHandTrigger),

            new UsageAxis2DMapping(ControllerAxis2DUsage.Primary2DAxis, OVRInput.Axis2D.PrimaryThumbstick),
        };

        #endregion

        protected void Awake()
        {
            if (TrackingToWorldTransformer == null)
            {
                TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            }
            if (CameraRigRef == null)
            {
                CameraRigRef = _cameraRigRef as IOVRCameraRigRef;
            }

            UpdateConfig();
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(CameraRigRef, nameof(CameraRigRef));
            this.AssertField(TrackingToWorldTransformer, nameof(TrackingToWorldTransformer));
            if (_handedness == Handedness.Left)
            {
                _ovrController = OVRInput.Controller.LTouch;
            }
            else
            {
                _ovrController = OVRInput.Controller.RTouch;
            }
            _pointerPoseSelector = new OVRPointerPoseSelector(_handedness);

            UpdateConfig();
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied += HandleInputDataDirtied;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied -= HandleInputDataDirtied;
            }

            base.OnDisable();

            MarkInputDataRequiresUpdate();
        }

        private void HandleInputDataDirtied(bool isLateUpdate)
        {
            if (isLateUpdate && !_processLateUpdates)
            {
                return;
            }
            MarkInputDataRequiresUpdate();
        }

        private ControllerDataSourceConfig Config
        {
            get
            {
                if (_config != null)
                {
                    return _config;
                }

                _config = new ControllerDataSourceConfig()
                {
                    Handedness = _handedness
                };

                return _config;
            }
        }

        private void UpdateConfig()
        {
            Config.Handedness = _handedness;
            Config.TrackingToWorldTransformer = TrackingToWorldTransformer;
        }

        protected override void UpdateData()
        {
            _controllerDataAsset.Config = Config;
            _controllerDataAsset.IsDataValid = true;
            _controllerDataAsset.IsConnected =
                (OVRInput.GetConnectedControllers() & _ovrController) > 0;
            if (!_controllerDataAsset.IsConnected || !this.isActiveAndEnabled)
            {
                // revert state fields to their defaults
                _controllerDataAsset.IsConnected = false;
                _controllerDataAsset.IsTracked = default;
                _controllerDataAsset.Input = default;
                _controllerDataAsset.RootPoseOrigin = default;
                return;
            }

            _controllerDataAsset.IsTracked = true;

            OVRInput.Handedness dominantHand = OVRInput.GetDominantHand();
            _controllerDataAsset.IsDominantHand =
                (dominantHand == OVRInput.Handedness.LeftHanded && _handedness == Handedness.Left)
                || (dominantHand == OVRInput.Handedness.RightHanded && _handedness == Handedness.Right);

            // Update button usages
            _controllerDataAsset.Input.Clear();

            OVRInput.Controller controllerMask = _ovrController;
            foreach (IUsage mapping in ControllerUsageMappings)
            {
                mapping.Apply(_controllerDataAsset, controllerMask);
            }

            // Update poses

            // Root pose, in tracking space.
            _controllerDataAsset.RootPose = new Pose(
                OVRInput.GetLocalControllerPosition(_ovrController),
                OVRInput.GetLocalControllerRotation(_ovrController));
            _controllerDataAsset.RootPoseOrigin = PoseOrigin.RawTrackedPose;

            // Convert controller pointer pose from local to tracking space.
            Pose localPointerPose = _pointerPoseSelector.GetPointerPose();
            Matrix4x4 controllerModelToTracking = Matrix4x4.TRS(
                _controllerDataAsset.RootPose.position,
                _controllerDataAsset.RootPose.rotation,
                Vector3.one);
            _controllerDataAsset.PointerPose =
                new Pose(controllerModelToTracking.MultiplyPoint3x4(localPointerPose.position),
                    _controllerDataAsset.RootPose.rotation * localPointerPose.rotation);

            _controllerDataAsset.PointerPoseOrigin = PoseOrigin.RawTrackedPose;
        }

        protected override ControllerDataAsset DataAsset => _controllerDataAsset;

        #region Inject

        public void InjectAllFromOVRControllerDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            Handedness handedness, IOVRCameraRigRef cameraRigRef, ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectHandedness(handedness);
            InjectCameraRigRef(cameraRigRef);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
        }

        [System.Obsolete("Use version with `cameraRigRef` parameter")]
        public void InjectAllFromOVRControllerDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            Handedness handedness, ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectHandedness(handedness);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
        }

        public void InjectHandedness(Handedness handedness)
        {
            _handedness = handedness;
        }

        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            _trackingToWorldTransformer = trackingToWorldTransformer as UnityEngine.Object;
            TrackingToWorldTransformer = trackingToWorldTransformer;
        }

        public void InjectCameraRigRef(IOVRCameraRigRef cameraRigRef)
        {
            _cameraRigRef = cameraRigRef as UnityEngine.Object;
            CameraRigRef = cameraRigRef;
        }
        #endregion
    }
}
