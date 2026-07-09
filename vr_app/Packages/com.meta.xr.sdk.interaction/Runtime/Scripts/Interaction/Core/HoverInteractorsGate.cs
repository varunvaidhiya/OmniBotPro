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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This component checks the Status of the InteractorsA and InteractorsB.
    /// Whenever one of the Interactors inside the lists Hovers,
    /// all the Interactors in the opposite group have their components disabled.
    /// </summary>
    public class HoverInteractorsGate : MonoBehaviour
    {
        /// <summary>
        /// Group of interactors A will disable _interactorsB when one of them is hovered
        /// </summary>
        [SerializeField, Interface(typeof(IInteractor))]
        private List<UnityEngine.Object> _interactorsA;
        private List<IInteractor> InteractorsA;

        /// <summary>
        /// Group of interactors B will disable _interactorsA when one of them is hovered
        /// </summary>
        [SerializeField, Interface(typeof(IInteractor))]
        private List<UnityEngine.Object> _interactorsB;
        private List<IInteractor> InteractorsB;

        private int _hoveringInteractorsACount = 0;
        private int _hoveringInteractorsBCount = 0;

        protected bool _started = false;

        protected virtual void Awake()
        {
            this.WarnInspectorCollectionItems(_interactorsA, nameof(_interactorsA));
            InteractorsA = _interactorsA
                .FindAll(i => i != null)
                .ConvertAll(i => i as IInteractor);

            this.WarnInspectorCollectionItems(_interactorsB, nameof(_interactorsB));
            InteractorsB = _interactorsB
                .FindAll(i => i != null)
                .ConvertAll(i => i as IInteractor);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertCollectionItems(InteractorsA, nameof(_interactorsA));
            this.AssertCollectionItems(InteractorsB, nameof(_interactorsB));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                foreach (IInteractor interactorA in InteractorsA)
                {
                    interactorA.WhenStateChanged += HandleInteractorAStateChanged;
                }

                foreach (IInteractor interactorB in InteractorsB)
                {
                    interactorB.WhenStateChanged += HandleInteractorBStateChanged;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                foreach (IInteractor interactorA in InteractorsA)
                {
                    interactorA.WhenStateChanged -= HandleInteractorAStateChanged;
                }

                foreach (IInteractor interactorB in InteractorsB)
                {
                    interactorB.WhenStateChanged -= HandleInteractorBStateChanged;
                }

                _hoveringInteractorsACount = 0;
                _hoveringInteractorsBCount = 0;
            }
        }

        private void HandleInteractorAStateChanged(InteractorStateChangeArgs stateChange)
        {
            ProcessInteractorsStateChange(stateChange, ref _hoveringInteractorsACount, InteractorsB);
        }

        private void HandleInteractorBStateChanged(InteractorStateChangeArgs stateChange)
        {
            ProcessInteractorsStateChange(stateChange, ref _hoveringInteractorsBCount, InteractorsA);
        }

        private void ProcessInteractorsStateChange(InteractorStateChangeArgs stateChange, ref int hoveringCounter,
            List<IInteractor> oppositeInteractors)
        {
            if (stateChange.PreviousState == InteractorState.Normal
                && stateChange.NewState == InteractorState.Hover)
            {
                if (hoveringCounter++ == 0)
                {
                    EnableAll(oppositeInteractors, false);
                }
            }

            if (stateChange.PreviousState == InteractorState.Hover
                && stateChange.NewState == InteractorState.Normal)
            {
                if (--hoveringCounter == 0)
                {
                    EnableAll(oppositeInteractors, true);
                }
            }
        }

        private void EnableAll(List<IInteractor> interactors, bool enable)
        {
            foreach (IInteractor interactor in interactors)
            {
                if (interactor is Behaviour monoBehaviour)
                {
                    monoBehaviour.enabled = enable;
                }
            }
        }

        #region Inject

        public void InjectAllHoverInteractorsGate(List<IInteractor> interactorsA, List<IInteractor> interactorsB)
        {
            InjectInteractorsA(interactorsA);
            InjectInteractorsB(interactorsB);
        }

        public void InjectInteractorsA(List<IInteractor> interactors)
        {
            InteractorsA = interactors;
            _interactorsA = interactors.ConvertAll(i => i as UnityEngine.Object);
        }

        public void InjectInteractorsB(List<IInteractor> interactors)
        {
            InteractorsB = interactors;
            _interactorsB = interactors.ConvertAll(i => i as UnityEngine.Object);
        }
        #endregion
    }
}
