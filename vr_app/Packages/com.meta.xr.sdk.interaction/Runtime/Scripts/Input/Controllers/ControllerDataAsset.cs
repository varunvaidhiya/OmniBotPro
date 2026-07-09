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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// The data asset used by <see cref="DataModifier{ControllerDataAsset}"/> to pipe
    /// data through the <see cref="DataModifier{TData}"/> stack,
    /// and contains controller state data.
    /// </summary>
    [Serializable]
    public class ControllerDataAsset : ICopyFrom<ControllerDataAsset>
    {
        /// <summary>
        /// Is the data in this asset considered valid.
        /// </summary>
        public bool IsDataValid;

        /// <summary>
        /// Is the controller connected to the headset.
        /// </summary>
        public bool IsConnected;

        /// <summary>
        /// Is the controller being tracked by the headset.
        /// </summary>
        public bool IsTracked;

        /// <summary>
        /// Input state data such as button presses and axis state.
        /// </summary>
        public ControllerInput Input;

        /// <summary>
        /// The root pose of the controller, which represents the
        /// canonical controller position. See <see cref="RootPoseOrigin"/>
        /// for information on how this pose was created.
        /// </summary>
        public Pose RootPose;

        /// <summary>
        /// Information about how <see cref="RootPose"/> was created, for example
        /// whether it is the raw pose from tracking, or whether is has been
        /// filtered or modified in the <see cref="DataModifier{TData}"/> stack.
        /// </summary>
        public PoseOrigin RootPoseOrigin;

        /// <summary>
        /// The pointer pose for the controller, which is generally used as the
        /// raycast source pose. See <see cref="PointerPoseOrigin"/>
        /// for information on how this pose was created.
        /// </summary>
        public Pose PointerPose;

        /// <summary>
        /// Information about how <see cref="PointerPose"/> was created, for example
        /// whether it is the raw pose from tracking, or whether is has been
        /// filtered or modified in the <see cref="DataModifier{TData}"/> stack.
        /// </summary>
        public PoseOrigin PointerPoseOrigin;

        /// <summary>
        /// True if this controller is associated with the dominant hand,
        /// as set by the system handedness.
        /// </summary>
        public bool IsDominantHand;

        /// <summary>
        /// This data object contains configuration data that is shuttled
        /// through the <see cref="DataModifier{TData}"/> stack.
        /// </summary>
        public ControllerDataSourceConfig Config;

        /// <summary>
        /// Copies the provided <see cref="ControllerDataAsset"/> into the caller.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        public void CopyFrom(ControllerDataAsset source)
        {
            IsDataValid = source.IsDataValid;
            IsConnected = source.IsConnected;
            IsTracked = source.IsTracked;
            IsDominantHand = source.IsDominantHand;
            Config = source.Config;
            CopyPosesAndStateFrom(source);
        }

        /// <summary>
        /// Copies only pose and state data from a provided
        /// <see cref="ControllerDataAsset"/> into the caller.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        public void CopyPosesAndStateFrom(ControllerDataAsset source)
        {
            Input = source.Input;
            RootPose = source.RootPose;
            RootPoseOrigin = source.RootPoseOrigin;
            PointerPose = source.PointerPose;
            PointerPoseOrigin = source.PointerPoseOrigin;
        }
    }
}
