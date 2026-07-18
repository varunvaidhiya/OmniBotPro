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

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Specifically relating to grab interactions, this characterizes a type which can apply 3D transforms
    /// (move, rotate, scale, etc.) in response to grab input expressed through an <see cref="IGrabbable"/>.
    /// In a very direct sense, IGrabbable instances are considered to "own" their associated ITransformer
    /// instances, and only the owning IGrabbables should ever invoke any methods on their ITransformers.
    /// </summary>
    /// <remarks>
    /// During a grab interaction, ITransformers typically "take over" the <see cref="IGrabbable.Transform"/>
    /// in order to move/rotate/scale it according to the evolving state of the
    /// <see cref="IGrabbable.GrabPoints"/>. In this sense, ITransforms are essentially procedural animations.
    /// </remarks>
    public interface ITransformer
    {
        /// <summary>
        /// Invoked by the owning <see cref="IGrabbable"/> as part of its own initialization,
        /// passing itself as <paramref name="grabbable"/> so that the ITransformer can do any required
        /// initial setup.
        /// </summary>
        /// <param name="grabbable">The owning IGrabbable for this instance.</param>
        void Initialize(IGrabbable grabbable);

        /// <summary>
        /// Invoked by the owning <see cref="IGrabbable"/> to instruct the ITransformer to start
        /// its procedural animation.
        /// </summary>
        /// <remarks>
        /// Typically, this is invoked in response to the start of a selecting grab interaction.
        /// </remarks>
        void BeginTransform();

        /// <summary>
        /// Invoked by the owning <see cref="IGrabbable"/> to instruct the ITransformer to advance
        /// to the next frame of its procedural animation.
        /// </summary>
        /// <remarks>
        /// Typically, this is invoked in response to the continuation of a selecting grab interaction.
        /// </remarks>
        void UpdateTransform();

        /// <summary>
        /// Invoked by the owning <see cref="IGrabbable"/> to instruct the ITransformer to end
        /// its procedural animation.
        /// </summary>
        /// <remarks>
        /// Typically, this is invoked in response to the end of a selecting grab interaction.
        /// </remarks>
        void EndTransform();
    }
}
