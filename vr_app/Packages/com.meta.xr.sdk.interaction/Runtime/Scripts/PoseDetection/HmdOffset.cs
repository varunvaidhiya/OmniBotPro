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

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Drives its <see cref="Transform"/> to match the pose provided by an <see cref="IHmd"/>,
    /// with optional constraints and added position/rotation offset.
    /// </summary>
    public class HmdOffset : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHmd))]
        private UnityEngine.Object _hmd;
        private IHmd Hmd;

        [SerializeField]
        private Vector3 _offsetTranslation = Vector3.zero;
        [SerializeField]
        private Vector3 _offsetRotation = Vector3.zero;

        [SerializeField]
        private bool _disablePitchFromSource = false;
        [SerializeField]
        private bool _disableYawFromSource = false;
        [SerializeField]
        private bool _disableRollFromSource = false;

        protected bool _started;

        protected virtual void Awake()
        {
            if (Hmd == null)
            {
                Hmd = _hmd as IHmd;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hmd, nameof(Hmd));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hmd.WhenUpdated += HandleHmdUpdated;
                HandleHmdUpdated();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hmd.WhenUpdated -= HandleHmdUpdated;
            }
        }

        protected virtual void HandleHmdUpdated()
        {
            if (!Hmd.TryGetRootPose(out Pose centerEyePose))
            {
                return;
            }

            var centerEyePosition = centerEyePose.position;
            var centerEyeRotation = centerEyePose.rotation;

            var eulerAngles = centerEyeRotation.eulerAngles;
            var pitch = Quaternion.Euler(new Vector3(eulerAngles.x, 0.0f, 0.0f));
            var yaw = Quaternion.Euler(new Vector3(0.0f, eulerAngles.y, 0.0f));
            var roll = Quaternion.Euler(new Vector3(0.0f, 0.0f, eulerAngles.z));
            var finalSourceRotation = Quaternion.identity;

            if (!_disableYawFromSource)
            {
                finalSourceRotation *= yaw;
            }
            if (!_disablePitchFromSource)
            {
                finalSourceRotation *= pitch;
            }
            if (!_disableRollFromSource)
            {
                finalSourceRotation *= roll;
            }

            var totalRotation = finalSourceRotation * Quaternion.Euler(_offsetRotation);
            transform.position = centerEyePosition +
                totalRotation * _offsetTranslation;
            transform.rotation = totalRotation;
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="HmdOffset"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHmdOffset(IHmd hmd)
        {
            InjectHmd(hmd);
        }

        /// <summary>
        /// Sets the underlying <see cref="IHmd"/> for a dynamically instantiated
        /// <see cref="HmdOffset"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHmd(IHmd hmd)
        {
            _hmd = hmd as UnityEngine.Object;
            Hmd = hmd;
        }

        /// <summary>
        /// Sets the underlying position offset for a dynamically instantiated
        /// <see cref="HmdOffset"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalOffsetTranslation(Vector3 val)
        {
            _offsetTranslation = val;
        }

        /// <summary>
        /// Sets the underlying rotation offset for a dynamically instantiated
        /// <see cref="HmdOffset"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalOffsetRotation(Vector3 val)
        {
            _offsetRotation = val;
        }

        /// <summary>
        /// Sets the underlying DisablePitchFromSource for a dynamically instantiated
        /// <see cref="HmdOffset"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalDisablePitchFromSource(bool val)
        {
            _disablePitchFromSource = val;
        }

        /// <summary>
        /// Sets the underlying DisableYawFromSource for a dynamically instantiated
        /// <see cref="HmdOffset"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalDisableYawFromSource(bool val)
        {
            _disableYawFromSource = val;
        }

        /// <summary>
        /// Sets the underlying DisableRollFromSource for a dynamically instantiated
        /// <see cref="HmdOffset"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalDisableRollFromSource(bool val)
        {
            _disableRollFromSource = val;
        }

        #endregion
    }
}
