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

using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.Input;
using Oculus.Interaction.Input.Visuals;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Editor.BuildingBlocks
{
    internal class OVRInteractionControllerTrackingBlockData : OVRInteractionBaseBlockData
    {
        protected override bool TryGetPreexistingNonBlock(out GameObject nonBlockObject)
        {
            return TryGetInteractorBase<Controller>(InteractorUtils.CONTROLLER_INTERACTOR_PARENT_NAME,
                out nonBlockObject);
        }

        protected override void ApplyAdditionalHooks(GameObject blockObject)
        {
            var controllerTrackingBlocks =
                Meta.XR.BuildingBlocks.Editor.Utils.GetBlocks(Meta.XR.BuildingBlocks.Editor.BlockDataIds
                    .ControllerTracking);

            var controllerHelpers = new Dictionary<Handedness, OVRControllerHelper>();
            foreach (var controllerTracking in controllerTrackingBlocks)
            {
                var controllerHelper = controllerTracking.GetComponent<OVRControllerHelper>();
                if (controllerHelper == null)
                {
                    continue;
                }

                var trackingHandedness = controllerHelper.m_controller ==
                                         OVRInput.Controller.LTouch
                    ? Handedness.Left
                    : Handedness.Right;
                controllerHelpers.TryAdd(trackingHandedness, controllerHelper);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var controllerVisual in blockObject.GetComponentsInChildren<OVRControllerVisual>())
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var controller = controllerVisual.GetComponentInParent<Controller>();
                if (controller == null)
                {
                    continue;
                }

                if (controllerHelpers.TryGetValue(controller.Handedness, out var controllerHelper))
                {
                    controllerVisual.InjectAllOVRControllerHelper(controllerHelper);
                }
            }
        }
    }
}
