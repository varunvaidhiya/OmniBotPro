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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// The origin of the ray used by <see cref="RayInteractor"/>s associated with <see cref="IController"/>s.
    /// This is used heavily in Interaction SDK prefabs, wizards, and example scenes, which are also the best
    /// way to adopt this functionality.
    /// </summary>
    /// <remarks>
    /// This type implements <see cref="IActiveState"/> as an indication of whether the underlying controller
    /// interaction is active and updating. This is an unusual pattern, however, and for most usages you should
    /// prefer to monitor an <see cref="InteractorActiveState"/> associated with the downstream interactor rather
    /// than monitoring the activity of a pointer pose.
    /// </remarks>
    public class ControllerPointerPose : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// A controller ray interactor.
        /// </summary>
        [Tooltip("A controller ray interactor.")]
        [SerializeField, Interface(typeof(IController))]
        private UnityEngine.Object _controller;
        /// <summary>
        /// The <see cref="IController"/> from which the downstream <see cref="RayInteractor"/> should be controlled.
        /// </summary>
        public IController Controller { get; private set; }

        /// <summary>
        /// How much the ray origin is offset relative to the controller.
        /// </summary>
        [Tooltip("How much the ray origin is offset relative to the controller.")]
        [SerializeField]
        private Vector3 _offset;

        protected bool _started = false;

        /// <summary>
        /// Implementation of <see cref="IActiveState.Active"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public bool Active { get; private set; }

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Controller, nameof(Controller));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Controller.WhenUpdated += HandleUpdated;
                if (Controller.IsConnected)
                {
                    HandleUpdated();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Controller.WhenUpdated -= HandleUpdated;
            }
        }

        private void HandleUpdated()
        {
            IController controller = Controller;
            if (controller.TryGetPointerPose(out Pose pose))
            {
                pose.position += pose.rotation * (Controller.Scale * _offset);
                transform.SetPose(pose);
                Active = true;
            }
            else
            {
                Active = false;
            }
        }

        #region Inject

        /// <summary>
        /// Sets the <see cref="IController"/> for a dynamically instantiated ControllerPointerPose. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectController(IController controller)
        {
            _controller = controller as UnityEngine.Object;
            Controller = controller;
        }

        /// <summary>
        /// Sets the offset vector for a dynamically instantiated ControllerPointerPose. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOffset(Vector3 offset)
        {
            _offset = offset;
        }

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated ControllerPointerPose; effectively wraps
        /// <see cref="InjectController(IController)"/> and <see cref="InjectOffset(Vector3)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllControllerPointerPose(IController controller, Vector3 offset)
        {
            InjectController(controller);
            InjectOffset(offset);
        }

        #endregion
    }
}
