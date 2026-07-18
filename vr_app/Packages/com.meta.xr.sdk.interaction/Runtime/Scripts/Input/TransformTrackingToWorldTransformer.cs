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
using UnityEngine;

namespace Oculus.Interaction
{
    public class TransformTrackingToWorldTransformer : MonoBehaviour, ITrackingToWorldTransformer
    {
        [SerializeField]
        private Transform TrackingSpace;
        public Transform Transform { get => TrackingSpace; }

        public Pose ToWorldPose(Pose pose)
        {
            Transform trackingToWorldSpace = Transform;
            pose.position = trackingToWorldSpace.TransformPoint(pose.position);
            pose.rotation = trackingToWorldSpace.rotation * pose.rotation;
            return pose;
        }

        public Pose ToTrackingPose(in Pose worldPose)
        {
            Transform trackingToWorldSpace = Transform;
            Vector3 position = trackingToWorldSpace.InverseTransformPoint(worldPose.position);
            Quaternion rotation = Quaternion.Inverse(trackingToWorldSpace.rotation) * worldPose.rotation;
            return new Pose(position, rotation);
        }

        public Quaternion WorldToTrackingWristJointFixup { get; } = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);
    }
}
