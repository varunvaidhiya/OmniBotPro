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
using UnityEngine.UI;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Changes the height offset of the Locomotor if the player indicates
    /// that it is seated so it can remaing at standing height while using
    /// the experience
    /// </summary>
    [System.Obsolete("Use " + nameof(StandingSetting) + " instead")]
    public class LocomotionSeatedSetting : MonoBehaviour
    {
        /// <summary>
        /// Toggle that indicate if the player is seated
        /// </summary>
        [SerializeField]
        private Toggle _seated;
        /// <summary>
        /// Toggle that indicates if the player is standing
        /// </summary>
        [SerializeField]
        private Toggle _standing;
        /// <summary>
        /// Locomotor to which apply the height offset if seated
        /// </summary>
        [SerializeField]
        private FirstPersonLocomotor _locomotor;
        /// <summary>
        /// Amount of height to apply if the user is seated
        /// </summary>
        [SerializeField]
        private float _seatedHeightOffset = 0.5f;
        public float SeatedHeightOffset
        {
            get => _seatedHeightOffset;
            set => _seatedHeightOffset = value;
        }

        protected bool _started = false;

        protected void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_seated, nameof(_seated));
            this.AssertField(_standing, nameof(_standing));
            this.AssertField(_locomotor, nameof(_locomotor));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _seated.onValueChanged.AddListener(HandleSeatedChanged);
                _standing.onValueChanged.AddListener(HandleStandingChanged);

                if (_standing.isOn)
                {
                    HandleStandingChanged(true);
                }
                else
                {
                    HandleSeatedChanged(true);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _seated.onValueChanged.RemoveListener(HandleSeatedChanged);
                _standing.onValueChanged.RemoveListener(HandleStandingChanged);
            }
        }

        private void HandleSeatedChanged(bool seated)
        {
            if (seated)
            {
                _locomotor.HeightOffset = _seatedHeightOffset;
            }
        }

        private void HandleStandingChanged(bool standing)
        {
            if (standing)
            {
                _locomotor.HeightOffset = 0f;
            }
        }

        #region Injects

        public void InjectAllSeatedMode(Toggle seated, Toggle standing, FirstPersonLocomotor locomotor)
        {
            InjectSeated(seated);
            InjectStanding(standing);
            InjectLocomotor(locomotor);
        }

        public void InjectSeated(Toggle seated)
        {
            _seated = seated;
        }

        public void InjectStanding(Toggle standing)
        {
            _standing = standing;
        }

        public void InjectLocomotor(FirstPersonLocomotor locomotor)
        {
            _locomotor = locomotor;
        }

        #endregion
    }
}
