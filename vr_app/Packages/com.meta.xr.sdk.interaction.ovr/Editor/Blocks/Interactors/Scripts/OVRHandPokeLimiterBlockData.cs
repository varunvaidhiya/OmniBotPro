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

using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;

namespace Oculus.Interaction.Editor.BuildingBlocks
{
    internal class OVRHandPokeLimiterBlockData : BlockData
    {
        protected override bool UsesPrefab => false;
        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
            var pokeLimiters = new List<GameObject>();
            foreach (var hand in BlocksUtils.GetHands())
            {
                if (!TryGetSyntheticHand(hand, out var syntheticHand)) continue;

                var handPokeInteractor = hand.GetComponentInChildren<PokeInteractor>();
                var handPokeLimiterVisual = hand.GetComponentInChildren<HandPokeLimiterVisual>(true);
                handPokeLimiterVisual.gameObject.SetActive(true);
                handPokeLimiterVisual.InjectAllHandPokeLimiterVisual(hand, handPokeInteractor, syntheticHand);
                Undo.RegisterCompleteObjectUndo(handPokeLimiterVisual, $"Setup {nameof(HandPokeLimiterVisual)}");

                var syntheticHandVisual = syntheticHand.GetComponentInChildren<HandVisual>();
                var handRenderer = syntheticHand.GetComponentInChildren<SkinnedMeshRenderer>();
                var materialEditor =
                    syntheticHand.GetComponentInChildren<MaterialPropertyBlockEditor>();
                var overshootGlow = hand.GetComponentInChildren<HandPokeOvershootGlow>(true);
                overshootGlow.gameObject.SetActive(true);
                overshootGlow.InjectAllHandPokeOvershootGlow(hand, handPokeInteractor, syntheticHandVisual, handRenderer, materialEditor);
                Undo.RegisterCompleteObjectUndo(overshootGlow, $"Setup {nameof(HandPokeOvershootGlow)}");

                pokeLimiters.Add(handPokeLimiterVisual.gameObject);
            }
            return pokeLimiters;
        }

        private bool TryGetSyntheticHand(Hand hand, out SyntheticHand expectedSyntheticHand)
        {
            var blocks = Meta.XR.BuildingBlocks.Editor.Utils.GetBlocks(BlockDataIds.SyntheticHandBlockData);
            expectedSyntheticHand = blocks.Select(block => block.GetComponent<SyntheticHand>()).FirstOrDefault(syntheticHand => syntheticHand.Handedness == hand.Handedness);
            return expectedSyntheticHand != null;
        }
    }
}
