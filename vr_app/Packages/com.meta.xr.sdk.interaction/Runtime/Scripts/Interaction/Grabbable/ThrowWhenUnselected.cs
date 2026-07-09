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
using Oculus.Interaction.Throw;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Oculus.Interaction
{
    /// <summary>
    /// Tracks the movement of a rigidbody while it is selected by an <see cref="IPointable"/>
    /// and applies a throw velocity when it becomes fully unselected.
    /// </summary>
    public class ThrowWhenUnselected : ITimeConsumer, IDisposable
    {
        private Rigidbody _rigidbody;
        private IPointable _pointable;
        private HashSet<int> _selectors;
        private Func<float> _timeProvider = () => Time.time;

        /// <summary>
        /// Sets the function that provides with the time in order
        /// to measure the velocities of the rigidbody
        /// </summary>
        /// <param name="timeProvider">The function that provides the time</param>
        public void SetTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Sets the Rigidbody used for the throw velocity calculation
        /// </summary>
        /// <param name="rigidbody">The rigidbody to apply velocity to and measure deltas from</param>
        public void SetRigidBody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        /// <summary>
        /// Delegate signature for the loaded velocities
        /// </summary>
        /// <param name="velocity">Linear velocity</param>
        /// <param name="torque">Angular velocity</param>
        public delegate void VelocitiesLoadedDelegate(Vector3 velocity, Vector3 torque);
        /// <summary>
        /// Callback triggered when the object sets the throwing velocities of the
        /// referenced rigidbody
        /// </summary>
        public event VelocitiesLoadedDelegate WhenThrown = delegate { };

        private static IObjectPool<RANSACVelocity> _ransacVelocityPool = new ObjectPool<RANSACVelocity>(
            createFunc: () => new RANSACVelocity(10, 2),
            collectionCheck: false,
            defaultCapacity: 2);

        private static IObjectPool<HashSet<int>> _selectorsPool = new ObjectPool<HashSet<int>>(
            createFunc: () => new HashSet<int>(),
            actionOnRelease: (s) => s.Clear(),
            collectionCheck: false,
            defaultCapacity: 2);

        private RANSACVelocity _ransacVelocity = null;

        private Pose _prevPose = Pose.identity;
        private float _prevTime = 0f;
        private bool _isHighConfidence = true;

        /// <summary>
        /// Creates a new instance that listens to the provided IPointable events.
        /// Note that this instance must be disposed via .Dispose() to release the event listener.
        /// </summary>
        /// <param name="rigidbody">The rigidbody to track velocity from and throw.</param>
        /// <param name="pointable">The IPointable indicating when the rigidbody is selected and unselected.</param>
        public ThrowWhenUnselected(Rigidbody rigidbody, IPointable pointable)
        {
            _rigidbody = rigidbody;
            _pointable = pointable;

            _pointable.WhenPointerEventRaised += HandlePointerEventRaised;
        }

        /// <summary>
        /// Unregisters the instance from the IPointable events
        /// </summary>
        public void Dispose()
        {
            _pointable.WhenPointerEventRaised -= HandlePointerEventRaised;
        }

        /// <summary>
        /// Retrieves the current tracked velocity of the rigidbody
        /// </summary>
        /// <param name="velocity">Linear velocity</param>
        /// <param name="torque">Angular velocity</param>
        /// <returns>True if the velocities could be retrieved</returns>
        public bool TryGetVelocities(out Vector3 velocity, out Vector3 torque)
        {
            if (_ransacVelocity != null)
            {
                _ransacVelocity.GetVelocities(out velocity, out torque);
                return true;
            }

            velocity = torque = Vector3.zero;
            return false;
        }

        private void AddSelection(int selectorId)
        {
            if (_selectors == null)
            {
                Initialize();
            }

            _selectors.Add(selectorId);
        }

        private void RemoveSelection(int selectorId, bool canThrow)
        {
            _selectors.Remove(selectorId);
            if (_selectors.Count == 0)
            {
                if (canThrow)
                {
                    //During Unselection, a Move call is executed storing the
                    //previous frame data, then the Target is moved to the final pose
                    //and the Unselect is invoked. At this point the Target position can
                    //be expected (in the general cases) to be certain, so we can process
                    //the data as if it happened this frame.
                    Process(false);
                    LoadThrowVelocities();
                }
                Teardown();
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    AddSelection(evt.Identifier);
                    break;
                case PointerEventType.Move:
                    if (_selectors != null &&
                        _selectors.Contains(evt.Identifier))
                    {
                        //Move is invoked before the actual Transformer is applied to the target.
                        //Additionally several Move events can be fired per frame when grabbing
                        //with multiple points.
                        //So the pose of the target is still one frame behind, and we should store it
                        //as the previous frame data, not this one.
                        Process(true);
                        MarkFrameConfidence(evt.Identifier);
                    }
                    break;
                case PointerEventType.Cancel:
                    RemoveSelection(evt.Identifier, false);
                    break;
                case PointerEventType.Unselect:
                    MarkFrameConfidence(evt.Identifier);
                    RemoveSelection(evt.Identifier, true);
                    break;
            }
        }

        private void Initialize()
        {
            _selectors = _selectorsPool.Get();
            _ransacVelocity = _ransacVelocityPool.Get();
            _ransacVelocity.Initialize();
        }

        private void Teardown()
        {
            _selectorsPool.Release(_selectors);
            _selectors = null;
            _ransacVelocityPool.Release(_ransacVelocity);
            _ransacVelocity = null;
        }

        private void MarkFrameConfidence(int emitterKey)
        {
            if (!_isHighConfidence)
            {
                return;
            }

            if (HandTrackingConfidenceProvider.TryGetTrackingConfidence(emitterKey,
                out bool isHighConfidence))
            {
                if (!isHighConfidence)
                {
                    _isHighConfidence = false;
                }
            }
        }

        private void Process(bool saveAsPreviousFrame)
        {
            if (_rigidbody == null)
            {
                _isHighConfidence = false;
                return;
            }

            float time = _timeProvider.Invoke();
            Pose pose = _rigidbody.transform.GetPose();

            if (time > _prevTime || !saveAsPreviousFrame)
            {
                float frameTime = saveAsPreviousFrame ? _prevTime : time;
                _isHighConfidence &= pose.position != _prevPose.position;
                _ransacVelocity.Process(pose, frameTime, _isHighConfidence);
                _isHighConfidence = true;
            }

            _prevTime = time;
            _prevPose = pose;
        }

        private void LoadThrowVelocities()
        {
            if (TryGetVelocities(out Vector3 velocity, out Vector3 torque))
            {
                if (_rigidbody != null && !_rigidbody.isKinematic)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    _rigidbody.velocity = velocity;
#pragma warning restore CS0618 // Type or member is obsolete
                    _rigidbody.angularVelocity = torque;
                }
                WhenThrown.Invoke(velocity, torque);
            }
        }

    }
}
