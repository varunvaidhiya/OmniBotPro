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
using Oculus.Interaction.PoseDetection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    internal class HandPoseSelectorWizard : EditorWindow
    {
        private static readonly HandFinger[] Fingers = new HandFinger[]
        {
            HandFinger.Thumb,
            HandFinger.Index,
            HandFinger.Middle,
            HandFinger.Ring,
            HandFinger.Pinky,
        };

        private static readonly FingerFeature[] Features = new FingerFeature[]
        {
            FingerFeature.Curl,
            FingerFeature.Flexion,
            FingerFeature.Abduction,
            FingerFeature.Opposition,
        };

        private static readonly TransformFeature[] TransformFeatures = new TransformFeature[]
        {
            TransformFeature.WristUp,
            TransformFeature.WristDown,
            TransformFeature.PalmDown,
            TransformFeature.PalmUp,
            TransformFeature.PalmTowardsFace,
            TransformFeature.PalmAwayFromFace,
            TransformFeature.FingersUp,
            TransformFeature.FingersDown,
            TransformFeature.PinchClear
        };

        private const string DefaultTransformFeatureStateThresholdsGUID = "039cf5a7424e1e046b79287e9375cf09";

        [SerializeField]
        private KeyCode _recordKey = KeyCode.Space;

        [SerializeField]
        private string _newPoseName = "NewHandPoseSelector";

        private FingerFeatureStateProvider _fingerFeatureStateProvider;
        private TransformFeatureStateProvider _transformFeatureStateProvider;

#if UNITY_6000_3_OR_NEWER
        [SerializeField]
        private EntityId _fingerFeatureStateProviderInstanceId;

        [SerializeField]
        private EntityId _transformFeatureStateProviderInstanceId;

        [SerializeField]
        private EntityId _handInstanceId;
#else
        [SerializeField]
        private int _fingerFeatureStateProviderInstanceId;

        [SerializeField]
        private int _transformFeatureStateProviderInstanceId;

        [SerializeField]
        private int _handInstanceId;
#endif

        [SerializeField]
        private Vector3 _positionOffset;

        [SerializeField]
        private Vector3 _rotationOffset;

        [SerializeField]
        private UpVectorType _upVectorType;

        [SerializeField]
        private TransformFeatureStateThresholds _featureThresholds;

        [SerializeField]
        private bool _autoAddPrefabAfterRecording = false;

        [SerializeField]
        private string _prefabPathToAdd = null;

        [MenuItem("Meta/Interaction SDK/Hand Pose Selector Recorder")]
        private static void CreateWizard()
        {
            var window = EditorWindow.GetWindow<HandPoseSelectorWizard>();
            window.titleContent = new GUIContent("Hand Pose Selector Recorder");
            window.Show();
        }

        private GUIStyle _richTextStyle;
        private Vector2 _scrollPos = Vector2.zero;

        private void OnEnable()
        {
            _richTextStyle = EditorGUIUtility.GetBuiltinSkin(EditorGUIUtility.isProSkin ? EditorSkin.Scene : EditorSkin.Inspector).label;
            _richTextStyle.richText = true;
            _richTextStyle.wordWrap = true;

            _upVectorType = UpVectorType.World;
            var defaultThresholdsPath = AssetDatabase.GUIDToAssetPath(DefaultTransformFeatureStateThresholdsGUID);
            var defaultThresholdsResource = AssetDatabase.LoadAssetAtPath(defaultThresholdsPath, typeof(TransformFeatureStateThresholds));
            _featureThresholds = defaultThresholdsResource as TransformFeatureStateThresholds;

            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
        }

        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (Application.isPlaying)
            {
                IEnumerator coroutine()
                {
                    while (true)
                    {
                        if (UnityEngine.Input.GetKeyDown(_recordKey))
                        {
                            Context.Global.GetInstance().StartCoroutine(RecordPoseCoroutine());
                        }
                        yield return null;
                    }
                };
                Context.Global.GetInstance().StartCoroutine(coroutine());
            }
            else
            {
                if (_prefabPathToAdd != null && _prefabPathToAdd.Length > 0)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath(_prefabPathToAdd, typeof(GameObject));
                    var instantiation = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
#if UNITY_6000_3_OR_NEWER
                    var hand = EditorUtility.EntityIdToObject(_handInstanceId) as IHand;
                    var fingerFeatureStateProvider = EditorUtility.EntityIdToObject(_fingerFeatureStateProviderInstanceId) as FingerFeatureStateProvider;
                    var transformFeatureStateProvider = EditorUtility.EntityIdToObject(_transformFeatureStateProviderInstanceId) as TransformFeatureStateProvider;
#else
                    var hand = EditorUtility.InstanceIDToObject(_handInstanceId) as IHand;
                    var fingerFeatureStateProvider = EditorUtility.InstanceIDToObject(_fingerFeatureStateProviderInstanceId) as FingerFeatureStateProvider;
                    var transformFeatureStateProvider = EditorUtility.InstanceIDToObject(_transformFeatureStateProviderInstanceId) as TransformFeatureStateProvider;
#endif

                    instantiation.name = prefab.name;
                    instantiation.GetComponent<HandRef>().InjectAllHandRef(hand);
                    instantiation.GetComponent<FingerFeatureStateProviderRef>().InjectAllFingerFeatureStateProviderRef(fingerFeatureStateProvider);
                    instantiation.GetComponent<TransformFeatureStateProviderRef>().InjectAllTransformFeatureStateProviderRef(transformFeatureStateProvider);

                    _prefabPathToAdd = null;
                }
            }
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown
                && e.keyCode == _recordKey)
            {
                Context.Global.GetInstance().StartCoroutine(RecordPoseCoroutine());
                e.Use();
            }
            GUILayout.Label("Generate a new hand pose selector (i.e., \"pose recognizer\") <b>using your Hand in Play Mode</b>.", _richTextStyle);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            GUILayout.Label(
                "\n<size=20>1.</size>\t" +
                "Assign the feature providers that will provide the features for your new selector. These are usually in your scene " +
                "on a GameObject called \"HandFeaturesRight\" or \"HandFeaturesLeft.\" Both are commonly on the same GameObject; if " +
                "so, the second will auto-populate when you assign the first.\n\n" +
                "Note: If your scene contains more than one kind of hand input modality (tracked hands and controller-driven hands, " +
                "for example), there may be several of these. Take care to chose the ones for the actual hand you want to record.\n", _richTextStyle);

            _fingerFeatureStateProvider = EditorGUILayout.ObjectField(_fingerFeatureStateProvider, typeof(FingerFeatureStateProvider), true) as FingerFeatureStateProvider;
            _transformFeatureStateProvider = EditorGUILayout.ObjectField(_transformFeatureStateProvider, typeof(TransformFeatureStateProvider), true) as TransformFeatureStateProvider;

            if (_fingerFeatureStateProvider != null &&
                _transformFeatureStateProvider == null &&
                _fingerFeatureStateProvider.gameObject.GetComponent<TransformFeatureStateProvider>() != null)
            {
                _transformFeatureStateProvider = _fingerFeatureStateProvider.gameObject.GetComponent<TransformFeatureStateProvider>();
            }
            else if (_transformFeatureStateProvider != null &&
                _fingerFeatureStateProvider == null &&
                _transformFeatureStateProvider.gameObject.GetComponent<FingerFeatureStateProvider>() != null)
            {
                _fingerFeatureStateProvider = _transformFeatureStateProvider.gameObject.GetComponent<FingerFeatureStateProvider>();
            }

            if (_fingerFeatureStateProvider != null) _fingerFeatureStateProviderInstanceId = _fingerFeatureStateProvider.GetHashCode();
            if (_transformFeatureStateProvider != null) _transformFeatureStateProviderInstanceId = _transformFeatureStateProvider.GetHashCode();

            GUILayout.Label("\nOptionally, you can also choose a name for your new recording.\n");
            _newPoseName = EditorGUILayout.TextField("New pose name:", _newPoseName);

            GUILayout.Label("\nYou can also change the settings of your transform configuration, for example if you need to change " +
                "UpVectorType from World to Head.\n");
            _positionOffset = EditorGUILayout.Vector3Field("Position offset:", _positionOffset);
            _rotationOffset = EditorGUILayout.Vector3Field("Rotation offset:", _rotationOffset);
            _upVectorType = (UpVectorType)EditorGUILayout.EnumPopup("Up vector type:", _upVectorType);
            _featureThresholds = EditorGUILayout.ObjectField(_featureThresholds, typeof(TransformFeatureStateThresholds), true) as TransformFeatureStateThresholds;

            GUILayout.Label($"\n<size=20>2.</size>\t" +
                "Go to <b>Play Mode</b> to record your hand pose. Press the big <b>Record</b> button with your free hand or the " +
                $"<b>{_recordKey}</b> key to record a new pose and create a selector for it <b>(requires focus on either this " +
                "window or the scene view playing the scene)</b>.\n", _richTextStyle);
            _recordKey = (KeyCode)EditorGUILayout.EnumPopup(_recordKey);
            if (GUILayout.Button("Record New Hand Pose Selector", GUILayout.Height(100)))
            {
                Context.Global.GetInstance().StartCoroutine(RecordPoseCoroutine());
            }

            GUILayout.Label("\n<size=20>3.</size>\t" +
                "Recording takes a few seconds, so try to hold still for a count of 5, or until you see the \"Recording complete\" message " +
                "logged to the console. Recording will automatically generate both a HandShape asset and a fully wired-up selector prefab in " +
                "the Assets/RecordedHandPoseSelectors directory <b>(overwriting any pre-existing assets in the same place of the same name)</b>. " +
                "Optionally, you can choose to have the resultant prefab automatically added to your scene and wired up once you exit Play " +
                "mode; to select this option, check the checkbox below.\n", _richTextStyle);

            _autoAddPrefabAfterRecording = EditorGUILayout.Toggle("Auto-add prefab: ", _autoAddPrefabAfterRecording);

            GUILayout.EndScrollView();
        }

        private IEnumerator RecordPoseCoroutine()
        {
#if UNITY_6000_3_OR_NEWER
            var fingerFeatureStateProvider = EditorUtility.EntityIdToObject(_fingerFeatureStateProviderInstanceId) as FingerFeatureStateProvider;
            var transformFeatureStateProvider = EditorUtility.EntityIdToObject(_transformFeatureStateProviderInstanceId) as TransformFeatureStateProvider;
#else
            var fingerFeatureStateProvider = EditorUtility.InstanceIDToObject(_fingerFeatureStateProviderInstanceId) as FingerFeatureStateProvider;
            var transformFeatureStateProvider = EditorUtility.InstanceIDToObject(_transformFeatureStateProviderInstanceId) as TransformFeatureStateProvider;
#endif
            var hand = fingerFeatureStateProvider.Hand;
            var handednessText = (hand.Handedness == Handedness.Left ? "left" : "right");

            Debug.Log("Recording new " + handednessText + "-handed pose selector...");

            TransformConfig transformConfig = new();
            transformConfig.PositionOffset = _positionOffset;
            transformConfig.RotationOffset = _rotationOffset;
            transformConfig.UpVectorType = _upVectorType;
            transformConfig.FeatureThresholds = _featureThresholds;
            transformFeatureStateProvider.RegisterConfig(transformConfig);

            Dictionary<HandFinger, Dictionary<FingerFeature, HashSet<string>>> fingerToFeatureToStates = new();
            foreach (var finger in Fingers)
            {
                Dictionary<FingerFeature, HashSet<string>> featureToStates = new();
                foreach (var feature in Features)
                {
                    featureToStates.Add(feature, new());
                }
                fingerToFeatureToStates.Add(finger, featureToStates);
            }

            Dictionary<TransformFeature, HashSet<string>> transformFeatureToStates = new();
            foreach (var feature in TransformFeatures)
            {
                transformFeatureToStates.Add(feature, new());
            }

            var delay = new WaitForSecondsRealtime(0.1f);
            string state;
            for (int idx = 0; idx < 20; ++idx)
            {
                foreach (var finger in Fingers)
                {
                    foreach (var feature in Features)
                    {
                        if (fingerFeatureStateProvider.GetCurrentState(finger, feature, out state))
                        {
                            fingerToFeatureToStates[finger][feature].Add(state);
                        }
                    }
                }

                foreach (var feature in TransformFeatures)
                {
                    if (transformFeatureStateProvider.GetCurrentState(transformConfig, feature, out state))
                    {
                        transformFeatureToStates[feature].Add(state);
                    }
                }

                yield return delay;
            }

            Dictionary<HandFinger, ShapeRecognizer.FingerFeatureConfig[]> fingerToConfigList = new();
            var abductionNoneFeatureDescriptor = FingerFeatureProperties.FeatureDescriptions[FingerFeature.Abduction].FeatureStates.First(s => s.Name == "None");
            var oppositionTouchingFeatureDescriptor = FingerFeatureProperties.FeatureDescriptions[FingerFeature.Opposition].FeatureStates.First(s => s.Name == "Touching");
            foreach (var finger in fingerToFeatureToStates.Keys)
            {
                List<ShapeRecognizer.FingerFeatureConfig> configList = new();
                foreach (var feature in fingerToFeatureToStates[finger].Keys)
                {
                    var observed = fingerToFeatureToStates[finger][feature];
                    HashSet<string> unobserved = new();
                    foreach (var description in FingerFeatureProperties.FeatureDescriptions[feature].FeatureStates)
                    {
                        if (!observed.Contains(description.Id))
                        {
                            unobserved.Add(description.Id);
                        }
                    }

                    switch (feature)
                    {
                        case FingerFeature.Curl:
                        case FingerFeature.Flexion:
                            // For curl and flexion, all possible feature states are viable.
                            if (unobserved.Count == 1)
                            {
                                configList.Add(new()
                                {
                                    Mode = FeatureStateActiveMode.IsNot,
                                    Feature = feature,
                                    State = unobserved.First(),
                                });
                            }
                            else if (observed.Count == 1)
                            {
                                configList.Add(new()
                                {
                                    Mode = FeatureStateActiveMode.Is,
                                    Feature = feature,
                                    State = observed.First(),
                                });
                            }
                            break;
                        case FingerFeature.Abduction:
                            // For abduction, none is not applicable to the pinky finger.
                            if (finger != HandFinger.Pinky && observed.Count == 1 && observed.First() != abductionNoneFeatureDescriptor.Id)
                            {
                                configList.Add(new()
                                {
                                    Mode = FeatureStateActiveMode.Is,
                                    Feature = feature,
                                    State = observed.First(),
                                });
                            }
                            break;
                        case FingerFeature.Opposition:
                            // Opposition is not applicable to the thumb, and for the others we only care about it if it's touching.
                            if (finger != HandFinger.Thumb && observed.Count == 1 && observed.First() == oppositionTouchingFeatureDescriptor.Id)
                            {
                                configList.Add(new()
                                {
                                    Mode = FeatureStateActiveMode.Is,
                                    Feature = feature,
                                    State = observed.First(),
                                });
                            }
                            break;
                        default:
                            throw new NotImplementedException("Unrecognized finger feature");
                    }
                }
                fingerToConfigList.Add(finger, configList.ToArray());
            }
            ShapeRecognizer recognizer = new();
            recognizer.InjectAllShapeRecognizer(fingerToConfigList);

            List<TransformFeatureConfig> transformFeatureConfigs = new();
            foreach (var feature in TransformFeatures)
            {
                var observed = transformFeatureToStates[feature];
                if (observed.Count == 1)
                {
                    transformFeatureConfigs.Add(new()
                    {
                        Mode = FeatureStateActiveMode.Is,
                        Feature = feature,
                        State = observed.First(),
                    });
                }
            }
            var transformFeatureConfigList = TransformFeatureConfigList.Create(transformFeatureConfigs);

            if (!Directory.Exists("Assets/RecordedHandPoseSelectors"))
            {
                Directory.CreateDirectory("Assets/RecordedHandPoseSelectors");
            }
            var handShapeAssetPath = "Assets/RecordedHandPoseSelectors/" + _newPoseName + "HandShape.asset";
            AssetDatabase.DeleteAsset(handShapeAssetPath);
            AssetDatabase.CreateAsset(recognizer, handShapeAssetPath);

            GameObject prefab = new();
            prefab.SetActive(false);

            var handRef = prefab.AddComponent<HandRef>();
            var fingerFeatureStateProviderRef = prefab.AddComponent<FingerFeatureStateProviderRef>();
            var transformFeatureStateProviderRef = prefab.AddComponent<TransformFeatureStateProviderRef>();

            var selectorUnityEventWrapper = prefab.AddComponent<SelectorUnityEventWrapper>();
            var selector = prefab.AddComponent<ActiveStateSelector>();
            var activeState = prefab.AddComponent<ActiveStateGroup>();
            var shapeRecognizer = prefab.AddComponent<ShapeRecognizerActiveState>();
            var transformRecognizer = prefab.AddComponent<TransformRecognizerActiveState>();

            handRef.InjectAllHandRef(fingerFeatureStateProvider.Hand);
            fingerFeatureStateProviderRef.InjectAllFingerFeatureStateProviderRef(fingerFeatureStateProvider);
            transformFeatureStateProviderRef.InjectAllTransformFeatureStateProviderRef(transformFeatureStateProvider);
            shapeRecognizer.InjectAllShapeRecognizerActiveState(fingerFeatureStateProviderRef, new ShapeRecognizer[] { recognizer });
            transformRecognizer.InjectAllTransformRecognizerActiveState(transformFeatureStateProviderRef, transformFeatureConfigList, transformConfig);
            activeState.InjectAllActiveStateGroup(new() { shapeRecognizer, transformRecognizer });
            selector.InjectAllActiveStateSelector(activeState);
            selectorUnityEventWrapper.InjectAllSelectorUnityEventWrapper(selector);

            prefab.SetActive(true);

            var prefabPath = "Assets/RecordedHandPoseSelectors/" + _newPoseName + ".prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, prefabPath, InteractionMode.AutomatedAction);

            Debug.Log("Recording complete! Results saved to Assets/RecordedHandPoseSelectors");

            GameObject.Destroy(prefab);

            if (_autoAddPrefabAfterRecording)
            {
                _prefabPathToAdd = prefabPath;
                _handInstanceId = (hand as UnityEngine.Object).GetHashCode();
            }
        }
    }
}

