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
    /// IRelativeToRef provides a way to access a Unity Transform that may not be retreivable via the typical
    /// transform property.  If the Transform most relevant to the object is not the one it has direct access to as a Monobehaviour,
    /// but rather one it modifies as an interactable, <see cref="RelativeTo"/> accesses the relevant Transform.
    /// </summary>
    /// <remarks>
    /// The most common reason for needing IRelativeToRef is when an interactable (for example,
    /// <see cref="DistanceGrabInteractable"/>) resides on a GameObject which is not the root of the hierarchy to which the interactable
    /// "relates". A relatively common pattern in Interaction SDK organizes different types of <see cref="IInteractable"/>s in
    /// different child GameObjects beneath the root GameObject which is conceptually "the interactable"; thus, a cube which can be both
    /// distance grabbed and selected with a poke might have a root GameObject, a child GameObject for the
    /// <see cref="DistanceGrabInteractable"/> and related scripts, another child GameObject for the <see cref="PokeInteractable"/> and
    /// related scripts, as well as other GameObjects for visuals, sounds, etc. In this case, the Transform most relevant to the
    /// <see cref="DistanceGrabInteractable"/> is not the one to which it has direct access as a MonoBehaviour, but rather the one it
    /// modifies as an interactable. IRelativeToRef characterizes this and provides a way to access this other Transform that the
    /// instance is "relative to".
    /// </remarks>
    public interface IRelativeToRef
    {
        /// <summary>
        /// Retrieves the Transform to which the current instance is related.
        /// <summary>
        /// <remarks>For example, if the implementing type is an
        /// <see cref="IInteractable"/>, RelativeTo should retrieve the Transform which represents the root of the conceptually
        /// "interactable" hierarchy, rather than the Transform of the GameObject to which the instance happens to be attached.
        /// </remarks>
        Transform RelativeTo { get; }
    }
}
