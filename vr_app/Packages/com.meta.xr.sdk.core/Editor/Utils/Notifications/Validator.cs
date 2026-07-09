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
using Meta.XR.Editor.Settings;
using Meta.XR.Editor.ToolingSupport;
using UnityEngine;

namespace Meta.XR.Editor.Notifications
{
    internal class Validator
    {
        private abstract class FilterChecker
        {
            public abstract bool CheckCondition(string @operator, string value);
        }

        private abstract class BaseFilterChecker<T> : FilterChecker
        {
            protected abstract T GetField();

            public override bool CheckCondition(string @operator, string value)
            {
                if (!TryParse(value, out var parsedValue))
                {
                    return false;
                }

                var field = GetField();

                return @operator switch
                {
                    "=" => Equals(field, parsedValue),
                    "!=" => !Equals(field, parsedValue),
                    ">" when field is IComparable comparableField => comparableField.CompareTo(parsedValue) > 0,
                    "<" when field is IComparable comparableField => comparableField.CompareTo(parsedValue) < 0,
                    ">=" when field is IComparable comparableField => comparableField.CompareTo(parsedValue) >= 0,
                    "<=" when field is IComparable comparableField => comparableField.CompareTo(parsedValue) <= 0,
                    _ => false
                };
            }

            private static bool TryParse(string value, out T result)
            {
                try
                {
                    if (typeof(T) == typeof(Version))
                    {
                        result = (T)(object)Version.Parse(value);
                        return true;
                    }

                    result = (T)Convert.ChangeType(value, typeof(T));
                    return true;
                }
                catch
                {
                    result = default;
                    return false;
                }
            }
        }

        private class ValueFilterChecker<T> : BaseFilterChecker<T>
        {
            private readonly T _field;

            public ValueFilterChecker(T field)
            {
                _field = field;
            }

            protected override T GetField() => _field;
        }

        private readonly IReadOnlyDictionary<string, FilterChecker> _checkers;

        internal struct ToolUsageData
        {
            public string ToolId;
            public bool IsUsed;
            public int TimesUsed;
            public int DaysSinceLastUsed;
            public int LastUsedInSdkVersion;
        }

        internal struct UsageData
        {
            public int NumberOfActiveSessions;
            public int DaysSinceActivation;
            public int? CurrentSdkVersion;
            public int InitialSdkVersion;
            public int LastUsedSdkVersion;
            public bool IsFirstSessionAfterSdkUpdate;

            public static UsageData Resolve()
            {
                var sdkVersion = ToolUsage.GetSdkVersion();
                var previousSdkVersion = UsageSettings.LastUsedSDKVersion.Value;

                if (sdkVersion.HasValue)
                {
                    UsageSettings.InitialSDKVersion.SetValue(sdkVersion.Value);
                    UsageSettings.LastUsedSDKVersion.SetValue(sdkVersion.Value);
                }

                int daysSinceActivation = 0;
                if (long.TryParse(UsageSettings.UserActivationDate, out var activationTime))
                {
                    var storedDate = DateTimeOffset.FromUnixTimeSeconds(activationTime);
                    var elapsed = DateTimeOffset.UtcNow - storedDate;
                    daysSinceActivation = (int)elapsed.TotalDays;
                }

                return new UsageData
                {
                    NumberOfActiveSessions = UsageSettings.NumberOfActiveSessions.Value,
                    DaysSinceActivation = daysSinceActivation,
                    CurrentSdkVersion = sdkVersion,
                    InitialSdkVersion = sdkVersion.HasValue ? UsageSettings.InitialSDKVersion.Value : 0,
                    LastUsedSdkVersion = previousSdkVersion,
                    IsFirstSessionAfterSdkUpdate = sdkVersion.HasValue && previousSdkVersion != 0 && previousSdkVersion != sdkVersion.Value
                };
            }
        }

        private static IEnumerable<(string Key, FilterChecker Checker)> GetFiltersForToolUsageData(
            ToolUsageData toolUsageData)
        {
            yield return (
                $"{toolUsageData.ToolId}_is_used",
                new ValueFilterChecker<bool>(toolUsageData.IsUsed));

            yield return (
                $"{toolUsageData.ToolId}_times_used",
                new ValueFilterChecker<int>(toolUsageData.TimesUsed));

            yield return (
                $"{toolUsageData.ToolId}_days_since_last_used",
                new ValueFilterChecker<int>(toolUsageData.DaysSinceLastUsed));

            yield return (
                $"{toolUsageData.ToolId}_last_used_in_sdk_version",
                new ValueFilterChecker<int>(toolUsageData.LastUsedInSdkVersion));
        }

        private static IEnumerable<ToolUsageData> GetToolUsageDataFromRegistry()
        {
            foreach (var tool in ToolRegistry.Registry)
            {
                yield return new ToolUsageData
                {
                    ToolId = tool.Usage.ToolId,
                    IsUsed = tool.Usage.IsUsed,
                    TimesUsed = tool.Usage.TimesUsed,
                    DaysSinceLastUsed = tool.Usage.DaysSinceLastUsed,
                    LastUsedInSdkVersion = tool.Usage.LastUsedInSDKVersion ?? ToolUsage.MissingSDKVersion
                };
            }
        }

        public Validator() : this(UsageData.Resolve())
        {
        }


        private Validator(UsageData usageData, IEnumerable<ToolUsageData> toolUsages = null)
        {
            toolUsages ??= GetToolUsageDataFromRegistry();

            var checkers = new Dictionary<string, FilterChecker>
            {
                { "platform", new ValueFilterChecker<string>(Application.platform.ToString()) },
                { "unity_version", new ValueFilterChecker<Version>(ParseUnityVersion(Application.unityVersion)) },
                { "number_active_sessions", new ValueFilterChecker<int>(usageData.NumberOfActiveSessions) },
                { "days_since_activation", new ValueFilterChecker<int>(usageData.DaysSinceActivation) }
            };

            if (usageData.CurrentSdkVersion.HasValue)
            {
                checkers.Add("sdk_version", new ValueFilterChecker<int>(usageData.CurrentSdkVersion.Value));
                checkers.Add("initial_sdk_version", new ValueFilterChecker<int>(usageData.InitialSdkVersion));
                checkers.Add("last_used_sdk_version", new ValueFilterChecker<int>(usageData.LastUsedSdkVersion));
                checkers.Add("is_first_session_after_sdk_update", new ValueFilterChecker<bool>(usageData.IsFirstSessionAfterSdkUpdate));
            }

            foreach (var toolUsageData in toolUsages)
            {
                foreach (var (key, checker) in GetFiltersForToolUsageData(toolUsageData))
                {
                    if (checkers.TryAdd(key, checker))
                    {
                        continue;
                    }
                }
            }

            _checkers = checkers;
        }

        public bool ValidateFilter(NotificationFilter filter)
        {
            if (string.IsNullOrEmpty(filter.field) || string.IsNullOrEmpty(filter.@operator))
            {
                return false;
            }

            if (!_checkers.TryGetValue(filter.field, out var checker))
            {
                return false;
            }

            try
            {
                return checker.CheckCondition(filter.@operator, filter.value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Version ParseUnityVersion(string versionString)
        {
            try
            {
                var versionParts = versionString.Split('.');

                if (versionParts.Length < 2)
                {
                    return new Version(0, 0, 0);
                }

                if (!int.TryParse(versionParts[0], out var major) || !int.TryParse(versionParts[1], out var minor))
                {
                    return new Version(0, 0, 0);
                }

                var patch = 0;
                if (versionParts.Length > 2)
                {
                    var patchString = new string(versionParts[2].TakeWhile(char.IsDigit).ToArray());
                    if (!string.IsNullOrEmpty(patchString))
                    {
                        int.TryParse(patchString, out patch);
                    }
                }

                return new Version(major, minor, patch);
            }
            catch (Exception)
            {
                return new Version(0, 0, 0);
            }
        }
    }
}
