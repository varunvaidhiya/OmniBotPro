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
    /// Adjusts the Locomotor height offset when the player is seated,
    /// allowing them to remain at standing height during the experience.
    /// </summary>
    public class StandingSetting : MonoBehaviour
    {
        public enum StandingMode
        {
            Standing = 0,
            Seating = 1,
        }

        [SerializeField]
        [LocomotionSetting]
        [Tooltip("Whether the player is standing or seated.")]
        private ReactiveValue<StandingMode> _standing;
        /// <summary>Whether the player is standing or seated.</summary>
        public ReactiveValue<StandingMode> Standing => _standing;

        [Space]
        [SerializeField, Optional]
        [Tooltip("Locomotor to apply the height offset when seated.")]
        private FirstPersonLocomotor _locomotor;

        [SerializeField]
        [Tooltip("Height offset in meters applied when the user is seated.")]
        private float _seatedHeightOffset = 0.5f;
        /// <summary>Height offset in meters applied when the user is seated.</summary>
        public float SeatedHeightOffset
        {
            get => _seatedHeightOffset;
            set => _seatedHeightOffset = value;
        }

        protected bool _started = false;

        protected void Start()
        {
            this.BeginStart(ref _started);

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Standing.WhenChanged += HandleStandingChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Standing.WhenChanged -= HandleStandingChanged;
            }
        }
        private void HandleStandingChanged(StandingMode standingMode)
        {
            if (_locomotor == null)
            {
                return;
            }

            if (standingMode == StandingMode.Standing)
            {
                _locomotor.HeightOffset = 0f;
            }
            else if (standingMode == StandingMode.Seating)
            {
                _locomotor.HeightOffset = _seatedHeightOffset;
            }
        }

        #region Injects

        public void InjectOptionalLocomotor(FirstPersonLocomotor locomotor)
        {
            _locomotor = locomotor;
        }

        #endregion
    }
}
