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
    public class RayInteractorCursorVisual : MonoBehaviour
    {
        [SerializeField]
        private RayInteractor _rayInteractor;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private Color _hoverColor = Color.black;

        [SerializeField]
        private Color _selectColor = Color.black;

        [SerializeField]
        private Color _outlineColor = Color.black;

        [SerializeField, Range(0f, 0.5f)]
        private float _hoverSize = 0.48f;

        [SerializeField, Range(0f, 0.5f)]
        private float _selectSize = 0.4f;

        [SerializeField]
        private float _offsetAlongNormal = 0.005f;

        [Tooltip("Players head transform, used to maintain the same cursor size on screen as it is moved in the scene.")]
        [SerializeField, Optional]
        private Transform _playerHead;
        private Vector3 _startScale;

        #region Properties

        public Transform PlayerHead
        {
            get
            {
                return _playerHead;
            }
            set
            {
                _playerHead = value;
                if (_started && value is null)
                {
                    this.transform.localScale = _startScale;
                }
            }
        }

        public Color HoverColor
        {
            get
            {
                return _hoverColor;
            }
            set
            {
                _hoverColor = value;
            }
        }

        public Color SelectColor
        {
            get
            {
                return _selectColor;
            }
            set
            {
                _selectColor = value;
            }
        }

        public Color OutlineColor
        {
            get
            {
                return _outlineColor;
            }
            set
            {
                _outlineColor = value;
            }
        }

        public float HoverSize
        {
            get
            {
                return _hoverSize;
            }
            set
            {
                _hoverSize = value;
            }
        }

        public float SelectSize
        {
            get
            {
                return _selectSize;
            }
            set
            {
                _selectSize = value;
            }
        }

        public float OffsetAlongNormal
        {
            get
            {
                return _offsetAlongNormal;
            }
            set
            {
                _offsetAlongNormal = value;
            }
        }

        #endregion

        private int _shaderInnerColor = Shader.PropertyToID("_Color");
        private int _shaderOutlineColor = Shader.PropertyToID("_OutlineColor");
        private int _shaderRadius = Shader.PropertyToID("_Radius");

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_rayInteractor, nameof(_rayInteractor));
            this.AssertField(_renderer, nameof(_renderer));
            UpdateVisual();
            _startScale = transform.localScale;
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed += UpdateVisual;
                _rayInteractor.WhenStateChanged += UpdateVisualState;
                UpdateVisual();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed -= UpdateVisual;
                _rayInteractor.WhenStateChanged -= UpdateVisualState;
            }
        }

        private void UpdateVisual()
        {
            if (_rayInteractor.State == InteractorState.Disabled)
            {
                if (_renderer.enabled) _renderer.enabled = false;
                return;
            }

            if (_rayInteractor.CollisionInfo == null)
            {
                _renderer.enabled = false;
                return;
            }

            if (!_renderer.enabled)
            {
                _renderer.enabled = true;
            }

            Vector3 collisionNormal = _rayInteractor.CollisionInfo.Value.Normal;
            this.transform.position = _rayInteractor.End + collisionNormal * _offsetAlongNormal;
            this.transform.rotation = Quaternion.LookRotation(_rayInteractor.CollisionInfo.Value.Normal, Vector3.up);

            if (PlayerHead != null)
            {
                float distance = Vector3.Distance(this.transform.position, PlayerHead.position);
                this.transform.localScale = _startScale * distance;
            }

            bool isSelecting = _rayInteractor.State == InteractorState.Select;

            _renderer.material.SetFloat(_shaderRadius, isSelecting ? _selectSize : _hoverSize);
            _renderer.material.SetColor(_shaderInnerColor, isSelecting ? _selectColor : _hoverColor);
            _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }

        private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

        #region Inject

        public void InjectAllRayInteractorCursorVisual(RayInteractor rayInteractor,
            Renderer renderer)
        {
            InjectRayInteractor(rayInteractor);
            InjectRenderer(renderer);
        }

        public void InjectRayInteractor(RayInteractor rayInteractor)
        {
            _rayInteractor = rayInteractor;
        }

        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        #endregion
    }
}
