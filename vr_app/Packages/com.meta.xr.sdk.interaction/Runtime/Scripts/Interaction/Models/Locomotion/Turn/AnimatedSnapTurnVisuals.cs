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
using System.Collections;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Drives the visuals of a TurnArrowVisuals when a Rotation.Relative
    /// event is detected from a ILocomotionEventBroadcaster.
    /// </summary>
    public class AnimatedSnapTurnVisuals : MonoBehaviour,
        ITimeConsumer
    {
        /// <summary>
        /// Actual visuals to be driven
        /// </summary>
        [SerializeField]
        private TurnArrowVisuals _visuals;
        /// <summary>
        /// LocomotionBroadcaster that will produce the turning event
        /// </summary>
        [SerializeField, Interface(typeof(ILocomotionEventBroadcaster))]
        private UnityEngine.Object _locomotionEventBroadcaster;
        private ILocomotionEventBroadcaster LocomotionEventBroadcaster { get; set; }
        /// <summary>
        /// Animation of the Value for the visuals when a turn is detected
        /// </summary>
        [SerializeField]
        private AnimationCurve _animation;
        public AnimationCurve Animation
        {
            get => _animation;
            set => _animation = value;
        }
        /// <summary>
        /// When the animation value reaches this offset the Visual
        /// is highlighted
        /// </summary>
        [SerializeField]
        private float _highlightOffset = 0.8f;
        public float HighlightOffset
        {
            get => _highlightOffset;
            set => _highlightOffset = value;
        }

        private Func<float> _timeProvider = () => Time.time;
        public void SetTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        private float _progressValue = 0f;
        private Coroutine _animationRoutine;

        protected bool _started;

        protected virtual void Awake()
        {
            LocomotionEventBroadcaster = _locomotionEventBroadcaster as ILocomotionEventBroadcaster;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_visuals, nameof(_visuals));
            this.AssertField(LocomotionEventBroadcaster, nameof(_locomotionEventBroadcaster));
            this.AssertField(_animation, nameof(_animation));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                LocomotionEventBroadcaster.WhenLocomotionPerformed += HandleLocomotionPerformed;

                _visuals.Progress = 0f;
                _visuals.Value = 0f;
                _visuals.HighLight = false;
                _visuals.UpdateVisual();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                LocomotionEventBroadcaster.WhenLocomotionPerformed -= HandleLocomotionPerformed;
            }
        }

        private void HandleLocomotionPerformed(LocomotionEvent ev)
        {
            if (ev.Rotation == LocomotionEvent.RotationType.Relative)
            {
                StopAnimation();

                float direction = Mathf.Repeat(ev.Pose.rotation.eulerAngles.y, 360f) < 180f ? 1f : -1f;
                _animationRoutine = StartCoroutine(AnimationRoutine(direction));
            }
        }

        private void StopAnimation()
        {
            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }
        }

        private IEnumerator AnimationRoutine(float direction)
        {
            float totalTime = _animation.keys[_animation.keys.Length - 1].time;
            float startTime = _timeProvider.Invoke();
            float ellapsedTime = 0f;

            _visuals.Progress = 0f;
            _visuals.Value = direction;
            _visuals.HighLight = false;
            _visuals.UpdateVisual();

            while (ellapsedTime < totalTime)
            {
                _visuals.Progress = _animation.Evaluate(ellapsedTime);
                _visuals.HighLight = _progressValue > 0.8f;
                ellapsedTime = _timeProvider.Invoke() - startTime;
                _visuals.UpdateVisual();
                yield return null;
            }

            _visuals.Progress = 0f;
            _visuals.Value = 0f;
            _visuals.HighLight = false;
            _visuals.UpdateVisual();
        }

        #region Injects

        public void InjectAllAnimatedSnapTurnVisuals(TurnArrowVisuals visuals, ILocomotionEventBroadcaster locomotionEventBroadcaster)
        {
            InjectVisuals(visuals);
            InjectLocomotionEventBroadcaster(locomotionEventBroadcaster);
        }

        public void InjectVisuals(TurnArrowVisuals visuals)
        {
            _visuals = visuals;
        }

        public void InjectLocomotionEventBroadcaster(ILocomotionEventBroadcaster locomotionEventBroadcaster)
        {
            LocomotionEventBroadcaster = locomotionEventBroadcaster;
            _locomotionEventBroadcaster = locomotionEventBroadcaster as UnityEngine.Object;
        }
        #endregion
    }
}
