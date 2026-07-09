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

namespace Oculus.Interaction
{
    /// <summary>
    /// PointableCanvas allows any <see cref="IPointable"/> to forward its events onto an associated Unity Canvas via the
    /// <see cref="IPointableCanvas"/> interface. Requires a <see cref="PointableCanvasModule"/> to be instantiated in the scene.
    /// </summary>
    /// <remarks>
    /// Forwarding <see cref="PointerEvent"/>s to Unity Canvases allows conventional 2D Unity UIs to be used in VR with any
    /// <see cref="PointerInteractor{TInteractor, TInteractable}"/> (<see cref="RayInteractor"/>, <see cref="PokeInteractor"/>, etc.).
    /// [The online documentation](https://developer.oculus.com/documentation/unity/unity-isdk-create-ui/) contains overviews of how
    /// to do this.
    /// </remarks>
    public class PointableCanvas : PointableElement, IPointableCanvas
    {
        [Tooltip("PointerEvents will be forwarded to this Unity Canvas.")]
        [SerializeField]
        private Canvas _canvas;

        /// <summary>
        /// Implementation of <see cref="IPointableCanvas.Canvas"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public Canvas Canvas => _canvas;

        private bool _registered = false;


        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Canvas, nameof(Canvas));
            this.AssertIsTrue(Canvas.TryGetComponent(out GraphicRaycaster raycaster),
                $"{nameof(PointableCanvas)} requires that the {nameof(Canvas)} object has an attached GraphicRaycaster.");


            this.EndStart(ref _started);
        }

        private void Register()
        {
            PointableCanvasModule.RegisterPointableCanvas(this);
            _registered = true;
        }

        private void Unregister()
        {
            if (!_registered) return;
            PointableCanvasModule.UnregisterPointableCanvas(this);
            _registered = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                Register();
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                Unregister();
            }
            base.OnDisable();
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated PointableCanvas; because only a Unity Canvas is required,
        /// this simply wraps <see cref="InjectCanvas(Canvas)"/>. This method exists to support Interaction SDK's dependency injection
        /// pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllPointableCanvas(Canvas canvas)
        {
            InjectCanvas(canvas);
        }

        /// <summary>
        /// Sets the Unity <see cref="Canvas"/> for a dynamically instantiated PointableCanvas. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectCanvas(Canvas canvas)
        {
            _canvas = canvas;
        }

        #endregion

    }
}
