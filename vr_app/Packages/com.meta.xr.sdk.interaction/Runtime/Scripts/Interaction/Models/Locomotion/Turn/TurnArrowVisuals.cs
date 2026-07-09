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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This visuals component renders one curved arrow to the left, and another one to the right.
    /// Using the Visual parameters one can control the angle, spacing, curvature, thickness of the arrows.
    /// Using the controllers one can highlight one or the other arrow, make them grow or move along their trail.
    /// </summary>
    public class TurnArrowVisuals : MonoBehaviour
    {
        /// <summary>
        /// Renderer for the Left arrow cone
        /// </summary>
        [Header("Visual renderers")]
        [Tooltip("Renderer for the Left arrow cone")]
        [SerializeField]
        private Renderer _leftArrow;
        /// <summary>
        /// Renderer for the Right arrow cone
        /// </summary>
        [Tooltip("Renderer for the Right arrow cone")]
        [SerializeField]
        private Renderer _rightArrow;
        /// <summary>
        /// TubeRenderer that will draw the rail of the left arrow
        /// </summary>
        [Tooltip("TubeRenderer that will draw the rail of the left arrow")]
        [SerializeField]
        private TubeRenderer _leftRail;
        /// <summary>
        /// TubeRenderer that will draw the rail of the right arrow
        /// </summary>
        [Tooltip("TubeRenderer that will draw the rail of the right arrow")]
        [SerializeField]
        private TubeRenderer _rightRail;
        /// <summary>
        /// TubeRenderer that will draw the trail of the right arrow
        /// </summary>
        [Tooltip("TubeRenderer that will draw the trail of the right arrow")]
        [SerializeField]
        private TubeRenderer _leftTrail;
        /// <summary>
        /// TubeRenderer that will draw the trail of the right arrow
        /// </summary>
        [Tooltip("TubeRenderer that will draw the trail of the right arrow")]
        [SerializeField]
        private TubeRenderer _rightTrail;
        /// <summary>
        /// Material block for the left arrow items so they can be controller
        /// </summary>
        [Tooltip("Material block for the left arrow items so they can be controller")]
        [SerializeField]
        private MaterialPropertyBlockEditor _leftMaterialBlock;
        /// <summary>
        /// Material block for the right arrow items so they can be controller
        /// </summary>
        [Tooltip("Material block for the right arrow items so they can be controller")]
        [SerializeField]
        private MaterialPropertyBlockEditor _rightMaterialBlock;

        /// <summary>
        /// Radius of the circle in which the arrows are circunscribed
        /// </summary>
        [Header("Visual parameters")]
        [Tooltip("Radius of the circle in which the arrows are circunscribed")]
        [SerializeField]
        private float _radius = 0.07f;
        public float Radius => _radius;
        /// <summary>
        /// Gap, in degrees, left between the arrows
        /// </summary>
        [Tooltip("Gap, in degrees, left between the arrows")]
        [SerializeField]
        private float _margin = 2f;
        public float Margin => _margin;
        /// <summary>
        /// Length, in degrees, of the trail of the arrows
        /// </summary>
        [Tooltip("Length, in degrees, of the trail of the arrows")]
        [SerializeField]
        private float _trailLength = 15f;
        public float TrailLength => _trailLength;
        /// <summary>
        /// Max angle, in degrees, the arrows can follow when highlighted
        /// </summary>
        [Tooltip("Max angle, in degrees, the arrows can follow when highlighted")]
        [SerializeField]
        private float _maxAngle = 45f;
        public float MaxAngle => _maxAngle;
        /// <summary>
        /// Length of the transparent gap in the rail left by the arrow
        /// </summary>
        [Tooltip("Length of the transparent gap in the rail left by the arrow")]
        [SerializeField]
        private float _railGap = 0.005f;
        public float RailGap => _railGap;
        /// <summary>
        /// Length, in degrees, that the arrows can grow when highlighted
        /// </summary>
        [Tooltip("Length, in degrees, that the arrows can grow when highlighted")]
        [SerializeField]
        private float _squeezeLength = 5f;
        public float SqueezeLength => _squeezeLength;
        /// <summary>
        /// Color of the arrow when not active
        /// </summary>
        [Header("Visual controllers")]
        [Tooltip("Color of the arrow when not active")]
        [SerializeField]
        private Color _disabledColor = new Color(1f, 1f, 1f, 0.2f);
        public Color DisabledColor
        {
            get => _disabledColor;
            set => _disabledColor = value;
        }
        /// <summary>
        /// Color of the arrow when active
        /// </summary>
        [Tooltip("Color of the arrow when active")]
        [SerializeField]
        private Color _enabledColor = new Color(1f, 1f, 1f, 0.6f);
        public Color EnabledColor
        {
            get => _enabledColor;
            set => _enabledColor = value;
        }
        /// <summary>
        /// Color of the arrow when highlighted
        /// </summary>
        [Tooltip("Color of the arrow when highlighted")]
        [SerializeField]
        private Color _highligtedColor = new Color(1f, 1f, 1f, 1f);
        public Color HighligtedColor
        {
            get => _highligtedColor;
            set => _highligtedColor = value;
        }
        /// <summary>
        /// If true, the current active arrow will
        /// look highlighted
        /// </summary>
        [Tooltip("If true, the current active arrow will")]
        [SerializeField]
        private bool _highLight = false;
        public bool HighLight
        {
            get => _highLight;
            set => _highLight = value;
        }
        /// <summary>
        /// This value controls wich arrow is active, <0 for the left and >0 for the right
        /// </summary>
        [Tooltip("This value controls wich arrow is active, <0 for the left and >0 for the right")]
        [SerializeField]
        private float _value = 0f;
        public float Value
        {
            get => _value;
            set => _value = value;
        }
        /// <summary>
        /// Indicates how much the active arrow must grow
        /// </summary>
        [Tooltip("Indicates how much the active arrow must grow")]
        [SerializeField]
        private float _progress;
        public float Progress
        {
            get => _progress;
            set => _progress = value;
        }
        /// <summary>
        /// Indicates wheter the active arrow should follow the rail
        /// </summary>
        [Tooltip("Indicates wheter the active arrow should follow the rail")]
        [SerializeField]
        private bool _followArrow = false;
        public bool FollowArrow
        {
            get => _followArrow;
            set => _followArrow = value;
        }

        private const float _degreesPerSegment = 1f;

        private static readonly Quaternion _rotationCorrectionLeft = Quaternion.Euler(0f, -90f, 0f);
        private static readonly int _colorShaderPropertyID = Shader.PropertyToID("_Color");

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_leftTrail, nameof(_leftTrail));
            this.AssertField(_rightTrail, nameof(_rightTrail));
            this.AssertField(_leftArrow, nameof(_leftArrow));
            this.AssertField(_rightArrow, nameof(_rightArrow));
            this.AssertField(_leftRail, nameof(_leftRail));
            this.AssertField(_rightRail, nameof(_rightRail));

            this.AssertField(_leftMaterialBlock, nameof(_leftMaterialBlock));
            this.AssertField(_rightMaterialBlock, nameof(_rightMaterialBlock));

            InitializeVisuals();
            DisableVisuals();

            this.EndStart(ref _started);
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                DisableVisuals();
            }
        }

        /// <summary>
        /// Disables all the visual renderers
        /// </summary>
        public void DisableVisuals()
        {
            _leftTrail.enabled = false;
            _rightTrail.enabled = false;
            _leftArrow.enabled = false;
            _rightArrow.enabled = false;
            _leftRail.enabled = false;
            _rightRail.enabled = false;
        }

        private void InitializeVisuals()
        {
            TubePoint[] trailPoints = InitializeSegment(new Vector2(_margin, _maxAngle + _squeezeLength));
            _leftTrail.RenderTube(trailPoints, Space.Self);
            _rightTrail.RenderTube(trailPoints, Space.Self);

            TubePoint[] railPoints = InitializeSegment(new Vector2(_margin, _maxAngle));
            _leftRail.RenderTube(railPoints, Space.Self);
            _rightRail.RenderTube(railPoints, Space.Self);
        }

        /// <summary>
        /// Updates the Arrows and Colors with the provided Values.
        /// It also re-enables the renderers so they are instantly drawn.
        /// </summary>
        public void UpdateVisual()
        {
            UpdateArrows(Value);
            UpdateColors(HighLight, Value);
        }

        private void UpdateArrows(float value)
        {
            float angle = Mathf.Lerp(0f, _maxAngle, Mathf.Abs(value));
            bool isLeft = value < 0;
            bool follow = _followArrow;
            float squeeze = Mathf.Lerp(0f, _squeezeLength, _progress);

            _leftTrail.enabled = true;
            _rightTrail.enabled = true;
            _leftArrow.enabled = true;
            _rightArrow.enabled = true;

            _rightRail.enabled = !isLeft;
            _leftRail.enabled = isLeft;

            angle = Mathf.Max(angle, _trailLength);

            UpdateArrowPosition(isLeft ? _trailLength : angle + squeeze, _rightArrow.transform);
            RotateTrail(follow && !isLeft ? angle - _trailLength : 0f, _rightTrail);
            UpdateTrail(isLeft ? _trailLength : (follow ? _trailLength : angle) + squeeze, _rightTrail);

            UpdateArrowPosition(!isLeft ? -_trailLength : -angle - squeeze, _leftArrow.transform);
            RotateTrail(follow && isLeft ? -angle + _trailLength : 0f, _leftTrail);
            UpdateTrail(!isLeft ? _trailLength : (follow ? _trailLength : angle) + squeeze, _leftTrail);

            UpdateRail(angle, squeeze, isLeft ? _leftRail : _rightRail);
        }

        private void UpdateArrowPosition(float angle, Transform arrow)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            arrow.localPosition = rotation * Vector3.forward * _radius;
            arrow.localRotation = rotation * _rotationCorrectionLeft;
        }

        private void RotateTrail(float angle, TubeRenderer trail)
        {
            trail.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.up);
        }

        private void UpdateTrail(float angle, TubeRenderer trail)
        {
            float max = _maxAngle + _squeezeLength;
            float segmentLenght = trail.TotalLength;
            float start = -100;
            float end = (max - angle - _margin) / max;

            trail.StartFadeThresold = segmentLenght * start;
            trail.EndFadeThresold = segmentLenght * end;
            trail.InvertThreshold = false;
            trail.RedrawFadeThresholds();
        }

        private void UpdateRail(float angle, float extra, TubeRenderer rail)
        {
            float segmentLenght = rail.TotalLength;
            float start = (angle - _trailLength - _margin) / _maxAngle;
            float end = (_maxAngle - angle - extra - _margin) / _maxAngle;

            float gap = _railGap + rail.Feather;
            rail.StartFadeThresold = segmentLenght * start - gap;
            rail.EndFadeThresold = segmentLenght * end - gap;
            rail.InvertThreshold = true;
            rail.RedrawFadeThresholds();
        }

        private void UpdateColors(bool isSelection, float value)
        {
            _leftMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, value < 0 ? (isSelection ? _highligtedColor : _enabledColor) : _disabledColor);
            _rightMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, value > 0 ? (isSelection ? _highligtedColor : _enabledColor) : _disabledColor);

            _leftMaterialBlock.UpdateMaterialPropertyBlock();
            _rightMaterialBlock.UpdateMaterialPropertyBlock();
        }

        private TubePoint[] InitializeSegment(Vector2 minMax)
        {
            float lowLimit = minMax.x;
            float upLimit = minMax.y;
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

        public void InjectAllTurnArrowVisuals(
            Renderer leftArrow, Renderer rightArrow,
            TubeRenderer leftRail, TubeRenderer rightRail, TubeRenderer leftTrail, TubeRenderer rightTrail,
            MaterialPropertyBlockEditor leftMaterialBlock, MaterialPropertyBlockEditor rightMaterialBlock,
            float radius, float margin, float trailLength, float maxAngle, float railGap, float squeezeLength)
        {
            InjectLeftArrow(leftArrow);
            InjectRightArrow(rightArrow);
            InjectLeftRail(leftRail);
            InjectRightRail(rightRail);
            InjectLeftTrail(leftTrail);
            InjectRightTrail(rightTrail);
            InjectLeftMaterialBlock(leftMaterialBlock);
            InjectRightMaterialBlock(rightMaterialBlock);
            InjectRadius(radius);
            InjectMargin(margin);
            InjectTrailLength(trailLength);
            InjectMaxAngle(maxAngle);
            InjectRailGap(railGap);
            InjectSqueezeLength(squeezeLength);
        }

        public void InjectLeftArrow(Renderer leftArrow)
        {
            _leftArrow = leftArrow;
        }
        public void InjectRightArrow(Renderer rightArrow)
        {
            _rightArrow = rightArrow;
        }
        public void InjectLeftRail(TubeRenderer leftRail)
        {
            _leftRail = leftRail;
        }
        public void InjectRightRail(TubeRenderer rightRail)
        {
            _rightRail = rightRail;
        }
        public void InjectLeftTrail(TubeRenderer leftTrail)
        {
            _leftTrail = leftTrail;
        }
        public void InjectRightTrail(TubeRenderer rightTrail)
        {
            _rightTrail = rightTrail;
        }
        public void InjectLeftMaterialBlock(MaterialPropertyBlockEditor leftMaterialBlock)
        {
            _leftMaterialBlock = leftMaterialBlock;
        }
        public void InjectRightMaterialBlock(MaterialPropertyBlockEditor rightMaterialBlock)
        {
            _rightMaterialBlock = rightMaterialBlock;
        }
        public void InjectRadius(float radius)
        {
            _radius = radius;
        }
        public void InjectMargin(float margin)
        {
            _margin = margin;
        }
        public void InjectTrailLength(float trailLength)
        {
            _trailLength = trailLength;
        }
        public void InjectMaxAngle(float maxAngle)
        {
            _maxAngle = maxAngle;
        }
        public void InjectRailGap(float railGap)
        {
            _railGap = railGap;
        }
        public void InjectSqueezeLength(float squeezeLength)
        {
            _squeezeLength = squeezeLength;
        }

        #endregion
    }
}
