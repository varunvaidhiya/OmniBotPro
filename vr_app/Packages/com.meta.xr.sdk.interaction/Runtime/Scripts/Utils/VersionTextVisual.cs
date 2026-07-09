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

using TMPro;
using UnityEngine;

namespace Oculus.Interaction
{
    public class VersionTextVisual : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _text;

        [SerializeField]
        private string _format = "Version: {0}";

        #region Editor

        protected virtual void Reset()
        {
            _text = GetComponent<TMP_Text>();
        }

        #endregion

        protected virtual void Start()
        {
            this.AssertField(_text, nameof(_text));
            this.AssertField(_format, nameof(_format));
            _text.text = string.Format(_format, Application.version);
        }

        #region Inject

        public void InjectAllVersionTextVisual(TMP_Text text, string format)
        {
            InjectText(text);
            InjectFormat(format);
        }

        public void InjectText(TMP_Text text)
        {
            _text = text;
        }

        public void InjectFormat(string format)
        {
            _format = format;
        }
        #endregion
    }
}
