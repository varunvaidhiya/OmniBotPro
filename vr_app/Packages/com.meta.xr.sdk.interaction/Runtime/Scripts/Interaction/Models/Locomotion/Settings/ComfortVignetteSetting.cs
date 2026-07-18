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
    /// Controls the vignette strength based on locomotion events (turning, acceleration, movement).
    /// Applies the appropriate AnimationCurve to the tunneling component based on comfort level.
    /// </summary>
    public class ComfortVignetteSetting : MonoBehaviour
    {
        /// <summary>
        /// Types of movement that trigger the comfort vignette.
        /// </summary>
        public enum ComfortType
        {
            Turning,
            Accelerating,
            Moving
        }

        /// <summary>
        /// Intensity levels for the comfort vignette effect.
        /// </summary>
        public enum ComfortAssistance
        {
            Off = 0,
            Low = 1,
            Medium = 2,
            High = 3,
        }

        [SerializeField]
        [LocomotionSetting]
        [Tooltip("Vignette intensity level for motion comfort.")]
        private ReactiveValue<ComfortAssistance> _comfortLevel;
        /// <summary>Vignette intensity level for motion comfort.</summary>
        public ReactiveValue<ComfortAssistance> ComfortLevel => _comfortLevel;

        [SerializeField]
        [Tooltip("Type of movement that triggers the vignette effect.")]
        private ComfortType _comfortType;
        /// <summary>Type of movement that triggers the vignette effect.</summary>
        public ComfortType Comfort
        {
            get => _comfortType;
            set => _comfortType = value;
        }

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("Component that applies the tunneling vignette effect.")]
        private LocomotionTunneling _tunneling;

        [SerializeField, ConditionalHide(nameof(_tunneling), null, ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [Tooltip("FOV animation curve for Off comfort level.")]
        private AnimationCurve _curveOff;
        [SerializeField, ConditionalHide(nameof(_tunneling), null, ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [Tooltip("FOV animation curve for Low comfort level.")]
        private AnimationCurve _curveLow;
        [SerializeField, ConditionalHide(nameof(_tunneling), null, ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [Tooltip("FOV animation curve for Medium comfort level.")]
        private AnimationCurve _curveMedium;
        [SerializeField, ConditionalHide(nameof(_tunneling), null, ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [Tooltip("FOV animation curve for High comfort level.")]
        private AnimationCurve _curveHigh;

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            if (_tunneling != null)
            {
                this.AssertField(_curveOff, nameof(_curveOff));
                this.AssertField(_curveLow, nameof(_curveLow));
                this.AssertField(_curveMedium, nameof(_curveMedium));
                this.AssertField(_curveHigh, nameof(_curveHigh));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                ComfortLevel.WhenChanged += HandleComfortChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                ComfortLevel.WhenChanged -= HandleComfortChanged;
            }
        }

        private void HandleComfortChanged(ComfortAssistance comfortLevel)
        {
            if (_tunneling == null)
            {
                return;
            }
            AnimationCurve curve = _curveMedium;
            switch (comfortLevel)
            {
                case ComfortAssistance.Off:
                    curve = _curveOff; break;
                case ComfortAssistance.Low:
                    curve = _curveLow; break;
                case ComfortAssistance.Medium:
                    curve = _curveMedium; break;
                case ComfortAssistance.High:
                    curve = _curveHigh; break;
            }

            switch (_comfortType)
            {
                case ComfortType.Turning:
                    _tunneling.RotationStrength = curve;
                    break;
                case ComfortType.Accelerating:
                    _tunneling.AccelerationStrength = curve;
                    break;
                case ComfortType.Moving:
                    _tunneling.MovementStrength = curve;
                    break;
            }
        }

        #region Injects

        public void InjectOptionalTunneling(LocomotionTunneling tunneling)
        {
            _tunneling = tunneling;
        }

        public void InjectOptionalCurveOff(AnimationCurve curveOff)
        {
            _curveOff = curveOff;
        }

        public void InjectOptionalCurveLow(AnimationCurve curveLow)
        {
            _curveLow = curveLow;
        }

        public void InjectOptionalCurveMedium(AnimationCurve curveMedium)
        {
            _curveMedium = curveMedium;
        }

        public void InjectOptionalCurveHigh(AnimationCurve curveHigh)
        {
            _curveHigh = curveHigh;
        }

        #endregion
    }
}
