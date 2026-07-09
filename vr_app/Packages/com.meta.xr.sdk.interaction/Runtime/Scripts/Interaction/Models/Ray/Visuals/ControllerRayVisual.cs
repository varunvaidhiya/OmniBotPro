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

namespace Oculus.Interaction
{
    /// <summary>
    /// A visual affordance designed to accompany <see cref="RayInteractor"/>s. This is used in most ray interaction prefabs,
    /// wizards, and example scenes provided by the Interaction SDK. Though this class includes a number of customization
    /// options and can be set up independently, you should usually start from an example (scene or prefab) rather than trying
    /// to add this visual from scratch as this type makes assumptions about certain of its dependencies, such as those added
    /// by <see cref="InjectRenderer(Renderer)"/> and <see cref="InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor)"/>.
    /// </summary>
    [Obsolete("Use " + nameof(RayInteractorRayVisual) + "")]
    public class ControllerRayVisual : MonoBehaviour
    {
        [SerializeField]
        private RayInteractor _rayInteractor;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private MaterialPropertyBlockEditor _materialPropertyBlockEditor;

        [SerializeField]
        private float _maxRayVisualLength = 0.5f;

        [SerializeField]
        private Color _hoverColor0 = Color.white;

        [SerializeField]
        private Color _hoverColor1 = Color.white;

        [SerializeField]
        private Color _selectColor0 = Color.blue;

        [SerializeField]
        private Color _selectColor1 = Color.blue;

        [SerializeField]
        private bool _hideWhenNoInteractable = false;

        #region Properties

        /// <summary>
        /// The maximum distance the visual ray is allowed to extend. Note that this has nothing to do with
        /// <see cref="RayInteractor.MaxRayLength"/>, which is usually much longer as the visual typically
        /// attenuates only part of the way to the ray's actual terminus.
        /// </summary>
        public float MaxRayVisualLength
        {
            get
            {
                return _maxRayVisualLength;
            }

            set
            {
                _maxRayVisualLength = value;
            }
        }

        /// <summary>
        /// The inner color to show when hovering.
        /// </summary>
        public Color HoverColor0
        {
            get
            {
                return _hoverColor0;
            }

            set
            {
                _hoverColor0 = value;
            }
        }

        /// <summary>
        /// The outer color to show when hovering.
        /// </summary>
        public Color HoverColor1
        {
            get
            {
                return _hoverColor1;
            }

            set
            {
                _hoverColor1 = value;
            }
        }

        /// <summary>
        /// The inner color to show when selecting. This color will supersede <see cref="HoverColor0"/>
        /// when the associated <see cref="RayInteractor"/> moves from <see cref="InteractorState.Hover"/>
        /// to <see cref="InteractorState.Select"/>.
        /// </summary>
        public Color SelectColor0
        {
            get
            {
                return _selectColor0;
            }

            set
            {
                _selectColor0 = value;
            }
        }

        /// <summary>
        /// The outer color to show when selecting. This color will supersede <see cref="HoverColor1"/>
        /// when the associated <see cref="RayInteractor"/> moves from <see cref="InteractorState.Hover"/>
        /// to <see cref="InteractorState.Select"/>.
        /// </summary>
        public Color SelectColor1
        {
            get
            {
                return _selectColor1;
            }

            set
            {
                _selectColor1 = value;
            }
        }

        #endregion

        private int _shaderColor0 = Shader.PropertyToID("_Color0");
        private int _shaderColor1 = Shader.PropertyToID("_Color1");

        private bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_rayInteractor, nameof(_rayInteractor));
            this.AssertField(_renderer, nameof(_renderer));
            this.AssertField(_materialPropertyBlockEditor, nameof(_materialPropertyBlockEditor));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed += UpdateVisual;
                _rayInteractor.WhenStateChanged += HandleStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed -= UpdateVisual;
                _rayInteractor.WhenStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_rayInteractor.State == InteractorState.Disabled ||
                (_hideWhenNoInteractable && _rayInteractor.Interactable == null))
            {
                _renderer.enabled = false;
                return;
            }

            _renderer.enabled = true;
            transform.SetPositionAndRotation(_rayInteractor.Origin, _rayInteractor.Rotation);

            transform.localScale = new Vector3(
                transform.localScale.x,
                transform.localScale.y,
                Mathf.Min(_maxRayVisualLength, (_rayInteractor.End - transform.position).magnitude));

            _materialPropertyBlockEditor.MaterialPropertyBlock.SetColor(_shaderColor0, _rayInteractor.State == InteractorState.Select ? _selectColor0 : _hoverColor0);
            _materialPropertyBlockEditor.MaterialPropertyBlock.SetColor(_shaderColor1, _rayInteractor.State == InteractorState.Select ? _selectColor1 : _hoverColor1);
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated ControllerRayVisual; effectively wraps
        /// <see cref="InjectRayInteractor(RayInteractor)"/>, <see cref="InjectRenderer(Renderer)"/>, and
        /// <see cref="InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllControllerRayVisual(RayInteractor rayInteractor,
            Renderer renderer,
            MaterialPropertyBlockEditor materialPropertyBlockEditor)
        {
            InjectRayInteractor(rayInteractor);
            InjectRenderer(renderer);
            InjectMaterialPropertyBlockEditor(materialPropertyBlockEditor);
        }

        /// <summary>
        /// Sets the <see cref="RayInteractor"/> for a dynamically instantiated ControllerRayVisual. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRayInteractor(RayInteractor rayInteractor)
        {
            _rayInteractor = rayInteractor;
        }

        /// <summary>
        /// Sets the <see cref="Renderer"/> for a dynamically instantiated ControllerRayVisual. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// Sets the <see cref="MaterialPropertyBlockEditor"/> for a dynamically instantiated ControllerRayVisual. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectMaterialPropertyBlockEditor(
            MaterialPropertyBlockEditor materialPropertyBlockEditor)
        {
            _materialPropertyBlockEditor = materialPropertyBlockEditor;
        }

        #endregion
    }
}
