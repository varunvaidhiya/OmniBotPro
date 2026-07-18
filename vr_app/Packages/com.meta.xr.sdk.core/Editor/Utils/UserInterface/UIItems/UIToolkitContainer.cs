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
using Meta.XR.Editor.UserInterface.RLDS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.XR.Editor.UserInterface
{
    /// <summary>
    /// A container that renders UIToolkit content within an IMGUI context.
    /// This allows UIToolkit features (like clickable link tags) to work within IMGUI windows.
    /// The container obtains its rect from IMGUI layout, similar to ScrollView.
    /// </summary>
    internal class UIToolkitContainer : GroupedItem
    {
        private VisualElement _container;
        private bool _isAttached;
        private EditorWindow _parentWindow;
        private Rect _lastRect;
        private bool _drawnThisFrame;
        private bool _updateRegistered;

        public GUIStyle BoxStyle { get; set; } = Styles.GUIStyles.ScrollViewBox;

        public UIToolkitContainer(IEnumerable<IUserInterfaceItem> items,
            Utils.UIItemPlacementType placementType = Utils.UIItemPlacementType.Vertical,
            params GUILayoutOption[] options)
            : base(items, new GUIStyle(), placementType, options)
        {
        }

        public override void Draw()
        {
            var rect = EditorGUILayout.BeginVertical(BoxStyle, GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            DrawCardStyle(rect, 1);
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                if (_parentWindow == null)
                {
                    _parentWindow = EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
                }

                if (_parentWindow == null)
                {
                    return;
                }

                var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                var windowPos = _parentWindow.position.position;
                var windowRelativePos = screenPos - windowPos;

                _lastRect = new Rect(windowRelativePos.x, windowRelativePos.y, rect.width, rect.height);
                EnsureAttached();
                UpdatePosition();

                _drawnThisFrame = true;
                if (_container != null)
                {
                    _container.style.display = DisplayStyle.Flex;
                }

                if (!_updateRegistered)
                {
                    EditorApplication.update += OnEditorUpdate;
                    _updateRegistered = true;
                }
            }
        }

        private void OnEditorUpdate()
        {
            if (!_drawnThisFrame && _container != null)
            {
                _container.style.display = DisplayStyle.None;
            }
            _drawnThisFrame = false;
        }

        /// <summary>
        /// Unregisters the EditorApplication.update callback and detaches the container
        /// from the parent window. Call this when the container is no longer needed.
        /// </summary>
        public void Cleanup()
        {
            if (_updateRegistered)
            {
                EditorApplication.update -= OnEditorUpdate;
                _updateRegistered = false;
            }

            if (_isAttached && _container != null && _parentWindow != null
                && _parentWindow.rootVisualElement != null)
            {
                _parentWindow.rootVisualElement.Remove(_container);
            }

            _isAttached = false;
            _container = null;
            _parentWindow = null;
        }

        private void DrawCardStyle(Rect contentRect, int borderWidth)
        {
            var bottomRect = new Rect(
                new Vector2(contentRect.position.x - borderWidth, contentRect.position.y - borderWidth),
                new Vector2(contentRect.width + (2 * borderWidth), contentRect.height + (2 * borderWidth)));

            var borderColor = Styles.Colors.DarkBorder;
            GUI.DrawTexture(bottomRect, borderColor.ToTexture(), ScaleMode.ScaleAndCrop,
                false, 1f, GUI.color, Vector4.zero, Styles.Constants.RoundedBorderVectors);

            var backgroundColor = Styles.Colors.Grey40;
            GUI.DrawTexture(contentRect, backgroundColor.ToTexture(), ScaleMode.ScaleAndCrop,
                false, 1f, GUI.color, Vector4.zero, Styles.Constants.RoundedBorderVectors);
        }

        private void EnsureAttached()
        {
            if (_isAttached && _container != null)
            {
                return;
            }

            if (_parentWindow == null || _parentWindow.rootVisualElement == null)
            {
                return;
            }

            AttachToWindow();
        }

        private void AttachToWindow()
        {
            _container = CreateStyledContainer(useAbsolutePositioning: true);

            var scrollView = CreateScrollViewWithItems();
            _container.Add(scrollView);

            _parentWindow.rootVisualElement.Add(_container);
            _isAttached = true;
        }

        private void UpdatePosition()
        {
            if (_container == null)
            {
                return;
            }

            _container.style.left = _lastRect.x;
            _container.style.top = _lastRect.y;
            _container.style.width = _lastRect.width;
            _container.style.height = _lastRect.height;
        }

        public override VisualElement Build()
        {
            if (_container != null)
            {
                return _container;
            }

            _container = CreateStyledContainer(useAbsolutePositioning: false);

            var scrollView = CreateScrollViewWithItems();
            _container.Add(scrollView);

            return _container;
        }

        private VisualElement CreateStyledContainer(bool useAbsolutePositioning)
        {
            var lightMode = !EditorGUIUtility.isProSkin;
            var styleSheet = RLDSUtils.LoadStyleSheet(lightMode);

            var container = new VisualElement();
            container.style.position = useAbsolutePositioning ? Position.Absolute : Position.Relative;
            if (!useAbsolutePositioning)
            {
                container.style.flexGrow = 1;
            }
            container.style.overflow = Overflow.Hidden;
            container.style.borderTopLeftRadius = Styles.Constants.RoundedBorderVectors.x;
            container.style.borderTopRightRadius = Styles.Constants.RoundedBorderVectors.y;
            container.style.borderBottomRightRadius = Styles.Constants.RoundedBorderVectors.z;
            container.style.borderBottomLeftRadius = Styles.Constants.RoundedBorderVectors.w;

            if (styleSheet != null)
            {
                container.styleSheets.Add(styleSheet);
            }

            return container;
        }

        private UnityEngine.UIElements.ScrollView CreateScrollViewWithItems()
        {
            var scrollView = new UnityEngine.UIElements.ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.contentContainer.style.paddingTop = RLDS.Styles.Spacing.SpaceSM;
            scrollView.contentContainer.style.paddingBottom = RLDS.Styles.Spacing.SpaceSM;
            scrollView.contentContainer.style.paddingLeft = RLDS.Styles.Spacing.SpaceSM;
            scrollView.contentContainer.style.paddingRight = RLDS.Styles.Spacing.SpaceSM;

            foreach (var item in Items)
            {
                if (item.Hide) continue;
                scrollView.Add(item.Build());
            }

            return scrollView;
        }
    }
}
