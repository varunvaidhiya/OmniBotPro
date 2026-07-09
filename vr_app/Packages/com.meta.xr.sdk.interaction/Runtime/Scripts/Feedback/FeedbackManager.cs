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

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Manages the processing and playback of interaction feedback (haptics, audio, visuals, etc.).
    /// It subscribes to an <see cref="InteractionEventChannel"/> to receive interaction events.
    /// For each event, it consults a <see cref="FeedbackConfig"/> for default feedback rules and
    /// checks for <see cref="FeedbackSettings"/> on the interaction source GameObject, which can
    /// suppress or override the default feedback.
    /// It also provides an interface for haptic playback via <see cref="IHapticsPlayer"/>.
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        /// <summary>
        /// Gets the active singleton instance of the <see cref="FeedbackManager"/>.
        /// </summary>
        public static FeedbackManager Instance { get; private set; }

        /// <summary>
        /// Checks if a <see cref="FeedbackManager"/> instance currently exists and is active.
        /// </summary>
        public static bool Exists => Instance != null;

        [Tooltip("The FeedbackConfig asset defining default feedback rules.")]
        [SerializeField]
        private FeedbackConfig _feedbackConfig;

        [Tooltip("The InteractionEventChannel to subscribe to for interaction events.")]
        [SerializeField]
        private InteractionEventChannel _eventChannel;

        private IHapticsPlayer _hapticsPlayer;
        private bool _started = false;

        /// <summary>
        /// Gets the <see cref="FeedbackConfig"/> currently assigned to this manager.
        /// </summary>
        public FeedbackConfig Config => _feedbackConfig;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate instance of {nameof(FeedbackManager)} on {gameObject.name}. Destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_feedbackConfig, nameof(_feedbackConfig));
            this.AssertField(_eventChannel, nameof(_eventChannel));

            InitializeHapticsPlayer();

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (!_started) return;

            _eventChannel.OnEventRaised += HandleInteractionEvent;
        }

        protected virtual void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.OnEventRaised -= HandleInteractionEvent;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleInteractionEvent(InteractionEvent e)
        {
            if (_feedbackConfig == null)
            {
                Debug.LogError($"{nameof(FeedbackManager)}: {nameof(_feedbackConfig)} is not assigned. Cannot process event.", this);
                return;
            }

            // Resolve an identifier that works for both paths (3D and UI pointer)
            int id = e.InteractorView?.Identifier ?? e._pointerId;

            if (e._source != null &&
                e._source.TryGetComponent(out FeedbackSettings settings))
            {
                switch (settings.Mode)
                {
                    case FeedbackMode.Suppress:
                        return; // Feedback is suppressed for this object

                    case FeedbackMode.Override:
                        if (settings.TryGetOverrideActions(e._type, out var overrideActions) &&
                            overrideActions != null && overrideActions.Count > 0)
                        {
                            foreach (var action in overrideActions)
                            {
                                action?.Execute(id, e._source, this);
                            }
                        }
                        // Whether actions were found or not, Override mode means we don't process default config.
                        return;
                }
            }

            // Proceed with default feedback from FeedbackConfig
            var rule = _feedbackConfig.FindRule(e._type, e._source, e.InteractorView, e._pointerId);

            if (rule == null)
            {
                return; // No matching default rule
            }

            foreach (var action in rule.Actions)
            {
                action?.Execute(id, e._source, this);
            }

            if (rule.UseDefaultHaptics)
            {
                PlayHaptics(id, rule.DefaultHaptics);
            }
        }

        private void InitializeHapticsPlayer()
        {
            if (_hapticsPlayer != null) return;

            _hapticsPlayer = new UnityXRPlayer(this);
            Debug.Log($"{nameof(FeedbackManager)}: Initialized with {nameof(UnityXRPlayer)}.", this);
        }

        /// <summary>
        /// Plays a haptic pattern on the controller associated with the given interactor or pointer ID.
        /// </summary>
        /// <param name="sourceId">The identifier of the interactor or pointer that should produce the haptic feedback.
        /// This can be <see cref="IInteractorView.Identifier"/> or <see cref="InteractionEvent._pointerId"/>.</param>
        /// <param name="pattern">The <see cref="HapticPattern"/> to play.</param>
        internal void PlayHaptics(int sourceId, in HapticPattern pattern)
        {
            if (sourceId == -1 && pattern._amplitude > 0) // PointerId default is -1
            {
                return; // Block haptics for default invalid pointerId
            }
            _hapticsPlayer?.Play(sourceId, pattern);
        }

        /// <summary>
        /// Stops any active haptics on the controller associated with the given interactor or pointer ID.
        /// </summary>
        /// <param name="sourceId">The identifier of the interactor or pointer whose haptics should be stopped.</param>
        internal void StopHaptics(int sourceId)
        {
            if (sourceId == -1) return; // Don't try to stop haptics for default invalid pointerId
            _hapticsPlayer?.Stop(sourceId);
        }

        /// <summary>
        /// Retrieves the <see cref="IController"/> associated with a specific interactor identifier.
        /// This method directly queries the decorator system to find controller associations.
        /// It is designed for use by <see cref="IHapticsPlayer"/> implementations to determine
        /// the target device for haptic feedback.
        /// </summary>
        /// <param name="interactorId">The unique identifier of the interactor (<see cref="IInteractorView.Identifier"/>).</param>
        /// <param name="controller">When this method returns, contains the associated <see cref="IController"/>
        /// if found; otherwise, null.</param>
        /// <returns>True if a controller was found for the specified ID, false otherwise.</returns>
        internal bool TryGetController(int interactorId, out IController controller)
        {
            if (InteractorControllerDecorator.TryGetControllerForInteractorId(interactorId, out controller))
            {
                return true;
            }

            controller = null;
            return false;
        }
    }
}
