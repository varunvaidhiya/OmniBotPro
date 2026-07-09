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
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    [InitializeOnLoad]
    public static class OVRAutoWiring
    {
        static OVRAutoWiring()
        {
            AutoWiring.Register(
                typeof(OVRCameraRigRef),
                new[] {
                    new ComponentWiringStrategyConfig("_ovrCameraRig", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_leftHand", new FieldWiringStrategy[]
                        {
                            (m,f,t) => WireToOVRHandWithHandedness(m, f, t, OVRPlugin.Hand.HandLeft)
                        }),
                    new ComponentWiringStrategyConfig("_rightHand", new FieldWiringStrategy[]
                        {
                            (m,f,t) => WireToOVRHandWithHandedness(m, f, t, OVRPlugin.Hand.HandRight)
                        })
                }
            );

            AutoWiring.Register(
                typeof(FromOVRHandDataSource),
                new[] {
                    new ComponentWiringStrategyConfig("_cameraRigRef", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_trackingToWorldTransformer", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );

            AutoWiring.Register(
                typeof(FromOVRControllerDataSource),
                new[] {
                    new ComponentWiringStrategyConfig("_cameraRigRef", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_trackingToWorldTransformer", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );

            AutoWiring.Register(
                typeof(OVRMicrogestureEventSource),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            WireToOVRHandWithHandedness
                        })
                }
            );
        }

        public static bool WireToOVRHandWithHandedness(MonoBehaviour monoBehaviour,
            FieldInfo field, System.Type targetType)
        {
            if (targetType != typeof(OVRHand))
            {
                return false;
            }

            if (!TryGetHandedness(monoBehaviour.gameObject, out Handedness handedness))
            {
                return FieldWiringStrategies.WireFieldToSceneComponent(monoBehaviour, field, targetType);
            }

            OVRPlugin.Hand ovrHandedness = handedness == Handedness.Left ?
                OVRPlugin.Hand.HandLeft : OVRPlugin.Hand.HandRight;

            return WireToOVRHandWithHandedness(monoBehaviour, field, targetType, ovrHandedness);
        }

        public static bool WireToOVRHandWithHandedness(MonoBehaviour monoBehaviour,
            FieldInfo field, System.Type targetType, OVRPlugin.Hand ovrHandedness)
        {
            if (targetType != typeof(OVRHand))
            {
                return false;
            }

            OVRHand hand = Object.FindObjectsByType<OVRHand>(FindObjectsSortMode.InstanceID)
                 .Where(hand => hand.gameObject.scene == monoBehaviour.gameObject.scene)
                 .FirstOrDefault(hand => hand.GetHand() == ovrHandedness);

            if (hand != null)
            {
                field.SetValue(monoBehaviour, hand);
                EditorUtility.SetDirty(monoBehaviour);
                return true;
            }

            return false;
        }


        private static bool TryGetHandedness(GameObject origin, out Handedness handedness)
        {
            handedness = Handedness.Right;
            Transform transform = origin.transform;

            while (transform != null)
            {
                if (transform.TryGetComponent(out Hand hand))
                {
                    //during editor time the Hand hierarchy has not been wired yet
                    //so we can only "trust" the naming, as the Handedness value
                    //is not initialised yet and will always return Left.
                    string name = hand.name.ToLower();
                    if (name.Contains("left"))
                    {
                        handedness = Handedness.Left;
                        return true;
                    }
                    else if (name.Contains("right"))
                    {
                        handedness = Handedness.Right;
                        return true;
                    }
                    return false;
                }
                transform = transform.parent;
            }

            return false;
        }
    }
}
