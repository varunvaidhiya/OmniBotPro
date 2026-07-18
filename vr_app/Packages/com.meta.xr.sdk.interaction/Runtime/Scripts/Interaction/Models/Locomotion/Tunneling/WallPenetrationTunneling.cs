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
    /// This component will check the ray from the character logical head to the
    /// actual player tracked head for collisions. If there is a collider between these
    /// two positions it means a wall or other occluding obstacle has been physically traspased
    /// and will apply a tunneling effect in the player's view that point them towards the exit of
    /// the collision.
    /// </summary>
    public class WallPenetrationTunneling : MonoBehaviour
    {
        /// <summary>
        /// The actual tracked position of the player head
        /// </summary>
        [SerializeField]
        private Transform _trackedPosition;
        /// <summary>
        /// The constrained position of the player head
        /// </summary>
        [SerializeField]
        private Transform _logicalPosition;
        /// <summary>
        /// The tunneling effect that will apply the result
        /// of this component
        /// </summary>
        [SerializeField]
        private TunnelingEffect _tunneling;
        /// <summary>
        /// Curve indicating what would be the FOV (in degrees)
        /// based on the penetration (in meters)
        /// </summary>
        [SerializeField]
        private AnimationCurve _penetrationFov;
        public AnimationCurve PenetrationFov
        {
            get => _penetrationFov;
            set => _penetrationFov = value;
        }
        /// <summary>
        /// Extra distance to check past the tracked head center for walls.
        /// </summary>
        [SerializeField]
        private float _extraDistance = 0.22f;
        public float ExtraDistance
        {
            get => _extraDistance;
            set => _extraDistance = value;
        }
        /// <summary>
        /// Max number of colliders to check when using Physics.RaycastNonAlloc
        /// </summary>
        [SerializeField, Min(1)]
        private int _maxCollidersCheck = 5;
        /// <summary>
        /// Optional. If check, colliders with this tag will be skip
        /// </summary>
        [SerializeField, Optional]
        private string _ignoreTag = "Player";
        public string IgnoreTag
        {
            get => _ignoreTag;
            set => _ignoreTag = value;
        }
        /// <summary>
        /// Physics layers to check for collisions
        /// </summary>
        [SerializeField]
        private LayerMask _layerMask = -1;
        public LayerMask LayerMask
        {
            get => _layerMask;
            set => _layerMask = value;
        }

        private RaycastHit[] _hits;

        protected bool _started = false;

        protected virtual void Awake()
        {
            _hits = new RaycastHit[_maxCollidersCheck];
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_trackedPosition, nameof(_trackedPosition));
            this.AssertField(_logicalPosition, nameof(_logicalPosition));
            this.AssertField(_tunneling, nameof(_tunneling));
            this.AssertIsTrue(_maxCollidersCheck > 0);
            this.AssertField(_penetrationFov, nameof(_penetrationFov));
            this.EndStart(ref _started);
        }

        protected virtual void LateUpdate()
        {
            bool headBlocked = CalculatePenetration(out float distance);
            UpdateTunneling(headBlocked, distance);
        }

        private bool CalculatePenetration(out float distance)
        {
            Vector3 origin = _logicalPosition.position;
            Vector3 direction = _trackedPosition.position - origin;
            float length = direction.magnitude + _extraDistance;

            Ray ray = new Ray(origin, direction.normalized);
            int hitsCount = Physics.RaycastNonAlloc(ray, _hits, length, _layerMask.value);
            if (hitsCount > 0)
            {
                if (string.IsNullOrEmpty(_ignoreTag))
                {
                    distance = Mathf.Max(0f, length - _hits[0].distance);
                    return true;
                }
                else
                {
                    for (int i = 0; i < hitsCount; i++)
                    {
                        if (_ignoreTag != _hits[i].collider.tag)
                        {
                            distance = Mathf.Max(0f, length - _hits[i].distance);
                            return true;
                        }
                    }
                }
            }
            distance = 0f;
            return false;
        }

        private void UpdateTunneling(bool headBlocked, float penetrationDistance)
        {
            float fov = _penetrationFov.Evaluate(penetrationDistance);
            if (!headBlocked || fov >= 360f)
            {
                _tunneling.enabled = false;
                _tunneling.UserFOV = 360f;
                return;
            }

            Vector3 penetrationDirection = (_logicalPosition.position - _trackedPosition.position).normalized;
            _tunneling.enabled = true;
            _tunneling.UseAimingTarget = true;
            _tunneling.AimingDirection = penetrationDirection;
            _tunneling.UserFOV = fov;
        }

        #region Injects

        public void InjectAllWallPenetrationTunneling(Transform trackedPosition, Transform logicalPosition,
            TunnelingEffect tunneling, int maxCollidersCheck)
        {
            InjectTrackedPosition(trackedPosition);
            InjectLogicalPosition(logicalPosition);
            InjectTunneling(tunneling);
            InjectMaxCollidersCheck(maxCollidersCheck);
        }

        public void InjectTrackedPosition(Transform trackedPosition)
        {
            _trackedPosition = trackedPosition;
        }

        public void InjectLogicalPosition(Transform logicalPosition)
        {
            _logicalPosition = logicalPosition;
        }

        public void InjectTunneling(TunnelingEffect tunneling)
        {
            _tunneling = tunneling;
        }

        public void InjectMaxCollidersCheck(int maxCollidersCheck)
        {
            _maxCollidersCheck = maxCollidersCheck;
        }

        #endregion
    }
}
