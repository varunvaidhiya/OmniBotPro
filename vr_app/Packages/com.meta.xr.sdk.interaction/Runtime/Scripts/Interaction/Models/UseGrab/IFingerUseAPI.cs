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

using Oculus.Interaction.Input;

namespace Oculus.Interaction
{
    /// <summary>
    /// The IFingerUseAPI interface provides the user with methods and functions revolving around the <see cref="HandFinger"/> and their usage.
    /// An implementation of this API will map a <see cref="HandFinger"/> to a user's input. (such as a controller 1D axis, or a pinch gesture)
    /// and then return a normalized value of that input to signify the strength of the action.
    /// </summary>
    public interface IFingerUseAPI
    {
        /// <summary>
        /// Calculates and returns the precision at which a <see cref="HandFinger"/> is being used by the user.
        /// An example being how much the the user is pressing the <see cref="HandFinger.Index"/> Input on their controller.
        /// </summary>
        /// <param name="finger">The strength at which the <see cref="HandFigner"/> inputs are being used</param>
        /// <returns>The precision in which the given <see cref="HandFinger"/> is being used</returns>
        float GetFingerUseStrength(HandFinger finger);
    }
}
