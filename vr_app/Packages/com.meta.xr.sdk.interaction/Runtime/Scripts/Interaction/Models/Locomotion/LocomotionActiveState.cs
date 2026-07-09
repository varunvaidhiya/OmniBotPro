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
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Keeps an ActiveState active if it receives any locomotion event. It becomes false
    /// after IdleTime seconds without receiving any events, in order to prevent false deactivations
    /// due to the continous nature of sliding locomotion events.
    /// </summary>
    public class LocomotionActiveState : MonoBehaviour,
        IActiveState, ITimeConsumer
    {
        [SerializeField, Interface(typeof(ILocomotionEventBroadcaster))]
        private UnityEngine.Object _locomotionBroadcaster;
        private ILocomotionEventBroadcaster LocomotionBroadcaster { get; set; }

        [SerializeField]
        private float _idleTime = 0.1f;
        public float IdleTime
        {
            get => _idleTime;
            set => _idleTime = value;
        }

        private Func<float> _timeProvider = () => Time.time;
        public void SetTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public bool Active { get; private set; }

        private float _lastEventTime = 0;
        protected bool _started = false;

        protected void Awake()
        {
            if (LocomotionBroadcaster == null)
            {
                LocomotionBroadcaster = _locomotionBroadcaster as ILocomotionEventBroadcaster;
            }
        }

        protected void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(LocomotionBroadcaster, nameof(_locomotionBroadcaster));
            this.EndStart(ref _started);
        }

        protected void OnEnable()
        {
            if (_started)
            {
                LocomotionBroadcaster.WhenLocomotionPerformed += HandleLocomotionPerformed;
            }
        }

        protected void OnDisable()
        {
            if (_started)
            {
                Active = false;
                LocomotionBroadcaster.WhenLocomotionPerformed -= HandleLocomotionPerformed;
            }
        }

        protected void Update()
        {
            if (Active
                && _timeProvider.Invoke() - _lastEventTime > _idleTime)
            {
                Active = false;
            }
        }

        private void HandleLocomotionPerformed(LocomotionEvent obj)
        {
            if (obj.Translation != LocomotionEvent.TranslationType.None
                || obj.Rotation != LocomotionEvent.RotationType.None)
            {
                _lastEventTime = _timeProvider.Invoke();
                Active = true;
            }
        }
    }
}
