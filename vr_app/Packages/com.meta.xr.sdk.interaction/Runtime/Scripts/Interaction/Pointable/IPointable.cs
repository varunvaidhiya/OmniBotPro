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

namespace Oculus.Interaction
{
    /// <summary>
    /// Enumeration of the types of occurrences a <see cref="PointerEvent"/> can represent.
    /// </summary>
    public enum PointerEventType
    {
        Hover,
        Unhover,
        Select,
        Unselect,
        Move,
        Cancel
    }

    /// <summary>
    /// Created and emitted to encapsulate the relevant information of a "pointer interaction," which is an interaction the user
    /// conducted by conceptually "pointing at" something. This event contains information about the interactor (canonically a
    /// <see cref="PointerInteractor{TInteractor, TInteractable}"/>) which originated the interaction, what type of interaction it was,
    /// where it occurred, and relevant ancillary data.
    /// </summary>
    public struct PointerEvent : IEvent
    {
        /// <summary>
        /// The <see cref="IInteractorView.Identifier"/> associated with interactor which originated the interaction represented by this
        /// event. This identifier is uniquely associated with a single interactor instance in the experience.
        /// </summary>
        public int Identifier { get; }

        /// <summary>
        /// A unique value associated with this specific event, meaning the semantic occurrence at a specific point in time which
        /// motivated the creation of this data object.
        /// </summary>
        public UInt64 EventId { get; }

        /// <summary>
        /// A characterization of the type of interaction this event represents. Each PointerEvent can represent only one simple
        /// interaction, and complex interactions (for example, an interactable being deselected by one interactor in order to be selected
        /// by another) must consequently be represented by multiple sequential PointerEvents.
        /// </summary>
        public PointerEventType Type { get; }

        /// <summary>
        /// The world-space position associated with the represented interaction. The precise meaning of this <see cref="Pose"/> varies
        /// based on the interaction <see cref="Type"/> and what kind of interaction it is: a <see cref="PokeInteractor"/>'s
        /// <see cref="PointerEventType.Select"/> might designate the pose of the interactor at the moment of selection, a
        /// <see cref="GrabInteractor"/> <see cref="PointerEventType.Move"/> might designate the expected pose of the interactable based on
        /// the behavior of the relevant <see cref="IMovement"/> in the most recent processing frame, etc. PointerEvent thus does not specify
        /// a unique, precise contract for this value, and the responsibility for interpreting this pose correctly belongs to the handler.
        /// </summary>
        public Pose Pose { get; }

        /// <summary>
        /// A generic metadata property used for a variety of purposes in Interaction SDK logic. Though this property
        /// is public, it has no formal contract, and consequently it is generally not safe to assume that any specific
        /// piece of information is available through this property on any instance at any time.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Constructor for new PointerEvents.
        /// </summary>
        /// <param name="identifier">The <see cref="IInteractorView.Identifier"/> of the originating interactor</param>
        /// <param name="type">The <see cref="PointerEventType"/> which most accurately characterizes the represented interaction</param>
        /// <param name="pose">The world space position and orientation associated with this interaction</param>
        /// <param name="data">Contractually undefined metadata</param>
        public PointerEvent(int identifier, PointerEventType type, Pose pose, object data = null)
        {
            Identifier = identifier;
            EventId = ++_nextEventId;
            // At 100 pointer events per frame, this field is expected not to overflow for more than fifty million years, so hitting this
            // assert suggests either a massive overproduction of pointer events or memory corruption.
            Debug.Assert(_nextEventId != UInt64.MaxValue);
            Type = type;
            Pose = pose;
            Data = data;
        }

        private static UInt64 _nextEventId = 0;
    }

    /// <summary>
    /// This interface specifies that an implementing type is a source for <see cref="PointerEvent"/>s, which are raised in response
    /// to certain actions which reflect user intent by "pointing" at something. Events raised by IPointables are both consumed and
    /// propagated by <see cref="IPointableElement"/>s.
    /// </summary>
    public interface IPointable
    {
        /// <summary>
        /// Event raised when the IPointable undergoes any of the state changes representable by <see cref="PointerEvent"/>s:
        /// hover, unhover, selection, etc. These behaviors map readily to behaviours common among 2D UIs (hover, unhover, etc.).
        /// </summary>
        event Action<PointerEvent> WhenPointerEventRaised;
    }

    /// <summary>
    /// This interface specifies that an implementing type is a recipient of <see cref="PointerEvent"/>s and is thus conceptually
    /// something, "to which one can point." Note that IPointableElement is (as an <see cref="IPointable"/>) both an originator of
    /// and a consumer of <see cref="PointerEvent"/>s; this manifests the common Interaction SDK pattern that
    /// <see cref="PointerEvent"/>s are created by the instrument which _does_ the pointing, are received by the target to which one
    /// points, and are then forwarded by that target to additional consumers.
    /// </summary>
    public interface IPointableElement : IPointable
    {
        /// <summary>
        /// Instructs the IPointableElement to handle a provided <see cref="PointerEvent"/> which represents a user action of
        /// "pointing" something at this IPointableElement. This processing will typically result in the processed
        /// <see cref="PointerEvent"/> being re-emitted after processing by the IPointableElement as part of its role as an
        /// <see cref="IPointable"/>.
        /// </summary>
        /// <param name="evt">The <see cref="PointerEvent"/> to be processed</param>
        void ProcessPointerEvent(PointerEvent evt);
    }
}
