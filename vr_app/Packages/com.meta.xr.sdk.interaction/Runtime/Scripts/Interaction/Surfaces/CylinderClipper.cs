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

using UnityEngine;

namespace Oculus.Interaction.Surfaces
{
    /// <summary>
    /// Clips a <cref="CylinderSurface" />. Each clipper represents the parameters of a cylinder segment.
    /// </summary>
    public class CylinderClipper : MonoBehaviour, ICylinderClipper
    {
        /// <summary>
        /// The rotation on the Y axis, expressed in degrees where 0 degrees aligns with the positive Z axis of the Cylinder.
        /// </summary>
        [Tooltip("The rotation of the center of the clip area " +
            "around the y axis, in degrees.")]
        [SerializeField, Range(-180f, 180f)]
        private float _rotation = 0f;

        /// <summary>
        /// The total arc degrees of the clip area, with the center point determined by Rotation.
        /// </summary>
        [Tooltip("The arc degrees of the clip area, " +
            "centered at the rotation value.")]
        [SerializeField, Range(0f, 360f)]
        private float _arcDegrees = 360f;

        /// <summary>
        /// The lower edge relative to the Y position of the cylinder.
        /// </summary>
        [Tooltip("The bottom extent of the clip area, along the y axis.")]
        [SerializeField]
        private float _bottom = -1;

        /// <summary>
        /// The upper edge relative to the Y position of the cylinder.
        /// </summary>
        [Tooltip("The top extent of the clip area, along the y axis.")]
        [SerializeField]
        private float _top = 1;

        public float ArcDegrees
        {
            get => _arcDegrees;
            set => _arcDegrees = value;
        }
        public float Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }
        public float Bottom
        {
            get => _bottom;
            set => _bottom = value;
        }
        public float Top
        {
            get => _top;
            set => _top = value;
        }

        public bool GetCylinderSegment(out CylinderSegment segment)
        {
            segment = new CylinderSegment(_rotation,
                _arcDegrees, _bottom, _top);
            return isActiveAndEnabled;
        }
    }
}
