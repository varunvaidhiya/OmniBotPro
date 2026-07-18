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
using UnityEngine.Pool;

namespace Oculus.Interaction
{
    /// <summary>
    /// Checks for the existence of a Secondary Interactor Connection given a Primary Interaction.
    /// Filters out Interactors which are not Secondary to a hovering or selecting Primary.
    /// </summary>
    public class SecondaryInteractorFilter : MonoBehaviour, IGameObjectFilter
    {
        [SerializeField, Interface(typeof(IInteractable))]
        private UnityEngine.Object _primaryInteractable;
        public IInteractable PrimaryInteractable { get; private set; }

        [SerializeField, Interface(typeof(IInteractable))]
        private UnityEngine.Object _secondaryInteractable;
        public IInteractable SecondaryInteractable { get; private set; }

        [SerializeField]
        private bool _selectRequired = false;

        private Dictionary<int, List<int>> _primaryToSecondaryMap = null;

        protected bool _started = false;

        protected virtual void Awake()
        {
            PrimaryInteractable = _primaryInteractable as IInteractable;
            SecondaryInteractable = _secondaryInteractable as IInteractable;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(PrimaryInteractable, nameof(_primaryInteractable));
            this.AssertField(SecondaryInteractable, nameof(_secondaryInteractable));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                if (_selectRequired)
                {
                    PrimaryInteractable.WhenSelectingInteractorViewAdded += HandleInteractorAdded;
                    PrimaryInteractable.WhenSelectingInteractorViewRemoved += HandleInteractorRemoved;
                }
                else
                {
                    PrimaryInteractable.WhenInteractorViewAdded += HandleInteractorAdded;
                    PrimaryInteractable.WhenInteractorViewRemoved += HandleInteractorRemoved;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_selectRequired)
                {
                    PrimaryInteractable.WhenSelectingInteractorViewAdded -= HandleInteractorAdded;
                    PrimaryInteractable.WhenSelectingInteractorViewRemoved -= HandleInteractorRemoved;
                }
                else
                {
                    PrimaryInteractable.WhenInteractorViewAdded -= HandleInteractorAdded;
                    PrimaryInteractable.WhenInteractorViewRemoved -= HandleInteractorRemoved;
                }
            }
        }

        public bool Filter(GameObject gameObject)
        {
            if (_primaryToSecondaryMap == null)
            {
                return false;
            }

            if (!gameObject.TryGetComponent(out SecondaryInteractorConnection connection))
            {
                return false;
            }

            int primaryInteractorID = connection.PrimaryInteractor.Identifier;

            if (!_primaryToSecondaryMap.ContainsKey(primaryInteractorID))
            {
                return false;
            }

            List<int> secondaryViews = _primaryToSecondaryMap[primaryInteractorID];
            if (!secondaryViews.Contains(connection.SecondaryInteractor.Identifier))
            {
                secondaryViews.Add(connection.SecondaryInteractor.Identifier);
            }

            return true;
        }

        private void HandleInteractorAdded(IInteractorView interactor)
        {
            if (_primaryToSecondaryMap == null)
            {
                _primaryToSecondaryMap = DictionaryPool<int, List<int>>.Get();
            }
            _primaryToSecondaryMap.Add(interactor.Identifier, ListPool<int>.Get());
        }

        private void HandleInteractorRemoved(IInteractorView primaryInteractor)
        {
            foreach (int secondartyInteractorID in _primaryToSecondaryMap[primaryInteractor.Identifier])
            {
                SecondaryInteractable.RemoveInteractorByIdentifier(secondartyInteractorID);
            }

            //Clear the map entry returning to the pool first the List and then the Dictionary if empty
            ListPool<int>.Release(_primaryToSecondaryMap[primaryInteractor.Identifier]);
            _primaryToSecondaryMap.Remove(primaryInteractor.Identifier);
            if (_primaryToSecondaryMap.Count == 0)
            {
                DictionaryPool<int, List<int>>.Release(_primaryToSecondaryMap);
                _primaryToSecondaryMap = null;
            }
        }

        #region Inject

        public void InjectAllSecondaryInteractorFilter(
            IInteractable primaryInteractable,
            IInteractable secondaryInteractable,
            bool selectRequired = false)
        {
            InjectPrimaryInteractable(primaryInteractable);
            InjectSecondaryInteractable(secondaryInteractable);
            InjectSelectRequired(selectRequired);
        }

        public void InjectPrimaryInteractable(IInteractable interactableView)
        {
            PrimaryInteractable = interactableView;
            _primaryInteractable = interactableView as UnityEngine.Object;
        }

        public void InjectSecondaryInteractable(IInteractable interactable)
        {
            SecondaryInteractable = interactable;
            _secondaryInteractable = interactable as UnityEngine.Object;
        }

        public void InjectSelectRequired(bool selectRequired)
        {
            _selectRequired = selectRequired;
        }

        #endregion
    }
}
