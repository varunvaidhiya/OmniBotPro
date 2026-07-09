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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// A utility component that delegates all of its <see cref="IHmd"/>
    /// implementation to the provided <see cref="IHmd"/> source object.
    /// </summary>
    public class HmdRef : MonoBehaviour, IHmd
    {
        [SerializeField, Interface(typeof(IHmd))]
        private UnityEngine.Object _hmd;
        private IHmd Hmd;

        /// <summary>
        /// Implementation of <see cref="IHmd.WhenUpdated"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public event Action WhenUpdated
        {
            add => Hmd.WhenUpdated += value;
            remove => Hmd.WhenUpdated -= value;
        }

        protected virtual void Awake()
        {
            Hmd = _hmd as IHmd;
        }

        protected virtual void Start()
        {
            this.AssertField(Hmd, nameof(Hmd));
        }

        /// <summary>
        /// Implementation of <see cref="IHmd.TryGetRootPose(out Pose)"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool TryGetRootPose(out Pose pose)
        {
            return Hmd.TryGetRootPose(out pose);
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated <see cref="HmdRef"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHmdRef(IHmd hmd)
        {
            InjectHmd(hmd);
        }

        /// <summary>
        /// Sets the underlying <see cref="IHmd"/> for a dynamically instantiated <see cref="HmdRef"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHmd(IHmd hmd)
        {
            _hmd = hmd as UnityEngine.Object;
            Hmd = hmd;
        }
        #endregion
    }
}
