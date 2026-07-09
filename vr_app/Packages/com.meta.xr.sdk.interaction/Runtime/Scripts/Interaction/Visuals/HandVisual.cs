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

using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Renders an <see cref="IHand"/>. This component drives a transform hierarchy corresponding
    /// to the bone positions of a Hand skeleton, and toggles the Mesh Renderer of the hand model.
    /// </summary>
    public class HandVisual : MonoBehaviour, IHandVisual
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// Implementation of <see cref="IHandVisual.Hand"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public IHand Hand { get; private set; }

        [SerializeField]
        private bool _updateRootPose = true;

        [SerializeField]
        private bool _updateRootScale = true;

        [SerializeField]
        private bool _updateVisibility = true;

#if ISDK_OPENXR_HAND
        [HideInInspector]
#endif
        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

#if ISDK_OPENXR_HAND
        [HideInInspector]
#endif
        [SerializeField, Optional]
        private Transform _root = null;

#if ISDK_OPENXR_HAND
        [HideInInspector]
#endif
        [SerializeField, Optional]
        private MaterialPropertyBlockEditor _handMaterialPropertyBlockEditor;

        [HideInInspector]
        [SerializeField]
        private List<Transform> _jointTransforms = new List<Transform>();

#if !ISDK_OPENXR_HAND
        [HideInInspector]
#endif
        [SerializeField]
        private SkinnedMeshRenderer _openXRSkinnedMeshRenderer;

#if !ISDK_OPENXR_HAND
        [HideInInspector]
#endif
        [SerializeField, Optional]
        private Transform _openXRRoot = null;

#if !ISDK_OPENXR_HAND
        [HideInInspector]
#endif
        [SerializeField, Optional]
        private MaterialPropertyBlockEditor _openXRHandMaterialPropertyBlockEditor;

        [HideInInspector]
        [SerializeField]
        private List<Transform> _openXRJointTransforms = new List<Transform>();

        /// <summary>
        /// Implementation of <see cref="IHandVisual.WhenHandVisualUpdated"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public event Action WhenHandVisualUpdated = delegate { };

        /// <summary>
        /// Implementation of <see cref="IHandVisual.IsVisible"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool IsVisible => SkinnedMeshRenderer != null && SkinnedMeshRenderer.enabled;

        private int _wristScalePropertyId;

        /// <summary>
        /// List of transforms corresponding to the hand joint poses from <see cref="Hand"/>.
        /// Specific joints in this list can be accessed by using the integer value of
        /// <see cref="HandJointId"/> as the index.
        /// </summary>
        public IList<Transform> Joints
        {
#if ISDK_OPENXR_HAND
            get => _openXRJointTransforms;
#else
            get => _jointTransforms;
#endif
        }

        /// <summary>
        /// The root of the transform hierarchy corresponding to
        /// the joint poses from <see cref="Hand"/>.
        /// </summary>
        public Transform Root
        {
#if ISDK_OPENXR_HAND
            get => _openXRRoot;
            private set => _openXRRoot = value;
#else
            get => _root;
            private set => _root = value;
#endif
        }

        private SkinnedMeshRenderer SkinnedMeshRenderer
        {
#if ISDK_OPENXR_HAND
            get => _openXRSkinnedMeshRenderer;
            set => _openXRSkinnedMeshRenderer = value;
#else
            get => _skinnedMeshRenderer;
            set => _skinnedMeshRenderer = value;
#endif
        }

        private MaterialPropertyBlockEditor HandMaterialPropertyBlockEditor
        {
#if ISDK_OPENXR_HAND
            get => _openXRHandMaterialPropertyBlockEditor;
            set => _openXRHandMaterialPropertyBlockEditor = value;
#else
            get => _handMaterialPropertyBlockEditor;
            set => _handMaterialPropertyBlockEditor = value;
#endif
        }

        private bool _forceOffVisibility;

        /// <summary>
        /// Implementation of <see cref="IHandVisual.ForceOffVisibility"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public bool ForceOffVisibility
        {
            get
            {
                return _forceOffVisibility;
            }
            set
            {
                _forceOffVisibility = value;
                if (_started)
                {
                    UpdateVisibility();
                }
            }
        }

        private bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
            if (Root == null && Joints.Count > 0 && Joints[0] != null)
            {
                Root = Joints[0].parent;
            }
#if ISDK_OPENXR_HAND
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
            if (_openXRRoot != null)
            {
                _openXRRoot.gameObject.SetActive(true);
            }

#else
            if (_root != null)
            {
                _root.gameObject.SetActive(true);
            }
            if (_openXRRoot != null)
            {
                _openXRRoot.gameObject.SetActive(false);
            }
#endif
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(SkinnedMeshRenderer, nameof(SkinnedMeshRenderer));
            if (HandMaterialPropertyBlockEditor != null)
            {
                _wristScalePropertyId = Shader.PropertyToID("_WristScale");
            }
            UpdateVisibility();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += UpdateSkeleton;
                if (Hand.IsConnected)
                {
                    UpdateSkeleton();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started && _hand != null)
            {
                Hand.WhenHandUpdated -= UpdateSkeleton;
            }
        }

        private void UpdateVisibility()
        {
            if (!_updateVisibility)
            {
                return;
            }

            if (!Hand.IsTrackedDataValid)
            {
                if (IsVisible || ForceOffVisibility)
                {
                    SkinnedMeshRenderer.enabled = false;
                }
            }
            else
            {
                if (!IsVisible && !ForceOffVisibility)
                {
                    SkinnedMeshRenderer.enabled = true;
                }
                else if (IsVisible && ForceOffVisibility)
                {
                    SkinnedMeshRenderer.enabled = false;
                }
            }
        }

        /// <summary>
        /// This method updates the skeleton transform hierarchy and renderer visibility.
        /// This method is specific to the <see cref="HandVisual"/> component and
        /// should not be called directly.
        /// </summary>
        public void UpdateSkeleton()
        {
            UpdateVisibility();
            if (!Hand.IsTrackedDataValid)
            {
                WhenHandVisualUpdated.Invoke();
                return;
            }

            if (_updateRootPose)
            {
                if (Root != null && Hand.GetRootPose(out Pose handRootPose))
                {
                    Root.position = handRootPose.position;
                    Root.rotation = handRootPose.rotation;
                }
            }

            if (_updateRootScale)
            {
                if (Root != null)
                {
                    float parentScale = Root.parent != null ? Root.parent.lossyScale.x : 1f;
                    Root.localScale = Hand.Scale / parentScale * Vector3.one;
                }
            }

            if (!Hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                return;
            }
            for (var i = 0; i < Constants.NUM_HAND_JOINTS; ++i)
            {
                if (Joints[i] == null)
                {
                    continue;
                }
                Joints[i].SetPose(localJoints[i], Space.Self);
            }

            if (HandMaterialPropertyBlockEditor != null)
            {
                HandMaterialPropertyBlockEditor.MaterialPropertyBlock.SetFloat(_wristScalePropertyId, Hand.Scale);
                HandMaterialPropertyBlockEditor.UpdateMaterialPropertyBlock();
            }
            WhenHandVisualUpdated.Invoke();
        }

        /// <summary>
        /// Returns the transform that this component is driving,
        /// corresponding to a provided <see cref="HandJointId"/>.
        /// </summary>
        public Transform GetTransformByHandJointId(HandJointId handJointId)
        {
            return Joints[(int)handJointId];
        }

        /// <summary>
        /// Implementation of <see cref="IHandVisual.GetJointPose(HandJointId, Space)"/>. For detailed
        /// information, refer to the related documentation provided for that interface.
        /// </summary>
        public Pose GetJointPose(HandJointId jointId, Space space)
        {
            return GetTransformByHandJointId(jointId).GetPose(space);
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated <see cref="HandVisual"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHandSkeletonVisual(IHand hand, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            InjectHand(hand);
            InjectSkinnedMeshRenderer(skinnedMeshRenderer);
        }

        /// <summary>
        /// Sets the underlying <see cref="IHand"/> for a dynamically instantiated <see cref="HandVisual"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        /// <summary>
        /// Sets the underlying <see cref="UnityEngine.SkinnedMeshRenderer"/> for a
        /// dynamically instantiated <see cref="HandVisual"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            SkinnedMeshRenderer = skinnedMeshRenderer;
        }

        /// <summary>
        /// Sets the <see cref="_updateRootPose"/> field for a dynamically instantiated <see cref="HandVisual"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalUpdateRootPose(bool updateRootPose)
        {
            _updateRootPose = updateRootPose;
        }

        /// <summary>
        /// Sets the <see cref="_updateRootScale"/> field for a dynamically instantiated <see cref="HandVisual"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalUpdateRootScale(bool updateRootScale)
        {
            _updateRootScale = updateRootScale;
        }

        /// <summary>
        /// Sets the <see cref="Root"/> property for a dynamically instantiated <see cref="HandVisual"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalRoot(Transform root)
        {
            Root = root;
        }

        /// <summary>
        /// Sets the <see cref="HandMaterialPropertyBlockEditor"/> property for a dynamically
        /// instantiated <see cref="HandVisual"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalMaterialPropertyBlockEditor(MaterialPropertyBlockEditor editor)
        {
            HandMaterialPropertyBlockEditor = editor;
        }

        #endregion
    }
}
