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

using Meta.XR.Editor.Id;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Editor-only extension methods for CallerIdentity to support IIdentified.
    /// </summary>
    public static class CallerIdentityExtensions
    {
        /// <summary>
        /// Create a CallerIdentity from an IIdentified object (Editor-only).
        /// Allows Editor code to easily create CallerIdentity from IIdentified objects.
        /// </summary>
        /// <param name="identified">The IIdentified object to create from</param>
        /// <returns>A new CallerIdentity with the ID from the IIdentified object</returns>
        public static CallerIdentity ToCallerIdentity(this IIdentified identified)
        {
            return new CallerIdentity(identified?.Id ?? string.Empty);
        }
    }
}
