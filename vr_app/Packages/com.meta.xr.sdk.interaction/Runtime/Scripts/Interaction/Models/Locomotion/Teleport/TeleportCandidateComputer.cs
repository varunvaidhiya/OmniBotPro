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
using static Oculus.Interaction.Locomotion.TeleportInteractor;
using InteractableSet = Oculus.Interaction.InteractableRegistry<Oculus.Interaction.Locomotion.TeleportInteractor, Oculus.Interaction.Locomotion.TeleportInteractable>.InteractableSet;


namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Simple strategy to compute the best teleport candidate.
    /// It iterates the list of candidates and score them based on the distance
    /// along the arc, returning the closest to the origin of the arc. It also
    /// supports checking blockers between a logical position and the arc origin.
    /// </summary>
    public class TeleportCandidateComputer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("(Meters, World) The threshold below which distances to a interactable " +
                 "are treated as equal for the purposes of ranking.")]
        private float _equalDistanceThreshold = 0.1f;
        /// <summary>
        /// (Meters, World) The threshold below which distances to a interactable
        /// are treated as equal for the purposes of ranking.
        /// </summary>
        public float EqualDistanceThreshold
        {
            get => _equalDistanceThreshold;
            set => _equalDistanceThreshold = value;
        }

        [SerializeField]
        [Tooltip("When provided, the Interactor will perform an extra check to ensure" +
            "nothing is blocking the line between this point and the teleport origin")]
        private Transform _blockCheckOrigin;
        /// <summary>
        /// Optional. When assigned, the segment from this point to the arc origin
        /// will check for blockers. You can assign it to a physics hand
        /// to prevent users from teleporting when clipping the arc through a wall.
        /// </summary>
        public Transform BlockCheckOrigin
        {
            get => _blockCheckOrigin;
            set => _blockCheckOrigin = value;
        }

        [SerializeField, Optional]
        [Tooltip("When assigned in Editor, this component will inject itself into the Interactor " +
            "during Awake.")]
        private TeleportInteractor _teleportInteractor;

        protected virtual void Awake()
        {
            if (_teleportInteractor != null)
            {
                _teleportInteractor.InjectOptionalCandidateComputer(this.ComputeCandidate);
            }
        }

        public virtual TeleportInteractable ComputeCandidate(
            IPolyline TeleportArc,
            in InteractableSet interactables,
            ComputeCandidateTiebreakerDelegate tiebreaker,
            out TeleportHit hitPose)
        {
            TeleportInteractable bestCandidate = null;
            float bestScore = float.PositiveInfinity;

            Vector3 arcOrigin = TeleportArc.PointAtIndex(0);
            Vector3 arcEnd = TeleportArc.PointAtIndex(TeleportArc.PointsCount - 1);
            TeleportHit bestHit = new TeleportHit(null, arcEnd, Vector3.up);

            if (_blockCheckOrigin != null)
            {
                bool blocked = false;
                foreach (TeleportInteractable interactable in interactables)
                {
                    if (!interactable.AllowTeleport)
                    {
                        blocked |= CheckOriginBlockers(_blockCheckOrigin.position, arcOrigin, interactable);
                    }
                }

                if (blocked)
                {
                    hitPose = bestHit;
                    return bestCandidate;
                }
            }

            foreach (TeleportInteractable interactable in interactables)
            {
                CheckCandidate(interactable);
            }

            hitPose = bestHit;
            return bestCandidate;

            bool CheckOriginBlockers(Vector3 from, Vector3 to, TeleportInteractable candidate)
            {
                if (candidate.DetectHit(from, to, out TeleportHit hit))
                {
                    float score = -Vector3.Distance(to, hit.Point);
                    return TrySetScore(candidate, hit, score);
                }
                return false;
            }

            void CheckCandidate(TeleportInteractable candidate)
            {
                Vector3 prevPoint = arcOrigin;
                float accumulatedDistance = 0;
                for (int i = 1; i < TeleportArc.PointsCount; i++)
                {
                    if (accumulatedDistance > bestScore)
                    {
                        break;
                    }

                    Vector3 point = TeleportArc.PointAtIndex(i);
                    if (candidate.DetectHit(prevPoint, point, out TeleportHit hit))
                    {
                        float score = accumulatedDistance
                            + Vector3.Distance(prevPoint, hit.Point);
                        if (TrySetScore(candidate, hit, score))
                        {
                            break;
                        }
                    }

                    accumulatedDistance += Vector3.Distance(prevPoint, point);
                    prevPoint = point;
                }
            }

            bool TrySetScore(TeleportInteractable candidate, TeleportHit hit, float score)
            {
                bool isTie = Mathf.Abs(bestScore - score) <= _equalDistanceThreshold;
                if (bestCandidate == null
                    || (!isTie && score < bestScore)
                    || (isTie && Tiebreak(candidate, bestCandidate) > 0))
                {
                    bestScore = score;
                    bestHit = hit;
                    bestCandidate = candidate;
                    return true;
                }
                return false;
            }

            int Tiebreak(TeleportInteractable a, TeleportInteractable b)
            {
                if (tiebreaker != null)
                {
                    return tiebreaker(a, b);
                }
                return a.TieBreakerScore.CompareTo(b.TieBreakerScore);
            }
        }
    }
}
