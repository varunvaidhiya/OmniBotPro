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
    /// Conceptually comparable to a PointerEvent, LocomotionEvents encapsulate information about locomotion
    /// interactions, indicating a player's intent to teleport, rotate, etc.
    /// </summary>
    /// <remarks>
    /// These events are emitted by <see cref="ILocomotionEventBroadcaster"/>s and processed by
    /// <see cref="ILocomotionEventHandler"/>s; for canonical examples, see <see cref="TeleportInteractor"/>
    /// and <see cref="PlayerLocomotor"/>, respectively.
    /// </remarks>
    public struct LocomotionEvent
    {
        /// <summary>
        /// The manner in which the translational part of a <see cref="LocomotionEvent.Pose"/> should be interpreted.
        /// </summary>
        /// <remarks>
        /// For example, if the <see cref="LocomotionEvent.Rotation"/> property of an event is
        /// <see cref="TranslationType.Absolute"/>, the position expressed in the pose should be understood as
        /// world space coordinates to which the player is trying to locomote.
        /// </remarks>
        public enum TranslationType
        {
            None,
            Velocity,
            Absolute,
            AbsoluteEyeLevel,
            Relative
        }

        /// <summary>
        /// The manner in which the rotational part of a <see cref="LocomotionEvent.Pose"/> should be interpreted.
        /// </summary>
        /// <remarks>
        /// For example, if the <see cref="LocomotionEvent.Rotation"/> property of an event is
        /// <see cref="RotationType.Relative"/>, the rotation expressed in the pose should be understood as an
        /// addendum to be applied to the current rotation.
        /// </remarks>
        public enum RotationType
        {
            None,
            Velocity,
            Absolute,
            Relative
        }

        /// <summary>
        /// The numerical identifier of the emitting interactor, comparable to PointerEvent.Identifier; for more information, see
        /// the documentation for <see cref="IInteractorView.Identifier"/>.
        /// </summary>
        public int Identifier { get; }

        /// <summary>
        /// The target pose represented by this locomotion event.
        /// </summary>
        /// <remarks>
        /// Depending on <see cref="Translation"/> and <see cref="Rotation"/>, not all of this pose may be applicable for a given
        /// event; for example, a LocomotionEvent with a <see cref="Translation"/> of <see cref="TranslationType.None"/> only
        /// represents a target rotation.
        /// </remarks>
        public Pose Pose { get; }

        /// <summary>
        /// The manner in which the translation component of the target <see cref="Pose"/> should be interpreted. For details, see
        /// <see cref="TranslationType"/>.
        /// </summary>
        public TranslationType Translation { get; }

        /// <summary>
        /// The manner in which the rotation component of the target <see cref="Pose"/> should be interpreted. For details, see
        /// <see cref="RotationType"/>.
        /// </summary>
        public RotationType Rotation { get; }

        /// <summary>
        /// A unique value associated with this specific event, meaning the semantic occurrence at a specific point in time which
        /// motivated the creation of this data object.
        /// </summary>
        public ulong EventId { get; }

        /// <summary>
        /// Constructor for combined translation-rotation locomotion events, such as teleportation with a turn.
        /// </summary>
        /// <param name="identifier">Numerical identifier of the emitting interactor (see <see cref="IInteractorView.Identifier"/> for details)</param>
        /// <param name="pose">The pose the user is intending to locomote toward</param>
        /// <param name="translationType">The manner in which the translation portion of the locomotion should be interpreted</param>
        /// <param name="rotationType">The manner in which the rotation portion of the locomotion should be interpreted</param>
        public LocomotionEvent(int identifier, Pose pose,
            TranslationType translationType, RotationType rotationType)
        {
            this.Identifier = identifier;
            EventId = ++_nextEventId;
            // At 100 pointer events per frame, this field is expected not to overflow for more than fifty million years, so hitting this
            // assert suggests either a massive overproduction of pointer events or memory corruption.
            Debug.Assert(_nextEventId != UInt64.MaxValue);
            this.Pose = pose;
            this.Translation = translationType;
            this.Rotation = rotationType;
        }

        /// <summary>
        /// Constructor for pure translation locomotion events, such as teleportation. The <see cref="Rotation"/> of the
        /// constructed event will be <see cref="RotationType.None"/>.
        /// </summary>
        /// <param name="identifier">Numerical identifier of the emitting interactor (see <see cref="IInteractorView.Identifier"/> for details)</param>
        /// <param name="position">The position the user is intending to locomote toward</param>
        /// <param name="translationType">The manner in which the requested locomotion should be interpreted</param>
        public LocomotionEvent(int identifier,
            Vector3 position, TranslationType translationType) :
            this(identifier,
                new Pose(position, Quaternion.identity),
                translationType, RotationType.None)
        {
        }

        /// <summary>
        /// Constructor for pure rotation locomotion events, such as snap turning. The <see cref="Translation"/> of the
        /// constructed event will be <see cref="TranslationType.None"/>.
        /// </summary>
        /// <param name="identifier">Numerical identifier of the emitting interactor (see <see cref="IInteractorView.Identifier"/> for details)</param>
        /// <param name="rotation">The rotation the user is intending to locomote toward</param>
        /// <param name="rotationType">The manner in which the requested rotation should be interpreted</param>
        public LocomotionEvent(int identifier,
            Quaternion rotation, RotationType rotationType) :
            this(identifier,
                new Pose(Vector3.zero, rotation),
                TranslationType.None, rotationType)
        {
        }

        private static UInt64 _nextEventId = 0;
    }
}
