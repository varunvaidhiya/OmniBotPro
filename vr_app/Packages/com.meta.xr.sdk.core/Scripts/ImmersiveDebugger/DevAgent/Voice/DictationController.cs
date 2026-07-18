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

#if HAS_META_VOICE_SDK
using Meta.XR.ImmersiveDebugger;
using Oculus.Voice.Dictation;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    internal class DictationController : MonoBehaviour
    {
        [SerializeField]
        internal AppDictationExperience experience;

        [DebugMember(Category = "Voice Integration")]
        private string _full;
        [DebugMember(Category = "Voice Integration")]
        private string _partial;
        [DebugMember(Category = "Voice Integration")]
        private string _concatenatedFull = "";
        [DebugMember(Category = "Voice Integration")]
        private bool micActive => experience.MicActive;
        [DebugMember(Category = "Voice Integration")]
        private bool experienceActive => experience.Active;
        [DebugMember(Category = "Voice Integration")]
        private bool requestActive => experience.IsRequestActive;

        [DebugMember(Category = "Voice Integration")]
        private float _currentMicLevel;

        // Events for live transcription
        internal event Action<string> OnPartialTranscriptionUpdate;
        internal event Action<string> OnTranscriptionFinalized;

        private TaskCompletionSource<string> _transcriptionCompletionSource;

        [DebugMember(Category = "Voice Integration")]
        private string _lastEvent;
        private string LastEvent
        {
            get => _lastEvent;
            set
            {
                _lastEvent = value;
            }
        }
        private void Start()
        {
            experience.TranscriptionEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            experience.TranscriptionEvents.OnFullTranscription.AddListener(OnFullTranscription);
            experience.DictationEvents.OnDictationSessionStarted.AddListener(_ => { LastEvent = "OnDictationSessionStarted"; });
            experience.DictationEvents.OnDictationSessionStopped.AddListener(_ => { LastEvent = "OnDictationSessionStopped"; });
            experience.DictationEvents.OnStartListening.AddListener(() => { LastEvent = "OnStartListening"; });
            experience.DictationEvents.OnStoppedListening.AddListener(() => { LastEvent = "OnStoppedListening"; });
            experience.DictationEvents.OnMicStartedListening.AddListener(() => { LastEvent = "OnMicStartedListening"; });
            experience.DictationEvents.OnMicStoppedListening.AddListener(() => { LastEvent = "OnMicStoppedListening"; });
            experience.DictationEvents.OnStoppedListeningDueToDeactivation.AddListener(() => { LastEvent = "OnStoppedListeningDueToDeactivation"; });
            experience.DictationEvents.OnStoppedListeningDueToInactivity.AddListener(() => { LastEvent = "OnStoppedListeningDueToInactivity"; });
            experience.DictationEvents.OnStoppedListeningDueToTimeout.AddListener(() => { LastEvent = "OnStoppedListeningDueToTimeout"; });
            experience.DictationEvents.OnMicAudioLevelChanged.AddListener(OnMicAudioLevelChanged);
        }

        private void OnMicAudioLevelChanged(float level)
        {
            _currentMicLevel = level;
        }

        [DebugMember(Category = "Voice Integration")]
        internal void Toggle(bool activate)
        {
            if (activate && !experience.Active)
            {
                // activating it, resetting all transcriptions
                _full = "";
                _partial = "";
                _concatenatedFull = "";
                experience.ActivateImmediately();
            }
            else if (!activate && experience.Active)
            {
                experience.Deactivate();
                // stopping, finalize the transcription
                OnTranscriptionFinalized?.Invoke(_concatenatedFull);
            }
        }

        private void OnPartialTranscription(string transcription)
        {
            _partial = transcription;

            // Notify listeners of partial transcription update
            // This will be the full message with all previous full concatenated + current partial
            string combined = _concatenatedFull + transcription;
            OnPartialTranscriptionUpdate?.Invoke(combined);
        }

        private void OnFullTranscription(string transcription)
        {
            _full = transcription;
            _concatenatedFull += transcription + ". ";
            _transcriptionCompletionSource?.SetResult(transcription);
        }

        internal async Task<string> FetchTranscription(CancellationToken cancellationToken = default)
        {
            // If we already have a full transcription, return it immediately
            if (!string.IsNullOrEmpty(_full))
            {
                return _full;
            }

            // Create a new TaskCompletionSource to wait for the transcription
            _transcriptionCompletionSource = new TaskCompletionSource<string>();

            // Register cancellation callback
            using (cancellationToken.Register(() => _transcriptionCompletionSource?.TrySetCanceled()))
            {
                try
                {
                    // Wait for the transcription to complete without busy looping
                    return await _transcriptionCompletionSource.Task;
                }
                catch (OperationCanceledException)
                {
                    // Clean up and re-throw
                    _transcriptionCompletionSource = null;
                    throw;
                }
            }
        }
    }
}
#endif
