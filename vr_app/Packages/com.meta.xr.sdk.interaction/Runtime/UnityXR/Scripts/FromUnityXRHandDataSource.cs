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

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
#if USE_XR_HANDS
using UnityEngine.XR.Hands;
#endif
using UnityEngine.XR.Management;

namespace Oculus.Interaction.Input.UnityXR
{
    /// <summary>
    ///   <para>Provides hand tracking data to Interaction SDK OpenXR via UnityXR Hands.</para>
    /// </summary>
    public class FromUnityXRHandDataSource : FromOpenXRHandDataSource
    {
#if USE_XR_HANDS
        [Tooltip("The XRHandSubsystem.UpdateType that will be used to drive this " +
            "data source. The Dynamic update type is recommended, see Unity's " +
            "documentation for further details.")]
        [SerializeField]
        private XRHandSubsystem.UpdateType _updateType = XRHandSubsystem.UpdateType.Dynamic;
#endif

        [Header("Shared Configuration")]
        [SerializeField]
        private Handedness _handedness;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private UnityEngine.Object _trackingToWorldTransformer;

        private ITrackingToWorldTransformer TrackingToWorldTransformer;

        private static string _metaAimHandActionMap = @"{
            ""maps"": [
                {
                    ""name"": ""MetaAimHand"",
                    ""actions"": [
                        {
                            ""name"": ""aimFlags"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {
                                    ""path"":""<MetaAimHand>{LeftHand}/aimFlags""
                                }
                            ]
                        },
                        {
                            ""name"": ""pinchStrengthIndex"",
                            ""expectedControlLayout"": ""Axis"",
                            ""bindings"": [
                                {
                                    ""path"":""<MetaAimHand>{LeftHand}/pinchStrengthIndex""
                                }
                            ]
                        },
                        {
                            ""name"": ""pinchStrengthMiddle"",
                            ""expectedControlLayout"": ""Axis"",
                            ""bindings"": [
                                {
                                    ""path"":""<MetaAimHand>{LeftHand}/pinchStrengthMiddle""
                                }
                            ]
                        },
                        {
                            ""name"": ""pinchStrengthRing"",
                            ""expectedControlLayout"": ""Axis"",
                            ""bindings"": [
                                {
                                    ""path"":""<MetaAimHand>{LeftHand}/pinchStrengthRing""
                                }
                            ]
                        },
                        {
                            ""name"": ""pinchStrengthLittle"",
                            ""expectedControlLayout"": ""Axis"",
                            ""bindings"": [
                                {
                                    ""path"":""<MetaAimHand>{LeftHand}/pinchStrengthLittle""
                                }
                            ]
                        },
                        {
                            ""name"": ""devicePosition"",
                            ""expectedControlLayout"": ""Vector3"",
                            ""bindings"": [
                                {
                                    ""path"":""<MetaAimHand>{LeftHand}/devicePosition""
                                }
                            ]
                        },
                        {
                            ""name"": ""deviceRotation"",
                            ""expectedControlLayout"": ""Quaternion"",
                            ""bindings"": [
                                {
                                    ""path"":""<MetaAimHand>{LeftHand}/deviceRotation""
                                }
                            ]
                        }
                    ]
                }
            ]}";

        [SerializeField]
        private InputActionMap _metaAimHandBindingsLeft =
            InputActionMap.FromJson(_metaAimHandActionMap).FirstOrDefault();

        [SerializeField]
        private InputActionMap _metaAimHandBindingsRight = InputActionMap
            .FromJson(_metaAimHandActionMap.Replace("{LeftHand}", "{RightHand}")).FirstOrDefault();

        private InputActionMap MetaAimHandBindings =>
            (_handedness == Handedness.Left) ? _metaAimHandBindingsLeft : _metaAimHandBindingsRight;

        private HandDataSourceConfig _config;
        private InputAction _metaAimFlags;
        private InputAction _pinchStrengthIndex;
        private InputAction _pinchStrengthMiddle;
        private InputAction _pinchStrengthRing;
        private InputAction _pinchStrengthLittle;
        private InputAction _devicePosition;
        private InputAction _deviceRotation;

#if ISDK_OPENXR_HAND
#else
        private readonly OpenXRHandDataAsset _dataAsset = new();
        protected override OpenXRHandDataAsset OpenXRData => _dataAsset;
#endif

        protected override void Awake()
        {
            base.Awake();
            if (TrackingToWorldTransformer == null)
            {
                TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            }
            UpdateConfig();
        }

        protected override void Start()
        {
            base.Start();
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(TrackingToWorldTransformer, nameof(TrackingToWorldTransformer));
#if USE_XR_HANDS
            XRHandSubsystem m_Subsystem =
                XRGeneralSettings.Instance?
                    .Manager?
                    .activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();
            if (m_Subsystem != null)
            {
                m_Subsystem.updatedHands += OnHandUpdate;
                m_Subsystem.trackingLost += OnTrackingLost;
            }
#endif
            UpdateConfig();

            var handBindings = MetaAimHandBindings;
            _metaAimFlags = handBindings["aimFlags"];
            _pinchStrengthIndex = handBindings["pinchStrengthIndex"];
            _pinchStrengthMiddle = handBindings["pinchStrengthMiddle"];
            _pinchStrengthRing = handBindings["pinchStrengthRing"];
            _pinchStrengthLittle = handBindings["pinchStrengthLittle"];
            _devicePosition = handBindings["devicePosition"];
            _deviceRotation = handBindings["deviceRotation"];

            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MetaAimHandBindings.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MetaAimHandBindings.Disable();
        }

        private HandDataSourceConfig Config
        {
            get
            {
                if (_config != null)
                {
                    return _config;
                }

                _config = new HandDataSourceConfig() { Handedness = _handedness };
                return _config;
            }
        }

        private void UpdateConfig()
        {
            Config.TrackingToWorldTransformer = TrackingToWorldTransformer;
            Config.HandSkeleton = (_handedness == Handedness.Left)
                ? HandSkeleton.DefaultLeftSkeleton
                : HandSkeleton.DefaultRightSkeleton;
            _dataAsset.Config = Config;
        }

#if USE_XR_HANDS
        private void OnTrackingLost(XRHand hand)
        {
            if ((hand.handedness == UnityEngine.XR.Hands.Handedness.Left && _handedness != Handedness.Left)
                || (hand.handedness == UnityEngine.XR.Hands.Handedness.Right && _handedness != Handedness.Right))
            {
                return;
            }

            _dataAsset.IsConnected = _dataAsset.IsTracked = _dataAsset.IsDataValid = false;
            MarkInputDataRequiresUpdate();
        }

        private void OnHandUpdate(XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (updateType != _updateType)
            {
                return;
            }

            XRHand hand;
            switch (_handedness)
            {
                case Handedness.Left
                    when (subsystem.updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints)
                          || subsystem.updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose)):
                    hand = subsystem.leftHand;
                    break;
                case Handedness.Right
                    when (subsystem.updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandJoints)
                          || subsystem.updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose))
                    :
                    hand = subsystem.rightHand;
                    break;
                default:
                    return;
            }

            _dataAsset.IsDataValid = subsystem.running && hand.isTracked;
            _dataAsset.IsConnected = subsystem.running;
            _dataAsset.IsTracked = hand.isTracked;

            // XR_EXT_hand_tracking
            _dataAsset.Root = hand.rootPose;
            _dataAsset.RootPoseOrigin = PoseOrigin.RawTrackedPose;

#if ISDK_OPENXR_HAND
            _dataAsset.IsHighConfidence = true;
            for (var i = 0; i < Constants.NUM_FINGERS; i++)
            {
                _dataAsset.IsFingerHighConfidence[i] = true;
            }
#endif
            for (var i = XRHandJointID.BeginMarker.ToIndex();
                 i < XRHandJointID.EndMarker.ToIndex();
                 i++)
            {
                var jointID = XRHandJointIDUtility.FromIndex(i);
                var trackingData = hand.GetJoint(jointID);
#if ISDK_OPENXR_HAND
                int jointIndex = jointID switch
                {
                    XRHandJointID.Palm => (int)HandJointId.HandPalm,
                    XRHandJointID.Wrist => (int)HandJointId.HandWristRoot,
                    _ => i
                };
                if (!trackingData.trackingState.HasFlag(XRHandJointTrackingState.Pose))
                {
                    _dataAsset.IsHighConfidence = false;
                    if (FingersMetadata.JOINT_TO_FINGER_INDEX[jointIndex] > 0)
                    {
                        _dataAsset.IsFingerHighConfidence[FingersMetadata.JOINT_TO_FINGER_INDEX[jointIndex]] = false;
                    }
                }
                if (trackingData.TryGetPose(out var pose))
                {
                    _dataAsset.JointPoses[jointIndex] = PoseUtils.Delta(_dataAsset.Root, pose);
                }
                if (trackingData.TryGetRadius(out var radius))
                {
                    _dataAsset.JointRadii[jointIndex] = radius;
                }
#else
                _dataAsset.JointStates[i] = (OpenXRHandDataAsset.JointTrackingState)trackingData.trackingState;
                if (trackingData.TryGetPose(out var pose))
                {
                    _dataAsset.JointPoses[i] = pose;
                }

                if (trackingData.TryGetRadius(out var radius))
                {
                    _dataAsset.JointRadiuses[i] = radius;
                }

                if (trackingData.TryGetAngularVelocity(out var angularVelocity))
                {
                    _dataAsset.JointAngularVelocities[i] = angularVelocity;
                }

                if (trackingData.TryGetLinearVelocity(out var linearVelocity))
                {
                    _dataAsset.JointLinearVelocities[i] = linearVelocity;
                }
#endif
            }

            // XR_FB_hand_tracking_aim
#if ISDK_OPENXR_HAND
            MetaAimFlags aimFlags = (MetaAimFlags)_metaAimFlags.ReadValue<int>();
            _shouldMockHandTrackingAim = _dataAsset.IsDataValidAndConnected && aimFlags == MetaAimFlags.None;
            _dataAsset.IsDominantHand = aimFlags.HasFlag(MetaAimFlags.DominantHand);
            _dataAsset.IsFingerPinching[(int)HandFinger.Index] = aimFlags.HasFlag(MetaAimFlags.IndexPinching);
            _dataAsset.IsFingerPinching[(int)HandFinger.Middle] = aimFlags.HasFlag(MetaAimFlags.MiddlePinching);
            _dataAsset.IsFingerPinching[(int)HandFinger.Ring] = aimFlags.HasFlag(MetaAimFlags.RingPinching);
            _dataAsset.IsFingerPinching[(int)HandFinger.Pinky] = aimFlags.HasFlag(MetaAimFlags.LittlePinching);
#else
            _dataAsset.AimFlags = (OpenXRHandDataAsset.AimFlagsFB)_metaAimFlags.ReadValue<int>();
#endif

            _dataAsset.FingerPinchStrength[(int)HandFinger.Index] = _pinchStrengthIndex.ReadValue<float>();
            _dataAsset.FingerPinchStrength[(int)HandFinger.Middle] = _pinchStrengthMiddle.ReadValue<float>();
            _dataAsset.FingerPinchStrength[(int)HandFinger.Ring] = _pinchStrengthRing.ReadValue<float>();
            _dataAsset.FingerPinchStrength[(int)HandFinger.Pinky] = _pinchStrengthLittle.ReadValue<float>();

            _dataAsset.PointerPose.position = _devicePosition.ReadValue<Vector3>();
            _dataAsset.PointerPose.rotation = _deviceRotation.ReadValue<Quaternion>();
            _dataAsset.PointerPoseOrigin = PoseOrigin.RawTrackedPose;

            // Notify update
            if (subsystem.updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) ||
                subsystem.updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandJoints))
            {
                MarkInputDataRequiresUpdate();
            }
        }
#endif

        #region Inject

        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            _trackingToWorldTransformer = trackingToWorldTransformer as UnityEngine.Object;
            TrackingToWorldTransformer = trackingToWorldTransformer;
            UpdateConfig();
        }
        #endregion
    }
}
