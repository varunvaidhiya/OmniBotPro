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
using UnityEngine;
using UnityEngine.Scripting;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// A wrapper that notifies subscribers when its value changes.
    /// Subscribers receive the current value immediately upon subscribing.
    /// </summary>
    [System.Serializable]
    public class ReactiveValue<TValue>
    {
        [SerializeField]
        private TValue _value = default;
        public TValue Value
        {
            get => _value;
            set
            {
                if (_equalityComparer == null)
                {
                    _equalityComparer = EqualityComparer<TValue>.Default;
                }
                if (!_equalityComparer.Equals(value, _value))
                {
                    _value = value;
                    _whenChanged.Invoke(value);
                }
            }
        }

        private Action<TValue> _whenChanged = delegate { };
        /// <summary>
        /// Notifies about the value change.
        /// The current value is notified immediately upon subscription.
        /// </summary>
        public event Action<TValue> WhenChanged
        {
            add
            {
                _whenChanged += value;
                value.Invoke(Value);
            }
            remove => _whenChanged -= value;
        }

        private EqualityComparer<TValue> _equalityComparer;

        [Preserve]
        private void Refresh()
        {
            _whenChanged.Invoke(Value);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReactiveValue<>))]
    internal class LocomotionSettingDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            UnityEditor.EditorGUI.BeginChangeCheck();
            UnityEditor.EditorGUI.PropertyField(position, property.FindPropertyRelative("_value"), label, false);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                var target = fieldInfo.GetValue(property.serializedObject.targetObject);
                target.GetType()
                    .GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(target, null);
            }
        }
        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            return UnityEditor.EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_value"), label, false);
        }
    }
#endif

}
