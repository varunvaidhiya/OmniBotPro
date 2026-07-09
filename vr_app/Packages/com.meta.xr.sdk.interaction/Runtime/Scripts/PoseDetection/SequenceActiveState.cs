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

using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Interaction.PoseDetection.Debug;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Wraps a <see cref="Sequence"/> component and adds several additional conditions to
    /// its <see cref="IActiveState"/> logic.
    /// </summary>
    public class SequenceActiveState : MonoBehaviour, IActiveState
    {
        [Tooltip("The Sequence that will drive this component.")]
        [SerializeField]
        private Sequence _sequence;

        [Tooltip("If true, this ActiveState will become Active as soon " +
            "as the first sequence step becomes Active.")]
        [SerializeField]
        private bool _activateIfStepsStarted;

        [Tooltip("If true, this ActiveState will be active when " +
            "the supplied Sequence is Active.")]
        [SerializeField]
        private bool _activateIfStepsComplete = true;

        protected virtual void Start()
        {
            this.AssertField(_sequence, nameof(_sequence));
        }

        /// <summary>
        /// Implementation of <see cref="IActiveState.Active"/>;
        /// for details, please refer to the related documentation provided for that interface.
        /// </summary>
        public bool Active
        {
            get
            {
                return (_activateIfStepsStarted && _sequence.CurrentActivationStep > 0 && !_sequence.Active) ||
                       (_activateIfStepsComplete && _sequence.Active);
            }
        }

        static SequenceActiveState()
        {
            ActiveStateDebugTree.RegisterModel<SequenceActiveState>(new DebugModel());
        }

        private class DebugModel : ActiveStateModel<SequenceActiveState>
        {
            protected override Task<IEnumerable<IActiveState>> GetChildrenAsync(SequenceActiveState activeState)
            {
                return Task.FromResult<IEnumerable<IActiveState>>(new[] { activeState._sequence });
            }
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="SequenceActiveState"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllSequenceActiveState(Sequence sequence,
            bool activateIfStepsStarted, bool activateIfStepsComplete)
        {
            InjectSequence(sequence);
            InjectActivateIfStepsStarted(activateIfStepsStarted);
            InjectActivateIfStepsComplete(activateIfStepsComplete);
        }

        /// <summary>
        /// Sets the underlying <see cref="Sequence"/> for a dynamically instantiated
        /// <see cref="SequenceActiveState"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSequence(Sequence sequence)
        {
            _sequence = sequence;
        }

        /// <summary>
        /// Sets ActivateIfStepsStarted for a dynamically instantiated
        /// <see cref="SequenceActiveState"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectActivateIfStepsStarted(bool activateIfStepsStarted)
        {
            _activateIfStepsStarted = activateIfStepsStarted;
        }

        /// <summary>
        /// Sets ActivateIfStepsComplete for a dynamically instantiated
        /// <see cref="SequenceActiveState"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectActivateIfStepsComplete(bool activateIfStepsComplete)
        {
            _activateIfStepsComplete = activateIfStepsComplete;
        }

        #endregion
    }
}
