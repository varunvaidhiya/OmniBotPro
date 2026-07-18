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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// LocomotionActionsBroadcaster generates LocomotionEvents without a Translation or Rotation but decorates
    /// them with extra information regarding instantaneous locomotion actions that can be consumed by a Locomotor.
    /// Decorations are only active during the WhenLocomotionPerformed event and should be consumed
    /// and managed by the specific implementations
    /// </summary>
    public class LocomotionActionsBroadcaster : MonoBehaviour,
        ILocomotionEventBroadcaster
    {
        public enum LocomotionAction
        {
            Crouch,
            StandUp,
            ToggleCrouch,
            Run,
            Walk,
            ToggleRun,
            Jump,
            InvalidTarget,
            Select
        }

        [SerializeField, Optional]
        private Context _context;

        private UniqueIdentifier _identifier;

        public int Identifier => _identifier.ID;

        public event Action<LocomotionEvent> WhenLocomotionPerformed = delegate { };

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(_context != null ? _context : Context.Global.GetInstance(), this);
        }

        /// <summary>
        /// Sends an Action down the LocomotionEvent pipeline and inmediately disposes it
        /// </summary>
        /// <param name="action">The action to be sent</param>
        public void SendLocomotionAction(LocomotionAction action)
        {
            SendLocomotionAction(action, Pose.identity);
        }

        /// <summary>
        /// Sends an Action down the LocomotionEvent pipeline and inmediately disposes it
        /// </summary>
        /// <param name="action">The action to be sent</param>
        /// <param name="target">The pose to associate with the action</param>
        public void SendLocomotionAction(LocomotionAction action, Pose target)
        {
            LocomotionEvent locomotionEvent = CreateLocomotionEventAction(Identifier, action, target, _context);
            WhenLocomotionPerformed.Invoke(locomotionEvent);
            //Decorations are disposed immediately after sending them
            DisposeLocomotionAction(locomotionEvent);
        }

        #region Inspector Helpers

        /// <summary>
        /// Sends an immediate Crouch Action down the LocomotionEvent pipeline
        /// </summary>
        public void Crouch()
        {
            SendLocomotionAction(LocomotionAction.Crouch);
        }

        /// <summary>
        /// Sends an immediate StandUp Action down the LocomotionEvent pipeline
        /// </summary>
        public void StandUp()
        {
            SendLocomotionAction(LocomotionAction.StandUp);
        }

        /// <summary>
        /// Sends an immediate ToggleCrouch Action down the LocomotionEvent pipeline
        /// </summary>
        public void ToggleCrouch()
        {
            SendLocomotionAction(LocomotionAction.ToggleCrouch);
        }

        /// <summary>
        /// Sends an immediate Run Action down the LocomotionEvent pipeline
        /// </summary>
        public void Run()
        {
            SendLocomotionAction(LocomotionAction.Run);
        }

        /// <summary>
        /// Sends an immediate Walk Action down the LocomotionEvent pipeline
        /// </summary>
        public void Walk()
        {
            SendLocomotionAction(LocomotionAction.Walk);
        }

        /// <summary>
        /// Sends an immediate ToggleRun Action down the LocomotionEvent pipeline
        /// </summary>
        public void ToggleRun()
        {
            SendLocomotionAction(LocomotionAction.ToggleRun);
        }

        /// <summary>
        /// Sends an immediate default Jump Action down the LocomotionEvent pipeline
        /// </summary>
        public void Jump()
        {
            SendLocomotionAction(LocomotionAction.Jump, Pose.identity);
        }

        /// <summary>
        /// Sends an immediate Jump Action down the LocomotionEvent pipeline
        /// </summary>
        /// <param name="force">The force to be applied to the jump</param>
        public void Jump(Vector3 force)
        {
            SendLocomotionAction(LocomotionAction.Jump, new Pose(force, Quaternion.identity));
        }

        /// <summary>
        /// Marks the locomotionEvent as an invalid target
        /// </summary>
        public void Invalid(Pose pose)
        {
            SendLocomotionAction(LocomotionAction.InvalidTarget, pose);
        }

        /// <summary>
        /// Marks the LocomotionEvent as a Select event
        /// </summary>
        public void Select()
        {
            SendLocomotionAction(LocomotionAction.Select);
        }
        #endregion

        #region Injects

        public void InjectOptionalContext(Context context)
        {
            _context = context;
        }

        #endregion

        /// <summary>
        /// This utility method allows creating LocomotionAction decorations
        /// </summary>
        /// <param name="identifier">The identifier of the sender</param>
        /// <param name="action">The action to send</param>
        /// <param name="pose">The pose to be sent in the LocomotionEvent, note that this won't have any Translation or Rotation applied</param>
        /// <param name="context">The context used for storing the decoration</param>
        /// <returns>The LocomotionEvent decorated with the action</returns>
        public static LocomotionEvent CreateLocomotionEventAction(int identifier, LocomotionAction action, Pose pose = default, Context context = null)
        {
            LocomotionEvent locomotionEvent = new LocomotionEvent(identifier,
                pose, LocomotionEvent.TranslationType.None, LocomotionEvent.RotationType.None);
            Decorator.GetFromContext(context).AddDecoration(locomotionEvent.EventId, action);
            return locomotionEvent;
        }

        /// <summary>
        /// This utility methods allows retrieving the LocomotionAction from a given LocomotionEvent, if it contains one.
        /// </summary>
        /// <param name="locomotionEvent">The LocomotionEvent potentially containing the LocomotionAction</param>
        /// <param name="action">The LocomotionAction that it contained</param>
        /// <param name="context">The context used for storing the decoration</param>
        /// <returns>True if the LocomotionEvent contained a valid LocomotionAction</returns>
        public static bool TryGetLocomotionActions(LocomotionEvent locomotionEvent, out LocomotionAction action, Context context = null)
        {
            if (Decorator.GetFromContext(context).TryGetDecoration(locomotionEvent.EventId, out LocomotionAction decoration))
            {
                action = decoration;
                return true;
            }
            action = default;
            return false;
        }

        /// <summary>
        /// This utility method removes the LocomotionAction decoration from the LocomotionEvent.
        /// These decorations are not automatically removed, so it is important to manually call this method
        /// to avoid filling the memory with obsolete decorations
        /// </summary>
        /// <param name="locomotionEvent">The event containing the LocomotionAction decoration.</param>
        /// <param name="context">The context storing the decoration</param>
        public static void DisposeLocomotionAction(LocomotionEvent locomotionEvent, Context context = null)
        {
            Decorator.GetFromContext(context).RemoveDecoration(locomotionEvent.EventId);
        }

        private class Decorator : ValueToValueDecorator<ulong, LocomotionAction>
        {
            private Decorator() { }

            public static Decorator GetFromContext(Context context = null)
            {
                if (context == null)
                {
                    context = Context.Global.GetInstance();
                }
                return context.GetOrCreateSingleton<Decorator>(() => new());
            }
        }
    }
}
