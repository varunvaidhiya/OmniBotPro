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

namespace Meta.MCPBridge.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    internal class ToolAttribute : DefinitionAttribute
    {
        private readonly List<string> _remarks = new();

        public string Returns { get; set; }

        /// <summary>
        /// Gets the list of remarks for this tool.
        /// </summary>
        internal IReadOnlyList<string> Remarks => _remarks;

        /// <summary>
        /// Adds a remark to this tool.
        /// </summary>
        /// <param name="remark">The remark to add.</param>
        internal void AddRemark(string remark)
        {
            if (!string.IsNullOrEmpty(remark))
            {
                _remarks.Add(remark);
            }
        }

        /// <summary>
        /// Creates a new instance of the ToolAttribute class.
        /// </summary>
        internal ToolAttribute()
        {
        }

        /// <summary>
        /// Creates a new instance of the ToolAttribute class with the specified description.
        /// </summary>
        /// <param name="description">The description of the tool.</param>
        internal ToolAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Creates a new instance of the ToolAttribute class with the specified description and remarks.
        /// </summary>
        /// <param name="description">The description of the tool.</param>
        /// <param name="remarks">The remarks for the tool.</param>
        internal ToolAttribute(string description, params string[] remarks)
        {
            Description = description;

            if (remarks != null)
            {
                foreach (var remark in remarks)
                {
                    AddRemark(remark);
                }
            }
        }
    }
}
