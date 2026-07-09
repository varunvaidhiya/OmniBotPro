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

namespace Oculus.Interaction.Input
{
    public partial class OneEuroFilter
    {
        /// <summary>
        /// Factory method for creating One Euro filter instances to filter `float` signals.
        /// </summary>
        /// <returns>A new instance of an <see cref="IOneEuroFilter{TData}"/> for `float`</returns>
        public static IOneEuroFilter<float> CreateFloat()
        {
            return new OneEuroFilter();
        }

        /// <summary>
        /// Factory method for creating One Euro filter instances to filter `Vector2` signals.
        /// </summary>
        /// <returns>A new instance of an <see cref="IOneEuroFilter{TData}"/> for `Vector2`</returns>
        public static IOneEuroFilter<Vector2> CreateVector2()
        {
            return new OneEuroFilterMulti<Vector2>(2,
                (values) => new Vector2(values[0], values[1]),
                (value, index) => value[index]);
        }

        /// <summary>
        /// Factory method for creating One Euro filter instances to filter `Vector3` signals.
        /// </summary>
        /// <returns>A new instance of an <see cref="IOneEuroFilter{TData}"/> for `Vector3`</returns>
        public static IOneEuroFilter<Vector3> CreateVector3()
        {
            return new OneEuroFilterMulti<Vector3>(3,
                (values) => new Vector3(values[0], values[1], values[2]),
                (value, index) => value[index]);
        }

        /// <summary>
        /// Factory method for creating One Euro filter instances to filter `Vector4` signals.
        /// </summary>
        /// <returns>A new instance of an <see cref="IOneEuroFilter{TData}"/> for `Vector4`</returns>
        public static IOneEuroFilter<Vector4> CreateVector4()
        {
            return new OneEuroFilterMulti<Vector4>(4,
                (values) => new Vector4(values[0], values[1], values[2], values[3]),
                (value, index) => value[index]);
        }

        /// <summary>
        /// Factory method for creating One Euro filter instances to filter `Quaternion` signals.
        /// </summary>
        /// <returns>A new instance of an <see cref="IOneEuroFilter{TData}"/> for `Quaternion`</returns>
        public static IOneEuroFilter<Quaternion> CreateQuaternion()
        {
            return new OneEuroFilterMulti<Quaternion>(4,
                (values) => new Quaternion(values[0], values[1], values[2], values[3]).normalized,
                (value, index) => value[index]);
        }

        /// <summary>
        /// Factory method for creating One Euro filter instances to filter `Pose` signals.
        /// </summary>
        /// <returns>A new instance of an <see cref="IOneEuroFilter{TData}"/> for `Pose`</returns>
        public static IOneEuroFilter<Pose> CreatePose()
        {
            return new OneEuroFilterMulti<Pose>(7,
                (values) => new Pose(new Vector3(values[0], values[1], values[2]), new Quaternion(values[3], values[4], values[5], values[6]).normalized),
                (value, index) => (index > 2) ? value.rotation[index - 3] : value.position[index]);
        }
    }
}
