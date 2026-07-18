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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// HandRef is a utility component that delegates all of its <see cref="IHand"/> implementation
    /// to the provided <see cref="Hand"/> object.
    /// </summary>
    /// <remarks>
    /// HandRef can be thought of as a "redirect," which is useful for making Unity Component configurations
    /// flexible with limited setup. For example, if making a prefab containing multiple hand-referencing
    /// components which should be usable with either hand, it is more convenient to have a single HandRef
    /// at the root of the prefab (to which all the other Components connect) and connect only that to the
    /// desired hand versus having to connect every Component individually for every instance of the prefab.
    /// </remarks>
    public class HandRef : MonoBehaviour, IHand, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// The underlying <see cref="IHand"/> to which this HandRef is a shim. All IHand methods invoked on
        /// this HandRef will be passed along to this instance.
        /// </summary>
        public IHand Hand { get; private set; }

        /// <summary>
        /// Retrieves the <see cref="IHand.Handedness"/> of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public Handedness Handedness => Hand.Handedness;

        /// <summary>
        /// Retrieves the <see cref="IHand.IsConnected"/> value of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public bool IsConnected => Hand.IsConnected;

        /// <summary>
        /// Retrieves the <see cref="IHand.IsHighConfidence"/> value of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public bool IsHighConfidence => Hand.IsHighConfidence;

        /// <summary>
        /// Retrieves the <see cref="IHand.IsDominantHand"/> value of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public bool IsDominantHand => Hand.IsDominantHand;

        /// <summary>
        /// Retrieves the <see cref="IHand.Scale"/> value of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public float Scale => Hand.Scale;

        /// <summary>
        /// Retrieves the <see cref="IHand.IsPointerPoseValid"/> value of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public bool IsPointerPoseValid => Hand.IsPointerPoseValid;

        /// <summary>
        /// Retrieves the <see cref="IHand.IsTrackedDataValid"/> value of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public bool IsTrackedDataValid => Hand.IsTrackedDataValid;

        /// <summary>
        /// Retrieves the <see cref="IHand.CurrentDataVersion"/> value of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public int CurrentDataVersion => Hand.CurrentDataVersion;

        /// <summary>
        /// Retrieves the <see cref="IHand.WhenHandUpdated"/> event of the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Hand"/>.
        /// </remarks>
        public event Action WhenHandUpdated
        {
            add => Hand.WhenHandUpdated += value;
            remove => Hand.WhenHandUpdated -= value;
        }

        /// <summary>
        /// Implements <see cref="IActiveState.Active"/>, in this case indicating whether the underlying
        /// <see cref="Hand"/> is connected. This is a remapping method which makes the
        /// <see cref="IsConnected"/> value available to consumers treating the HandRef as an
        /// <see cref="IActiveState"/>.
        /// </summary>
        public bool Active => IsConnected;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.AssertField(Hand, nameof(Hand));
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetFingerIsPinching(HandFinger)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetFingerIsPinching(HandFinger finger)
        {
            return Hand.GetFingerIsPinching(finger);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetIndexFingerIsPinching()"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetIndexFingerIsPinching()
        {
            return Hand.GetIndexFingerIsPinching();
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetPointerPose(out Pose)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetPointerPose(out Pose pose)
        {
            return Hand.GetPointerPose(out pose);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetJointPose(HandJointId, out Pose)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetJointPose(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPose(handJointId, out pose);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetJointPoseLocal(HandJointId, out Pose)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetJointPoseLocal(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPoseLocal(handJointId, out pose);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetJointPosesLocal(out ReadOnlyHandJointPoses)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetJointPosesLocal(out ReadOnlyHandJointPoses jointPosesLocal)
        {
            return Hand.GetJointPosesLocal(out jointPosesLocal);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetJointPoseFromWrist(HandJointId, out Pose)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPoseFromWrist(handJointId, out pose);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist)
        {
            return Hand.GetJointPosesFromWrist(out jointPosesFromWrist);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetPalmPoseLocal(out Pose)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetPalmPoseLocal(out Pose pose)
        {
            return Hand.GetPalmPoseLocal(out pose);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetFingerIsHighConfidence(HandFinger)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetFingerIsHighConfidence(HandFinger finger)
        {
            return Hand.GetFingerIsHighConfidence(finger);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetFingerPinchStrength(HandFinger)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public float GetFingerPinchStrength(HandFinger finger)
        {
            return Hand.GetFingerPinchStrength(finger);
        }

        /// <summary>
        /// Invokes <see cref="IHand.GetRootPose(out Pose)"/> on the underlying <see cref="Hand"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Hand"/>.
        /// </remarks>
        public bool GetRootPose(out Pose pose)
        {
            return Hand.GetRootPose(out pose);
        }

        #region Inject
        /// <summary>
        /// Sets all required dependencies for a dynamically instantiated HandRef. This is a convenience method wrapping
        /// <see cref="InjectHand(IHand)"/>. This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHandRef(IHand hand)
        {
            InjectHand(hand);
        }

        /// <summary>
        /// Sets the an <see cref="IHand"/> as the <see cref="Hand"/> for a dynamically instantiated HandRef. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }
        #endregion
    }
}
