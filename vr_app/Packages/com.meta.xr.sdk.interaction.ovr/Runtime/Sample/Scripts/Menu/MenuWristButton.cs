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
using UnityEngine.UI;

namespace Oculus.Interaction.Samples
{
    /// <summary>
    /// The menu wrist button uses the unity UI toggle to show/hide the ISDK menu panel
    /// </summary>
    public class MenuWristButton : MonoBehaviour
    {
        /// <summary>
        /// The Toggle Button
        /// </summary>
        [Header("The Toggle Button")]
        [Tooltip("Place the toggle on the wrist here")]
        [SerializeField] private Toggle _toggle;

        /// <summary>
        /// The Menu Manager
        /// </summary>
        [Header("The Menu Manager")]
        [Tooltip("There should only be 1 ISDK manager in the scene loacted on the ISDKMenuManager.prefab")]
        [SerializeField] private ISDKSceneMenuManager _menuManager;

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_toggle, nameof(_toggle));
            this.AssertField(_menuManager, nameof(_menuManager));

            this.EndStart(ref _started);

        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        private void OnToggleValueChanged(bool value)
        {
            _menuManager.ToggleMenu();
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }

        #region Inject
        public void InjectAllMenuWrist(Toggle toggle, ISDKSceneMenuManager manager)
        {
            InjectToggle(toggle);
            InjectManager(manager);
        }

        public void InjectToggle(Toggle toggle)
        {
            _toggle = toggle;
        }

        public void InjectManager(ISDKSceneMenuManager manager)
        {
            _menuManager = manager;
        }
        #endregion
    }
}
