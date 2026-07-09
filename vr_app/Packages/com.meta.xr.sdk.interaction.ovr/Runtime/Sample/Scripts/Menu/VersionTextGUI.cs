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
using TMPro;
using UnityEngine;

namespace Oculus.Interaction
{
    [Obsolete("Use " + nameof(VersionTextVisual) + " instead")]
    public class VersionTextGUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        protected bool _started = false;
        protected virtual void Reset()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        protected virtual void Start()
        {
            this.AssertField(_text, nameof(_text));
            _text.text = "Version: " + Application.version;
        }

        #region Inject
        public void InjectAllVersionTextGuiVisual(TextMeshProUGUI text)
        {
            InjectTextUI(text);
        }

        public void InjectTextUI(TextMeshProUGUI text)
        {
            _text = text;
        }
        #endregion
    }
}
