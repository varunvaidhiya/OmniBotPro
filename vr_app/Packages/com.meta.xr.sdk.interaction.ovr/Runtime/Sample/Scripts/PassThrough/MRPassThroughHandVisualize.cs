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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Samples
{
    /// <summary>
    /// The MRPassThroughVisulaize is used when in MR passthrough is toggled on the hand will not have
    /// an overlay of a 3d object hand overlaying the hand. When the hand overlaps a 3d object or a
    /// canvas it will then apply 3d in game hand overlay over the hand so that details are not lost
    /// or confused when interacting with 3d objects.
    /// </summary>
    public class MRPassThroughHandVisualize : MonoBehaviour
    {
        [SerializeField]
        private List<Transform> _eyeAnchors;

        [SerializeField]
        private HandVisual _handVisual;

        [Header("Raycast Properties")]
        [SerializeField]
        private LayerMask _layer;

        [SerializeField]
        private float _sphereRadius;

        [SerializeField]
        private float _castDistance;

        [Header("Material Properties")]

        [SerializeField]
        private MaterialPropertyBlockEditor[] _handMaterialPropertyBlocks;

        [SerializeField]
        private float _opacity;

        [SerializeField]
        private float _outlineOpacity;

        [SerializeField]
        private float _animationSpeed;

        private float _currentOpacity;
        private float _currentOutlineOpacity;

        private readonly int _opacityId = Shader.PropertyToID("_Opacity");
        private readonly int _outlineOpacityId = Shader.PropertyToID("_OutlineOpacity");

        private (Vector3, float) _palmTarget;
        private readonly HandJointId[] _handJointTargets = new HandJointId[]
        {
            HandJointId.HandIndex2,
            HandJointId.HandIndex3,
            HandJointId.HandThumb2,
            HandJointId.HandThumb3,
            HandJointId.HandMiddle2,
            HandJointId.HandMiddle3,
            HandJointId.HandRing2,
            HandJointId.HandRing3,
            HandJointId.HandPinky2,
            HandJointId.HandPinky3,
        };

        private Ray[] _eyeRays;
        private bool _started = false;

        private void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_handVisual, nameof(_handVisual));
            this.AssertCollectionField(_handMaterialPropertyBlocks, nameof(_handMaterialPropertyBlocks));
            this.EndStart(ref _started);

            _eyeRays = new Ray[_eyeAnchors.Count];
            _currentOpacity = _opacity;
            _currentOutlineOpacity = _outlineOpacity;

            var palmJoints = new List<Vector3>(){
            _handVisual.GetJointPose(HandJointId.HandWristRoot, Space.World).position,
            _handVisual.GetJointPose(HandJointId.HandThumb1, Space.World).position,
            _handVisual.GetJointPose(HandJointId.HandIndex1, Space.World).position,
            _handVisual.GetJointPose(HandJointId.HandMiddle1, Space.World).position,
            _handVisual.GetJointPose(HandJointId.HandRing1, Space.World).position,
            _handVisual.GetJointPose(HandJointId.HandPinky1, Space.World).position,
        };

            var palmCenter = Vector3.zero;
            foreach (var origin in palmJoints)
            {
                palmCenter += origin;
            }
            palmCenter *= (1.0f / (float)palmJoints.Count);
            var WristTransform = _handVisual.GetTransformByHandJointId(HandJointId.HandWristRoot);
            var palmCenterWrist = WristTransform.InverseTransformPoint(palmCenter);

            var maxDistance = 0.0f;
            foreach (var origin in palmJoints)
            {
                maxDistance = Mathf.Max(maxDistance, Vector3.Distance(palmCenter, origin));
            }

            _palmTarget = (palmCenterWrist, maxDistance * 0.65f);
        }

        private bool SphereCast(Vector3 target, float radius)
        {
            for (int i = 0; i < _eyeAnchors.Count; i++)
            {
                var AnchorPosition = _eyeAnchors[i].position;
                var AnchorDirection = (target - AnchorPosition).normalized;
                _eyeRays[i] = new Ray(AnchorPosition, AnchorDirection);
            }
            foreach (var ray in _eyeRays)
            {
                if (Physics.SphereCast(ray, radius, _castDistance, _layer))
                {
                    return true;
                }
            }
            return false;
        }

        private bool SphereCastAllTargets()
        {
            var WristTransform = _handVisual.GetTransformByHandJointId(HandJointId.HandWristRoot);
            var PalmCenter = WristTransform.TransformPoint(_palmTarget.Item1);
            if (SphereCast(PalmCenter, _palmTarget.Item2))
            {
                return true;
            }
            foreach (var joint in _handJointTargets)
            {
                var pose = _handVisual.GetJointPose(joint, Space.World);
                if (SphereCast(pose.position, _sphereRadius))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateMaterialPropertyBlock(bool sphereCastHit)
        {
            var targetOpacity = sphereCastHit ? _opacity : 0.0f;
            var targetOutlineOpacity = sphereCastHit ? _outlineOpacity : 0.0f;
            var animParam = _animationSpeed * Time.deltaTime;

            _currentOpacity = Mathf.Lerp(_currentOpacity, targetOpacity, animParam);
            _currentOutlineOpacity = Mathf.Lerp(_currentOutlineOpacity, targetOutlineOpacity, animParam);

            foreach (var handMaterialPropertyBlock in _handMaterialPropertyBlocks)
            {
                handMaterialPropertyBlock.MaterialPropertyBlock.SetFloat(_opacityId, _currentOpacity);
                handMaterialPropertyBlock.MaterialPropertyBlock.SetFloat(_outlineOpacityId, _currentOutlineOpacity);
            }
        }

        private void Update()
        {
            if (MRPassthrough.PassThrough.IsPassThroughOn)
            {
                if (_eyeAnchors == null || _handVisual == null)
                {
                    return;
                }
                UpdateMaterialPropertyBlock(SphereCastAllTargets());
            }
            else
            {
                foreach (var handMaterialPropertyBlock in _handMaterialPropertyBlocks)
                {
                    handMaterialPropertyBlock.MaterialPropertyBlock.SetFloat(_opacityId, _opacity);
                    handMaterialPropertyBlock.MaterialPropertyBlock.SetFloat(_outlineOpacityId, _outlineOpacity);
                }
            }
        }
    }
}
