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

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// A helper class that uses an underlying <see cref="RandomSampleConsensus{TModel}"/> to select the best pair of linear
    /// and angular velocities from a buffer of recent timed poses.
    /// </summary>
    /// <remarks>
    /// RANSACVelocity allows for the stable estimation of velocity under noise-prone circumstances. This is particuarly
    /// important when throwing objects because of the unreliability of data around the moment of intended release: if the
    /// perceived moment of release is slightly too late, for example, the behavior of the throwing implement (hand, controller,
    /// etc.) should not impact the calculated velocity. RANSAC velocity estimation across several frames mitigates this problem.
    /// </remarks>
    public class RANSACVelocity
    {
        private bool _highConfidenceStreak = false;
        private float _lastProcessedTime = 0;

        private float _maxSyntheticSpeed = 5.0f;

        /// <summary>
        /// Maximum speed (in m/s) allowed for synthetic poses. This is used in internal calculations and should generally not
        /// need to be tuned.
        /// </summary>
        /// <remarks>
        /// Synthetic poses are generated and used by RANSACVelocity to fill tracking gaps (when
        /// <see cref="Input.IHand.IsHighConfidence"/> for a tracked hand is false, for example). Capping the allowed speed
        /// mitigates the risk of generating nonsensical synthetic poses.
        /// </remarks>
        public float MaxSyntheticSpeed
        {
            get => _maxSyntheticSpeed;
            set => _maxSyntheticSpeed = Mathf.Max(_minSyntheticSpeed, value);
        }

        private const float _minSyntheticSpeed = 0.0001f;

        private RandomSampleConsensus<Vector3> _ransac;
        private RingBuffer<TimedPose> _poses;

        private RandomSampleConsensus<Vector3>.GenerateModel _velocityModel;
        private RandomSampleConsensus<Vector3>.EvaluateModelScore _velocityScorer;
        private RandomSampleConsensus<Vector3>.GenerateModel _torqueModel;
        private RandomSampleConsensus<Vector3>.EvaluateModelScore _torqueScorer;

        /// <summary>
        /// Creates a new RANSACVelocity for estimating velocity.
        /// </summary>
        /// <param name="samplesCount">The size of the rolling sample buffer from which to seek consensus.</param>
        /// <param name="samplesDeadZone">The number of most recent samples to exclude from the consensus.</param>
        public RANSACVelocity(int samplesCount = 10, int samplesDeadZone = 2)
        {
            _poses = new RingBuffer<TimedPose>(samplesCount);
            _ransac = new RandomSampleConsensus<Vector3>(samplesCount, samplesDeadZone);
        }

        /// <summary>
        /// Initializes a RANSACVelocity calculator, clearing its state and preparing it to receive new data.
        /// Can be called repeatedly to reset the state of an existing RANSACVelocity.
        /// </summary>
        public void Initialize()
        {
            _poses.Clear();
            _highConfidenceStreak = false;
        }

        /// <summary>
        /// Consumes a new frame of Pose data --- for example, from the <see cref="IGrabbable.Transform"/> of a grabbed
        /// <see cref="GrabInteractable"/>. The RANSACVelocity instance must be regularly supplied with such data so
        /// that velocities can be estimated when needed.
        /// </summary>
        /// <param name="pose">The Pose observed.</param>
        /// <param name="time">The time at which <paramref name="pose"/> was observed.</param>
        /// <param name="isHighConfidence">
        /// Whether or not <paramref name="pose"/> was observed with high confidence. For example, if <paramref name="pose"/>
        /// is from an <see cref="IGrabbable.Transform"/> being held by an <see cref="Input.IHand"/> for which
        /// <see cref="Input.IHand.IsHighConfidence"/> is false, then <paramref name="pose"/> might also be considered to
        /// be known with low confidence.
        /// </param>
        public void Process(Pose pose, float time,
            bool isHighConfidence = true)
        {
            if (_poses.Count > 0 && _poses.Peek().time == time)
            {
                return;
            }

            if (!isHighConfidence)
            {
                _highConfidenceStreak = false;
            }
            else
            {
                //first high-confidence frame
                if (!_highConfidenceStreak &&
                    _poses.Count > 0)
                {
                    TimedPose repeatedPose = _poses.Peek();
                    //remove the dirty data from the buffer
                    _poses.Clear();
                    //add a first synthetic pose as if it where from the previous frame
                    //cap the speed so it is within the allowed limit
                    float distance = Vector3.Distance(pose.position, repeatedPose.pose.position);
                    float deltaTime = time - _lastProcessedTime;
                    if (Mathf.Approximately(deltaTime, 0f)
                        || distance / deltaTime > _maxSyntheticSpeed)
                    {
                        repeatedPose.time = time - (distance / _maxSyntheticSpeed);
                    }
                    else
                    {
                        repeatedPose.time = _lastProcessedTime;
                    }
                    _poses.Add(repeatedPose);
                }

                _highConfidenceStreak = true;

                TimedPose timedPose = new TimedPose(time, pose);
                _poses.Add(timedPose);
            }

            _lastProcessedTime = time;
        }

        /// <summary>
        /// Estimates the current translational and rotational velocities based on the available data.
        /// If there is insufficient data to produce an estimate, returns 0 trivial velocities.
        /// </summary>
        /// <param name="velocity">Output parameter for translational velocity.</param>
        /// <param name="torque">Output parameter for rotational velocity.</param>
        public void GetVelocities(out Vector3 velocity, out Vector3 torque)
        {
            if (_poses.Count >= 2)
            {
                if (_velocityModel == null)
                {
                    _velocityModel = CalculateVelocityFromSamples;
                    _velocityScorer = ScoreDistance;
                    _torqueModel = CalculateTorqueFromSamples;
                    _torqueScorer = ScoreAngularDistance;
                }
                velocity = _ransac.FindOptimalModel(
                    _velocityModel, _velocityScorer, _poses.Count);
                torque = _ransac.FindOptimalModel(
                    _torqueModel, _torqueScorer, _poses.Count);
            }
            else
            {
                velocity = Vector3.zero;
                torque = Vector3.zero;
            }
        }

        private Vector3 CalculateVelocityFromSamples(int idx1, int idx2)
        {
            GetSortedTimePoses(idx1, idx2, out TimedPose older, out TimedPose younger);
            float timeShift = younger.time - older.time;
            Vector3 positionShift = PositionOffset(younger.pose, older.pose);
            return positionShift / timeShift;
        }

        private Vector3 CalculateTorqueFromSamples(int idx1, int idx2)
        {
            GetSortedTimePoses(idx1, idx2, out TimedPose older, out TimedPose younger);
            Vector3 torque = GetTorque(older, younger);
            return torque;
        }

        protected virtual Vector3 PositionOffset(Pose youngerPose, Pose olderPose)
        {
            return youngerPose.position - olderPose.position;
        }

        private float ScoreDistance(Vector3 distance, Vector3[,] distances)
        {
            float score = 0f;
            for (int i = 0; i < _poses.Count; ++i)
            {
                for (int j = i + 1; j < _poses.Count; ++j)
                {
                    score += (distance - distances[i, j]).sqrMagnitude;
                }
            }
            return score;
        }

        protected void GetSortedTimePoses(int idx1, int idx2,
            out TimedPose older, out TimedPose younger)
        {
            int youngerIdx = idx1;
            int olderIdx = idx2;
            if (idx2 > idx1)
            {
                youngerIdx = idx2;
                olderIdx = idx1;
            }

            older = _poses[olderIdx];
            younger = _poses[youngerIdx];
        }

        private float ScoreAngularDistance(Vector3 angularDistance, Vector3[,] angularDistances)
        {
            float score = 0f;

            Quaternion target = Quaternion.Euler(angularDistance);

            for (int i = 0; i < _poses.Count; ++i)
            {
                for (int j = i + 1; j < _poses.Count; ++j)
                {
                    Quaternion sample = Quaternion.Euler(angularDistances[i, j]);
                    score += Mathf.Abs(Quaternion.Dot(target, sample));
                }
            }
            return score;
        }

        protected static Vector3 GetTorque(TimedPose older, TimedPose younger)
        {
            float timeShift = younger.time - older.time;
            Quaternion olderRot = older.pose.rotation;
            Quaternion youngerRot = younger.pose.rotation;

            //Quaternions have two ways of expressing the same rotation.
            //This code ensures that the result is the same rotation but expressed in the desired sign.
            if (Quaternion.Dot(olderRot, youngerRot) < 0)
            {
                youngerRot.x = -youngerRot.x;
                youngerRot.y = -youngerRot.y;
                youngerRot.z = -youngerRot.z;
                youngerRot.w = -youngerRot.w;
            }
            Quaternion deltaRotation = youngerRot * Quaternion.Inverse(olderRot);
            deltaRotation.ToAngleAxis(out float angularSpeed, out Vector3 torqueAxis);
            angularSpeed = (angularSpeed * Mathf.Deg2Rad) / timeShift;
            return torqueAxis * angularSpeed;
        }

        protected struct TimedPose
        {
            public float time;
            public Pose pose;

            public TimedPose(float time, Pose pose)
            {
                this.time = time;
                this.pose = pose;
            }
        }
    }
}
