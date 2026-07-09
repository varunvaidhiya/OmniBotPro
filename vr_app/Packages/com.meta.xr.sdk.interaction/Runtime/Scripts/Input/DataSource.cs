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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Interface expressing that an instance is an integration point from which data can be retrieved. Any type
    /// that is a provider of a certain type of data (forwarding <see cref="HandDataAsset"/> data from the system as done by
    /// <see cref="Hand"/>, altering and re-forwarding <see cref="HandDataAsset"/> data based on scene state as done by
    /// <see cref="SyntheticHand"/>, etc.) should be a DataSource for that kind of data.
    /// </summary>
    /// <remarks>
    /// This is a base interface for many different types supplying many different types of data throughout the Interaction SDK.
    /// For example, <see cref="Hand"/>s are sources for <see cref="HandDataAsset"/>s, while <see cref="Controller"/>s are sources
    /// for <see cref="ControllerDataAsset"/>.
    /// </remarks>
    public interface IDataSource
    {
        /// <summary>
        /// Indicator which is incremented every time new data becomes available.
        /// </summary>
        /// <remarks>
        /// Exceptionally high-frequency data in exceptionally long-running experiences can potentially overflow this value,
        /// leading to undefined behavior. While this is not expected to be a factor in typical cases, wildly high-frequency
        /// scenarios may need to destroy and recreate their IDataSources periodically to avoid hitting this limit.
        /// </remarks>
        int CurrentDataVersion { get; }

        /// <summary>
        /// Marks the data asset (for example, <see cref="HandDataAsset"/>) stored as outdated, which means it should be
        /// re-processed the next time data is retrieved from this source.
        /// </summary>
        void MarkInputDataRequiresUpdate();

        /// <summary>
        /// Notifies observers that new data is available from this source. Data is typically retrieved by calling
        /// <see cref="IDataSource{TData}.GetData"/>.
        /// </summary>
        /// <remarks>
        /// If observing a <see cref="Hand"/> instance, use <see cref="Hand.WhenHandUpdated"/> instead of this event.
        /// </remarks>
        event Action InputDataAvailable;
    }

    /// <summary>
    /// Interface expressing that an instance is an integration point from which data can be retrieved. This is a conceptual
    /// increment on <see cref="IDataSource"/>, adding only a specification of the type of data provided and an accessor to
    /// retrieve that data.
    /// </summary>
    public interface IDataSource<TData> : IDataSource
    {
        /// <summary>
        /// Retrieves the <typeparamref name="TData"/> currently contained by this source. If
        /// <see cref="IDataSource.MarkInputDataRequiresUpdate"/> has been called since the last invocation of this method,
        /// all required work to lazily update the contained data will be executed within this call.
        /// </summary>
        /// <returns>The <typeparamref name="TData"/> currently contained by this source</returns>
        TData GetData();
    }

    /// <summary>
    /// Base class for most concrete <see cref="IDataSource"/> types. Conceptually, any type which produces data can be an
    /// <see cref="IDataSource"/> for that data type, but in practice usage is more constrained (though still pervasive throughout
    /// the Interaction SDK). Descendent types of this DataSource implementation specifically are also MonoBehaviours; thus, the
    /// expectation for descendent types is that they will be long-lived instances which persistently provide data over a period of
    /// time, on a schedule specified by <see cref="UpdateModeFlags"/> but typically tied in some way into MonoBehaviour's built-in
    /// update capabilites.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public abstract class DataSource<TData> : MonoBehaviour, IDataSource<TData>
        where TData : class, ICopyFrom<TData>, new()
    {
        /// <summary>
        /// Indicates whether or not the MonoBehaviour start-up process has completed for this instance.
        /// </summary>
        public bool Started => _started;
        protected bool _started = false;
        private bool _requiresUpdate = true;

        /// <summary>
        /// Enumeration controlling when this <see cref="IDataSource"/> updates its data. This enum is intended to be used as a bit
        /// mask, meaning multiple different update modes can be enabled at the same time.
        /// </summary>
        [Flags]
        public enum UpdateModeFlags
        {
            Manual = 0,
            UnityUpdate = 1 << 0,
            UnityFixedUpdate = 1 << 1,
            UnityLateUpdate = 1 << 2,
            AfterPreviousStep = 1 << 3
        }

        [Header("Update")]
        [SerializeField]
        private UpdateModeFlags _updateMode;

        /// <summary>
        /// Returns the <see cref="UpdateModeFlags"/> specifying the circumstances under which the data contained in this instance
        /// will be updated.
        /// </summary>
        public UpdateModeFlags UpdateMode => _updateMode;

        [SerializeField, Interface(typeof(IDataSource))]
        [Optional(OptionalAttribute.Flag.DontHide)]
        private UnityEngine.Object _updateAfter;

        private IDataSource UpdateAfter;
        private int _currentDataVersion;

        protected bool UpdateModeAfterPrevious => (_updateMode & UpdateModeFlags.AfterPreviousStep) != 0;

        /// <summary>
        /// Implementation of <see cref="IDataSource.InputDataAvailable"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public event Action InputDataAvailable = delegate { };

        /// <summary>
        /// Implementation of <see cref="IDataSource.CurrentDataVersion"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public virtual int CurrentDataVersion => _currentDataVersion;

        #region Unity Lifecycle
        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            if (_updateAfter != null)
            {
                UpdateAfter = _updateAfter as IDataSource;
                this.AssertField(UpdateAfter, nameof(UpdateAfter));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                if (UpdateModeAfterPrevious && UpdateAfter != null)
                {
                    UpdateAfter.InputDataAvailable += MarkInputDataRequiresUpdate;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (UpdateAfter != null)
                {
                    UpdateAfter.InputDataAvailable -= MarkInputDataRequiresUpdate;
                }
            }
        }

        protected virtual void Update()
        {
            if ((_updateMode & UpdateModeFlags.UnityUpdate) != 0)
            {
                MarkInputDataRequiresUpdate();
            }
        }

        protected virtual void FixedUpdate()
        {
            if ((_updateMode & UpdateModeFlags.UnityFixedUpdate) != 0)
            {
                MarkInputDataRequiresUpdate();
            }
        }

        protected virtual void LateUpdate()
        {
            if ((_updateMode & UpdateModeFlags.UnityLateUpdate) != 0)
            {
                MarkInputDataRequiresUpdate();
            }
        }
        #endregion

        protected void ResetUpdateAfter(IDataSource updateAfter, UpdateModeFlags updateMode)
        {
            bool wasActive = isActiveAndEnabled;
            if (isActiveAndEnabled) { OnDisable(); }

            _updateMode = updateMode;
            UpdateAfter = updateAfter;
            _requiresUpdate = true;
            _currentDataVersion += 1;

            if (wasActive) { OnEnable(); }
        }

        /// <summary>
        /// Implementation of <see cref="IDataSource{TData}.GetData"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public TData GetData()
        {
            if (RequiresUpdate())
            {
                UpdateData();
                _requiresUpdate = false;
            }

            return DataAsset;
        }

        protected bool RequiresUpdate()
        {
            return _requiresUpdate;
        }

        /// <summary>
        /// Implementation of <see cref="IDataSource.MarkInputDataRequiresUpdate"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public virtual void MarkInputDataRequiresUpdate()
        {
            _requiresUpdate = true;
            _currentDataVersion += 1;
            InputDataAvailable();
        }

        protected abstract void UpdateData();

        /// <summary>
        /// Returns the current DataAsset, without performing any updates.
        /// </summary>
        /// <returns>
        /// Null if no call to GetData has been made since this data source was initialized.
        /// </returns>
        protected abstract TData DataAsset { get; }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated DataSource; effectively wraps
        /// <see cref="InjectUpdateMode(UpdateModeFlags)"/> and <see cref="InjectUpdateAfter(IDataSource)"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity
        /// Editor-based usage.
        /// </summary>
        public void InjectAllDataSource(UpdateModeFlags updateMode, IDataSource updateAfter)
        {
            InjectUpdateMode(updateMode);
            InjectUpdateAfter(updateAfter);
        }

        /// <summary>
        /// Sets the <see cref="UpdateModeFlags"/> for a dynamically instantiated DataSource. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        /// <param name="updateMode"></param>
        public void InjectUpdateMode(UpdateModeFlags updateMode)
        {
            _updateMode = updateMode;
        }

        /// <summary>
        /// Sets the <see cref="IDataSource"/> to update after for a dynamically instantiated DataSource. This method exists to
        /// support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectUpdateAfter(IDataSource updateAfter)
        {
            _updateAfter = updateAfter as UnityEngine.Object;
            UpdateAfter = updateAfter;
        }
        #endregion
    }
}
