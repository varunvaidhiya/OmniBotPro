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

using Meta.XR.ImmersiveDebugger;
using System.Linq;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// MonoBehaviour component that provides axis transform data for a specific GameObject.
    /// This component is registered with the Immersive Debugger's Inspector system.
    /// Uses caching to avoid expensive GameObject lookups on every access.
    /// </summary>
    internal class AxisGizmoProvider : MonoBehaviour
    {
        private string _targetObjectName;
        private GameObject _cachedTargetObject;
        private Transform _cachedTransform;

        /// <summary>
        /// Sets up this provider to track a specific GameObject
        /// </summary>
        public void Setup(string targetObjectName)
        {
            _targetObjectName = targetObjectName;
            RefreshCache();
        }

        /// <summary>
        /// The transform of the target object - used directly by Axis gizmo
        /// </summary>
        public Transform AxisTransform
        {
            get
            {
                // Check if cached object is still valid
                if (_cachedTargetObject == null || _cachedTransform == null)
                {
                    RefreshCache();
                }

                return _cachedTransform;
            }
        }

        public bool TargetExists
        {
            get
            {
                if (string.IsNullOrEmpty(_targetObjectName))
                    return false;

                // Check cached object first
                if (_cachedTargetObject != null)
                    return true;

                // If cache is invalid, try to refresh
                RefreshCache();
                return _cachedTargetObject != null;
            }
        }

        public string TargetObject => _targetObjectName ?? "None";

        /// <summary>
        /// Refreshes the cached GameObject reference
        /// </summary>
        private void RefreshCache()
        {
            if (string.IsNullOrEmpty(_targetObjectName))
            {
                _cachedTargetObject = null;
                _cachedTransform = null;
                return;
            }

            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _cachedTargetObject = allObjects.FirstOrDefault(obj => obj.name == _targetObjectName);
            _cachedTransform = _cachedTargetObject?.transform;
        }

        /// <summary>
        /// Force refresh the cache (useful if the target object might have changed)
        /// </summary>
        public void InvalidateCache()
        {
            _cachedTargetObject = null;
            _cachedTransform = null;
        }
    }
}
