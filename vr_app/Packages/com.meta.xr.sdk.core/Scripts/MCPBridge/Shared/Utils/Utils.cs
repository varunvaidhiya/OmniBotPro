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
using System.Threading.Tasks;

namespace Meta.MCPBridge.Utils
{
    internal static class TaskExtensions
    {
        internal static Task Then(this Task first, Task second)
        {
            return first.ContinueWith(_ => second, TaskScheduler.Default).Unwrap();
        }
    }

    internal static class JsonExtensions
    {
        internal static string GetJsonSchemaType(this Type type)
        {
            return type switch
            {
                _ when type == typeof(string) => "string",
                _ when type == typeof(int) || type == typeof(long) || type == typeof(short) => "integer",
                _ when type == typeof(float) || type == typeof(double) || type == typeof(decimal) => "number",
                _ when type == typeof(bool) => "boolean",
                _ => "string"
            };
        }
    }
}
