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
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// <see cref="ISelector"/> for determining whether an <see cref="IController"/> is selecting based on button presses.
    /// This is the most common controller-based selection mechanism for all interactions.
    /// </summary>
    /// <remarks>
    /// It is possible to have multiple of these selectors monitoring the same <see cref="IController"/>, particularly when
    /// different selection mechanisms apply to different interaction types. For example, if simultaneously controlling a
    /// <see cref="RayInteractor"/> from the trigger button and <see cref="GrabInteractor"/> from the grip button, each of
    /// those interactors would have its own associated ControllerSelector specific to the relevant buttons.
    /// </remarks>
    public class ControllerSelector : MonoBehaviour, ISelector
    {
        /// <summary>
        /// Determines whether selection requires all <see cref="ControllerButtonUsage"/> buttons, or merely one of them, in
        /// order to enter a selection state. Used by the <see cref="RequireButtonUsages"/> property.
        /// </summary>
        public enum ControllerSelectorLogicOperator
        {
            Any = 0,
            All = 1
        }

        /// <summary>
        /// The controller to check.
        /// </summary>
        [Tooltip("The controller to check.")]
        [SerializeField, Interface(typeof(IController))]
        private UnityEngine.Object _controller;

        /// <summary>
        /// The buttons to check.
        /// </summary>
        [Tooltip("The buttons to check.")]
        [SerializeField]
        private ControllerButtonUsage _controllerButtonUsage;

        /// <summary>
        /// Determines how many of the checked buttons must be pressed for the controller to be selecting. 'All' requires all of the buttons to be pressed. 'Any' requires only one to be pressed.
        /// </summary>
        [Tooltip("Determines how many of the checked buttons must be pressed for the controller to be selecting. 'All' requires all of the buttons to be pressed. 'Any' requires only one to be pressed.")]
        [SerializeField]
        private ControllerSelectorLogicOperator _requireButtonUsages =
            ControllerSelectorLogicOperator.Any;

        #region Properties
        /// <summary>
        /// Flag set determining which buttons on <see cref="Controller"/> can be activated (pressed, touched, etc.)
        /// in order for this ControllerSelector to enter a "select" state. Depending on <see cref="RequireButtonUsages"/>,
        /// one or all of these buttons may be required to select.
        /// </summary>
        public ControllerButtonUsage ControllerButtonUsage
        {
            get
            {
                return _controllerButtonUsage;
            }
            set
            {
                _controllerButtonUsage = value;
            }
        }

        /// <summary>
        /// Determines whether selection requires all <see cref="ControllerButtonUsage"/> buttons, or merely one of them, in
        /// order to enter a selection state.
        /// </summary>
        public ControllerSelectorLogicOperator RequireButtonUsages
        {
            get
            {
                return _requireButtonUsages;
            }
            set
            {
                _requireButtonUsages = value;
            }
        }
        #endregion

        /// <summary>
        /// The <see cref="IController"/> which dictates the state of this selector. <see cref="WhenSelected"/> and
        /// <see cref="WhenUnselected"/> will be invoked based on the input state of this controller, as specified
        /// by the <see cref="ControllerButtonUsage"/> and <see cref="RequireButtonUsages"/> properties.
        /// </summary>
        public IController Controller { get; private set; }

        /// <summary>
        /// Implementation of <see cref="ISelector.WhenSelected"/>; for details, please refer to
        /// the related documentation provided for that event.
        /// </summary>
        public event Action WhenSelected = delegate { };

        /// <summary>
        /// Implementation of <see cref="ISelector.WhenUnselected"/>; for details, please refer to
        /// the related documentation provided for that event.
        /// </summary>
        public event Action WhenUnselected = delegate { };

        private bool _selected;

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.AssertField(Controller, nameof(Controller));
        }

        protected virtual void Update()
        {
            bool selected = _requireButtonUsages == ControllerSelectorLogicOperator.All
                ? Controller.IsButtonUsageAllActive(_controllerButtonUsage)
                : Controller.IsButtonUsageAnyActive(_controllerButtonUsage);

            if (selected)
            {
                if (_selected) return;
                _selected = true;
                WhenSelected();
            }
            else
            {
                if (!_selected) return;
                _selected = false;
                WhenUnselected();
            }
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated ControllerSelector; effectively wraps
        /// <see cref="InjectController(IController)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllControllerSelector(IController controller)
        {
            InjectController(controller);
        }

        /// <summary>
        /// Sets an <see cref="IController"/> for a dynamically instantiated ControllerSelector. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectController(IController controller)
        {
            _controller = controller as UnityEngine.Object;
            Controller = controller;
        }

        #endregion
    }
}
