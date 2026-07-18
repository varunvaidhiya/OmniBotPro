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
    /// This component draws a vignette at a cameara's near plane.
    /// The direction of the vignette can be controller, it does not need to necesarily
    /// centered and can even point backwards to draw a solid circle.
    /// </summary>
    public class TunnelingEffect : MonoBehaviour
    {
        [Header("Mask Setup")]
        /// <summary>
        /// Center eye, the effect will be rendered in its near plane
        /// </summary>
        [SerializeField]
        private Camera _centerEyeCamera;
        /// <summary>
        /// Mesh filter to generate the quad that renders the vignette
        /// </summary>
        [SerializeField]
        private MeshFilter _meshFilter;
        /// <summary>
        /// Optional. Indicates the actual center of the vignette.
        /// You need to set UseAimingTarget to use it
        /// </summary>
        [SerializeField, Optional]
        private Vector3 _aimingDirection;
        public Vector3 AimingDirection
        {
            get => _aimingDirection;
            set => _aimingDirection = value;
        }
        /// <summary>
        /// When set, AimingDirection will dictate the center of the vignette,
        /// if not it is assumed to be centered in the camera.
        /// </summary>
        [SerializeField]
        private bool _useAimingTarget;
        public bool UseAimingTarget
        {
            get => _useAimingTarget;
            set => _useAimingTarget = value;
        }
        /// <summary>
        /// Distance from the camera to draw the effect
        /// </summary>
        [Header("Mask State")]
        [SerializeField]
        private float _planeDistance;
        public float PlaneDistance
        {
            get => _planeDistance;
            set => _planeDistance = value;
        }
        /// <summary>
        /// Color of the vignette at the opposite side of the aiming direction
        /// </summary>
        [Header("Mask Properties")]
        [SerializeField]
        private Color _maskOuterColor = Color.black;
        public Color MaskOuterColor
        {
            get => _maskOuterColor;
            set => _maskOuterColor = value;
        }
        /// <summary>
        /// Color of the vignette towards the center of the vignette
        /// </summary>
        [SerializeField]
        private Color _maskInnerColor = Color.black;
        public Color MaskInnerColor
        {
            get => _maskInnerColor;
            set => _maskInnerColor = value;
        }
        /// <summary>
        /// Actual field-of-view (in degrees) that the vignette will allow.
        /// Set it to 360 to allow the player to have full visibility and 0
        /// to cover everything with a vignette
        /// </summary>
        [SerializeField, Range(0f, 360f)]
        private float _userFOV = 360f;
        public float UserFOV
        {
            get => _userFOV;
            set => _userFOV = value;
        }
        /// <summary>
        /// Feather transition, in degrees, for the vignette from solid color
        /// to full transparency.
        /// </summary>
        [SerializeField, Range(0f, 180f)]
        private float _featheredFOV = 10f;
        public float ExtraFeatheredFOV
        {
            get => _featheredFOV;
            set => _featheredFOV = value;
        }
        /// <summary>
        /// Alpha multipler for the colored areas of the vignette
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float _alphaStrength = 1f;
        public float AlphaStrength
        {
            get => _alphaStrength;
            set => _alphaStrength = value;
        }

        private readonly int _maskColorInnerID = Shader.PropertyToID("_ColorInner");
        private readonly int _maskColorOuterID = Shader.PropertyToID("_ColorOuter");
        private readonly int _maskDirectionID = Shader.PropertyToID("_Direction");
        private readonly int _minRadiusID = Shader.PropertyToID("_MinRadius");
        private readonly int _maxRadiusID = Shader.PropertyToID("_MaxRadius");
        private readonly int _alphaID = Shader.PropertyToID("_Alpha");

        private Mesh _maskMesh;
        private Transform _meshTransform;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _materialPropertyBlock;

        protected bool _started;

        private static readonly Vector3[] _vertices = new Vector3[4]
        {
            new Vector3(-1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f, -1.0f, 0.0f), new Vector3(1.0f, -1.0f, 0.0f)
        };

        private static readonly Vector3[] _uv0 = new Vector3[4]
        {
            new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f)
        };

        private static readonly int[] _triangles = new int[6]
        {
            0, 1, 3, 0, 3, 2
        };

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_centerEyeCamera, nameof(_centerEyeCamera));
            this.AssertField(_meshFilter, nameof(_meshFilter));

            _meshTransform = _meshFilter.gameObject.transform;
            _meshRenderer = _meshFilter.GetComponent<MeshRenderer>();
            _maskMesh = new Mesh();

            _maskMesh.SetVertices(_vertices);
            _maskMesh.SetTriangles(_triangles, 0);
            _maskMesh.SetUVs(0, _uv0);

            _maskMesh.name = "Tunnel";
            _meshFilter.sharedMesh = _maskMesh;
            _materialPropertyBlock = new MaterialPropertyBlock();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _meshRenderer.enabled = true;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _meshRenderer.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (_meshRenderer is null || _meshTransform is null)
            {
                return;
            }

            this.transform.SetPose(_centerEyeCamera.transform.GetPose());

            float radFov = Mathf.Deg2Rad * _centerEyeCamera.fieldOfView;
            float planeHeight = (Mathf.Tan(radFov / 2.0f) * _planeDistance) * 2.0f;
            float planeWidth = planeHeight * _centerEyeCamera.aspect;
            planeWidth += GetIPD();
            Vector2 planeSize = new Vector2(planeWidth, planeHeight);
            //Just a magic number, for some reason the quad does not properly covers the frustum.
            //The actual VR projection matrix is a bit different so the assumtion for the math above is not quite correct.
            planeSize *= 1.2f;

            _meshTransform.localPosition = new Vector3(0.0f, 0.0f, _planeDistance);
            _meshTransform.localScale = new Vector3(planeSize.x * 0.5f, planeSize.y * 0.5f, 1.0f);

            float fov = UserFOV * 0.5f * Mathf.Deg2Rad;
            float maxMask = Mathf.Cos(fov);
            float minMask = Mathf.Cos(fov - ExtraFeatheredFOV * Mathf.Deg2Rad);

            _materialPropertyBlock.SetFloat(_alphaID, _alphaStrength);
            _materialPropertyBlock.SetFloat(_minRadiusID, minMask);
            _materialPropertyBlock.SetFloat(_maxRadiusID, maxMask);
            _materialPropertyBlock.SetColor(_maskColorInnerID, _maskInnerColor);
            _materialPropertyBlock.SetColor(_maskColorOuterID, _maskOuterColor);

            Vector3 direction = _useAimingTarget ? _aimingDirection : _centerEyeCamera.transform.forward;
            _materialPropertyBlock.SetVector(_maskDirectionID, direction.normalized);

            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        private float GetIPD()
        {
            return _centerEyeCamera.stereoSeparation;

        }

        #region Inject

        public void InjectAllTunnelingEffect(Camera centerEyeCamera, MeshFilter meshFilter)
        {
            InjectCenterEyeCamera(centerEyeCamera);
            InjectMeshFilter(meshFilter);
        }

        [Obsolete("Use " + nameof(InjectAllTunnelingEffect) + " without leftEyeAnchor and rightEyeAnchor instead")]
        public void InjectAllTunnelingEffect(Transform leftEyeAnchor, Transform rightEyeAnchor,
            Camera centerEyeCamera, MeshFilter meshFilter)
        {
            InjectLeftEyeAnchor(leftEyeAnchor);
            InjectRightEyeAnchor(rightEyeAnchor);
            InjectCenterEyeCamera(centerEyeCamera);
            InjectMeshFilter(meshFilter);
        }

        [Obsolete("leftEyeAnchor has been deprecated")]
        public void InjectLeftEyeAnchor(Transform leftEyeAnchor)
        {
        }

        [Obsolete("rightEyeAnchor has been deprecated")]
        public void InjectRightEyeAnchor(Transform rightEyeAnchor)
        {
        }

        public void InjectCenterEyeCamera(Camera centerEyeCamera)
        {
            _centerEyeCamera = centerEyeCamera;
        }
        public void InjectMeshFilter(MeshFilter meshFilter)
        {
            _meshFilter = meshFilter;
        }

        #endregion
    }
}
