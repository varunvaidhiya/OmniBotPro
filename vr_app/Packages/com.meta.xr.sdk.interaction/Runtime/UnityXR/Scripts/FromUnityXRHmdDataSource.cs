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
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace Oculus.Interaction.UnityXR
{
    /// <summary>
    ///   <para>Provides HMD tracking data to Interaction SDK OpenXR via UnityXR</para>
    /// </summary>
    public class FromUnityXRHmdDataSource : DataSource<HmdDataAsset>
    {
        [Header("Shared Configuration")]
        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private UnityEngine.Object _trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer;

        private HmdDataAsset _hmdDataAsset = new HmdDataAsset();
        private HmdDataSourceConfig _config;
        protected void Awake()
        {
            if (TrackingToWorldTransformer == null)
            {
                TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            }
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(TrackingToWorldTransformer, nameof(TrackingToWorldTransformer));
            this.EndStart(ref _started);
        }

        private HmdDataSourceConfig Config
        {
            get
            {
                if (_config != null)
                {
                    return _config;
                }

                _config = new HmdDataSourceConfig()
                {
                    TrackingToWorldTransformer = TrackingToWorldTransformer
                };

                return _config;
            }
        }

        [SerializeField]
        private XROrigin _origin;

        protected override void UpdateData()
        {

            _hmdDataAsset.Config = Config;
            _hmdDataAsset.Root = _origin.Camera.transform.GetLocalPose();
            _hmdDataAsset.IsTracked = XRSettings.isDeviceActive;
            _hmdDataAsset.FrameId = Time.frameCount;
        }

        protected override HmdDataAsset DataAsset => _hmdDataAsset;

        #region Inject

        public void InjectAllFromOVRHmdDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            bool useOvrManagerEmulatedPose, ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
        }
        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            _trackingToWorldTransformer = trackingToWorldTransformer as UnityEngine.Object;
            TrackingToWorldTransformer = trackingToWorldTransformer;
        }
        #endregion
    }
}
