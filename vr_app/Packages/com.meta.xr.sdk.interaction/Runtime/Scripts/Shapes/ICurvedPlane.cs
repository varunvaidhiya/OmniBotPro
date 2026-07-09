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
    /// Represents a curved rectangular section of a cylinder wall, typically used for UI surfaces in the Interaction SDK. This interface defines the geometric properties needed to describe a section of a curved surface
    /// that wraps around a cylinder, making it ideal for curved menus and interactive panels that follow ergonomic viewing angles.
    /// See <see cref="Oculus.Interaction.UnityCanvas.CanvasCylinder"/> for an example implementation.
    /// </summary>
    public interface ICurvedPlane
    {
        /// <summary>
        /// Gets the cylinder that defines the curved surface's base geometry. This cylinder
        /// provides the underlying shape and orientation for the <see cref="ICurvedPlane"/>.
        /// </summary>
        Cylinder Cylinder { get; }

        /// <summary>
        /// Gets the horizontal arc extent of the <see cref="ICurvedPlane"/> in degrees, determining how far
        /// the surface wraps around the cylinder's circumference.
        /// </summary>
        float ArcDegrees { get; }

        /// <summary>
        /// Gets the rotational offset in degrees from the cylinder's forward Z axis to the
        /// center of the <see cref="ICurvedPlane"/>, determining the plane's horizontal positioning.
        /// </summary>
        float Rotation { get; }

        /// <summary>
        /// Gets the vertical position of the plane's bottom edge relative to the cylinder's Y position,
        /// in the cylinder's local space coordinates.
        /// </summary>
        float Bottom { get; }

        /// <summary>
        /// Gets the vertical position of the plane's top edge relative to the cylinder's Y position,
        /// in the cylinder's local space coordinates.
        /// </summary>
        float Top { get; }
    }
}
