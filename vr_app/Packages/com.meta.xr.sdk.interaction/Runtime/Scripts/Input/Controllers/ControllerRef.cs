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
    /// A utility component that delegates all of its <see cref="IController"/>
    /// implementation to the provided <see cref="IController"/> source object.
    /// </summary>
    public class ControllerRef : MonoBehaviour, IController, IActiveState
    {
        [SerializeField, Interface(typeof(IController))]
        private UnityEngine.Object _controller;
        private IController Controller;

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.AssertField(Controller, nameof(Controller));
        }

        /// <summary>
        /// Implementation of <see cref="IController.Handedness"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public Handedness Handedness => Controller.Handedness;

        /// <summary>
        /// Implementation of <see cref="IController.IsConnected"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool IsConnected => Controller.IsConnected;

        /// <summary>
        /// Implementation of <see cref="IController.IsPoseValid"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool IsPoseValid => Controller.IsPoseValid;

        /// <summary>
        /// Implementation of <see cref="IController.ControllerInput"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public ControllerInput ControllerInput => Controller.ControllerInput;

        /// <summary>
        /// Implementation of <see cref="IController.WhenUpdated"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public event Action WhenUpdated
        {
            add => Controller.WhenUpdated += value;
            remove => Controller.WhenUpdated -= value;
        }

        /// <summary>
        /// Implementation of <see cref="IController.Active"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool Active => IsConnected;

        /// <summary>
        /// Implementation of <see cref="IController.TryGetPose"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool TryGetPose(out Pose pose)
        {
            return Controller.TryGetPose(out pose);
        }

        /// <summary>
        /// Implementation of <see cref="IController.TryGetPointerPose"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool TryGetPointerPose(out Pose pose)
        {
            return Controller.TryGetPointerPose(out pose);
        }

        /// <summary>
        /// Implementation of <see cref="IController.Scale"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public float Scale => Controller.Scale;

        /// <summary>
        /// Implementation of <see cref="IController.IsButtonUsageAnyActive"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool IsButtonUsageAnyActive(ControllerButtonUsage buttonUsage)
        {
            return Controller.IsButtonUsageAnyActive(buttonUsage);
        }

        /// <summary>
        /// Implementation of <see cref="IController.IsButtonUsageAllActive"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool IsButtonUsageAllActive(ControllerButtonUsage buttonUsage)
        {
            return Controller.IsButtonUsageAllActive(buttonUsage);
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated <see cref="ControllerRef"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllControllerRef(IController controller)
        {
            InjectController(controller);
        }

        /// <summary>
        /// Sets the underlying <see cref="IController"/> for a dynamically instantiated <see cref="ControllerRef"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectController(IController controller)
        {
            _controller = controller as UnityEngine.Object;
            Controller = controller;
        }

        #endregion
    }
}
