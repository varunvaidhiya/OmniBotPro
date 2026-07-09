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
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// This component tracks the distance between two hand joints and reports
    /// <see cref="IActiveState.Active"/> when distance is under a provided threshold.
    /// </summary>
    public class JointDistanceActiveState : MonoBehaviour, IActiveState
    {
        [Tooltip("The IHand that JointIdA will be sourced from.")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _handA;
        private IHand HandA;

        [Tooltip("The joint of HandA to use for distance check.")]
        [SerializeField]
        private HandJointId _jointIdA;

        [Tooltip("The joint of HandA to use for distance check.")]
        [SerializeField]
        private HandJointId _jointA;

        public HandJointId JointIdA
        {
#if ISDK_OPENXR_HAND
            get => _jointA;
            set => _jointA = value;
#else
            get => _jointIdA;
            set => _jointIdA = value;
#endif
        }

        [Tooltip("The IHand that JointIdB will be sourced from.")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _handB;
        private IHand HandB;

        [Tooltip("The joint of HandB to use for distance check.")]
        [SerializeField]
        private HandJointId _jointIdB;

        [Tooltip("The joint of HandB to use for distance check.")]
        [SerializeField]
        private HandJointId _jointB;

        public HandJointId JointIdB
        {
#if ISDK_OPENXR_HAND
            get => _jointB;
            set => _jointB = value;
#else
            get => _jointIdB;
            set => _jointIdB = value;
#endif
        }

        [Tooltip("The ActiveState will become Active when joints are " +
            "within this distance from each other.")]
        [SerializeField]
        private float _distance = 0.05f;

        [Tooltip("The distance value will be modified by this width " +
            "to create differing enter/exit thresholds. Used to prevent " +
            "chattering at the threshold edge.")]
        [SerializeField]
        private float _thresholdWidth = 0.02f;

        [Tooltip("A new state must be maintaned for at least this " +
            "many seconds before the Active property changes.")]
        [SerializeField]
        private float _minTimeInState = 0.05f;

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    return false;
                }

                UpdateActiveState();
                return _activeState;
            }
        }

        private bool _activeState = false;
        private bool _internalState = false;
        private float _lastStateChangeTime = 0f;
        private int _lastStateUpdateFrame = 0;

        protected virtual void Awake()
        {
            if (HandA == null)
            {
                HandA = _handA as IHand;
            }
            if (HandB == null)
            {
                HandB = _handB as IHand;
            }
        }

        protected virtual void Start()
        {
            this.AssertField(HandA, nameof(HandA));
            this.AssertField(HandB, nameof(HandB));
        }

        protected virtual void Update()
        {
            UpdateActiveState();
        }

        private void UpdateActiveState()
        {
            if (Time.frameCount <= _lastStateUpdateFrame)
            {
                return;
            }
            _lastStateUpdateFrame = Time.frameCount;

            bool newState = JointDistanceWithinThreshold();
            if (newState != _internalState)
            {
                _internalState = newState;
                _lastStateChangeTime = Time.unscaledTime;
            }

            if (Time.unscaledTime - _lastStateChangeTime >= _minTimeInState)
            {
                _activeState = _internalState;
            }
        }

        private bool JointDistanceWithinThreshold()
        {
            if (HandA.GetJointPose(JointIdA, out Pose poseA) &&
                HandB.GetJointPose(JointIdB, out Pose poseB))
            {
                float threshold = _internalState ?
                                  _distance + _thresholdWidth * 0.5f :
                                  _distance - _thresholdWidth * 0.5f;

                return Vector3.Distance(poseA.position, poseB.position) <= threshold;
            }
            else
            {
                return false;
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _distance = Mathf.Max(_distance, 0f);
            _minTimeInState = Mathf.Max(_minTimeInState, 0f);
            _thresholdWidth = Mathf.Max(_thresholdWidth, 0f);
        }
#endif

        #region Inject
        public void InjectAllJointDistanceActiveState(IHand handA, IHand handB)
        {
            InjectHandA(handA);
            InjectHandB(handB);
        }

        public void InjectHandA(IHand handA)
        {
            _handA = handA as UnityEngine.Object;
            HandA = handA;
        }

        [Obsolete("Use the " + nameof(JointIdA) + " setter instead")]
        public void InjectJointIdA(HandJointId jointIdA)
        {
            JointIdA = jointIdA;
        }

        public void InjectHandB(IHand handB)
        {
            _handB = handB as UnityEngine.Object;
            HandB = handB;
        }

        [Obsolete("Use the " + nameof(JointIdB) + " setter instead")]
        public void InjectJointIdB(HandJointId jointIdB)
        {
            JointIdB = jointIdB;
        }

        public void InjectOptionalDistance(float val)
        {
            _distance = val;
        }

        public void InjectOptionalThresholdWidth(float val)
        {
            _thresholdWidth = val;
        }

        public void InjectOptionalMinTimeInState(float val)
        {
            _minTimeInState = val;
        }
        #endregion
    }
}
