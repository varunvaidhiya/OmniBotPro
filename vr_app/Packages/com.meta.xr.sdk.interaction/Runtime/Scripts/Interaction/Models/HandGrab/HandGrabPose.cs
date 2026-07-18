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

using Oculus.Interaction.Grab;
using Oculus.Interaction.Grab.GrabSurfaces;
using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The HandGrabPose defines the local point in an object to which the grip point of a <see cref="HandGrabInteractor"/>
    /// should align. It can also contain information about the final pose of the hand for perfect alignment as well as a
    /// surface that indicates the valid positions for the point.
    /// </summary>
    public class HandGrabPose : MonoBehaviour
    {
        internal enum OVROffsetMode
        {
            /// <summary>
            /// Pose was not upgraded to OpenXR, and offset will not be applied
            /// </summary>
            None,

            /// <summary>
            /// Pose has been upgraded to OpenXR, and an offset will be applied
            /// to the transform rotation for compatibility.
            /// </summary>
            Apply,

            /// <summary>
            /// Pose was upgraded to OpenXR but the offset will be ignored.
            /// </summary>
            Ignore,
        }

        private static readonly Pose OVR_OFFSET_LH = new Pose(Vector3.zero, Quaternion.Euler(0, 90, 180));
        private static readonly Pose OVR_OFFSET_RH = new Pose(Vector3.zero, Quaternion.Euler(0, 90, 0));

        [SerializeField, Optional, Interface(typeof(IGrabSurface))]
        private UnityEngine.Object _surface = null;
        private IGrabSurface _snapSurface;
        /// <summary>
        /// The <see cref="IGrabSurface"/> containing information to help hand visuals pose themselves when grabbing.
        /// </summary>
        /// <remarks>
        /// This property is primarily used by internal HandGrabPose calculations and by Editor-based pose authoring
        /// tools.
        /// </remarks>
        public IGrabSurface SnapSurface
        {
            get => _snapSurface ?? _surface as IGrabSurface;
            private set
            {
                _snapSurface = value;
            }
        }

        [SerializeField]
        [Tooltip("Transform used as a reference to measure the local data of the HandGrabPose")]
        private Transform _relativeTo;

        [SerializeField]
        private bool _usesHandPose = true;

        [SerializeField, Optional]
        [HideInInspector]
        [InspectorName("Hand Pose")]
        private HandPose _handPose = new HandPose();

        [SerializeField, Optional]
        [HideInInspector]
        private HandPose _targetHandPose = new HandPose();

        [SerializeField]
        [HideInInspector]
        private HandGhostProvider _ghostProvider;

        [SerializeField]
        [HideInInspector]
        private HandGhostProvider _handGhostProvider;

        [SerializeField]
        [HideInInspector]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        private OVROffsetMode _ovrOffsetMode = OVROffsetMode.None;
#pragma warning restore CS0414

#if !ISDK_OPENXR_HAND
        private bool _ovrOffsetAppliedToTransform = false;
        private bool ShouldApplyOVROffset =>
            _ovrOffsetMode == OVROffsetMode.Apply &&
            !_ovrOffsetAppliedToTransform;
#endif

        /// <summary>
        /// The <see cref="HandGrab.HandPose"/> a hand visual should adopt when grabbing an interactable
        /// equipped with this HandGrabPose.
        /// </summary>
        public HandPose HandPose
        {
#if ISDK_OPENXR_HAND
            get => _usesHandPose ? _targetHandPose : null;
#else
            get => _usesHandPose ? _handPose : null;
#endif
        }

        internal static Pose GetOVROffset(Handedness handedness)
        {
            return handedness == Handedness.Left ? OVR_OFFSET_LH : OVR_OFFSET_RH;
        }

        /// <summary>
        /// Returns the current scale of the HandGrabPose relative to its reference transform.
        /// </summary>
        /// <remarks>
        /// This value is estimated based the <see cref="RelativeTo"/> transform's scale in its local X dimension;
        /// this estimate is not guaranteed to be accurate, particularly under arbitrary rotations and skewing
        /// scales.
        /// </remarks>
        public float RelativeScale
        {
            get
            {
                return this.transform.lossyScale.x / _relativeTo.lossyScale.x;
            }
        }

        /// <summary>
        /// Pose of the HandGrabPose relative to its reference transform.
        /// </summary>
        public Pose RelativePose
        {
            get
            {
                if (_relativeTo != null)
                {
                    return PoseUtils.DeltaScaled(_relativeTo, WorldPose);
                }
                else
                {
                    return LocalPose;
                }
            }
        }

        /// <summary>
        /// Reference transform of the HandGrabPose. This is the transform relative to which many fundamental
        /// calculations are performed.
        /// </summary>
        public Transform RelativeTo => _relativeTo;

        private Pose LocalPose
        {
            get
            {
                Pose result = transform.GetPose(Space.Self);
#if !ISDK_OPENXR_HAND
                if (ShouldApplyOVROffset)
                {
                    result.Premultiply(GetOVROffset(HandPose.Handedness));
                }
#endif
                return result;
            }
        }

        private Pose WorldPose
        {
            get
            {
                Pose result = transform.GetPose(Space.World);
#if !ISDK_OPENXR_HAND
                if (ShouldApplyOVROffset)
                {
                    result.Premultiply(GetOVROffset(HandPose.Handedness));
                }
#endif
                return result;
            }
        }

        protected virtual void Awake()
        {
#if !ISDK_OPENXR_HAND
            if (ShouldApplyOVROffset)
            {
                Matrix4x4 unmodified = transform.localToWorldMatrix;
                Pose localPose = transform.GetPose(Space.Self);
                localPose.Premultiply(GetOVROffset(HandPose.Handedness));
                transform.SetPose(localPose, Space.Self);
                _ovrOffsetAppliedToTransform = true;

                foreach (Transform child in transform)
                {
                    // Restore children to previous pose
                    child?.SetPositionAndRotation(
                        unmodified.MultiplyPoint(child.localPosition),
                        unmodified.rotation * child.localRotation);
                }
            }
#endif
        }

        #region editor events
        protected virtual void Reset()
        {
            _relativeTo = this.GetComponentInParent<IRelativeToRef>()?.RelativeTo;
        }
        #endregion

        /// <summary>
        /// Checks whether this instance uses hand poses.
        /// </summary>
        /// <returns>True if <see cref="HandPose"/> should return a valid <see cref="HandGrab.HandPose"/>, false otherwise</returns>
        public bool UsesHandPose()
        {
            return _usesHandPose;
        }

        /// <summary>
        /// Obsolete implementation; use
        /// <see cref="CalculateBestPose(in Pose, in Pose, Transform, Handedness, PoseMeasureParameters, ref HandGrabResult)"/>
        /// instead.
        /// </summary>
        /// <param name="userPose">The current pose of the user</param>
        /// <param name="handedness">The handedness of the grab</param>
        /// <param name="scoringModifier"><see cref="PoseMeasureParameters"/> for score modification</param>
        /// <param name="relativeTo">The relative transform</param>
        /// <param name="result">Out parameter for the calculated pose</param>
        /// <returns>True always (this obsolete method always returns a valid result)</returns>
        [Obsolete("Use " + nameof(CalculateBestPose) + " with offset instead")]
        public virtual bool CalculateBestPose(Pose userPose,
            Handedness handedness, PoseMeasureParameters scoringModifier,
            Transform relativeTo, ref HandGrabResult result)
        {
            CalculateBestPose(userPose, Pose.identity, relativeTo, handedness, scoringModifier, ref result);
            return true;
        }

        /// <summary>
        /// Finds the most similar pose to the provided pose.
        /// If the HandGrabPose contains a surface it will defer the calculation to it.
        /// </summary>
        /// <param name="userPose">The current pose of the user</param>
        /// <param name="offset">The offset from the root, for accurate scoring</param>
        /// <param name="relativeTo">The relative transform</param>
        /// <param name="handedness">The handedness of the grab</param>
        /// <param name="scoringModifier"><see cref="PoseMeasureParameters"/> for score modification</param>
        /// <param name="result">Out parameter for the calculated pose</param>
        public virtual void CalculateBestPose(in Pose userPose, in Pose offset, Transform relativeTo,
            Handedness handedness, PoseMeasureParameters scoringModifier,
            ref HandGrabResult result)
        {
            result.HasHandPose = false;

            result.Score = CompareNearPoses(userPose, offset, relativeTo,
                scoringModifier, out Pose worldPose);
            result.RelativePose = PoseUtils.Delta(relativeTo, worldPose);
            if (HandPose != null)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(HandPose);
            }
        }

        /// <summary>
        /// Finds the most similar pose to the provided pose.
        /// If the HandGrabPose contains a surface it will defer the calculation to it.
        /// </summary>
        /// <param name="worldPoint">The desired pose in world space</param>
        /// <param name="scoringModifier">How much to weight the translational or rotational distance</returns>
        /// <param name="relativeTo">Reference transform used to measure the local parameters</param>
        /// <param name="bestWorldPose">Best pose available that is near the desired one</param>
        /// <returns>The score from the desired worldPoint to the result BestWorldPose</returns>
        private GrabPoseScore CompareNearPoses(in Pose worldPoint, in Pose offset,
            Transform relativeTo, PoseMeasureParameters scoringModifier, out Pose bestWorldPose)
        {
            GrabPoseScore bestScore;
            if (SnapSurface != null)
            {
                bestScore = SnapSurface.CalculateBestPoseAtSurface(worldPoint, offset, out bestWorldPose, scoringModifier, relativeTo);
            }
            else
            {
                bestWorldPose = PoseUtils.GlobalPoseScaled(relativeTo, this.RelativePose);
                bestScore = new GrabPoseScore(worldPoint, bestWorldPose, offset, scoringModifier);
            }

            return bestScore;
        }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated HandGrabPose; effectively wraps
        /// <see cref="InjectRelativeTo(Transform)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHandGrabPose(Transform relativeTo)
        {
            InjectRelativeTo(relativeTo);
        }

        /// <summary>
        /// Sets the <see cref="RelativeTo"/> transform for a dynamically instantiated HandGrabPose. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }

        /// <summary>
        /// Sets the <see cref="IGrabSurface"/> for a dynamically instantiated HandGrabPose. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalSurface(IGrabSurface surface)
        {
            _surface = surface as UnityEngine.Object;
            SnapSurface = surface;
        }

        /// <summary>
        /// Sets the <see cref="HandPose"/> for a dynamically instantiated HandGrabPose. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalHandPose(HandPose handPose)
        {
#if ISDK_OPENXR_HAND
            _targetHandPose = handPose;
            _usesHandPose = _targetHandPose != null;
#else
            _handPose = handPose;
            _usesHandPose = _handPose != null;
#endif
        }

        #endregion

    }
}
