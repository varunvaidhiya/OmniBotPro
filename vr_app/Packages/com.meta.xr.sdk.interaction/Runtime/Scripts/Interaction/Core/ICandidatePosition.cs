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
    /// This interface characterizes a type which encapsulates the world space position of an interaction candidate. This is
    /// used to allow generalized spatial comparison operations (such as <see cref="ICandidateComparer"/>) to operate on different
    /// types of candidates such as <see cref="RayInteractor.RayCandidateProperties"/>.
    /// </summary>
    public interface ICandidatePosition
    {
        /// <summary>
        /// The position relative to which this candidate should be evaluated. Note that this is not necessarily the position of the
        /// candidate's transform, but rather the position most relevant to the interaction for which this is a candidate. For example,
        /// <see cref="RayInteractor.RayCandidateProperties"/> set this value to the position of the raycast hit for this interaction.
        /// </summary>
        Vector3 CandidatePosition { get; }
    }
}
