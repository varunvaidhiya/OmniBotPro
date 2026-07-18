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
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Oculus.Interaction.Samples
{
    public class AnimatorOverrideLayerWeight : MonoBehaviour, ITimeConsumer
    {
        [SerializeField]
        [FormerlySerializedAs("animator")]
        private Animator _animator;

        [SerializeField]
        [FormerlySerializedAs("overrideLayer")]
        private string _overrideLayer = "Selected Layer";

        [SerializeField]
        [FormerlySerializedAs("transitionDuration")]
        public float _transitionDuration = 0.2f;
        public float TransitionDuration
        {
            get => _transitionDuration;
            set => _transitionDuration = value;
        }

        [SerializeField]
        [FormerlySerializedAs("transitionCurve")]
        public AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve TransitionCurve
        {
            get => _transitionCurve;
            set => _transitionCurve = value;
        }

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("If provided, the animation layer will be syncronized with the isOn state of the toggle")]
        public Toggle _toggle;

        private bool _layerIsActive = false;
        private int _layerIndex = -1;
        protected bool _started;
        private Func<float> _timeProvider = () => Time.time;

        public void SetTimeProvider(Func<float> timeProvider) => _timeProvider = timeProvider;

        #region Editor events

        protected virtual void Reset()
        {
            _animator = this.GetComponent<Animator>();
            _toggle = this.GetComponent<Toggle>();
        }

        #endregion

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_animator, nameof(_animator));
            this.AssertField(_transitionCurve, nameof(_transitionCurve));

            _layerIndex = _animator.GetLayerIndex(_overrideLayer);

            this.AssertIsTrue(_layerIndex >= 0,
                whyItFailed: $"The Override Layer {_overrideLayer} could not be found in the Animator.",
                howToFix: $"Ensure you provide a layer that exists in the Animator");

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                if (_layerIsActive)
                {
                    //After disabling the component. The state of the layers of the Animator is reset
                    //ensure we restore it accordingly.
                    _animator.SetLayerWeight(_layerIndex, 1.0f);
                }

                if (_toggle != null)
                {
                    _toggle.onValueChanged.AddListener(SetOverrideLayerActive);
                    SetOverrideLayerActive(_toggle.isOn);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_toggle != null)
                {
                    _toggle.onValueChanged.RemoveListener(SetOverrideLayerActive);
                }
            }
        }

        public void SetOverrideLayerActive(bool active)
        {
            _layerIsActive = active;

            if (!_started)
            {
                // OnEnable will correctly set the state of the layer if this was called before Start.
                return;
            }

            if (_transitionDuration > 0.0)
            {
                StopAllCoroutines();
                StartCoroutine(LayerTransition(_layerIndex, active ? 1.0f : 0.0f));
            }
            else
            {
                _animator.SetLayerWeight(_layerIndex, active ? 1.0f : 0.0f);
            }
        }

        private IEnumerator LayerTransition(int layerIndex, float targetWeight)
        {
            float startTime = _timeProvider();
            float startWeight = _animator.GetLayerWeight(layerIndex);
            while (true)
            {
                float transitionTime = (_timeProvider() - startTime) / _transitionDuration;
                float targetWeightParam = _transitionCurve.Evaluate(Mathf.Clamp01(transitionTime));
                float weigth = Mathf.Lerp(startWeight, targetWeight, targetWeightParam);
                _animator.SetLayerWeight(layerIndex, weigth);

                if (transitionTime >= 1.0)
                {
                    yield break;
                }

                yield return null;
            }
        }

        #region Inject

        [Obsolete("Use InjectAllAnimatorOverrideLayerWeight instead", true)]
        public void InjectAllAnimatorOverrideLayerWeigth(Animator animator, string overrideLayer)
        {
            InjectAnimator(animator);
            InjectOverrideLayer(overrideLayer);
        }

        public void InjectAllAnimatorOverrideLayerWeight(Animator animator, string overrideLayer)
        {
            InjectAnimator(animator);
            InjectOverrideLayer(overrideLayer);
        }

        public void InjectAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void InjectOverrideLayer(string overrideLayer)
        {
            _overrideLayer = overrideLayer;
        }
        public void InjectOptionalToggle(Toggle toggle)
        {
            _toggle = toggle;
        }

        #endregion
    }

    [Obsolete("Use " + nameof(AnimatorOverrideLayerWeight) + " instead", true)]
    public class AnimatorOverrideLayerWeigth : AnimatorOverrideLayerWeight { }
}
