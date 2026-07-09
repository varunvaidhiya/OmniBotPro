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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Hands.Editor
{
    public static class HandJointsAutoPopulatorHelper
    {
        //Possible boundaries of words around key names in the hierarchy.
        //It detects normal boundaries such as space or commas, but also underscores.
        private static readonly string _b = @"(\b|_)";

        private static readonly string _tipRegex = $"{_b}tip{_b}";
        private static readonly Dictionary<HandFinger, string> _fingerRegexs = new Dictionary<HandFinger, string>()
        {
            { HandFinger.Thumb, $"{_b}thumb{_b}" },
            { HandFinger.Index, $"{_b}index{_b}" },
            { HandFinger.Middle, $"{_b}middle{_b}" },
            { HandFinger.Ring, $"{_b}ring{_b}" },
            { HandFinger.Pinky, $"{_b}(pinky|little){_b}" },
        };

        public static void DisplayJoints(SerializedProperty jointsProperty)
        {
            EditorGUILayout.LabelField("Joints", EditorStyles.boldLabel);
            for (int i = (int)HandJointId.HandStart; i < (int)HandJointId.HandEnd; ++i)
            {
                SerializedProperty joint = jointsProperty.GetArrayElementAtIndex(i);
                string jointName = ((HandJointId)i).ToString();
                joint.objectReferenceValue = (Transform)EditorGUILayout.ObjectField(jointName,
                    joint.objectReferenceValue, typeof(Transform), true);
            }
        }

        public static void InitializeCollection(SerializedProperty jointsProperty)
        {
            int count = (int)HandJointId.HandEnd - (int)HandJointId.HandStart;
            jointsProperty.arraySize = count;
        }

        public static void AutoMapJoints(SerializedProperty jointsProperty, Transform rootTransform)
        {
            Transform fingersOrigin = FindFingersOrigin(rootTransform);

            //wire the fingers hierarchy, from tip to base
            WireFingers(jointsProperty, fingersOrigin);

            //wire the remaining stubs of the hand's base
            WireStubs(jointsProperty, fingersOrigin);
        }

        private static void WireFingers(SerializedProperty jointsProperty, Transform rootTransform)
        {
            for (HandFinger finger = HandFinger.Thumb; finger <= HandFinger.Pinky; finger++)
            {
                Transform transform = FindTipTransform(finger, rootTransform);
                HandJointId[] fingerJoints = HandJointUtils.FingerToJointList[(int)finger];

                for (int i = fingerJoints.Length - 1; i >= 0; --i)
                {
                    HandJointId jointID = fingerJoints[i];
                    jointsProperty.GetArrayElementAtIndex((int)jointID).objectReferenceValue = transform;


                    transform = transform.parent;
                    if (transform == rootTransform)
                    {
                        break;
                    }
                }
            }
        }

        private static void WireStubs(SerializedProperty jointsProperty, Transform rootTransform)
        {
            HandJointId thumbBase = HandJointUtils.FingerToJointList[(int)HandFinger.Thumb][0];
            HandJointId thumbParent = HandJointUtils.JointParentList[(int)thumbBase];
            HandJointId[] stubIds = HandJointUtils.JointChildrenList[(int)thumbParent];

            //wire the real wrist of the hand
            jointsProperty.GetArrayElementAtIndex((int)thumbParent).objectReferenceValue = rootTransform;

            foreach (HandJointId stubId in stubIds)
            {
                //Skip fingers
                if (HandJointUtils.JointToFingerList[(int)stubId] != HandFinger.Invalid)
                {
                    continue;
                }

                HandFingerJointFlags jointAsFlag = (HandFingerJointFlags)(1 << (int)stubId);
                string jointName = ToRegexCase(Enum.GetName(typeof(HandFingerJointFlags), jointAsFlag));
                Transform transform = rootTransform.FindMostSimilarNamedChild(jointName);
                jointsProperty.GetArrayElementAtIndex((int)stubId).objectReferenceValue = transform;
            }
        }

        private static Transform FindTipTransform(HandFinger finger, Transform rootTransform)
        {
            string fingerRegex = _fingerRegexs[finger];

            Transform tipTransform = rootTransform.FindChildRecursive((jointTransform) =>
            {
                string name = SplitCapitals(jointTransform.name);

                Match tipMatch = Regex.Match(name, _tipRegex, RegexOptions.IgnoreCase);
                Match fingerMatch = Regex.Match(name, fingerRegex, RegexOptions.IgnoreCase);

                if (tipMatch.Success && fingerMatch.Success)
                {
                    return true;
                }
                return false;
            });
            return tipTransform;
        }

        private static Transform FindMostSimilarNamedChild(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                string childName = SplitCapitals(child.name);
                Match match = Regex.Match(childName, name, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return child;
                }
            }
            return null;
        }

        private static string ToRegexCase(string input)
        {
            string pattern = @"([A-Z])";
            string replacement = $"{_b}$1";
            return Regex.Replace(input, pattern, replacement);
        }

        private static string SplitCapitals(string input)
        {
            string pattern = @"([A-Z])";
            string replacement = $"_$1";
            return Regex.Replace(input, pattern, replacement);
        }

        private static Transform FindFingersOrigin(Transform hierarchyRoot)
        {
            Transform thumbTransform = FindTipTransform(HandFinger.Thumb, hierarchyRoot);
            List<Transform> commonAncestors = Ancestors(thumbTransform);
            for (HandFinger finger = HandFinger.Index; finger <= HandFinger.Pinky; finger++)
            {
                Transform transform = FindTipTransform(finger, hierarchyRoot);
                List<Transform> fingerAncestors = Ancestors(transform);
                commonAncestors = commonAncestors.Intersect(fingerAncestors).ToList();
            }
            return commonAncestors.FirstOrDefault();

            List<Transform> Ancestors(Transform transform)
            {
                List<Transform> ancestors = new List<Transform>();
                while (transform != null)
                {
                    ancestors.Add(transform);
                    transform = transform.parent;
                }
                return ancestors;
            }
        }
    }
}
