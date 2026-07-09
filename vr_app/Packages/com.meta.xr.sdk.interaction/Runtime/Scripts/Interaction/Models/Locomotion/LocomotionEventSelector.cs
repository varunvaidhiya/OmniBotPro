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
using static Oculus.Interaction.Locomotion.LocomotionActionsBroadcaster;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Transforms <see cref="LocomotionAction"/> of type Select incoming from the
    /// <see cref="LocomotionEvent"/> pipeline into a <see cref="ISelector"/>
    /// Select event followed by an Unselect event.
    /// </summary>
    public class LocomotionEventSelector : MonoBehaviour,
        ILocomotionEventHandler, ISelector
    {
        [SerializeField, Optional]
        private Context _context;

        private Action<LocomotionEvent, Pose> _whenLocomotionEventHandled = delegate { };
        public event Action<LocomotionEvent, Pose> WhenLocomotionEventHandled
        {
            add => _whenLocomotionEventHandled += value;
            remove => _whenLocomotionEventHandled -= value;
        }

        private Action _whenSelected = delegate { };
        public event Action WhenSelected
        {
            add => _whenSelected += value;
            remove => _whenSelected -= value;
        }

        private Action _whenUnselected = delegate { };
        public event Action WhenUnselected
        {
            add => _whenUnselected += value;
            remove => _whenUnselected -= value;
        }

        public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            if (TryGetLocomotionActions(locomotionEvent, out LocomotionAction action, _context))
            {
                if (action == LocomotionAction.Select)
                {
                    _whenSelected.Invoke();
                    _whenLocomotionEventHandled.Invoke(locomotionEvent, locomotionEvent.Pose);
                    _whenUnselected.Invoke();
                }
            }
        }
    }
}
