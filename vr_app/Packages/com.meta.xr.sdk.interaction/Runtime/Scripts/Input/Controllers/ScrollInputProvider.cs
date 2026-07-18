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
using UnityEngine.EventSystems;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// This class utilizes the <see cref="IAxis2D"/> interface to extract horizontal and vertical axis input data,
    /// and the <see cref="IInteractorView"/> interface to associate the scrolling input with a specific <see cref="Interactor{TInteractor,TInteractable}"/>.
    /// The combination of these two interfaces enables this class to provide scrolling input data
    /// that can be used to drive scrolling behavior for various <see cref="PointableCanvas"/> elements in the Interaction SDK.
    /// </summary>
    public class ScrollInputProvider : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IAxis2D))]
        [Tooltip("Input 2D Axis from which the horizontal and vertical axis will be extracted")]
        private UnityEngine.Object _axis2D;

        [SerializeField, Interface(typeof(IInteractorView))]
        private UnityEngine.Object _interactor;

        [SerializeField, Optional]
        [Tooltip("The speed at which scrolling occurs.")]
        private float _scrollSpeed = 5f;

        [SerializeField, Optional]
        [Tooltip("The dead zone threshold for input.")]
        private float _deadZone = 0.1f;

        [SerializeField, Optional]
        [Tooltip("Enable or disable scrolling on the X axis.")]
        private bool _scrollXAxis = true;

        [SerializeField, Optional]
        [Tooltip("Enable or disable scrolling on the Y axis.")]
        private bool _scrollYAxis = true;

        [SerializeField, Optional]
        [Tooltip("Invert the X axis input.")]
        private bool _invertXAxis = false;

        [SerializeField, Optional]
        [Tooltip("Invert the Y axis input.")]
        private bool _invertYAxis = false;

        private IAxis2D Axis2D { get; set; }
        private IInteractorView InteractorView { get; set; }
        private PointerEventData _pointerEventData;
        private PointableCanvasModule.Pointer _currentPointer;
        private bool _started = false;

        private void Awake()
        {
            if (Axis2D == null)
            {
                Axis2D = _axis2D as IAxis2D;
            }
            if (InteractorView == null)
            {
                InteractorView = _interactor as IInteractorView;
            }
        }

        private void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_axis2D, nameof(_axis2D));
            this.AssertField(_interactor, nameof(_interactor));
            _pointerEventData = new PointerEventData(EventSystem.current);
            this.EndStart(ref _started);
        }

        private void OnEnable()
        {
            if (_started)
            {
                PointableCanvasModule.WhenPointerStarted += HandlePointerStarted;
            }
        }

        private void OnDisable()
        {
            if (_started)
            {
                PointableCanvasModule.WhenPointerStarted -= HandlePointerStarted;

                if (_currentPointer != null)
                {
                    _currentPointer.WhenUpdated -= HandlePointerUpdated;
                    _currentPointer = null;
                }
            }
        }

        private void HandlePointerStarted(PointableCanvasModule.Pointer pointer)
        {
            if (pointer.Identifier == InteractorView.Identifier)
            {
                if (_currentPointer != null)
                {
                    _currentPointer.WhenUpdated -= HandlePointerUpdated;
                }

                pointer.WhenUpdated += HandlePointerUpdated;
                _currentPointer = pointer;
            }
        }

        private void HandlePointerUpdated(PointerEventData pointerEventData)
        {
            Vector2 scrollDelta = TryGetScrollData();

            if (scrollDelta != Vector2.zero)
            {
                _pointerEventData.scrollDelta = scrollDelta;
                _pointerEventData.position = pointerEventData.position;
                ExecuteEvents.ExecuteHierarchy(pointerEventData.pointerCurrentRaycast.gameObject, _pointerEventData, ExecuteEvents.scrollHandler);
            }
        }

        private Vector2 TryGetScrollData()
        {
            Vector2 scrollInput = Axis2D.Value();

            if (scrollInput.magnitude < _deadZone)
            {
                return Vector2.zero;
            }

            scrollInput = ApplyAxisSettings(scrollInput);
            return scrollInput * _scrollSpeed;
        }

        private Vector2 ApplyAxisSettings(Vector2 input)
        {
            input.x = _scrollXAxis ? (_invertXAxis ? -input.x : input.x) : 0;
            input.y = _scrollYAxis ? (_invertYAxis ? -input.y : input.y) : 0;

            return input;
        }

        #region Inject

        /// <summary>
        /// Injects all dependencies into this instance, including the 2D axis input and interactor view.
        /// This method is used to initialize the scroll input provider with the required dependencies.
        /// </summary>
        /// <param name="axis2D">The <see cref="IAxis2D"/> input that will be used to extract horizontal and vertical axis data.</param>
        /// <param name="interactorView">The <see cref="IInteractorView"/> view that will be associated with this scroll input provider.</param>
        public void InjectAll(IAxis2D axis2D, IInteractorView interactorView)
        {
            InjectAxis2D(axis2D);
            InjectInteractorView(interactorView);
        }

        /// <summary>
        /// Injects the 2D axis input dependency into this instance, allowing for horizontal and vertical axis data extraction.
        /// This method is used to set the 2D axis input that will be used by the scroll input provider.
        /// </summary>
        /// <param name="axis2D">The <see cref="IAxis2D"/> input that will be used to extract horizontal and vertical axis data.</param>
        public void InjectAxis2D(IAxis2D axis2D)
        {
            _axis2D = axis2D as UnityEngine.Object;
            Axis2D = axis2D;
        }

        /// <summary>
        /// Injects the interactor view dependency into this instance, associating it with the scroll input provider.
        /// This method is used to set the interactor view that will be used by the scroll input provider.
        /// </summary>
        /// <param name="interactorView">The <see cref="IInteractorView"/> view that will be associated with this scroll input provider.</param>
        public void InjectInteractorView(IInteractorView interactorView)
        {
            _interactor = interactorView as UnityEngine.Object;
            InteractorView = interactorView;
        }

        #endregion
    }
}
