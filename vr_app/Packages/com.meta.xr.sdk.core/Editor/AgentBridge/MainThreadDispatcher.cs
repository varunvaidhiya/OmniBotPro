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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Dispatcher for executing actions on Unity's main thread from async operations.
    /// Essential for AI service callbacks since Unity APIs can only be called from the main thread.
    /// </summary>
    [InitializeOnLoad]
    public static class MainThreadDispatcher
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static readonly object _lock = new object();
        private static bool _started;

        static MainThreadDispatcher()
        {
            // Only hook into the editor update loop if the master toggle is enabled.
            // When dormant, no per-frame processing occurs.
            if (Settings.Enabled.Value)
            {
                Start();
            }
        }

        private static void Start()
        {
            if (_started) return;
            _started = true;
            EditorApplication.update += Update;
        }

        /// <summary>
        /// Ensure the dispatcher is running. Called when the user enables AI packages.
        /// </summary>
        public static void EnsureStarted()
        {
            Start();
        }

        /// <summary>
        /// Execute an action on the main thread during the next editor update.
        /// Thread-safe and can be called from any thread.
        /// </summary>
        /// <param name="action">The action to execute on the main thread</param>
        public static void ExecuteOnMainThread(Action action)
        {
            if (action == null)
            {
                Log.Error("Cannot execute null action on main thread");
                return;
            }

            lock (_lock)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Execute a function on the main thread and return its result.
        /// This is synchronous and will block until the function completes.
        /// </summary>
        /// <typeparam name="T">The return type of the function</typeparam>
        /// <param name="func">The function to execute on the main thread</param>
        /// <returns>The result of the function execution</returns>
        public static T ExecuteOnMainThreadSync<T>(Func<T> func)
        {
            if (func == null)
            {
                Log.Error("Cannot execute null function on main thread");
                return default(T)!;
            }

            T result = default(T)!;
            var resetEvent = new System.Threading.ManualResetEventSlim(false);

            ExecuteOnMainThread(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error executing function on main thread: {ex.Message}");
                }
                finally
                {
                    resetEvent.Set();
                }
            });

            resetEvent.Wait();
            return result;
        }

        /// <summary>
        /// Process all queued actions on the main thread.
        /// Called automatically during EditorApplication.update.
        /// </summary>
        private static void Update()
        {
            // Process all pending actions
            while (true)
            {
                Action? action = null;

                lock (_lock)
                {
                    if (_executionQueue.Count > 0)
                    {
                        action = _executionQueue.Dequeue();
                    }
                }

                if (action == null)
                    break;

                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error executing action on main thread: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Get the current queue size (for debugging purposes).
        /// </summary>
        public static int GetQueueSize()
        {
            lock (_lock)
            {
                return _executionQueue.Count;
            }
        }

        /// <summary>
        /// Clear all pending actions (use with caution).
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _executionQueue.Clear();
            }
        }
    }
}
