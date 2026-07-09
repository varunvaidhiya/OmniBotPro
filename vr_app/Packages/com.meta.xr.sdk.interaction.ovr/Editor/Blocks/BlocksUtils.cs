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
using Meta.XR.BuildingBlocks.Editor;
using UnityEngine;
using System.Linq;



namespace Oculus.Interaction.Editor.BuildingBlocks
{
    internal static class BlocksUtils
    {
        public static void UpdateForAutoWiring(GameObject gameObject)
        {
            UnityObjectAddedBroadcaster.HandleObjectWasAdded(gameObject);

        }


        public static IEnumerable<Hand> GetHands()
        {
            var handsBlock = Meta.XR.BuildingBlocks.Editor.Utils.GetBlock(BlockDataIds.InteractionHandTracking);
            if (handsBlock == null)
            {
                yield break;
            }

            var hands = handsBlock.GetComponentsInChildren<Hand>();
            if (hands == null || hands.Length == 0)
            {
                yield break;
            }

            foreach (var hand in hands)
            {
                if (hand == null)
                {
                    continue;
                }

                yield return hand;
            }
        }

        public static IEnumerable<Controller> GetControllers()
        {
            var interactionBlock = Meta.XR.BuildingBlocks.Editor.Utils.GetBlock(BlockDataIds.InteractionControllerTracking);
            if (interactionBlock == null)
            {
                return Enumerable.Empty<Controller>();
            }

            return interactionBlock.GetComponentsInChildren<Controller>();
        }
    }
}
