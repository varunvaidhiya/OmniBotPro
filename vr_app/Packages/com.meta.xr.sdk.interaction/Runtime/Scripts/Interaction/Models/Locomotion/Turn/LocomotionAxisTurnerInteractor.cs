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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Defines an <see cref="IInteractor"/> which performs locomotion turns (i.e., rotating the user in virtual space) based on
    /// input from an <see cref="IAxis2D"/>, canonically a controller thumbstick, from which only the horizontal (x) axis will
    /// be used. There is no real interactable for this interactor (<see cref="LocomotionAxisTurnerInteractable"/> is an empty
    /// type, as described in the comments on that class).
    /// </summary>
    /// <remarks>
    /// For the hand tracking counterpart to this type, see <see cref="LocomotionTurnerInteractor"/>.
    /// </remarks>
    public class LocomotionAxisTurnerInteractor : Interactor<LocomotionAxisTurnerInteractor, LocomotionAxisTurnerInteractable>
        , IAxis1D
    {
        [SerializeField, Interface(typeof(IAxis2D))]
        [Tooltip("Input 2D Axis from which the Horizontal axis will be extracted")]
        private UnityEngine.Object _axis2D;
        /// <summary>
        /// Input 2D Axis from which the Horizontal axis will be extracted
        /// </summary>
        private IAxis2D Axis2D;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("The Axis.x absolute value must be bigger than this to go into Hover and Select states")]
        private float _deadZone = 0.5f;
        /// <summary>
        /// The "dead zone" of control for this interactor: the absolute value of the horizontal (x) axis of the input control
        /// <see cref="IAxis2D"/> must be exceed this value before this interactor begins to interact (i.e., enter Hover or Select
        /// states).
        /// </summary>
        public float DeadZone
        {
            get
            {
                return _deadZone;
            }
            set
            {
                _deadZone = value;
            }
        }

        private float _horizontalAxisValue;

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldHover"/>, more documentation for which can be found in the defining
        /// interface. Becomes true or false based on the absolute value of the horizontal (x) axis of the input
        /// <see cref="IAxis2D"/> compared with the <see cref="DeadZone"/>.
        /// </summary>
        /// <remarks>
        /// The exact relationship between ShouldHover and the input axis is an implementation detail; the value is set
        /// based on the axis's value relative to the <see cref="DeadZone"/>, but exactly *when* that happens is not specified
        /// in the contract. Consequently, there may be times when ShouldHover does not return the value you might expect from
        /// directly comparing the input axis to the <see cref="DeadZone"/>.
        /// </remarks>
        public override bool ShouldHover => Mathf.Abs(_horizontalAxisValue) > _deadZone;

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldUnhover"/>, more documentation for which can be found in the defining
        /// interface. Becomes true or false based on the absolute value of the horizontal (x) axis of the input
        /// <see cref="IAxis2D"/> compared with the <see cref="DeadZone"/>.
        /// </summary>
        /// <remarks>
        /// The exact relationship between ShouldUnhover and the input axis is an implementation detail; the value is set
        /// based on the axis's value relative to the <see cref="DeadZone"/>, but exactly *when* that happens is not specified
        /// in the contract. Consequently, there may be times when ShouldUnhover does not return the value you might expect from
        /// directly comparing the input axis to the <see cref="DeadZone"/>.
        /// </remarks>
        public override bool ShouldUnhover => !ShouldHover;

        protected override bool ComputeShouldSelect()
        {
            return ShouldHover;
        }

        protected override bool ComputeShouldUnselect()
        {
            return ShouldUnhover;
        }

        protected override void Awake()
        {
            base.Awake();
            Axis2D = _axis2D as IAxis2D;
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                _horizontalAxisValue = 0f;
            }
            base.OnDisable();
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_axis2D, nameof(_axis2D));
            this.EndStart(ref _started);
        }

        protected override void DoPreprocess()
        {
            base.DoPreprocess();
            _horizontalAxisValue = Axis2D.Value().x;
        }

        protected override LocomotionAxisTurnerInteractable ComputeCandidate()
        {
            return null;
        }

        /// <summary>
        /// Returns the most recently sampled value of the horizontal (x) axis of the input <see cref="IAxis2D"/>.
        /// </summary>
        /// <remarks>
        /// Though this value is set from the input axis, exactly when and how that happens is not specified in the
        /// contract. Consequently, there may be times when Value does not return the actual current value of the
        /// input axis.
        /// </remarks>
        public float Value()
        {
            return _horizontalAxisValue;
        }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated LocomotionAxisTurnerInteractor; effectively wraps
        /// <see cref="InjectAxis2D(IAxis2D)"/>. This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllLocomotionAxisTurner(IAxis2D axis2D)
        {
            InjectAxis2D(axis2D);
        }

        /// <summary>
        /// Sets an <see cref="IAxis2D"/> for a dynamically instantiated LocomotionAxisTurnerInteractor. This method exists to
        /// support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAxis2D(IAxis2D axis2D)
        {
            _axis2D = axis2D as UnityEngine.Object;
            Axis2D = axis2D;
        }

        #endregion
    }

    /// <summary>
    /// Contractually, <see cref="Interactor{TInteractor, TInteractable}"/>s require interactables, but
    /// <see cref="LocomotionAxisTurnerInteractor"/>'s interaction (turning the user) doesn't logically require
    /// a target since it simply invokes a specific effect. LocomotionAxisTurnerInteractable is thus
    /// provided as an empty interactable type, no instances of which should ever be created, simply as an
    /// implementation detail of <see cref="LocomotionAxisTurnerInteractor"/>.
    /// </summary>
    public class LocomotionAxisTurnerInteractable : Interactable<LocomotionAxisTurnerInteractor, LocomotionAxisTurnerInteractable> { }
}
