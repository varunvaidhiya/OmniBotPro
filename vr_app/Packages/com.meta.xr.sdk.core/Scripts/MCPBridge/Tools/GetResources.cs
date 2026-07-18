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
using System.Linq;
using System.Threading.Tasks;
using Meta.MCPBridge;
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Schemas;
using Meta.MCPBridge.Services;

namespace MCPServices.Tools
{
    [Tool(
        "Access read-only reference data (API docs, type info, enum definitions, concept guides) registered as MCP resources.",
        "WHEN TO USE: When you need documentation or type details that are NOT available by inspecting live objects. Call ListResources first to discover what is available, then GetResource with a URI.",
        "WORKFLOW: 1) ListResources to see available URIs 2) GetResource(uri) to fetch content."
    )]
    internal interface IGetResourcesService : IService
    {
        [Tool(Description = "List the available resources (and their URI)")]
        public Task<IEnumerable<ResourceSchema>> ListResources();

        [Tool(
            Description =
                "Get specific resource by URI with additional parameter, ideal to get more information about types, enums, concepts.",
            Returns = "The resource requested, similar to how it would be passed from the resources.")]
        public Task<ResourceGetDataSchema> GetResource(string uri);
    }

    internal class GetResourcesService : SingletonService<GetResourcesService>, IGetResourcesService
    {
        public async Task<IEnumerable<ResourceSchema>> ListResources()
        {
            if (await LocalExecutor.instance.ResourceRegistry.ToSchema() is not ResourceListResultSchema result)
                throw new InvalidOperationException("Could not get list of resources");

            return result.Resources;
        }

        public async Task<ResourceGetDataSchema> GetResource(string uri)
        {
            var resource = await LocalExecutor.instance.ResourceRegistry.Fetch(uri);
            if (resource == null)
                throw new InvalidOperationException($"Could not find resource {uri}");

            var result = await resource.Read(LocalExecutor.instance);
            if (result is not ResourceGetResultSchema getResult)
                throw new InvalidOperationException($"Could not get results for {uri}");

            var content = getResult.Content.FirstOrDefault();
            if (content == null)
                throw new InvalidOperationException($"Empty result for {uri}");

            return content;
        }
    }
}
