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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Acts as a forwarder of Trigger events for Rigidbody overlaps. Used in conjunction with
    /// <see cref="CollisionInteractionRegistry{TInteractor, TInteractable}"/>.
    ///
    /// Note: If <see cref="Physics.simulationMode"/> is <see cref="SimulationMode.Script"/>,
    /// <see cref="ForceGlobalUpdateTriggers"/> should be called after every call to <see cref="Physics.Simulate(float)"/>
    /// </summary>
    public class InteractableTriggerBroadcaster : MonoBehaviour
    {
        /// <summary>
        /// Raised when a rigidbody collision is detected with the associated <see cref="IInteractable"/>.
        /// </summary>
        [Obsolete("Use " + nameof(WhenRigidbodyEntered) + " instead")]
        public Action<IInteractable, Rigidbody> WhenTriggerEntered = delegate { };

        /// <summary>
        /// Raised when a rigidbody collision with the associated <see cref="IInteractable"/> ends.
        /// </summary>
        [Obsolete("Use " + nameof(WhenRigidbodyExited) + " instead")]
        public Action<IInteractable, Rigidbody> WhenTriggerExited = delegate { };

        /// <summary>
        /// Raised when a rigidbody collision is detected.
        ///  Initialized to null to know if it's been set.
        /// </summary>
        public Action<Rigidbody> WhenRigidbodyEntered = null;

        /// <summary>
        /// Raised when a rigidbody collision ends.
        ///  Initialized to null to know if it's been set.
        /// </summary>
        public Action<Rigidbody> WhenRigidbodyExited = null;

        private IInteractable _interactable;
        private Dictionary<Rigidbody, bool> _rigidbodyTriggers;
        private List<Rigidbody> _rigidbodies;

        private static HashSet<InteractableTriggerBroadcaster> _broadcasters =
            new HashSet<InteractableTriggerBroadcaster>();

        protected bool _started = false;
        private bool _skippedPhysics;
        private bool _forcedGlobalPhysicsUpdate;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            _rigidbodyTriggers = new Dictionary<Rigidbody, bool>();
            _rigidbodies = new List<Rigidbody>();
            _skippedPhysics = false;
            _forcedGlobalPhysicsUpdate = false;
            this.EndStart(ref _started);
        }

        protected virtual void OnTriggerStay(Collider collider)
        {
            if (!_started)
            {
                return;
            }

            Rigidbody rigidbody = collider.attachedRigidbody;
            if (rigidbody == null)
            {
                return;
            }

            if (!_rigidbodyTriggers.ContainsKey(rigidbody))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                WhenTriggerEntered(_interactable, rigidbody);
#pragma warning restore CS0618 // Type or member is obsolete
                WhenRigidbodyEntered?.Invoke(rigidbody);
                _rigidbodyTriggers.Add(rigidbody, true);
            }
            else
            {
                _rigidbodyTriggers[rigidbody] = true;
            }
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _broadcasters.Add(this);
            }
        }

        protected virtual void FixedUpdate()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (Physics.autoSimulation)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                UpdateTriggers();
            }
            else
            {
                _skippedPhysics = true;
            }
        }

        private void UpdateTriggers()
        {
            _rigidbodies.Clear();
            _rigidbodies.AddRange(_rigidbodyTriggers.Keys);
            foreach (Rigidbody rigidbody in _rigidbodies)
            {
                if (_rigidbodyTriggers[rigidbody] == false)
                {
                    _rigidbodyTriggers.Remove(rigidbody);
#pragma warning disable CS0618 // Type or member is obsolete
                    WhenTriggerExited(_interactable, rigidbody);
#pragma warning restore CS0618 // Type or member is obsolete
                    WhenRigidbodyExited?.Invoke(rigidbody);
                }
                else
                {
                    _rigidbodyTriggers[rigidbody] = false;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                // Clean up any remaining active triggers
                foreach (Rigidbody rigidbody in _rigidbodyTriggers.Keys)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    WhenTriggerExited(_interactable, rigidbody);
#pragma warning restore CS0618 // Type or member is obsolete
                    WhenRigidbodyExited?.Invoke(rigidbody);
                }
                _broadcasters.Remove(this);
                _rigidbodies.Clear();
                Assert.IsTrue(!_skippedPhysics || _forcedGlobalPhysicsUpdate,
                    $"If Physics.autoSimulation is false, {nameof(InteractableTriggerBroadcaster)}." +
                    $"{nameof(ForceGlobalUpdateTriggers)} must be called manually.");
            }
        }

        protected virtual void OnDestroy()
        {
            if (_started)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                WhenTriggerEntered = null;
                WhenTriggerExited = null;
#pragma warning restore CS0618 // Type or member is obsolete
                WhenRigidbodyEntered = null;
                WhenRigidbodyExited = null;
            }
        }

        /// <summary>
        /// Forces trigger broadcasters to update their trigger states.
        /// If <see cref="Physics.simulationMode"/> is <see cref="SimulationMode.Script"/>,
        /// <see cref="ForceGlobalUpdateTriggers"/> should be called after every call to <see cref="Physics.Simulate(float)"/>
        /// </summary>
        public static void ForceGlobalUpdateTriggers()
        {
            foreach (InteractableTriggerBroadcaster broadcaster in _broadcasters)
            {
                broadcaster._forcedGlobalPhysicsUpdate = true;
                broadcaster.UpdateTriggers();
            }
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="InteractableTriggerBroadcaster"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        [Obsolete]
        public void InjectAllInteractableTriggerBroadcaster(IInteractable interactable)
        {
            InjectInteractable(interactable);
        }

        /// <summary>
        /// Sets the underlying <see cref="IInteractable"/> for a dynamically instantiated
        /// <see cref="InteractableTriggerBroadcaster"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        [Obsolete]
        public void InjectInteractable(IInteractable interactable)
        {
            _interactable = interactable;
        }
        #endregion
    }
}
