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

#if USING_XR_COMPOSITION_LAYERS && USING_XR_SDK_OPENXR
using System;
using UnityEngine;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CompositionLayers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;

namespace Meta.XR
{
    /// <summary>
    /// Subclass of <see cref="CompositionLayerExtension" /> to support
    /// secure content for the <see cref="CompositionLayer"/> instance
    /// on the same game object.
    ///
    /// Support for this component is up the the instance of <see cref="ILayerProvider" />
    /// currently assigned to the <see cref="Unity.XR.CompositionLayers.Services.CompositionLayerManager" />.
    ///
    /// If this extension is not added to a layer game object, it is expected that
    /// the provider will assume the layer contains no secure content.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/Composition Layers/Extensions/Secure Content")]
    public class SecureContentExtension : CompositionLayerExtension
    {
        // Keep in sync with XrCompositionLayerSecureContentFlagsFB
        public enum XrCompositionLayerSecureContentFlags
        {
            None = 0x0,
            ExcludeLayer = 0x1,
            ReplaceLayer = 0x2,
        }

        /// <summary>
        /// Options for which type of object this extension should be associated with.
        /// </summary>
        public override ExtensionTarget Target => ExtensionTarget.Layer;

        [FormerlySerializedAs("m_LayerFlags")]
        [SerializeField]
        [Tooltip("The secure content flags for this layer")]
        XrCompositionLayerSecureContentFlags m_Flags = 0;

        NativeArray<XrCompositionLayerSecureContentFB> m_NativeArray;

        /// <summary>
        /// The value used determine how secure content will be rendered when viewed from external sources
        /// </summary>
        public XrCompositionLayerSecureContentFlags Flags
        {
            get => m_Flags;
            set => m_Flags = UpdateValue(m_Flags, value);
        }

        ///<summary>
        /// Return a pointer to this extension's native struct.
        /// </summary>
        /// <returns>the pointer to the secure content extension's native struct.</returns>
        public override unsafe void* GetNativeStructPtr()
        {
            var secureContent = new XrCompositionLayerSecureContentFB
            {
                Type = XrCompositionLayerSecureContentFB.StructureType,
                Flags = (XrCompositionLayerSecureContentFlagsFB)Flags,
            };

            if (!m_NativeArray.IsCreated)
                m_NativeArray = new NativeArray<XrCompositionLayerSecureContentFB>(1, Allocator.Persistent);

            m_NativeArray[0] = secureContent;
            return m_NativeArray.GetUnsafePtr();
        }

        /// <inheritdoc cref="MonoBehaviour"/>
        public override void OnDestroy()
        {
            base.OnDestroy();

            if (m_NativeArray.IsCreated)
                m_NativeArray.Dispose();
        }
    }
}
#endif
