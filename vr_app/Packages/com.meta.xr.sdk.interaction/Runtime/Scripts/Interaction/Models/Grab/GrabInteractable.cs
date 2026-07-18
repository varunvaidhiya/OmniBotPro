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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// The <see cref="GrabInteractable"/> class enables objects to be grabbable by controllers, provided they are within arm's reach.
    /// This class implements interfaces for handling rigidbody physics and collider references, facilitating detailed control over the interaction dynamics.
    /// </summary>
    public class GrabInteractable : PointerInteractable<GrabInteractor, GrabInteractable>,
                                      IRigidbodyRef, ICollidersRef
    {
        private Collider[] _colliders;
        public Collider[] Colliders => _colliders;

        /// <summary>
        /// The Rigidbody of the object.
        /// </summary>
        [Tooltip("The Rigidbody of the object.")]
        [SerializeField]
        Rigidbody _rigidbody;

        /// <summary>
        /// Provides access to the Rigidbody component associated with the interactable object, allowing for physics-based interactions.
        /// </summary>
        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>
        /// An optional origin point for the grab.
        /// </summary>
        [Tooltip("An optional origin point for the grab.")]
        [SerializeField, Optional]
        private Transform _grabSource;

        /// <summary>
        /// If true, use the closest point to the interactor as the grab source.
        /// </summary>
        [Tooltip("If true, use the closest point to the interactor as the grab source.")]
        [SerializeField]
        private bool _useClosestPointAsGrabSource;

        /// <summary>
        ///
        /// </summary>
        [Tooltip(" ")]
        [SerializeField]
        private float _releaseDistance = 0f;

        /// <summary>
        /// Forces a release on all other grabbing interactors when grabbed by a new interactor.
        /// </summary>
        [Tooltip("Forces a release on all other grabbing interactors when grabbed by a new interactor.")]
        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;

        private static CollisionInteractionRegistry<GrabInteractor, GrabInteractable> _grabRegistry = null;

        /// <summary>
        /// Determines whether the closest point to the interactor should be used as the grab source, enhancing precision in grab interactions.
        /// </summary>
        #region Properties
        public bool UseClosestPointAsGrabSource
        {
            get
            {
                return _useClosestPointAsGrabSource;
            }
            set
            {
                _useClosestPointAsGrabSource = value;
            }
        }

        /// <summary>
        /// Specifies the distance at which the object will automatically be released from the grab, providing a limit to maintain realistic interactions.
        /// </summary>
        public float ReleaseDistance
        {
            get
            {
                return _releaseDistance;
            }
            set
            {
                _releaseDistance = value;
            }
        }

        /// <summary>
        /// Indicates whether the grab should be reset when the grab points are updated, allowing for dynamic adjustment during ongoing interactions.
        /// </summary>
        public bool ResetGrabOnGrabsUpdated
        {
            get
            {
                return _resetGrabOnGrabsUpdated;
            }
            set
            {
                _resetGrabOnGrabsUpdated = value;
            }
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Rigidbody, nameof(Rigidbody));
            if (_grabRegistry == null)
            {
                _grabRegistry = new CollisionInteractionRegistry<GrabInteractor, GrabInteractable>();
                SetRegistry(_grabRegistry);
            }
            _colliders = Rigidbody.GetComponentsInChildren<Collider>();
            this.AssertCollectionField(_colliders, nameof(_colliders),
               $"The associated {AssertUtils.Nicify(nameof(Rigidbody))} must have at least one Collider.");
            this.EndStart(ref _started);
        }

        /// <summary>
        /// Calculates the grab source position for a given target, which determines the initial location from which the object will be grabbed.
        /// </summary>
        /// <param name="target">The pose of the interactor targeting the grabbable object.</param>
        /// <returns>The calculated <see cref="Pose"/> representing the grab source.</returns>
        public Pose GetGrabSourceForTarget(Pose target)
        {
            if (_grabSource == null && !_useClosestPointAsGrabSource)
            {
                return target;
            }

            if (_useClosestPointAsGrabSource)
            {
                return new Pose(
                    Collisions.ClosestPointToColliders(target.position, _colliders),
                    target.rotation);
            }

            return _grabSource.GetPose();
        }

        #region Inject

        /// <summary>
        /// Adds a Rigidbody to a dynamically instantiated GameObject.
        /// </summary>
        public void InjectAllGrabInteractable(Rigidbody rigidbody)
        {
            InjectRigidbody(rigidbody);
        }

        /// <summary>
        /// Adds a Rigidbody to the interactable, enabling physics interactions.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to be added to the interactable object.</param>
        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        /// <summary>
        /// Optionally sets a specific transform as the grab source for the interactable, allowing for customized grab initiation points.
        /// </summary>
        /// <param name="grabSource">The transform to be used as the grab source.</param>
        public void InjectOptionalGrabSource(Transform grabSource)
        {
            _grabSource = grabSource;
        }

        /// <summary>
        /// Optionally sets a specific release distance for the interactable, defining how far the interactor can move before the grab is released.
        /// </summary>
        /// <param name="releaseDistance">The release distance to be set.</param>
        public void InjectOptionalReleaseDistance(float releaseDistance)
        {
            _releaseDistance = releaseDistance;
        }
        #endregion
    }
}
