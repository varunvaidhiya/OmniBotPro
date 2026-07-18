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

using static Oculus.Interaction.TransformerUtils;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that moves the target in a 1-1 fashion with the GrabPoint.
    /// Updates transform the target in such a way as to maintain the target's
    /// local positional and rotational offsets from the GrabPoint.
    /// </summary>
    [Obsolete("Use " + nameof(GrabFreeTransformer) + " instead")]
    public class OneGrabFreeTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField]
        private PositionConstraints _positionConstraints =
            new PositionConstraints()
            {
                XAxis = new ConstrainedAxis(),
                YAxis = new ConstrainedAxis(),
                ZAxis = new ConstrainedAxis()
            };

        [SerializeField]
        private RotationConstraints _rotationConstraints =
            new RotationConstraints()
            {
                XAxis = new ConstrainedAxis(),
                YAxis = new ConstrainedAxis(),
                ZAxis = new ConstrainedAxis()
            };


        private IGrabbable _grabbable;
        private Pose _grabDeltaInLocalSpace;
        private PositionConstraints _parentConstraints;

        private Pose _localToTarget;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
            Vector3 initialPosition = _grabbable.Transform.localPosition;
            _parentConstraints = GenerateParentConstraints(_positionConstraints, initialPosition);
        }

        public void BeginTransform()
        {
            var grabPose = _grabbable.GrabPoints[0];
            Transform target = _grabbable.Transform;
            _localToTarget = WorldToLocalPose(grabPose, target.worldToLocalMatrix);
        }

        public void UpdateTransform()
        {
            Transform target = _grabbable.Transform;
            var grabPose = _grabbable.GrabPoints[0];
            Pose result = AlignLocalToWorldPose(target.localToWorldMatrix, _localToTarget, grabPose);

            target.rotation = GetConstrainedTransformRotation(result.rotation, _rotationConstraints);
            target.position = GetConstrainedTransformPosition(result.position, _parentConstraints, target.parent);
        }

        public void EndTransform() { }
    }
}
