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
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

#if USING_XR_SDK_OPENXR
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
#endif

namespace Meta.XR
{
#if UNITY_OPENXR_PLUGIN_1_17_0_OR_NEWER
#if UNITY_EDITOR
    [MetaOpenXRFeature(
        featureId: featureId,
        uiName: "Oculus Touch Controller Proximity Interaction",
        desc: "Adds proximity interaction paths for trigger and thumb on the Oculus Touch Controller Interaction Profile.",
        docsLink: "https://developers.meta.com/horizon/documentation/unity/unity-ovrinput/#button-touch-and-neartouch",
        version: "0.0.1",
        targetApiVersion: "1.1.45",
        extensions: new[] { "XR_FB_touch_controller_proximity" })]
#endif
    public class OculusTouchControllerProximityProfile : OpenXRInteractionFeature
    {
        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "com.meta.openxr.feature.input.oculustouch.proximity";

        protected override bool IsAdditive => true;

        [Preserve, InputControlLayout(displayName = "Oculus Touch Controller Proximity (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
        public class OculusTouchControllerProximity : XRController
        {
            [Preserve, InputControl(usage = "TriggerProximity")]
            public ButtonControl triggerProximity { get; private set; }

            [Preserve, InputControl(usage = "ThumbProximity")]
            public ButtonControl thumbProximity { get; private set; }

            /// <summary>
            /// Internal call used to assign controls to the the correct element.
            /// </summary>
            protected override void FinishSetup()
            {
                base.FinishSetup();
                triggerProximity = GetChildControl<ButtonControl>("triggerProximity");
                thumbProximity = GetChildControl<ButtonControl>("thumbProximity");
            }
        }

        public const string triggerProximity = "/input/trigger/proximity_fb";

        public const string thumbProximity = "/input/thumb_fb/proximity_fb";

        public const string profile = "/interaction_profiles/oculus/touch_controller";

        private const string kDeviceLocalizedName = "Oculus Touch Controller Proximity Interaction OpenXR";

        /// <inheritdoc/>
        protected override bool OnInstanceCreate(ulong instance)
        {
            return base.OnInstanceCreate(instance);
        }

        protected override void RegisterDeviceLayout()
        {
            InputSystem.RegisterLayout(typeof(OculusTouchControllerProximity),
                            matches: new InputDeviceMatcher()
                                .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                                .WithProduct(kDeviceLocalizedName));
        }

        protected override void UnregisterDeviceLayout()
        {
            InputSystem.RemoveLayout(nameof(OculusTouchControllerProximity));
        }

        protected override string GetDeviceLayoutName()
        {
            return nameof(OculusTouchControllerProximity);
        }

        protected override void RegisterActionMapsWithRuntime()
        {
            ActionMapConfig actionMap = new ActionMapConfig()
            {
                name = "proximityinteraction",
                localizedName = kDeviceLocalizedName,
                desiredInteractionProfile = profile,
                manufacturer = "Meta",
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>(),
                actions = new List<ActionConfig>()
                {
                    //Trigger Proximity
                    new ActionConfig()
                    {
                        name = "triggerProximity",
                        localizedName = "Trigger Proximity",
                        type = ActionType.Binary,
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = triggerProximity,
                                interactionProfileName = profile,
                            }
                        },
                        isAdditive = true
                    },
                    //Thumb Proximity
                    new ActionConfig()
                    {
                        name = "thumbProximity",
                        localizedName = "Thumb Proximity",
                        type = ActionType.Binary,
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = thumbProximity,
                                interactionProfileName = profile,
                            }
                        },
                        isAdditive = true
                    },
                }
            };

            AddActionMap(actionMap);
        }

        protected override void AddAdditiveActions(List<OpenXRInteractionFeature.ActionMapConfig> actionMaps, ActionMapConfig additiveMap)
        {
            foreach (var actionMap in actionMaps)
            {
                //Add  proximity paths to oculus touch controller
                if (actionMap.desiredInteractionProfile != OculusTouchControllerProfile.profile)
                    continue;

                foreach (var additiveAction in additiveMap.actions)
                {
                    if (additiveAction.isAdditive)
                        actionMap.actions.Add(additiveAction);
                }
            }
        }
    }
#endif // USING_XR_SDK_OPENXR
}
