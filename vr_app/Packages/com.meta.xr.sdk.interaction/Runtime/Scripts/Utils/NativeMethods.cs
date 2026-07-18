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

#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || (UNITY_ANDROID && !UNITY_EDITOR))
#define ISDK_NATIVE_UNSUPPORTED_PLATFORM
#endif

using System.Runtime.InteropServices;
using System.Security;

namespace Oculus.Interaction
{
    /// <summary>
    /// Native methods for Interactor
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        public const int IsdkSuccess = 0;

#if !ISDK_NATIVE_UNSUPPORTED_PLATFORM
        [DllImport("InteractionSdk")]
        public static extern int isdk_NativeComponent_Activate(ulong id);
#else
        public static int isdk_NativeComponent_Activate(ulong id) => IsdkSuccess;
#endif
    }
}
