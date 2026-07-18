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

using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using System.Collections;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Controller for managing the thinking animation with three animated dots.
    /// Handles creation, animation, and cleanup of the thinking indicator UI.
    /// </summary>
    internal class ThinkingAnimationController
    {
        private readonly MonoBehaviour _owner;
        private readonly Flex _parentContainer;

        private Flex _thinkingContainer;
        private Background[] _thinkingDots;
        private Coroutine _thinkingAnimation;
        private bool _isThinking = false;

        // Animation configuration
        private const float LARGE_DOT_SCALE = 1.1f; // Smaller size difference for smoother animation
        private const float NORMAL_DOT_SCALE = 0.9f;
        private const float ANIMATION_SPEED = 0.5f; // Slightly slower for smoother feel
        private const float FADE_DURATION = 0.3f; // Duration for color/opacity transition
        private const int DOT_COUNT = 3;
        private const int DOT_SIZE = 8; // Small animated dots, twice the size of ConversationDot

        // Color configuration for opacity effect
        private const float ACTIVE_ALPHA = 1.0f;
        private const float INACTIVE_ALPHA = 0.3f;

        /// <summary>
        /// Gets whether the thinking animation is currently active
        /// </summary>
        internal bool IsThinking => _isThinking;

        /// <summary>
        /// Initialize the thinking animation controller
        /// </summary>
        /// <param name="owner">MonoBehaviour owner for coroutine management</param>
        /// <param name="parentContainer">Parent flex container to add the thinking animation to</param>
        internal ThinkingAnimationController(MonoBehaviour owner, Flex parentContainer)
        {
            _owner = owner;
            _parentContainer = parentContainer;
        }

        /// <summary>
        /// Start the thinking animation with three animated dots
        /// </summary>
        internal void StartAnimation()
        {
            if (_isThinking) return;

            _isThinking = true;

            CreateThinkingContainer();
            CreateThinkingDots();
            StartAnimationCoroutine();
        }

        /// <summary>
        /// Stop the thinking animation and remove the dots
        /// </summary>
        internal void StopAnimation()
        {
            if (!_isThinking) return;

            _isThinking = false;

            StopAnimationCoroutine();
            RemoveThinkingContainer();
        }

        /// <summary>
        /// Force cleanup of the animation (useful for panel destruction)
        /// </summary>
        internal void ForceCleanup()
        {
            if (_thinkingAnimation != null && _owner != null)
            {
                _owner.StopCoroutine(_thinkingAnimation);
                _thinkingAnimation = null;
            }

            _isThinking = false;
            RemoveThinkingContainer();
        }

        private void CreateThinkingContainer()
        {
            _thinkingContainer = _parentContainer.Append<Flex>("thinkingContainer");
            _thinkingContainer.LayoutStyle = Style.Load<LayoutStyle>("ThinkingContainer");
        }

        private void CreateThinkingDots()
        {
            _thinkingDots = new Background[DOT_COUNT];
            var baseColor = Style.Load<ImageStyle>("PillInfo").color; // Use assistant color

            for (var i = 0; i < DOT_COUNT; i++)
            {
                _thinkingDots[i] = _thinkingContainer.Append<Background>($"thinkingDot{i}");
                _thinkingDots[i].LayoutStyle = Style.Load<LayoutStyle>("ThinkingDot");

                // Set up dot appearance with initial inactive state
                var inactiveColor = new Color(baseColor.r, baseColor.g, baseColor.b, INACTIVE_ALPHA);
                _thinkingDots[i].Color = inactiveColor;
                _thinkingDots[i].transform.localScale = Vector3.one * NORMAL_DOT_SCALE;

                // Create a simple circular sprite if none exists
                var dotTexture2D = Resources.Load<Texture2D>("Textures/circle_icon");
                if (dotTexture2D != null)
                {
                    _thinkingDots[i].Sprite = Sprite.Create(
                        dotTexture2D,
                        new Rect(0, 0, dotTexture2D.width, dotTexture2D.height),
                        Vector2.one * 0.5f);
                }
                else
                {
                    // Use existing pill sprites as fallback
                    var pillStyle = Style.Load<ImageStyle>("PillInfo");
                    _thinkingDots[i].Sprite = pillStyle.sprite;
                }
            }
        }

        private void StartAnimationCoroutine()
        {
            if (_owner != null)
            {
                _thinkingAnimation = _owner.StartCoroutine(AnimateThinkingDots());
            }
        }

        private void StopAnimationCoroutine()
        {
            if (_thinkingAnimation != null && _owner != null)
            {
                _owner.StopCoroutine(_thinkingAnimation);
                _thinkingAnimation = null;
            }
        }

        private void RemoveThinkingContainer()
        {
            if (_thinkingContainer != null)
            {
                _parentContainer.Remove(_thinkingContainer, true);
                _thinkingContainer = null;
                _thinkingDots = null;
            }
        }

        /// <summary>
        /// Animate the thinking dots with smooth scale and opacity transitions
        /// </summary>
        private IEnumerator AnimateThinkingDots()
        {
            var baseColor = Style.Load<ImageStyle>("PillInfo").color;
            var activeColor = new Color(baseColor.r, baseColor.g, baseColor.b, ACTIVE_ALPHA);
            var inactiveColor = new Color(baseColor.r, baseColor.g, baseColor.b, INACTIVE_ALPHA);

            int activeDot = 0;

            while (_isThinking && _thinkingDots != null)
            {
                // Start simultaneous animations for all dots
                var animationCoroutines = new Coroutine[DOT_COUNT];

                for (int i = 0; i < DOT_COUNT; i++)
                {
                    if (_thinkingDots[i] != null)
                    {
                        bool isActive = i == activeDot;
                        float targetScale = isActive ? LARGE_DOT_SCALE : NORMAL_DOT_SCALE;
                        Color targetColor = isActive ? activeColor : inactiveColor;

                        animationCoroutines[i] = _owner.StartCoroutine(
                            AnimateDotTransition(_thinkingDots[i], targetScale, targetColor));
                    }
                }

                // Wait for transition to complete
                yield return new WaitForSecondsRealtime(FADE_DURATION);

                // Wait before moving to next dot
                yield return new WaitForSecondsRealtime(ANIMATION_SPEED - FADE_DURATION);

                // Move to next dot
                activeDot = (activeDot + 1) % DOT_COUNT;
            }
        }

        /// <summary>
        /// Smoothly animate a single dot's scale and color
        /// </summary>
        private IEnumerator AnimateDotTransition(Background dot, float targetScale, Color targetColor)
        {
            if (dot == null) yield break;

            Vector3 startScale = dot.transform.localScale;

            // Since Color property is write-only, we need to determine the start color
            // based on current scale (active dots are larger and have full alpha)
            var baseColor = Style.Load<ImageStyle>("PillInfo").color;
            bool currentlyActive = Mathf.Approximately(startScale.x, LARGE_DOT_SCALE);
            Color startColor = currentlyActive
                ? new Color(baseColor.r, baseColor.g, baseColor.b, ACTIVE_ALPHA)
                : new Color(baseColor.r, baseColor.g, baseColor.b, INACTIVE_ALPHA);

            float elapsedTime = 0f;

            while (elapsedTime < FADE_DURATION)
            {
                if (dot == null) yield break;

                float progress = elapsedTime / FADE_DURATION;

                // Use smooth easing function (ease-in-out)
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

                // Interpolate scale
                dot.transform.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, easedProgress);

                // Interpolate color
                Color currentColor = Color.Lerp(startColor, targetColor, easedProgress);
                dot.Color = currentColor;

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            // Ensure final values are set exactly
            if (dot != null)
            {
                dot.transform.localScale = Vector3.one * targetScale;
                dot.Color = targetColor;
            }
        }
    }
}
