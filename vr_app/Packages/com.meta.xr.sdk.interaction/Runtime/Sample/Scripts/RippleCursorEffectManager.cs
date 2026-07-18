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

using Oculus.Interaction.Feedback;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Oculus.Interaction.Samples
{
    /// <summary>
    /// Manages a dynamic ripple and visual cursor effect for any number of <see cref="PokeInteractor"/> instances.
    /// This component uses an object pool for cursors and subscribes to each interactor's <see cref="IInteractor.WhenPostprocessed"/> event.
    /// It creates visual feedback for poke interactions including surface penetration glow and an animated ripple effect on tap.
    /// Requires <see cref="InteractionBroadcaster"/> to be configured in the scene to receive <see cref="InteractionEvent"/>s.
    /// </summary>
    public class RippleCursorEffectManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The prefab to use for the visual cursor. This will be pooled.")]
        private GameObject _cursorPrefab;
        [SerializeField, Tooltip("The initial number of cursors to create and add to the pool.")]
        private int _initialPoolSize = 2;
        [SerializeField, Tooltip("Duration of the tap pulse animation in seconds, triggered on SelectStart.")]
        private float _tapAnimationDuration = 0.20f;
        [SerializeField, Tooltip("Easing curve for the tap pulse animation.")]
        private AnimationCurve _tapAnimationEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Tooltip("Shader value for ripple just before tap.")]
        private float _tapAnimationUpperShaderValue = -0.01f;
        [SerializeField, Tooltip("Shader value for ripple at peak of tap (animation target).")]
        private float _tapAnimationLowerShaderValue = -0.25f;

        private static readonly int RippleProgressId = Shader.PropertyToID("_RippleProgress");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private readonly Dictionary<PokeInteractor, InteractorState> _activePokeInteractors = new();
        private IObjectPool<Renderer> _cursorRendererPool;
        private Color _rippleColor = Color.white;
        private bool _forceRippleColorUpdate;

        protected virtual void Awake()
        {
            InitializeCursorPool();
        }

        protected virtual void OnEnable()
        {
            InteractionBroadcaster.OnEventRaised += HandleInteractionEvent;
        }

        protected virtual void OnDisable()
        {
            InteractionBroadcaster.OnEventRaised -= HandleInteractionEvent;
            CleanupAllActiveEffects();
        }

        private void InitializeCursorPool()
        {
            _cursorRendererPool = new ObjectPool<Renderer>(
                createFunc: () =>
                {
                    GameObject instance = Instantiate(_cursorPrefab, this.transform);
                    instance.name = $"{_cursorPrefab.name}_Pooled{instance.GetHashCode()}";
                    return instance.GetComponent<Renderer>();
                },
                actionOnGet: (renderer) => renderer.gameObject.SetActive(true),
                actionOnRelease: (renderer) => renderer.gameObject.SetActive(false),
                actionOnDestroy: (renderer) => Destroy(renderer.gameObject),
                collectionCheck: false,
                defaultCapacity: _initialPoolSize
            );
        }

        private void CleanupAllActiveEffects()
        {
            var keysToClean = new List<PokeInteractor>(_activePokeInteractors.Keys);
            foreach (var pokeInteractor in keysToClean)
            {
                OnPokeHoverEnd(pokeInteractor);
            }
        }

        private void HandleInteractionEvent(InteractionEvent interactionEvent)
        {
            if (interactionEvent.InteractorView is not PokeInteractor pokeInteractor) return;

            switch (interactionEvent._type)
            {
                case InteractionType.HoverStart:
                    OnPokeHoverBegin(pokeInteractor);
                    break;
                case InteractionType.HoverEnd:
                    OnPokeHoverEnd(pokeInteractor);
                    break;
                case InteractionType.SelectStart:
                    OnPokeSelectStart(pokeInteractor);
                    break;
            }
        }

        private void OnPokeHoverBegin(PokeInteractor pokeInteractor)
        {
            if (_activePokeInteractors.ContainsKey(pokeInteractor)) return;
            IPointableElement pointableElement = pokeInteractor.Interactable.PointableElement;
            if (pointableElement is not PointableCanvas &&
                pointableElement is not PointableCanvasMesh) return;

            Renderer cursorRenderer = _cursorRendererPool.Get();
            cursorRenderer.gameObject.SetActive(true);

            Action updateAction = () => OnInteractorPostprocessed(pokeInteractor);
            pokeInteractor.WhenPostprocessed += updateAction;

            _activePokeInteractors[pokeInteractor] = new InteractorState
            {
                ActiveCursorRenderer = cursorRenderer,
                PropertyBlock = new MaterialPropertyBlock(),
                InitialPokeDistance = pokeInteractor.Candidate.EnterHoverNormal,
                TapAnimationCoroutine = null,
                UpdateAction = updateAction
            };

            UpdateRippleColor(_activePokeInteractors[pokeInteractor]);
        }

        private void OnPokeHoverEnd(PokeInteractor pokeInteractor)
        {
            if (!_activePokeInteractors.TryGetValue(pokeInteractor, out InteractorState state)) return;

            pokeInteractor.WhenPostprocessed -= state.UpdateAction;
            ResetInteractorVisuals(state);
            _cursorRendererPool.Release(state.ActiveCursorRenderer);
            _activePokeInteractors.Remove(pokeInteractor);
        }

        private void OnPokeSelectStart(PokeInteractor pokeInteractor)
        {
            if (!_activePokeInteractors.TryGetValue(pokeInteractor, out InteractorState state) ||
                state.TapAnimationCoroutine != null) return;

            state.TapAnimationCoroutine = StartCoroutine(
                AnimateTapBurstCoroutine(pokeInteractor)
            );
            _activePokeInteractors[pokeInteractor] = state;
        }

        private void OnInteractorPostprocessed(PokeInteractor pokeInteractor)
        {
            if (_activePokeInteractors.TryGetValue(pokeInteractor, out InteractorState state))
            {
                UpdateInteractorVisuals(pokeInteractor, state);
            }
        }

        private void UpdateInteractorVisuals(PokeInteractor pokeInteractor, InteractorState state)
        {
            if (!pokeInteractor.HasCandidate || !state.ActiveCursorRenderer) return;

            Transform cursorTransform = state.ActiveCursorRenderer.transform;
            cursorTransform.position = pokeInteractor.ClosestPoint;
            cursorTransform.rotation = Quaternion.LookRotation(pokeInteractor.TouchNormal);

            float currentDistance = Vector3.Distance(pokeInteractor.Origin, pokeInteractor.ClosestPoint);
            float penetration = Mathf.Clamp01(1f - (currentDistance / state.InitialPokeDistance));

            if (state.TapAnimationCoroutine == null)
            {
                state.PropertyBlock.SetFloat(RippleProgressId, 1.0f - penetration);
            }

            if (_forceRippleColorUpdate)
            {
                state.PropertyBlock.SetColor(TintColorId, _rippleColor);
                _forceRippleColorUpdate = false;
            }

            state.ActiveCursorRenderer.SetPropertyBlock(state.PropertyBlock);
        }

        private IEnumerator AnimateTapBurstCoroutine(PokeInteractor interactorForTap)
        {
            if (!_activePokeInteractors.TryGetValue(interactorForTap, out InteractorState state)) yield break;

            float elapsedTime = 0f;
            while (elapsedTime < _tapAnimationDuration)
            {
                float normalizedTime = elapsedTime / _tapAnimationDuration;
                float easedTime = _tapAnimationEase.Evaluate(normalizedTime);
                float value = Mathf.Lerp(_tapAnimationUpperShaderValue, _tapAnimationLowerShaderValue, easedTime);

                state.PropertyBlock.SetFloat(RippleProgressId, value);
                state.ActiveCursorRenderer.SetPropertyBlock(state.PropertyBlock);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Clean up animation reference
            if (_activePokeInteractors.TryGetValue(interactorForTap, out InteractorState finalState))
            {
                finalState.TapAnimationCoroutine = null;
                _activePokeInteractors[interactorForTap] = finalState;
            }
        }

        private void ResetInteractorVisuals(InteractorState state)
        {
            if (state.ActiveCursorRenderer != null)
            {
                state.PropertyBlock.SetFloat(RippleProgressId, 0f);
                state.ActiveCursorRenderer.SetPropertyBlock(state.PropertyBlock);
            }

            if (state.TapAnimationCoroutine != null)
            {
                StopCoroutine(state.TapAnimationCoroutine);
            }
        }

        public void SetThemeRippleColor(Color color)
        {
            _rippleColor = color;
            _forceRippleColorUpdate = true;
        }

        private void UpdateRippleColor(InteractorState state)
        {
            state.PropertyBlock.SetColor(TintColorId, _rippleColor);
            state.ActiveCursorRenderer.SetPropertyBlock(state.PropertyBlock);
        }

        private struct InteractorState
        {
            public Renderer ActiveCursorRenderer;
            public MaterialPropertyBlock PropertyBlock;
            public float InitialPokeDistance;
            public Coroutine TapAnimationCoroutine;
            public Action UpdateAction;
        }
    }
}


