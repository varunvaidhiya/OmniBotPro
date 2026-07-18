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

#if USE_OPENXR
using Oculus.Interaction.Input;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace Oculus.Interaction.UnityXR
{
    public class FromUnityXRControllerDataSource : DataSource<ControllerDataAsset>
    {
        [Header("Shared Configuration")]
        [SerializeField]
        private Handedness _handedness;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private UnityEngine.Object _trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer;

        private static string ControllerActionMap = $@"{{
            ""maps"": [
                {{
                    ""name"": ""XRController"",
                    ""actions"": [
                        {{
                            ""name"": ""PrimaryButton"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/primaryButton""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""PrimaryTouch"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/primaryTouched""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""SecondaryButton"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/secondaryButton""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""SecondaryTouch"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/secondaryTouched""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""GripButton"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/gripPressed""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""TriggerButton"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/triggerPressed""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""TriggerTouch"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/triggerTouched""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""MenuButton"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/menu""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""Primary2DAxisClick"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/thumbstickClicked""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""Primary2DAxisTouch"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/thumbstickTouched""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""Thumbrest"",
                            ""expectedControlLayout"": ""Integer"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/thumbrestTouched""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""Trigger"",
                            ""expectedControlLayout"": ""Axis1D"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/trigger""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""Grip"",
                            ""expectedControlLayout"": ""Axis1D"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/grip""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""Primary2DAxis"",
                            ""expectedControlLayout"": ""Axis2D"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/thumbstick""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""Secondary2DAxis"",
                            ""expectedControlLayout"": ""Axis2D"",
                            ""bindings"": [
                                {{
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""RootPose"",
                            ""expectedControlLayout"": ""Pose"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/devicePose""
                                }}
                            ]
                        }},
                        {{
                            ""name"": ""PointerPose"",
                            ""expectedControlLayout"": ""Pose"",
                            ""bindings"": [
                                {{
                                    ""path"":""<XRController>{{LeftHand}}/pointer""
                                }}
                            ]
                        }}
                    ]
                }}
            ]}}";

        [SerializeField]
        private InputActionMap _leftHandControllerBindings = InputActionMap.FromJson(ControllerActionMap).FirstOrDefault();

        [SerializeField]
        private InputActionMap _rightHandControllerBindings = InputActionMap.FromJson(ControllerActionMap.Replace("{LeftHand}", "{RightHand}")).FirstOrDefault();

        private readonly ControllerDataAsset _dataAsset = new();
        private readonly ControllerDataSourceConfig _config = new();

        private static readonly Quaternion OpenXRToOVRLeftRotTipInverted = Quaternion.Inverse(
              Quaternion.AngleAxis(90, Vector3.forward));
        private static readonly Quaternion OpenXRToOVRRightRotTipInverted = Quaternion.Inverse(
            Quaternion.AngleAxis(180, Vector3.right) * Quaternion.AngleAxis(-90, Vector3.forward));

        private static readonly Func<int> DefaultFrameCountProvider = () => Time.frameCount;
        private Func<int> _frameCountProvider = DefaultFrameCountProvider;
        private int _lastRequiredUpdate;

        public void SetTimeFrameCountProvider(Func<int> frameCountProvider)
        {
            frameCountProvider ??= DefaultFrameCountProvider;
            _frameCountProvider = frameCountProvider;
        }

        private void Awake()
        {
            if (TrackingToWorldTransformer == null)
            {
                TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            }
            UpdateConfig();

            InputActionMap map = (_handedness == Handedness.Left)? _leftHandControllerBindings : _rightHandControllerBindings;

            // TODO - source from meta handaim
            _dataAsset.IsDominantHand = _handedness != Handedness.Left;

            foreach (var usageName in Enum.GetNames(typeof(ControllerButtonUsage)))
            {
                if (usageName == ControllerButtonUsage.None.ToString()) continue;
                var usage = Enum.Parse<ControllerButtonUsage>(usageName);
                map[usageName].started += _ => _dataAsset.Input.SetButton(usage, true);
                map[usageName].canceled += _ => _dataAsset.Input.SetButton(usage, false);
            }
            foreach (var usageName in Enum.GetNames(typeof(ControllerAxis1DUsage)))
            {
                if (usageName == ControllerAxis1DUsage.None.ToString()) continue;
                map[usageName].performed += context =>
                    _dataAsset.Input.SetAxis1D(Enum.Parse<ControllerAxis1DUsage>(usageName), context.ReadValue<float>());
                map[usageName].canceled += context =>
                    _dataAsset.Input.SetAxis1D(Enum.Parse<ControllerAxis1DUsage>(usageName), 0);
            }
            foreach (var usageName in Enum.GetNames(typeof(ControllerAxis2DUsage)))
            {
                if (usageName == ControllerAxis2DUsage.None.ToString()) continue;
                map[usageName].performed += context =>
                    _dataAsset.Input.SetAxis2D(Enum.Parse<ControllerAxis2DUsage>(usageName), context.ReadValue<Vector2>());
                map[usageName].canceled += context =>
                    _dataAsset.Input.SetAxis2D(Enum.Parse<ControllerAxis2DUsage>(usageName), Vector2.zero);
            }
            map[nameof(_dataAsset.RootPose)].performed +=
                context =>
                {
                    var poseState = context.ReadValue<PoseState>();
                    _dataAsset.RootPose = new Pose(poseState.position, poseState.rotation);

                    _dataAsset.RootPose = FlipZ(_dataAsset.RootPose);

                    _dataAsset.RootPose.rotation *= (_dataAsset.Config.Handedness == Handedness.Left)
                        ? OpenXRToOVRLeftRotTipInverted
                        : OpenXRToOVRRightRotTipInverted;

                    _dataAsset.RootPose = FlipZ(_dataAsset.RootPose);


                    _dataAsset.RootPoseOrigin = PoseOrigin.RawTrackedPose;

                    _dataAsset.IsTracked = poseState.trackingState.HasFlag(InputTrackingState.Position) && poseState.trackingState.HasFlag(InputTrackingState.Rotation);
                    _dataAsset.IsDataValid = _dataAsset.IsTracked;
                    _dataAsset.IsConnected = _dataAsset.IsTracked;


                    var frameCount = _frameCountProvider.Invoke();
                    if (_lastRequiredUpdate != frameCount)
                    {
                        _lastRequiredUpdate = frameCount;
                        MarkInputDataRequiresUpdate();
                    }
                };
            map[nameof(_dataAsset.RootPose)].canceled +=
                _ => {
                _dataAsset.RootPoseOrigin = PoseOrigin.None;
            };

            map[nameof(_dataAsset.PointerPose)].performed +=
                context =>
                {
                    var poseState = context.ReadValue<PoseState>();
                    _dataAsset.PointerPose = new Pose(poseState.position, poseState.rotation);
                    _dataAsset.PointerPoseOrigin = PoseOrigin.RawTrackedPose;
                };
            map[nameof(_dataAsset.PointerPose)].canceled += _ =>
            {
                _dataAsset.PointerPoseOrigin = PoseOrigin.None;
            };
            map.Enable();
        }

        private static Quaternion FlipZ(Quaternion q)
        {
            return new Quaternion() { x = -q.x, y = -q.y, z = q.z, w = q.w };
        }

        public static Pose FlipZ(Pose p)
        {
            p.rotation = FlipZ(p.rotation);
            p.position = new Vector3() { x = p.position.x, y = p.position.y, z = -p.position.z };
            return p;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(TrackingToWorldTransformer, nameof(TrackingToWorldTransformer));
            UpdateConfig();
            this.EndStart(ref _started);
        }

        protected override void UpdateData()
        {

        }

        private void UpdateConfig()
        {
            _config.Handedness = _handedness;
            _config.TrackingToWorldTransformer = TrackingToWorldTransformer;
            _dataAsset.Config = _config;
        }

        protected override ControllerDataAsset DataAsset
        {
            get => _dataAsset;
        }

        #region Inject
        public void InjectHandedness(Handedness handedness)
        {
            _handedness = handedness;
        }

        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            _trackingToWorldTransformer = trackingToWorldTransformer as UnityEngine.Object;
            TrackingToWorldTransformer = trackingToWorldTransformer;
        }

        #endregion
    }
}

#endif // USE_OPENXR
