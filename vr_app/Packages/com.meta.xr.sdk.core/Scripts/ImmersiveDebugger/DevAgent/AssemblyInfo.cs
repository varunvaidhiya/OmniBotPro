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

using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

// Prevent IL2CPP from stripping this assembly during builds
// This is critical for reflection-based panel registration
[assembly: AlwaysLinkAssembly]

// Allow test assemblies to access internal members
[assembly: InternalsVisibleTo("Meta.XR.ImmersiveDebugger.DevAgent.Tests.EditMode")]
[assembly: InternalsVisibleTo("Meta.XR.ImmersiveDebugger.DevAgent.Tests.PlayMode")]

// Allow Editor assembly to access internal members
[assembly: InternalsVisibleTo("Meta.XR.ImmersiveDebugger.DevAgent.Editor")]
