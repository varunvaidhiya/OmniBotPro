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

#nullable enable

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// ScriptableObject wrapper for persisting conversation state to disk.
    /// Stored in Library/ folder to avoid version control pollution.
    /// </summary>
    [FilePath("Library/AgentBridge/ConversationState.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ConversationStateAsset : ScriptableSingleton<ConversationStateAsset>
    {
        [SerializeField]
        private ConversationState _state = new ConversationState();

        /// <summary>
        /// Per-caller conversation states for multi-client support.
        /// Key is CallerIdentity.Id, value is the caller's conversation state.
        /// </summary>
        [SerializeField]
        private List<CallerStateEntry> _callerStates = new List<CallerStateEntry>();

        /// <summary>
        /// Serializable entry for per-caller state (Dictionary not serializable).
        /// </summary>
        [System.Serializable]
        public class CallerStateEntry
        {
            public string CallerId = string.Empty;
            public ConversationState State = new ConversationState();
        }

        /// <summary>
        /// The default/shared conversation state (for backward compatibility).
        /// </summary>
        public ConversationState State
        {
            get => _state;
            set
            {
                _state = value;
                Save(true);
            }
        }

        /// <summary>
        /// Get or create a conversation state for a specific caller.
        /// </summary>
        public ConversationState GetStateForCaller(string callerId)
        {
            // Empty or null callerId uses the default state (for backward compatibility)
            if (string.IsNullOrEmpty(callerId))
            {
                return _state;
            }

            var entry = _callerStates.Find(e => e.CallerId == callerId);
            if (entry == null)
            {
                entry = new CallerStateEntry { CallerId = callerId, State = new ConversationState() };
                _callerStates.Add(entry);
                Save(true);
            }
            return entry.State;
        }

        /// <summary>
        /// Clear the state for a specific caller.
        /// </summary>
        public void ClearStateForCaller(string callerId)
        {
            // Empty or null callerId uses the default state (for backward compatibility)
            if (string.IsNullOrEmpty(callerId))
            {
                _state = new ConversationState();
            }
            else
            {
                var index = _callerStates.FindIndex(e => e.CallerId == callerId);
                if (index >= 0)
                {
                    _callerStates[index].State = new ConversationState();
                }
            }
            Save(true);
        }

        /// <summary>
        /// Clear all per-caller states and the default state.
        /// </summary>
        public void ClearAllStates()
        {
            _state = new ConversationState();
            _callerStates.Clear();
            Save(true);
        }

        public void SaveState()
        {
            Save(true);
        }
    }

    /// <summary>
    /// Manages persistence of conversation state across domain reloads.
    /// Supports per-caller state isolation for multi-client scenarios.
    /// </summary>
    [InitializeOnLoad]
    public static class ConversationPersistence
    {
        /// <summary>
        /// Default caller ID used when no specific caller is provided.
        /// Empty string indicates no caller specified.
        /// </summary>
        public const string DefaultCallerId = "";

        private static ConversationState? _cachedState;
        private static readonly Dictionary<string, ConversationState> _callerStates = new Dictionary<string, ConversationState>();
        private static readonly object _lock = new object();

        static ConversationPersistence()
        {
            Meta.XR.Editor.Callbacks.InitializeOnLoad.Register(Load);
        }

        /// <summary>
        /// Gets the default conversation state.
        /// </summary>
        public static ConversationState GetState()
        {
            return GetStateForCaller(DefaultCallerId);
        }

        /// <summary>
        /// Gets the conversation state for a specific caller.
        /// Creates a new state if one doesn't exist.
        /// </summary>
        /// <param name="callerId">The caller identifier. Empty/null returns the default state.</param>
        public static ConversationState GetStateForCaller(string? callerId)
        {
            // Normalize null/empty to DefaultCallerId for consistent caching
            string normalizedId = string.IsNullOrEmpty(callerId) ? DefaultCallerId : callerId!;

            lock (_lock)
            {
                // Check in-memory cache first
                if (_callerStates.TryGetValue(normalizedId, out var cachedState))
                {
                    return cachedState;
                }

                // Load from asset
                var asset = ConversationStateAsset.instance;
                var state = asset.GetStateForCaller(normalizedId);

                // Cache it
                _callerStates[normalizedId] = state;
                return state;
            }
        }

        /// <summary>
        /// Updates the default conversation state and saves it.
        /// </summary>
        public static void SetState(ConversationState state)
        {
            SetStateForCaller(DefaultCallerId, state);
        }

        /// <summary>
        /// Updates the conversation state for a specific caller and saves it.
        /// </summary>
        public static void SetStateForCaller(string? callerId, ConversationState state)
        {
            // Normalize null/empty to DefaultCallerId for consistent behavior
            string normalizedId = string.IsNullOrEmpty(callerId) ? DefaultCallerId : callerId!;

            lock (_lock)
            {
                _callerStates[normalizedId] = state;
                Save();
            }
        }

        /// <summary>
        /// Loads the conversation state from disk.
        /// </summary>
        private static void Load()
        {
            var asset = ConversationStateAsset.instance;
            _cachedState = asset.State ?? new ConversationState();
            _callerStates[DefaultCallerId] = _cachedState;
        }

        /// <summary>
        /// Saves the current conversation state to disk.
        /// Thread-safe: Can be called from any thread; will dispatch to main thread if needed.
        /// </summary>
        public static void Save()
        {
            lock (_lock)
            {
                // Unity serialization APIs must be called from the main thread
                // Use MainThreadDispatcher to safely execute the save operation
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        var asset = ConversationStateAsset.instance;
                        asset.SaveState();
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"Error saving conversation state: {ex.Message}");
                    }
                });
            }
        }

        /// <summary>
        /// Adds a message to the default conversation state in a thread-safe manner.
        /// </summary>
        public static void AddMessage(ConversationMessage message)
        {
            AddMessageForCaller(DefaultCallerId, message);
        }

        /// <summary>
        /// Adds a message to a specific caller's conversation state in a thread-safe manner.
        /// </summary>
        public static void AddMessageForCaller(string? callerId, ConversationMessage message)
        {
            // Normalize null/empty to DefaultCallerId for consistent behavior
            string normalizedId = string.IsNullOrEmpty(callerId) ? DefaultCallerId : callerId!;

            lock (_lock)
            {
                var state = GetStateForCaller(normalizedId);
                state.Messages.Add(message);
                Save();
            }
        }

        /// <summary>
        /// Clears the default conversation state.
        /// </summary>
        public static void Clear()
        {
            ClearForCaller(DefaultCallerId);
        }

        /// <summary>
        /// Clears the conversation state for a specific caller.
        /// </summary>
        public static void ClearForCaller(string? callerId)
        {
            // Normalize null/empty to DefaultCallerId for consistent behavior
            string normalizedId = string.IsNullOrEmpty(callerId) ? DefaultCallerId : callerId!;

            lock (_lock)
            {
                _callerStates[normalizedId] = new ConversationState();

                ClearAssetStateForCaller(normalizedId);
            }
        }

        /// <summary>
        /// Clears the asset-level state for a specific caller.
        /// Dispatches to main thread if needed since Unity serialization APIs
        /// can only be called from the main thread.
        /// </summary>
        private static void ClearAssetStateForCaller(string normalizedId)
        {
            void DoClear()
            {
                try
                {
                    var asset = ConversationStateAsset.instance;
                    asset.ClearStateForCaller(normalizedId);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Error clearing conversation state: {ex.Message}");
                }
            }

            if (IsMainThread())
            {
                DoClear();
            }
            else
            {
                MainThreadDispatcher.ExecuteOnMainThread(DoClear);
            }
        }

        /// <summary>
        /// Clears all conversation states (default and all per-caller states).
        /// Used for complete cleanup (e.g., in test teardown).
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _callerStates.Clear();
                _cachedState = new ConversationState();
                _callerStates[DefaultCallerId] = _cachedState;

                ClearAllAssetStates();
            }
        }

        /// <summary>
        /// Clears all asset-level states.
        /// Dispatches to main thread if needed since Unity serialization APIs
        /// can only be called from the main thread.
        /// </summary>
        private static void ClearAllAssetStates()
        {
            void DoClear()
            {
                try
                {
                    var asset = ConversationStateAsset.instance;
                    asset.ClearAllStates();
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Error clearing all conversation states: {ex.Message}");
                }
            }

            if (IsMainThread())
            {
                DoClear();
            }
            else
            {
                MainThreadDispatcher.ExecuteOnMainThread(DoClear);
            }
        }

        /// <summary>
        /// Returns true if the current thread is the main (UI) thread.
        /// </summary>
        private static readonly int _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        private static bool IsMainThread() => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;
    }
}
