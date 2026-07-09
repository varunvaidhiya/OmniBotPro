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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// A specialization of <see cref="DataSource{TData}"/> which consumes data from another <see cref="IDataSource{TData}"/>,
    /// modifies that data in some way, then makes the modified data available to downstream consumers. Examples of this include
    /// <see cref="Filter.HandFilter"/> (which applies smoothing to hand tracking data) and <see cref="SyntheticHand"/> (which
    /// can change the position and shape of hand tracking data based on conditions in the scene).
    /// </summary>
    public abstract class
        DataModifier<TData> : DataSource<TData>
        where TData : class, ICopyFrom<TData>, new()
    {
        [Header("Data Modifier")]
        [SerializeField, Interface(nameof(_modifyDataFromSource))]
        protected UnityEngine.Object _iModifyDataFromSourceMono;
        private IDataSource<TData> _modifyDataFromSource;

        [SerializeField]
        [Tooltip("If this is false, then this modifier will simply pass through " +
                 "data without performing any modification. This saves on memory " +
                 "and computation")]
        protected bool _applyModifier = true;

        private static TData InvalidAsset { get; } = new TData();
        private TData _thisDataAsset;
        private TData _currentDataAsset = InvalidAsset;

        protected override TData DataAsset => _currentDataAsset;

        /// <summary>
        /// Returns the <see cref="IDataSource{TData}"/> from which this DataModifier retrieves the <typeparamref name="TData"/> it
        /// modifies. This source is typically set through the Unity Editor, but it can also be set programmatically using
        /// <see cref="InjectModifyDataFromSource(IDataSource{TData})"/>.
        /// </summary>
        public virtual IDataSource<TData> ModifyDataFromSource => _modifyDataFromSource == null
            ? (_modifyDataFromSource = _iModifyDataFromSourceMono as IDataSource<TData>)
            : _modifyDataFromSource;

        /// <summary>
        /// Implementation of <see cref="IDataSource.CurrentDataVersion"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public override int CurrentDataVersion
        {
            get
            {
                return _applyModifier
                    ? base.CurrentDataVersion
                    : ModifyDataFromSource.CurrentDataVersion;
            }
        }

        /// <summary>
        /// Changes the source from which this modifier retrieves the data it modifies, the source for updates, and the
        /// <see cref="DataSource{TData}.UpdateModeFlags"/>.
        /// </summary>
        /// <param name="modifyDataFromSource">The source from which this modifier retrieves the data it modifies</param>
        /// <param name="updateAfter">The <see cref="IDataSource"/> after which this modifier should be updated</param>
        /// <param name="updateMode">The <see cref="DataSource{TData}.UpdateModeFlags"/> to use from now on</param>
        /// <remarks>
        /// Typically, the same value is passed as both <paramref name="modifyDataFromSource"/> and <paramref name="updateAfter"/>
        /// so that the modifier is updated whenever and immediately after the source from which it retrieves the unmodified data
        /// acquires new data to modify.
        /// </remarks>
        public void ResetSources(IDataSource<TData> modifyDataFromSource, IDataSource updateAfter, UpdateModeFlags updateMode)
        {
            ResetUpdateAfter(updateAfter, updateMode);
            _modifyDataFromSource = modifyDataFromSource;
            _currentDataAsset = InvalidAsset;
        }

        protected override void UpdateData()
        {
            if (_applyModifier)
            {
                if (_thisDataAsset == null)
                {
                    _thisDataAsset = new TData();
                }

                _thisDataAsset.CopyFrom(ModifyDataFromSource.GetData());
                _currentDataAsset = _thisDataAsset;
                Apply(_currentDataAsset);
            }
            else
            {
                _currentDataAsset = ModifyDataFromSource.GetData();
            }
        }

        protected abstract void Apply(TData data);

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(ModifyDataFromSource, nameof(ModifyDataFromSource));
            this.EndStart(ref _started);
        }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated DataModifier; effectively wraps
        /// <see cref="DataSource{TData}.InjectAllDataSource(DataSource{TData}.UpdateModeFlags, IDataSource)"/>,
        /// <see cref="InjectModifyDataFromSource(IDataSource{TData})"/>, and <see cref="InjectApplyModifier(bool)"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity
        /// Editor-based usage.
        /// </summary>
        public void InjectAllDataModifier(UpdateModeFlags updateMode, IDataSource updateAfter, IDataSource<TData> modifyDataFromSource, bool applyModifier)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectModifyDataFromSource(modifyDataFromSource);
            InjectApplyModifier(applyModifier);
        }

        /// <summary>
        /// Sets the <see cref="IDataSource{TData}"/> for unmodified data on a dynamically instantiated DataModifier. This method exists
        /// to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        /// <param name="updateMode"></param>
        public void InjectModifyDataFromSource(IDataSource<TData> modifyDataFromSource)
        {
            _modifyDataFromSource = modifyDataFromSource;
            _iModifyDataFromSourceMono = modifyDataFromSource as Object;
        }

        /// <summary>
        /// Sets whether or not to apply modification on a dynamically instantiated DataSource. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        /// <param name="updateMode"></param>
        public void InjectApplyModifier(bool applyModifier)
        {
            _applyModifier = applyModifier;
        }
        #endregion
    }
}
