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
using Oculus.Interaction.Surfaces;
using System;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines a near-poke interaction target that is driven by a near-distance proximity computation and a raycast between the
    /// position recorded across two frames against a target surface. The interactor type for this interactable is
    /// <see cref="PokeInteractor"/>.
    /// </summary>
    public class PokeInteractable : PointerInteractable<PokeInteractor, PokeInteractable>
    {
        /// <summary>
        /// An ISurfacePatch, which provides both the backing surface (generally infinite) and pokable area (generally finite) of the interactor.
        /// </summary>
        [Tooltip("Represents the pokeable surface area of this interactable.")]
        [SerializeField, Interface(typeof(ISurfacePatch))]
        private UnityEngine.Object _surfacePatch;

        /// <summary>
        /// An ISurfacePatch, which provides both the backing surface (generally infinite) and pokable area (generally finite) of the
        /// interactor.
        /// </summary>
        public ISurfacePatch SurfacePatch { get; private set; }

        /// <summary>
        /// The distance from the surface along the normal that hover begins.
        /// </summary>
        /// <remarks>
        /// When a <see cref="PokeInteractor"/> is near a PokeInteractable, some point on the <see cref="SurfacePatch"/> is considered
        /// nearest to the interactor. Hover is determined by taking the direction of the surface's normal at this point, calculating
        /// the distance _specifically in that direction_ between the interactor and the surface, then checking that value against
        /// this setting.
        /// </remarks>
        [SerializeField]
        [FormerlySerializedAs("_maxDistance")]
        [Tooltip("The distance required for a poke interactor to enter hovering, " +
                 "measured along the normal to the surface (in meters)")]
        private float _enterHoverNormal = 0.15f;

        /// <summary>
        /// The distance from the surface perpendicular to the normal that hover begins.
        /// </summary>
        [SerializeField]
        [Tooltip("The distance required for a poke interactor to enter hovering, " +
                 "measured along the tangent plane to the surface (in meters)")]
        private float _enterHoverTangent = 0;

        /// <summary>
        /// The distance from the surface along the normal that hover ends.
        /// </summary>
        [SerializeField]
        [Tooltip("The distance required for a poke interactor to exit hovering, " +
                 "measured along the normal to the surface (in meters)")]
        private float _exitHoverNormal = 0.2f;

        /// <summary>
        /// The distance from the surface perpendicular to the normal that hover ends.
        /// </summary>
        [SerializeField]
        [Tooltip("The distance required for a poke interactor to exit hovering, " +
                 "measured along the tangent plane to the surface (in meters)")]
        private float _exitHoverTangent = 0f;

        /// <summary>
        /// The distance you must poke through the surface in order for selection to cancel.
        /// A zero value means selection is never canceled no matter how deeply you penetrate the surface.
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("_releaseDistance")]
        [Tooltip("If greater than zero, " +
                 "the distance required for a selecting poke interactor to cancel selection, " +
                 "measured along the negative normal to the surface (in meters).")]
        private float _cancelSelectNormal = 0.3f;

        /// <summary>
        /// If greater than zero, the distance required for a selecting poke interactor to cancel selection, measured along the tangent plane
        /// to the surface (in meters). A zero value means selection is never cancelled no matter how far away you are from the surface.
        /// </summary>
        [SerializeField]
        [Tooltip("If greater than zero, " +
                 "the distance required for a selecting poke interactor to cancel selection, " +
                 "measured along the tangent plane to the surface (in meters).")]
        private float _cancelSelectTangent = 0.03f;

        /// <summary>
        /// Configures the minimum distance required for a <see cref="PokeInteractor"/> to surpass before it can hover a
        /// <see cref="PokeInteractable"/>. This configuration can vary by instance and can be set either through the Unity Editor or
        /// by programmatically setting the <see cref="PokeInteractable.MinThresholds"/> property.
        /// </summary>
        [Serializable]
        public class MinThresholdsConfig
        {
            /// <summary>
            /// Sets whether or not this config block is enabled. If disabled, all values will be ignored and default behavior will
            /// apply.
            /// </summary>
            [Tooltip("If true, minimum thresholds will be applied.")]
            public bool Enabled;

            /// <summary>
            /// The minimum distance required for a <see cref="PokeInteractor"/> to surpass before being able to hover, measured along
            /// the normal to the surface (in meters).
            /// </summary>
            [Tooltip("The minimum distance required for a poke interactor to surpass before " +
                     "being able to hover, measured along the normal to the surface (in meters).")]
            public float MinNormal = 0.01f;
        }

        /// <summary>
        /// If enabled, a poke interactor must approach the surface from at least a minimum distance of the surface (in meters).
        /// </summary>
        [SerializeField]
        [Tooltip("If enabled, a poke interactor must approach the surface from at least a " +
                 "minimum distance of the surface (in meters).")]
        private MinThresholdsConfig _minThresholds =
            new MinThresholdsConfig()
            {
                Enabled = false,
                MinNormal = 0.01f
            };

        /// <summary>
        /// Configures the drag thresholds, which are useful for distinguishing between press and drag and suppressing move pointer
        /// events when a <see cref="PokeInteractor"/> follows a pressing motion. This configuration can vary by instance and can
        /// be set either through the Unity Editor or by programmatically setting the <see cref="PokeInteractable.DragThresholds"/>
        /// property.
        /// </summary>
        [Serializable]
        public class DragThresholdsConfig
        {
            /// <summary>
            /// Sets whether or not this config block is enabled. If disabled, all values will be ignored and default behavior will
            /// apply.
            /// </summary>
            [Tooltip("If true, drag thresholds will be applied.")]
            public bool Enabled;

            /// <summary>
            /// The distance a <see cref="PokeInteractor"/> must travel to be treated as a press, measured along the normal to the
            /// surface (in meters).
            /// </summary>
            [FormerlySerializedAs("ZThreshold")]
            [Tooltip("The distance a poke interactor must travel to be treated as a press, " +
                     "measured as a distance along the normal to the surface (in meters).")]
            public float DragNormal;

            /// <summary>
            /// The distance a <see cref="PokeInteractor"/> must travel to be treated as a press, measured along the tangent plane to
            /// the surface (in meters
            /// </summary>
            [FormerlySerializedAs("SurfaceThreshold")]
            [Tooltip("The distance a poke interactor must travel to be treated as a drag, " +
                     "measured as a distance along the tangent plane to the surface (in meters).")]
            public float DragTangent;

            /// <summary>
            /// The curve that a <see cref="PokeInteractor"/> will use to ease when transitioning between a press and drag state.
            /// </summary>
            [Tooltip("The curve that a poke interactor will use to ease when transitioning " +
                     "between a press and drag state.")]
            public ProgressCurve DragEaseCurve;
        }

        [SerializeField]
        [FormerlySerializedAs("_dragThresholding")]
        [Tooltip("If enabled, drag thresholds will be applied in 3D space. " +
                 "Useful for disambiguating press vs drag and suppressing move pointer events " +
                 "when a poke interactor follows a pressing motion.")]
        private DragThresholdsConfig _dragThresholds =
            new DragThresholdsConfig()
            {
                Enabled = true,
                DragNormal = 0.01f,
                DragTangent = 0.01f,
                DragEaseCurve = new ProgressCurve(AnimationCurve.EaseInOut(0, 0, 1, 1), 0.05f)
            };

        /// <summary>
        /// Position pinning is applied to surface motion during drag, which is useful for adding a sense of friction to initial
        /// drag motion. This configuration can vary by instance and can be set either through the Unity Editor or by
        /// programmatically setting the <see cref="PokeInteractable.PositionPinning"/> property.
        /// </summary>
        /// </summary>
        [Serializable]
        public class PositionPinningConfig
        {
            /// <summary>
            /// Sets whether or not this config block is enabled. If disabled, all values will be ignored and default behavior will
            /// apply.
            /// </summary>
            [Tooltip("If true, position pinning will be applied.")]
            public bool Enabled;

            /// <summary>
            /// The distance over which a <see cref="PokeInteractor"/> drag motion will be remapped to the surface (in meters). If the
            /// hand is detected to move along this surface _less_ than this value, those movements will be ignored; this can help to
            /// eliminate noise and reduce the degree to which poking feels "slippery."
            /// </summary>
            [Tooltip("The distance over which a poke interactor drag motion will be remapped " +
                     "measured along the tangent plane to the surface (in meters)")]
            public float MaxPinDistance;

            /// <summary>
            /// The <see cref="PokeInteractor"/> position will be remapped along this curve from the initial touch point to the current
            /// position on surface.
            /// </summary>
            [Tooltip("The poke interactor position will be remapped along this curve from the " +
                "initial touch point to the current position on surface.")]
            public AnimationCurve PinningEaseCurve;

            /// <summary>
            /// In cases where a resync is necessary between the pinned position and the unpinned position, this time-based curve will
            /// be used.
            /// </summary>
            [Tooltip("In cases where a resync is necessary between the pinned position and the " +
                "unpinned position, this time-based curve will be used.")]
            public ProgressCurve ResyncCurve;
        }

        [SerializeField]
        [Tooltip("If enabled, position pinning will be applied to surface motion during drag. " +
                 "Useful for adding a sense of friction to initial drag motion.")]
        private PositionPinningConfig _positionPinning =
            new PositionPinningConfig()
            {
                Enabled = false,
                MaxPinDistance = 0.075f,
                PinningEaseCurve = AnimationCurve.EaseInOut(0.2f, 0, 1, 1),
                ResyncCurve = new ProgressCurve(AnimationCurve.EaseInOut(0, 0, 1, 1), 0.2f)
            };

        /// <summary>
        /// Recoil assist will affect unselection and reselection criteria, which is useful for triggering unselect in response to a
        /// smaller motion in the negative direction from a surface. This configuration can vary by instance and can be set either
        /// through the Unity Editor or by programmatically setting the <see cref="PokeInteractable.RecoilAssist"/> property.
        /// </summary>
        [Serializable]
        public class RecoilAssistConfig
        {
            /// <summary>
            /// Sets whether or not this config block is enabled. If disabled, all values will be ignored and default behavior will
            /// apply.
            /// </summary>
            [Tooltip("If true, recoil assist will be applied.")]
            public bool Enabled;

            /// <summary>
            /// If true, <see cref="DynamicDecayCurve"/> will be used to decay the max distance based on the normal velocity. Otherwise,
            /// default behavior will apply.
            /// </summary>
            [Tooltip("If true, DynamicDecayCurve will be used to decay the max distance based on the normal velocity.")]
            public bool UseDynamicDecay;

            /// <summary>
            /// A Unity AnimationCurve of the normal movement ratio to determine the rate of decay.
            /// </summary>
            [Tooltip("A function of the normal movement ratio to determine the rate of decay.")]
            public AnimationCurve DynamicDecayCurve;

            /// <summary>
            /// Expand recoil window when fast Z motion is detected.
            /// </summary>
            /// <remarks>
            /// When poking a surface rapidly, users are much more likely to over-penetrate --- to move their hands spatially further
            /// past the virtual <see cref="PokeInteractable.SurfacePatch"/> than they might normally do, simply because they're moving
            /// fast. Under such circumstances, both motion tracking and user proprioception tend to contain large spatial errors, and
            /// typical interaction windows may consequently be inadequate in such scenarios. This leads to the concept of "velocity
            /// expansion," which pads or "expands" the error margins for certain interaction parameters (notably recoil assist) based on
            /// the velocity of the user's interaction.
            /// </remarks>
            [Tooltip("Expand recoil window when fast Z motion is detected.")]
            public bool UseVelocityExpansion;

            /// <summary>
            /// When average velocity in interactable Z is greater than min speed, the recoil window will begin expanding. See
            /// <see cref="UseVelocityExpansion"/> for a more detailed overview of what "velocity expansion" means.
            /// </summary>
            [Tooltip("When average velocity in interactable Z is greater than min speed, the recoil window will begin expanding.")]
            public float VelocityExpansionMinSpeed;

            /// <summary>
            /// Full recoil window expansion reached at this speed. See <see cref="UseVelocityExpansion"/> for a more detailed overview
            /// of what "velocity expansion" means.
            /// </summary>
            [Tooltip("Full recoil window expansion reached at this speed.")]
            public float VelocityExpansionMaxSpeed;

            /// <summary>
            /// Window will expand by this distance when Z velocity reaches max speed. See <see cref="UseVelocityExpansion"/> for a
            /// more detailed overview of what "velocity expansion" means.
            /// </summary>
            [Tooltip("Window will expand by this distance when Z velocity reaches max speed.")]
            public float VelocityExpansionDistance;

            /// <summary>
            /// Window will contract toward ExitDistance at this rate (in meters) per second when velocity lowers. See
            /// <see cref="UseVelocityExpansion"/> for a more detailed overview of what "velocity expansion" means.
            /// </summary>
            [Tooltip("Window will contract toward ExitDistance at this rate (in meters) per second when velocity lowers.")]
            public float VelocityExpansionDecayRate;


            /// <summary>
            /// The distance over which a poke interactor must surpass to trigger an early unselect, measured along the normal to the
            /// surface (in meters).
            /// </summary>
            [Tooltip("The distance over which a poke interactor must surpass to trigger " +
                     "an early unselect, measured along the normal to the surface (in meters)")]
            public float ExitDistance;
            /// <summary>
            /// When in recoil, the distance which a poke interactor must surpass to trigger a subsequent select, measured along
            /// the negative normal to the surface (in meters).
            /// </summary>
            [Tooltip("When in recoil, the distance which a poke interactor must surpass to trigger " +
                     "a subsequent select, measured along the negative normal to the surface (in meters)")]
            public float ReEnterDistance;
        }

        [SerializeField]
        [Tooltip("If enabled, recoil assist will affect unselection and reselection criteria. " +
                 "Useful for triggering unselect in response to a smaller motion in the negative " +
                 "direction from a surface.")]
        private RecoilAssistConfig _recoilAssist =
            new RecoilAssistConfig()
            {
                Enabled = false,
                UseDynamicDecay = false,
                DynamicDecayCurve = new AnimationCurve(
                    new Keyframe(0f, 50f), new Keyframe(0.9f, 0.5f, -47, -47)),
                UseVelocityExpansion = false,
                VelocityExpansionMinSpeed = 0.4f,
                VelocityExpansionMaxSpeed = 1.4f,
                VelocityExpansionDistance = 0.055f,
                VelocityExpansionDecayRate = 0.125f,
                ExitDistance = 0.02f,
                ReEnterDistance = 0.02f
            };

        [SerializeField, Optional]
        [Tooltip("(Meters, World) The threshold below which distances near this surface " +
                 "are treated as equal in depth for the purposes of ranking.")]
        private float _closeDistanceThreshold = 0.001f;

        [SerializeField, Optional]
        private int _tiebreakerScore = 0;

        #region Properties

        /// <summary>
        /// The distance required for a <see cref="PokeInteractor"/> to enter hovering, measured along the tangent plane to the surface
        /// (in meters).
        /// </summary>
        public float EnterHoverNormal
        {
            get
            {
                return _enterHoverNormal;
            }

            set
            {
                _enterHoverNormal = value;
            }
        }

        /// <summary>
        /// The distance required for a <see cref="PokeInteractor"/> to enter hovering, measured along the tangent plane to the surface
        /// (in meters).
        /// </summary>
        public float EnterHoverTangent
        {
            get
            {
                return _enterHoverTangent;
            }

            set
            {
                _enterHoverTangent = value;
            }
        }

        /// <summary>
        /// The distance required for a <see cref="PokeInteractor"/> to exit hovering, measured along the normal to the surface (in
        /// meters).
        /// </summary>
        public float ExitHoverNormal
        {
            get
            {
                return _exitHoverNormal;
            }

            set
            {
                _exitHoverNormal = value;
            }
        }

        /// <summary>
        /// The distance required for a <see cref="PokeInteractor"/> to exit hovering, measured along the tangent plane to the surface
        /// (in meters).
        /// </summary>
        public float ExitHoverTangent
        {
            get
            {
                return _exitHoverTangent;
            }

            set
            {
                _exitHoverTangent = value;
            }
        }

        /// <summary>
        /// If greater than zero, the distance required for a selecting <see cref="PokeInteractor"/> to cancel selection, measured
        /// along the negative normal to the surface (in meters).
        /// </summary>
        public float CancelSelectNormal
        {
            get
            {
                return _cancelSelectNormal;
            }

            set
            {
                _cancelSelectNormal = value;
            }
        }

        /// <summary>
        /// If greater than zero, the distance required for a selecting <see cref="PokeInteractor"/> to cancel selection, measured
        /// along the tangent plane to the surface (in meters).
        /// </summary>
        public float CancelSelectTangent
        {
            get
            {
                return _cancelSelectTangent;
            }

            set
            {
                _cancelSelectTangent = value;
            }
        }

        /// <summary>
        /// The threshold below which distances near this surface are treated as equal in depth for the purposes of ranking, in
        /// world-space meters.
        /// </summary>
        public float CloseDistanceThreshold
        {
            get
            {
                return _closeDistanceThreshold;
            }
            set
            {
                _closeDistanceThreshold = value;
            }
        }

        /// <summary>
        /// If a <see cref="PokeInteractor"/> is positioned near multiple interactables such that it is equally interactable with
        /// any of them (i.e., the distances to all of them are within <see cref="CloseDistanceThreshold"/> of each other), the
        /// interactable with the largest TiebreakerScore wins and is targeted for interaction. If there's a tie for largest
        /// TiebreakerScore, one of the interactables will be selected arbitrarily.
        /// </summary>
        public int TiebreakerScore
        {
            get
            {
                return _tiebreakerScore;
            }
            set
            {
                _tiebreakerScore = value;
            }
        }

        /// <summary>
        /// The <see cref="MinThresholdsConfig"/> for this interactable instance. This value is typically set from the Unity Editor,
        /// but it can also be set and modified programmatically.
        /// </summary>
        public MinThresholdsConfig MinThresholds
        {
            get
            {
                return _minThresholds;
            }

            set
            {
                _minThresholds = value;
            }
        }

        /// <summary>
        /// The <see cref="DragThresholdsConfig"/> for this interactable instance. This value is typically set from the Unity Editor,
        /// but it can also be set and modified programmatically.
        /// </summary>
        public DragThresholdsConfig DragThresholds
        {
            get
            {
                return _dragThresholds;
            }

            set
            {
                _dragThresholds = value;
            }
        }

        /// <summary>
        /// The <see cref="PositionPinningConfig"/> for this interactable instance. This value is typically set from the Unity Editor,
        /// but it can also be set and modified programmatically.
        /// </summary>
        public PositionPinningConfig PositionPinning
        {
            get
            {
                return _positionPinning;
            }

            set
            {
                _positionPinning = value;
            }
        }

        /// <summary>
        /// The <see cref="RecoilAssistConfig"/> for this interactable instance. This value is typically set from the Unity Editor,
        /// but it can also be set and modified programmatically.
        /// </summary>
        public RecoilAssistConfig RecoilAssist
        {
            get
            {
                return _recoilAssist;
            }

            set
            {
                _recoilAssist = value;
            }
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            if (SurfacePatch == null)
            {
                SurfacePatch = _surfacePatch as ISurfacePatch;
            }
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(SurfacePatch, nameof(SurfacePatch));

            // _exitHover thresholds must be at a minimum the magnitude of the _enterHover thresholds
            _exitHoverNormal =
                Mathf.Max(_enterHoverNormal, _exitHoverNormal);

            _exitHoverTangent =
                Mathf.Max(_enterHoverTangent, _exitHoverTangent);

            // If non-zero, _cancelSelectTangent must be at a minimum the magnitude of _exitHoverTangent
            if (_cancelSelectTangent > 0)
            {
                _cancelSelectTangent =
                    Mathf.Max(_exitHoverTangent, _cancelSelectTangent);
            }

            if (_minThresholds.Enabled && _minThresholds.MinNormal > 0f)
            {
                _minThresholds.MinNormal =
                    Mathf.Min(_minThresholds.MinNormal,
                    _enterHoverNormal);
            }
            this.EndStart(ref _started);
        }

        /// <summary>
        /// Convenience method wrapping a call to <see cref="ISurface.ClosestSurfacePoint(in Vector3, out SurfaceHit, float)"/> on
        /// the <see cref="SurfacePatch"/>.
        /// </summary>
        /// <param name="point">The point, in world space, for which the nearest point on the surface must be found</param>
        /// <param name="hit">The returned hit data in world space if a nearest point could be found, default otherwise</param>
        /// <returns>True if the racyast hit the surface, false otherwise</returns>
        public bool ClosestSurfacePatchHit(Vector3 point, out SurfaceHit hit)
        {
            return SurfacePatch.ClosestSurfacePoint(point, out hit);
        }

        /// <summary>
        /// Convenience method wrapping a call to <see cref="ISurface.ClosestSurfacePoint(in Vector3, out SurfaceHit, float)"/> on
        /// the <see cref="ISurfacePatch.BackingSurface"/> of this interactable's <see cref="SurfacePatch"/>.
        /// </summary>
        /// <param name="point">The point, in world space, for which the nearest point on the surface must be found</param>
        /// <param name="hit">The returned hit data in world space if a nearest point could be found, default otherwise</param>
        /// <returns>True if the racyast hit the surface, false otherwise</returns>
        public bool ClosestBackingSurfaceHit(Vector3 point, out SurfaceHit hit)
        {
            return SurfacePatch.BackingSurface.ClosestSurfacePoint(point, out hit);
        }

        private void Reset()
        {
            _minThresholds.Enabled = false;
            _dragThresholds.Enabled = false;
            _positionPinning.Enabled = true;
            _recoilAssist.Enabled = true;
            _recoilAssist.UseVelocityExpansion = true;
            _recoilAssist.UseDynamicDecay = true;
        }

        #region Inject

        /// <summary>
        /// Sets all the required values for a PokeInteractable on a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllPokeInteractable(ISurfacePatch surfacePatch)
        {
            InjectSurfacePatch(surfacePatch);
        }

        /// <summary>
        /// Sets a surface patch for a dynamically instantiated GameObject. This method exists to support Interaction SDK's dependency
        /// injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSurfacePatch(ISurfacePatch surfacePatch)
        {
            _surfacePatch = surfacePatch as UnityEngine.Object;
            SurfacePatch = surfacePatch;
        }

        #endregion
    }
}
