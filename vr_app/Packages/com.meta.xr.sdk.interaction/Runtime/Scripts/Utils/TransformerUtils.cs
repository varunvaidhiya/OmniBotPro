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

namespace Oculus.Interaction
{
    /// <summary>
    /// Contains a variety of static methods for specialized utility purposes, along with related child structures and classes.
    /// The static methods constrain a 3D data in a variety of ways, or encapsulate highly specific arithmetic operations.
    /// </summary>
    /// <remarks>
    /// These functions are not general; they are used by various <see cref="ITransformer"/> implementations, and the degree to
    /// which each function is tightly coupled to the conventions and assumptions of its usage varies. For that reason, you
    /// should avoid leveraging these directly; only add new usage if copy-pasting from existing usages, and use the existing
    /// implementation as a guide to understand how to use each of these correctly.
    /// </remarks>
    public class TransformerUtils
    {
        /// <summary>
        /// Indicates an inclusive range of permissible values for a given floating point datum.
        /// </summary>
        [Serializable]
        public struct FloatRange
        {
            public float Min;
            public float Max;
        }

        /// <summary>
        /// Struct describing a set of constraints for a one-dimensional floating point datum. This is used to describe,
        /// store, and apply constraints to values like spatial axes (X axis, Y axis, etc.).
        /// </summary>
        [Serializable]
        public struct ConstrainedAxis
        {
            /// <summary>
            /// Indicates whether the constraints described in this ConstrainedAxis should be applied.
            /// </summary>
            /// <remarks>
            /// If false, the datum to which this ConstrainedAxis pertains should be left unconstrained by this instance.
            /// </remarks>
            public bool ConstrainAxis;

            /// <summary>
            /// Indicates the <see cref="FloatRange"/> of permissible values for the datum to which this ConstrainedAxis pertains.
            /// </summary>
            public FloatRange AxisRange;

            /// <summary>
            /// A default ConstrainedAxis instance which applies no constraints.
            /// </summary>
            public static ConstrainedAxis Unconstrained => new ConstrainedAxis()
            {
                ConstrainAxis = false,
                AxisRange = new FloatRange() { Min = 1, Max = 1 }
            };
        }

        /// <summary>
        /// A collection of <see cref="ConstrainedAxis"/> constraints specifically constraining the position values of a
        /// 3D datum (typically a Transform).
        /// </summary>
        [Serializable]
        public class PositionConstraints
        {
            /// <summary>
            /// Indicates whether the constraints should be considered relative (i.e., applying to a Transform's local position
            /// relative to its parent Transform) or absolute (i.e., applying to a Transform's position in world space).
            /// </summary>
            public bool ConstraintsAreRelative;
            public ConstrainedAxis XAxis;
            public ConstrainedAxis YAxis;
            public ConstrainedAxis ZAxis;
        }

        /// <summary>
        /// A collection of <see cref="ConstrainedAxis"/> constraints specifically constraining the rotation values of a
        /// 3D datum (typically a Transform). These constraints are Euler angles expressed in degrees.
        /// </summary>
        [Serializable]
        public class RotationConstraints
        {
            public ConstrainedAxis XAxis;
            public ConstrainedAxis YAxis;
            public ConstrainedAxis ZAxis;
        }

        /// <summary>
        /// A collection of <see cref="ConstrainedAxis"/> constraints specifically constraining the scale values of a
        /// 3D datum (typically a Transform).
        /// </summary>
        [Serializable]
        public class ScaleConstraints
        {
            /// <summary>
            /// Indicates whether the constraints should be considered relative (i.e., applying to a Transform's local scale
            /// relative to its parent Transform) or absolute (i.e., applying to a Transform's scale relative to world space).
            /// </summary>
            public bool ConstraintsAreRelative;
            public ConstrainedAxis XAxis;
            public ConstrainedAxis YAxis;
            public ConstrainedAxis ZAxis;
        }

        /// <summary>
        /// Generates a new set of <see cref="PositionConstraints"/> based on an <paramref name="initialPosition"/>.
        /// </summary>
        /// <param name="constraints">The <see cref="PositionConstraints"/> upon which the new constraints should be based.</param>
        /// <param name="initialPosition">The initial position from which relative constraints should be relative.</param>
        /// <returns>New constraints which take <paramref name="initialPosition"/> into account.</returns>
        public static PositionConstraints GenerateParentConstraints(PositionConstraints constraints, Vector3 initialPosition)
        {
            PositionConstraints parentConstraints;

            if (!constraints.ConstraintsAreRelative)
            {
                parentConstraints = constraints;
            }
            else
            {
                parentConstraints = new PositionConstraints();

                parentConstraints.XAxis = new ConstrainedAxis();
                parentConstraints.YAxis = new ConstrainedAxis();
                parentConstraints.ZAxis = new ConstrainedAxis();

                if (constraints.XAxis.ConstrainAxis)
                {
                    parentConstraints.XAxis.ConstrainAxis = true;
                    parentConstraints.XAxis.AxisRange.Min = constraints.XAxis.AxisRange.Min + initialPosition.x;
                    parentConstraints.XAxis.AxisRange.Max = constraints.XAxis.AxisRange.Max + initialPosition.x;
                }
                if (constraints.YAxis.ConstrainAxis)
                {
                    parentConstraints.YAxis.ConstrainAxis = true;
                    parentConstraints.YAxis.AxisRange.Min = constraints.YAxis.AxisRange.Min + initialPosition.y;
                    parentConstraints.YAxis.AxisRange.Max = constraints.YAxis.AxisRange.Max + initialPosition.y;
                }
                if (constraints.ZAxis.ConstrainAxis)
                {
                    parentConstraints.ZAxis.ConstrainAxis = true;
                    parentConstraints.ZAxis.AxisRange.Min = constraints.ZAxis.AxisRange.Min + initialPosition.z;
                    parentConstraints.ZAxis.AxisRange.Max = constraints.ZAxis.AxisRange.Max + initialPosition.z;
                }
            }

            return parentConstraints;
        }

        /// <summary>
        /// Generates a new set of <see cref="ScaleConstraints"/> based on an <paramref name="initialScale"/>.
        /// </summary>
        /// <param name="constraints">The <see cref="ScaleConstraints"/> upon which the new constraints should be based.</param>
        /// <param name="initialScale">The initial scale from which relative constraints should be relative.</param>
        /// <returns>New constraints which take <paramref name="initialScale"/> into account.</returns>
        public static ScaleConstraints GenerateParentConstraints(ScaleConstraints constraints, Vector3 initialScale)
        {
            ScaleConstraints parentConstraints;

            if (!constraints.ConstraintsAreRelative)
            {
                parentConstraints = constraints;
            }
            else
            {
                parentConstraints = new ScaleConstraints();

                parentConstraints.XAxis = new ConstrainedAxis();
                parentConstraints.YAxis = new ConstrainedAxis();
                parentConstraints.ZAxis = new ConstrainedAxis();

                if (constraints.XAxis.ConstrainAxis)
                {
                    parentConstraints.XAxis.ConstrainAxis = true;
                    parentConstraints.XAxis.AxisRange.Min = constraints.XAxis.AxisRange.Min * initialScale.x;
                    parentConstraints.XAxis.AxisRange.Max = constraints.XAxis.AxisRange.Max * initialScale.x;
                }
                if (constraints.YAxis.ConstrainAxis)
                {
                    parentConstraints.YAxis.ConstrainAxis = true;
                    parentConstraints.YAxis.AxisRange.Min = constraints.YAxis.AxisRange.Min * initialScale.y;
                    parentConstraints.YAxis.AxisRange.Max = constraints.YAxis.AxisRange.Max * initialScale.y;
                }
                if (constraints.ZAxis.ConstrainAxis)
                {
                    parentConstraints.ZAxis.ConstrainAxis = true;
                    parentConstraints.ZAxis.AxisRange.Min = constraints.ZAxis.AxisRange.Min * initialScale.z;
                    parentConstraints.ZAxis.AxisRange.Max = constraints.ZAxis.AxisRange.Max * initialScale.z;
                }
            }

            return parentConstraints;
        }

        /// <summary>
        /// Applies a set of <see cref="PositionConstraints"/> to a vector representing the position of a Transform.
        /// </summary>
        /// <param name="unconstrainedPosition">The position of the Transform before constraining.</param>
        /// <param name="positionConstraints">The constraints to be applied to <paramref name="positionConstraints"/>.</param>
        /// <param name="relativeTransform">
        /// The transform to which constraint should be considered relative; if omitted, constraining will be applied relative to
        /// world space.
        /// </param>
        /// <returns>
        /// A position which is as similar as possible to <paramref name="unconstrainedPosition"/> but allowed by
        /// <paramref name="positionConstraints"/>.
        /// </returns>
        public static Vector3 GetConstrainedTransformPosition(Vector3 unconstrainedPosition, PositionConstraints positionConstraints, Transform relativeTransform = null)
        {
            Vector3 constrainedPosition = unconstrainedPosition;

            // the translation constraints occur in parent space
            if (relativeTransform != null)
            {
                constrainedPosition = relativeTransform.InverseTransformPoint(constrainedPosition);
            }

            if (positionConstraints.XAxis.ConstrainAxis)
            {
                constrainedPosition.x = Mathf.Clamp(constrainedPosition.x, positionConstraints.XAxis.AxisRange.Min, positionConstraints.XAxis.AxisRange.Max);
            }
            if (positionConstraints.YAxis.ConstrainAxis)
            {
                constrainedPosition.y = Mathf.Clamp(constrainedPosition.y, positionConstraints.YAxis.AxisRange.Min, positionConstraints.YAxis.AxisRange.Max);
            }
            if (positionConstraints.ZAxis.ConstrainAxis)
            {
                constrainedPosition.z = Mathf.Clamp(constrainedPosition.z, positionConstraints.ZAxis.AxisRange.Min, positionConstraints.ZAxis.AxisRange.Max);
            }

            // Convert the constrained position back to world space
            if (relativeTransform != null)
            {
                constrainedPosition = relativeTransform.TransformPoint(constrainedPosition);
            }

            return constrainedPosition;
        }

        /// <summary>
        /// Applies a set of <see cref="RotationConstraints"/> to a Quaternion representing the rotation of a Transform.
        /// </summary>
        /// <param name="unconstrainedRotation">The rotation of the Transform before constraining.</param>
        /// <param name="rotationConstraints">The constraints to be applied to <paramref name="unconstrainedRotation"/>.</param>
        /// <param name="relativeTransform">
        /// The transform to which constraint should be considered relative; if omitted, constraining will be applied relative to
        /// world space.
        /// </param>
        /// <returns>
        /// A rotation which is as similar as possible to <paramref name="unconstrainedRotation"/> but allowed by
        /// <paramref name="rotationConstraints"/>.
        /// </returns>
        public static Quaternion GetConstrainedTransformRotation(Quaternion unconstrainedRotation, RotationConstraints rotationConstraints, Transform relativeTransform = null)
        {
            if (relativeTransform != null)
            {
                unconstrainedRotation = Quaternion.Inverse(relativeTransform.rotation) * unconstrainedRotation;
            }

            Vector3 euler = unconstrainedRotation.eulerAngles;
            float xAngle = euler.x;
            float yAngle = euler.y;
            float zAngle = euler.z;

            if (rotationConstraints.XAxis.ConstrainAxis)
            {
                xAngle = ClampAngle(xAngle, rotationConstraints.XAxis.AxisRange.Min, rotationConstraints.XAxis.AxisRange.Max);
            }
            if (rotationConstraints.YAxis.ConstrainAxis)
            {
                yAngle = ClampAngle(yAngle, rotationConstraints.YAxis.AxisRange.Min, rotationConstraints.YAxis.AxisRange.Max);
            }
            if (rotationConstraints.ZAxis.ConstrainAxis)
            {
                zAngle = ClampAngle(zAngle, rotationConstraints.ZAxis.AxisRange.Min, rotationConstraints.ZAxis.AxisRange.Max);
            }

            Quaternion constrainedRotation = Quaternion.Euler(xAngle, yAngle, zAngle);

            // Convert the constrained position back to world space
            if (relativeTransform != null)
            {
                constrainedRotation = relativeTransform.rotation * constrainedRotation;
            }

            return constrainedRotation.normalized;


            float ClampAngle(float angle, float min, float max)
            {
                if (min == max)
                {
                    return min;
                }

                if (min <= max)
                {
                    if (angle >= min && angle <= max)
                    {
                        return angle;
                    }
                }
                else
                {
                    if (angle >= min || angle <= max)
                    {
                        return angle;
                    }
                }

                if (Mathf.Abs(Mathf.DeltaAngle(angle, min)) <= Mathf.Abs(Mathf.DeltaAngle(max, angle)))
                {
                    return min;
                }
                return max;
            }
        }

        /// <summary>
        /// Applies a set of <see cref="ScaleConstraints"/> to a vector representing the scale of a Transform.
        /// </summary>
        /// <param name="unconstrainedScale">The scale of the Transform before constraining.</param>
        /// <param name="scaleConstraints">The constraints to be applied to <paramref name="unconstrainedScale"/>.</param>
        /// <returns>
        /// A scale which is as similar as possible to <paramref name="unconstrainedScale"/> but allowed by
        /// <paramref name="scaleConstraints"/>.
        /// </returns>
        public static Vector3 GetConstrainedTransformScale(Vector3 unconstrainedScale, ScaleConstraints scaleConstraints)
        {
            Vector3 constrainedScale = unconstrainedScale;

            if (scaleConstraints.XAxis.ConstrainAxis)
            {
                constrainedScale.x = Mathf.Clamp(constrainedScale.x, scaleConstraints.XAxis.AxisRange.Min, scaleConstraints.XAxis.AxisRange.Max);
            }
            if (scaleConstraints.YAxis.ConstrainAxis)
            {
                constrainedScale.y = Mathf.Clamp(constrainedScale.y, scaleConstraints.YAxis.AxisRange.Min, scaleConstraints.YAxis.AxisRange.Max);
            }
            if (scaleConstraints.ZAxis.ConstrainAxis)
            {
                constrainedScale.z = Mathf.Clamp(constrainedScale.z, scaleConstraints.ZAxis.AxisRange.Min, scaleConstraints.ZAxis.AxisRange.Max);
            }

            return constrainedScale;
        }

        /// <summary>
        /// Convenience method for taking a Pose in world space (or whatever space is the domain of the transform represented
        /// by <paramref name="worldToLocal"/>) and returning its representation in local space (or whatever space is the
        /// range of <paramref name="worldToLocal"/>).
        /// </summary>
        /// <param name="worldPose">The Pose to be transformed.</param>
        /// <param name="worldToLocal">The transformation to be applied.</param>
        /// <returns>The image of <paramref name="worldPose"/> in <paramref name="worldToLocal"/>'s range.</returns>
        public static Pose WorldToLocalPose(Pose worldPose, Matrix4x4 worldToLocal)
        {
            return new Pose(worldToLocal.MultiplyPoint3x4(worldPose.position),
                            worldToLocal.rotation * worldPose.rotation);
        }

        /// <summary>
        /// This is an old utility method, new uses of which should be avoided. For homogeneous transform arithmetic, just use
        /// Unity's built-in Transform and matrix math support.
        /// </summary>
        /// <remarks>
        /// The naming and use of Pose representation in this implementation can be confusing, so this explanation
        /// will avoid them and map back at the end. Conceptually, there are three things: a scaled transform T in world space,
        /// an unscaled transform A in T space, and an unscaled transform B in world space. The goal is to find a scaled
        /// transform T' (sharing the scale of T) such that an unmodified A in T' equals B when mapped to world space;
        /// colloquially, "find where we need to move T to so that A and B are on top of one another." Mathematically, this
        /// means the relationship between T' and B is the same as the relationship between T and A; thus, if we find T in
        /// the space of A (or, more precisely, A as a scaled transform in world space), we can use that same relationship with
        /// B to find T'. This gives us the following arithmetic (treating each transform as a "local to world" matrix):
        ///
        ///     given T, A, and B
        ///     A_inWorldSpace := T * A
        ///     T_inASpace := inverse(A_inWorldSpace) * T
        ///     T' := b * T_inASpace
        ///
        /// Mapping this back to the variables and assumptions of this implementation, <paramref name="localToWorld"/> is T,
        /// <paramref name="local"/> is A, and <paramref name="world"/> is B. The fact that the result T' is a scaled transform
        /// is not reflected in the return value of this extension and is instead implicit in its usage. The position and
        /// rotation of the returned Pose must be assigned directly to the corresponding fields of the Transform which provided
        /// localToWorld, without modifying the scale of that Transform; any other usage may not yield the desired alignment.
        /// </remarks>
        /// <param name="localToWorld">T (see remarks for details).</param>
        /// <param name="local">A (see remarks for details).</param>
        /// <param name="world">B (see remarks for details).</param>
        /// <returns>T', excluding scale (see remarks for details).</returns>
        public static Pose AlignLocalToWorldPose(Matrix4x4 localToWorld, Pose local, Pose world)
        {
            var basePose = new Pose(localToWorld.MultiplyPoint3x4(local.position),
                                    localToWorld.rotation * local.rotation);
            Pose baseInverse = new Pose();
            PoseUtils.Inverse(basePose, ref baseInverse);
            Pose poseInBase = PoseUtils.Multiply(baseInverse, new Pose(localToWorld.GetPosition(), localToWorld.rotation));
            Pose poseInWorld = PoseUtils.Multiply(world, poseInBase);
            return poseInWorld;
        }

        /// <summary>
        /// Calculates how large a certain magnitude in world space is in local space.
        /// </summary>
        /// <remarks>
        /// This method specifically works by approximating the scale change from the world-space Z axis. If the transform
        /// hierarchy involves skew (rotated nonuniform scales), the result may not fully describe the scale relationship between
        /// local and world space. For more information, refer to Unity's documentation for Transform.lossyScale.
        /// </remarks>
        /// <param name="magnitude">The magnitude to be converted to local space.</param>
        /// <param name="localToWorld">The homogeneous transform from world to local space.</param>
        /// <returns>The magnitude in local space.</returns>
        public static float WorldToLocalMagnitude(float magnitude, Matrix4x4 worldToLocal)
        {
            return worldToLocal.MultiplyVector(magnitude * Vector3.forward).magnitude;
        }

        /// <summary>
        /// Calculates how large a certain magnitude in local space is in world space.
        /// </summary>
        /// <remarks>
        /// This method specifically works by approximating the scale change from the local-space Z axis. If the transform
        /// hierarchy involves skew (rotated nonuniform scales), the result may not fully describe the scale relationship between
        /// local and world space. For more information, refer to Unity's documentation for Transform.lossyScale.
        /// </remarks>
        /// <param name="magnitude">The magnitude to be converted to world space.</param>
        /// <param name="localToWorld">The homogeneous transform from local to world space.</param>
        /// <returns>The magnitude in world space.</returns>
        public static float LocalToWorldMagnitude(float magnitude, Matrix4x4 localToWorld)
        {
            return localToWorld.MultiplyVector(magnitude * Vector3.forward).magnitude;
        }

        /// <summary>
        /// Constrains a <paramref name="position"/> to a certain range of positions along a line defined by
        /// <paramref name="position"/> and <paramref name="direction"/> minimally distant from a range of points
        /// from <paramref name="origin"/> plus <paramref name="direction"/> scaled by <paramref name="min"/> to
        /// <paramref name="origin"/> plus <paramref name="direction"/> scaled by <paramref name="max"/>.
        /// </summary>
        /// <remarks>
        /// This is an extremely specialized utility used only by <see cref="TwoGrabPlaneTransformer"/>.
        /// </remarks>
        /// <param name="position">The position to be constrained.</param>
        /// <param name="origin">The origin of the line to which <paramref name="position"/> should be projected for constraint.</param>
        /// <param name="direction">The direction along which <paramref name="position"/> should be constrained.</param>
        /// <param name="min">
        /// The minimum signed distance along the line defined by <paramref name="origin"/> and <paramref name="direction"/> to
        /// which the closest projection of <paramref name="position"/> should be constrained.
        /// </param>
        /// <param name="max">
        /// The maximum signed distance along the line defined by <paramref name="origin"/> and <paramref name="direction"/> to
        /// which the closest projection of <paramref name="position"/> should be constrained.
        /// </param>
        /// <returns>The constrained position.</returns>
        public static Vector3 ConstrainAlongDirection(
            Vector3 position, Vector3 origin, Vector3 direction,
            FloatConstraint min, FloatConstraint max)
        {
            if (!min.Constrain && !max.Constrain) return position;

            float distanceAlongDirection = Vector3.Dot(position - origin, direction);

            float distanceConstrained = distanceAlongDirection;
            if (min.Constrain)
            {
                distanceConstrained = Mathf.Max(distanceConstrained, min.Value);
            }
            if (max.Constrain)
            {
                distanceConstrained = Mathf.Min(distanceConstrained, max.Value);
            }

            float distanceDelta = distanceConstrained - distanceAlongDirection;
            return position + direction * distanceDelta;
        }
    }
}
