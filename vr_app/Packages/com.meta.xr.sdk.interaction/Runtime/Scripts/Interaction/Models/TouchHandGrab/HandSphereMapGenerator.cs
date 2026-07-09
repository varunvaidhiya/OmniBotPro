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
using UnityEngine;

#if UNITY_EDITOR
namespace Oculus.Interaction
{
    /// <summary>
    /// Generate a mapping of joints to spheres from a HandPrefabDataSource
    /// that has a set of transforms representing sphere positions and radii.
    /// </summary>
    public static class HandSphereMapGenerator
    {
        private static readonly float SphereRadiusMin = 0.005f;
        private static readonly float SphereRadiusMax = 0.0095f;
        private static readonly float SphereRadiusThumb = 0.010f;
        private static readonly string SphereName = "sphere";

        public static void RegenerateJointSpheres(FromHandPrefabDataSource handPrefabDataSource)
        {
            // Destroy existing spheres relative to the configured wrist root
            var wristRoot = handPrefabDataSource.GetTransformFor(HandJointId.HandWristRoot);
            DestroyExistingSpheres(wristRoot);

            // Create new spheres
            for (var i = 0; i < Constants.NUM_FINGERS; i++)
            {
                var joints = HandJointUtils.FingerToJointList[i];
                for (var ii = joints.Length - 1; ii > 1; ii--)
                {
                    var joint = handPrefabDataSource.GetTransformFor(joints[ii]);
                    var parentJoint = handPrefabDataSource.GetTransformFor(joints[ii - 1]);

                    var position = joint.position;
                    var parentPosition = parentJoint.position;
                    Vector3 direction = (position - parentPosition).normalized;
                    float distance = Vector3.Distance(position, parentPosition);

                    Vector3 middleSpherePosition = parentPosition + direction * (distance/2);

                    var sphereRadius = Mathf.Lerp(SphereRadiusMax, SphereRadiusMin, (ii) / (joints.Length - 1.0f));

                    Vector3 leftSpherePosition = parentPosition + direction * (distance - sphereRadius);
                    Vector3 rightSpherePosition = position - direction * (distance - sphereRadius);

                    foreach (var pos in new[]
                             {
                                 leftSpherePosition, middleSpherePosition, rightSpherePosition
                             })
                    {
                        CreateSphere(parentJoint, pos, sphereRadius);
                    }
                    UnityEditor.EditorUtility.SetDirty(parentJoint);
                }
            }
#if ISDK_OPENXR_HAND
            // OpenXR does not have thumb0
            var thumb1 = handPrefabDataSource.GetTransformFor(HandJointId.HandThumb1);
            CreateSphere(thumb1, thumb1.position, SphereRadiusThumb);
#else
            var thumb0 = handPrefabDataSource.GetTransformFor(HandJointId.HandThumb0);
            CreateSphere(thumb0, thumb0.position, SphereRadiusThumb);
#endif

            UnityEditor.EditorUtility.SetDirty(wristRoot);
        }

        private static void CreateSphere(Transform parentTransform, Vector3 position, float sphereRadius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
            go.transform.SetParent(parentTransform);
            go.name = SphereName;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (sphereRadius * 2);
        }

        private static void DestroyExistingSpheres(Transform target)
        {
            for (int i = 0; i < target.childCount; i++)
            {
                var child = target.GetChild(i);
                if (child.childCount > 0)
                {
                    DestroyExistingSpheres(child);
                }
                if (child.gameObject.name == SphereName)
                {
                    Object.DestroyImmediate(child.gameObject);
                    i--;
                }
            }
        }
    }
}
#endif
