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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// This Synthetic Controller will stay attached to a Synthetic Hand when this one
    /// is limited by an interaction such as Poke.
    /// </summary>
    public class SyntheticControllerInHand : Controller
    {
        /// <summary>
        /// The actual tracking hand, used to measure the offset to the real controller
        /// </summary>
        [SerializeField, Interface(typeof(IHand)), Optional]
        private UnityEngine.Object _rawHand;
        private IHand RawHand { get; set; }

        /// <summary>
        /// The synthetic hand, used to attach the synthetic controller to it
        /// </summary>
        [SerializeField, Interface(typeof(IHand)), Optional]
        private UnityEngine.Object _syntheticHand;
        private IHand SyntheticHand { get; set; }

        private Pose _handToController = Pose.identity;
        private Pose _rootToPointer = Pose.identity;

        protected virtual void Awake()
        {
            if (RawHand == null)
            {
                RawHand = _rawHand as IHand;
            }
            if (SyntheticHand == null)
            {
                SyntheticHand = _syntheticHand as IHand;
            }
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            if (_rawHand != null)
            {
                this.AssertField(RawHand, nameof(RawHand));
            }
            if (_syntheticHand != null)
            {
                this.AssertField(SyntheticHand, nameof(SyntheticHand));
            }
            this.EndStart(ref _started);
        }

        protected override void LateUpdate()
        {
            if (_applyModifier)
            {
                UpdateOffsets(ModifyDataFromSource.GetData());
            }
            base.LateUpdate();
        }

        protected override void Apply(ControllerDataAsset data)
        {
            ApplyOffsets(data);
        }

        private void UpdateOffsets(ControllerDataAsset data)
        {
            if (TryGetTrackingRoot(RawHand, data, out Pose root))
            {
                _handToController = PoseUtils.Delta(root, data.RootPose);
                _rootToPointer = PoseUtils.Delta(data.RootPose, data.PointerPose);
            }
        }

        private void ApplyOffsets(ControllerDataAsset data)
        {
            if (TryGetTrackingRoot(SyntheticHand, data, out Pose root))
            {
                PoseUtils.Multiply(root, _handToController, ref data.RootPose);
                PoseUtils.Multiply(data.RootPose, _rootToPointer, ref data.PointerPose);
            }
        }

        private bool TryGetTrackingRoot(IHand hand, ControllerDataAsset controller, out Pose root)
        {
            if (hand != null
               && hand.GetRootPose(out root))
            {
                ITrackingToWorldTransformer transformer = controller.Config.TrackingToWorldTransformer;
                if (transformer != null)
                {
                    root = transformer.ToTrackingPose(root);
                }
                return true;
            }
            root = Pose.identity;
            return false;
        }

        #region Inject

        public void InjectAllSyntheticControllerInHand(UpdateModeFlags updateMode, IDataSource updateAfter,
            IDataSource<ControllerDataAsset> modifyDataFromSource, bool applyModifier)
        {
            base.InjectAllController(updateMode, updateAfter, modifyDataFromSource, applyModifier);
        }

        public void InjectOptionalRawHand(IHand rawHand)
        {
            _rawHand = rawHand as UnityEngine.Object;
            RawHand = rawHand;
        }

        public void InjectOptionalSyntheticHand(IHand syntheticHand)
        {
            _syntheticHand = syntheticHand as UnityEngine.Object;
            SyntheticHand = syntheticHand;
        }

        #endregion
    }
}
