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

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Interface for a system that can play haptic patterns on a controller
    /// associated with an interactor.
    /// </summary>
    public interface IHapticsPlayer
    {
        /// <summary>
        /// Plays the specified haptic pattern on the device associated with the interactorId.
        /// </summary>
        /// <param name="interactorId">The identifier of the interactor whose controller should play haptics.</param>
        /// <param name="pattern">The haptic pattern to play.</param>
        void Play(int interactorId, in HapticPattern pattern);

        /// <summary>
        /// Stops any active haptics on the device associated with the interactorId.
        /// </summary>
        /// <param name="interactorId">The identifier of the interactor whose controller should stop haptics.</param>
        void Stop(int interactorId);
    }
}
