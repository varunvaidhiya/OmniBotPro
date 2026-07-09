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

#if HAS_META_VOICE_SDK

using Meta.Voice.Logging;
using Meta.WitAi;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Oculus.Voice.Dictation;
using Oculus.Voice.Dictation.Configuration;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    internal class VoiceSetupController : MonoBehaviour
    {
        /// <summary>
        /// Demo client access token for Wit.ai's "Built-in Models" app.
        /// This is a public demo key with limited rate limits and no custom intents/entities.
        /// For production use, create your own Wit.ai app at https://wit.ai and pass a
        /// WitConfiguration via <see cref="CreateVoiceSetupAsChild(GameObject, WitConfiguration)"/>,
        /// or assign one in DevAgentSettings (Project Settings > Immersive Debugger > Voice Input).
        /// If no WitConfiguration is provided, a runtime instance is created automatically
        /// using this demo token (see <see cref="SetupVoiceComponents"/>).
        /// </summary>
        private const string DemoClientAccessToken = "HOKEABS7HPIQVSRSVWRPTTV75TQJ5QBP";

        private WitConfiguration witConfiguration;

        private GameObject _voiceGameObject;
        private AppDictationExperience _dictationExperience;
        private DictationController _dictationController;
        private bool _isRuntimeCreatedConfiguration;

        /// <summary>
        /// Event invoked when the DictationController is ready to use.
        /// </summary>
        internal event Action<DictationController> OnDictationControllerReady;

        /// <summary>
        /// Gets the DictationController instance created by this setup controller.
        /// </summary>
        public DictationController DictationController => _dictationController;

        /// <summary>
        /// Gets the AppDictationExperience instance created by this setup controller.
        /// </summary>
        public AppDictationExperience DictationExperience => _dictationExperience;

        private void Awake()
        {
            // Suppress verbose Voice SDK logging by default (set to false for debugging)
            VLog.SuppressLogs = true;

            // Request microphone permission before setup (Android only)
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => SetupVoiceComponents();
                callbacks.PermissionDenied += _ =>
                    Debug.LogError("[VoiceSetupController] Microphone permission denied - voice features will not work. " +
                        "Grant via Settings or: adb shell pm grant <package> android.permission.RECORD_AUDIO");
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
            }
            else
            {
                SetupVoiceComponents();
            }
#else
            SetupVoiceComponents();
#endif
        }


        private void SetupVoiceComponents()
        {
            if (witConfiguration == null)
            {
                witConfiguration = ScriptableObject.CreateInstance<WitConfiguration>();
                witConfiguration.SetClientAccessToken(DemoClientAccessToken);
                witConfiguration.isDemoOnly = true;
                witConfiguration.useConduit = false;
                _isRuntimeCreatedConfiguration = true;
            }

            _voiceGameObject = new GameObject("[Voice] Dictation");
            _voiceGameObject.transform.SetParent(transform);
            _voiceGameObject.transform.localPosition = Vector3.zero;
            _voiceGameObject.transform.localRotation = Quaternion.identity;

            _dictationExperience = _voiceGameObject.AddComponent<AppDictationExperience>();

            // Configuration needs to happen after the component initializes, so use a coroutine
            StartCoroutine(ConfigureAfterInitialization());
        }

        private IEnumerator ConfigureAfterInitialization()
        {
            // Wait for end of frame to ensure AppDictationExperience.Awake() has run
            yield return new WaitForEndOfFrame();

            ConfigureDictationExperience();

            _dictationController = _voiceGameObject.AddComponent<DictationController>();
            _dictationController.experience = _dictationExperience;

            // Notify listeners that DictationController is ready
            OnDictationControllerReady?.Invoke(_dictationController);
        }

        private void ConfigureDictationExperience()
        {
            if (_dictationExperience == null)
            {
                Debug.LogError("[VoiceSetupController] Cannot configure dictation experience: _dictationExperience is null");
                return;
            }

            // Create and assign RuntimeDictationConfiguration if it doesn't exist
            var runtimeConfig = _dictationExperience.RuntimeDictationConfiguration;
            if (runtimeConfig == null)
            {
                runtimeConfig = new WitDictationRuntimeConfiguration();
                _dictationExperience.RuntimeDictationConfiguration = runtimeConfig;
            }

            // Create and assign DictationConfiguration if it doesn't exist
            if (runtimeConfig.dictationConfiguration == null)
            {
                runtimeConfig.dictationConfiguration = new DictationConfiguration();
            }

            runtimeConfig.witConfiguration = witConfiguration;
            runtimeConfig.minKeepAliveVolume = 1;
            runtimeConfig.minKeepAliveTimeInSeconds = 5;
            runtimeConfig.minTranscriptionKeepAliveTimeInSeconds = 3;
            runtimeConfig.maxRecordingTime = 60;
            runtimeConfig.overrideTimeoutMs = 0;
            runtimeConfig.soundWakeThreshold = 0.0005f;
            runtimeConfig.sampleLengthInMs = 10;
            runtimeConfig.micBufferLengthInSeconds = 1;
            runtimeConfig.maxConcurrentRequests = 5;
            runtimeConfig.sendAudioToWit = true;
            runtimeConfig.preferredActivationOffset = -0.5f;

            var dictationConfig = runtimeConfig.dictationConfiguration;
            dictationConfig.multiPhrase = false;
            dictationConfig.scenario = "default";
            dictationConfig.inputType = "text_default";

            _dictationExperience.UsePlatformIntegrations = false;
            _dictationExperience.DoNotFallbackToWit = false;

            // IMPORTANT: Force re-initialization of the dictation service with the configured RuntimeConfiguration.
            // InitDictation() runs during OnEnable() when runtimeConfiguration is still null.
            // By disabling and re-enabling the component, we force InitDictation() to run again with the correct configuration.
            _dictationExperience.enabled = false;
            _dictationExperience.enabled = true;
        }

        public static VoiceSetupController CreateVoiceSetupAsChild(GameObject parent, WitConfiguration witConfig = null)
        {
            if (parent == null)
            {
                Debug.LogError("[VoiceSetupController] Cannot create voice setup with null parent GameObject.");
                return null;
            }

            var voiceSetupObject = new GameObject("VoiceSetup");
            voiceSetupObject.transform.SetParent(parent.transform);
            voiceSetupObject.transform.localPosition = Vector3.zero;
            voiceSetupObject.transform.localRotation = Quaternion.identity;

            var setup = voiceSetupObject.AddComponent<VoiceSetupController>();
            if (witConfig != null)
            {
                setup.witConfiguration = witConfig;
            }

            return setup;
        }

        private void OnDestroy()
        {
            // Clean up the voice GameObject if it exists
            if (_voiceGameObject != null)
            {
                Destroy(_voiceGameObject);
            }

            // Destroy runtime-created WitConfiguration to prevent memory leaks
            if (_isRuntimeCreatedConfiguration && witConfiguration != null)
            {
                Destroy(witConfiguration);
                witConfiguration = null;
            }
        }
    }
}
#endif
