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
    /// This interface characterizes a type for which the most relevant Unity Rigidbody can be on an arbitrary GameObject, not
    /// necessarily on the same GameObject as the current instance. For example, a <see cref="HandGrab.HandGrabInteractor"/> component
    /// may be placed on a separate GameObject from the Rigidbody it uses for its internal logic. This interface provides a unified
    /// way of accessing the relevant Rigidbody.
    /// </summary>
    public interface IRigidbodyRef
    {
        /// <summary>
        /// The Unity Rigidbody most relevant to the current instance. The role this Rigidbody plays varies according to the
        /// implementing type; for example, an <see cref="HandGrab.HandGrabInteractor"/> may use a Rigidbody for collision
        /// detection when grabbing.
        /// </summary>
        Rigidbody Rigidbody { get; }
    }
}
