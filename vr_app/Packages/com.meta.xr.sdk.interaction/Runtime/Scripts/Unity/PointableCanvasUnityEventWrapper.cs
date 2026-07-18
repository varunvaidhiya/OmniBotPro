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
using UnityEngine.Events;

namespace Oculus.Interaction
{
    /// <summary>
    /// Hooks into events raised from <see cref="PointableCanvasModule"/> that correspond to a referenced
    /// <see cref="IPointableCanvas" />, and tracks Unity Selectable (button, toggle, etc) events on that
    /// canvas rather than hooking into each Selectable on the Canvas individually.
    /// </summary>
    public class PointableCanvasUnityEventWrapper : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="IPointableCanvas" /> component to wrap.
        /// </summary>
        [SerializeField, Interface(typeof(IPointableCanvas))]
        private UnityEngine.Object _pointableCanvas;
        private IPointableCanvas PointableCanvas;

        /// <summary>
        /// If true, hover events will be ignored while the pointer is dragging.
        /// </summary>
        [SerializeField, Tooltip("Selection and hover events will not be fired while dragging.")]
        private bool _suppressWhileDragging = true;

        /// <summary>
        /// Raised when any Selectable on the canvas is hovered.
        /// </summary>
        [Obsolete("This event is obsolete. Use _whenBeginHighlightWithObject instead.")]
        [SerializeField]
        [Tooltip("Raised when beginning hover of a uGUI selectable")]
        private UnityEvent _whenBeginHighlight;

        /// <summary>
        /// Raised when any Selectable on the canvas is unhovered.
        /// </summary>
        [Obsolete("This event is obsolete. Use _whenEndHighlightWithObject instead.")]
        [SerializeField]
        [Tooltip("Raised when ending hover of a uGUI selectable")]
        private UnityEvent _whenEndHighlight;

        /// <summary>
        /// Raised when a pointer press happens on a Selectable.
        /// </summary>
        [Obsolete("This event is obsolete. Use _whenSelectedHoveredWithObject instead.")]
        [SerializeField]
        [Tooltip("Raised when selecting a hovered uGUI selectable")]
        private UnityEvent _whenSelectedHovered;

        /// <summary>
        /// Raised when a pointer press happens over an empty area.
        /// </summary>
        [SerializeField]
        [Tooltip("Raised when selecting with no uGUI selectable hovered")]
        private UnityEvent _whenSelectedEmpty;

        /// <summary>
        /// Raised when a pointer release happens on a Selectable.
        /// </summary>
        [Obsolete("This event is obsolete. Use _whenUnselectedHoveredWithObject instead.")]
        [SerializeField]
        [Tooltip("Raised when deselecting a hovered uGUI selectable")]
        private UnityEvent _whenUnselectedHovered;

        /// <summary>
        /// Raised when a pointer release happens over an empty area.
        /// </summary>
        [SerializeField]
        [Tooltip("Raised when deselecting with no uGUI selectable hovered")]
        private UnityEvent _whenUnselectedEmpty;


        /// <summary>
        ///     Raised when any Selectable on the canvas is hovered. Provides the hovered GameObject.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Raised when beginning hover of a uGUI selectable. Provides the hovered GameObject.")]
        private UnityEvent<GameObject> _whenBeginHighlightWithObject;

        /// <summary>
        ///     Raised when any Selectable on the canvas is unhovered. Provides the unhovered GameObject.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Raised when ending hover of a uGUI selectable. Provides the unhovered GameObject.")]
        private UnityEvent<GameObject> _whenEndHighlightWithObject;

        /// <summary>
        ///     Raised when a pointer press happens on a Selectable. Provides the selected GameObject.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Raised when selecting a hovered uGUI selectable. Provides the selected GameObject.")]
        private UnityEvent<GameObject> _whenSelectedHoveredWithObject;

        /// <summary>
        ///     Raised when a pointer release happens on a Selectable. Provides the unselected GameObject.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Raised when deselecting a hovered uGUI selectable. Provides the unselected GameObject.")]
        private UnityEvent<GameObject> _whenUnselectedHoveredWithObject;


        protected bool _started = false;

        private bool ShouldFireEvent(PointableCanvasEventArgs args)
        {
            if (args.Canvas != PointableCanvas.Canvas)
            {
                return false;
            }
            if (_suppressWhileDragging && args.Dragging)
            {
                return false;
            }
            return true;
        }

        private void PointableCanvasModule_WhenSelectableHoverEnter(PointableCanvasEventArgs args)
        {
            if (ShouldFireEvent(args))
            {
                // Fire original event for backwards compatibility
#pragma warning disable 0618                
                _whenBeginHighlight.Invoke();
#pragma warning restore 0618
                // Fire new parameterized event if the GameObject is available
                if (args.Hovered != null)
                {
                    _whenBeginHighlightWithObject.Invoke(args.Hovered);
                }
            }
        }

        private void PointableCanvasModule_WhenSelectableHoverExit(PointableCanvasEventArgs args)
        {
            if (ShouldFireEvent(args))
            {
                // Fire original event for backwards compatibility
#pragma warning disable 0618
                _whenEndHighlight.Invoke();
#pragma warning restore 0618
                // Fire new parameterized event if the GameObject is available
                if (args.Hovered != null)
                {
                    _whenEndHighlightWithObject.Invoke(args.Hovered);
                }
            }
        }

        private void PointableCanvasModule_WhenSelectableSelected(PointableCanvasEventArgs args)
        {
            if (ShouldFireEvent(args))
            {
                if (args.Hovered == null)
                {
                    _whenSelectedEmpty.Invoke();
                }
                else
                {
                    // Fire original event for backwards compatibility
#pragma warning disable 0618
                    _whenSelectedHovered.Invoke();
#pragma warning restore 0618
                    // Fire new parameterized event
                    _whenSelectedHoveredWithObject.Invoke(args.Hovered);
                }
            }
        }

        private void PointableCanvasModule_WhenSelectableUnselected(PointableCanvasEventArgs args)
        {
            if (ShouldFireEvent(args))
            {
                if (args.Hovered == null)
                {
                    _whenUnselectedEmpty.Invoke();
                }
                else
                {
                    // Fire original event for backwards compatibility
#pragma warning disable 0618
                    _whenUnselectedHovered.Invoke();
#pragma warning restore 0618
                    // Fire new parameterized event
                    _whenUnselectedHoveredWithObject.Invoke(args.Hovered);
                }
            }
        }

        protected virtual void Awake()
        {
            PointableCanvas = _pointableCanvas as IPointableCanvas;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(PointableCanvas, nameof(PointableCanvas));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                PointableCanvasModule.WhenSelectableHovered += PointableCanvasModule_WhenSelectableHoverEnter;
                PointableCanvasModule.WhenSelectableUnhovered += PointableCanvasModule_WhenSelectableHoverExit;
                PointableCanvasModule.WhenSelected += PointableCanvasModule_WhenSelectableSelected;
                PointableCanvasModule.WhenUnselected += PointableCanvasModule_WhenSelectableUnselected;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                PointableCanvasModule.WhenSelectableHovered -= PointableCanvasModule_WhenSelectableHoverEnter;
                PointableCanvasModule.WhenSelectableUnhovered -= PointableCanvasModule_WhenSelectableHoverExit;
                PointableCanvasModule.WhenSelected -= PointableCanvasModule_WhenSelectableSelected;
                PointableCanvasModule.WhenUnselected -= PointableCanvasModule_WhenSelectableUnselected;
            }
        }
    }
}
