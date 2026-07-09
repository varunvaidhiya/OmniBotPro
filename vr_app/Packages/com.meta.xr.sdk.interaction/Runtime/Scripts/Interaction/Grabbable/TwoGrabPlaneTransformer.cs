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
using System;
using UnityEngine;
using UnityEngine.Serialization;

using static Oculus.Interaction.TransformerUtils;

namespace Oculus.Interaction
{
    /// <summary>
    /// Legacy <see cref="ITransformer"/> that translates, rotates and scales the target on a plane. For most usages,
    /// this has been superseded by <see cref="GrabFreeTransformer"/>, which can be constrained in Z scale to achieve
    /// planar behavior; for more information, see <see cref="ScaleConstraints"/> and
    /// <see cref="GrabFreeTransformer.InjectOptionalScaleConstraints(ScaleConstraints)"/>.
    /// </summary>
    public class TwoGrabPlaneTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField, Optional]
        private Transform _planeTransform = null;

        [SerializeField, Optional]
        private Vector3 _localPlaneNormal = new Vector3(0, 1, 0);

        /// <summary>
        /// Constraints applied to a <see cref="TwoGrabPlaneTransformer"/>'s transformation. These constraints limit
        /// various aspects of how the target object can be transformed; for details, see the documentation remarks on the
        /// various individual component constraints.
        /// </summary>
        [Serializable]
        public class TwoGrabPlaneConstraints
        {
            /// <summary>
            /// Sets the maximum allowed scale the <see cref="TwoGrabPlaneTransformer"/> can apply to its target.
            /// </summary>
            /// <remarks>
            /// Scale constraints (MaxScale and <see cref="MinScale"/>) specifically constrain the local X scale of the
            /// target, keeping the ratio of that dimension to the other local scale dimensions constant.
            /// </remarks>
            public FloatConstraint MaxScale;

            /// <summary>
            /// Sets the minimum allowed scale the <see cref="TwoGrabPlaneTransformer"/> can apply to its target.
            /// </summary>
            /// <remarks>
            /// Scale constraints (<see cref="MaxScale"/> and MinScale) specifically constrain the local X scale of the
            /// target, keeping the ratio of that dimension to the other local scale dimensions constant.
            /// </remarks>
            public FloatConstraint MinScale;

            /// <summary>
            /// Sets the minimum allowable value in a constraint operation along the Y direction. For more information, see
            /// <see cref="TransformerUtils.ConstrainAlongDirection(Vector3, Vector3, Vector3, FloatConstraint, FloatConstraint)"/>.
            /// </summary>
            public FloatConstraint MaxY;

            /// <summary>
            /// Sets the maximum allowable value in a constraint operation along the Y direction. For more information, see
            /// <see cref="TransformerUtils.ConstrainAlongDirection(Vector3, Vector3, Vector3, FloatConstraint, FloatConstraint)"/>.
            /// </summary>
            public FloatConstraint MinY;
        }

        [SerializeField]
        private TwoGrabPlaneConstraints _constraints;

        /// <summary>
        /// Gets or sets the <see cref="TwoGrabPlaneConstraints"/> for this transformer.
        /// </summary>
        /// <remarks>
        /// If constraints have already been assigned through the Editor or through <see cref="InjectOptionalConstraints(TwoGrabPlaneConstraints)"/>,
        /// this assignment will override them.
        /// </remarks>
        public TwoGrabPlaneConstraints Constraints
        {
            get => _constraints;
            set => _constraints = value;
        }

        /// <summary>
        /// The quintessential state of a <see cref="TwoGrabPlaneTransformer"/>.
        /// </summary>
        /// <remarks>
        /// Despite being public, this type is not used outside <see cref="TwoGrabPlaneTransformer"/>. It can be accessed and used
        /// by Editor scripts for debug visualizations, but you should avoid taking direct external dependencies on this type in
        /// production code.
        /// </remarks>
        public struct TwoGrabPlaneState
        {
            /// <summary>
            /// The centroid of the two grabs actuating a <see cref="TwoGrabPlaneTransformer"/>.
            /// </summary>
            /// <remarks>
            /// The position of this centroid is at the average of the projections onto the transforming plane of the two grab
            /// points. The orientation of this centroid is calculated from the line from grab point to grab point as well as
            /// the plane's normal. These values together logically constitute a homogeneous transform, but the scale for that
            /// is stored separately as <see cref="PlanarDistance"/>
            /// </remarks>
            public Pose Center;

            /// <summary>
            /// The distance, in world space, between the projections onto the transforming plane of the two grab points.
            /// </summary>
            public float PlanarDistance;
        }

        private IGrabbable _grabbable;

        /// <summary>
        /// Implementation of <see cref="ITransformer.Initialize(IGrabbable)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        private Pose _localToTarget;
        private float _localMagnitudeToTarget;

        private Vector3 WorldPlaneNormal()
        {
            Transform t = _planeTransform != null ? _planeTransform : _grabbable.Transform;
            return t.TransformDirection(_localPlaneNormal).normalized;
        }

        /// <summary>
        /// Implementation of <see cref="ITransformer.BeginTransform"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void BeginTransform()
        {
            var target = _grabbable.Transform;
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];
            var planeNormal = WorldPlaneNormal();

            var twoGrabPlaneState = TwoGrabPlane(grabA.position, grabB.position, planeNormal);
            _localToTarget = WorldToLocalPose(twoGrabPlaneState.Center, target.worldToLocalMatrix);
            _localMagnitudeToTarget = WorldToLocalMagnitude(twoGrabPlaneState.PlanarDistance, target.worldToLocalMatrix);
        }

        /// <summary>
        /// Implementation of <see cref="ITransformer.UpdateTransform"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void UpdateTransform()
        {
            var target = _grabbable.Transform;
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];
            var planeNormal = WorldPlaneNormal();

            var twoGrabPlaneState = TwoGrabPlane(grabA.position, grabB.position, planeNormal);

            float prevDistInWorld = LocalToWorldMagnitude(_localMagnitudeToTarget, target.localToWorldMatrix);
            float scaleDelta = prevDistInWorld != 0 ? twoGrabPlaneState.PlanarDistance / prevDistInWorld : 1f;

            float targetScale = scaleDelta * target.localScale.x;
            if (_constraints.MinScale.Constrain)
            {
                targetScale = Mathf.Max(_constraints.MinScale.Value, targetScale);
            }
            if (_constraints.MaxScale.Constrain)
            {
                targetScale = Mathf.Min(_constraints.MaxScale.Value, targetScale);
            }
            target.localScale = (targetScale / target.localScale.x) * target.localScale;

            Pose result = AlignLocalToWorldPose(target.localToWorldMatrix, _localToTarget, twoGrabPlaneState.Center);
            target.position = result.position;
            target.rotation = result.rotation;

            target.position = ConstrainAlongDirection(
                target.position, target.parent != null ? target.parent.position : Vector3.zero,
                planeNormal, _constraints.MinY, _constraints.MaxY);
        }

        /// <summary>
        /// Implementation of <see cref="ITransformer.EndTransform"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void EndTransform() { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="planeNormal"></param>
        /// <returns></returns>
        public static TwoGrabPlaneState TwoGrabPlane(Vector3 p0, Vector3 p1, Vector3 planeNormal)
        {
            Vector3 centroid = p0 * 0.5f + p1 * 0.5f;

            Vector3 p0planar = Vector3.ProjectOnPlane(p0, planeNormal);
            Vector3 p1planar = Vector3.ProjectOnPlane(p1, planeNormal);

            Vector3 planarDelta = p1planar - p0planar;
            Quaternion poseDir = Quaternion.LookRotation(planarDelta, planeNormal);

            return new TwoGrabPlaneState()
            {
                Center = new Pose(centroid, poseDir),
                PlanarDistance = planarDelta.magnitude
            };
        }

        #region Inject

        /// <summary>
        /// Injects all dependencies for a dynamically instantiated TwoGrabPlaneTransformer; effectively wraps
        /// <see cref="InjectOptionalConstraints(TwoGrabPlaneConstraints)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalPlaneTransform(Transform planeTransform)
        {
            _planeTransform = planeTransform;
        }

        /// <summary>
        /// Sets the <see cref="TwoGrabPlaneConstraints"/> for a dynamically instantiated TwoGrabPlaneTransformer. This method exists to
        /// support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalConstraints(TwoGrabPlaneConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
