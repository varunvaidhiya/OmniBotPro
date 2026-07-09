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
    /// Manages movement style settings and toggles Slide/Teleport GameObjects.
    /// </summary>
    public class MovingSetting : MonoBehaviour
    {
        public enum MovementStyle
        {
            Slide = 0,
            Teleport = 1,
        }

        [SerializeField]
        [LocomotionSetting]
        [Tooltip("Slide or Teleport movement style for controllers.")]
        private ReactiveValue<MovementStyle> _controllerMovement;
        /// <summary>Slide or Teleport movement style for controllers.</summary>
        public ReactiveValue<MovementStyle> ControllerMovement => _controllerMovement;

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("GameObjects enabled when teleport movement is active.")]
        private List<GameObject> _teleportGameObjects;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("GameObjects enabled when slide movement is active.")]
        private List<GameObject> _slideGameObjects;

        protected bool _started = false;

        protected void Start()
        {
            this.BeginStart(ref _started);
            if (_teleportGameObjects != null)
            {
                this.AssertCollectionItems(_teleportGameObjects, nameof(_teleportGameObjects));
            }
            if (_slideGameObjects != null)
            {
                this.AssertCollectionItems(_slideGameObjects, nameof(_slideGameObjects));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                ControllerMovement.WhenChanged += HandleMovingChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                ControllerMovement.WhenChanged -= HandleMovingChanged;
            }
        }

        private void HandleMovingChanged(MovementStyle moving)
        {
            //First disable all irrelevant, then enable relevant
            //This avoids undesired deactivations if a gameobject is in multiple lists
            switch (moving)
            {
                case MovementStyle.Slide:
                    _teleportGameObjects?.ForEach(c => c.SetActive(false));
                    _slideGameObjects?.ForEach(c => c.SetActive(true));
                    break;
                case MovementStyle.Teleport:
                    _slideGameObjects?.ForEach(c => c.SetActive(false));
                    _teleportGameObjects?.ForEach(c => c.SetActive(true));
                    break;
            }
        }

        #region Injects

        public void InjectOptionalTeleportGameObjects(List<GameObject> teleportGameObjects)
        {
            _teleportGameObjects = teleportGameObjects;
        }

        public void InjectOptionalSlideGameObjects(List<GameObject> slideGameObjects)
        {
            _slideGameObjects = slideGameObjects;
        }

        #endregion
    }
}
