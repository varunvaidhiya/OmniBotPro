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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Utility classes for binding UI elements to locomotion settings.
    /// </summary>
    public static class LocomotionSettingsUIUtilities
    {
        /// <summary>
        /// Defines a step in the rotation slider, mapping to snap angle and smooth velocity values.
        /// </summary>
        [Serializable]
        public struct RotationSliderStep
        {
            /// <summary>Snap rotation angle in degrees for this step.</summary>
            public float rotationSnapAngle;
            /// <summary>Smooth rotation velocity in degrees per second for this step.</summary>
            public float rotationSmoothVelocity;
        }

        /// <summary>
        /// Interface for UI bindings that can be registered and unregistered.
        /// </summary>
        public interface ISettingsUIBinding
        {
            /// <summary>Registers the binding, connecting UI events to values.</summary>
            void Register();
            /// <summary>Unregisters the binding, disconnecting UI events from values.</summary>
            void Unregister();
        }

        /// <summary>
        /// Binds a group of toggles to an enum-based ReactiveValue setting.
        /// Each toggle represents one enum value; selecting it updates the setting.
        /// </summary>
        /// <typeparam name="TSetting">The enum type for the setting.</typeparam>
        public class ToggleEnumBinding<TSetting> : ISettingsUIBinding
            where TSetting : struct, Enum
        {
            private ToggleBinding[] _bindings;

            public ToggleEnumBinding(ReactiveValue<TSetting> setting, params (Toggle toggle, TSetting value)[] bindings)
            {
                _bindings = bindings.Select(b => new ToggleBinding(b.toggle, b.value, setting)).ToArray();
            }

            public void Register()
            {
                foreach (ToggleBinding binding in _bindings)
                {
                    binding.Register();
                }
            }

            public void Unregister()
            {
                foreach (ToggleBinding binding in _bindings)
                {
                    binding.Unregister();
                }
            }

            private class ToggleBinding
            {
                private Toggle _toggle;
                private TSetting _value;
                private ReactiveValue<TSetting> _setting;

                public ToggleBinding(Toggle toggle, TSetting value, ReactiveValue<TSetting> setting)
                {
                    _toggle = toggle;
                    _value = value;
                    _setting = setting;
                }

                public void Register()
                {
                    _toggle.onValueChanged.AddListener(HandleToggleChanged);
                    _setting.WhenChanged += HandleSettingChanged;
                }

                public void Unregister()
                {
                    _toggle.onValueChanged.RemoveListener(HandleToggleChanged);
                    _setting.WhenChanged -= HandleSettingChanged;
                }

                private void HandleToggleChanged(bool isOn)
                {
                    if (isOn)
                    {
                        _setting.Value = _value;
                    }
                }

                private void HandleSettingChanged(TSetting setting)
                {
                    _toggle.isOn = EqualityComparer<TSetting>.Default.Equals(setting, _value);
                }
            }
        }

        /// <summary>
        /// Binds a slider to rotation settings, updating snap angle and smooth velocity
        /// based on predefined steps when the slider value changes.
        /// </summary>
        public class TurnSettingsBinding : ISettingsUIBinding
        {
            private Slider _slider;
            private ReactiveValue<float> _snapAngle;
            private ReactiveValue<float> _smoothVelocity;
            private List<RotationSliderStep> _turnSteps;

            public TurnSettingsBinding(Slider slider, ReactiveValue<float> snapAngle, ReactiveValue<float> smoothVelocity, List<RotationSliderStep> rotationSliderSteps)
            {
                _slider = slider;
                _snapAngle = snapAngle;
                _smoothVelocity = smoothVelocity;
                _turnSteps = rotationSliderSteps;
            }

            public void Register()
            {
                _slider.onValueChanged.AddListener(HandleTurnVelocityChanged);
                _snapAngle.WhenChanged += HandleSettingsRotationSnapAngleChanged;
            }

            public void Unregister()
            {
                _slider.onValueChanged.RemoveListener(HandleTurnVelocityChanged);
                _snapAngle.WhenChanged -= HandleSettingsRotationSnapAngleChanged;
            }

            private void HandleTurnVelocityChanged(float index)
            {
                int indexInt = Mathf.RoundToInt(index);
                RotationSliderStep turnStep = _turnSteps[indexInt];
                _snapAngle.Value = turnStep.rotationSnapAngle;
                _smoothVelocity.Value = turnStep.rotationSmoothVelocity;
            }

            private void HandleSettingsRotationSnapAngleChanged(float degrees)
            {
                int index = _turnSteps.FindIndex((step) => step.rotationSnapAngle == degrees);
                if (index >= 0)
                {
                    _slider.value = index;
                }
            }
        }

    }
}
