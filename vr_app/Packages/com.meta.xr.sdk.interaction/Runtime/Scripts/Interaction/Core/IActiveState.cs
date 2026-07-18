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

namespace Oculus.Interaction
{
    /// <summary>
    /// The <see cref="IActiveState"/> interface defines the contract for components that can represent an active state
    /// within the Interaction SDK. Implementing classes are responsible for determining whether they are currently
    /// in an "active" state, which can be used to trigger various interactions, events, or behaviors.
    /// </summary>
    /// <remarks> The <see cref="IActiveState"/> interface is a fundamental part of the Interaction SDK, providing a standardized
    /// way to represent and query active conditions. It is commonly used in conjunction with other components, such as
    /// <see cref="ActiveStateGroup"/>, <see cref="ActiveStateNot"/>, and <see cref="ActiveStateUnityEventWrapper"/>, to build complex interaction logic based on multiple conditions. </remarks>
    public interface IActiveState
    {
        /// <summary>
        /// Evaluates the current state of the component and returns whether it is active.
        /// </summary>
        /// <remarks>
        /// The logic for determining the active state is defined by the implementing class. This method is typically
        /// used in interaction scripts to check whether a certain condition is met before triggering an event or action.
        /// For example implementations, please refer to <see cref="Input.HandRef.Active"/> and <see cref="ActiveStateGroup.Active"/>.
        /// </remarks>
        /// <returns>Returns true if the component is currently active; otherwise, returns false.</returns>
        bool Active { get; }
    }
}
