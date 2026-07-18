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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Provides a hand-specific, collisions-based, grab interaction model (distinct from <see cref="HandGrab.HandGrabInteractor"/>'s
    /// spatial hueristic approach) where selection begins when finger tips overlap with an associated interactable. This interaction
    /// mode simulates the physicality of grabbing a real-world object with the fingertips.
    /// </summary>
    public class TouchHandGrabInteractable : PointerInteractable<TouchHandGrabInteractor, TouchHandGrabInteractable>
    {
        [SerializeField]
        private Collider _boundsCollider;

        [SerializeField]
        private List<Collider> _colliders;

        private ColliderGroup _colliderGroup;

        /// <summary>
        /// Retrieves the <see cref="Interaction.ColliderGroup"/> associated with this interactable.
        /// </summary>
        /// <remarks>
        /// TouchHandGrabInteractables can require multiple Colliders in order to effectively characterize the grabbable shape,
        /// particularly for complex models, so a group is used to aggregate them.
        /// </remarks>
        public ColliderGroup ColliderGroup => _colliderGroup;

        protected override void Start()
        {
            base.Start();
            this.AssertField(_boundsCollider, nameof(_boundsCollider));
            this.AssertCollectionField(_colliders, nameof(_colliders));
            _colliderGroup = new ColliderGroup(_colliders, _boundsCollider);
        }

        #region Inject

        /// <summary>
        /// Convenience method combining <see cref="InjectBoundsCollider(Collider)"/> and <see cref="InjectColliders(List{Collider})"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectAllTouchHandGrabInteractable(Collider boundsCollider, List<Collider> colliders)
        {
            InjectBoundsCollider(boundsCollider);
            InjectColliders(colliders);
        }

        /// <summary>
        /// Sets a Unity Collider as the bounds collider for a dynamically instantiated TouchHandGrabInteractable. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectBoundsCollider(Collider boundsCollider)
        {
            _boundsCollider = boundsCollider;
        }

        /// <summary>
        /// Sets the list of Unity Colliders to be made into an <see cref="Interaction.ColliderGroup"/> of a dynamically
        /// instantiated TouchHandGrabInteractable. Because <see cref="ColliderGroup"/> is instantiated from this list in Unity's
        /// Start() method, this _must_ be called before the MonoBehaviour lifecycle initializes this instance; otherwise, the
        /// provided <paramref name="colliders"/> will be ignored. This method exists to support Interaction SDK's dependency
        /// injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectColliders(List<Collider> colliders)
        {
            _colliders = colliders;
        }

        #endregion
    }
}
