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
using UnityEditor;
using UnityEngine;
using static Oculus.Interaction.Input.HandMirroring;

namespace Oculus.Interaction.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandRootOffset))]
    public class HandRootOffsetEditor : SimplifiedEditor
#if ISDK_OPENXR_HAND
        , IOpenXRMigrableEditor
#endif
    {
        private HandRootOffset _rootOffset;
        private Pose _cachedPose;

#if ISDK_OPENXR_HAND
        private bool _unrollConverter = true;
        private static readonly string[] _ovrPropertyNames =
            {"_offset", "_rotation" };
#else
        private static readonly string[] _openXRPropertyNames =
            {"_posOffset", "_rotOffset" };
#endif

        private void Awake()
        {
            _rootOffset = target as HandRootOffset;
        }

        protected override void OnEnable()
        {
            if (!target)
                return;

            base.OnEnable();
#if ISDK_OPENXR_HAND
            _unrollConverter = NeedsConversion(serializedObject);
            _editorDrawer.Hide(_ovrPropertyNames);
#else
            _editorDrawer.Hide(_openXRPropertyNames);
#endif
        }

#if ISDK_OPENXR_HAND
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            this.OpenXRConversionMenu(ref _unrollConverter, (so) => Convert(so), _ovrPropertyNames);
        }

        public bool NeedsConversion(SerializedObject target)
        {
            ConversionValues(target,
                out Vector3 offsetPosition, out Quaternion rotation);

            GetOpenXRProperties(target,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            return (offsetProp.vector3Value != offsetPosition
                || rotationProp.quaternionValue != rotation);
        }

        public void Convert(SerializedObject target)
        {
            ConversionValues(target, out Vector3 offsetPosition, out Quaternion rotation);

            GetOpenXRProperties(target,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            offsetProp.vector3Value = offsetPosition;
            rotationProp.quaternionValue = rotation;
        }

        private static void ConversionValues(SerializedObject target,
            out Vector3 offsetPosition, out Quaternion rotation)
        {
            GetOVRProperties(target,
                out SerializedProperty ovrOffsetProp,
                out SerializedProperty ovrRotationProp);

            Vector3 localPositionOffset = ovrOffsetProp.vector3Value;
            Quaternion rotationOffset = ovrRotationProp.quaternionValue;
            Pose translatedRoot = TranslateRoot(target, localPositionOffset, rotationOffset);
            offsetPosition = translatedRoot.position;
            rotation = translatedRoot.rotation;
        }

        private static void GetOVRProperties(SerializedObject target,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            offsetProp = target.FindProperty("_offset");
            rotationProp = target.FindProperty("_rotation");
        }

        private static void GetOpenXRProperties(SerializedObject target,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            offsetProp = target.FindProperty("_posOffset");
            rotationProp = target.FindProperty("_rotOffset");
        }

        private static readonly HandSpace _openXRRootLeft = new HandSpace(
            Vector3.forward, Vector3.up, Vector3.right);
        private static readonly HandSpace _openXRRootRight = new HandSpace(
            Vector3.forward, Vector3.up, Vector3.left);
        private static readonly HandSpace _ovrRootLeft = new HandSpace(
            Vector3.right, Vector3.down, Vector3.back);
        private static readonly HandSpace _ovrRootRight = new HandSpace(
            Vector3.left, Vector3.up, Vector3.back);
        private static readonly HandsSpace openXRRootHands = new HandsSpace(_openXRRootLeft, _openXRRootRight);
        private static readonly HandsSpace ovrRootHands = new HandsSpace(_ovrRootLeft, _ovrRootRight);

        internal static Pose TranslateRoot(SerializedObject target, Vector3 localPositionOffset, Quaternion rotationOffset)
        {
            bool handednessFound = TryGetHandedness(target, out Handedness handedness);
            bool mirror = target.FindProperty("_mirrorOffsetsForLeftHand").boolValue;

            Handedness transformHandedness = mirror ? Handedness.Right : handedness;
            HandSpace fromHand = ovrRootHands[transformHandedness];
            HandSpace toHand = openXRRootHands[transformHandedness];

            Vector3 offsetPosition =
                HandMirroring.TransformPosition(localPositionOffset,
                    fromHand, toHand);

            Vector3 forward = TransformPosition(rotationOffset * Vector3.forward, fromHand, toHand);
            Vector3 up = TransformPosition(rotationOffset * Vector3.up, fromHand, toHand);
            Quaternion rotation = Quaternion.LookRotation(forward, up);

            if (!mirror && handedness == Handedness.Left)
            {
                rotation = HandMirroring.Mirror(rotation);
            }

            return new Pose(offsetPosition, rotation);
        }

        private static bool TryGetHandedness(SerializedObject target, out Handedness handedness)
        {
            handedness = Handedness.Right;

            if (target == null)
            {
                return false;
            }

            SerializedProperty handProperty = target.FindProperty("_hand");
            if (handProperty == null)
            {
                if (target.targetObject is Hand hand)
                {
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
                    //We cannot trust the hand.Handedness value during editor time.
                    //It might return Handedness.Left before initialization.
                    handedness = hand.Handedness;
                    return false;
                }
                return false;
            }

            Object handObject = handProperty.objectReferenceValue;
            if (handObject == null)
            {
                return false;
            }

            SerializedObject handSerializedObject = new SerializedObject(handObject);
            return TryGetHandedness(handSerializedObject, out handedness);

        }
#endif

        private void OnSceneGUI()
        {
            _cachedPose.position = _rootOffset.Offset;
            _cachedPose.rotation = _rootOffset.Rotation;

            Pose rootPose = _rootOffset.transform.GetPose();
            _cachedPose.Postmultiply(rootPose);
            DrawAxis(_cachedPose);
        }

        private void DrawAxis(in Pose pose)
        {
            float scale = HandleUtility.GetHandleSize(pose.position);

#if UNITY_2020_2_OR_NEWER
            Handles.color = Color.red;
            Handles.DrawLine(pose.position, pose.position + pose.right * scale, EditorConstants.LINE_THICKNESS);
            Handles.color = Color.green;
            Handles.DrawLine(pose.position, pose.position + pose.up * scale, EditorConstants.LINE_THICKNESS);
            Handles.color = Color.blue;
            Handles.DrawLine(pose.position, pose.position + pose.forward * scale, EditorConstants.LINE_THICKNESS);
#else
            Handles.color = Color.red;
            Handles.DrawLine(pose.position, pose.position + pose.right * scale);
            Handles.color = Color.green;
            Handles.DrawLine(pose.position, pose.position + pose.up * scale);
            Handles.color = Color.blue;
            Handles.DrawLine(pose.position, pose.position + pose.forward * scale);
#endif
        }
    }
}
