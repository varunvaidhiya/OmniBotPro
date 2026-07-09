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

namespace Oculus.Interaction
{
    /// <summary>
    /// IInteractableStateChangeArgs instances contain information relevant to the changing state of
    /// an <see cref="IInteractableView"/>. This information is used internally by the Interaction SDK for a
    /// variety of tasks and can be leveraged directly in custom code. However, it is typically
    /// easier to observe <see cref="IInteractable"/> state changes using an
    /// <see cref="InteractableUnityEventWrapper"/>, which will unpack this information into discrete events which
    /// can be accessed and manipulated in the Unity Editor.
    /// </summary>
    public struct InteractableStateChangeArgs
    {
        /// <summary>
        /// The <see cref="InteractableState"/> of the emitting <see cref="IInteractable"/> prior to this state change.
        /// Note that state change events are emitted after the change has already taken place, and thus querying
        /// the state in a handler of such an event returns the new state, not the previous state.
        /// </summary>
        public InteractableState PreviousState { get; }

        /// <summary>
        /// The <see cref="InteractableState"/> of the emitting <see cref="IInteractable"/> after this state change.
        /// State change events are emitted after the change has already taken place, so querying
        /// the state in a handler of such an event will return this new state.
        /// </summary>
        public InteractableState NewState { get; }

        /// <summary>
        /// Constructor for InteractableStateChangeArgs. In general, only <see cref="IInteractable"/>
        /// instances are expected to invoke this.
        /// </summary>
        /// <param name="previousState">The <see cref="IInteractable"/>'s prior <see cref="InteractableState"/></param>
        /// <param name="newState">The <see cref="IInteractable"/>'s new <see cref="InteractableState"/></param>
        public InteractableStateChangeArgs(
            InteractableState previousState,
            InteractableState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// IInteractableView provides the most high-level, abstract view available of a conceptual "interactable,"
    /// instances of which represent the targets of transitive user intent and agency in an experience; in other words,
    /// when the user is trying to _do_ something, interactables are what they're trying to do it _to_. This interface
    /// is used internally in Interaction SDK logic, but because of its highly abstract nature and lack of information
    /// about the type of interaction is represented, you will typically work with descendant types
    /// rather than with IInteractableView directly.
    /// </summary>
    public interface IInteractableView
    {
        /// <summary>
        /// A generic metadata property used for a variety of purposes in Interaction SDK logic. Though this property
        /// is public, it has no formal contract, and consequently it is generally not safe to assume that any specific
        /// piece of information is available through this property on any instance at any time.
        /// </summary>
        object Data { get; }

        /// <summary>
        /// The current <see cref="InteractableState"/> of the conceptual "interactable" represented by this IInteractableView.
        /// This property is used in core Interaction SDK logic, and querying it directly is not recommended as the state
        /// can change repeatedly and at unpredictable times within a frame depending on occurrances within the interaction
        /// system. Rather than querying the state at an arbitrary point during the frame, it is recommended to observe the
        /// <see cref="WhenStateChanged"/> event (either directly our through an <see cref="InteractableUnityEventWrapper"/>)
        /// in order to reliably handle all state changes as they come.
        /// </summary>
        InteractableState State { get; }

        /// <summary>
        /// An event invoked every time the conceptual "interactable" represented by this IInteractableView changes
        /// <see cref="State"/>. This is the recommended way to observe and respond to the state of interactables. To make
        /// the information communicated by this event available in the Unity Editor, an <see cref="InteractableUnityEventWrapper"/>
        /// can be leveraged to unpack the details of the state change and expose them in the Editor as Unity events.
        /// </summary>
        event Action<InteractableStateChangeArgs> WhenStateChanged;

        /// <summary>
        /// The largest number of conceptual "interactor" instances (technically <see cref="IInteractorView"/>s) which can
        /// simultaneously interact with this instance. By convention, -1 indicates an unbounded maximum.
        /// </summary>
        int MaxInteractors { get; }

        /// <summary>
        /// The largest number of conceptual "interactor" instances (technically <see cref="IInteractorView"/>s) which can
        /// simultaneously select this instance. By convention, -1 indicates an unbounded maximum.
        /// </summary>
        int MaxSelectingInteractors { get; }

        /// <summary>
        /// Enumerates all of the <see cref="IInteractorView"/>s currently associated with this instance. This includes all
        /// varieties of interaction association (both <see cref="InteractableState.Hover"/> and
        /// <see cref="InteractableState.Select"/>), but does not include candidacy (see <see cref="IInteractorView.HasCandidate"/>).
        /// </summary>
        IEnumerable<IInteractorView> InteractorViews { get; }

        /// <summary>
        /// Enumerates all of the <see cref="IInteractorView"/>s currently selecting this instance, excluding all states of
        /// association other than <see cref="InteractableState.Select"/>.
        /// </summary>
        IEnumerable<IInteractorView> SelectingInteractorViews { get; }

        /// <summary>
        /// Invoked when a new <see cref="IInteractorView"/> begins interacting with this interactable and is consequently added
        /// to the <see cref="InteractorViews"/> enumeration. Note that this event is invoked after the new element has already
        /// been added to the enumeration.
        /// </summary>
        event Action<IInteractorView> WhenInteractorViewAdded;

        /// <summary>
        /// Invoked when an <see cref="IInteractorView"/> stops interacting with this interactable and is consequently removed
        /// from the <see cref="InteractorViews"/> enumeration. Note that this event is invoked after the enumeration has already
        /// been modified.
        /// </summary>
        event Action<IInteractorView> WhenInteractorViewRemoved;

        /// <summary>
        /// Invoked when a new <see cref="IInteractorView"/> begins selecting this interactable and is consequently added to the
        /// <see cref="SelectingInteractorViews"/> enumeration. Note that this event is invoked after the new element has already
        /// been added to the enumeration.
        /// </summary>
        event Action<IInteractorView> WhenSelectingInteractorViewAdded;

        /// <summary>
        /// Invoked when an <see cref="IInteractorView"/> stops interacting with this interactable and is consequently removed
        /// from the <see cref="InteractorViews"/> enumeration. Note that this event is invoked after the enumeration has already
        /// been modified.
        /// </summary>
        event Action<IInteractorView> WhenSelectingInteractorViewRemoved;
    }

    /// <summary>
    /// An IInteractable represents a target for user intent and agency in a transitive interaction. For example, a
    /// <see cref="PokeInteractable"/> is the aspect of something in the experience which allows it to be interacted with by
    /// poking it. Individual conceptual "objects" can have many different IInteractable aspects: a virtual smartphone, for
    /// example, might have a <see cref="PokeInteractable"/> allowing its touchscreen to be poked as well as a
    /// <see cref="GrabInteractable"/> allowing it to be picked up and manipulated. IInteractable is one of the most fundamental
    /// concepts of the Interaction SDK.
    ///
    /// Despite its conceptual centrality, you should not directly invoke the <see cref="Enable"/>/<see cref="Disable"/> methods
    /// defined on the IInteractable interface. These APIs are intended for internal use within Interaction SDK core logic.
    /// You should not modify <see cref="MaxInteractors"/> or <see cref="MaxSettingInteractors"/> while interactions are ongoing.
    /// You should not use <see cref="RemoveInteractorByIdentifier"/> as that can cause unintended side effects; instead, remove
    /// them by interacting with the GameObject or Monobehaviour.
    /// </summary>
    public interface IInteractable : IInteractableView
    {
        /// <summary>
        /// Ensures that the neccessary steps have been performed in order to ready this interactable instance for interaction.
        /// Importantly, this is _not_ the correct mechanism to manually control whether an interactable is active in the scene;
        /// you should enable/disable interactables by enabling/disabling MonoBehaviours and GameObjects as appropriate.
        /// This API is part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Enable();

        /// <summary>
        /// Ensures that the neccessary steps have been performed in order to for this interactable to cease interaction.
        /// Importantly, this is _not_ the correct mechanism to manually control whether an interactable is active in the scene;
        /// interactable enabling/disabling should be handled by enabling/disabling MonoBehaviours and GameObjects as appropriate.
        /// This API is part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Disable();

        /// <summary>
        /// Gets or sets the maximum number of interactors which may simultaneously interact with this interactable. This API is
        /// intended both for core logic and for general use; however, modifying this value while interactions are ongoing is not
        /// supported and may result in unexpected behavior.
        ///
        /// Note: because <see cref="InteractableState.Hover"/> as well as <see cref="InteractableState.Select"/> are considered
        /// to be states of interaction, changing this value can have subtle consequences. As an example,
        /// <see cref="GrabInteractable"/>s can be configured so that they can only be grabbed by one hand at a time, but that is
        /// _not_ done by setting this property to 1. (For information about constraining the number of simultaneous grabs, see
        /// <see cref="Grabbable.MaxGrabPoints"/>.) Setting this property to 1 will prevent two simultaneous grabs, but it will also
        /// prevent "hand-off" (where something grabbed in one hand can be grabbed with the other hand, whereupon it ceases to be
        /// grabbed by the first hand so that it can be grabbed by the second) because grabbing is a selection action which cannot
        /// be invoked except from an interactor-interactable relationship which is already in a "hover"
        /// (<see cref="InteractorState.Hover"/> and <see cref="InteractableState.Hover"/>) state. Because a MaxInteractors
        /// value of 1 prevents any more than one interactor from entering any interaction state with this interactable, an
        /// interactable which has this value and is already being selected cannot even be hovered, and thus cannot be selected, by
        /// any other interactors. Nuances such as this can be tricky to debug, so caution should be exercised when modifying
        /// MaxInteractors and similarly fundamental properties.
        /// </summary>
        new int MaxInteractors { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of interactors which may simultaneously select this interactable. This API is
        /// intended both for core logic and for general use; however, modifying this value while interactions are ongoing is not
        /// supported and may result in unexpected behavior.
        /// </summary>
        new int MaxSelectingInteractors { get; set; }

        /// <summary>
        /// Causes an interactor which is currently interacting with this IInteractable to cease all interaction. This is used in
        /// core Interaction SDK logic, principally for the purposes of interaction cancellation. This can be used by general logic
        /// to force the termination of an ongoing interaction. However, doing this can have hard-to-predict consequences (terminating
        /// a "hover" interaction between a <see cref="GrabInteractor"/> and a <see cref="GrabInteractable"/> may result in the
        /// immediate, same-frame resumption of the same relationship if the interactor is still positioned correctly to hover the
        /// interactable, for example) and so caution should be exercised when invoking this method directly. Where possible, it is
        /// recommended to directly manage the relationships of interactors and interactables by enabling/disabling interactables at
        /// the MonoBehaviour or GameObject level.
        /// </summary>
        /// <param name="id">The <see cref="IInteractorView.Identifier"/> of the interactor to be removed</param>
        void RemoveInteractorByIdentifier(int id);
    }
}
