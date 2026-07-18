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
using UnityEngine.UI;

namespace Oculus.Interaction.Locomotion
{
    public class LocomotionSettingsUIController : MonoBehaviour
    {
        [SerializeField]
        private MovingSetting _movingSetting;
        [SerializeField]
        private TurningSetting _turningSetting;
        [SerializeField]
        private MovementAimingSetting _aimingSetting;
        [SerializeField]
        private StandingSetting _standingSetting;
        [SerializeField]
        private ComfortVignetteSetting _comfortMovingSetting;
        [SerializeField]
        private ComfortVignetteSetting _comfortTurningSetting;

        [Space]
        [Header("Movement Style")]
        [SerializeField]
        private Toggle _movementStyleSlide;
        [SerializeField]
        private Toggle _movementStyleTeleport;

        [Header("Slide Direction")]
        [SerializeField]
        private Toggle _slideDirectionFacing;
        [SerializeField]
        private Toggle _slideDirectionHand;

        [Header("Rotation Angle")]
        [SerializeField]
        private Slider _turnVelocity;
        [SerializeField]
        private List<RotationSliderStep> _turnSteps;
        [SerializeField]
        private Toggle _rotationSnap;
        [SerializeField]
        private Toggle _rotationSmooth;

        [Header("Comfort Turning")]
        [SerializeField]
        private Toggle _comfortAssistanceTurningOff;
        [SerializeField]
        private Toggle _comfortAssistanceTurningLow;
        [SerializeField]
        private Toggle _comfortAssistanceTurningMedium;
        [SerializeField]
        private Toggle _comfortAssistanceTurningHigh;

        [Header("Comfort Moving")]
        [SerializeField]
        private Toggle _comfortAssistanceMovingOff;
        [SerializeField]
        private Toggle _comfortAssistanceMovingLow;
        [SerializeField]
        private Toggle _comfortAssistanceMovingMedium;
        [SerializeField]
        private Toggle _comfortAssistanceMovingHigh;

        [Header("Seating")]
        [SerializeField]
        private Toggle _seating;
        [SerializeField]
        private Toggle _standing;

        [System.Serializable]
        public struct RotationSliderStep
        {
            public float rotationSnapAngle;
            public float rotationSmoothVelocity;
        }

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_movingSetting, nameof(_movingSetting));
            this.AssertField(_turningSetting, nameof(_turningSetting));
            this.AssertField(_aimingSetting, nameof(_aimingSetting));
            this.AssertField(_standingSetting, nameof(_standing));
            this.AssertField(_comfortMovingSetting, nameof(_comfortMovingSetting));
            this.AssertField(_comfortTurningSetting, nameof(_comfortTurningSetting));

            this.AssertField(_movementStyleSlide, nameof(_movementStyleSlide));
            this.AssertField(_movementStyleTeleport, nameof(_movementStyleTeleport));
            this.AssertField(_slideDirectionFacing, nameof(_slideDirectionFacing));
            this.AssertField(_slideDirectionHand, nameof(_slideDirectionHand));
            this.AssertField(_turnVelocity, nameof(_turnVelocity));
            this.AssertField(_rotationSnap, nameof(_rotationSnap));
            this.AssertField(_rotationSmooth, nameof(_rotationSmooth));
            this.AssertField(_comfortAssistanceTurningOff, nameof(_comfortAssistanceTurningOff));
            this.AssertField(_comfortAssistanceTurningLow, nameof(_comfortAssistanceTurningLow));
            this.AssertField(_comfortAssistanceTurningMedium, nameof(_comfortAssistanceTurningMedium));
            this.AssertField(_comfortAssistanceTurningHigh, nameof(_comfortAssistanceTurningHigh));
            this.AssertField(_comfortAssistanceMovingOff, nameof(_comfortAssistanceMovingOff));
            this.AssertField(_comfortAssistanceMovingLow, nameof(_comfortAssistanceMovingLow));
            this.AssertField(_comfortAssistanceMovingMedium, nameof(_comfortAssistanceMovingMedium));
            this.AssertField(_comfortAssistanceMovingHigh, nameof(_comfortAssistanceMovingHigh));
            this.AssertField(_seating, nameof(_seating));
            this.AssertField(_standing, nameof(_standing));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _movementStyleSlide.onValueChanged.AddListener(HandleMovementStyleSlideChanged);
                _movementStyleTeleport.onValueChanged.AddListener(HandleMovementStyleTeleportChanged);
                _slideDirectionFacing.onValueChanged.AddListener(HandleSlideDirectionFacingChanged);
                _slideDirectionHand.onValueChanged.AddListener(HandleSlideDirectionHandChanged);
                _turnVelocity.onValueChanged.AddListener(HandleTurnVelocityChanged);
                _rotationSnap.onValueChanged.AddListener(HandleRotationSnapChanged);
                _rotationSmooth.onValueChanged.AddListener(HandleRotationSmoothChanged);
                _comfortAssistanceTurningOff.onValueChanged.AddListener(HandleComfortTurningOffChanged);
                _comfortAssistanceTurningLow.onValueChanged.AddListener(HandleComfortTurningLowChanged);
                _comfortAssistanceTurningMedium.onValueChanged.AddListener(HandleComfortTurningMediumChanged);
                _comfortAssistanceTurningHigh.onValueChanged.AddListener(HandleComfortTurningHighChanged);
                _comfortAssistanceMovingOff.onValueChanged.AddListener(HandleComfortMovingOffChanged);
                _comfortAssistanceMovingLow.onValueChanged.AddListener(HandleComfortMovingLowChanged);
                _comfortAssistanceMovingMedium.onValueChanged.AddListener(HandleComfortMovingMediumChanged);
                _comfortAssistanceMovingHigh.onValueChanged.AddListener(HandleComfortMovingHighChanged);
                _seating.onValueChanged.AddListener(HandleSeatingChanged);
                _standing.onValueChanged.AddListener(HandleStandingChanged);

                _movingSetting.ControllerMovement.WhenChanged += HandleSettingsControllerMovementChanged;
                _aimingSetting.Aiming.WhenChanged += HandleSettingsAimingChanged;
                _turningSetting.ControllerTurn.WhenChanged += HandleSettingsControllerTurnChanged;
                _turningSetting.RotationSnapAngle.WhenChanged += HandleSettingsRotationSnapAngleChanged;
                _comfortMovingSetting.ComfortLevel.WhenChanged += HandleSettingsMovementComfortChanged;
                _comfortTurningSetting.ComfortLevel.WhenChanged += HandleSettingsTurningComfortChanged;
                _standingSetting.Standing.WhenChanged += HandleSettingsStandingChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _movementStyleSlide.onValueChanged.RemoveListener(HandleMovementStyleSlideChanged);
                _movementStyleTeleport.onValueChanged.RemoveListener(HandleMovementStyleTeleportChanged);
                _slideDirectionFacing.onValueChanged.RemoveListener(HandleSlideDirectionFacingChanged);
                _slideDirectionHand.onValueChanged.RemoveListener(HandleSlideDirectionHandChanged);
                _turnVelocity.onValueChanged.RemoveListener(HandleTurnVelocityChanged);
                _rotationSnap.onValueChanged.RemoveListener(HandleRotationSnapChanged);
                _rotationSmooth.onValueChanged.RemoveListener(HandleRotationSmoothChanged);
                _comfortAssistanceTurningOff.onValueChanged.RemoveListener(HandleComfortTurningOffChanged);
                _comfortAssistanceTurningLow.onValueChanged.RemoveListener(HandleComfortTurningLowChanged);
                _comfortAssistanceTurningMedium.onValueChanged.RemoveListener(HandleComfortTurningMediumChanged);
                _comfortAssistanceTurningHigh.onValueChanged.RemoveListener(HandleComfortTurningHighChanged);
                _comfortAssistanceMovingOff.onValueChanged.RemoveListener(HandleComfortMovingOffChanged);
                _comfortAssistanceMovingLow.onValueChanged.RemoveListener(HandleComfortMovingLowChanged);
                _comfortAssistanceMovingMedium.onValueChanged.RemoveListener(HandleComfortMovingMediumChanged);
                _comfortAssistanceMovingHigh.onValueChanged.RemoveListener(HandleComfortMovingHighChanged);
                _seating.onValueChanged.RemoveListener(HandleSeatingChanged);
                _standing.onValueChanged.RemoveListener(HandleStandingChanged);

                _movingSetting.ControllerMovement.WhenChanged -= HandleSettingsControllerMovementChanged;
                _aimingSetting.Aiming.WhenChanged -= HandleSettingsAimingChanged;
                _turningSetting.ControllerTurn.WhenChanged -= HandleSettingsControllerTurnChanged;
                _turningSetting.RotationSnapAngle.WhenChanged -= HandleSettingsRotationSnapAngleChanged;
                _comfortMovingSetting.ComfortLevel.WhenChanged -= HandleSettingsMovementComfortChanged;
                _comfortTurningSetting.ComfortLevel.WhenChanged -= HandleSettingsTurningComfortChanged;
                _standingSetting.Standing.WhenChanged -= HandleSettingsStandingChanged;
            }
        }

        #region UI handlers
        private void HandleMovementStyleSlideChanged(bool arg0)
        {
            if (arg0)
            {
                _movingSetting.ControllerMovement.Value = MovingSetting.MovementStyle.Slide;
            }
        }

        private void HandleMovementStyleTeleportChanged(bool arg0)
        {
            if (arg0)
            {
                _movingSetting.ControllerMovement.Value = MovingSetting.MovementStyle.Teleport;
            }
        }

        private void HandleSlideDirectionFacingChanged(bool arg0)
        {
            if (arg0)
            {
                _aimingSetting.Aiming.Value = MovementAimingSetting.AimingStyle.Facing;
            }
        }

        private void HandleSlideDirectionHandChanged(bool arg0)
        {
            if (arg0)
            {
                _aimingSetting.Aiming.Value = MovementAimingSetting.AimingStyle.Hand;
            }
        }

        private void HandleTurnVelocityChanged(float index)
        {
            int indexInt = Mathf.RoundToInt(index);
            RotationSliderStep turnStep = _turnSteps[indexInt];
            _turningSetting.RotationSnapAngle.Value = turnStep.rotationSnapAngle;
            _turningSetting.RotationSmoothVelocity.Value = turnStep.rotationSmoothVelocity;
        }

        private void HandleRotationSnapChanged(bool arg0)
        {
            if (arg0)
            {
                _turningSetting.ControllerTurn.Value = TurningSetting.RotationStyle.Snap;
            }
        }

        private void HandleRotationSmoothChanged(bool arg0)
        {
            if (arg0)
            {
                _turningSetting.ControllerTurn.Value = TurningSetting.RotationStyle.Smooth;
            }
        }

        private void HandleComfortTurningOffChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortTurningSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Off;
            }
        }

        private void HandleComfortTurningLowChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortTurningSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Low;
            }
        }

        private void HandleComfortTurningMediumChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortTurningSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Medium;
            }
        }

        private void HandleComfortTurningHighChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortTurningSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.High;
            }
        }

        private void HandleComfortMovingOffChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortMovingSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Off;
            }
        }

        private void HandleComfortMovingLowChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortMovingSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Low;
            }
        }

        private void HandleComfortMovingMediumChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortMovingSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Medium;
            }
        }

        private void HandleComfortMovingHighChanged(bool arg0)
        {
            if (arg0)
            {
                _comfortMovingSetting.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.High;
            }
        }

        private void HandleSeatingChanged(bool arg0)
        {
            if (arg0)
            {
                _standingSetting.Standing.Value = StandingSetting.StandingMode.Seating;
            }
        }

        private void HandleStandingChanged(bool arg0)
        {
            if (arg0)
            {
                _standingSetting.Standing.Value = StandingSetting.StandingMode.Standing;
            }
        }
        #endregion

        #region Settings handlers
        private void HandleSettingsControllerMovementChanged(MovingSetting.MovementStyle style)
        {
            switch (style)
            {
                case MovingSetting.MovementStyle.Slide: _movementStyleSlide.isOn = true; break;
                case MovingSetting.MovementStyle.Teleport: _movementStyleTeleport.isOn = true; break;
            }
        }

        private void HandleSettingsAimingChanged(MovementAimingSetting.AimingStyle style)
        {
            switch (style)
            {
                case MovementAimingSetting.AimingStyle.Facing: _slideDirectionFacing.isOn = true; break;
                case MovementAimingSetting.AimingStyle.Hand: _slideDirectionHand.isOn = true; break;
            }
        }

        private void HandleSettingsControllerTurnChanged(TurningSetting.RotationStyle style)
        {
            switch (style)
            {
                case TurningSetting.RotationStyle.Snap: _rotationSnap.isOn = true; break;
                case TurningSetting.RotationStyle.Smooth: _rotationSmooth.isOn = true; break;
            }
        }

        private void HandleSettingsRotationSnapAngleChanged(float degrees)
        {
            int index = _turnSteps.FindIndex((step) => step.rotationSnapAngle == degrees);
            if (index >= 0)
            {
                _turnVelocity.value = index;
            }
        }

        private void HandleSettingsMovementComfortChanged(ComfortVignetteSetting.ComfortAssistance assistance)
        {
            switch (assistance)
            {
                case ComfortVignetteSetting.ComfortAssistance.Off: _comfortAssistanceMovingOff.isOn = true; break;
                case ComfortVignetteSetting.ComfortAssistance.Low: _comfortAssistanceMovingLow.isOn = true; break;
                case ComfortVignetteSetting.ComfortAssistance.Medium: _comfortAssistanceMovingMedium.isOn = true; break;
                case ComfortVignetteSetting.ComfortAssistance.High: _comfortAssistanceMovingHigh.isOn = true; break;
            }
        }

        private void HandleSettingsTurningComfortChanged(ComfortVignetteSetting.ComfortAssistance assistance)
        {
            switch (assistance)
            {
                case ComfortVignetteSetting.ComfortAssistance.Off: _comfortAssistanceTurningOff.isOn = true; break;
                case ComfortVignetteSetting.ComfortAssistance.Low: _comfortAssistanceTurningLow.isOn = true; break;
                case ComfortVignetteSetting.ComfortAssistance.Medium: _comfortAssistanceTurningMedium.isOn = true; break;
                case ComfortVignetteSetting.ComfortAssistance.High: _comfortAssistanceTurningHigh.isOn = true; break;
            }
        }

        private void HandleSettingsStandingChanged(StandingSetting.StandingMode standing)
        {
            switch (standing)
            {
                case StandingSetting.StandingMode.Seating: _seating.isOn = true; break;
                case StandingSetting.StandingMode.Standing: _standing.isOn = true; break;
            }
        }

        #endregion

        #region Injects

        public void InjectAllLocomotionSettingsUIController(
            MovingSetting movingSetting,
            TurningSetting turningSetting,
            MovementAimingSetting aimingSetting,
            StandingSetting standingSetting,
            ComfortVignetteSetting comfortMovingSetting,
            ComfortVignetteSetting comfortTurningSetting,
            Toggle movementStyleSlide,
            Toggle movementStyleTeleport,
            Toggle slideDirectionFacing,
            Toggle slideDirectionHand,
            Slider turnVelocity,
            List<RotationSliderStep> turnSteps,
            Toggle rotationSnap,
            Toggle rotationSmooth,
            Toggle comfortAssistanceTurningOff,
            Toggle comfortAssistanceTurningLow,
            Toggle comfortAssistanceTurningMedium,
            Toggle comfortAssistanceTurningHigh,
            Toggle comfortAssistanceMovingOff,
            Toggle comfortAssistanceMovingLow,
            Toggle comfortAssistanceMovingMedium,
            Toggle comfortAssistanceMovingHigh,
            Toggle seating,
            Toggle standing)
        {
            InjectMovingSetting(movingSetting);
            InjectTurningSetting(turningSetting);
            InjectAimingSetting(aimingSetting);
            InjectStandingSetting(standingSetting);
            InjectComfortMovingSetting(comfortMovingSetting);
            InjectComfortTurningSetting(comfortTurningSetting);
            InjectMovementStyleSlide(movementStyleSlide);
            InjectMovementStyleTeleport(movementStyleTeleport);
            InjectSlideDirectionFacing(slideDirectionFacing);
            InjectSlideDirectionHand(slideDirectionHand);
            InjectTurnVelocity(turnVelocity);
            InjectTurnSteps(turnSteps);
            InjectRotationSnap(rotationSnap);
            InjectRotationSmooth(rotationSmooth);
            InjectComfortAssistanceTurningOff(comfortAssistanceTurningOff);
            InjectComfortAssistanceTurningLow(comfortAssistanceTurningLow);
            InjectComfortAssistanceTurningMedium(comfortAssistanceTurningMedium);
            InjectComfortAssistanceTurningHigh(comfortAssistanceTurningHigh);
            InjectComfortAssistanceMovingOff(comfortAssistanceMovingOff);
            InjectComfortAssistanceMovingLow(comfortAssistanceMovingLow);
            InjectComfortAssistanceMovingMedium(comfortAssistanceMovingMedium);
            InjectComfortAssistanceMovingHigh(comfortAssistanceMovingHigh);
            InjectSeating(seating);
            InjectStanding(standing);
        }

        public void InjectMovingSetting(MovingSetting movingSetting)
        {
            _movingSetting = movingSetting;
        }

        public void InjectTurningSetting(TurningSetting turningSetting)
        {
            _turningSetting = turningSetting;
        }

        public void InjectAimingSetting(MovementAimingSetting aimingSetting)
        {
            _aimingSetting = aimingSetting;
        }

        public void InjectStandingSetting(StandingSetting standingSetting)
        {
            _standingSetting = standingSetting;
        }

        public void InjectComfortMovingSetting(ComfortVignetteSetting comfortMovingSetting)
        {
            _comfortMovingSetting = comfortMovingSetting;
        }

        public void InjectComfortTurningSetting(ComfortVignetteSetting comfortTurningSetting)
        {
            _comfortTurningSetting = comfortTurningSetting;
        }

        public void InjectMovementStyleSlide(Toggle movementStyleSlide)
        {
            _movementStyleSlide = movementStyleSlide;
        }

        public void InjectMovementStyleTeleport(Toggle movementStyleTeleport)
        {
            _movementStyleTeleport = movementStyleTeleport;
        }

        public void InjectSlideDirectionFacing(Toggle slideDirectionFacing)
        {
            _slideDirectionFacing = slideDirectionFacing;
        }

        public void InjectSlideDirectionHand(Toggle slideDirectionHand)
        {
            _slideDirectionHand = slideDirectionHand;
        }

        public void InjectTurnVelocity(Slider turnVelocity)
        {
            _turnVelocity = turnVelocity;
        }

        public void InjectTurnSteps(List<RotationSliderStep> turnSteps)
        {
            _turnSteps = turnSteps;
        }

        public void InjectRotationSnap(Toggle rotationSnap)
        {
            _rotationSnap = rotationSnap;
        }

        public void InjectRotationSmooth(Toggle rotationSmooth)
        {
            _rotationSmooth = rotationSmooth;
        }

        public void InjectComfortAssistanceTurningOff(Toggle comfortAssistanceTurningOff)
        {
            _comfortAssistanceTurningOff = comfortAssistanceTurningOff;
        }

        public void InjectComfortAssistanceTurningLow(Toggle comfortAssistanceTurningLow)
        {
            _comfortAssistanceTurningLow = comfortAssistanceTurningLow;
        }

        public void InjectComfortAssistanceTurningMedium(Toggle comfortAssistanceTurningMedium)
        {
            _comfortAssistanceTurningMedium = comfortAssistanceTurningMedium;
        }

        public void InjectComfortAssistanceTurningHigh(Toggle comfortAssistanceTurningHigh)
        {
            _comfortAssistanceTurningHigh = comfortAssistanceTurningHigh;
        }

        public void InjectComfortAssistanceMovingOff(Toggle comfortAssistanceMovingOff)
        {
            _comfortAssistanceMovingOff = comfortAssistanceMovingOff;
        }

        public void InjectComfortAssistanceMovingLow(Toggle comfortAssistanceMovingLow)
        {
            _comfortAssistanceMovingLow = comfortAssistanceMovingLow;
        }

        public void InjectComfortAssistanceMovingMedium(Toggle comfortAssistanceMovingMedium)
        {
            _comfortAssistanceMovingMedium = comfortAssistanceMovingMedium;
        }

        public void InjectComfortAssistanceMovingHigh(Toggle comfortAssistanceMovingHigh)
        {
            _comfortAssistanceMovingHigh = comfortAssistanceMovingHigh;
        }

        public void InjectSeating(Toggle seating)
        {
            _seating = seating;
        }

        public void InjectStanding(Toggle standing)
        {
            _standing = standing;
        }

        #endregion
    }
}
