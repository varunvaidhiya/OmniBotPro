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

namespace Oculus.Interaction.Samples
{
    /// <summary>
    /// When MR Passthrough is toggled to true this will change the current object material
    /// to a transparent one to fit the aesthetic of Passthrough
    /// </summary>
    public class MRPassThroughMaterialChanger : MonoBehaviour
    {
        [Header("Passthrough Material")]
        [Tooltip("Material that should be rendered during passthrough")]
        [SerializeField]
        private Material _passThroughMaterial;

        [Header("Current GameObject Material")]
        [SerializeField]
        private Material _material;

        [Tooltip("This current gameobject renderer")]
        [SerializeField]
        private Renderer _renderer;

        protected bool _started = false;

        protected virtual void Reset()
        {
            _renderer = gameObject.GetComponent<Renderer>();
            _material = _renderer.material;
        }

        protected virtual void Start()
        {
            if (_renderer == null)
            {
                _renderer = gameObject.GetComponent<Renderer>();
                _material = _renderer.material;
            }

            this.BeginStart(ref _started);
            this.AssertField(_passThroughMaterial, nameof(_passThroughMaterial));
            this.AssertField(_material, nameof(_material));
            this.AssertField(_renderer, nameof(_renderer));

            this.EndStart(ref _started);
        }

        private void Update()
        {
            if (_passThroughMaterial != null && MRPassthrough.PassThrough.IsPassThroughOn)
            {
                _renderer.material = _passThroughMaterial;
            }
            else
            {
                _renderer.material = _material;
            }
        }
        #region Inject
        public void InjectAllChanger(Material passthroughMaterial,
            Renderer render, Material material)
        {
            InjectPassthroughMaterial(passthroughMaterial);
            InjectRenderer(render);
            InjectMaterial(material);
        }
        public void InjectPassthroughMaterial(Material passthroughMaterial)
        {
            _passThroughMaterial = passthroughMaterial;
        }

        public void InjectRenderer(Renderer render)
        {
            _renderer = render;
        }

        public void InjectMaterial(Material material)
        {
            _material = material;
        }
        #endregion
    }
}
