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

using Oculus.Interaction.HandGrab;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Manages interaction event broadcasting from various <see cref="IInteractableView"/> instances.
    /// It centralizes core interaction events (like Hover, Select) and UI pointer events,
    /// forwarding them to a specified <see cref="InteractionEventChannel"/>.
    /// This component observes <see cref="InteractableRegistry{TInteractor, TInteractable}"/> for dynamic
    /// registration and unregistration of interactables, and manages individual event listeners.
    /// It also directly hooks into <see cref="PointableCanvasModule"/> for UI pointer events.
    /// </summary>
    public sealed class InteractionBroadcaster : MonoBehaviour
    {
        /// <summary>
        /// The ScriptableObject event channel asset through which interaction events are broadcast.
        /// </summary>
        [Tooltip("The event channel to broadcast interaction events on.")]
        [SerializeField]
        internal InteractionEventChannel _eventChannel;

        /// <summary>
        /// If true, common interaction types (e.g., Grab, Poke, Ray, HandGrab variants)
        /// and UI pointer events will be automatically registered for broadcasting when this component is enabled.
        /// </summary>
        [Tooltip("If true, standard interaction types (Grab, Poke, Ray) will be automatically registered on enable.")]
        [SerializeField]
        private bool _autoRegisterInteractionTypes = true;

        /// <summary>
        /// Uses weak references to track event handlers for interactables, enabling automatic garbage collection
        /// when interactables are destroyed.
        /// </summary>
        private readonly ConditionalWeakTable<IInteractableView, Handler> _handlers = new();

        /// <summary>
        /// Singleton instance of the <see cref="InteractionBroadcaster"/>.
        /// </summary>
        private static InteractionBroadcaster _instance;
        private bool _started = false;

        /// <summary>
        /// Gets the active singleton instance of the <see cref="InteractionBroadcaster"/>.
        /// Returns null if no instance is active.
        /// </summary>
        public static InteractionBroadcaster Instance => _instance;

        /// <summary>
        /// Optional C# event for interaction monitoring. Complements the
        /// <see cref="_eventChannel"/> ScriptableObject workflow with a pure C#
        /// implementation. Subscribers receive all interaction events regardless
        /// of the ScriptableObject channel configuration.
        /// </summary>
        /// <remarks>
        /// Use this event for performance-sensitive systems or when you need to avoid
        /// ScriptableObject dependencies.
        /// </remarks>
        public static event Action<InteractionEvent> OnEventRaised;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"Multiple instances of {nameof(InteractionBroadcaster)} found. Destroying this one on {gameObject.name}.", this);
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_instance, nameof(_instance));
            this.EndStart(ref _started);
        }

        private void OnEnable()
        {
            if (!_started || _instance != this) return;

            if (_autoRegisterInteractionTypes)
            {
                RegisterInteractionType<GrabInteractor, GrabInteractable>();
                RegisterInteractionType<PokeInteractor, PokeInteractable>();
                RegisterInteractionType<RayInteractor, RayInteractable>();
                RegisterInteractionType<HandGrabInteractor, HandGrabInteractable>();
                RegisterInteractionType<HandGrabUseInteractor, HandGrabUseInteractable>();
                RegisterInteractionType<DistanceGrabInteractor, DistanceGrabInteractable>();
                RegisterInteractionType<DistanceHandGrabInteractor, DistanceHandGrabInteractable>();
                RegisterInteractionType<TouchHandGrabInteractor, TouchHandGrabInteractable>();

                // UI pointer hooks
                PointableCanvasModule.WhenSelectableHovered += OnSelectableHovered;
                PointableCanvasModule.WhenSelectableUnhovered += OnSelectableUnhovered;
                PointableCanvasModule.WhenSelected += OnSelected;
                PointableCanvasModule.WhenUnselected += OnUnSelected;
            }
        }

        private void OnDisable()
        {
            if (_instance != this) return; // Only the active instance should unregister

            UnregisterInteractionType<GrabInteractor, GrabInteractable>();
            UnregisterInteractionType<PokeInteractor, PokeInteractable>();
            UnregisterInteractionType<RayInteractor, RayInteractable>();
            UnregisterInteractionType<HandGrabInteractor, HandGrabInteractable>();
            UnregisterInteractionType<HandGrabUseInteractor, HandGrabUseInteractable>();
            UnregisterInteractionType<DistanceGrabInteractor, DistanceGrabInteractable>();
            UnregisterInteractionType<DistanceHandGrabInteractor, DistanceHandGrabInteractable>();
            UnregisterInteractionType<TouchHandGrabInteractor, TouchHandGrabInteractable>();

            PointableCanvasModule.WhenSelectableHovered -= OnSelectableHovered;
            PointableCanvasModule.WhenSelectableUnhovered -= OnSelectableUnhovered;
            PointableCanvasModule.WhenSelected -= OnSelected;
            PointableCanvasModule.WhenUnselected -= OnUnSelected;


            foreach (var keyValuePair in _handlers)
            {
                UnregisterInteractable(keyValuePair.Key);
            }

            _handlers.Clear();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Registers a specific pair of Interactor and Interactable types for event monitoring.
        /// This method subscribes to the static registration events of the corresponding
        /// <see cref="InteractableRegistry{TInteractor, TInteractable}"/> and registers all
        /// currently existing interactables of that type.
        /// </summary>
        /// <typeparam name="TInteractor">The type of Interactor.</typeparam>
        /// <typeparam name="TInteractable">The type of Interactable, which must implement <see cref="IInteractableView"/>.</typeparam>
        public void RegisterInteractionType<TInteractor, TInteractable>()
            where TInteractor : Interactor<TInteractor, TInteractable>
            where TInteractable : Interactable<TInteractor, TInteractable>, IInteractableView
        {
            if (!_started || _instance != this)
            {
                Debug.LogWarning($"Attempted to call {nameof(RegisterInteractionType)} on {name} before it was started or on a non-singleton instance.", this);
                return;
            }

            var registryList = InteractableRegistry<TInteractor, TInteractable>.ListStatic();

            if (registryList == null)
            {
                return;
            }

            InteractableRegistry<TInteractor, TInteractable>.WhenRegistered +=
                RegisterInteractable;
            InteractableRegistry<TInteractor, TInteractable>.WhenUnregistered +=
                UnregisterInteractable;

            foreach (var interactable in registryList)
            {
                RegisterInteractable(interactable);
            }
        }

        /// <summary>
        /// Stops monitoring a specific pair of Interactor and Interactable types.
        /// This method unsubscribes from the static registration events of the
        /// <see cref="InteractableRegistry{TInteractor, TInteractable}"/>.
        /// Note: It does not automatically unregister currently tracked interactables of this type;
        /// that is typically handled in <see cref="OnDisable"/> or by direct calls to <see cref="UnregisterInteractable"/>.
        /// </summary>
        /// <typeparam name="TInteractor">The type of Interactor.</typeparam>
        /// <typeparam name="TInteractable">The type of Interactable.</typeparam>
        public void UnregisterInteractionType<TInteractor, TInteractable>()
            where TInteractor : Interactor<TInteractor, TInteractable>
            where TInteractable : Interactable<TInteractor, TInteractable>, IInteractableView
        {
            InteractableRegistry<TInteractor, TInteractable>.WhenRegistered -= RegisterInteractable;
            InteractableRegistry<TInteractor, TInteractable>.WhenUnregistered -= UnregisterInteractable;
        }

        internal void RegisterInteractable(IInteractableView interactable)
        {
            if (interactable == null || _handlers.TryGetValue(interactable, out _))
            {
                return;
            }

            var handler = new Handler(this, interactable);
            handler.Register();
            _handlers.Add(interactable, handler);
        }

        internal void UnregisterInteractable(IInteractableView interactable)
        {
            if (interactable == null || !_handlers.TryGetValue(interactable, out var handler))
            {
                return;
            }

            handler.Unregister();
            _handlers.Remove(interactable);
        }

        /// <summary>
        /// Allows external static invocation to register a custom interaction type pair
        /// using the active <see cref="InteractionBroadcaster"/> instance.
        /// If no instance exists, a warning is logged.
        /// </summary>
        /// <typeparam name="TInteractor">The type of Interactor.</typeparam>
        /// <typeparam name="TInteractable">The type of Interactable, which must implement <see cref="IInteractableView"/>.</typeparam>
        public static void RegisterCustomInteractionType<TInteractor, TInteractable>()
            where TInteractor : Interactor<TInteractor, TInteractable>
            where TInteractable : Interactable<TInteractor, TInteractable>, IInteractableView
        {
            if (_instance != null)
            {
                _instance.RegisterInteractionType<TInteractor, TInteractable>();
            }
            else
            {
                Debug.LogWarning($"{nameof(InteractionBroadcaster)} instance not found. Cannot {nameof(RegisterCustomInteractionType)} statically.");
            }
        }

        /// <summary>
        /// Allows external static invocation to unregister a custom interaction type pair
        /// using the active <see cref="InteractionBroadcaster"/> instance.
        /// If no instance exists, a warning is logged.
        /// </summary>
        /// <typeparam name="TInteractor">The type of Interactor.</typeparam>
        /// <typeparam name="TInteractable">The type of Interactable.</typeparam>
        public static void UnregisterCustomInteractionType<TInteractor, TInteractable>()
            where TInteractor : Interactor<TInteractor, TInteractable>
            where TInteractable : Interactable<TInteractor, TInteractable>, IInteractableView
        {
            if (_instance != null)
            {
                _instance.UnregisterInteractionType<TInteractor, TInteractable>();
            }
            else
            {
                Debug.LogWarning($"{nameof(InteractionBroadcaster)} instance not found. Cannot {nameof(UnregisterCustomInteractionType)} statically.");
            }
        }

        /// <summary>
        /// Broadcasts an interaction event through the configured <see cref="_eventChannel"/>.
        /// This method is internal and typically called by the <see cref="Handler"/> or UI event callbacks.
        /// </summary>
        /// <param name="interactorView">The view of the interactor involved in the event. Can be null.</param>
        /// <param name="type">The type of interaction event.</param>
        /// <param name="interactableView">The view of the interactable involved in the event.</param>
        internal void BroadcastEvent(IInteractorView interactorView, InteractionType type, IInteractableView interactableView)
        {
            var eventData = new InteractionEvent(type, interactorView, (interactableView as MonoBehaviour)?.gameObject);
            // Broadcast through ScriptableObject channel if configured
            if (_eventChannel != null)
            {
                _eventChannel.Raise(eventData);
            }
            // Broadcast through C# event to all subscribers
            OnEventRaised?.Invoke(eventData);
        }

        private void OnSelectableHovered(PointableCanvasEventArgs args)
        {
            BroadcastUIPointer(args, InteractionType.UIHoverStart);
        }

        private void OnSelectableUnhovered(PointableCanvasEventArgs args)
        {
            BroadcastUIPointer(args, InteractionType.UIHoverEnd);
        }

        private void OnSelected(PointableCanvasEventArgs args)
        {
            BroadcastUIPointer(args, InteractionType.UISelectStart);
        }

        private void OnUnSelected(PointableCanvasEventArgs args)
        {
            BroadcastUIPointer(args, InteractionType.UISelectEnd);
        }

        /// <summary>
        /// Broadcasts a UI pointer interaction event through the configured <see cref="_eventChannel"/>.
        /// </summary>
        /// <param name="args">The arguments from the <see cref="PointableCanvasModule"/> event.</param>
        /// <param name="type">The type of UI pointer interaction event.</param>
        private void BroadcastUIPointer(PointableCanvasEventArgs args, InteractionType type)
        {
            if (args.Hovered == null) return;
            if (args.PointerId == null) return;

            var eventData = new InteractionEvent(type, null, args.Hovered, args.PointerId.Value);
            // Broadcast through ScriptableObject if configured
            if (_eventChannel != null)
            {
                _eventChannel.Raise(new InteractionEvent(type, null, args.Hovered,
                    args.PointerId.Value));
            }
            // Broadcast through C# event to all subscribers
            OnEventRaised?.Invoke(eventData);
        }

        /// <summary>
        /// Manages event subscriptions for a single <see cref="IInteractableView"/> instance.
        /// This class is used to avoid GC allocations that would occur with lambda closures
        /// for event handlers.
        /// </summary>
        private sealed class Handler
        {
            private readonly InteractionBroadcaster _broadcaster;
            private readonly IInteractableView _interactable;

            /// <summary>
            /// Initializes a new instance of the <see cref="Handler"/> class.
            /// </summary>
            /// <param name="broadcaster">The <see cref="InteractionBroadcaster"/> instance to use for broadcasting events.</param>
            /// <param name="interactable">The <see cref="IInteractableView"/> to monitor.</param>
            public Handler(InteractionBroadcaster broadcaster, IInteractableView interactable)
            {
                _broadcaster = broadcaster;
                _interactable = interactable;
            }

            /// <summary>
            /// Subscribes to the interaction events of the <see cref="_interactable"/>.
            /// </summary>
            public void Register()
            {
                _interactable.WhenInteractorViewAdded += OnHoverEnter;
                _interactable.WhenInteractorViewRemoved += OnHoverExit;
                _interactable.WhenSelectingInteractorViewAdded += OnSelectEnter;
                _interactable.WhenSelectingInteractorViewRemoved += OnSelectExit;
            }

            /// <summary>
            /// Unsubscribes from the interaction events of the <see cref="_interactable"/>.
            /// </summary>
            public void Unregister()
            {
                _interactable.WhenInteractorViewAdded -= OnHoverEnter;
                _interactable.WhenInteractorViewRemoved -= OnHoverExit;
                _interactable.WhenSelectingInteractorViewAdded -= OnSelectEnter;
                _interactable.WhenSelectingInteractorViewRemoved -= OnSelectExit;
            }

            // Event handlers without closures, preventing GC allocations
            private void OnHoverEnter(IInteractorView interactor) =>
                _broadcaster.BroadcastEvent(interactor, InteractionType.HoverStart, _interactable);

            private void OnHoverExit(IInteractorView interactor) =>
                _broadcaster.BroadcastEvent(interactor, InteractionType.HoverEnd, _interactable);

            private void OnSelectEnter(IInteractorView interactor) =>
                _broadcaster.BroadcastEvent(interactor, InteractionType.SelectStart, _interactable);

            private void OnSelectExit(IInteractorView interactor) =>
                _broadcaster.BroadcastEvent(interactor, InteractionType.SelectEnd, _interactable);
        }
    }
}
