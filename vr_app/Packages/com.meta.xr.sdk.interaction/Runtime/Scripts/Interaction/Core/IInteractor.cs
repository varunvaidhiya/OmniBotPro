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

namespace Oculus.Interaction
{
    /// <summary>
    /// IInteractorStateChangeArgs instances contain information relevant to the changing state of
    /// an <see cref="IInteractorView"/>. This information is used internally by the Interaction SDK for a
    /// variety of tasks (for example, when observing and managing the state of multiple interactors within an
    /// <see cref="InteractorGroup"/>) and can be leveraged directly in custom code. However, it is typically
    /// easier to observe <see cref="IInteractor"/> state changes using an
    /// <see cref="InteractorUnityEventWrapper"/>, which will unpack this information into discrete events which
    /// can be accessed and manipulated in the Unity Editor.
    /// </summary>
    public struct InteractorStateChangeArgs
    {
        /// <summary>
        /// The <see cref="InteractorState"/> of the emitting <see cref="IInteractor"/> prior to this state change.
        /// Note that state change events are emitted after the change has already taken place, and thus querying
        /// the state in a handler of such an event returns the new state, not the previous state.
        /// </summary>
        public InteractorState PreviousState { get; }

        /// <summary>
        /// The <see cref="InteractorState"/> of the emitting <see cref="IInteractor"/> after this state change.
        /// State change events are emitted after the change has already taken place, so querying
        /// the state in a handler of such an event will return this new state.
        /// </summary>
        public InteractorState NewState { get; }

        /// <summary>
        /// Constructor for InteractorStateChangeArgs. In general, only <see cref="IInteractor"/>
        /// instances are expected to invoke this.
        /// </summary>
        /// <param name="previousState">The <see cref="IInteractor"/>'s prior <see cref="InteractorState"/></param>
        /// <param name="newState">The <see cref="IInteractor"/>'s new <see cref="InteractorState"/></param>
        public InteractorStateChangeArgs(
            InteractorState previousState,
            InteractorState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// IInteractorView provides the most high-level, abstract view available of a conceptual "interactor,"
    /// instances of which represent user intent and agency in an experience. This interface is used internally
    /// in Interaction SDK logic (for example, within <see cref="InteractableGroup"/>), but because of its highly
    /// abstract nature and lack of information about the type of interaction is represented, you
    /// will typically work with descendant types rather than with IInteractorView directly.
    /// </summary>
    public interface IInteractorView
    {
        /// <summary>
        /// An identifier uniquely associated with the conceptual "interactor" instance represented by this IInteractorView.
        /// Identifiers are associated with individual instances (not classes) of concrete "interactor" types; thus,
        /// many different IInteractorView instances can return the same identifier, but there is exactly one
        /// concrete "interactor" instance (<see cref="PokeInteractor"/>, <see cref="GrabInteractor"/>, etc.) associated
        /// with that identifier, which all the views reflect.
        /// </summary>
        int Identifier { get; }

        /// <summary>
        /// A generic metadata property used for a variety of purposes in Interaction SDK logic. Though this property
        /// is public, it has no formal contract, and consequently it is generally not safe to assume that any specific
        /// piece of information is available through this property on any instance at any time.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// A property indicating whether there is some interaction that the conceptual "interactor" could undertake.
        /// For typical transitive interactions (i.e., <see cref="PokeInteractor"/>, <see cref="GrabInteractor"/>, and
        /// others where the interaction is applied to some conceptual "interactable"), this indicates whether or not
        /// there is some <see cref="IInteractable"/> with which this interactor can initiate an interaction. For
        /// intransitive interactions such as <see cref="Locomotion.LocomotionTurnerInteractor"/> which do not conceptually
        /// apply the interaction _to_ something, this property merely indicates whether or not it is possible for the
        /// intransitive interaction to take place at this time.
        /// </summary>
        bool HasCandidate { get; }

        /// <summary>
        /// A generic metadata property used for a variety of purposes in Interaction SDK logic. Though this property
        /// is public and canonically may contain instances of types such as <see cref="RayInteractor.RayCandidateProperties"/>,
        /// it has no formal contract and is not safe to use outside of Interaction SDK core logic.
        /// </summary>
        object CandidateProperties { get; }

        /// <summary>
        /// A property indicating whether this conceptual "interactor" is actively hovering or selecting, which for most
        /// interactions means there is an <see cref="IInteractable"/> with which the interactor is currently associated.
        /// In general, this will be true when <see cref="State"/> returns either
        /// <see cref="InteractorState.Hover"/> or <see cref="InteractorState.Select"/>.
        /// </summary>
        bool HasInteractable { get; }

        /// <summary>
        /// A property indicating whether this conceptual "interactor" is actively selecting, which for most interactions
        /// means there is an <see cref="IInteractable"/> with which the interactor is currently interacting. This is only
        /// true if <see cref="State"/> is <see cref="InteractorState.Select"/>.
        /// </summary>
        bool HasSelectedInteractable { get; }

        /// <summary>
        /// The current <see cref="InteractorState"/> of the conceptual "interactor" represented by this IInteractorView.
        /// This property is used in core Interaction SDK logic, and querying it directly is not recommended as the state
        /// can change repeatedly and at unpredictable times within a frame depending on occurrances within the interaction
        /// system. Rather than querying the state at an arbitrary point during the frame, it is recommended to observe the
        /// <see cref="WhenStateChanged"/> event (either directly our through an <see cref="InteractorUnityEventWrapper"/>)
        /// in order to reliably handle all state changes as they come.
        /// </summary>
        InteractorState State { get; }

        /// <summary>
        /// An event invoked every time the conceptual "interactor" represented by this IInteractorView changes
        /// <see cref="State"/>. This is the recommended way to observe and respond to the state of interactors. To make
        /// the information communicated by this event available in the Unity Editor, an <see cref="InteractorUnityEventWrapper"/>
        /// can be leveraged to unpack the details of the state change and expose them in the Editor as Unity events.
        /// </summary>
        event Action<InteractorStateChangeArgs> WhenStateChanged;

        /// <summary>
        /// An event invoked when the conceptual "interactor" represented by this IInteractorView is preprocessed. This is
        /// part of Interaction SDK core logic connected to the internal function of interactors and interactor groups, and
        /// it is not recommended that these events be used for non-core development.
        /// </summary>
        event Action WhenPreprocessed;

        /// <summary>
        /// An event invoked when the conceptual "interactor" represented by this IInteractorView is processed. This is
        /// part of Interaction SDK core logic connected to the internal function of interactors and interactor groups, and
        /// it is not recommended that these events be used for non-core development.
        /// </summary>
        event Action WhenProcessed;

        /// <summary>
        /// An event invoked when the conceptual "interactor" represented by this IInteractorView is postprocessed. This is
        /// part of Interaction SDK core logic connected to the internal function of interactors and interactor groups, and
        /// it is not recommended that these events be used for non-core development.
        /// </summary>
        event Action WhenPostprocessed;
    }

    /// <summary>
    /// An IUpdateDriver is an object which presents its own instance-level concept of an "update." Because certain aspects
    /// of Interaction SDK core logic require order-sensitive processing across multiple instances (for example, correctly
    /// interleaving the processing phases of <see cref="IInteractor"/>s within an <see cref="InteractorGroup"/>), IUpdateDriver
    /// provides a cohesive execution model not readily available in Unity's built-in update mechanism alone. This is part of
    /// Interaction SDK core logic, you should not call it directly.
    /// </summary>
    public interface IUpdateDriver
    {
        /// <summary>
        /// Returns true if the current instance is responsible for invoking its own <see cref="Drive"/> method, false otherwise.
        /// If this IUpdateDriver is not a root driver, then some other instance (typically another IUpdateDriver) is responsible
        /// for invoking execution. In this way, IUpdateDriver instances form a tree, with <see cref="Drive"/> execution beginning
        /// from the root.
        /// </summary>
        bool IsRootDriver { get; set; }

        /// <summary>
        /// Runs the processing to "update" the current instance. This is conceptually similar to Unity's built-in update mechanism,
        /// but bound to a structure which allows execution order to be more explicitly controlled. This is part of Interaction SDK
        /// core logic, and it is not recommended that direct dependencies be taken on this execution order. Instead, changes to
        /// interaction state should be handled by observing events such as <see cref="IInteractorView.WhenStateChanged"/>, which
        /// will allow correct handling without depending on the details of execution order.
        /// </summary>
        void Drive();
    }

    /// <summary>
    /// An IInteractor is an instance which represents user intent and agency in an experience. For example, a
    /// <see cref="PokeInteractor"/> is responsible for indicating within experience logic that the user is able to poke
    /// things, determining when the user is trying to do so, and translating that user intent into interaction. Interactors
    /// are arguably the most fundamental concept of the Interaction SDK.
    ///
    /// Despite its conceptual centrality, the IInteractor interface itself contains no methods or properties which should
    /// be leveraged in normal (non-core) development. All typical developer-facing APIs are defined in other types, either
    /// <see cref="IInteractorView"/> or descendents of IInteractor. IInteractor itself exclusively contains details of core
    /// Interaction SDK logic, and so these methods and properties should only be accessed to add or modify core functionality.
    /// </summary>
    public interface IInteractor : IInteractorView, IUpdateDriver
    {
        /// <summary>
        /// Runs the "preprocess" phase, a part of the internal contract of updating an IInteractor.
        /// This is a part of Interaction SDK core logic and is not expected to be invoked outside of core functionality.
        /// </summary>
        void Preprocess();

        /// <summary>
        /// Runs the "process" phase, a part of the internal contract of updating an IInteractor.
        /// This is a part of Interaction SDK core logic and is not expected to be invoked outside of core functionality.
        /// </summary>
        void Process();

        /// <summary>
        /// Runs the "postprocess" phase, a part of the internal contract of updating an IInteractor.
        /// This is a part of Interaction SDK core logic and is not expected to be invoked outside of core functionality.
        /// </summary>
        void Postprocess();

        /// <summary>
        /// Runs interaction-specific logic to identify whether there is currently an opportunity for interaction and,
        /// if so and if applicable, with which <see cref="IInteractable"/>.
        /// This is a part of Interaction SDK core logic and is not expected to be invoked outside of core functionality.
        /// </summary>
        void ProcessCandidate();

        /// <summary>
        /// Ensures that the neccessary steps have been performed in order to ready this interactor instance for interaction.
        /// Importantly, this is _not_ the correct mechanism to manually control whether an interactor is active in the scene;
        /// interactor enabling/disabling should be handled by enabling/disabling MonoBehaviours and GameObjects as appropriate.
        /// This API is part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Enable();

        /// <summary>
        /// Ensures that the neccessary steps have been performed in order for this interactor instance to cease interaction.
        /// Importantly, this is _not_ the correct mechanism to manually control whether an interactor is active in the scene;
        /// interactor enabling/disabling should be handled by enabling/disabling MonoBehaviours and GameObjects as appropriate.
        /// This API is part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Disable();

        /// <summary>
        /// Follows through on a processing result which determined that this interactor instance <see cref="ShouldHover"/>.
        /// Importantly, this is _not_ a mechanism by which an interactor can be forced to adopt a hover state. No such
        /// mechanism is supported at this level of abstraction; for more information and relevant concerns, see
        /// <see cref="Interactor{TInteractor, TInteractable}.SetComputeCandidateOverride(Func{TInteractable}, bool)"/>.
        /// This API is a part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Hover();

        /// <summary>
        /// Follows through on a processing result which determined that this interactor instance <see cref="ShouldUnhover"/>.
        /// Importantly, this is _not_ a mechanism by which an interactor can be forced to abandon a hover state. No such
        /// mechanism is supported at this level of abstraction; for more information and relevant concerns, see
        /// <see cref="Interactor{TInteractor, TInteractable}.SetComputeCandidateOverride(Func{TInteractable}, bool)"/>.
        /// This API is a part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Unhover();

        /// <summary>
        /// Follows through on a processing result which determined that this interactor instance <see cref="ShouldSelect"/>.
        /// Importantly, this is _not_ a mechanism by which an interactor can be forced to adopt a select state. No such
        /// mechanism is supported at this level of abstraction; for more information and relevant concerns, see
        /// <see cref="Interactor{TInteractor, TInteractable}.SetComputeShouldSelectOverride(Func{bool}, bool)"/>.
        /// This API is a part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Select();

        /// <summary>
        /// Follows through on a processing result which determined that this interactor instance <see cref="ShouldUnselect"/>.
        /// Importantly, this is _not_ a mechanism by which an interactor can be forced to abandon a select state. No such
        /// mechanism is supported at this level of abstraction; for more information and relevant concerns, see
        /// <see cref="Interactor{TInteractor, TInteractable}.SetComputeShouldUnselectOverride(Func{bool}, bool)"/>.
        /// This API is a part of Interaction SDK core logic and should not be invoked outside of core functionality.
        /// </summary>
        void Unselect();

        /// <summary>
        /// Returns true if interactor processing has determined that this interactor should enter a hover state (i.e., it
        /// has identified an acceptable <see cref="IInteractable"/> candidate and has no stipulations which would prevent it
        /// from hovering), false otherwise.
        /// This API is a part of Interaction SDK core logic and is meaningless outside of interactor processing.
        /// </summary>
        bool ShouldHover { get; }

        /// <summary>
        /// Returns true if interactor processing has determined that this interactor should abandon its hover state (i.e., it
        /// has no acceptable <see cref="IInteractable"/> candidate or has stipulations which prevent it from hovering), false
        /// otherwise.
        /// This API is a part of Interaction SDK core logic and is meaningless outside of interactor processing.
        /// </summary>
        bool ShouldUnhover { get; }

        /// <summary>
        /// Returns true if interactor processing has determined that this interactor should enter a select state (i.e., it
        /// has an <see cref="IInteractable"/> candidate and has met the criteria needed to select), false otherwise.
        /// This API is a part of Interaction SDK core logic and is meaningless outside of interactor processing.
        /// </summary>
        bool ShouldSelect { get; }

        /// <summary>
        /// Returns true if interactor processing has determined that this interactor should abandon its select state (i.e., it
        /// is already selecting an <see cref="IInteractable"/>, but it no longer meets the requirements to continue selecting
        /// that interactable), false otherwise.
        /// This API is a part of Interaction SDK core logic and is meaningless outside of interactor processing.
        /// </summary>
        bool ShouldUnselect { get; }
    }
}
