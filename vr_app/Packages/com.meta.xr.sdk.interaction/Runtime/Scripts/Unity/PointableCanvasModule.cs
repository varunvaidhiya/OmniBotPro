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
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.Assertions;
using System;

namespace Oculus.Interaction
{
    /// <summary>
    /// Arguments from events emitted by <see cref="PointableCanvasModule"/>
    /// containing data about the state of the event system.
    /// </summary>
    public class PointableCanvasEventArgs
    {
        /// <summary>
        /// The canvas that's being interacted with.
        /// </summary>
        public readonly Canvas Canvas;

        /// <summary>
        /// The GameObject that's being hovered, if any.
        /// </summary>
        public readonly GameObject Hovered;

        /// <summary>
        /// Whether the user is dragging at the time of this event.
        /// Corresponds to <see cref="PointerEventData.dragging"/>
        /// </summary>
        public readonly bool Dragging;

        /// <summary>
        /// Gets the unique identifier for the pointer that triggered the event
        /// </summary>
        public readonly int? PointerId;

        /// <summary>
        /// Create a new <see cref="PointableCanvasEventArgs"/>
        /// </summary>
        /// <param name="canvas">The canvas that's being interacted with.</param>
        /// <param name="hovered">The GameObject that's being hovered, if any.</param>
        /// <param name="dragging">Whether the user is dragging at the time of this event.</param>
        /// <param name="pointerId">Unique identifier for the pointer that triggered the event</param>
        public PointableCanvasEventArgs(Canvas canvas, GameObject hovered, bool dragging, int? pointerId = null)
        {
            Canvas = canvas;
            Hovered = hovered;
            Dragging = dragging;
            PointerId = pointerId;
        }
    }

    /// <summary>
    /// PointableCanvasModule is a context-like object which exists in the scene and handles routing for certain types of
    /// <see cref="PointerEvent"/>s, translating them into Unity pointer events which can be routed to and consumed by Unity Canvases.
    /// <see cref="PointableCanvas"/> requires that the scene contain a PointableCanvasModule.
    /// </summary>
    public class PointableCanvasModule : PointerInputModule
    {
        /// <summary>
        /// Global event invoked in response to a <see cref="PointerEventType.Select"/> on an <see cref="IPointableCanvas"/>.
        /// Though this event itself is static, it is invoked by the PointableCanvasModule instance in the scene as part of
        /// <see cref="Process"/>.
        /// </summary>
        public static event Action<PointableCanvasEventArgs> WhenSelected;

        /// <summary>
        /// Global event invoked in response to a <see cref="PointerEventType.Unselect"/> on an <see cref="IPointableCanvas"/>.
        /// Though this event itself is static, it is invoked by the PointableCanvasModule instance in the scene as part of
        /// <see cref="Process"/>.
        /// </summary>
        public static event Action<PointableCanvasEventArgs> WhenUnselected;

        /// <summary>
        /// Global event invoked in response to a <see cref="PointerEventType.Hover"/> on an <see cref="IPointableCanvas"/>.
        /// Though this event itself is static, it is invoked by the PointableCanvasModule instance in the scene as part of
        /// <see cref="Process"/>.
        /// </summary>
        public static event Action<PointableCanvasEventArgs> WhenSelectableHovered;

        /// <summary>
        /// Global event invoked in response to a <see cref="PointerEventType.Unhover"/> on an <see cref="IPointableCanvas"/>.
        /// Though this event itself is static, it is invoked by the PointableCanvasModule instance in the scene as part of
        /// <see cref="Process"/>.
        /// </summary>
        public static event Action<PointableCanvasEventArgs> WhenSelectableUnhovered;

        /// <summary>
        /// Global event invoked whenever a new <see cref="Pointer"/> is started. Though this event itself is static, it is
        /// invoked by the PointableCanvasModule instance in the scene as part of <see cref="Process"/>.
        /// </summary>
        public static event Action<Pointer> WhenPointerStarted;

        [Tooltip("If true, the initial press position will be used as the drag start " +
            "position, rather than the position when drag threshold is exceeded. This is used " +
            "to prevent the pointer position shifting relative to the surface while dragging.")]
        [SerializeField]
        private bool _useInitialPressPositionForDrag = true;

        [Tooltip("If true, this module will disable other input modules in the event system " +
            "and will be the only input module used in the scene.")]
        [SerializeField]
        private bool _exclusiveMode = false;

        /// <summary>
        /// If true, this module will disable other input modules in the event system and will be the only input module used in the
        /// scene.
        /// </summary>
        public bool ExclusiveMode { get => _exclusiveMode; set => _exclusiveMode = value; }

        private Camera _pointerEventCamera;
        private static PointableCanvasModule _instance = null;
        private static PointableCanvasModule Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Registers an <see cref="IPointableCanvas"/> with the PointableCanvasModule in the scene so that its
        /// <see cref="PointerEvent"/>s can be correctly handled, converted, and forwarded.
        /// </summary>
        /// <param name="pointerCanvas">The <see cref="IPointableCanvas"/> to register</param>
        public static void RegisterPointableCanvas(IPointableCanvas pointerCanvas)
        {
            Assert.IsNotNull(Instance, $"A <b>{nameof(PointableCanvasModule)}</b> is required in the scene.");
            Instance.AddPointerCanvas(pointerCanvas);
        }

        /// <summary>
        /// Unregisters an <see cref="IPointableCanvas"/> with the PointableCanvasModule in the scene. <see cref="PointerEvent"/>s
        /// from that canvas will no longer be propagated to the Unity Canvas.
        /// </summary>
        /// <param name="pointerCanvas">The <see cref="IPointableCanvas"/> to unregister</param>
        public static void UnregisterPointableCanvas(IPointableCanvas pointerCanvas)
        {
            Instance?.RemovePointerCanvas(pointerCanvas);
        }

        private Dictionary<int, PointerImpl> _pointerMap = new Dictionary<int, PointerImpl>();
        private List<RaycastResult> _raycastResultCache = new List<RaycastResult>();
        private List<PointerImpl> _pointersForDeletion = new List<PointerImpl>();
        private Dictionary<IPointableCanvas, Action<PointerEvent>> _pointerCanvasActionMap =
            new Dictionary<IPointableCanvas, Action<PointerEvent>>();
        private List<BaseInputModule> _inputModules = new List<BaseInputModule>();

        private PointerImpl[] _pointersToProcessScratch = Array.Empty<PointerImpl>();

        private void AddPointerCanvas(IPointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = (args) => HandlePointerEvent(pointerCanvas.Canvas, args);
            _pointerCanvasActionMap.Add(pointerCanvas, pointerCanvasAction);
            pointerCanvas.WhenPointerEventRaised += pointerCanvasAction;
        }

        private void RemovePointerCanvas(IPointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = _pointerCanvasActionMap[pointerCanvas];
            _pointerCanvasActionMap.Remove(pointerCanvas);
            pointerCanvas.WhenPointerEventRaised -= pointerCanvasAction;

            List<int> pointerIDs = new List<int>(_pointerMap.Keys);
            foreach (int pointerID in pointerIDs)
            {
                PointerImpl pointer = _pointerMap[pointerID];
                if (pointer.Canvas != pointerCanvas.Canvas)
                {
                    continue;
                }
                ClearPointerSelection(pointer.PointerEventData);
                pointer.MarkForDeletion();
                _pointersForDeletion.Add(pointer);
                _pointerMap.Remove(pointerID);
            }
        }

        private void HandlePointerEvent(Canvas canvas, PointerEvent evt)
        {
            PointerImpl pointer;

            switch (evt.Type)
            {
                case PointerEventType.Hover:
                    pointer = new PointerImpl(evt.Identifier, canvas);
                    pointer.PointerEventData = new PointerEventData(eventSystem)
                    {
                        pointerId = evt.Identifier
                    };
                    pointer.SetPosition(evt.Pose.position);
                    _pointerMap.Add(evt.Identifier, pointer);
                    WhenPointerStarted?.Invoke(pointer);
                    break;
                case PointerEventType.Unhover:
                    if (_pointerMap.TryGetValue(evt.Identifier, out pointer))
                    {
                        _pointerMap.Remove(evt.Identifier);
                        pointer.MarkForDeletion();
                        _pointersForDeletion.Add(pointer);
                    }
                    break;
                case PointerEventType.Select:
                    if (_pointerMap.TryGetValue(evt.Identifier, out pointer))
                    {
                        pointer.SetPosition(evt.Pose.position);
                        pointer.Press();
                    }
                    break;
                case PointerEventType.Unselect:
                    if (_pointerMap.TryGetValue(evt.Identifier, out pointer))
                    {
                        pointer.SetPosition(evt.Pose.position);
                        pointer.Release();
                    }
                    break;
                case PointerEventType.Move:
                    if (_pointerMap.TryGetValue(evt.Identifier, out pointer))
                    {
                        pointer.SetPosition(evt.Pose.position);
                    }
                    break;
                case PointerEventType.Cancel:
                    if (_pointerMap.TryGetValue(evt.Identifier, out pointer))
                    {
                        _pointerMap.Remove(evt.Identifier);
                        ClearPointerSelection(pointer.PointerEventData);
                        pointer.MarkForDeletion();
                        _pointersForDeletion.Add(pointer);
                    }
                    break;
            }
        }

        /// <summary>
        /// Representation of an ongoing interaction between a <see cref="PointerInteractor{TInteractor, TInteractable}"/>
        /// and the <see cref="PointerInteractable{TInteractor, TInteractable}"/>s at which it points. Instance of this
        /// can be observed and leveraged to add pointer interaction functionality no built into PointableCanvasModule,
        /// such as scrolling based on controller thumbsticks.
        /// </summary>
        public class Pointer
        {
            internal Pointer(int identifier)
            {
                Identifier = identifier;
            }

            /// <summary>
            /// The <see cref="PointerEvent.Identifier"/> of the <see cref="PointerEvent"/>s which control the state of
            /// this Pointer. Canonically, this will be the same as the <see cref="IInteractorView.Identifier"/> of the
            /// actuating interactor.
            /// </summary>
            public int Identifier { get; }

            internal PointerEventData PointerEventData { get; set; }

            /// <summary>
            /// Invoked when the Pointer has been updated, exposing the PointerEventData representing what
            /// PointableCanvasModule does and knows about this Pointer's behavior.
            /// </summary>
            /// <remarks>
            /// PointerEventData is exposed here in order to be leveraged in extending functionality, such as scrolling
            /// support. However, modifying this data is not supported and can result in unexpected behavior. Rather than
            /// modifying this data, consumers should copy relevant information to their own PointerEventData instances
            /// and use those to execute events. For similar reasons, references to this PointerEventData should not be
            /// cached off or accessed except during the WhenUpdated callback itself.
            /// </remarks>
            public event Action<PointerEventData> WhenUpdated = _ => { };

            /// <summary>
            /// Invoked when the pointer is being disposed. Observers can use this signal to clean up any references or
            /// resources they may have retained related to this Pointer.
            /// </summary>
            public event Action WhenDisposed = () => { };

            internal void InvokeWhenUpdated()
            {
                WhenUpdated(PointerEventData);
            }

            internal void InvokeWhenDisposed()
            {
                WhenDisposed();
            }
        }

        /// <summary>
        /// Pointer class that is used for state associated with IPointables that are currently
        /// tracked by any IPointableCanvases in the scene.
        /// </summary>
        private class PointerImpl : Pointer
        {
            public bool MarkedForDeletion { get; private set; }

            private Canvas _canvas;
            public Canvas Canvas => _canvas;

            private Vector3 _position;
            public Vector3 Position => _position;

            private Vector3 _targetPosition;

            private GameObject _hoveredSelectable;
            public GameObject HoveredSelectable => _hoveredSelectable;

            private bool _pressing = false;
            private bool _pressed;
            private bool _released;

            public PointerImpl(int identifier, Canvas canvas) : base(identifier)
            {
                _canvas = canvas;
                _pressed = _released = false;
            }

            public void Press()
            {
                if (_pressing) return;
                _pressing = true;
                _pressed = true;
            }
            public void Release()
            {
                if (!_pressing) return;
                _pressing = false;
                _released = true;
            }

            public void ReadAndResetPressedReleased(out bool pressed, out bool released)
            {
                pressed = _pressed;
                released = _released;
                _pressed = _released = false;
                _position = _targetPosition;
            }

            public void MarkForDeletion()
            {
                MarkedForDeletion = true;
                Release();
            }

            public void SetPosition(Vector3 position)
            {
                _targetPosition = position;
                if (!_released)
                {
                    _position = position;
                }
            }

            public void SetHoveredSelectable(GameObject hoveredSelectable)
            {
                _hoveredSelectable = hoveredSelectable;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Assert.IsNull(_instance, "There must be at most one PointableCanvasModule in the scene");
            _instance = this;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _exclusiveMode = true;
        }
#endif

        protected override void OnDestroy()
        {
            // Must unset _instance prior to calling the base.OnDestroy, otherwise error is thrown:
            //   Can't add component to object that is being destroyed.
            //   UnityEngine.EventSystems.BaseInputModule:get_input ()
            _instance = null;
            base.OnDestroy();
        }

        protected bool _started = false;

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            if (_exclusiveMode)
            {
                DisableOtherModules();
            }
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                _pointerEventCamera = gameObject.AddComponent<Camera>();
                _pointerEventCamera.nearClipPlane = 0.1f;

                // We do not need this camera to be enabled to serve this module's purposes:
                // as a dependency for Canvases and for its WorldToScreenPoint functionality
                _pointerEventCamera.enabled = false;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                Destroy(_pointerEventCamera);
                _pointerEventCamera = null;
            }

            base.OnDisable();
        }

        private void DisableOtherModules()
        {
            GetComponents(_inputModules);
            foreach (var module in _inputModules)
            {
                if (module != this && module.enabled)
                {
                    module.enabled = false;
                    Debug.Log($"PointableCanvasModule: Disabling {module.GetType().Name}.");
                }
            }
        }

        /// <summary>
        /// This is an internal API which is invoked to update the PointableCanvasModule. This overrides the UpdateModule() method of
        /// Unity's BaseInputModule, from which PointableCanvasModule is descended, and should not be invoked directly.
        /// </summary>
        public override void UpdateModule()
        {
            base.UpdateModule();

            if (_exclusiveMode)
            {
                if (eventSystem.currentInputModule != null &&
                    eventSystem.currentInputModule != this)
                {
                    DisableOtherModules();
                }
            }
        }

        // Based On FindFirstRaycast
        protected static RaycastResult FindFirstRaycastWithinCanvas(List<RaycastResult> candidates, Canvas canvas)
        {
            GameObject candidateGameObject;
            Canvas candidateCanvas;
            for (var i = 0; i < candidates.Count; ++i)
            {
                candidateGameObject = candidates[i].gameObject;
                if (candidateGameObject == null) continue;

                candidateCanvas = candidateGameObject.GetComponentInParent<Canvas>();
                if (candidateCanvas == null) continue;
                if (candidateCanvas.rootCanvas != canvas) continue;

                return candidates[i];
            }
            return new RaycastResult();
        }

        private void UpdateRaycasts(PointerImpl pointer, out bool pressed, out bool released)
        {
            PointerEventData pointerEventData = pointer.PointerEventData;
            Vector2 prevPosition = pointerEventData.position;
            pointerEventData.Reset();

            Vector3 pointerPosition3D = pointer.Position;
            pointer.ReadAndResetPressedReleased(out pressed, out released);

            if (pointer.MarkedForDeletion)
            {
                pointerEventData.pointerCurrentRaycast = new RaycastResult();
                return;
            }

            Canvas canvas = pointer.Canvas;
            canvas.worldCamera = _pointerEventCamera;

            Vector3 position = Vector3.zero;
            var plane = new Plane(-1f * canvas.transform.forward, canvas.transform.position);
            var ray = new Ray(pointerPosition3D - canvas.transform.forward, canvas.transform.forward);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                position = ray.GetPoint(enter);
            }

            // We need to position our camera at an offset from the Pointer position or else
            // a graphic raycast may ignore a world canvas that's outside of our regular camera view(s)
            _pointerEventCamera.transform.position = pointerPosition3D - canvas.transform.forward;
            _pointerEventCamera.transform.LookAt(pointerPosition3D, canvas.transform.up);

            Vector2 pointerPosition2D = _pointerEventCamera.WorldToScreenPoint(position);
            pointerEventData.position = pointerPosition2D;

            // RaycastAll raycasts against with every GraphicRaycaster in the scene,
            // including nested ones like in the case of a dropdown
            eventSystem.RaycastAll(pointerEventData, _raycastResultCache);

            RaycastResult firstResult = FindFirstRaycastWithinCanvas(_raycastResultCache, canvas);
            pointer.PointerEventData.pointerCurrentRaycast = firstResult;

            _raycastResultCache.Clear();

            // We use a static translation offset from the canvas for 2D position delta tracking
            _pointerEventCamera.transform.position = canvas.transform.position - canvas.transform.forward;
            _pointerEventCamera.transform.LookAt(canvas.transform.position, canvas.transform.up);

            pointerPosition2D = _pointerEventCamera.WorldToScreenPoint(position);
            pointerEventData.position = pointerPosition2D;

            if (pressed)
            {
                pointerEventData.delta = Vector2.zero;
            }
            else
            {
                pointerEventData.delta = pointerEventData.position - prevPosition;
            }

            pointerEventData.button = PointerEventData.InputButton.Left;
        }

        /// <summary>
        /// This is an internal API which is invoked to process input. This overrides the Process() method of Unity's
        /// BaseInputModule, from which PointableCanvasModule is descended, and should not be invoked manually.
        /// </summary>
        public override void Process()
        {
            ProcessPointers(_pointersForDeletion, true);
            ProcessPointers(_pointerMap.Values, false);
        }

        private void ProcessPointers(ICollection<PointerImpl> pointers, bool clearAndReleasePointers)
        {
            // Before processing pointers, take a copy of the array since _pointersForDeletion or
            // _pointerMap may be modified if a pointer event handler adds or removes a
            // PointableCanvas.

            int pointersToProcessCount = pointers.Count;
            if (pointersToProcessCount == 0)
            {
                return;
            }

            if (pointersToProcessCount > _pointersToProcessScratch.Length)
            {
                _pointersToProcessScratch = new PointerImpl[pointersToProcessCount];
            }

            pointers.CopyTo(_pointersToProcessScratch, 0);
            if (clearAndReleasePointers)
            {
                pointers.Clear();
            }

            foreach (PointerImpl pointer in _pointersToProcessScratch)
            {
                ProcessPointer(pointer, clearAndReleasePointers);

                if (clearAndReleasePointers)
                {
                    pointer.InvokeWhenDisposed();
                }
            }
        }

        private void ProcessPointer(PointerImpl pointer, bool forceRelease = false)
        {
            bool pressed = false;
            bool released = false;
            bool wasDragging = pointer.PointerEventData.dragging;

            UpdateRaycasts(pointer, out pressed, out released);

            PointerEventData pointerEventData = pointer.PointerEventData;
            UpdatePointerEventData(pointerEventData, pressed, released);

            released |= forceRelease;

            if (!released)
            {
                ProcessMove(pointerEventData);
                ProcessDrag(pointerEventData);
            }
            else
            {
                HandlePointerExitAndEnter(pointerEventData, null);
                RemovePointerData(pointerEventData);
            }

            HandleSelectableHover(pointer, wasDragging);
            HandleSelectablePress(pointer, pressed, released, wasDragging);

            pointer.InvokeWhenUpdated();
        }

        private void HandleSelectableHover(PointerImpl pointer, bool wasDragging)
        {
            bool dragging = pointer.PointerEventData.dragging || wasDragging;

            GameObject currentOverGo = pointer.PointerEventData.pointerCurrentRaycast.gameObject;
            GameObject prevHoveredSelectable = pointer.HoveredSelectable;
            GameObject newHoveredSelectable = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            pointer.SetHoveredSelectable(newHoveredSelectable);

            if (newHoveredSelectable != null && newHoveredSelectable != prevHoveredSelectable)
            {
                WhenSelectableHovered?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging, pointer.Identifier));
            }
            else if (prevHoveredSelectable != null && newHoveredSelectable == null)
            {
                WhenSelectableUnhovered?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging, pointer.Identifier));
            }
        }

        private void HandleSelectablePress(PointerImpl pointer, bool pressed, bool released, bool wasDragging)
        {
            bool dragging = pointer.PointerEventData.dragging || wasDragging;

            if (pressed)
            {
                WhenSelected?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging, pointer.Identifier));
            }
            else if (released && !pointer.MarkedForDeletion)
            {
                // Unity handles UI selection on release, so we verify the hovered element has been selected
                bool hasSelectedHoveredObject = pointer.HoveredSelectable != null &&
                                                pointer.HoveredSelectable == pointer.PointerEventData.selectedObject;
                GameObject selectedObject = hasSelectedHoveredObject ? pointer.HoveredSelectable : null;
                WhenUnselected?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, selectedObject, dragging, pointer.Identifier));
            }
        }

        /// <summary>
        /// This method is based on ProcessTouchPoint in StandaloneInputModule,
        /// but is instead used for Pointer events
        /// </summary>
        protected void UpdatePointerEventData(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

            }

            // PointerUp notification
            if (released)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                }

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }
        }

        /// <summary>
        /// Override of PointerInputModule's ProcessDrag to allow using the initial press position for drag begin.
        /// Set _useInitialPressPositionForDrag to false if you prefer the default behaviour of PointerInputModule.
        /// </summary>
        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            if (!pointerEvent.IsPointerMoving() ||
                pointerEvent.pointerDrag == null)
                return;

            if (!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position,
                    eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                if (_useInitialPressPositionForDrag)
                {
                    pointerEvent.position = pointerEvent.pressPosition;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent,
                    ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    ClearPointerSelection(pointerEvent);
                }

                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent,
                    ExecuteEvents.dragHandler);
            }
        }

        private void ClearPointerSelection(PointerEventData pointerEvent)
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent,
                ExecuteEvents.pointerUpHandler);

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;
        }

        /// <summary>
        /// Used in PointerInputModule's ProcessDrag implementation. Brought into this subclass with a protected
        /// signature (as opposed to the parent's private signature) to be used in this subclass's overridden ProcessDrag.
        /// </summary>
        protected static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }
    }
}
