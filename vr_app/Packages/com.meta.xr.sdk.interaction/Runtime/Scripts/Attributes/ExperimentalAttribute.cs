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

namespace Oculus.Interaction
{
    /// <summary>
    /// This attribute can be applied to both classes and serialized properties to mark them as
    /// experimental and subject to change. Message boxes will be displayed in the inspector for
    /// components & properties, but only if the component editor extends from SimplifiedEditor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class ExperimentalAttribute : PropertyAttribute
    {
        public const string CLASS_HEADER = "{0} is for evaluation purposes only " +
            "and is subject to change or removal in future updates.";

        public const string MEMBER_HEADER = "These properties are for evaluation purposes only " +
            "and are subject to change or removal in future updates.";

        /// <summary>
        /// When the <see cref="ExperimentalAttribute"/> is applied to a class, this message will
        /// be appended to the default inspector header. This property is not used when the
        /// <see cref="ExperimentalAttribute"/> is applied to a member.
        /// </summary>
        public string ClassMessage { get; set; }

        public ExperimentalAttribute()
        {
        }
    }
}
