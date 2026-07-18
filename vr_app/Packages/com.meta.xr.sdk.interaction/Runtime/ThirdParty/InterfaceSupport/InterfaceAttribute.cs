/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;
using System;

namespace Oculus.Interaction
{
    /// <summary>
    /// When this attribute is attached to a <see cref="UnityEngine.Object"/> field within a
    /// <see cref="MonoBehaviour"/>, this allows an interface to be specified in to to
    /// entire only a specific type of object can be attached.
    /// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InterfaceAttribute : PropertyAttribute
    {
        /// <summary>
        /// The interface types that can be assigned to the target
        /// <see cref="UnityEngine.Object"/> field in the inspector
        /// </summary>
        public Type[] Types = null;

        /// <summary>
        /// Objects matching the type of this field will be allowed
        /// to be assigned to the <see cref="UnityEngine.Object"/>
        /// field in the inspector.
        /// </summary>
        public string TypeFromFieldName;

        /// <summary>
        /// Creates a new Interface attribute.
        /// </summary>
        /// <param name="type">The type of interface which is allowed.</param>
        /// <param name="types">Extra types of interface which is allowed.</param>
        public InterfaceAttribute(Type type, params Type[] types)
        {
            Debug.Assert(type.IsInterface, $"{type.Name} needs to be an interface.");

            Types = new Type[types.Length + 1];
            Types[0] = type;
            for (int i = 0; i < types.Length; i++)
            {
                Debug.Assert(types[i].IsInterface, $"{types[i].Name} needs to be an interface.");
                Types[i + 1] = types[i];
            }
        }

        /// <summary>
        /// Creates a new Interface attribute.
        /// </summary>
        /// <param name="typeFromFieldName">The name of the field that will
        /// supply the type constraint used by the interface editor.</param>
        public InterfaceAttribute(string typeFromFieldName)
        {
            this.TypeFromFieldName = typeFromFieldName;
        }
    }
}
