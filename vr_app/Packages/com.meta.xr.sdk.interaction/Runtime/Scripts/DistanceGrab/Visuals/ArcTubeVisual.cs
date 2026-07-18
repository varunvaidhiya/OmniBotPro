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

namespace Oculus.Interaction
{
    /// <summary>
    /// Populates a TubeRenderer with the points to draw the section of a circle
    /// </summary>
    public class ArcTubeVisual : MonoBehaviour
    {
        [Header("Visual renderers")]
        [SerializeField]
        private TubeRenderer _tubeRenderer;

        [Header("Visual parameters")]
        [SerializeField]
        private float _radius = 0.07f;
        [SerializeField]
        private float _minAngle = 0f;
        [SerializeField]
        private float _maxAngle = 45f;

        private const float _degreesPerSegment = 1f;

        private static readonly Quaternion _rotationCorrectionLeft = Quaternion.Euler(0f, -90f, 0f);

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_tubeRenderer, nameof(_tubeRenderer));
            InitializeVisuals();
            this.EndStart(ref _started);
        }

        private void InitializeVisuals()
        {
            TubePoint[] trailPoints = InitializeSegment(new Vector2(_minAngle, _maxAngle));
            _tubeRenderer.RenderTube(trailPoints, Space.Self);
        }

        private TubePoint[] InitializeSegment(Vector2 minMaxAngle)
        {
            float lowLimit = minMaxAngle.x;
            float upLimit = minMaxAngle.y;
            int segments = Mathf.RoundToInt(Mathf.Repeat(upLimit - lowLimit, 360f) / _degreesPerSegment);
            TubePoint[] tubePoints = new TubePoint[segments];
            float segmentLenght = 1f / segments;
            for (int i = 0; i < segments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(-i * _degreesPerSegment - lowLimit, Vector3.up);
                tubePoints[i] = new TubePoint()
                {
                    position = rotation * Vector3.forward * _radius,
                    rotation = rotation * _rotationCorrectionLeft,
                    relativeLength = i * segmentLenght
                };
            }
            return tubePoints;
        }

        #region Inject

        public void InjectAllArcTubeVisual(TubeRenderer tubeRenderer,
            float radius, float minAngle, float maxAngle)
        {
            InjectTubeRenderer(tubeRenderer);
            InjectRadius(radius);
            InjectMinAngle(minAngle);
            InjectMaxAngle(maxAngle);
        }

        public void InjectTubeRenderer(TubeRenderer tubeRenderer)
        {
            _tubeRenderer = tubeRenderer;
        }

        public void InjectRadius(float radius)
        {
            _radius = radius;
        }

        public void InjectMinAngle(float minAngle)
        {
            _minAngle = minAngle;
        }

        public void InjectMaxAngle(float maxAngle)
        {
            _maxAngle = maxAngle;
        }

        #endregion
    }
}
