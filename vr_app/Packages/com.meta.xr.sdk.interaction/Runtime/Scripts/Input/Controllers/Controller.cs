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
    /// The Interaction SDK's default concrete implementation of <see cref="IController"/>. Instances of this type represent and
    /// expose the capabilities of tracked XR controllers in the physical world. Controllers are leveraged in many different ways
    /// throughout the Interaction SDK (as <see cref="IController"/>s directly, as <see cref="ISelector"/>s, as sources for
    /// <see cref="RayInteractor.Ray"/> data, etc.) and, along with <see cref="Hand"/>s, is one of the main interaction modalities
    /// supported by the SDK.
    /// </summary>
    public class Controller :
        DataModifier<ControllerDataAsset>,
        IController
    {
        /// <summary>
        /// Implementation of <see cref="IController.Handedness"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual Handedness Handedness => GetData().Config.Handedness;

        /// <summary>
        /// Implementation of <see cref="IController.IsConnected"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual bool IsConnected
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid && currentData.IsConnected;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IController.IsPoseValid"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual bool IsPoseValid
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid &&
                       currentData.RootPoseOrigin != PoseOrigin.None;
            }
        }

        /// <summary>
        /// Checks whether a valid "pointer pose" is currently available. See <see cref="IController.TryGetPointerPose(out Pose)"/>
        /// for details on what a pointer pose is.
        /// </summary>
        public virtual bool IsPointerPoseValid
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid &&
                       currentData.PointerPoseOrigin != PoseOrigin.None;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IController.ControllerInput"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual ControllerInput ControllerInput
        {
            get
            {
                var currentData = GetData();
                return currentData.Input;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IController.WhenUpdated"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual event Action WhenUpdated = delegate { };

        private ITrackingToWorldTransformer TrackingToWorldTransformer =>
            GetData().Config.TrackingToWorldTransformer;

        /// <summary>
        /// Implementation of <see cref="IController.Scale"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual float Scale => TrackingToWorldTransformer != null
            ? TrackingToWorldTransformer.Transform.lossyScale.x
            : 1;

        /// <summary>
        /// Implementation of <see cref="IController.IsButtonUsageAnyActive(ControllerButtonUsage)"/>; for details, please refer to
        /// the related documentation provided for that interface.
        /// </summary>
        public virtual bool IsButtonUsageAnyActive(ControllerButtonUsage buttonUsage)
        {
            var currentData = GetData();
            return
                currentData.IsDataValid &&
                (buttonUsage & currentData.Input.ButtonUsageMask) != 0;
        }

        /// <summary>
        /// Implementation of <see cref="IController.IsButtonUsageAllActive(ControllerButtonUsage)"/>; for details, please refer to
        /// the related documentation provided for that interface.
        /// </summary>
        public virtual bool IsButtonUsageAllActive(ControllerButtonUsage buttonUsage)
        {
            var currentData = GetData();
            return currentData.IsDataValid &&
                   (buttonUsage & currentData.Input.ButtonUsageMask) == buttonUsage;
        }

        /// <summary>
        /// Implementation of <see cref="IController.TryGetPose(out Pose)"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual bool TryGetPose(out Pose pose)
        {
            if (!IsPoseValid)
            {
                pose = Pose.identity;
                return false;
            }

            pose = GetData().Config.TrackingToWorldTransformer.ToWorldPose(GetData().RootPose);
            return true;
        }

        /// <summary>
        /// Implementation of <see cref="IController.TryGetPointerPose(out Pose)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public virtual bool TryGetPointerPose(out Pose pose)
        {
            if (!IsPointerPoseValid)
            {
                pose = Pose.identity;
                return false;
            }

            pose = GetData().Config.TrackingToWorldTransformer.ToWorldPose(GetData().PointerPose);
            return true;
        }

        /// <summary>
        /// Overrides and encapsulates <see cref="DataSource{TData}.MarkInputDataRequiresUpdate"/>, augmenting the behavior of the
        /// base type's method by invoking <see cref="WhenUpdated"/> to notify observers of the occurrence.
        /// </summary>
        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                WhenUpdated.Invoke();
            }
        }

        protected override void Apply(ControllerDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }

        #region Inject

        /// <summary>
        /// Wrapper for
        /// <see cref="DataModifier{TData}.InjectAllDataModifier(DataSource{TData}.UpdateModeFlags, IDataSource, IDataSource{TData}, bool)"/>,
        /// for injecting the required dependencies to a dynamically-allocated Controller instance. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllController(UpdateModeFlags updateMode, IDataSource updateAfter,
            IDataSource<ControllerDataAsset> modifyDataFromSource, bool applyModifier)
        {
            base.InjectAllDataModifier(updateMode, updateAfter, modifyDataFromSource, applyModifier);
        }

        #endregion
    }
}
