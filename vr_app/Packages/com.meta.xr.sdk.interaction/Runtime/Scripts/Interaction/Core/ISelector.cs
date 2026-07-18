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
    /// Defines an input abstraction that can broadcast select events and release events.
    /// </summary>
    /// <remarks>
    /// Implementing types can be used to create and provide new mechanisms of selection, either by implementing
    /// this type directly in a custom type or by implementing <see cref="IActiveState"/> and routing logic through
    /// an <see cref="ActiveStateSelector"/>, for example.
    /// </remarks>
    public interface ISelector
    {
        /// <summary>
        /// Invoked to represent selection.
        /// </summary>
        /// <remarks>
        /// This event should be invoked when the input state is observed to represent the user's intent to select, the
        /// conditions for which vary by selection mechanism; for example, <see cref="IndexPinchSelector"/> invokes
        /// this event when the user's index finger and thumb are observed to go from "not pinching" to "pinching."
        /// Contractually, there must be a 1:1 relationship between WhenSelected and <see cref="WhenUnselected"/>
        /// invocations, with one of the latter always following one of the former.
        /// </remarks>
        event Action WhenSelected;

        /// <summary>
        /// Invoked to represent unselection.
        /// </summary>
        /// <remarks>
        /// This event should be invoked when the input state is observed to represent the user's intent to stop
        /// selecting, the conditions for which vary by selection mechanism; for example, <see cref="IndexPinchSelector"/>
        /// invokes this event when the user's index finger and thumb are observed to go from "pinching" to "not
        /// pinching." Contractually, there must be a 1:1 relationship between <see cref="WhenSelected"/> and
        /// WhenUnselected invocations, with one of the latter always following one of the former.
        /// </remarks>
        event Action WhenUnselected;
    }
}
