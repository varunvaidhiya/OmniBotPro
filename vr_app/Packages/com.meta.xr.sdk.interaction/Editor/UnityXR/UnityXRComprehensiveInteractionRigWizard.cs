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

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.Editor.QuickActions;
using Unity.XR.CoreUtils;

#if UNITY_2022_1_OR_NEWER
using System.IO;
#endif

namespace Oculus.Interaction.Editor.UnityXR.QuickActions
{
    internal class UnityXRComprehensiveInteractionRigWizard : QuickActionsWizard
    {
        private const string MENU_NAME_ADD_NEW_RIG = QuickActionsWizard.MENU_FOLDER +
            "Add UnityXR Comprehensive Interaction Rig";

        private const string LEFT_INTERACTIONS_NAME = "ComprehensiveInteractorsLeft";
        private const string RIGHT_INTERACTIONS_NAME = "ComprehensiveInteractorsRight";

        public static readonly Template UnityXRCameraRig =
            new Template(
                "UnityXRCameraRig",
                "f9f107e8ecae49048bda9d4fd7e1d1ec");

        public static readonly Template UnityXRInteractionComprehensive =
            new Template(
                "UnityXRComprehensiveInteractionRig",
                "5c149baf016750b42ac7e9cdbc905b96");

        public static readonly Template ComprehensiveInteractors =
           new Template(
               "ComprehensiveInteractors",
               "46a17985c3f323a49a075ccb386282e9");

#if UNITY_2022_1_OR_NEWER
        [SerializeField]
        [WizardSetting]
        [Tooltip("Unpacks the interaction rig prefab and creates a common variant between the left and right hierarchies " +
            "so you can easily inspect and apply modifications to both sides.")]
        private bool _generateAsEditableCopy = true;

        [SerializeField]
        [WizardSetting]
        [Tooltip("Path within Assets/ to store the editable variant if GenerateAsEditableCopy is selected")]
        [ConditionalHide(nameof(_generateAsEditableCopy), true,
            ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        private string _prefabPath = "InteractionSDK/ComprehensiveInteractors.prefab";
#endif

        [SerializeField]
        [Tooltip("The Interaction Rig will be added under this UnityXRCameraRig and reference its hands, controllers and hmd.")]
        [WizardDependency(Category = Category.Required,
            FindMethod = nameof(AssignExistingCameraRig),
            FixMethod = nameof(CreateMissingCameraRig))]
        private XROrigin _cameraRig;

        [MenuItem(MENU_NAME_ADD_NEW_RIG, priority = 100)]
        internal static void OpenWizard()
        {
            ShowWindow<UnityXRComprehensiveInteractionRigWizard>(null);
        }

        protected override void Create()
        {
            CreateInteractionRig();
        }

        internal GameObject CreateInteractionRig()
        {
            GameObject interactionRig = Templates.CreateFromTemplate(
                _cameraRig.transform, UnityXRInteractionComprehensive, asPrefab: true);
            UnityObjectAddedBroadcaster.HandleObjectWasAdded(interactionRig);

#if UNITY_2022_1_OR_NEWER
            if (_generateAsEditableCopy)
            {
                MakeInteractionRigEditable(interactionRig);
            }
#endif
            Selection.activeObject = interactionRig;
            return interactionRig;
        }

#if UNITY_2022_1_OR_NEWER
        private void MakeInteractionRigEditable(GameObject interactionRig)
        {
            PrefabUtility.UnpackPrefabInstance(interactionRig,
                PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

            //Create the folder structure for storing the new prefab variant
            string fileName = Path.GetFileNameWithoutExtension(_prefabPath);
            string directoryName = Path.GetDirectoryName(_prefabPath);
            string prefabFolder = Path.Combine("Assets", directoryName);
            string savePath = Path.Combine(prefabFolder, $"{fileName}.prefab");
            if (!Directory.Exists(prefabFolder))
            {
                Directory.CreateDirectory(prefabFolder);
            }

            //generate an editable prefab variant out of the comprehensive prefab in the package
            Object comprehensiveInteractorsSource = AssetDatabase.LoadMainAssetAtPath(
                AssetDatabase.GUIDToAssetPath(ComprehensiveInteractors.AssetGUID));
            GameObject comprehensiveInteractorsPrefab = PrefabUtility.InstantiatePrefab(comprehensiveInteractorsSource) as GameObject;
            GameObject comprehensiveInteractorsVariant = PrefabUtility.SaveAsPrefabAsset(comprehensiveInteractorsPrefab, savePath);
            GameObject.DestroyImmediate(comprehensiveInteractorsPrefab);

            GameObject leftInteractors = interactionRig.transform.Find(LEFT_INTERACTIONS_NAME).gameObject;
            GameObject rightInteractors = interactionRig.transform.Find(RIGHT_INTERACTIONS_NAME).gameObject;

            PrefabReplacingSettings replaceSettings = new PrefabReplacingSettings()
            {
                changeRootNameToAssetName = false,
                logInfo = true,
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides
            };

            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(leftInteractors,
                comprehensiveInteractorsVariant, replaceSettings, InteractionMode.AutomatedAction);
            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(rightInteractors,
                comprehensiveInteractorsVariant, replaceSettings, InteractionMode.AutomatedAction);
        }
#endif

        internal void AssignExistingCameraRig()
        {
            _cameraRig = Object.FindFirstObjectByType<XROrigin>();
        }

        public static XROrigin FindExistingCameraRig()
        {
            return Object.FindFirstObjectByType<XROrigin>();
        }

        internal void CreateMissingCameraRig()
        {
            GameObject cameraRig = Templates.CreateFromTemplate(
                null, UnityXRCameraRig, asPrefab: true);

            if (!cameraRig.TryGetComponent(out _cameraRig))
            {
                AssignExistingCameraRig();
            }

            if (_cameraRig != null)
            {
                _cameraRig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            }
        }


        #region Injects

        public void InjectCameraRig(XROrigin cameraRig)
        {
            _cameraRig = cameraRig;
        }

#if UNITY_2022_1_OR_NEWER
        public void InjectOptionalGenerateAsEditableCopy(bool generateAsEditableCopy)
        {
            _generateAsEditableCopy = generateAsEditableCopy;
        }

        public void InjectOptionalPrefabPath(string prefabPath)
        {
            _prefabPath = prefabPath;
        }
#endif

        #endregion
    }
}
