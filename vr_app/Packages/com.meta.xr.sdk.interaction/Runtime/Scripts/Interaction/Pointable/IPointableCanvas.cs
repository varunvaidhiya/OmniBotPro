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

using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This interface characterizes something which contains a Unity <see cref="Canvas"/> which can be the target of Interaction
    /// SDK "pointer interactions." Commonly, this is used to implement screen-like interactions (for menus, etc.) in otherwise
    /// 3D experiences and applications: the Unity Canvas can be used to create a 2D interface just as it would be for traditional
    /// screen-based experiences, and the Interaction SDK's <see cref="PointerInteractor{TInteractor, TInteractable}"/>s allow XR
    /// users to interact with this UI in ways that are usable and familiar.
    /// </summary>
    public interface IPointableCanvas : IPointableElement
    {
        /// <summary>
        /// The Unity Canvas to which users can "point" in order interact with the UI implemented within.
        /// </summary>
        Canvas Canvas { get; }
    }
}
