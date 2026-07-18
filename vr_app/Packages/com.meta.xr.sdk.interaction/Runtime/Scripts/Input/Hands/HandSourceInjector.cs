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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Injects into a Hand the Source and ModifyAfter parameters depending
    /// on which hand device is currently active. It maintaines the source
    /// until it becomes inactive.
    /// </summary>
    public class HandSourceInjector : MonoBehaviour
    {
        [System.Serializable]
        private class ActiveDataSource
        {
            [SerializeField, Interface(typeof(IDataSource<HandDataAsset>))]
            private UnityEngine.Object _source;
            public IDataSource<HandDataAsset> Source { get; private set; }

            [SerializeField, Interface(typeof(IDataSource))]
            private UnityEngine.Object _modifyAfter;
            public IDataSource ModifyAfter { get; private set; }

            public void Initialize()
            {
                Source = _source as IDataSource<HandDataAsset>;
                ModifyAfter = _modifyAfter as IDataSource;

                AssertField(Source, nameof(Source));
                AssertField(ModifyAfter, nameof(ModifyAfter));

                void AssertField(object obj, string name)
                {
                    Assert.IsNotNull(obj,
                        $"At component <b>{nameof(HandSourceInjector)}</b>. {nameof(ActiveDataSource)}. " +
                        $"Required <b>{nameof(name)}</b> reference is missing. ");
                }
            }

            public bool IsActive()
            {
                HandDataAsset data = this.Source.GetData();
                return data.IsDataValidAndConnected;
            }
        }

        /// <summary>
        /// Hand that whose sources will be overriden
        /// </summary>
        [SerializeField]
        private Hand _targetHand;

        /// <summary>
        /// List of sources to override in the hand,
        /// the top most sources will have higher priority
        /// </summary>
        [SerializeField]
        private ActiveDataSource[] _sources;

        private ActiveDataSource _activeDataSource;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_targetHand, nameof(_targetHand));
            this.AssertCollectionField(_sources, nameof(_sources));

            foreach (var source in _sources)
            {
                source.Initialize();
            }

            UpdateActiveSource();
            if (_activeDataSource == null)
            {
                ApplySource(_sources[0]);
            }

            this.EndStart(ref _started);
        }

        private void Update()
        {
            if (!_activeDataSource.IsActive())
            {
                UpdateActiveSource();
            }
        }

        private void UpdateActiveSource()
        {
            foreach (ActiveDataSource source in _sources)
            {
                if (source.IsActive())
                {
                    ApplySource(source);
                    return;
                }
            }
        }

        private void ApplySource(ActiveDataSource activeDataSource)
        {
            _activeDataSource = activeDataSource;
            _targetHand.ResetSources(activeDataSource.Source, activeDataSource.ModifyAfter, _targetHand.UpdateMode);
        }
    }
}
