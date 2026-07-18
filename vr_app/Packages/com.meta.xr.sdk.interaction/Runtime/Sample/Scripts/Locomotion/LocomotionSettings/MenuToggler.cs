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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Component to show a panel gameobject placing it in
    /// front of the user. It can be controller externally
    /// and closed with a button.
    /// </summary>
    public class MenuToggler : MonoBehaviour
    {
        /// <summary>
        /// The actual panel gameobject, it's transform will be
        /// modified when activated to be in front of the user
        /// </summary>
        [SerializeField]
        private GameObject _panel;
        /// <summary>
        /// Optiona. Close button  to listen for OnClick so the
        /// panel closes inmediately
        /// </summary>
        [SerializeField, Optional]
        private Button _closeButton;
        /// <summary>
        /// The head transform of the user
        /// </summary>
        [SerializeField]
        private Transform _headAnchor;
        public Transform HeadAnchor
        {
            get => _headAnchor;
            set => _headAnchor = value;
        }
        /// <summary>
        /// Local offset from the headAnchor to place the panel
        /// when activated
        /// </summary>
        [SerializeField]
        private Vector3 _spawnOffset = new Vector3(0f, -0.1f, 0.3f);
        public Vector3 SpawnOffset
        {
            get => _spawnOffset;
            set => _spawnOffset = value;
        }

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_panel, nameof(_panel));
            this.AssertField(_headAnchor, nameof(_headAnchor));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                if (_closeButton != null)
                {
                    _closeButton.onClick.AddListener(HidePanel);
                }
                if (!_panel.activeSelf)
                {
                    HidePanel();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_closeButton != null)
                {
                    _closeButton.onClick.RemoveListener(HidePanel);
                }
            }
        }

        /// <summary>
        /// Deactivates the panel if it is active and vice versa
        /// </summary>
        public void TogglePanel()
        {
            if (_panel.activeSelf)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }

        /// <summary>
        /// Deactivates the panel
        /// </summary>
        public void HidePanel()
        {
            _panel.SetActive(false);
        }

        /// <summary>
        /// Activates the panel and places it in front of the user's head
        /// </summary>
        public void ShowPanel()
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(_headAnchor.forward, Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(flatForward);
            Vector3 position = _headAnchor.position + rotation * _spawnOffset;
            rotation = rotation * Quaternion.Euler(15f, 0f, 0f);
            Pose pose = new Pose(position, rotation);

            _panel.transform.SetPose(pose);
            _panel.SetActive(true);
        }

        #region Injects

        public void InjectAllAUIToggler(GameObject panel)
        {
            InjectPanel(panel);
        }

        public void InjectPanel(GameObject panel)
        {
            _panel = panel;
        }

        public void InjectOptionalCloseButton(Button closeButton)
        {
            _closeButton = closeButton;
        }

        #endregion
    }
}
