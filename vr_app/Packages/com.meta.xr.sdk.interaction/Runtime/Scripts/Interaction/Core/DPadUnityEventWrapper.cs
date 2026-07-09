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

using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Interaction
{
    /// <summary>
    /// Translates Axis2D events into D-Pad events (Left, Right, Up, Down)
    /// and outputs them as UnityEvents. Events will be fired as soon as the
    /// axis exits the deadzone.
    /// </summary>
    public class DPadUnityEventWrapper : MonoBehaviour
    {
        /// <summary>
        /// Axis to detect and transform into events
        /// </summary>
        [SerializeField, Interface(typeof(IAxis2D))]
        private UnityEngine.Object _axis;
        private IAxis2D Axis { get; set; }

        /// <summary>
        /// Absolute value to detect the Axis event as triggered
        /// </summary>
        [SerializeField]
        private float _positiveDeadZone = 0.9f;
        public float PositiveDeadZone
        {
            get => _positiveDeadZone;
            set => _positiveDeadZone = value;
        }

        /// <summary>
        /// Absolute value to detect the Axis event as released
        /// </summary>
        [SerializeField]
        private float _negativeDeadZone = 0.5f;
        public float NegativeDeadZone
        {
            get => _negativeDeadZone;
            set => _negativeDeadZone = value;
        }

        /// <summary>
        /// Event invoked when the axis2D value is left
        /// </summary>
        [SerializeField]
        private UnityEvent _whenPressLeft;
        /// <summary>
        /// Event invoked when the axis2D value is right
        /// </summary>
        [SerializeField]
        private UnityEvent _whenPressRight;
        /// <summary>
        /// Event invoked when the axis2D value is up
        /// </summary>
        [SerializeField]
        private UnityEvent _whenPressUp;
        /// <summary>
        /// Event invoked when the axis2D value is down
        /// </summary>
        [SerializeField]
        private UnityEvent _whenPressDown;


        public UnityEvent WhenPressLeft => _whenPressLeft;
        public UnityEvent WhenPressRight => _whenPressRight;
        public UnityEvent WhenPressUp => _whenPressUp;
        public UnityEvent WhenPressDown => _whenPressDown;

        protected bool _started = false;

        private Vector2Int _lastDirection = Vector2Int.zero;

        protected virtual void Awake()
        {
            Axis = _axis as IAxis2D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Axis, nameof(_axis));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _lastDirection = Vector2Int.zero;
            }
        }

        protected virtual void Update()
        {
            Vector2Int direction = AxisToDPadDirection(Axis.Value());

            if (_lastDirection != direction)
            {
                _lastDirection = direction;

                if (direction.x < 0)
                {
                    _whenPressLeft.Invoke();
                }
                if (direction.x > 0)
                {
                    _whenPressRight.Invoke();
                }
                if (direction.y < 0)
                {
                    _whenPressDown.Invoke();
                }
                if (direction.y > 0)
                {
                    _whenPressUp.Invoke();
                }

            }
        }

        private Vector2Int AxisToDPadDirection(Vector2 axisValue)
        {
            Vector2Int direction = Vector2Int.zero;
            if (Mathf.Abs(axisValue.x) > _positiveDeadZone
                && Mathf.Abs(axisValue.y) < _negativeDeadZone)
            {
                direction.x = axisValue.x >= 0 ? 1 : -1;
            }

            if (Mathf.Abs(axisValue.y) > _positiveDeadZone
                && Mathf.Abs(axisValue.x) < _negativeDeadZone)
            {
                direction.y = axisValue.y >= 0 ? 1 : -1;
            }

            return direction;
        }

        #region Injects

        public void InjectAllDPadUnityEventWrapper(IAxis2D axis)
        {
            InjectAxis(axis);
        }

        public void InjectAxis(IAxis2D axis)
        {
            Axis = axis;
            _axis = axis as UnityEngine.Object;
        }

        #endregion
    }
}
