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
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Oculus.Interaction.Samples
{
    [CreateAssetMenu(menuName = "Meta/Interaction SDK/Scene Group")]
    public class SampleSceneGroup : ScriptableObject
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
    {
        public interface ISceneInfo
        {
            /// <summary>
            /// The name of the scene that will be
            /// shown in the scene menu.
            /// </summary>
            string DisplayName { get; }

            /// <summary>
            /// The name of the scene asset.
            /// </summary>
            string SceneName { get; }

            /// <summary>
            /// The GUID of the scene asset.
            /// </summary>
            string SceneGuid { get; }

            /// <summary>
            /// The 16x9 thumbnail that will be display
            /// in the scene menu.
            /// </summary>
            Sprite Thumbnail { get; }
        }

        [Serializable]
        private class SceneInfo : ISceneInfo
        {
            public string DisplayName;
            public string SceneName;
            public string SceneGuid;
            public Sprite Thumbnail;

            string ISceneInfo.DisplayName => DisplayName;
            string ISceneInfo.SceneName => SceneName;
            Sprite ISceneInfo.Thumbnail => Thumbnail;
            string ISceneInfo.SceneGuid => SceneGuid;
        }

        [Tooltip("Scenes in this group will be " +
            "displayed under this header in the scene menu.")]
        [SerializeField]
        private string _groupName;

        [Tooltip("Only Enabled scene groups will be " +
            "shown in the scene menu.")]
        [SerializeField]
        private bool _groupEnabled = true;

        [Tooltip("Scene groups will appear in the scene menu " +
            "sorted in ascending order by this value.")]
        [SerializeField]
        private int _groupDisplayOrder = 0;

        [SerializeField, HideInInspector]
        private SceneInfo[] _sceneInfos;

        /// <summary>
        /// Scenes in this group will be displayed under this
        /// header in the scene menu.
        /// </summary>
        public string GroupName => _groupName;

        /// <summary>
        /// One Enabled scene groups will be shown in the scene menu.
        /// </summary>
        public bool GroupEnabled => _groupEnabled;

        /// <summary>
        /// Scene groups will appear in the scene menu sorted
        /// in ascending order by this value.
        /// </summary>
        public int GroupDisplayOrder => _groupDisplayOrder;

        /// <summary>
        /// The number of scenes in this scene group.
        /// </summary>
        public int SceneCount => _sceneInfos.Length;

        /// <summary>
        /// Returns <see cref="ISceneInfo"/>s which provide
        /// display data for the scene menu.
        /// </summary>
        public IEnumerable<ISceneInfo> GetScenes()
        {
            foreach (var item in _sceneInfos)
            {
                yield return item;
            }
        }

#if UNITY_EDITOR
        private class SceneRenameHandler : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                if (movedAssets == null || movedAssets.Length == 0)
                {
                    return;
                }

                // If a group contains a reference to a sample scene that's been renamed,
                // mark as dirty to ensure we reserialize the correct scene name.
                var groups = Resources.LoadAll<SampleSceneGroup>("");
                foreach (var group in groups)
                {
                    foreach (var infoAsset in group._sceneInfoAssets)
                    {
                        string scenePath = AssetDatabase.GetAssetPath(infoAsset.SceneAsset);
                        if (movedAssets.Contains(scenePath))
                        {
                            EditorUtility.SetDirty(group);
                            break;
                        }
                    }
                }
            }
        }

        [Serializable]
        private class SceneInfoAsset
        {
            [Tooltip("The name of the scene that " +
                "will be shown in the scene menu.")]
            public string DisplayName;

            [Tooltip("The scene asset that will be opened.")]
            public SceneAsset SceneAsset;

            [Tooltip("The 16x9 thumbnail of the scene " +
                "that will be shown in the scene menu.")]
            public Sprite Thumbnail;

            public bool CheckValid()
            {
                bool valid = true;
                valid &= !string.IsNullOrWhiteSpace(DisplayName);
                valid &= SceneAsset != null;
                valid &= Thumbnail != null;
                return valid;
            }
        }

        [SerializeField]
        private SceneInfoAsset[] _sceneInfoAssets;

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
            if (_sceneInfoAssets == null)
            {
                return;
            }

            _sceneInfos = _sceneInfoAssets
                .Where(a => a.CheckValid())
                .Where(a => _sceneInfoAssets
                    .Count(b => b.DisplayName == a.DisplayName) == 1)
                .Select(a => new SceneInfo()
                {
                    DisplayName = a.DisplayName,
                    SceneName = a.SceneAsset.name,
                    SceneGuid = GetAssetGuid(a.SceneAsset),
                    Thumbnail = a.Thumbnail,
                })
                .ToArray();
        }

        private static string GetAssetGuid(UnityEngine.Object obj)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
        }
#endif
    }
}
