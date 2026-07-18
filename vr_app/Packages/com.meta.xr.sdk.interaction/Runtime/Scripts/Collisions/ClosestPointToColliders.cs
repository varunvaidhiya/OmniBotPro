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

using UnityEngine;

namespace Oculus.Interaction
{
    public static partial class Collisions
    {
        public static Vector3 ClosestPointToColliders(Vector3 point, Collider[] colliders)
        {
            Vector3 closestPoint = point;
            float closestSqrDistance = float.PositiveInfinity;
            foreach (Collider collider in colliders)
            {
                Vector3 closest = ClosestPointToCollider(point, collider);
                float sqrDistance = Vector3.SqrMagnitude(closest - point);

                if (sqrDistance <= float.Epsilon)
                {
                    return closest;
                }

                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestPoint = closest;
                }
            }

            return closestPoint;
        }

        public static Vector3 ClosestPointToCollider(Vector3 point, Collider collider)
        {
            if (collider is MeshCollider meshCollider)
            {
                //Convex Mesh Colliders have a small error near the edges. So we magnetize the point a bit
                if (meshCollider.convex)
                {
                    Vector3 convexColliderPoint = Physics.ClosestPoint(point, collider, collider.transform.position, collider.transform.rotation);
                    if (Vector3.SqrMagnitude(convexColliderPoint - point) < collider.contactOffset * collider.contactOffset)
                    {
                        return point;
                    }
                    return convexColliderPoint;
                }
                //Concave Mesh Colliders always return the point itself when using Physics.ClosestPoint.
                else
                {
                    return meshCollider.ClosestPointOnBounds(point);
                }
            }

            return Physics.ClosestPoint(point, collider, collider.transform.position, collider.transform.rotation);
        }
    }
}
