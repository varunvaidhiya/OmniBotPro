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

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// A FeedbackAction ScriptableObject that plays a predefined haptic pattern.
    /// </summary>
    [CreateAssetMenu(menuName = "Meta/Interaction SDK/Feedback/Haptic")]
    public class HapticActionSO : FeedbackActionSO
    {
        [Tooltip("The haptic pattern to play when this action is executed.")]
        [SerializeField]
        private HapticPattern m_pattern = new HapticPattern(0.4f, 0.08f, 1f);

        /// <summary>
        /// Executes the haptic feedback action by telling the FeedbackManager to play the defined pattern.
        /// </summary>
        /// <param name="identifier">The <see cref="IInteractorView"/>'s unique identifier</param>
        /// <param name="source">The GameObject that was interacted with (ignored by this specific action).</param>
        /// <param name="manager">The FeedbackManager instance used to play haptics.</param>
        public override void Execute(int identifier, GameObject source, FeedbackManager manager)
        {
            manager.PlayHaptics(identifier, m_pattern);
        }
    }
}
