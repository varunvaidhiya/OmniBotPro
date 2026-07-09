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

using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// An <see cref="ITransformer"/> that can translate, rotate and scale a transform using any number of grab points
    /// while also constraining the transformation if desired. This is the default Interaction SDK grab behavior
    /// and should be preferred over older implementations such as <see cref="OneGrabFreeTransformer"/> and
    /// <see cref="TwoGrabFreeTransformer"/>.
    /// </summary>
    public class GrabFreeTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField]
        [Tooltip("Constrains the position of the object along different axes. Units are meters.")]
        private TransformerUtils.PositionConstraints _positionConstraints =
            new TransformerUtils.PositionConstraints()
            {
                XAxis = new TransformerUtils.ConstrainedAxis(),
                YAxis = new TransformerUtils.ConstrainedAxis(),
                ZAxis = new TransformerUtils.ConstrainedAxis()
            };

        [SerializeField]
        [Tooltip("Constrains the rotation of the object along different axes. Units are degrees.")]
        private TransformerUtils.RotationConstraints _rotationConstraints =
            new TransformerUtils.RotationConstraints()
            {
                XAxis = new TransformerUtils.ConstrainedAxis(),
                YAxis = new TransformerUtils.ConstrainedAxis(),
                ZAxis = new TransformerUtils.ConstrainedAxis()
            };

        [SerializeField]
        [Tooltip("Constrains the local scale of the object along different axes. Expressed as a scale factor.")]
        private TransformerUtils.ScaleConstraints _scaleConstraints =
            new TransformerUtils.ScaleConstraints()
            {
                ConstraintsAreRelative = true,
                XAxis = new TransformerUtils.ConstrainedAxis()
                {
                    ConstrainAxis = true,
                    AxisRange = new TransformerUtils.FloatRange() { Min = 1, Max = 1 }
                },
                YAxis = new TransformerUtils.ConstrainedAxis()
                {
                    ConstrainAxis = true,
                    AxisRange = new TransformerUtils.FloatRange() { Min = 1, Max = 1 }
                },
                ZAxis = new TransformerUtils.ConstrainedAxis()
                {
                    ConstrainAxis = true,
                    AxisRange = new TransformerUtils.FloatRange() { Min = 1, Max = 1 }
                },
            };

        [SerializeField]
        [Tooltip("If enabled, breaks the \"grab point\" when scale is constrained so that reversing the scale motion " +
            "immediately scales rather than waiting for the grabs to \"catch up\" to the original grab point.")]
        private bool _resetScaleResponsivenessOnConstraintOvershoot = false;

        private IGrabbable _grabbable;
        private Pose _grabDeltaInLocalSpace;
        private TransformerUtils.PositionConstraints _relativePositionConstraints;
        private TransformerUtils.ScaleConstraints _relativeScaleConstraints;

        private Quaternion _lastRotation = Quaternion.identity;
        private Vector3 _lastScale = Vector3.one;

        private GrabPointDelta[] _deltas;

        internal struct GrabPointDelta
        {
            private const float _epsilon = 0.000001f;

            public Vector3 PrevCentroidOffset { get; private set; }
            public Vector3 CentroidOffset { get; private set; }

            public Quaternion PrevRotation { get; private set; }
            public Quaternion Rotation { get; private set; }

            public GrabPointDelta(Vector3 centroidOffset, Quaternion rotation)
            {
                this.PrevCentroidOffset = this.CentroidOffset = centroidOffset;
                this.PrevRotation = this.Rotation = rotation;
            }

            public void UpdateData(Vector3 centroidOffset, Quaternion rotation)
            {
                this.PrevCentroidOffset = this.CentroidOffset;
                this.CentroidOffset = centroidOffset;

                this.PrevRotation = this.Rotation;

                //Quaternions have two ways of expressing the same rotation.
                //This code ensures that the result is the same rotation but expressed in the desired sign.
                if (Quaternion.Dot(rotation, this.Rotation) < 0)
                {
                    rotation.x = -rotation.x;
                    rotation.y = -rotation.y;
                    rotation.z = -rotation.z;
                    rotation.w = -rotation.w;
                }

                this.Rotation = rotation;
            }

            public bool IsValidAxis()
            {
                return CentroidOffset.sqrMagnitude > _epsilon;
            }
        }

        /// <summary>
        /// Implementation of <see cref="ITransformer.Initialize(IGrabbable)"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
            _relativePositionConstraints = TransformerUtils.GenerateParentConstraints(_positionConstraints, _grabbable.Transform.localPosition);
            _relativeScaleConstraints = TransformerUtils.GenerateParentConstraints(_scaleConstraints, _grabbable.Transform.localScale);
        }

        /// <summary>
        /// Implementation of <see cref="ITransformer.BeginTransform"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public void BeginTransform()
        {
            int count = _grabbable.GrabPoints.Count;

            //rent space only while using
            _deltas = ArrayPool<GrabPointDelta>.Shared.Rent(count);

            InitializeDeltas(count, _grabbable.GrabPoints, ref _deltas);
            Vector3 centroid = GetCentroid(_grabbable.GrabPoints);

            Transform targetTransform = _grabbable.Transform;
            _grabDeltaInLocalSpace = new Pose(
                targetTransform.InverseTransformVector(centroid - targetTransform.position),
                targetTransform.rotation);
            _lastRotation = Quaternion.identity;
            _lastScale = targetTransform.localScale;
        }

        /// <summary>
        /// Implementation of <see cref="ITransformer.UpdateTransform"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public void UpdateTransform()
        {
            int count = _grabbable.GrabPoints.Count;
            Transform targetTransform = _grabbable.Transform;

            Vector3 localPosition = UpdateTransformerPointData(_grabbable.GrabPoints, ref _deltas);

            _lastScale = count <= 1 ? targetTransform.localScale : UpdateScale(count, _deltas) * _lastScale;
            targetTransform.localScale = TransformerUtils.GetConstrainedTransformScale(_lastScale, _relativeScaleConstraints);
            if (_resetScaleResponsivenessOnConstraintOvershoot)
            {
                _lastScale = targetTransform.localScale;
            }

            _lastRotation = UpdateRotation(count, _deltas) * _lastRotation;
            Quaternion rotation = _lastRotation * _grabDeltaInLocalSpace.rotation;
            targetTransform.rotation = TransformerUtils.GetConstrainedTransformRotation(rotation, _rotationConstraints, targetTransform.parent);

            Vector3 position = localPosition - targetTransform.TransformVector(_grabDeltaInLocalSpace.position);
            targetTransform.position = TransformerUtils.GetConstrainedTransformPosition(position, _relativePositionConstraints, targetTransform.parent);
        }

        /// <summary>
        /// Implementation of <see cref="ITransformer.EndTransform"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public void EndTransform()
        {
            //return the uneeded space
            ArrayPool<GrabPointDelta>.Shared.Return(_deltas);
            _deltas = null;
        }

        internal static void InitializeDeltas(int count, List<Pose> poses, ref GrabPointDelta[] deltas)
        {
            Vector3 centroid = GetCentroid(poses);
            for (int i = 0; i < count; i++)
            {
                Vector3 centroidOffset = GetCentroidOffset(poses[i], centroid);
                deltas[i] = new GrabPointDelta(centroidOffset, poses[i].rotation);
            }
        }

        internal static Vector3 UpdateTransformerPointData(List<Pose> poses, ref GrabPointDelta[] deltas)
        {
            Vector3 centroid = GetCentroid(poses);
            for (int i = 0; i < poses.Count; i++)
            {
                Vector3 centroidOffset = GetCentroidOffset(poses[i], centroid);
                deltas[i].UpdateData(centroidOffset, poses[i].rotation);
            }
            return centroid;
        }

        internal static Vector3 GetCentroid(List<Pose> poses)
        {
            int count = poses.Count;
            Vector3 sumPosition = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Pose pose = poses[i];
                sumPosition += pose.position;
            }

            return sumPosition / count;
        }

        internal static Vector3 GetCentroidOffset(Pose pose, Vector3 centre)
        {
            Vector3 centroidOffset = centre - pose.position;
            return centroidOffset;
        }

        internal static Quaternion UpdateRotation(int count, GrabPointDelta[] deltas)
        {
            Quaternion combinedRotation = Quaternion.identity;

            //each point can only affect a fraction of the rotation
            float fraction = 1f / count;
            for (int i = 0; i < count; i++)
            {
                GrabPointDelta data = deltas[i];

                //overall delta rotation since last update
                Quaternion rotDelta = data.Rotation * Quaternion.Inverse(data.PrevRotation);

                if (data.IsValidAxis())
                {
                    Vector3 aimingAxis = data.CentroidOffset.normalized;
                    //rotation along aiming axis
                    Quaternion dirDelta = Quaternion.FromToRotation(data.PrevCentroidOffset.normalized, aimingAxis);
                    combinedRotation = Quaternion.Slerp(Quaternion.identity, dirDelta, fraction) * combinedRotation;

                    //twist along the aiming axis
                    rotDelta.ToAngleAxis(out float angle, out Vector3 axis);
                    float projectionFactor = Vector3.Dot(axis, aimingAxis);
                    rotDelta = Quaternion.AngleAxis(angle * projectionFactor, aimingAxis);
                }

                combinedRotation = Quaternion.Slerp(Quaternion.identity, rotDelta, fraction) * combinedRotation;
            }

            return combinedRotation;
        }

        internal static float UpdateScale(int count, GrabPointDelta[] deltas)
        {
            float scaleDelta = 0f;
            for (int i = 0; i < count; i++)
            {
                GrabPointDelta data = deltas[i];
                if (data.IsValidAxis())
                {
                    float factor = Mathf.Sqrt(data.CentroidOffset.sqrMagnitude / data.PrevCentroidOffset.sqrMagnitude);
                    scaleDelta += factor / count;
                }
                else
                {
                    scaleDelta += 1f / count;
                }
            }
            return scaleDelta;
        }


        #region Inject

        /// <summary>
        /// Sets the optional <see cref="TransformerUtils.PositionConstraints"/> for a dynamically-allocated
        /// GrabFreeTransformer instance. This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalPositionConstraints(TransformerUtils.PositionConstraints constraints)
        {
            _positionConstraints = constraints;
        }

        /// <summary>
        /// Sets the optional <see cref="TransformerUtils.RotationConstraints"/> for a dynamically-allocated
        /// GrabFreeTransformer instance. This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalRotationConstraints(TransformerUtils.RotationConstraints constraints)
        {
            _rotationConstraints = constraints;
        }

        /// <summary>
        /// Sets the optional <see cref="TransformerUtils.ScaleConstraints"/> for a dynamically-allocated
        /// GrabFreeTransformer instance. This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalScaleConstraints(TransformerUtils.ScaleConstraints constraints)
        {
            _scaleConstraints = constraints;
        }

        #endregion
    }
}
