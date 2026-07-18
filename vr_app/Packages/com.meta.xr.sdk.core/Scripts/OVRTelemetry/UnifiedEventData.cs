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
using System.Text;

namespace Meta.XR.Telemetry
{
    /// <summary>
    /// Common annotation type constants for telemetry metadata.
    /// </summary>
    public static class AnnotationType
    {
        public const string Label = "label";
        public const string Url = "url";
        public const string Type = "type";
        public const string Origin = "origin";
        public const string OriginData = "origin_data";
        public const string Action = "action";
        public const string ActionData = "action_data";
        public const string ActionType = "action_type";
        public const string Value = "value";
        public const string SubOrigin = "sub_origin";
    }

    /// <summary>
    /// Unified event result for telemetry.
    /// </summary>
    public enum UnifiedEventResult
    {
        SUCCESS,
        FAIL,
        CANCEL
    }

    /// <summary>
    /// Product type for telemetry events. Mirrors OVRPlugin.ProductType but decoupled from OVRPlugin.
    /// </summary>
    public enum TelemetryProductType
    {
        None,
        Editor,
        XRFeature,
        Pst,
        MetaWand,
        CoreSdk,
        XrSim,
        BuildingBlocks,
        Mruk,
        ImmersiveDebugger,
        PlatformSdk,
        HapticsSdk,
        MovementSdk
    }

    /// <summary>
    /// Result type for telemetry operations. Decoupled from OVRPlugin.Result.
    /// </summary>
    public enum TelemetryResult
    {
        Success = 0,
        Failure = -1000,
        Failure_NotInitialized = -1001,
        Failure_Unsupported = -1002
    }

    /// <summary>
    /// Extension methods for converting between telemetry types and OVRPlugin types.
    /// These are used at the boundary when calling OVRPlugin methods.
    /// </summary>
    internal static class TelemetryTypeConversions
    {
        internal static OVRPlugin.ProductType ToOVRPluginProductType(this TelemetryProductType value)
        {
            return value switch
            {
                TelemetryProductType.None => OVRPlugin.ProductType.None,
                TelemetryProductType.Editor => OVRPlugin.ProductType.Editor,
                TelemetryProductType.XRFeature => OVRPlugin.ProductType.XRFeature,
                TelemetryProductType.Pst => OVRPlugin.ProductType.Pst,
                TelemetryProductType.MetaWand => OVRPlugin.ProductType.MetaWand,
                TelemetryProductType.CoreSdk => OVRPlugin.ProductType.CoreSdk,
                TelemetryProductType.XrSim => OVRPlugin.ProductType.XrSim,
                TelemetryProductType.BuildingBlocks => OVRPlugin.ProductType.BuildingBlocks,
                TelemetryProductType.Mruk => OVRPlugin.ProductType.Mruk,
                TelemetryProductType.ImmersiveDebugger => OVRPlugin.ProductType.ImmersiveDebugger,
                TelemetryProductType.PlatformSdk => OVRPlugin.ProductType.PlatformSdk,
                TelemetryProductType.HapticsSdk => OVRPlugin.ProductType.HapticsSdk,
                TelemetryProductType.MovementSdk => OVRPlugin.ProductType.MovementSdk,
                _ => OVRPlugin.ProductType.None
            };
        }

        internal static TelemetryResult FromOVRPluginResult(OVRPlugin.Result result)
        {
            return result switch
            {
                OVRPlugin.Result.Success => TelemetryResult.Success,
                OVRPlugin.Result.Failure_NotInitialized => TelemetryResult.Failure_NotInitialized,
                OVRPlugin.Result.Failure_Unsupported => TelemetryResult.Failure_Unsupported,
                _ => TelemetryResult.Failure
            };
        }
    }

    /// <summary>
    /// Unified event data for Falco telemetry.
    /// This struct provides a decoupled API in the Meta.XR.Telemetry namespace.
    /// </summary>
    public struct UnifiedEventData
    {
        public bool isEssential;
        public TelemetryProductType productType;
        public string eventName;
        public string metadata_json;
        public string project_name;
        public string entrypoint;
        public string project_guid;
        public string type;
        public string target;
        public string error_msg;
        public bool? is_internal_build;
        public bool? batch_mode;
        public ulong machine_oculus_user_id;
        public int metadataHandle;
        public UnifiedEventResult? result;
        public bool? is_runtime;
        private Dictionary<string, string> MetadataDictionary;

        public UnifiedEventData(string eventName)
        {
            isEssential = false;
            productType = TelemetryProductType.None;
            this.eventName = eventName;
            project_name = "";
            entrypoint = "";
            type = "";
            target = "";
            error_msg = "";
            metadata_json = "";
            batch_mode = null;
            machine_oculus_user_id = 0;
            metadataHandle = 0;
            result = null;
            is_runtime = null;
            MetadataDictionary = null;

#if UNITY_EDITOR
            project_guid = Meta.XR.Editor.Callbacks.InitializeOnLoad.EditorReady
                ? OVRRuntimeSettings.Instance.TelemetryProjectGuid
                : string.Empty;

            if (batch_mode == null)
            {
                batch_mode = UnityEngine.Application.isBatchMode;
            }

#else
            project_guid = OVRRuntimeSettings.Instance.TelemetryProjectGuid;
#endif

            is_internal_build = false;

            if (string.IsNullOrEmpty(project_name))
            {
                var hasConsent = OVRPlugin.UnifiedConsent.GetUnifiedConsent();
                if (hasConsent == true)
                {
                    project_name = UnityEngine.Application.identifier;
                }
            }

            SetMetadata(OVRTelemetryConstants.OVRManager.AnnotationTypes.ProcessorType, UnityEngine.SystemInfo.processorType);
            SetMetadata(OVRTelemetryConstants.OVRManager.AnnotationTypes.FalcoMigrationCentral, "9");
            is_runtime = UnityEngine.Application.isPlaying;
        }

        private bool EnsureMetadataHandle()
        {
            if (metadataHandle != 0)
            {
                return true;
            }

            OVRPlugin.Result createResult = OVRPlugin.TelemetryCreateMetadataHandle(out metadataHandle);
            return createResult == OVRPlugin.Result.Success;
        }

        public bool SetMetadata(string key, string value)
        {
            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataHandleMinVersion)
            {
                MetadataDictionary ??= new Dictionary<string, string>();
                MetadataDictionary[key] = value;
                return true;
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadata(key, value, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public bool SetMetadata(string key, int value)
        {
            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataHandleMinVersion)
            {
                return SetMetadata(key, value.ToString());
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataInt(key, value, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public bool SetMetadata(string key, float value)
        {
            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataHandleMinVersion)
            {
                return SetMetadata(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataFloat(key, value, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public bool SetMetadata(string key, double value)
        {
            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataHandleMinVersion)
            {
                return SetMetadata(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataDouble(key, value, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public bool SetMetadata(string key, bool value)
        {
            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataHandleMinVersion)
            {
                return SetMetadata(key, value.ToString());
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataBool(key, value, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public bool SetMetadata(string key, long value)
        {
            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataArrayMinVersion)
            {
                return SetMetadata(key, value.ToString());
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataLong(key, value, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public unsafe bool SetMetadata(string key, int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataArrayMinVersion)
            {
                return SetMetadata(key, string.Join(",", values));
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            fixed (int* ptr = values)
            {
                OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataIntArray(key, ptr, values.Length, metadataHandle);
                return result == OVRPlugin.Result.Success;
            }
        }

        public unsafe bool SetMetadata(string key, long[] values)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataArrayMinVersion)
            {
                return SetMetadata(key, string.Join(",", values));
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            fixed (long* ptr = values)
            {
                OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataLongArray(key, ptr, values.Length, metadataHandle);
                return result == OVRPlugin.Result.Success;
            }
        }

        public unsafe bool SetMetadata(string key, long* values, int count)
        {
            if (values == null || count <= 0)
            {
                return false;
            }

            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataArrayMinVersion)
            {
                return false;
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataLongArray(key, values, count, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public unsafe bool SetMetadata(string key, double[] values)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataArrayMinVersion)
            {
                return SetMetadata(key, string.Join(",", values.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            fixed (double* ptr = values)
            {
                OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataDoubleArray(key, ptr, values.Length, metadataHandle);
                return result == OVRPlugin.Result.Success;
            }
        }

        public bool SetMetadata(string key, string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataArrayMinVersion)
            {
                return SetMetadata(key, string.Join(",", values));
            }

            if (!EnsureMetadataHandle())
            {
                return false;
            }

            OVRPlugin.Result result = OVRPlugin.TelemetrySetMetadataStringArray(key, values, values.Length, metadataHandle);
            return result == OVRPlugin.Result.Success;
        }

        public string GetMetadata()
        {
            if (OVRPlugin.version < OVRPlugin.TelemetryMetadataHandleMinVersion)
            {
                if (!string.IsNullOrEmpty(metadata_json))
                {
                    return metadata_json;
                }

                return GetJsonFromMetadataDictionary();
            }

            if (metadataHandle == 0)
            {
                return "{}";
            }

            return "{}";
        }

        private string GetJsonFromMetadataDictionary()
        {
            if (MetadataDictionary == null || MetadataDictionary.Count == 0)
            {
                return "{}";
            }

            var sb = new StringBuilder();
            sb.Append("{");
            var first = true;
            foreach (var (key, value) in MetadataDictionary)
            {
                if (!first)
                    sb.Append(",");
                first = false;

                sb.Append($"\n  \"{key}\": \"{value}\"");
            }
            sb.Append("\n}");
            return sb.ToString();
        }

        public bool Send()
        {
            return SendUnifiedEvent(this) == TelemetryResult.Success;
        }


        /// <summary>
        /// Sends a unified telemetry event.
        /// </summary>
        /// <param name="eventData">The event data to send.</param>
        /// <returns>The result of the send operation.</returns>
        public static TelemetryResult SendUnifiedEvent(UnifiedEventData eventData)
        {
#if OVRPLUGIN_UNSUPPORTED_PLATFORM
            return TelemetryResult.Failure_Unsupported;
#else
            var result = OVRPlugin.SendUnifiedEvent(eventData);
            return TelemetryTypeConversions.FromOVRPluginResult(result);
#endif
        }
    }
}
