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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This component serves as a nexus point between LocomotionEventBroadcasters
    /// and a LocomotionEventHandler. Use it to group several LocomotionEventBroadcasters
    /// and re-route them to your player controller, or invoke it directly to get the event
    /// forwarded.
    /// </summary>
    public class LocomotionEventsConnection : MonoBehaviour
        , ILocomotionEventHandler, ILocomotionEventBroadcaster
    {
        [SerializeField, Interface(typeof(ILocomotionEventBroadcaster))]
        [Optional(OptionalAttribute.Flag.DontHide)]
        private List<UnityEngine.Object> _broadcasters;
        private List<ILocomotionEventBroadcaster> Broadcasters { get; set; }

        [Obsolete("Use the list of Handlers instead")]
        [SerializeField, Interface(typeof(ILocomotionEventHandler))]
        [Optional(OptionalAttribute.Flag.Obsolete)]
        private UnityEngine.Object _handler;

        [SerializeField, Interface(typeof(ILocomotionEventHandler))]
        private List<UnityEngine.Object> _handlers;
        private List<ILocomotionEventHandler> Handlers { get; set; }

        private bool _started;

        /// <summary>
        /// Implementation of <see cref="ILocomotionEventBroadcaster.WhenLocomotionPerformed"/>;
        /// for details, please refer to the related documentation provided for that interface.
        /// </summary>
        public event Action<LocomotionEvent> WhenLocomotionPerformed = delegate { };
        /// <summary>
        /// Implementation of <see cref="ILocomotionEventHandler.WhenLocomotionEventHandled"/>;
        /// for details, please refer to the related documentation provided for that interface.
        /// </summary>
        public event Action<LocomotionEvent, Pose> WhenLocomotionEventHandled = delegate { };

        protected virtual void Awake()
        {
            if (Broadcasters == null)
            {
                Broadcasters = _broadcasters.ConvertAll(b => b as ILocomotionEventBroadcaster);
            }

            if (Handlers == null)
            {
                Handlers = _handlers.ConvertAll(b => b as ILocomotionEventHandler);

#pragma warning disable CS0618 // Type or member is obsolete
                if (_handler is ILocomotionEventHandler handler
                    && !Handlers.Contains(handler))
                {
                    Handlers.Add(handler);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionItems(Broadcasters, nameof(Broadcasters));
            this.AssertCollectionItems(Handlers, nameof(_handlers));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                foreach (var eventRaiser in Broadcasters)
                {
                    eventRaiser.WhenLocomotionPerformed += HandleLocomotionEvent;
                }
                foreach (var handler in Handlers)
                {
                    handler.WhenLocomotionEventHandled += HandlerWhenLocomotionEventHandled;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                foreach (var eventRaiser in Broadcasters)
                {
                    eventRaiser.WhenLocomotionPerformed -= HandleLocomotionEvent;
                }
                foreach (var handler in Handlers)
                {
                    handler.WhenLocomotionEventHandled -= HandlerWhenLocomotionEventHandled;
                }
            }
        }

        /// <summary>
        /// Implementation of <see cref="ILocomotionEventHandler.HandleLocomotionEvent"/>;
        /// for details, please refer to the related documentation provided for that interface.
        /// </summary>
        private void HandlerWhenLocomotionEventHandled(LocomotionEvent arg1, Pose arg2)
        {
            WhenLocomotionEventHandled.Invoke(arg1, arg2);
        }

        public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            if (_started && this.isActiveAndEnabled)
            {
                WhenLocomotionPerformed.Invoke(locomotionEvent);

                foreach (var handler in Handlers)
                {
                    handler.HandleLocomotionEvent(locomotionEvent);
                }
            }
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="LocomotionEventsConnection"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllLocomotionBroadcastersHandlerConnection(
            List<ILocomotionEventHandler> handlers)
        {
            InjectHandlers(handlers);
        }

        /// <summary>
        /// Sets the underlying <see cref="ILocomotionEventBroadcaster"/> set for a dynamically instantiated
        /// <see cref="LocomotionEventsConnection"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalBroadcasters(List<ILocomotionEventBroadcaster> broadcasters)
        {
            Broadcasters = broadcasters;
            _broadcasters = broadcasters.ConvertAll(b => b as UnityEngine.Object);
        }

        /// <summary>
        /// Sets the underlying <see cref="ILocomotionEventHandler"/> set for a dynamically instantiated
        /// <see cref="LocomotionEventsConnection"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHandlers(List<ILocomotionEventHandler> handlers)
        {
            Handlers = handlers;
            _handlers = handlers.ConvertAll(b => b as UnityEngine.Object);
        }

        /// <summary>
        /// Sets the underlying <see cref="ILocomotionEventHandler"/> for a dynamically instantiated
        /// <see cref="LocomotionEventsConnection"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        [Obsolete("Use the list version instead")]
        public void InjectHandler(ILocomotionEventHandler handler)
        {
            _handler = handler as UnityEngine.Object;
        }

        #endregion
    }
}
