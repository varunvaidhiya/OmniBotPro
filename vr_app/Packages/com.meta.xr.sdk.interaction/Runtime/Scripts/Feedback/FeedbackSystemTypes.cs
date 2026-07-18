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

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Defines the types of interaction events that can occur.
    /// </summary>
    [Serializable]
    public enum InteractionType
    {
        /// <summary>
        /// No specific interaction type.
        /// </summary>
        None = 0,
        /// <summary>
        /// An interactor has started hovering over an interactable.
        /// </summary>
        HoverStart = 1,
        /// <summary>
        /// An interactor has stopped hovering over an interactable.
        /// </summary>
        HoverEnd = 2,
        /// <summary>
        /// An interactor has selected an interactable.
        /// </summary>
        SelectStart = 3,
        /// <summary>
        /// An interactor has unselected an interactable.
        /// </summary>
        SelectEnd = 4,

        /// <summary>
        /// A UI pointer has started hovering over a UI element.
        /// </summary>
        UIHoverStart = 10,
        /// <summary>
        /// A UI pointer has stopped hovering over a UI element.
        /// </summary>
        UIHoverEnd = 11,
        /// <summary>
        /// A UI pointer has selected a UI element.
        /// </summary>
        UISelectStart = 12,
        /// <summary>
        /// A UI pointer has unselected a UI element.
        /// </summary>
        UISelectEnd = 13,
    }
}
