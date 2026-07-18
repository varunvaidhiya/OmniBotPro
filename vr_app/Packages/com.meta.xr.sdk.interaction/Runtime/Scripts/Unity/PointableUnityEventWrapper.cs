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
using UnityEngine.Events;

namespace Oculus.Interaction
{
    /// <summary>
    /// Exposes Unity pointer events that broadcast pointer events from an <see cref="IPointable"/> component (ex. a poke interactable on a button).
    /// </summary>
    public class PointableUnityEventWrapper : MonoBehaviour
    {
        /// <summary>
        /// The Pointable component to wrap.
        /// </summary>
        [Tooltip("The Pointable component to wrap.")]
        [SerializeField, Interface(typeof(IPointable))]
        private UnityEngine.Object _pointable;
        private IPointable Pointable;

        private HashSet<int> _pointers;

        /// <summary>
        /// Raised when the IPointable is released.
        /// </summary>
        [Tooltip("Raised when the IPointable is released.")]
        [SerializeField]
        private UnityEvent<PointerEvent> _whenRelease;

        /// <summary>
        /// Raised when the IPointable is hovered.
        /// </summary>
        [Tooltip("Raised when the IPointable is hovered.")]
        [SerializeField]
        private UnityEvent<PointerEvent> _whenHover;

        /// <summary>
        /// Raised when the IPointable is unhovered (it was hovered but now it isn't).
        /// </summary>
        [Tooltip("Raised when the IPointable is unhovered (it was hovered but now it isn't).")]
        [SerializeField]
        private UnityEvent<PointerEvent> _whenUnhover;

        /// <summary>
        /// Raised when the IPointable is selected.
        /// </summary>
        [Tooltip("Raised when the IPointable is selected.")]
        [SerializeField]
        private UnityEvent<PointerEvent> _whenSelect;

        /// <summary>
        /// Raised when the IPointable is unselected (it was selected but now it isn't).
        /// </summary>
        [Tooltip("Raised when the IPointable is unselected (it was selected but now it isn't).")]
        [SerializeField]
        private UnityEvent<PointerEvent> _whenUnselect;

        /// <summary>
        /// Raised when the IPointable moves.
        /// </summary>
        [Tooltip("Raised when the IPointable moves.")]
        [SerializeField]
        private UnityEvent<PointerEvent> _whenMove;

        /// <summary>
        /// Raised when the IPointable is canceled.
        /// </summary>
        [Tooltip("Raised when the IPointable is canceled.")]
        [SerializeField]
        private UnityEvent<PointerEvent> _whenCancel;

        /// <summary>
        /// Raised when the <see cref="IPointable"/> emits a <see cref="PointerEvent"/>
        /// with the <see cref="PointerEventType.Unselect"/> state.
        /// </summary>
        public UnityEvent<PointerEvent> WhenRelease => _whenRelease;

        /// <summary>
        /// Raised when the <see cref="IPointable"/> emits a <see cref="PointerEvent"/>
        /// with the <see cref="PointerEventType.Hover"/> state.
        /// </summary>
        public UnityEvent<PointerEvent> WhenHover => _whenHover;

        /// <summary>
        /// Raised when the <see cref="IPointable"/> emits a <see cref="PointerEvent"/>
        /// with the <see cref="PointerEventType.Unhover"/> state.
        /// </summary>
        public UnityEvent<PointerEvent> WhenUnhover => _whenUnhover;

        /// <summary>
        /// Raised when the <see cref="IPointable"/> emits a <see cref="PointerEvent"/>
        /// with the <see cref="PointerEventType.Select"/> state.
        /// </summary>
        public UnityEvent<PointerEvent> WhenSelect => _whenSelect;

        /// <summary>
        /// Raised when the <see cref="IPointable"/> emits a <see cref="PointerEvent"/>
        /// with the <see cref="PointerEventType.Unselect"/> state.
        /// </summary>
        public UnityEvent<PointerEvent> WhenUnselect => _whenUnselect;

        /// <summary>
        /// Raised when the <see cref="IPointable"/> emits a <see cref="PointerEvent"/>
        /// with the <see cref="PointerEventType.Move"/> state.
        /// </summary>
        public UnityEvent<PointerEvent> WhenMove => _whenMove;

        /// <summary>
        /// Raised when the <see cref="IPointable"/> emits a <see cref="PointerEvent"/>
        /// with the <see cref="PointerEventType.Cancel"/> state.
        /// </summary>
        public UnityEvent<PointerEvent> WhenCancel => _whenCancel;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Pointable = _pointable as IPointable;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Pointable, nameof(Pointable));
            _pointers = new HashSet<int>();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Hover:
                    _whenHover.Invoke(evt);
                    _pointers.Add(evt.Identifier);
                    break;
                case PointerEventType.Unhover:
                    _whenUnhover.Invoke(evt);
                    _pointers.Remove(evt.Identifier);
                    break;
                case PointerEventType.Select:
                    _whenSelect.Invoke(evt);
                    break;
                case PointerEventType.Unselect:
                    if (_pointers.Contains(evt.Identifier))
                    {
                        _whenRelease.Invoke(evt);
                    }
                    _whenUnselect.Invoke(evt);
                    break;
                case PointerEventType.Move:
                    _whenMove.Invoke(evt);
                    break;
                case PointerEventType.Cancel:
                    _whenCancel.Invoke(evt);
                    _pointers.Remove(evt.Identifier);
                    break;
            }
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="PointableUnityEventWrapper"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllPointableUnityEventWrapper(IPointable pointable)
        {
            InjectPointable(pointable);
        }

        /// <summary>
        /// Sets the underlying <see cref="IPointable"/> for a dynamically instantiated
        /// <see cref="PointableUnityEventWrapper"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectPointable(IPointable pointable)
        {
            _pointable = pointable as UnityEngine.Object;
            Pointable = pointable;
        }

        #endregion
    }
}
