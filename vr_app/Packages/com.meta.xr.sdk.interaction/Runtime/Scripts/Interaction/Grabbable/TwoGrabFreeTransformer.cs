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

using System.Collections.Generic;
using UnityEngine;
using System;

using static Oculus.Interaction.TransformerUtils;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that transforms the target in a free form way for an intuitive
    /// two hand translation, rotation and scale.
    /// </summary>
    [Obsolete("Use " + nameof(GrabFreeTransformer) + " instead")]
    public class TwoGrabFreeTransformer : MonoBehaviour, ITransformer
    {
        [Serializable]
        public class TwoGrabFreeConstraints
        {
            [Tooltip("If true then the constraints are relative to the initial/base scale of the object " +
                     "if false, constraints are absolute with respect to the object's selected axes.")]
            public bool ConstraintsAreRelative;
            public FloatConstraint MinScale;
            public FloatConstraint MaxScale;
            public bool ConstrainXScale = true;
            public bool ConstrainYScale = false;
            public bool ConstrainZScale = false;
        }

        [SerializeField]
        private TwoGrabFreeConstraints _constraints;

        public TwoGrabFreeConstraints Constraints
        {
            get => _constraints;
            set => _constraints = value;
        }

        public struct TwoGrabFreeState
        {
            public Pose Center;
            public float Distance;
        }

        private IGrabbable _grabbable;
        private Vector3 _baseScale;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
            _baseScale = _grabbable.Transform.localScale;
        }

        private Pose _localToTarget;
        private float _localMagnitudeToTarget;

        private Pose _prevGrabA;
        private Pose _prevGrabB;

        // The previous rotation for this transformation is tracked because it
        // cannot be derived each frame from the grab point information alone.
        private Quaternion _prevGrabRotation;

        public void BeginTransform()
        {
            var target = _grabbable.Transform;
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];

            _prevGrabA = grabA;
            _prevGrabB = grabB;

            var twoGrabFreeState = TwoGrabFreeInit(grabA.position, grabB.position);
            _prevGrabRotation = twoGrabFreeState.Center.rotation;
            _localToTarget = WorldToLocalPose(twoGrabFreeState.Center, target.worldToLocalMatrix);
            _localMagnitudeToTarget = WorldToLocalMagnitude(twoGrabFreeState.Distance, target.worldToLocalMatrix);
        }

        public void UpdateTransform()
        {
            var target = _grabbable.Transform;
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];

            var twoGrabFreeState = TwoGrabFree(
                _prevGrabRotation,
                _prevGrabA, _prevGrabB,
                grabA, grabB);

            float prevDistInWorld = LocalToWorldMagnitude(_localMagnitudeToTarget, target.localToWorldMatrix);
            float scaleDelta = prevDistInWorld != 0 ? twoGrabFreeState.Distance / prevDistInWorld : 1.0f;

            float targetScale = scaleDelta * target.localScale.x;
            float constrainedScale = ConstrainScale(targetScale);
            target.localScale = (constrainedScale / target.localScale.x) * target.localScale;

            Pose result = AlignLocalToWorldPose(target.localToWorldMatrix, _localToTarget, twoGrabFreeState.Center);
            target.position = result.position;
            target.rotation = result.rotation;

            _prevGrabRotation = twoGrabFreeState.Center.rotation;
            _prevGrabA = grabA;
            _prevGrabB = grabB;
        }

        private float ConstrainScale(float targetScale)
        {
            float finalScale = targetScale;
            if (_constraints.MinScale.Constrain)
            {
                Vector3 minScale = _constraints.MinScale.Value *
                    (_constraints.ConstraintsAreRelative ? _baseScale : Vector3.one);

                if (_constraints.ConstrainXScale)
                {
                    finalScale = Mathf.Max(finalScale, minScale.x);
                }
                if (_constraints.ConstrainYScale)
                {
                    finalScale = Mathf.Max(finalScale, minScale.y);
                }
                if (_constraints.ConstrainZScale)
                {
                    finalScale = Mathf.Max(finalScale, minScale.z);
                }
            }

            if (_constraints.MinScale.Constrain)
            {
                Vector3 maxScale = _constraints.MaxScale.Value *
                    (_constraints.ConstraintsAreRelative ? _baseScale : Vector3.one);

                if (_constraints.ConstrainXScale)
                {
                    finalScale = Mathf.Min(finalScale, maxScale.x);
                }
                if (_constraints.ConstrainYScale)
                {
                    finalScale = Mathf.Min(finalScale, maxScale.y);
                }
                if (_constraints.ConstrainZScale)
                {
                    finalScale = Mathf.Min(finalScale, maxScale.z);
                }
            }

            return finalScale;
        }

        public static TwoGrabFreeState TwoGrabFreeInit(Vector3 a, Vector3 b)
        {
            Vector3 center = Vector3.Lerp(a, b, 0.5f);
            Vector3 direction = b - a;
            Vector3 upDir = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.999 ? Vector3.up : Vector3.right;
            Quaternion rotation = Quaternion.LookRotation(direction, upDir);

            return new TwoGrabFreeState()
            {
                Center = new Pose(center, rotation),
                Distance = direction.magnitude
            };
        }

        public static TwoGrabFreeState TwoGrabFree(
            Quaternion initialRotation,
            Pose prevA, Pose prevB,
            Pose newA, Pose newB)
        {
            // Use the centroid of our grabs as the transformation center
            Vector3 initialCenter = Vector3.Lerp(prevA.position, prevB.position, 0.5f);
            Vector3 targetCenter = Vector3.Lerp(newA.position, newB.position, 0.5f);

            // The transformer rotation is based off of the previous rotation
            // The base rotation is based on the delta in vector rotation between grab points
            Vector3 initialVector = prevB.position - prevA.position;
            Vector3 targetVector = newB.position - newA.position;
            Quaternion baseRotation = Quaternion.FromToRotation(initialVector, targetVector);

            // Any local grab point rotation contributes 50% of its rotation to the final transformation
            // If both grab points rotate the same amount locally, the final result is a 1-1 rotation
            Quaternion deltaA = newA.rotation * Quaternion.Inverse(prevA.rotation);
            Quaternion halfDeltaA = Quaternion.Slerp(Quaternion.identity, deltaA, 0.5f).normalized;

            Quaternion deltaB = newB.rotation * Quaternion.Inverse(prevB.rotation);
            Quaternion halfDeltaB = Quaternion.Slerp(Quaternion.identity, deltaB, 0.5f).normalized;

            // Apply all the rotation deltas
            Quaternion baseTargetRotation = baseRotation * halfDeltaA * halfDeltaB * initialRotation;

            // Only affect the roll of the target vector using the target rotation
            Vector3 upDirection = baseTargetRotation * Vector3.up;
            Quaternion targetRotation = Quaternion.LookRotation(targetVector, upDirection).normalized;

            return new TwoGrabFreeState()
            {
                Center = new Pose(targetCenter, targetRotation),
                Distance = (newB.position - newA.position).magnitude
            };
        }

        public void MarkAsBaseScale()
        {
            _baseScale = _grabbable.Transform.localScale;
        }

        public void EndTransform() { }

        #region Inject

        public void InjectOptionalConstraints(TwoGrabFreeConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
