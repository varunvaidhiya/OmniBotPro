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
using System;
using Oculus.Interaction.Input;

namespace Oculus.Interaction
{
    public class HandDebugGizmos : SkeletonDebugGizmos, IHandVisual
    {
        public enum CoordSpace
        {
            World,
            Local,
        }

        [Tooltip("The IHand that will drive the visuals.")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        /// <summary>
        /// The coordinate space in which to draw the skeleton. World space draws the skeleton at the world Body location.
        /// Local draws the skeleton relative to this transform's position, and can be placed, scaled, or mirrored as desired.
        /// </summary>
        [Tooltip("The coordinate space in which to draw the skeleton. " +
            "World space draws the skeleton at the world Body location. " +
            "Local draws the skeleton relative to this transform's position, and can be placed, scaled, or mirrored as desired.")]
        [SerializeField]
        private CoordSpace _space = CoordSpace.World;

        public CoordSpace Space
        {
            get => _space;
            set => _space = value;
        }

        public bool ForceOffVisibility { get; set; }
        public bool IsVisible => _isVisible;

        public event Action WhenHandVisualUpdated = delegate { };

        private bool _isVisible = false;
        protected bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandUpdated;
                if (Hand.IsConnected)
                {
                    HandleHandUpdated();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;
            }
        }

        public Pose GetJointPose(HandJointId jointId, UnityEngine.Space space)
        {
            if (space == UnityEngine.Space.Self)
            {
                if (Hand.GetJointPoseLocal(jointId, out Pose pose))
                {
                    return pose;
                }
            }
            else if (space == UnityEngine.Space.World)
            {
                if (Hand.GetJointPose(jointId, out Pose pose))
                {
                    return pose;
                }
            }
            return new Pose();
        }

        private void HandleHandUpdated()
        {
            _isVisible = Hand.IsTrackedDataValid && !ForceOffVisibility;
            if (_isVisible)
            {
                for (var i = HandJointId.HandStart; i < HandJointId.HandEnd; ++i)
                {
                    Draw((int)i, Visibility);
                }
            }
            WhenHandVisualUpdated.Invoke();
        }

        protected override bool TryGetParentJointId(int jointId, out int parent)
        {
            if (jointId >= HandJointUtils.JointParentList.Length)
            {
                parent = (int)HandJointId.Invalid;
                return false;
            }
            parent = (int)HandJointUtils.JointParentList[jointId];
            return parent > (int)HandJointId.Invalid;
        }

        protected override bool TryGetJointPose(int jointId, out Pose pose)
        {
            bool result;
            switch (_space)
            {
                default:
                case CoordSpace.World:
                    result = Hand.GetJointPose((HandJointId)jointId, out pose);
                    break;
                case CoordSpace.Local:
                    result = Hand.GetJointPoseFromWrist((HandJointId)jointId, out pose);
                    pose.position = transform.TransformPoint(pose.position);
                    pose.rotation = transform.rotation * pose.rotation;
                    break;
            }
            return result;
        }

        #region Inject

        public void InjectAllHandDebugGizmos(IHand hand)
        {
            InjectHand(hand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        #endregion
    }

    [Obsolete("Use HandDebugGizmos instead.")]
    public class HandDebugVisual : HandDebugGizmos
    {
        [Obsolete("This method has been deprecated.", true)]
        public void UpdateSkeleton() => throw new System.NotImplementedException();
    }
}
