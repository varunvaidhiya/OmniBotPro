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

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Indicates that the implementing type is a delegate or handler for the "use" of a <see cref="HandGrabUseInteractable"/>.
    /// </summary>
    /// <remarks>
    /// Often, the "hand-grab-use" scenario does not require specializied interactors to enable: a virtual table, for example,
    /// can be poked while being grabbed by default, though for ergonomic reasons this must be done carefully if the grabbing
    /// and poking hands are the same. IHandGrabUseDelegate is specifically intended for scenarios where the secondary
    /// interactivity is both contingent upon and component to the initial interactivity. The canonical example of this is the
    /// "spray bottle" demo, where a bottle can be grabbed, then actuated with a finger motion to spray water. Since both
    /// interactions are done with the same hand and both are done using motions that more or less constitute "grabbing,"
    /// representing this as a single grab interactable is impractical, motivating the independence of the "hand-grab-use"
    /// family of interactions.
    /// </remarks>
    public interface IHandGrabUseDelegate
    {
        /// <summary>
        /// Invoked when a <see cref="HandGrabUseInteractable"/>'s secondary interactivity becomes actionable (i.e., when the
        /// interactable is selected by a <see cref="HandGrabUseInteractor"/>). This is an internally-invoked method and should
        /// not be called directly.
        /// </summary>
        void BeginUse();

        /// <summary>
        /// Invoked when a <see cref="HandGrabUseInteractable"/>'s secondary interactivity ceases to be actionable (i.e., when the
        /// interactable is unselected). This is an internally-invoked method and should not be called directly.
        /// </summary>
        void EndUse();

        /// <summary>
        /// Computes the "strength" of the secondary interactivity, where "strength" is a 0-to-1 value indicating whether the
        /// secondary interactivity is being lightly used or fully used, respectively.
        /// </summary>
        /// <remarks>
        /// In the "spray bottle" example, a "strength" of 0 would indicate that the spray lever is not depressed at all, whereas a
        /// "strength" of 1 indicates full depression of the lever.
        /// </remarks>
        /// <param name="strength">A "raw strength" which the IHandGrabUseDelegate can use to inform its calculation of the true "strength"</param>
        /// <returns>The true "strength" of the secondary interactivity</returns>
        float ComputeUseStrength(float strength);
    }
}
