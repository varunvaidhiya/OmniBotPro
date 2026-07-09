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
    /// <summary>
    /// This component maintains the Passthrough mode
    /// between scenes and listen to a toggle for changes.
    /// </summary>
    public class MRPassthrough : MonoBehaviour
    {
        public static class PassThrough
        {
            public static bool IsPassThroughOn;
            public static bool IsPassThroughCompatible;
        }

        /// <summary>
        /// Objects that shouldn't be rendered during passthrough
        /// </summary>
        [Tooltip("Objects that shouldn't be rendered during passthrough")]
        [Header("Passthrough Objects To Remove")]
        [SerializeField]
        private GameObject[] _objects;

        /// <summary>
        /// These are UI objects that should be toggled ON/OFF during passthrough
        /// </summary>
        [Tooltip("These are UI objects that should be toggled ON/OFF during passthrough button")]
        [SerializeField]
        private Toggle _passThroughToggle;

        /// <summary>
        /// The OVRPassthrough Layer
        /// </summary>
        [Tooltip("The OVRPassthrough Layer")]
        [SerializeField]
        private OVRPassthroughLayer _layer;

        /// <summary>
        /// Use the CenterEyeAnchor or Center Camera
        /// </summary>
        [Tooltip("Use the CenterEyeAnchor or Center Camera")]
        [SerializeField]
        private Camera _camera;

        protected bool _started = false;

        #region Editor Callbacks
        protected virtual void Reset()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _layer = Object.FindFirstObjectByType<OVRPassthroughLayer>();
#pragma warning restore CS0618 // Type or member is obsolete
            _camera = OVRManager.FindMainCamera();
        }
        #endregion

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertCollectionItems(_objects, nameof(_objects));
            this.AssertField(_layer, nameof(_layer));
            this.AssertField(_camera, nameof(_camera));
            this.AssertField(_passThroughToggle, nameof(_passThroughToggle));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                ValidatePassthrough();
            }
        }

        private void ValidatePassthrough()
        {
            if (OVRManager.HasInsightPassthroughInitFailed())
            {
                _camera.clearFlags = CameraClearFlags.Skybox;
                _passThroughToggle.enabled = false;
            }
            else
            {
                if (PassThrough.IsPassThroughOn)
                {
                    TurnPassThroughOn();
                }
                else
                {
                    TurnPassThroughOff();
                    if (PassThrough.IsPassThroughCompatible)
                    {
                        _passThroughToggle.enabled = false;
                    }
                    else
                    {
                        _passThroughToggle.enabled = true;
                    }
                }
            }
        }

        [System.Obsolete]
        public void TurnLocoMotionSceneOn()
        {
            PassThrough.IsPassThroughCompatible = true;
        }

        [System.Obsolete]
        public void TurnLocoMotionSceneOff()
        {
            PassThrough.IsPassThroughCompatible = false;
        }

        public void TogglePassThrough()
        {
            if (PassThrough.IsPassThroughOn)
            {
                TurnPassThroughOff();
            }
            else
            {
                TurnPassThroughOn();
            }
        }

        public void CheckPassthroughToggle()
        {
            if (_passThroughToggle.enabled && PassThrough.IsPassThroughOn)
            {
                _passThroughToggle.isOn = true;
                TurnPassThroughOn();
            }
        }

        private void TurnPassThroughOn()
        {
            if (OVRManager.IsInsightPassthroughInitialized())
            {
                PassThrough.IsPassThroughOn = true;
                _layer.textureOpacity = 1;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                foreach (GameObject obj in _objects)
                {
                    obj.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("Failed to initialize Passthrough please " +
                    "check the OVRManager in the Hierarchy and check if " +
                    "Passthrough is supported and enabled.");
            }
        }

        private void TurnPassThroughOff()
        {
            PassThrough.IsPassThroughOn = false;
            _layer.textureOpacity = 0;
            _camera.clearFlags = CameraClearFlags.Skybox;
            foreach (GameObject obj in _objects)
            {
                obj.SetActive(true);
            }
        }

        #region Injects

        public void InjectAllMRPassthrough(GameObject[] objects,
            Toggle passThroughToggle, OVRPassthroughLayer layer, Camera camera)
        {
            InjectObjects(objects);
            InjectPassThroughToggle(passThroughToggle);
            InjectLayer(layer);
            InjectCamera(camera);
        }

        public void InjectObjects(GameObject[] objects)
        {
            _objects = objects;
        }

        public void InjectPassThroughToggle(Toggle passThroughToggle)
        {
            _passThroughToggle = passThroughToggle;
        }

        public void InjectLayer(OVRPassthroughLayer layer)
        {
            _layer = layer;
        }

        public void InjectCamera(Camera camera)
        {
            _camera = camera;
        }
        #endregion
    }
}
