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
    /// This Interface is used to define the attributes of a grabbable object and makes it selectable.
    /// This should be added when you need to define where the object is being grabbed so you can
    /// apply the correct transformations to the object.
    /// </summary>
    public interface IGrabbable
    {
        /// <summary>
        /// A Collection of grab points as <see cref="Pose">s. Used to calculate user applied transfomations
        /// </summary>
        List<Pose> GrabPoints { get; }

        /// <summary>
        /// The transform that should be used when applying transformations to the Object.
        /// Can be the transform of the object itself or another specified transform
        /// </summary>
        Transform Transform { get; }
    }
}
