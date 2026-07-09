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
using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Interaction
{
    /// <summary>
    /// Enables scrolling of UI ScrollRects using microgestures combined with the
    /// interaction event pipeline for target detection.
    ///
    /// <para>
    /// Subscribes to <see cref="InteractionBroadcaster.OnEventRaised"/> for UI hover
    /// events to track which ScrollRect the user is targeting, then applies scroll
    /// velocity when swipe microgestures are detected. This decouples scrolling from
    /// any specific interactor type, it works with any pointer that feeds the interaction event pipeline.
    /// </para>
    ///
    /// <para>
    /// Key behaviors:
    /// <list type="bullet">
    ///   <item><description>Leverages ScrollRect's native physics for momentum and elastic bounce</description></item>
    ///   <item><description>Repeated swipes in the same direction accumulate velocity for faster scrolling</description></item>
    ///   <item><description>Stops momentum when the hovered target changes to a different ScrollRect or nothing</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class MicrogestureScrollProvider : MonoBehaviour
    {

        private enum ScrollDirection
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        #region Serialized Fields

        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Microgesture event sources for detecting swipe gestures (typically one per hand)")]
        private OVRMicrogestureEventSource[] _microgestureSources;

        [Header("Scroll Settings")]
        [SerializeField, Range(0.1f, 5f)]
        [Tooltip("Strength of the scroll impulse. Multiplies viewport size to determine velocity.")]
        private float _scrollStrength = 2f;

        [Header("Direction Settings")]
        [SerializeField]
        private bool _invertVertical = false;

        [SerializeField]
        private bool _invertHorizontal = false;

        [SerializeField]
        private bool _enableVerticalScroll = true;

        [SerializeField]
        private bool _enableHorizontalScroll = true;

        [Header("Behavior")]
        [SerializeField]
        [Tooltip("If true, scrolling stops immediately when hover target changes to a different ScrollRect.")]
        private bool _stopMomentumOnTargetChange = true;

        #endregion

        #region Properties

        public float ScrollStrength
        {
            get => _scrollStrength;
            set => _scrollStrength = Mathf.Clamp(value, 0.1f, 5f);
        }

        public bool InvertVertical
        {
            get => _invertVertical;
            set => _invertVertical = value;
        }

        public bool InvertHorizontal
        {
            get => _invertHorizontal;
            set => _invertHorizontal = value;
        }

        public bool EnableVerticalScroll
        {
            get => _enableVerticalScroll;
            set => _enableVerticalScroll = value;
        }

        public bool EnableHorizontalScroll
        {
            get => _enableHorizontalScroll;
            set => _enableHorizontalScroll = value;
        }

        public bool StopMomentumOnTargetChange
        {
            get => _stopMomentumOnTargetChange;
            set => _stopMomentumOnTargetChange = value;
        }

        #endregion

        private ScrollRect _hoveredScrollRect;
        private MicrogestureScrollSettings _hoveredScrollSettings;
        private ScrollRect _activeScrollRect;
        private ScrollDirection _lastScrollDirection = ScrollDirection.None;

        protected bool _started = false;

        #region Unity Lifecycle

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionField(_microgestureSources, nameof(_microgestureSources));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                InteractionBroadcaster.OnEventRaised += HandleInteractionEvent;
                if (_microgestureSources != null)
                {
                    foreach (var source in _microgestureSources)
                    {
                        if (source != null)
                        {
                            source.WhenGestureRecognized += HandleMicrogesture;
                        }
                    }
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                InteractionBroadcaster.OnEventRaised -= HandleInteractionEvent;
                if (_microgestureSources != null)
                {
                    foreach (var source in _microgestureSources)
                    {
                        if (source != null)
                        {
                            source.WhenGestureRecognized -= HandleMicrogesture;
                        }
                    }
                }
            }
            _hoveredScrollRect = null;
            _activeScrollRect = null;
            _lastScrollDirection = ScrollDirection.None;
        }

        protected virtual void Update()
        {
            if (_stopMomentumOnTargetChange && _activeScrollRect != null)
            {
                if (_activeScrollRect.velocity.sqrMagnitude > 1f)
                {
                    if (!IsStillTargetingActive())
                    {
                        _activeScrollRect.velocity = Vector2.zero;
                        _activeScrollRect = null;
                        _lastScrollDirection = ScrollDirection.None;
                    }
                }
                else
                {
                    _activeScrollRect = null;
                }
            }
        }

        #endregion

        private void HandleInteractionEvent(InteractionEvent e)
        {
            if (e._type == InteractionType.UIHoverStart && e._source != null)
            {
                var scrollRect = e._source.GetComponentInParent<ScrollRect>();
                if (scrollRect)
                {
                    var settings = scrollRect.GetComponent<MicrogestureScrollSettings>();
                    if (settings && !settings.EnableMicrogestureScroll)
                    {
                        scrollRect = null;
                        settings = null;
                    }
                    _hoveredScrollSettings = settings;
                }
                _hoveredScrollRect = scrollRect;
            }
            else if (e._type == InteractionType.UIHoverEnd)
            {
                _hoveredScrollRect = null;
                _hoveredScrollSettings = null;
            }
        }

        private void HandleMicrogesture(OVRHand.MicrogestureType gesture)
        {
            if (_hoveredScrollRect == null ||
                !_hoveredScrollRect.enabled ||
                !_hoveredScrollRect.gameObject.activeInHierarchy)
            {
                return;
            }

            if (!TryGetScrollDirection(gesture, out ScrollDirection scrollDirection))
            {
                return;
            }

            Vector2 direction = GetDirectionVector(scrollDirection);
            // Resolve the correct ScrollRect for this
            // swipe direction. If the innermost hovered ScrollRect doesn't support
            // the axis, walk up to find a parent that does.
            ScrollRect scrollRect = FindScrollRectForDirection(_hoveredScrollRect, scrollDirection);
            if (!scrollRect)
            {
                return;
            }

            float baseDimension = GetViewportDimension(scrollRect, scrollDirection);

            float strength = (_hoveredScrollSettings != null && _hoveredScrollSettings.HasStrengthOverride)
                ? _hoveredScrollSettings.ScrollStrengthOverride
                : _scrollStrength;

            Vector2 velocityDelta = direction * (baseDimension * strength);

            bool isDirectionChange = _lastScrollDirection != ScrollDirection.None
                && IsOpposite(_lastScrollDirection, scrollDirection);

            if (isDirectionChange)
            {
                scrollRect.StopMovement();
                ClampContentToBounds(scrollRect);
                scrollRect.velocity = velocityDelta;
            }
            else
            {
                scrollRect.velocity += velocityDelta;
            }

            _lastScrollDirection = scrollDirection;
            _activeScrollRect = scrollRect;
        }

        private bool TryGetScrollDirection(OVRHand.MicrogestureType gesture, out ScrollDirection scrollDirection)
        {
            scrollDirection = ScrollDirection.None;

            bool verticalEnabled = (_hoveredScrollSettings != null && _hoveredScrollSettings.OverrideVerticalScroll)
                ? _hoveredScrollSettings.EnableVerticalScroll
                : _enableVerticalScroll;

            bool horizontalEnabled = (_hoveredScrollSettings != null && _hoveredScrollSettings.OverrideHorizontalScroll)
                ? _hoveredScrollSettings.EnableHorizontalScroll
                : _enableHorizontalScroll;

            switch (gesture)
            {
                case OVRHand.MicrogestureType.SwipeForward:
                    if (!verticalEnabled) return false;
                    scrollDirection = _invertVertical ? ScrollDirection.Up : ScrollDirection.Down;
                    return true;

                case OVRHand.MicrogestureType.SwipeBackward:
                    if (!verticalEnabled) return false;
                    scrollDirection = _invertVertical ? ScrollDirection.Down : ScrollDirection.Up;
                    return true;

                case OVRHand.MicrogestureType.SwipeRight:
                    if (!horizontalEnabled) return false;
                    scrollDirection = _invertHorizontal ? ScrollDirection.Left : ScrollDirection.Right;
                    return true;

                case OVRHand.MicrogestureType.SwipeLeft:
                    if (!horizontalEnabled) return false;
                    scrollDirection = _invertHorizontal ? ScrollDirection.Right : ScrollDirection.Left;
                    return true;
            }
            return false;
        }

        private static Vector2 GetDirectionVector(ScrollDirection direction)
        {
            switch (direction)
            {
                case ScrollDirection.Up: return new Vector2(0, 1);
                case ScrollDirection.Down: return new Vector2(0, -1);
                case ScrollDirection.Left: return new Vector2(-1, 0);
                case ScrollDirection.Right: return new Vector2(1, 0);
                default: return Vector2.zero;
            }
        }

        private static bool IsHorizontal(ScrollDirection direction)
        {
            return direction == ScrollDirection.Left || direction == ScrollDirection.Right;
        }

        private static float GetViewportDimension(ScrollRect scrollRect, ScrollDirection direction)
        {
            if (scrollRect.viewport)
            {
                return IsHorizontal(direction)
                    ? scrollRect.viewport.rect.width
                    : scrollRect.viewport.rect.height;
            }
            if (scrollRect.transform is RectTransform rt)
            {
                return IsHorizontal(direction)
                    ? rt.rect.width
                    : rt.rect.height;
            }
            return 500f;
        }

        private static bool IsOpposite(ScrollDirection a, ScrollDirection b)
        {
            return (a == ScrollDirection.Up && b == ScrollDirection.Down)
                || (a == ScrollDirection.Down && b == ScrollDirection.Up)
                || (a == ScrollDirection.Left && b == ScrollDirection.Right)
                || (a == ScrollDirection.Right && b == ScrollDirection.Left);
        }

        private void ClampContentToBounds(ScrollRect scrollRect)
        {
            if (scrollRect.content == null) return;

            Vector2 norm = scrollRect.normalizedPosition;
            Vector2 clamped = new Vector2(
                Mathf.Clamp01(norm.x),
                Mathf.Clamp01(norm.y)
            );

            if (norm != clamped)
            {
                scrollRect.normalizedPosition = clamped;
            }
        }

        /// <summary>
        /// Finds the appropriate ScrollRect for the given swipe direction by walking
        /// up the hierarchy from the innermost hovered ScrollRect. If the innermost
        /// ScrollRect doesn't support the swipe axis (e.g., a horizontal-only child),
        /// returns the first ancestor ScrollRect that does.
        /// </summary>
        private ScrollRect FindScrollRectForDirection(ScrollRect innermost, ScrollDirection direction)
        {
            ScrollRect current = innermost;
            bool horizontal = IsHorizontal(direction);
            while (current)
            {
                if ((horizontal && current.horizontal) ||
                    (!horizontal && current.vertical))
                {
                    return current;
                }

                Transform parent = current.transform.parent;
                current = parent ? parent.GetComponentInParent<ScrollRect>() : null;
            }
            return null;
        }

        /// <summary>
        /// Checks whether the user is still gazing within the active ScrollRect.
        /// Returns true if the hovered ScrollRect is the active one, or if the
        /// hovered ScrollRect is a nested child of the active one (so that scrolling
        /// a parent continues while gazing at a child).
        /// </summary>
        private bool IsStillTargetingActive()
        {
            if (!_hoveredScrollRect) return false;
            if (_hoveredScrollRect == _activeScrollRect) return true;
            return _activeScrollRect &&
                   _hoveredScrollRect.transform.IsChildOf(_activeScrollRect.transform);
        }

        #region Inject

        public void InjectAllMicrogestureScrollProvider(
            OVRMicrogestureEventSource[] microgestureSources)
        {
            InjectMicrogestureSources(microgestureSources);
        }

        public void InjectMicrogestureSources(OVRMicrogestureEventSource[] microgestureSources)
        {
            _microgestureSources = microgestureSources;
        }

        #endregion
    }
}
