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

namespace Oculus.Interaction
{
    /// <summary>
    /// Will provide as an IAxis2D the joystick value of an IController.
    /// Note that multiple Axis can be read together, in that case it will
    /// return the sum of all axis.
    /// </summary>
    public class ControllerAxis2D : MonoBehaviour, IAxis2D
    {
        [SerializeField, Interface(typeof(IController))]
        private UnityEngine.Object _controller;
        private IController Controller { get; set; }

        [SerializeField]
        private ControllerAxis2DUsage _axis = ControllerAxis2DUsage.Primary2DAxis;
        public ControllerAxis2DUsage Axis
        {
            get
            {
                return _axis;
            }
            set
            {
                _axis = value;
            }
        }

        protected bool _started;

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Controller, nameof(_controller));
            this.EndStart(ref _started);
        }

        public Vector2 Value()
        {
            Vector2 axisValue = Vector2.zero;
            if (!_started)
            {
                return axisValue;
            }

            if ((_axis & ControllerAxis2DUsage.Primary2DAxis) != 0)
            {
                axisValue = Controller.ControllerInput.Primary2DAxis;
            }

            if ((_axis & ControllerAxis2DUsage.Secondary2DAxis) != 0)
            {
                axisValue += Controller.ControllerInput.Secondary2DAxis;
            }

            return axisValue;
        }

        #region Inject

        public void InjectAllControllerAxis2DActiveState(IController controller)
        {
            InjectController(controller);
        }

        public void InjectController(IController controller)
        {
            Controller = controller;
            _controller = controller as UnityEngine.Object;
        }

        #endregion
    }
}
