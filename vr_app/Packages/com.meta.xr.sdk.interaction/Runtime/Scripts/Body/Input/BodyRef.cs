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

using Oculus.Interaction.Body.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// BodyRef is a utility component that delegates all of its <see cref="IBody"/> implementation
    /// to the provided <see cref="Body"/> object.
    /// </summary>
    /// <remarks>
    /// BodyRef can be thought of as a "redirect," which is useful for making Unity Component configurations
    /// flexible with limited setup. For example, if making a prefab containing multiple body-referencing
    /// components which should be usable with either body, it is more convenient to have a single BodyRef
    /// at the root of the prefab (to which all the other Components connect) and connect only that to the
    /// desired body versus having to connect every Component individually for every instance of the prefab.
    /// </remarks>
    public class BodyRef : MonoBehaviour, IBody, IActiveState
    {
        [SerializeField, Interface(typeof(IBody))]
        private UnityEngine.Object _body;

        /// <summary>
        /// The underlying <see cref="IBody"/> to which this BodyRef is a shim. All IBody methods invoked on
        /// this BodyRef will be passed along to this instance.
        /// </summary>
        public IBody Body { get; private set; }

        /// <summary>
        /// Retrieves the <see cref="IBody.IsConnected"/> value of the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Body"/>.
        /// </remarks>
        public bool IsConnected => Body.IsConnected;

        /// <summary>
        /// Retrieves the <see cref="IBody.IsHighConfidence"/> value of the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Body"/>.
        /// </remarks>
        public bool IsHighConfidence => Body.IsHighConfidence;


        /// <summary>
        /// Retrieves the <see cref="IBody.Scale"/> value of the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Body"/>.
        /// </remarks>
        public float Scale => Body.Scale;

        /// <summary>
        /// Retrieves the <see cref="IBody.IsTrackedDataValid"/> value of the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Body"/>.
        /// </remarks>
        public bool IsTrackedDataValid => Body.IsTrackedDataValid;

        /// <summary>
        /// Retrieves the <see cref="IBody.CurrentDataVersion"/> value of the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Body"/>.
        /// </remarks>
        public int CurrentDataVersion => Body.CurrentDataVersion;

        /// <summary>
        /// Retrieves the <see cref="IBody.WhenBodyUpdated"/> event of the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Body"/>.
        /// </remarks>
        public event Action WhenBodyUpdated
        {
            add => Body.WhenBodyUpdated += value;
            remove => Body.WhenBodyUpdated -= value;
        }

        /// <summary>
        /// Implements <see cref="IActiveState.Active"/>, in this case indicating whether the underlying
        /// <see cref="Body"/> is connected. This is a remapping method which makes the
        /// <see cref="IsConnected"/> value available to consumers treating the BodyRef as an
        /// <see cref="IActiveState"/>.
        /// </summary>
        public bool Active => IsConnected;

        /// <summary>
        /// Retrieves the <see cref="IBody.SkeletonMapping"/> value of the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim property equivalent to accessing the same property on the <see cref="Body"/>.
        /// </remarks>
        public ISkeletonMapping SkeletonMapping => Body.SkeletonMapping;

        protected virtual void Awake()
        {
            if (Body == null)
            {
                Body = _body as IBody;
            }
        }

        protected virtual void Start()
        {
            this.AssertField(Body, nameof(_body));
        }

        /// <summary>
        /// Invokes <see cref="IBody.GetRootPose(out Pose)"/> on the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Body"/>.
        /// </remarks>
        public bool GetRootPose(out Pose pose)
        {
            return Body.GetRootPose(out pose);
        }

        /// <summary>
        /// Invokes <see cref="IBody.GetJointPose(BodyJointId bodyJointId, out Pose)"/> on the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Body"/>.
        /// </remarks>
        public bool GetJointPose(BodyJointId bodyJointId, out Pose pose)
        {
            return Body.GetJointPose(bodyJointId, out pose);
        }

        /// <summary>
        /// Invokes <see cref="IBody.GetJointPoseLocal(BodyJointId bodyJointId, out Pose)"/> on the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Body"/>.
        /// </remarks>
        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose)
        {
            return Body.GetJointPoseLocal(bodyJointId, out pose);
        }

        /// <summary>
        /// Invokes <see cref="IBody.GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose)"/> on the underlying <see cref="Body"/>.
        /// </summary>
        /// <remarks>
        /// This is a pure shim method equivalent to invoking the same method on the <see cref="Body"/>.
        /// </remarks>
        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose)
        {
            return Body.GetJointPoseFromRoot(bodyJointId, out pose);
        }
        #region Inject

        /// <summary>
        /// Sets all required dependencies for a dynamically instantiated BodyRef. This is a convenience method wrapping
        /// <see cref="InjectBody(IBody)"/>. This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllBodyRef(IBody body)
        {
            InjectBody(body);
        }

        /// <summary>
        /// Sets the an <see cref="IBody"/> as the <see cref="Body"/> for a dynamically instantiated BodyRef. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectBody(IBody body)
        {
            _body = body as UnityEngine.Object;
            Body = body;
        }
        #endregion
    }
}
