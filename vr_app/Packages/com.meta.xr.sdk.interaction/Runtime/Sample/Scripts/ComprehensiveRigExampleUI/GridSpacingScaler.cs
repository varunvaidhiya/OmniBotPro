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
using UnityEngine.UI;

namespace Oculus.Interaction.Samples
{
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup))]
    public class GridSpacingScaler : MonoBehaviour
    {
        public enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        public Axis scaleAxis;
        public Vector2 minSpacing;

        private GridLayoutGroup _gridLayoutGroup;
        private RectTransform _rectTransform;

        void Start()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform == null) Debug.LogError("GameObject Transform is not a Rect Transform");
            _gridLayoutGroup = gameObject.GetComponent<GridLayoutGroup>();
            if (_gridLayoutGroup == null) Debug.LogError("GameObject does not include a GridLayoutGroup");
        }

        void LateUpdate()
        {
            var contianerSize = Mathf.Floor(_rectTransform.rect.size[(int)scaleAxis]);
            var spacing = minSpacing[(int)scaleAxis];
            var elementSize = _gridLayoutGroup.cellSize[(int)scaleAxis];
            var padding = scaleAxis == Axis.Horizontal ? _gridLayoutGroup.padding.horizontal : _gridLayoutGroup.padding.vertical;

            if (elementSize + spacing <= 0)
            {
                return;
            }

            var cellCount = Mathf.Max(1, Mathf.FloorToInt((contianerSize - padding + spacing + 0.001f) / (elementSize + spacing)));
            var availableSpace = contianerSize - (cellCount * elementSize);

            _gridLayoutGroup.constraint = scaleAxis == Axis.Horizontal ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.FixedRowCount;
            _gridLayoutGroup.constraintCount = cellCount;
            if (cellCount > 1)
            {
                var scaledSpacing = availableSpace / ((float)(cellCount - 1));
                var layoutSpacing = _gridLayoutGroup.spacing;
                layoutSpacing[(int)scaleAxis] = Mathf.Floor(scaledSpacing);
                _gridLayoutGroup.spacing = layoutSpacing;
                return;
            }
        }
    }
}
