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

namespace Oculus.Interaction.Input
{
    public class ControllerAnimatedHand : MonoBehaviour,
        IDeltaTimeConsumer
    {
        public enum AllowThumbUp
        {
            Always,
            GripRequired,
            TriggerAndGripRequired,
        }

        [SerializeField, Interface(typeof(IController))]
        private UnityEngine.Object _controller;
        private IController Controller;

        [SerializeField]
        private Animator _animator = null;

        [SerializeField]
        [Tooltip("Indicates the input needed in order to perform a thumbs-up when the fist is closed")]
        private AllowThumbUp _allowThumbUp = AllowThumbUp.TriggerAndGripRequired;
        public AllowThumbUp AllowThumbUpMode
        {
            get => _allowThumbUp;
            set => _allowThumbUp = value;
        }

        [Header("Animation Speed")]
        [SerializeField]
        [Tooltip("Speed of the index flex animation")]
        private float _animFlexGain = 35;
        public float AnimFlexGain
        {
            get => _animFlexGain;
            set => _animFlexGain = value;
        }

        [SerializeField]
        [Tooltip("Speed of the pinch animation")]
        private float _animPinchGain = 35;
        public float AnimPinchGain
        {
            get => _animPinchGain;
            set => _animPinchGain = value;
        }

        [SerializeField]
        [Tooltip("Speed of the point, slide and thumbs up animation")]
        private float _animPointAndThumbsUpGain = 20;
        public float AnimPointAndThumbsUpGain
        {
            get => _animPointAndThumbsUpGain;
            set => _animPointAndThumbsUpGain = value;
        }

        public Func<float> DeltaTimeProvider
        {
            get; set;
        } = () => Time.deltaTime;

        private const string ANIM_LAYER_NAME_POINT = "Point Layer";
        private const string ANIM_LAYER_NAME_THUMB = "Thumb Layer";
        private const string ANIM_PARAM_NAME_FLEX = "Flex";
        private const string ANIM_PARAM_NAME_PINCH = "Pinch";
        private const string ANIM_PARAM_NAME_INDEX_SLIDE = "IndexSlide";

        private const float TRIGGER_MAX = 0.95f;

        private int _animLayerIndexThumb = -1;
        private int _animLayerIndexPoint = -1;
        private int _animParamIndexFlex = Animator.StringToHash(ANIM_PARAM_NAME_FLEX);
        private int _animParamPinch = Animator.StringToHash(ANIM_PARAM_NAME_PINCH);
        private int _animParamIndexSlide = Animator.StringToHash(ANIM_PARAM_NAME_INDEX_SLIDE);

        private bool _isGivingThumbsUp = false;
        private float _pointBlend = 0.0f;
        private float _slideBlend = 0.0f;

        private float _thumbsUpBlend = 0.0f;
        private float _pointTarget = 0.0f;
        private float _slideTarget = 0.0f;

        private float _animFlex = 0;
        private float _animPinch = 0;

        private bool _started = false;

        private Func<float> _deltaTimeProvider = () => Time.deltaTime;
        public void SetDeltaTimeProvider(Func<float> deltaTimeProvider)
        {
            _deltaTimeProvider = deltaTimeProvider;
        }

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Controller, nameof(_controller));
            _animLayerIndexPoint = _animator.GetLayerIndex(ANIM_LAYER_NAME_POINT);
            _animLayerIndexThumb = _animator.GetLayerIndex(ANIM_LAYER_NAME_THUMB);
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            UpdateCapTouchStates();

            _pointBlend = Mathf.Lerp(_pointBlend, _pointTarget, _animPointAndThumbsUpGain * _deltaTimeProvider());
            _slideBlend = Mathf.Lerp(_slideBlend, _slideTarget, _animPointAndThumbsUpGain * _deltaTimeProvider());
            _thumbsUpBlend = Mathf.Lerp(_thumbsUpBlend, _isGivingThumbsUp ? 1 : 0, _animPointAndThumbsUpGain * _deltaTimeProvider());

            UpdateAnimStates();
        }

        private void UpdateCapTouchStates()
        {
            float trigger = Controller.ControllerInput.Trigger;
            float grip = Controller.ControllerInput.Grip;
            bool thumbDown = 0 != (Controller.ControllerInput.ButtonUsageMask &
                (ControllerButtonUsage.PrimaryButton
                | ControllerButtonUsage.SecondaryButton
                | ControllerButtonUsage.PrimaryTouch
                | ControllerButtonUsage.SecondaryTouch
                | ControllerButtonUsage.Thumbrest));

            bool triggerThumbsUp = _allowThumbUp == AllowThumbUp.Always ||
                (_allowThumbUp == AllowThumbUp.GripRequired
                    && grip >= TRIGGER_MAX) ||
                (_allowThumbUp == AllowThumbUp.TriggerAndGripRequired
                    && grip >= TRIGGER_MAX
                    && trigger >= TRIGGER_MAX);

            _isGivingThumbsUp = triggerThumbsUp && !thumbDown;
            _pointTarget = 1f - trigger;
            _slideTarget = 0f;
        }

        private void UpdateAnimStates()
        {
            // Flex
            // blend between open hand and fully closed fist
            float flex = Controller.ControllerInput.Grip;
            _animFlex = Mathf.Lerp(_animFlex, flex, _animFlexGain * DeltaTimeProvider());
            _animator.SetFloat(_animParamIndexFlex, _animFlex);

            // Pinch
            float pinchAmount = Controller.ControllerInput.Trigger;
            _animPinch = Mathf.Lerp(_animPinch, pinchAmount, _animPinchGain * DeltaTimeProvider());
            _animator.SetFloat(_animParamPinch, _animPinch);

            // Point
            _animator.SetLayerWeight(_animLayerIndexPoint, _pointBlend);
            _animator.SetFloat(_animParamIndexSlide, _slideBlend);

            // Thumbs up
            _animator.SetLayerWeight(_animLayerIndexThumb, _thumbsUpBlend);
        }

        #region Inject

        public void InjectAllControllerAnimatedHand(IController controller, Animator animator)
        {
            InjectController(controller);
            InjectAnimator(animator);
        }

        public void InjectController(IController controller)
        {
            _controller = controller as UnityEngine.Object;
            Controller = controller;
        }

        public void InjectAnimator(Animator animator)
        {
            _animator = animator;
        }

        #endregion
    }
}
