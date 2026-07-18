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
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// ControllerActiveState is a utility component that provides an <see cref="IActiveState"/> implementation
    /// based on whether a controller is connected.
    /// </summary>
    /// <remarks>
    /// This class is deprecated. Use <see cref="ControllerRef"/> instead, which implements both
    /// <see cref="IController"/> and <see cref="IActiveState"/> with the same Active behavior (IsConnected).
    /// </remarks>
    [Obsolete("Use ControllerRef instead, which implements IActiveState with the same behavior (Active = IsConnected)")]
    public class ControllerActiveState : MonoBehaviour, IActiveState
    {
        [Tooltip("ActiveState will be true while this controller is connected.")]
        [SerializeField, Interface(typeof(IController))]
        UnityEngine.Object _controller;

        private IController Controller;

        public bool Active => Controller.IsConnected;

        protected virtual void Awake()
        {
            if (Controller == null)
            {
                Controller = _controller as IController;
            }
        }

        protected virtual void Start()
        {
            this.AssertField(Controller, nameof(Controller));
        }

        #region Inject

        public void InjectAllControllerActiveState(IController controller)
        {
            InjectController(controller);
        }

        public void InjectController(IController controller)
        {
            _controller = controller as UnityEngine.Object;
            Controller = controller;
        }

        #endregion
    }
}
