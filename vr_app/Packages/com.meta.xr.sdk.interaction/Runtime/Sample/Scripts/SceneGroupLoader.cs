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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine.SceneManagement;
#endif

namespace Oculus.Interaction.Samples
{
    public class SceneGroupLoader : MonoBehaviour
    {
        [SerializeField]
        private SceneLoader _sceneLoader;

        [SerializeField]
        private Transform _sceneGroupContainer;

        [SerializeField]
        private GameObject _missingSceneWarning;

        [Header("Group Template")]
        [SerializeField]
        private GameObject _groupTemplateParent;

        [SerializeField]
        private TextMeshProUGUI _groupTemplateLabel;

        [SerializeField]
        private RectTransform _groupTileContainer;

        [Header("Tile Template")]
        [SerializeField]
        private GameObject _tileTemplateParent;

        [SerializeField]
        private TextMeshProUGUI _tileTemplateLabel;

        [SerializeField]
        private Image _tileTemplateImage;

        [SerializeField]
        private Toggle _tileTemplateToggle;

        [SerializeField]
        private Image _tileTemplateSceneMissingOverlay;

        private void Start()
        {
            BuildSceneGroups();
        }

        private void BuildSceneGroups()
        {
            void InitializeGroupViewTemplate()
            {
                if (!_groupTemplateParent.TryGetComponent<SceneGroupView>(out var groupViewTemplate))
                {
                    groupViewTemplate = _groupTemplateParent.AddComponent<SceneGroupView>();
                    groupViewTemplate.GroupName = _groupTemplateLabel;
                    groupViewTemplate.TileContainer = _groupTileContainer;
                }
            }

            void InitializeTileViewTemplate()
            {
                if (!_tileTemplateParent.TryGetComponent<SceneTileView>(out var tileViewTemplate))
                {
                    tileViewTemplate = _tileTemplateParent.AddComponent<SceneTileView>();
                    tileViewTemplate.Image = _tileTemplateImage;
                    tileViewTemplate.Label = _tileTemplateLabel;
                    tileViewTemplate.Toggle = _tileTemplateToggle;
                    tileViewTemplate.SceneMissingOverlay = _tileTemplateSceneMissingOverlay;
                }
            }

            bool anySceneMissing = false;
            var sceneGroups = FindSceneGroupAssets()
                .Where(x => x.GroupEnabled)
                .Where(g => g.SceneCount > 0)
                .OrderBy(g => g.GroupDisplayOrder);

            foreach (var sceneGroup in sceneGroups)
            {
                InitializeGroupViewTemplate();

                var newGroupObj = Instantiate(_groupTemplateParent, _sceneGroupContainer);
                newGroupObj.name = sceneGroup.GroupName;
                newGroupObj.SetActive(true);

                var newGroupView = newGroupObj.GetComponent<SceneGroupView>();
                newGroupView.GroupName.text = sceneGroup.GroupName;

                foreach (var sceneMenuItem in sceneGroup.GetScenes())
                {
                    InitializeTileViewTemplate();

                    bool sceneExists = CheckSceneExists(sceneMenuItem);
                    anySceneMissing |= !sceneExists;

                    var newTileObj = Instantiate(_tileTemplateParent, newGroupView.TileContainer);
                    newTileObj.name = sceneMenuItem.DisplayName;
                    newTileObj.SetActive(true);

                    var newTileView = newTileObj.GetComponent<SceneTileView>();
                    newTileView.Label.text = sceneMenuItem.DisplayName;
                    newTileView.Toggle.enabled = sceneExists;
                    newTileView.Toggle.onValueChanged.AddListener((v) => LoadScene(sceneMenuItem));
                    newTileView.Image.sprite = sceneMenuItem.Thumbnail;
                    newTileView.Image.enabled = sceneExists;
                    newTileView.SceneMissingOverlay.sprite = sceneMenuItem.Thumbnail;
                    newTileView.SceneMissingOverlay.gameObject.SetActive(!sceneExists);
                }
            }

            _missingSceneWarning.SetActive(anySceneMissing);

        }


        private void LoadScene(SampleSceneGroup.ISceneInfo sceneInfo)
        {
#if UNITY_EDITOR
            _sceneLoader.LoadByPath(sceneInfo.SceneName,
                UnityEditor.AssetDatabase.GUIDToAssetPath(sceneInfo.SceneGuid));
#else
            _sceneLoader.Load(sceneInfo.SceneName);
#endif
        }

        private static bool CheckSceneExists(SampleSceneGroup.ISceneInfo sceneInfo)
        {
#if UNITY_EDITOR
            return !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GUIDToAssetPath(sceneInfo.SceneGuid));
#else
            return SceneUtility.GetBuildIndexByScenePath(sceneInfo.SceneName) >= 0;
#endif
        }

        private static IEnumerable<SampleSceneGroup> FindSceneGroupAssets()
        {
            return Resources.LoadAll<SampleSceneGroup>("");
        }

        private class SceneGroupView : MonoBehaviour
        {
            public TextMeshProUGUI GroupName;
            public RectTransform TileContainer;
        }

        private class SceneTileView : MonoBehaviour
        {
            public TextMeshProUGUI Label;
            public Image Image;
            public Toggle Toggle;
            public Image SceneMissingOverlay;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SceneGroupLoader))]
        public class SceneGroupLoaderEditor : UnityEditor.Editor
        {
            private SceneGroupLoader Loader => target as SceneGroupLoader;

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
            }
        }
#endif
    }
}
