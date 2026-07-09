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

using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Schemas;

namespace Meta.MCPBridge.Definitions
{
    internal abstract class Definition
    {
        internal abstract string Name { get; set; }
        internal abstract ISchema ToBaseSchema();
    }

    internal abstract class Definition<TSchema> : Definition where TSchema : ISchema
    {
        internal abstract TSchema ToSchema();

        internal override ISchema ToBaseSchema()
        {
            return ToSchema();
        }
    }

    internal abstract class Definition<TAttribute, TSchema> : Definition<TSchema>
        where TAttribute : DefinitionAttribute
        where TSchema : ISchema
    {
        protected Definition(TAttribute attribute)
        {
            Attribute = attribute;
        }

        internal TAttribute Attribute { get; }
    }
}
