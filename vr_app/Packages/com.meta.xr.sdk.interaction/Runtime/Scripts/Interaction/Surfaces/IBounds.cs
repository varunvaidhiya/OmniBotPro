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
using UnityEngine;

namespace Oculus.Interaction.Surfaces
{
    /// <summary>
    /// Indicates that a type exposes an axis-aligned bounding box called <see cref="Bounds"/> of the
    /// built-in Unity type of the same name. Because of its limited applicability, you should avoid
    /// making new types which implement or consume this interface.
    /// </summary>
    /// <remarks>
    /// Contractually, this bounding box is specifically axis-aligned to world space, limiting its utility
    /// almost exclusively to culling optimizations in spatial arithmetic (for example, when raycasting
    /// against a <see cref="CylinderSurface"/>).
    /// </remarks>
    public interface IBounds
    {
        /// <summary>
        /// The world space axis-aligned bounding box (AABB).
        /// </summary>
        /// <remarks>
        /// As a fundamentally world space-aligned data type, these bounds are incapable of expressing
        /// precise spatial data and should only be leveraged for specialized optimizations. For an example
        /// usage, see <see cref="Locomotion.TeleportInteractable"/>.
        /// </remarks>
        Bounds Bounds { get; }
    }
}
