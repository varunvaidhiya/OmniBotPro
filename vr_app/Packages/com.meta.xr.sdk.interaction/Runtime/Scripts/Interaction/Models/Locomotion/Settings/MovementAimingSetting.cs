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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Controls aiming style (head or hand) and enables/disables the corresponding aiming components.
    /// </summary>
    public class MovementAimingSetting : MonoBehaviour
    {
        /// <summary>
        /// Aiming direction source for locomotion.
        /// </summary>
        public enum AimingStyle
        {
            Facing = 0,
            Hand = 1,
        }

        [SerializeField]
        [LocomotionSetting]
        [Tooltip("Aiming direction based on head facing or hand orientation.")]
        private ReactiveValue<AimingStyle> _aiming;
        /// <summary>Aiming direction based on head facing or hand orientation.</summary>
        public ReactiveValue<AimingStyle> Aiming => _aiming;

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("Components enabled when hand aiming is active.")]
        private List<MonoBehaviour> _handAimingComponents;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("Components enabled when head aiming is active.")]
        private List<MonoBehaviour> _headAimingComponents;

        protected bool _started = false;

        protected void Start()
        {
            this.BeginStart(ref _started);
            if (_handAimingComponents != null)
            {
                this.AssertCollectionItems(_handAimingComponents, nameof(_handAimingComponents));
            }
            if (_headAimingComponents != null)
            {
                this.AssertCollectionItems(_headAimingComponents, nameof(_headAimingComponents));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Aiming.WhenChanged += HandleAimingChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Aiming.WhenChanged -= HandleAimingChanged;
            }
        }

        private void HandleAimingChanged(AimingStyle aiming)
        {
            _handAimingComponents?.ForEach(c => c.enabled = aiming == AimingStyle.Hand);
            _headAimingComponents?.ForEach(c => c.enabled = aiming == AimingStyle.Facing);
        }

        #region Injects

        public void InjectOptionalHandAimingComponents(List<MonoBehaviour> handAimingComponents)
        {
            _handAimingComponents = handAimingComponents;
        }

        public void InjectOptionalHeadAimingComponents(List<MonoBehaviour> headAimingComponents)
        {
            _headAimingComponents = headAimingComponents;
        }

        #endregion
    }
}
