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
using Oculus.Interaction.Locomotion;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction.DistanceReticles
{
    public class TeleportReticleDrawer : InteractorReticle<ReticleDataTeleport>
    {
        [SerializeField]
        private TeleportInteractor _interactor;

        [SerializeField]
        [FormerlySerializedAs("_validTargetRenderer")]
        private Renderer _targetRenderer;

        [SerializeField, Optional(OptionalAttribute.Flag.Obsolete)]
        [Obsolete("This renderer is not in use")]
        private Renderer _invalidTargetRenderer;

        [SerializeField, Optional, Interface(typeof(IAxis1D))]
        [FormerlySerializedAs("_progress")]
        private UnityEngine.Object _progressState;
        private IAxis1D ProgressState { get; set; }

        [SerializeField, Optional, Interface(typeof(IActiveState))]
        private UnityEngine.Object _highlightState;
        private IActiveState HighlightState { get; set; }

        [SerializeField]
        private Color _acceptColor = Color.white;
        public Color AcceptColor
        {
            get => _acceptColor;
            set => _acceptColor = value;
        }

        [SerializeField]
        private Color _rejectColor = Color.red;
        public Color RejectColor
        {
            get => _rejectColor;
            set => _rejectColor = value;
        }

        [SerializeField]
        private AnimationCurve _acceptAnimation = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        public AnimationCurve AcceptAnimation
        {
            get => _acceptAnimation;
            set => _acceptAnimation = value;
        }

        [SerializeField]
        private AnimationCurve _rejectAnimation = AnimationCurve.Linear(0f, 0f, 1f, 0.3f);
        public AnimationCurve RejectAnimation
        {
            get => _rejectAnimation;
            set => _rejectAnimation = value;
        }

        [SerializeField]
        private float _transitionSpeed = 8f;
        public float TransitionSpeed
        {
            get => _transitionSpeed;
            set => _transitionSpeed = value;
        }

        private static readonly int _progressKey = Shader.PropertyToID("_Progress");
        private static readonly int _highlightKey = Shader.PropertyToID("_Highlight");
        private static readonly int _colorKey = Shader.PropertyToID("_Color");
        private static readonly int _highlightColorKey = Shader.PropertyToID("_HighlightColor");

        private bool _selectionAnimation = false;
        private float _animatedProgress = 0f;
        private float _currentProgress = 0f;
        private bool _acceptMode = true;

        protected override IInteractorView Interactor { get; set; }
        protected override Component InteractableComponent => _interactor.Interactable;

        protected virtual void Awake()
        {
            Interactor = _interactor;
            ProgressState = _progressState as IAxis1D;
            HighlightState = _highlightState as IActiveState;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_interactor, nameof(_interactor));
            this.AssertField(_targetRenderer, nameof(_targetRenderer));
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                _interactor.WhenStateChanged += HandleStateChanged;

                SetReticleProgress(0f);
                _targetRenderer.enabled = false;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                _interactor.WhenStateChanged -= HandleStateChanged;
            }
            base.OnDisable();
        }

        protected override void Align(ReticleDataTeleport data)
        {
            bool highlight = HighlightState != null ? HighlightState.Active : _selectionAnimation;
            data.Highlight(highlight);

            if (data.HideReticle)
            {
                return;
            }

            Vector3 position = data.ProcessHitPoint(_interactor.ArcEnd.Point);
            Quaternion rotation = Quaternion.LookRotation(_interactor.ArcEnd.Normal);
            this.transform.SetPositionAndRotation(position, rotation);

            _targetRenderer.enabled = true;

            float targetProgress = ProgressState != null ? ProgressState.Value() : _animatedProgress;
            AnimationCurve animation = _acceptMode ? _acceptAnimation : _rejectAnimation;
            SetReticleProgress(animation.Evaluate(targetProgress));
            SetReticleHighlight(highlight);
        }

        protected override void Draw(ReticleDataTeleport data)
        {
            TeleportInteractable interactable = _interactor.Interactable;
            _acceptMode = interactable.AllowTeleport;
            _selectionAnimation = false;
            SetReticleColor(_acceptMode ? _acceptColor : _rejectColor);
        }

        protected override void Hide()
        {
            if (_targetRenderer != null)
            {
                _targetRenderer.enabled = false;
            }
            if (_targetData != null)
            {
                _targetData.Highlight(false);
            }
        }

        private void SetReticleColor(Color color)
        {
            _targetRenderer.material.SetColor(_colorKey, color);
            _targetRenderer.material.SetColor(_highlightColorKey, color);
        }

        private void SetReticleProgress(float progress)
        {
            if (_selectionAnimation)
            {
                _currentProgress = progress;
            }
            else
            {
                _currentProgress = Mathf.MoveTowards(_currentProgress, progress,
                        _transitionSpeed * Time.deltaTime);
            }

            _targetRenderer.material.SetFloat(_progressKey, _currentProgress);
        }

        private void SetReticleHighlight(bool highlight)
        {
            _targetRenderer.material.SetFloat(_highlightKey, highlight ? 1f : 0f);
        }

        private void HandleStateChanged(InteractorStateChangeArgs obj)
        {
            if (ProgressState == null && obj.NewState == InteractorState.Select)
            {
                StopAllCoroutines();
                StartCoroutine(SelectionAnimation());
            }
        }

        private IEnumerator SelectionAnimation()
        {
            float targetProgress = 1f;
            _animatedProgress = 0f;
            _selectionAnimation = true;
            while (!Mathf.Approximately(targetProgress, _animatedProgress))
            {
                _animatedProgress = Mathf.MoveTowards(_animatedProgress, targetProgress,
                    _transitionSpeed * Time.deltaTime);
                yield return null;
            }
            _selectionAnimation = false;
        }

        #region Inject

        public void InjectAllTeleportReticleDrawer(TeleportInteractor interactor,
            Renderer targetRenderer)
        {
            InjectInteractor(interactor);
            InjectTargetRenderer(targetRenderer);
        }

        public void InjectInteractor(TeleportInteractor interactor)
        {
            _interactor = interactor;
        }

        public void InjectTargetRenderer(Renderer targetRenderer)
        {
            _targetRenderer = targetRenderer;
        }

        [Obsolete("Use " + nameof(InjectTargetRenderer) + " instead")]
        public void InjectOptionalValidTargetRenderer(Renderer validTargetRenderer)
        {
            _targetRenderer = validTargetRenderer;
        }

        [Obsolete("Not in use")]
        public void InjectOptionalInalidTargetRenderer(Renderer invalidTargetRenderer)
        {
            _invalidTargetRenderer = invalidTargetRenderer;
        }

        public void InjectOptionalProgress(IAxis1D progressState)
        {
            _progressState = progressState as UnityEngine.Object;
            ProgressState = progressState;
        }

        public void InjectOptionalHighlightState(IActiveState highlightState)
        {
            _highlightState = highlightState as UnityEngine.Object;
            HighlightState = highlightState;
        }
        #endregion
    }
}
