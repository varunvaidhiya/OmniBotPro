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
    /// A helper class which invokes a callback on the main thread when this instance is garbage collected. This can be used
    /// to observe the destruction of resources without requiring them to be explicitly disposed.
    /// </summary>
    /// <remarks>
    /// Prior to Unity 6, this class relies on <see cref="Context.ExecuteOnMainThread(Action)"/> to invoke its callback
    /// on the main thread. In Unity 6+, this dependency is not necessary as
    /// [`Awaitable.MainThreadAsync`](https://docs.unity3d.com/6000.0/Documentation/Manual/async-await-support.html)
    /// can be used instead.
    /// </remarks>
    public class FinalAction
    {
        private readonly Action _action;
        private bool _cancelled = false;

        /// <summary>
        /// Constructor; takes the callback (typically as an inline function) which is to be invoked on the main thread
        /// (for example, using <see cref="Context.ExecuteOnMainThread(Action)"/>) when this FinalAction is destructed.
        /// </summary>
        /// <param name="action">The callback to be invoked on the main thread in response to destruction</param>
        public FinalAction(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Cancels the FinalAction. If this is invoked, then the destruction of this instance will no longer invoke
        /// the FinalAction's callback.
        /// </summary>
        /// <remarks>
        /// This is useful in situations where it is no longer necessary to know when the FinalAction is destructed.
        /// For example, <see cref="ValueToClassDecorator{InstanceT, DecorationT}"/> invokes this method when a
        /// decoration is manually removed, whereupon the associated FinalAction (which was created to remove the
        /// decoration association if the decoration itself is ever destructed) should no longer be called because
        /// the association has already been removed.
        /// </remarks>
        public void Cancel()
        {
            _cancelled = true;
        }

        ~FinalAction()
        {
            if (!_cancelled)
            {
                Context.ExecuteOnMainThread(_action);
            }
        }
    }
}
