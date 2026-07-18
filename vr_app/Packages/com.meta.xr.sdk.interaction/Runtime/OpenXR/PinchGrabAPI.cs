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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.GrabAPI
{
    /// <summary>
    /// This Finger API uses an advanced calculation for the pinch value of the fingers
    /// to detect if they are grabbing
    /// </summary>
    public class PinchGrabAPI : IFingerAPI
    {
        private bool _isPinchVisibilityGood;
        private float DistanceStart => _isPinchVisibilityGood ? PINCH_HQ_DISTANCE_START : PINCH_DISTANCE_START;
        private float DistanceStopMax => _isPinchVisibilityGood ? PINCH_HQ_DISTANCE_STOP_MAX : PINCH_DISTANCE_STOP_MAX;
        private float DistanceStopOffset => _isPinchVisibilityGood ? PINCH_HQ_DISTANCE_STOP_OFFSET : PINCH_DISTANCE_STOP_OFFSET;

        private const float PINCH_DISTANCE_START = 0.02f;
        private const float PINCH_DISTANCE_STOP_MAX = 0.1f;
        private const float PINCH_DISTANCE_STOP_OFFSET = 0.04f;

        private const float PINCH_HQ_DISTANCE_START = 0.016f;
        private const float PINCH_HQ_DISTANCE_STOP_MAX = 0.1f;
        private const float PINCH_HQ_DISTANCE_STOP_OFFSET = 0.016f;

        private const float THUMB_DISTANCE_START = 0.03f;
        private const float THUMB_DISTANCE_STOP_MAX = 0.05f;
        private const float THUMB_DISTANCE_STOP_OFFSET = 0.04f;
        private const float THUMB_MAX_DOT = 0.5f;

        private const float PINCH_HQ_VIEW_ANGLE_THRESHOLD = 40f;

        private readonly HandJointId[] THUMB_JOINTS_SELECT = new[]
        {
            HandJointId.HandThumb3,
            HandJointId.HandThumbTip
        };

        private readonly HandJointId[] THUMB_JOINTS_MAINTAIN = new[]
        {
            HandJointId.HandThumb2,
            HandJointId.HandThumb3,
            HandJointId.HandThumbTip
        };

        private readonly HandJointId[] INDEX_JOINTS = new[]
        {
            HandJointId.HandIndex1,
            HandJointId.HandIndex2,
            HandJointId.HandIndex3,
            HandJointId.HandIndexTip,
        };

        private class FingerPinchData
        {
            private readonly HandJointId _tipId;
            private float _minPinchDistance;

            public Vector3 TipPosition { get; private set; }
            public bool IsPinchingChanged { get; private set; }
            public float PinchStrength;
            public bool IsPinching;

            public FingerPinchData(HandFinger fingerId)
            {
                _tipId = HandJointUtils.GetHandFingerTip(fingerId);
            }

            public void UpdateTipPosition(ShadowHand hand)
            {
                var pose = hand.GetWorldPose(_tipId);
                TipPosition = pose.position;
            }

            public void UpdateIsPinching(float distance, float start, float stopOffset, float stopMax)
            {
                if (!IsPinching)
                {
                    if (distance < start)
                    {
                        IsPinching = true;
                        IsPinchingChanged = true;
                        _minPinchDistance = distance;
                    }
                }
                else
                {
                    _minPinchDistance = Mathf.Min(_minPinchDistance, distance);
                    if (distance > stopMax ||
                        distance > _minPinchDistance + stopOffset)
                    {
                        IsPinching = false;
                        IsPinchingChanged = true;
                        _minPinchDistance = float.MaxValue;
                    }
                }
            }

            public void ClearState()
            {
                IsPinchingChanged = false;
            }
        }

        private readonly FingerPinchData[] _fingersPinchData =
        {
            new FingerPinchData(HandFinger.Thumb),
            new FingerPinchData(HandFinger.Index),
            new FingerPinchData(HandFinger.Middle),
            new FingerPinchData(HandFinger.Ring),
            new FingerPinchData(HandFinger.Pinky)
        };

        private IHmd _hmd = null;
        private readonly ShadowHand _shadowHand = new();
        private float _handScale;
        private Pose _rootPose;

        public PinchGrabAPI(IHmd hmd = null)
        {
            _hmd = hmd;
        }

        public bool GetFingerIsGrabbing(HandFinger finger)
        {
            return _fingersPinchData[(int)finger].IsPinching;
        }

        public Vector3 GetWristOffsetLocal()
        {
            float maxStrength = _fingersPinchData[0].PinchStrength;
            Vector3 thumbTip = _fingersPinchData[0].TipPosition;
            Vector3 center = thumbTip;
            for (int i = 1; i < Constants.NUM_FINGERS; ++i)
            {
                float strength = _fingersPinchData[i].PinchStrength;
                if (strength > maxStrength)
                {
                    maxStrength = strength;
                    Vector3 fingerTip = _fingersPinchData[i].TipPosition;
                    center = (thumbTip + fingerTip) * 0.5f;
                }
            }
            return center;
        }

        public bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetPinchState)
        {
            return _fingersPinchData[(int)finger].IsPinchingChanged &&
                   _fingersPinchData[(int)finger].IsPinching == targetPinchState;
        }

        public float GetFingerGrabScore(HandFinger finger)
        {
            return _fingersPinchData[(int)finger].PinchStrength;
        }

        public void Update(IHand hand)
        {
            hand.GetRootPose(out var rootPose);
            hand.GetJointPosesLocal(out var localJointPoses);
            Update(localJointPoses, hand.Handedness, rootPose, hand.Scale);
        }

        internal void Update(IReadOnlyList<Pose> handPoses, Handedness handedness, Pose rootPose, float handScale)
        {
            ClearState();
            _shadowHand.SetRoot(Pose.identity);
#if ISDK_OPENXR_HAND
            _shadowHand.FromJoints(handPoses, false);
#else
            _shadowHand.FromJoints(handPoses, handedness == Handedness.Left);
#endif

            _rootPose = rootPose;
            _handScale = handScale;

            _isPinchVisibilityGood = PinchHasGoodVisibility(handedness);

            UpdateThumb(handedness);
            UpdateFinger(HandFinger.Index);
            UpdateFinger(HandFinger.Middle);
            UpdateFinger(HandFinger.Ring);
            UpdateFinger(HandFinger.Pinky);
        }

        private void UpdateThumb(Handedness handedness)
        {
            int fingerIndex = (int)HandFinger.Thumb;
            _fingersPinchData[fingerIndex].UpdateTipPosition(_shadowHand);

            float distance = float.PositiveInfinity;
            if (IsThumbNearIndex(handedness))
            {
                var thumb3Pose = _shadowHand.GetWorldPose(HandJointId.HandThumb3);
                Vector3 thumbStart = thumb3Pose.position;
                Vector3 thumbEnd = _fingersPinchData[fingerIndex].TipPosition;
                distance = GetClosestDistanceToJoints(thumbStart, thumbEnd, INDEX_JOINTS, THUMB_MAX_DOT);
            }
            UpdatePinchData(distance, fingerIndex,
            THUMB_DISTANCE_START, THUMB_DISTANCE_STOP_OFFSET, THUMB_DISTANCE_STOP_MAX);
        }

        private bool IsThumbNearIndex(Handedness handedness)
        {
            var thumbTipPose = _shadowHand.GetWorldPose(HandJointId.HandThumbTip);
            var indexPose = _shadowHand.GetWorldPose(HandJointId.HandIndex2);

            Vector3 indexSideDir = indexPose.rotation * (handedness == Handedness.Left ? Constants.LeftThumbSide : Constants.RightThumbSide);
            Plane indexPlane = new Plane(indexSideDir, indexPose.position);
            float distanceToPlane = Mathf.Abs(indexPlane.GetDistanceToPoint(thumbTipPose.position));
            return distanceToPlane > 0 && distanceToPlane < THUMB_DISTANCE_STOP_MAX;
        }

        private void UpdateFinger(HandFinger finger)
        {
            int fingerIndex = (int)finger;
            _fingersPinchData[fingerIndex].UpdateTipPosition(_shadowHand);

            float distance = float.PositiveInfinity;
            if (_fingersPinchData[fingerIndex].IsPinching)
            {
                distance = GetClosestDistanceToJoints(_fingersPinchData[fingerIndex].TipPosition, THUMB_JOINTS_MAINTAIN);
            }
            if (IsPointNearThumb(_fingersPinchData[fingerIndex].TipPosition, THUMB_JOINTS_SELECT))
            {
                distance = GetClosestDistanceToJoints(_fingersPinchData[fingerIndex].TipPosition, THUMB_JOINTS_SELECT);
            }

            UpdatePinchData(distance, fingerIndex,
                DistanceStart, DistanceStopOffset, DistanceStopMax);
        }

        private void UpdatePinchData(float distance, int fingerIndex,
            float distanceStart, float distanceStopOffset, float distanceStopMax)
        {
            _fingersPinchData[fingerIndex].UpdateIsPinching(distance,
                distanceStart, distanceStopOffset, distanceStopMax);
            float pinchPercent = (distance - distanceStart) /
                (distanceStopMax - distanceStart);
            float pinchStrength = 1f - Mathf.Clamp01(pinchPercent);

            _fingersPinchData[fingerIndex].PinchStrength = pinchStrength;
        }

        private void ClearState()
        {
            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                _fingersPinchData[i].ClearState();
            }
        }

        private bool IsPointNearThumb(Vector3 position, HandJointId[] thumbJoints)
        {
            var boneStart = _shadowHand.GetWorldPose(thumbJoints[0]);
            var boneEnd = _shadowHand.GetWorldPose(thumbJoints[1]);
            Vector3 p0 = boneStart.position;
            Vector3 p1 = boneEnd.position;
            Vector3 lineVec = p1 - p0;
            Vector3 fromP0 = position - p0;
            Vector3 projectedPos = Vector3.Project(fromP0, lineVec.normalized);
            return Vector3.Dot(projectedPos, lineVec) > 0;
        }

        private float GetClosestDistanceToJoints(Vector3 edgeStart, Vector3 edgeEnd, HandJointId[] targetJoints, float maximumDotAllowed = 1f)
        {
            float minDistance = float.PositiveInfinity;
            for (int i = 0; i < targetJoints.Length - 1; i++)
            {
                var boneStart = _shadowHand.GetWorldPose(targetJoints[i]);
                var boneEnd = _shadowHand.GetWorldPose(targetJoints[i + 1]);
                if (maximumDotAllowed < 1f
                    && Vector3.Dot((edgeEnd - edgeStart).normalized,
                        (boneEnd.position - boneStart.position).normalized) >= maximumDotAllowed)
                {
                    continue;
                }

                float distance = DistanceSegmentToSegment(edgeStart, edgeEnd, boneStart.position, boneEnd.position);
                minDistance = Mathf.Min(minDistance, distance);
            }

            return minDistance;
        }

        private float GetClosestDistanceToJoints(Vector3 position, HandJointId[] targetJoints)
        {
            float minDistance = float.PositiveInfinity;
            for (int i = 0; i < targetJoints.Length - 1; i++)
            {
                var boneStart = _shadowHand.GetWorldPose(targetJoints[i]);
                var boneEnd = _shadowHand.GetWorldPose(targetJoints[i + 1]);
                minDistance = Mathf.Min(minDistance,
                    DistancePointToSegment(position, boneStart.position, boneEnd.position));
            }

            return minDistance;
        }

        private float DistancePointToSegment(Vector3 point, Vector3 a0, Vector3 a1)
        {
            Vector3 lineVec = a1 - a0;
            Vector3 fromP0 = point - a0;
            float normalizedProjection = Vector3.Dot(fromP0, lineVec) / Vector3.Dot(lineVec, lineVec);
            float closestT = Mathf.Clamp01(normalizedProjection);
            Vector3 closestPoint = a0 + closestT * lineVec;
            return (closestPoint - point).magnitude;
        }

        private float DistanceSegmentToSegment(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            Vector3 aDir = (a1 - a0);
            Vector3 bDir = (b1 - b0);
            Vector3 orthogonalDir = Vector3.Cross(aDir, bDir);

            //In order to find the Segment (c) that is the shortest between (a) and (b):
            //Project the two segments (a) and (b) in the Orthogonal plane
            //This way we can find the (A)(B)(C) Triangle rectangle that has
            // A == a0 (in the projected space)
            // B == a point along vector (b) whose angle is 90 degrees to A (in the projected space)
            // C == the point where (a) and (b) cross each other (in the projected space)
            Vector3 A = Vector3.ProjectOnPlane(a0, orthogonalDir);
            Vector3 b0Projected = Vector3.ProjectOnPlane(b0, orthogonalDir);
            Vector3 aDirProjected = Vector3.ProjectOnPlane(aDir, orthogonalDir);
            Vector3 bDirProjected = Vector3.ProjectOnPlane(bDir, orthogonalDir);

            Vector3 B = b0Projected + Vector3.Project(A - b0Projected, bDirProjected);
            Vector3 adjacentSide = B - A;
            float angleA = Vector3.Dot(aDirProjected.normalized, adjacentSide.normalized);
            float hypotenuse = adjacentSide.magnitude / angleA;
            //C would be A + aDirProjected * hypotenuse.

            //c0 is the start point in world space for the segment (c). It has to be inside (a)
            Vector3 c0 = a0 + aDir * Mathf.Clamp01(hypotenuse / aDirProjected.magnitude);

            //c1 is the end point in world space, for the segment (c). It can be found by
            //projecting b0c0 into (b)
            Vector3 b0c1 = Vector3.Project(c0 - b0, bDir);

            //c1 has to be inside (b)
            if (Vector3.Dot(b0c1, bDir) < 0)
            {
                b0c1 = Vector3.zero;
            }
            else if (b0c1.sqrMagnitude > bDir.sqrMagnitude)
            {
                b0c1 = bDir;
            }
            Vector3 c1 = b0 + b0c1;

            return Vector3.Distance(c0, c1);
        }

        private bool PinchHasGoodVisibility(Handedness handedness)
        {
            if (_hmd == null
                || !_hmd.TryGetRootPose(out Pose centerEyePose))
            {
                return false;
            }

            Vector3 handVector = _rootPose.rotation *
                (handedness == Handedness.Left ? Constants.LeftPinkySide : Constants.RightPinkySide);
            Vector3 targetVector = centerEyePose.forward;

            float angle = Vector3.Angle(handVector, targetVector);
            return angle <= PINCH_HQ_VIEW_ANGLE_THRESHOLD;
        }
    }
}
