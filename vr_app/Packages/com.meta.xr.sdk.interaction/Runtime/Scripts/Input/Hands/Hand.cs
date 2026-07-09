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

using System;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    // A top level component that provides hand pose data, pinch states, and more.
    // Rather than sourcing data directly from the runtime layer, provides one
    // level of abstraction so that the aforementioned data can be injected
    // from other sources.
    public class Hand : DataModifier<HandDataAsset>, IHand
    {
        /// <summary>
        /// Implementation of <see cref="IHand.Handedness"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Handedness Handedness => GetData().Config.Handedness;

        /// <summary>
        /// Returns an <see cref="ITrackingToWorldTransformer"/> for the tracking space associated with this hand, capable
        /// of transforming tracking data from tracking space to world space.
        /// </summary>
        public ITrackingToWorldTransformer TrackingToWorldTransformer =>
            GetData().Config.TrackingToWorldTransformer;

        /// <summary>
        /// Returns the <see cref="HandSkeleton"/> containing the most recent tracking data from the system for the represented
        /// tracked hand.
        /// </summary>
        public HandSkeleton HandSkeleton => GetData().Config.HandSkeleton;

        private HandJointCache _jointPosesCache;

        /// <summary>
        /// Implementation of <see cref="IHand.WhenHandUpdated"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action WhenHandUpdated = delegate { };

        /// <summary>
        /// Implementation of <see cref="IHand.IsConnected"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool IsConnected => GetData().IsDataValidAndConnected;

        /// <summary>
        /// Implementation of <see cref="IHand.IsHighConfidence"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool IsHighConfidence => GetData().IsHighConfidence;

        /// <summary>
        /// Implementation of <see cref="IHand.IsDominantHand"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool IsDominantHand => GetData().IsDominantHand;

        /// <summary>
        /// Implementation of <see cref="IHand.Scale"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public float Scale => (TrackingToWorldTransformer != null
                               ? TrackingToWorldTransformer.Transform.lossyScale.x
                               : 1) * GetData().HandScale;

        private static readonly Vector3 PALM_LOCAL_OFFSET = new Vector3(0.08f, -0.01f, 0.0f);

        protected override void Apply(HandDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }

        /// <summary>
        /// Implementation of <see cref="DataSource{TData}.MarkInputDataRequiresUpdate"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                InitializeJointPosesCache();
                WhenHandUpdated.Invoke();
            }
        }

        private void InitializeJointPosesCache()
        {
            if (_jointPosesCache == null && GetData().IsDataValidAndConnected)
            {
#if ISDK_OPENXR_HAND
                _jointPosesCache = new HandJointCache();
#else
                _jointPosesCache = new HandJointCache(HandSkeleton);
#endif
            }
        }

        private void CheckJointPosesCacheUpdate()
        {
            if (_jointPosesCache != null
                && CurrentDataVersion != _jointPosesCache.LocalDataVersion)
            {
#if ISDK_OPENXR_HAND
                _jointPosesCache.Update(GetData(), CurrentDataVersion,
                    TrackingToWorldTransformer?.Transform);
#else
                _jointPosesCache.Update(GetData(), CurrentDataVersion);
#endif
            }
        }

        #region IHandState implementation

        /// <summary>
        /// Implementation of <see cref="IHand.GetFingerIsPinching(HandFinger)"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool GetFingerIsPinching(HandFinger finger)
        {
            HandDataAsset currentData = GetData();
            return currentData.IsConnected && currentData.IsFingerPinching[(int)finger];
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetIndexFingerIsPinching"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool GetIndexFingerIsPinching()
        {
            return GetFingerIsPinching(HandFinger.Index);
        }

        /// <summary>
        /// Implementation of <see cref="IHand.IsPointerPoseValid"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool IsPointerPoseValid => IsPoseOriginAllowed(GetData().PointerPoseOrigin);

        /// <summary>
        /// Implementation of <see cref="IHand.GetPointerPose(out Pose)"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool GetPointerPose(out Pose pose)
        {
            HandDataAsset currentData = GetData();
            return ValidatePose(currentData.PointerPose, currentData.PointerPoseOrigin,
                out pose);
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetJointPose(HandJointId, out Pose)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public bool GetJointPose(HandJointId handJointId, out Pose pose)
        {
            pose = Pose.identity;

            if (!IsTrackedDataValid
                || _jointPosesCache == null
                || !GetRootPose(out Pose rootPose))
            {
                return false;
            }
            CheckJointPosesCacheUpdate();
#if ISDK_OPENXR_HAND
            pose = _jointPosesCache.GetWorldJointPose(handJointId);
#else
            pose = _jointPosesCache.WorldJointPose(handJointId, rootPose, Scale);
#endif
            return true;
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetJointPoseLocal(HandJointId, out Pose)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public bool GetJointPoseLocal(HandJointId handJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses))
            {
                return false;
            }

            pose = localJointPoses[(int)handJointId];
            return true;
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetJointPosesLocal(out ReadOnlyHandJointPoses)"/>; for details, please refer to the
        /// related documentation provided for that interface.
        /// </summary>
        public bool GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses)
        {
            if (!IsTrackedDataValid || _jointPosesCache == null)
            {
                localJointPoses = ReadOnlyHandJointPoses.Empty;
                return false;
            }
            CheckJointPosesCacheUpdate();
            return _jointPosesCache.GetAllLocalPoses(out localJointPoses);
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetJointPoseFromWrist(HandJointId, out Pose)"/>; for details, please refer to the
        /// related documentation provided for that interface.
        /// </summary>
        public bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist))
            {
                return false;
            }

            pose = jointPosesFromWrist[(int)handJointId];
            return true;
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses)"/>; for details, please refer to
        /// the related documentation provided for that interface.
        /// </summary>
        public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist)
        {
            if (!IsTrackedDataValid || _jointPosesCache == null)
            {
                jointPosesFromWrist = ReadOnlyHandJointPoses.Empty;
                return false;
            }
            CheckJointPosesCacheUpdate();
            return _jointPosesCache.GetAllPosesFromWrist(out jointPosesFromWrist);
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetPalmPoseLocal(out Pose)"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool GetPalmPoseLocal(out Pose pose)
        {
            Quaternion rotationQuat = Quaternion.identity;
            Vector3 offset = PALM_LOCAL_OFFSET;
            if (Handedness == Handedness.Left)
            {
                offset = -offset;
            }
            pose = new Pose(offset * Scale, rotationQuat);
            return true;
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetFingerIsHighConfidence(HandFinger)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public bool GetFingerIsHighConfidence(HandFinger finger)
        {
            return GetData().IsFingerHighConfidence[(int)finger];
        }

        /// <summary>
        /// Implementation of <see cref="IHand.GetFingerPinchStrength(HandFinger)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public float GetFingerPinchStrength(HandFinger finger)
        {
            return GetData().FingerPinchStrength[(int)finger];
        }

        /// <summary>
        /// Implementation of <see cref="IHand.IsTrackedDataValid"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool IsTrackedDataValid => IsPoseOriginAllowed(GetData().RootPoseOrigin);

        /// <summary>
        /// Implementation of <see cref="IHand.GetRootPose(out Pose)"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool GetRootPose(out Pose pose)
        {
            HandDataAsset currentData = GetData();
            return ValidatePose(currentData.Root, currentData.RootPoseOrigin, out pose);
        }

        #endregion


        private bool ValidatePose(in Pose sourcePose, PoseOrigin sourcePoseOrigin, out Pose pose)
        {
            if (IsPoseOriginDisallowed(sourcePoseOrigin))
            {
                pose = Pose.identity;
                return false;
            }

            pose = TrackingToWorldTransformer != null
                ? TrackingToWorldTransformer.ToWorldPose(sourcePose)
                : sourcePose;

            return true;
        }

        private bool IsPoseOriginAllowed(PoseOrigin poseOrigin)
        {
            return poseOrigin != PoseOrigin.None;
        }

        private bool IsPoseOriginDisallowed(PoseOrigin poseOrigin)
        {
            return poseOrigin == PoseOrigin.None;
        }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated Hand; because the only required dependencies
        /// are those of the underlying <see cref="DataModifier{TData}"/>, this simply wraps
        /// <see cref="DataModifier{TData}.InjectAllDataModifier(DataSource{TData}.UpdateModeFlags, IDataSource)"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity
        /// Editor-based usage.
        /// </summary>
        public void InjectAllHand(UpdateModeFlags updateMode, IDataSource updateAfter,
            DataModifier<HandDataAsset> modifyDataFromSource, bool applyModifier)
        {
            base.InjectAllDataModifier(updateMode, updateAfter, modifyDataFromSource, applyModifier);
        }

        #endregion
    }
}
