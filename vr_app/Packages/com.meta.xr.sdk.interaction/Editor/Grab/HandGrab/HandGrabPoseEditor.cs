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

using Oculus.Interaction.Editor;
using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandGrabPose))]
    public class HandGrabPoseEditor : SimplifiedEditor
#if ISDK_OPENXR_HAND
        , IOpenXRMigrableEditor
#endif
    {
        private HandGrabPose _handGrabPose;
        private HandGhost _handGhost;
        private Handedness _lastHandedness;
        private Transform _relativeTo;

#if !ISDK_OPENXR_HAND
        private bool _openXRCompatFoldout = false;
#endif
        private int _editMode = 0;
        private SerializedProperty _handPoseProperty;
        private SerializedProperty _relativeToProperty;
        private SerializedProperty _ghostProviderProperty;

        private const float GIZMO_SCALE = 0.005f;
        private static readonly string[] EDIT_MODES = new string[] { "Edit fingers", "Follow Surface" };

#if ISDK_OPENXR_HAND
        private bool _unrollConverter = true;
        private static readonly string[] _ovrPropertyNames =
            {"_handPose" };
#endif

        private void Awake()
        {
            _handGrabPose = target as HandGrabPose;
        }

        protected override void OnEnable()
        {
            if (!target)
                return;

            base.OnEnable();

            _editorDrawer.Hide("_ovrOffsetMode");
            _editorDrawer.Hide("_handPose", "_targetHandPose");

#if ISDK_OPENXR_HAND
            GetOpenXRProperties(serializedObject, out _handPoseProperty);
            _unrollConverter = NeedsConversion(serializedObject);
#else
            GetOVRProperties(serializedObject, out _handPoseProperty);
#endif
            _relativeToProperty = serializedObject.FindProperty("_relativeTo");
#if ISDK_OPENXR_HAND
            _ghostProviderProperty = serializedObject.FindProperty("_handGhostProvider");
#else
            _ghostProviderProperty = serializedObject.FindProperty("_ghostProvider");
#endif
            AssignMissingGhostProvider();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            DestroyGhost();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _relativeTo = _relativeToProperty.objectReferenceValue as Transform;

            if (_handGrabPose.UsesHandPose())
            {
                EditorGUILayout.PropertyField(_handPoseProperty);
            }

            GUIStyle boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField("Interactive Edition (Editor only)", boldStyle);
            if (_handGrabPose.UsesHandPose())
            {
                DrawGhostMenu(_handGrabPose.HandPose);
            }
            else
            {
                DestroyGhost();
            }

#if ISDK_OPENXR_HAND
            this.OpenXRConversionMenu(ref _unrollConverter,
                (so) => Convert(so),
                    _ovrPropertyNames);
#else
            GetOVROffsetMode(serializedObject, out SerializedProperty ovrOffsetMode);
            if (ovrOffsetMode.intValue != (int)HandGrabPose.OVROffsetMode.None)
            {
                _openXRCompatFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                    _openXRCompatFoldout, "OpenXR Compatibility");
                if (_openXRCompatFoldout)
                {
                    EditorGUI.indentLevel++;

                    GUI.enabled = false;
                    Pose offset = HandGrabPose.GetOVROffset(_handGrabPose.HandPose.Handedness);
                    EditorGUILayout.Vector3Field("Position Offset", offset.position);
                    EditorGUILayout.Vector3Field("Rotation Offset", offset.rotation.eulerAngles);
                    GUI.enabled = !EditorApplication.isPlaying;

                    bool applyOffset = ovrOffsetMode.intValue == (int)HandGrabPose.OVROffsetMode.Apply;
                    applyOffset = EditorGUILayout.Toggle("Apply Offset", applyOffset);
                    ovrOffsetMode.intValue = applyOffset ?
                        (int)HandGrabPose.OVROffsetMode.Apply : (int)HandGrabPose.OVROffsetMode.Ignore;

                    GUI.enabled = true;
                    EditorGUILayout.HelpBox("This hand pose was originally set with the OVR hand skeleton, and " +
                        "has been converted to use OpenXR hand poses. This offset is being applied to the local position " +
                        "of this transform for backwards compatibility of built-in assets with the OVR hand.",
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
#endif
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGhostMenu(HandPose handPose)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_ghostProviderProperty);
            bool providerChanged = EditorGUI.EndChangeCheck();

            if (_handGhost == null
                || providerChanged
                || _lastHandedness != handPose.Handedness)
            {
                RegenerateGhost();
            }
            _lastHandedness = handPose.Handedness;

            if (_handGrabPose.SnapSurface == null)
            {
                _editMode = 0;
            }
            else
            {
                _editMode = GUILayout.Toolbar(_editMode, EDIT_MODES);
            }
        }

        public void OnSceneGUI()
        {
            if (SceneView.currentDrawingSceneView == null)
            {
                return;
            }

            if (_handGhost == null)
            {
                return;
            }

            if (_editMode == 0)
            {
                GhostEditFingers();
            }
            else if (_editMode == 1)
            {
                GhostFollowSurface();
            }
        }


        #region Ghost

        private void AssignMissingGhostProvider()
        {
            if (_ghostProviderProperty.objectReferenceValue as HandGhostProvider != null)
            {
                return;
            }


            if (HandGhostProviderUtils.TryGetDefaultProvider(out var ghostVisualsProvider))
            {
                _ghostProviderProperty.objectReferenceValue = ghostVisualsProvider;
                serializedObject.ApplyModifiedProperties();

            }
        }

        private void RegenerateGhost()
        {
            DestroyGhost();
            CreateGhost();
        }

        private void CreateGhost()
        {
            if (_ghostProviderProperty.objectReferenceValue is not HandGhostProvider ghostVisualsProvider)
            {
                return;
            }
            Transform relativeTo = _handGrabPose.RelativeTo;
            HandGhost ghostPrototype = ghostVisualsProvider.GetHand(_handGrabPose.HandPose.Handedness);
            _handGhost = GameObject.Instantiate(ghostPrototype, _handGrabPose.transform);
            _handGhost.gameObject.hideFlags = HideFlags.HideAndDontSave;

            Pose relativePose = _handGrabPose.RelativePose;
            Pose pose = PoseUtils.GlobalPoseScaled(relativeTo, relativePose);
            _handGhost.SetPose(_handGrabPose.HandPose, pose);
        }

        private void DestroyGhost()
        {
            if (_handGhost == null)
            {
                return;
            }

            GameObject.DestroyImmediate(_handGhost.gameObject);
        }

        private void GhostFollowSurface()
        {
            if (_handGhost == null)
            {
                return;
            }

            Pose ghostTargetPose = _handGrabPose.RelativePose;

            if (_handGrabPose.SnapSurface != null)
            {
                Vector3 mousePosition = Event.current.mousePosition;
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                if (_handGrabPose.SnapSurface.CalculateBestPoseAtSurface(ray, out Pose poseInSurface, _relativeTo))
                {
                    ghostTargetPose = PoseUtils.DeltaScaled(_relativeTo, poseInSurface);
                }
            }

            _handGhost.SetRootPose(ghostTargetPose, _relativeTo);
            Handles.color = EditorConstants.PRIMARY_COLOR_DISABLED;
            Handles.DrawSolidDisc(_handGhost.transform.position, _handGhost.transform.right, 0.01f);
        }

        private void GhostEditFingers()
        {
            HandPuppet puppet = _handGhost.GetComponent<HandPuppet>();
            if (puppet != null && puppet.JointMaps != null)
            {
                DrawBonesRotator(puppet.JointMaps);
            }
        }

        private void DrawBonesRotator(List<HandJointMap> bones)
        {
            bool anyChanged = false;
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                bool changed = false;
                HandJointId joint = FingersMetadata.HAND_JOINT_IDS[i];
                HandFinger finger = HandJointUtils.JointToFingerList[(int)joint];

                if (_handGrabPose.HandPose.FingersFreedom[(int)finger] == JointFreedom.Free)
                {
                    continue;
                }

                HandJointMap jointMap = bones.Find(b => b.id == joint);
                if (jointMap == null)
                {
                    continue;
                }

                if (i >= _handGrabPose.HandPose.JointRotations.Length)
                {
                    return;
                }

                if (!FingersMetadata.HAND_JOINT_CAN_MOVE[i])
                {
                    continue;
                }

                Transform transform = jointMap.transform;
                transform.localRotation = jointMap.RotationOffset * _handGrabPose.HandPose.JointRotations[i];

                float scale = GIZMO_SCALE * _handGrabPose.transform.lossyScale.x;
                Handles.color = EditorConstants.PRIMARY_COLOR;
                Quaternion entryRotation = transform.rotation;
                Quaternion rotation = Handles.Disc(entryRotation, transform.position,
                   transform.rotation * Constants.RightThumbSide, scale, false, 0);
                if (rotation != entryRotation)
                {
                    changed = true;
                }

                if (FingersMetadata.HAND_JOINT_CAN_SPREAD[i])
                {
                    Handles.color = EditorConstants.SECONDARY_COLOR;
                    Quaternion curlRotation = rotation;
                    rotation = Handles.Disc(curlRotation, transform.position,
                        transform.rotation * Constants.RightDorsal, scale, false, 0);
                    if (rotation != curlRotation)
                    {
                        changed = true;
                    }
                }

                if (!changed)
                {
                    continue;
                }

                transform.rotation = rotation;
                Undo.RecordObject(_handGrabPose, "Bone Rotation");
                _handGrabPose.HandPose.JointRotations[i] = jointMap.TrackedRotation;
                anyChanged = true;
            }

            if (anyChanged)
            {
                EditorUtility.SetDirty(_handGrabPose);
            }
        }
        #endregion

        #region Translation

        private static void GetOVRProperties(SerializedObject target,
           out SerializedProperty handPoseProp)
        {
            handPoseProp = target.FindProperty("_handPose");
        }

        private static void GetOpenXRProperties(SerializedObject target,
           out SerializedProperty handPoseProp)
        {
            handPoseProp = target.FindProperty("_targetHandPose");
        }

        private void GetOVROffsetMode(SerializedObject target,
                out SerializedProperty shouldApply)
        {
            shouldApply = target.FindProperty("_ovrOffsetMode");
        }

#if ISDK_OPENXR_HAND
        public bool NeedsConversion(SerializedObject target)
        {
            GetOpenXRProperties(target,
                out SerializedProperty openXRHandPoseProp);
            GetOVRProperties(target,
                out SerializedProperty ovrHandPoseProp);

            SerializedProperty fromHandedness = ovrHandPoseProp.FindPropertyRelative("_handedness");
            SerializedProperty fromJoints = ovrHandPoseProp.FindPropertyRelative("_jointRotations");
            SerializedProperty toJoints = openXRHandPoseProp.FindPropertyRelative("_jointRotations");
            Handedness handedness = (Handedness)fromHandedness.intValue;

            if (toJoints.arraySize != FingersMetadata.HAND_JOINT_IDS.Length)
            {
                return true;
            }

            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
                int ovrIdx = HandTranslationUtils.HAND_JOINT_IDS_OpenXRtoOVR[i];
                if (ovrIdx < 0)
                {
                    continue;
                }

                Quaternion desiredRotation =
                    GetTransformedRotation(handedness, jointID, fromJoints, ovrIdx);
                Quaternion currentRotation = toJoints.GetArrayElementAtIndex(i).quaternionValue;

                if (Quaternion.Angle(currentRotation, desiredRotation) > 0.1f)
                {
                    return true;
                }
            }
            return false;
        }

        public void Convert(SerializedObject target)
        {
            GetOpenXRProperties(target, out SerializedProperty openXRHandPoseProp);
            GetOVRProperties(target, out SerializedProperty ovrHandPoseProp);

            if (target.targetObject is not HandGrabPose handGrabPose)
            {
                return;
            }

            Pose ovrRoot = handGrabPose.transform.GetPose(Space.Self);
            HandPoseOVRToOpenXR(ovrHandPoseProp, openXRHandPoseProp, ref ovrRoot);

            GetOVROffsetMode(target, out SerializedProperty ovrOffsetMode);
            if (ovrOffsetMode.intValue == (int)HandGrabPose.OVROffsetMode.None)
            {
                ovrOffsetMode.intValue = (int)HandGrabPose.OVROffsetMode.Apply;
                Matrix4x4 unmodified = handGrabPose.transform.localToWorldMatrix;
                handGrabPose.transform.SetPose(ovrRoot, Space.Self);
                foreach (Transform child in handGrabPose.transform)
                {
                    // Restore children to previous pose
                    child?.SetPositionAndRotation(
                        unmodified.MultiplyPoint(child.localPosition),
                        unmodified.rotation * child.localRotation);
                }
            }
        }

        private static void HandPoseOVRToOpenXR(SerializedProperty from, SerializedProperty to, ref Pose root)
        {
            SerializedProperty fromHandedness = from.FindPropertyRelative("_handedness");
            SerializedProperty toHandedness = to.FindPropertyRelative("_handedness");
            toHandedness.intValue = fromHandedness.intValue;

            SerializedProperty fromFingersFreedomProp = from.FindPropertyRelative("_fingersFreedom");
            SerializedProperty toFingersFreedomProp = to.FindPropertyRelative("_fingersFreedom");
            for (int i = 0; i < fromFingersFreedomProp.arraySize; i++)
            {
                toFingersFreedomProp.GetArrayElementAtIndex(i).intValue =
                    fromFingersFreedomProp.GetArrayElementAtIndex(i).intValue;
            }

            SerializedProperty fromJoints = from.FindPropertyRelative("_jointRotations");
            SerializedProperty toJoints = to.FindPropertyRelative("_jointRotations");
            Handedness handedness = (Handedness)fromHandedness.intValue;
            //prepopulate the rotations with the default skeleton values in case some are missing
            IReadOnlyHandSkeletonJointList jointCollection = handedness == Handedness.Left ?
                HandSkeleton.DefaultLeftSkeleton : HandSkeleton.DefaultRightSkeleton;

            toJoints.arraySize = FingersMetadata.HAND_JOINT_IDS.Length;
            //replace the OVR rotations with the OpenXR equivalent
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
                int ovrIdx = HandTranslationUtils.HAND_JOINT_IDS_OpenXRtoOVR[i];

                if (ovrIdx < 0)
                {
                    toJoints.GetArrayElementAtIndex(i).quaternionValue = jointCollection[(int)jointID].pose.rotation;
                    continue;
                }

                toJoints.GetArrayElementAtIndex(i).quaternionValue =
                    GetTransformedRotation(handedness, jointID, fromJoints, ovrIdx);
            }

            Quaternion offsetRotation = root.rotation;
            //Special case: This is a root operation, not a wrist operation,
            //in OVR the Left needs to have its offset undone.
            if (handedness == Handedness.Left)
            {
                offsetRotation *= Quaternion.Euler(0f, 180f, 0f);
            }
            root.rotation = offsetRotation
                            * Quaternion.Inverse(HandTranslationUtils.ovrHands[handedness].rotation)
                            * HandTranslationUtils.openXRHands[handedness].rotation;
        }

        private static Quaternion GetTransformedRotation(Handedness handedness, HandJointId jointID,
            SerializedProperty jointsProp, int index)
        {
            Quaternion rotation;
            if (jointID == HandJointId.HandThumb1)
            {
                rotation = jointsProp.GetArrayElementAtIndex(index - 1).quaternionValue
                    * jointsProp.GetArrayElementAtIndex(index).quaternionValue;
            }
            else
            {
                rotation = jointsProp.GetArrayElementAtIndex(index).quaternionValue;
            }
            return HandMirroring.TransformRotation(rotation,
                HandTranslationUtils.ovrHands[handedness],
                HandTranslationUtils.openXRHands[handedness]);
        }
#endif
        #endregion
    }
}
