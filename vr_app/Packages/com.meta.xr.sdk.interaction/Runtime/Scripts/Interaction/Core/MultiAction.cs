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
    /// Interface wrapping single-argument events used by <see cref="Interactor{TInteractor, TInteractable}"/> and
    /// <see cref="Interactable{TInteractor, TInteractable}"/>.
    /// </summary>
    /// <remarks>
    /// This is an internal API and should not be used in the creation of new or independent APIs.
    /// </remarks>
    public interface MAction<out T>
    {
        /// <summary>
        /// The single-argument event wrapped by this MAction. For a canonical implementation, see
        /// <see cref="MultiAction{T}"/>, and for an example of usage see <see cref="Interactor{TInteractor, TInteractable}"/>
        /// or <see cref="Interactable{TInteractor, TInteractable}"/>.
        /// </summary>
        event Action<T> Action;
    }

    /// <summary>
    /// Canonical implementation of <see cref="MAction{T}"/>, providing single-argument event functionality used by
    /// <see cref="Interactor{TInteractor, TInteractable}"/> and <see cref="Interactable{TInteractor, TInteractable}"/>.
    /// </summary>
    /// <remarks>
    /// This is an internal API and should not be used in the creation of new or independent APIs.
    /// </remarks>
    public class MultiAction<T> : MAction<T>
    {
        protected HashSet<Action<T>> actions = new HashSet<Action<T>>();

        /// <summary>
        /// Implementation of <see cref="MAction{T}.Action"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action<T> Action
        {
            add
            {
                actions.Add(value);
            }
            remove
            {
                actions.Remove(value);
            }
        }

        /// <summary>
        /// Invokes the event underlying <see cref="Action"/>, signaling all listeners with the provided
        /// <paramref name="t"/> value.
        /// </summary>
        /// <param name="t">The value to be sent to <see cref="Action"/> listeners</param>
        public void Invoke(T t)
        {
            foreach (Action<T> action in actions)
            {
                action(t);
            }
        }
    }
}
