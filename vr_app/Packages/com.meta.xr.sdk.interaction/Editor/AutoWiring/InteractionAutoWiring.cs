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
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.PoseDetection;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    [InitializeOnLoad]
    public static class InteractionAutoWiring
    {
        static InteractionAutoWiring()
        {
            AutoWiring.Register(
                typeof(HandRef),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );

            AutoWiring.Register(
                typeof(HmdRef),
                new[] {
                    new ComponentWiringStrategyConfig("_hmd", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors,
                            FieldWiringStrategies.WireFieldToSceneComponent
                        })
                }
            );
            AutoWiring.Register(
                typeof(ControllerRef),
                new[]
                {
                    new ComponentWiringStrategyConfig("_controller", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );

            AutoWiring.Register(
                typeof(FingerFeatureStateProvider),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                }
            );

            AutoWiring.Register(
                typeof(ShapeRecognizerActiveState),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_fingerFeatureStateProvider", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors,
                            FieldWiringStrategies.WireFieldToNearestComponent
                        })
                }
            );

            AutoWiring.Register(
                typeof(TransformFeatureStateProvider),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_trackingToWorldTransformer", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors,
                            FieldWiringStrategies.WireFieldToNearestComponent

                        }),
                    new ComponentWiringStrategyConfig("_hmd", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors,
                            FieldWiringStrategies.WireFieldToSceneComponent
                        })
                }
            );

            AutoWiring.Register(
                typeof(JointDeltaProvider),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                }
            );

            AutoWiring.Register(
                typeof(TransformTrackingToWorldTransformer),
                new[] {
                    new ComponentWiringStrategyConfig("TrackingSpace", new FieldWiringStrategy[]
                        {
                            (monoBehaviour, field, targetType) =>
                                FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace"),
                            (monoBehaviour, field, targetType) =>
                                FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, Camera.main, "/..")
                        })
                }
            );

            #region HandGrab

            AutoWiring.Register(
                typeof(HandGrab.HandGrabInteractable),
                new[] {
                    new ComponentWiringStrategyConfig("_rigidbody", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_pointableElement", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_physicsGrabbable", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );

            AutoWiring.Register(
                typeof(HandGrab.HandGrabStateVisual),
                new[] {
                    new ComponentWiringStrategyConfig("_syntheticHand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToNearestComponent
                        })
                }
            );

            AutoWiring.Register(
                typeof(TouchHandGrabInteractorVisual),
                new[] {
                    new ComponentWiringStrategyConfig("_syntheticHand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToNearestComponent
                        })
                }
            );
            #endregion

            #region Poke

            AutoWiring.Register(
                typeof(HandPokeLimiterVisual),
                new[] {
                    new ComponentWiringStrategyConfig("_syntheticHand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToNearestComponent
                        })
                }
            );

            #endregion

            #region Locomotion

            AutoWiring.Register(
                typeof(LocomotionTurnerInteractor),
                new[] {
                    new ComponentWiringStrategyConfig("_transformer", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                }
            );

            AutoWiring.Register(
                typeof(TunnelingEffect),
                new[] {
                    new ComponentWiringStrategyConfig("_centerEyeCamera", new FieldWiringStrategy[]
                        {
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace/CenterEyeAnchor"),
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, Camera.main)
                        }),
                }
            );

            AutoWiring.Register(
                typeof(PlayerLocomotor),
                new[] {
                    new ComponentWiringStrategyConfig("_playerOrigin", new FieldWiringStrategy[]
                        {
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace/.."),
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, Camera.main, "/../..")
                        }),
                    new ComponentWiringStrategyConfig("_playerHead", new FieldWiringStrategy[]
                        {
                             (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace/CenterEyeAnchor"),
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, Camera.main)
                        })
                }
            );

            AutoWiring.Register(
                typeof(FirstPersonLocomotor),
                new[] {
                    new ComponentWiringStrategyConfig("_playerOrigin", new FieldWiringStrategy[]
                        {
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace/../..", "TrackingSpace/.."),
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, Camera.main, "/../../..", "/../..")
                        }),
                    new ComponentWiringStrategyConfig("_playerEyes", new FieldWiringStrategy[]
                        {
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace/CenterEyeAnchor"),
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType,  Camera.main)
                        })
                }
            );

            AutoWiring.Register(
                typeof(FlyingLocomotor),
                new[] {
                    new ComponentWiringStrategyConfig("_playerOrigin", new FieldWiringStrategy[]
                        {
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace/../..", "TrackingSpace/.."),
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType,  Camera.main, "/../../..", "/../..")
                        }),
                    new ComponentWiringStrategyConfig("_playerEyes", new FieldWiringStrategy[]
                        {
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType, "TrackingSpace/CenterEyeAnchor"),
                            (MonoBehaviour monoBehaviour, FieldInfo field, System.Type targetType)
                                => FieldWiringStrategies.WireFieldToPathComponent(monoBehaviour, field, targetType,  Camera.main)
                        })
                }
            );

            #endregion

            #region TouchHandGrab

            AutoWiring.Register(
                typeof(FromHandPrefabDataSource),
                new[] {
                    new ComponentWiringStrategyConfig("_handSkeletonProvider", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToSceneComponent
                        }),
                }
            );
            #endregion

        }
    }
}
