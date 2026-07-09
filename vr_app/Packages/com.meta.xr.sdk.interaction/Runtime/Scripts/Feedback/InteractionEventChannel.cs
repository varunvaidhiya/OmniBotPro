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
using UnityEngine.Serialization;

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// A ScriptableObject-based event channel designed for broadcasting <see cref="InteractionEvent"/>s
    /// throughout the application. Various systems can subscribe to the <see cref="OnEventRaised"/>
    /// event to react to global interaction state changes (e.g., Hover, Select, UI pointer events)
    /// in a decoupled manner.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionEventChannel", menuName = "Meta/Interaction SDK/Feedback/Interaction Event Channel")]
    public class InteractionEventChannel : ScriptableObject
    {
        /// <summary>
        /// Event invoked when a new <see cref="InteractionEvent"/> is raised via the <see cref="Raise"/> method.
        /// Systems interested in global interaction events should subscribe to this.
        /// <remarks>
        /// This event is marked as <see cref="NonSerializedAttribute"/> and will be reset (cleared of subscribers)
        /// after Unity domain reloads (e.g., script compilation, entering/exiting play mode).
        /// Subscribers should ensure they re-subscribe, typically in their <c>OnEnable</c> method.
        /// </remarks>
        /// </summary>
        [field: NonSerialized]
        public event Action<InteractionEvent> OnEventRaised = delegate { }; // Default to empty delegate to avoid null checks

        /// <summary>
        /// Raises an <see cref="InteractionEvent"/>, invoking the <see cref="OnEventRaised"/>
        /// delegate and notifying all current subscribers.
        /// </summary>
        /// <param name="interactionEvent">The <see cref="InteractionEvent"/> data to broadcast.
        /// This is passed by <see langword="in"/> ref to potentially avoid copying a large struct, though <see cref="InteractionEvent"/> is small.</param>
        public void Raise(in InteractionEvent interactionEvent)
        {
            // The null-conditional operator ?. is not needed here because OnEventRaised is initialized to an empty delegate.
            OnEventRaised(interactionEvent);
        }
    }

    /// <summary>
    /// Represents the data associated with a specific interaction state change,
    /// such as a hover, selection, or UI pointer event.
    /// </summary>
    [Serializable]
    public struct InteractionEvent
    {
        /// <summary>
        /// The specific type of interaction that occurred (e.g., HoverStart, SelectEnd, UISelectStart).
        /// </summary>
        [Tooltip("The type of interaction that occurred (e.g., HoverStart, SelectEnd).")]
        public InteractionType _type;

        /// <summary>
        /// The <see cref="Oculus.Interaction.IInteractorView"/> component that initiated or is involved in the interaction.
        /// This can be null if the event is not directly tied to a specific <see cref="IInteractorView"/> instance
        /// (e.g., some UI events might only have a <see cref="_pointerId"/>).
        /// </summary>
        [Tooltip("The IInteractorView component that performed the interaction. Can be null.")]
        public IInteractorView InteractorView; // This is an interface, cannot be serialized by Unity directly if exposed in Inspector. Fine for event data.

        /// <summary>
        /// The <see cref="GameObject"/> that is the primary target or source of the interaction
        /// (e.g., the interactable object that was hovered or selected).
        /// This can be null if the interaction source is not a <see cref="GameObject"/> or is not applicable.
        /// </summary>
        [Tooltip("The GameObject source of the interaction (e.g., the Interactable). Can be null.")]
        public GameObject _source;

        /// <summary>
        /// An identifier for UI pointer interactions, such as <see cref="UnityEngine.EventSystems.PointerEventData.pointerId"/>.
        /// Defaults to -1 if not applicable or unknown. For non-UI interactions tied to an <see cref="InteractorView"/>,
        /// the <see cref="IInteractorView.Identifier"/> should be preferred if a unique ID is needed.
        /// </summary>
        [Tooltip("Identifier for UI pointer interactions (e.g., PointerEventData.pointerId). Defaults to -1 if not applicable.")]
        public int _pointerId;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionEvent"/> struct.
        /// </summary>
        /// <param name="type">The <see cref="InteractionType"/> of the event.</param>
        /// <param name="interactorView">The <see cref="IInteractorView"/> involved in the interaction. Can be null.</param>
        /// <param name="source">The source <see cref="GameObject"/> of the interaction. Can be null.</param>
        /// <param name="pointerId">The pointer ID, typically for UI events. Defaults to -1.</param>
        public InteractionEvent(InteractionType type, IInteractorView interactorView, GameObject source, int pointerId = -1)
        {
            _type = type;
            InteractorView = interactorView;
            _source = source;
            _pointerId = pointerId;
        }
    }
}
