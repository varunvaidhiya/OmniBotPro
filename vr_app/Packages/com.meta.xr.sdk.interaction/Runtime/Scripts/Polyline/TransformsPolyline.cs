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
    /// A IPolyline defined by an array of transforms.
    /// Each transform represents a point in the line, in the given order.
    /// </summary>
    public class TransformsPolyline : MonoBehaviour,
        IPolyline
    {
        /// <summary>
        /// The array of transforms that represent the polyline in order
        /// </summary>
        [SerializeField]
        private Transform[] _transforms;

        public int PointsCount => _transforms.Length;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionField(_transforms, nameof(_transforms));
            this.EndStart(ref _started);
        }

        public Vector3 PointAtIndex(int index)
        {
            return _transforms[index].position;
        }

        #region Injects

        public void InjectAllTransformsPolyline(Transform[] transforms)
        {
            InjectTransforms(transforms);
        }

        public void InjectTransforms(Transform[] transforms)
        {
            _transforms = transforms;
        }

        #endregion
    }
}
