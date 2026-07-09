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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Sets the origin of the ray used by <see cref="RayInteractor"/>s for tracked hands.
    /// </summary>
    /// <remarks>
    /// RayInteractors themselves are agnostic to the origin of the ray, so they can work with any
    /// ray but cannot calculate that ray for themselves. HandPointerPose observes an <see cref="IHand"/>
    /// and uses it to compute a ray origin.
    /// </remarks>
    public class HandPointerPose : MonoBehaviour, IActiveState
    {
        [Tooltip("The hand used for ray interaction")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// The <see cref="IHand"/> used for ray interaction.
        /// </summary>
        /// <remarks>
        /// This value is typically set in the Unity Editor, but it can also be set programmatically during
        /// initialization using either <see cref="InjectAllHandPointerPose(IHand, Vector3)"/> or
        /// <see cref="InjectHand(IHand)"/>.
        /// </remarks>
        public IHand Hand { get; private set; }

        /// <summary>
        /// How much the ray origin is offset relative to the hand.
        /// </summary>
        [Tooltip("How much the ray origin is offset relative to the hand.")]
        [SerializeField]
        private Vector3 _offset;

        /// <summary>
        /// Implements <see cref="IActiveState.Active"/>, in this case indicating whether or not a valid ray
        /// origin is currently available.
        /// </summary>
        public bool Active => Hand.IsPointerPoseValid;

        protected bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandUpdated;
                if (Hand.IsConnected)
                {
                    HandleHandUpdated();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;
            }
        }

        private void HandleHandUpdated()
        {
            if (Hand.GetPointerPose(out Pose pointerPose))
            {
                pointerPose.position += pointerPose.rotation * _offset;
                transform.SetPose(pointerPose);
            }
        }

        #region Inject

        /// <summary>
        /// Sets all required dependencies for a dynamically instantiated HandPointerPose. This is a convenience method wrapping
        /// <see cref="InjectHand(IHand)"/> and <see cref="InjectOffset(Vector3)"/>. This method exists to support Interaction
        /// SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHandPointerPose(IHand hand,
            Vector3 offset)
        {
            InjectHand(hand);
            InjectOffset(offset);
        }

        /// <summary>
        /// Sets the an <see cref="IHand"/> as the <see cref="Hand"/> for a dynamically instantiated HandPointerPose. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        /// <summary>
        /// Sets a Unity Vector3 as the ray origin offset for a dynamically instantiated HandPointerPose. This method exists to
        /// support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOffset(Vector3 offset)
        {
            _offset = offset;
        }

        #endregion
    }
}
