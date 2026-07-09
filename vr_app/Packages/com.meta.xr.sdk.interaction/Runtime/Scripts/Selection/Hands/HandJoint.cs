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

using Oculus.Interaction.Input;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Interaction
{
    /// <summary>
    /// Standalone MonoBehavior representation of a tracked hand joint within an <see cref="IHand"/>. This a low-level type used
    /// primarily in core Interaction SDK operations such as detecting grabs and pinches. While various values in this type can be
    /// modified publicly, doing so is something only experts should consider trying.
    /// </summary>
    public class HandJoint : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// The <see cref="IHand"/> to which this joint belongs. This property is populated during MonoBehaviour start-up with the
        /// value of the `_hand` field, which is set through the Unity Editor.
        /// </summary>
        public IHand Hand { get; private set; }

        #region OVR Fields

        [SerializeField]
        private HandJointId _handJointId;

        [SerializeField]
        [InspectorName("Offset")]
        private Vector3 _localPositionOffset;

        [SerializeField]
        [InspectorName("Rotation")]
        private Quaternion _rotationOffset = Quaternion.identity;

        #endregion OVR Fields

        #region OpenXR Fields

        [SerializeField]
        private HandJointId _jointId;

        [SerializeField]
        [InspectorName("Offset")]
        private Vector3 _posOffset;

        [SerializeField]
        [InspectorName("Rotation")]
        private Quaternion _rotOffset = Quaternion.identity;

        [Tooltip("Provided for backwards compatibility. When set, the rotation of the driven " +
            "transform for this component will match the legacy hand skeleton joint orientation " +
            "rather than the current OpenXR joint orientation.")]
        [SerializeField]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        private bool _useLegacyOrientation = false;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        #endregion OpenXR Fields

#if ISDK_OPENXR_HAND
        /// <summary>
        /// Provided for backwards compatibility. When set, the rotation of the driven
        /// transform for this component will match the legacy hand skeleton joint orientation
        /// rather than the current OpenXR joint orientation.
        /// </summary>
        [Obsolete("This property is provided for backwards compatibility only, and its " +
            "function will be removed in a future version of Interaction SDK.")]
        public bool UseLegacyOrientation
        {
            get => _useLegacyOrientation;
            set => _useLegacyOrientation = value;
        }

        private static readonly Vector3 LEFT_LEGACY_ROT = new Vector3(180, 90, 0);
        private static readonly Vector3 RIGHT_LEGACY_ROT = new Vector3(0, -90, 0);
#endif

        [SerializeField]
        [Tooltip("When the attached hand's handedness is set to Left, this property will mirror the offsets. " +
            "This allows for offset values to be set in Right hand coordinates for both Left and Right hands.")]
        private bool _mirrorOffsetsForLeftHand = true;

        #region OpenXR Fields

#pragma warning disable CS0414 // Field is assigned but its value is never used
        [Header("Freeze rotations")]
        [SerializeField]
        private bool _freezeRotationX = false;

        [SerializeField]
        private bool _freezeRotationY = false;

        [SerializeField]
        private bool _freezeRotationZ = false;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        #endregion OpenXR Fields

#if ISDK_OPENXR_HAND
        public bool FreezeRotationX
        {
            get => _freezeRotationX;
            set => _freezeRotationX = value;
        }
        public bool FreezeRotationY
        {
            get => _freezeRotationY;
            set => _freezeRotationY = value;
        }
        public bool FreezeRotationZ
        {
            get => _freezeRotationZ;
            set => _freezeRotationZ = value;
        }
#endif

        /// <summary>
        /// When the attached <see cref="IHand"/>'s handedness is set to Left, this property will mirror the offsets.
        /// This allows for offset values to be set in Right hand coordinates for both Left and Right hands.
        /// </summary>
        public bool MirrorOffsetsForLeftHand
        {
            get => _mirrorOffsetsForLeftHand;
            set => _mirrorOffsetsForLeftHand = value;
        }

        #region Properties
        /// <summary>
        /// Gets and sets the <see cref="HandJointId"/> for this joint. This value is useful when mapping HandJoints to
        /// visualizations or other logic which must distinguish between the various tracked joints.
        /// </summary>
        public HandJointId HandJointId
        {
#if ISDK_OPENXR_HAND
            get => _jointId;
            set => _jointId = value;
#else
            get => _handJointId;
            set => _handJointId = value;
#endif
        }

        /// <summary>
        /// Gets and sets the position offset associated with this HandJoint. This is a low-level value which characterizes
        /// the expected shapes of certain tracked hand features (for example, phalanges, a.k.a. finger bones) according to the
        /// tracking system's hand model. These values can be set to fundamentally change the shape produced by hand tracking data,
        /// but this is something only experts should consider trying.
        /// </summary>
        public Vector3 LocalPositionOffset
        {
#if ISDK_OPENXR_HAND
            get => _posOffset;
            set => _posOffset = value;
#else
            get => _localPositionOffset;
            set => _localPositionOffset = value;
#endif
        }

        /// <summary>
        /// Gets and sets the rotation offset associated with this HandJoint. This is a low-level value which characterizes
        /// the expected relative orientations of certain tracked hand features (for example, phalanges, a.k.a. finger bones)
        /// when compared to the tracking data of a given hand tracking system. These values can be set to fundamentally change
        /// the shape produced by hand tracking data, but this is something only experts should consider trying.
        /// </summary>
        public Quaternion RotationOffset
        {
#if ISDK_OPENXR_HAND
            get => _rotOffset;
            set => _rotOffset = value;
#else
            get => _rotationOffset;
            set => _rotationOffset = value;
#endif
        }

        #endregion

        private Pose _cachedPose = Pose.identity;

        protected bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandUpdated;
                if (Hand.IsConnected)
                {
                    HandleHandUpdated();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;
            }
        }

        private void HandleHandUpdated()
        {
            if (Hand.GetJointPose(HandJointId, out Pose pose))
            {
                GetOffset(ref _cachedPose, Hand.Handedness, Hand.Scale);
#if ISDK_OPENXR_HAND
                _cachedPose.Postmultiply(pose);
#pragma warning disable 618
                if (UseLegacyOrientation)
                {
                    _cachedPose.rotation = pose.rotation *
                         (Hand.Handedness == Handedness.Left ?
                             Quaternion.Euler(LEFT_LEGACY_ROT) :
                             Quaternion.Euler(RIGHT_LEGACY_ROT));
                }
#pragma warning restore 618
#else
                //Note that RotationOffset should be on the right of pose.rotation in order to be applied locally.
                //having it pre-multiplying can yield unwanted results.
                _cachedPose.position = pose.position + RotationOffset * pose.rotation * _cachedPose.position;
                _cachedPose.rotation = pose.rotation;
#endif

#if ISDK_OPENXR_HAND
                _cachedPose.rotation = FreezeRotation(_cachedPose.rotation);
#endif

                transform.SetPose(_cachedPose);
            }
        }

#if ISDK_OPENXR_HAND
        private Quaternion FreezeRotation(Quaternion rotation)
        {
            if (_freezeRotationX
                || _freezeRotationY
                || _freezeRotationZ)
            {
                Vector3 eulerAngles = rotation.eulerAngles;
                Quaternion pitch = Quaternion.Euler(new Vector3(eulerAngles.x, 0.0f, 0.0f));
                Quaternion yaw = Quaternion.Euler(new Vector3(0.0f, eulerAngles.y, 0.0f));
                Quaternion roll = Quaternion.Euler(new Vector3(0.0f, 0.0f, eulerAngles.z));
                Quaternion finalSourceRotation = Quaternion.identity;

                if (!_freezeRotationY)
                {
                    finalSourceRotation *= yaw;
                }
                if (!_freezeRotationX)
                {
                    finalSourceRotation *= pitch;
                }
                if (!_freezeRotationZ)
                {
                    finalSourceRotation *= roll;
                }
                rotation = finalSourceRotation;
            }
            return rotation;
        }
#endif

        private void GetOffset(ref Pose pose, Handedness handedness, float scale)
        {
            if (_mirrorOffsetsForLeftHand && handedness == Handedness.Left)
            {
                pose.position = HandMirroring.Mirror(LocalPositionOffset * scale);
                pose.rotation = HandMirroring.Mirror(RotationOffset);
            }
            else
            {
                pose.position = LocalPositionOffset * scale;
                pose.rotation = RotationOffset;
            }
        }

        #region Inject
        /// <summary>
        /// Convenience method for injecting all requirements to a dynamically instantiated HandJoint. Because the only requirement
        /// of HandJoint is an <see cref="IHand"/>, this simply wraps <see cref="InjectHand(IHand)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHandJoint(IHand hand)
        {
            InjectHand(hand);
        }

        /// <summary>
        /// Adds an <see cref="IHand"/> to a dynamically instantiated HandJoint. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }
        #endregion;
    }
}
