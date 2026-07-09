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

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Abstract base class for ScriptableObjects that define a feedback action
    /// to be executed in response to an interaction event.
    /// </summary>
    public abstract class FeedbackActionSO : ScriptableObject
    {
        /// <summary>
        /// Executes the defined feedback action.
        /// </summary>
        /// <param name="identifier"><see cref="UniqueIdentifier"/> of the <see cref="IInteractor"/>></param>
        /// <param name="source">The GameObject that was interacted with (the source of the event).</param>
        /// <param name="manager">The FeedbackManager instance, providing access to playback services like haptics.</param>
        public abstract void Execute(int identifier, GameObject source, FeedbackManager manager);
    }
}
