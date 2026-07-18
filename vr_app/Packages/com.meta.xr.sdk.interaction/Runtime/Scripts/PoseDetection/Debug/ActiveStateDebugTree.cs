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

using Oculus.Interaction.DebugTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection.Debug
{
    /// <summary>
    /// Builds a visual representation of an IActiveState tree on a UI canvas, based on a provided root <cref="IActiveState" />. Two different node layout types are provided, ActiveStateDebugTreeHorizontal (for a left-to-right representation) and ActiveStateDebugTreeVertical (for a more compact top to bottom tree view).
    /// </summary>
    public class ActiveStateDebugTree : DebugTree<IActiveState>
    {
        public ActiveStateDebugTree(IActiveState root) : base(root)
        {
        }

        private static Dictionary<Type, IActiveStateModel> _models =
            new Dictionary<Type, IActiveStateModel>();

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void RegisterModel<TType>(IActiveStateModel stateModel)
            where TType : class, IActiveState
        {
            _models[typeof(TType)] = stateModel;
        }

        protected override async Task<IEnumerable<IActiveState>> TryGetChildrenAsync(IActiveState node)
        {
            if (_models.TryGetValue(node.GetType(), out IActiveStateModel model))
            {
                return await model.GetChildrenAsync(node);
            }
            return Enumerable.Empty<IActiveState>();
        }
    }
}
