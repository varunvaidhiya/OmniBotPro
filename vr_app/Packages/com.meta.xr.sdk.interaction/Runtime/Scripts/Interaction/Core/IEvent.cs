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

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines the base interface for all <see cref="Oculus.Interaction.PointerEvent"/>'s in the Interaction SDK. This interface provides
    /// a generic way to pass any interaction-specific data through the interaction event pipeline.
    /// See <see cref="IPointable"/> and <see cref="IPointableElement"/> for examples of how events are propogated through the interaction system.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Gets the data associated with this event. The data type varies depending on the specific
        /// event implementation and can be cast to the appropriate type by the event handler.
        /// </summary>
        object Data { get; }
    }
}
