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
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using System.Text;

namespace Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight
{
    public class AdrenoOfflineCompilerUtilityRO : EditorWindow
    {

        public enum ShaderStat
        {
            TotalInstructionCount,
            AluInstructionCount32Bit,
            AluInstructionCount16Bit,
            ComplexInstructionCount32Bit,
            ComplexInstructionCount16Bit,
            FlowControlInstructionCount,
            BarrierAndFenceInstructionCount,
            ShortLatencySyncInstructionCount,
            LongLatencySyncInstructionCount,
            TextureReadInstructionCount,
            MemoryReadInstructionCount,
            MemoryWriteInstructionCount,
            MiscellaneousInstructionCount,
            FullPrecisionRegisterFootprintPerShaderInstance,
            HalfPrecisionRegisterFootprintPerShaderInstance,
            OverallRegisterFootprintPerShaderInstance,
            ScratchMemoryUsagePerShaderInstance,
            LoopCount,
            OutputComponentCount,
            InputComponentCount,
            AluFiberOccupancyPercentage,

            Count
        }

        public struct ShaderStatConfig
        {
            public readonly string inputKey;
            public readonly int vertexGreen;
            public readonly int vertexRed;
            public readonly int fragmentMainGreen;
            public readonly int fragmentMainRed;
            public readonly int fragmentPreambleGreen;
            public readonly int fragmentPreambleRed;

            public ShaderStatConfig(string inputKey, int vertexGreen, int vertexRed, int fragmentMainGreen, int fragmentMainRed, int fragmentPreambleGreen, int fragmentPreambleRed)
            {
                this.inputKey = inputKey;
                this.vertexGreen = vertexGreen;
                this.vertexRed = vertexRed;
                this.fragmentMainGreen = fragmentMainGreen;
                this.fragmentMainRed = fragmentMainRed;
                this.fragmentPreambleGreen = fragmentPreambleGreen;
                this.fragmentPreambleRed = fragmentPreambleRed;
            }
        }

        public class ShaderStatInfo
        {
            public int subShader;
            public int pass;
            public string name;
            public string renderPipeline;
            public string renderType;
            public string renderQueue;
            public string lightMode;
            public string[] keywords;

            public bool valid;
            public string error;
            public bool hasPreamble;
            public int[] fragmentPreambleStats;
            public int[] fragmentMainStats;
            public int[] vertexStats;
            public int[] binningVertexStats;

            public string GetName()
            {
                string lightModeString = string.IsNullOrEmpty(lightMode) ? string.Empty : $", LightMode: {lightMode}";
                string renderPipelineString = string.IsNullOrEmpty(renderPipeline)
                    ? string.Empty
                    : $", RenderPipeline: {renderPipeline}";
                string renderQueueString =
                    string.IsNullOrEmpty(renderQueue) ? string.Empty : $", Queue: {renderQueue}";
                string renderTypeString = string.IsNullOrEmpty(renderType) ? string.Empty : $", RenderType {renderType}";
                return
                    $"SubShader {subShader}, Pass {pass} ({name}){renderTypeString}{renderQueueString}{lightModeString}{renderPipelineString}";
            }
        }


        public static readonly ShaderStatConfig[] kShaderStatConfigs = new ShaderStatConfig[(int)ShaderStat.Count]
        {
        new ShaderStatConfig("Total instruction count", 200, 400, 200, 400, 100, 200),
        new ShaderStatConfig("ALU instruction count - 32 bit", 200, 400, 100, 200, 100, 200),
        new ShaderStatConfig("ALU instruction count - 16 bit", 200, 400, 150, 300, 100, 200),
        new ShaderStatConfig("Complex instruction count - 32 bit", 10, 25, 2, 10, 4, 10),
        new ShaderStatConfig("Complex instruction count - 16 bit", 10, 25, 2, 10, 4, 10),
        new ShaderStatConfig("Flow control instruction count", 1, 4, 2, 4, 2, 4),
        new ShaderStatConfig("Barrier and fence Instruction count", 0, 2, 0, 2, 5, 10),
        new ShaderStatConfig("Short latency sync instruction count", 5, 10, 5, 10, 20, 40),
        new ShaderStatConfig("Long latency sync instruction count", 2, 5, 2, 8, 2, 5),
        new ShaderStatConfig("Texture read instruction count", 1, 4, 4, 8, 4, 8),
        new ShaderStatConfig("Memory read instruction count", 1, 4, 4, 8, 4, 8),
        new ShaderStatConfig("Memory write instruction count", 0, 2, 1, 2, 8, 16),
        new ShaderStatConfig("Miscellaneous instruction count", 100, 200, 50, 100, 50, 100),
        new ShaderStatConfig("Full precision register footprint per shader instance", 8, 16, 8, 16, 8, 16),
        new ShaderStatConfig("Half precision register footprint per shader instance", 16, 32, 16, 32, 16, 32),
        new ShaderStatConfig("Overall register footprint per shader instance", 8, 16, 8, 16, 8, 16),
        new ShaderStatConfig("Scratch memory usage per shader instance", 0, 16, 0, 16, 0, 16),
        new ShaderStatConfig("Loop count", 0, 2, 0, 2, 0, 2),
        new ShaderStatConfig("Output component count", 16, 32, 16, 32, 16, 32),
        new ShaderStatConfig("Input component count", 16, 32, 16, 32, 16, 32),
        new ShaderStatConfig("ALU fiber occupancy percentage", 100, 50, 100, 50, 100, 50),
        };

        private static readonly string[] kShaderPlatforms = new string[]
        {
        "GLES",
        "Vulkan"
        };

        private static readonly string[] kQuestDeviceNames = new string[]
        {
        "Quest 2",
        "Quest 3"
        };

        private static readonly string[] kQuestDeviceArchitectures = new string[]
        {
        "a650",
        "a740"
        };



        // These keywords don't work on Android, so skip them.
        private static readonly HashSet<string> kIgnoreKeywords = new HashSet<string>() { "STEREO_INSTANCING_ON", "UNITY_SINGLE_PASS_STEREO" };

        private static readonly string kDefaultAdrenoOfflineCompilerPath = "C:/Program Files/Qualcomm/Adreno Offline Compiler/aoc.exe";

        private static string _adrenoOfflineCompilerPath;
        private static string _editorPrefsAdrenoOfflineCompilerPath;

        private Material _currentMaterial;
        private Shader _currentShader;
        private ShaderCompilerPlatform _shaderPlatform;
        private List<ShaderStatInfo> _shaderStats;
        private bool[] _visible;
        private bool _showKeywords;
        private string _currentKeywords = string.Empty;
        private readonly Dictionary<string, bool> _enabledKeywords = new Dictionary<string, bool>();
        private Vector2 _scrollPosition;
        private int _selectedDeviceIndex;


        private void ResetMaterialKeywords()
        {
            if (_currentMaterial == null)
            {
                return;
            }
            foreach (var keyword in _currentMaterial.shaderKeywords)
            {
                if (kIgnoreKeywords.Contains(keyword))
                {
                    continue;
                }

                if (_enabledKeywords.ContainsKey(keyword))
                {
                    _enabledKeywords[keyword] = true;
                }
            }
        }

        public static void ShowAOCPathUI(bool disabled)
        {
            EditorGUILayout.LabelField("Path To Adreno Offline Compiler Executable");
            EditorGUI.BeginDisabledGroup(disabled);
            // Prompt for input
            _adrenoOfflineCompilerPath =
                EditorGUILayout.TextField(_adrenoOfflineCompilerPath);
            EditorGUI.EndDisabledGroup();

            if (string.IsNullOrEmpty(_adrenoOfflineCompilerPath) || !File.Exists(_adrenoOfflineCompilerPath) || !_adrenoOfflineCompilerPath.EndsWith(".exe"))
            {
                // Do not have AOC installed.
                if (EditorGUILayout.LinkButton("Adreno Offline Compiler Can be Downloaded from Qualcomm Here"))
                {
                    Application.OpenURL("https://qpm.qualcomm.com/#/main/tools/details/Adreno_GPU_Offline_Compiler");
                }
            }
        }

        public static bool InitAOCPath(out bool disabled)
        {
            {
                disabled = false;
                if (File.Exists(kDefaultAdrenoOfflineCompilerPath))
                {
                    _adrenoOfflineCompilerPath = kDefaultAdrenoOfflineCompilerPath;
                    disabled = true;
                }

                if (_editorPrefsAdrenoOfflineCompilerPath == null)
                {
                    if (EditorPrefs.HasKey("ADRENO_OFFLINE_COMPILER_PATH"))
                    {
                        _editorPrefsAdrenoOfflineCompilerPath = EditorPrefs.GetString("ADRENO_OFFLINE_COMPILER_PATH");
                    }
                    else
                    {
                        _editorPrefsAdrenoOfflineCompilerPath = string.Empty;
                    }
                }

                if (string.IsNullOrEmpty(_adrenoOfflineCompilerPath))
                {
                    _adrenoOfflineCompilerPath = _editorPrefsAdrenoOfflineCompilerPath;
                }
            }

            if (string.IsNullOrEmpty(_adrenoOfflineCompilerPath) || !File.Exists(_adrenoOfflineCompilerPath) || !_adrenoOfflineCompilerPath.EndsWith(".exe"))
            {
                return false;
            }

            if (_adrenoOfflineCompilerPath != _editorPrefsAdrenoOfflineCompilerPath)
            {
                EditorPrefs.SetString("ADRENO_OFFLINE_COMPILER_PATH", _adrenoOfflineCompilerPath);
                _editorPrefsAdrenoOfflineCompilerPath = _adrenoOfflineCompilerPath;
            }

            return true;
        }

        private static bool ValidPreambleStat(ShaderStat shaderStat)
        {
            return shaderStat <= ShaderStat.MiscellaneousInstructionCount;
        }

        public static List<ShaderStatInfo> GetShaderStats(Shader s, string[] keywords, ShaderCompilerPlatform shaderPlatform, string arch = "")
        {
            return GetShaderStats2(s, s.name, -1, -1, keywords, shaderPlatform, arch);
        }

        public static List<ShaderStatInfo> GetShaderStats2(Shader s, string name, int maxSubShader, int maxPass, string[] keywords, ShaderCompilerPlatform shaderPlatform, string arch, Dictionary<string, ShaderStatInfo> statsCache = null)
        {
            List<ShaderStatInfo> statsList = new List<ShaderStatInfo>();

            var shaderData = ShaderUtil.GetShaderData(s);
            string shaderDir = "Temp/AOC/" + name;
            Directory.CreateDirectory(shaderDir);

            int subShaderCount = maxSubShader < 0 ? shaderData.SubshaderCount : maxSubShader;

            var md5 = System.Security.Cryptography.MD5.Create();
            for (int i = 0; i < subShaderCount; i++)
            {
                var ss = shaderData.GetSubshader(i);
                int passCount = maxPass < 0 ? ss.PassCount : maxPass;

                for (int j = 0; j < passCount; j++)
                {

                    var timeStart = DateTime.Now;
                    var p = ss.GetPass(j);

                    string shaderHash = "";
                    if (statsCache != null)
                    {
                        string vertexStr = p.PreprocessVariant(ShaderType.Vertex, keywords, shaderPlatform,
                            BuildTarget.Android, GraphicsTier.Tier2, true).PreprocessedCode;
                        string fragmentStr = p.PreprocessVariant(ShaderType.Fragment, keywords, shaderPlatform,
                            BuildTarget.Android, GraphicsTier.Tier2, true).PreprocessedCode;

                        string combinedStr = vertexStr + fragmentStr;
                        byte[] combinedData = Encoding.ASCII.GetBytes(combinedStr);

                        var shaderMd5 = md5.ComputeHash(combinedData);
                        shaderHash = BitConverter.ToString(shaderMd5).Replace("-", "").ToLowerInvariant();

                        ShaderStatInfo value;
                        if (statsCache.TryGetValue(shaderHash, out value))
                        {
                            double timeSinceProcessed = (DateTime.Now - timeStart).TotalSeconds;
                            // UnityEngine.Debug.LogError("@@@@@@@@@@@@@@@@@@@@@ Cache Hit " + timeSinceProcessed);
                            statsList.Add(value);
                            continue;
                        }
                    }

                    var vertex = p.CompileVariant(ShaderType.Vertex, keywords, shaderPlatform,
                            BuildTarget.Android, GraphicsTier.Tier2, true);
                    var fragment = p.CompileVariant(ShaderType.Fragment, keywords, shaderPlatform,
                            BuildTarget.Android, GraphicsTier.Tier2, true);

                    if (!vertex.Success || !fragment.Success)
                    {
                        statsList.Add(new ShaderStatInfo
                        {
                            subShader = i,
                            pass = j,
                            name = p.Name,
                            renderPipeline = p.FindTagValue(new ShaderTagId("RenderPipeline")).name,
                            renderType = p.FindTagValue(new ShaderTagId("RenderType")).name,
                            renderQueue = p.FindTagValue(new ShaderTagId("Queue")).name,
                            lightMode = p.FindTagValue(new ShaderTagId("LightMode")).name,
                            keywords = keywords,
                            valid = false,
                            error = string.Join("\n", vertex.Messages.Select(m => m.message).ToArray())
                        });
                        continue;
                    }

                    string vertName = shaderDir + "/" + i + "_" + j + ".vs" + (shaderPlatform == ShaderCompilerPlatform.Vulkan ? ".spv" : "");
                    string fragName = shaderDir + "/" + i + "_" + j + ".fs" + (shaderPlatform == ShaderCompilerPlatform.Vulkan ? ".spv" : "");

                    File.WriteAllBytes(vertName, vertex.ShaderData);
                    File.WriteAllBytes(fragName, fragment.ShaderData);

                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = _adrenoOfflineCompilerPath,
                            Arguments = '"' + vertName + '"' + " " + '"' + fragName + '"' + (string.IsNullOrEmpty(arch) ? "" : " -arch=" + arch),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    int mode = -1;
                    int[][] stats = new int[][]
                    {
                    new int[(int)ShaderStat.Count],
                    new int[(int)ShaderStat.Count],
                    new int[(int)ShaderStat.Count],
                    new int[(int)ShaderStat.Count],
                    };
                    bool hasPreamble = false;

                    process.Start();
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string line = process.StandardOutput.ReadLine();
                        if (line.Contains("Shader Stats FS"))
                        {
                            mode = 0;
                        }
                        else if (line.Contains("Shader Stats VS"))
                        {
                            mode = 2;
                        }
                        else if (line.Contains("Shader Stats BINNING VS"))
                        {
                            mode = 3;
                        }

                        if (mode == -1)
                        {
                            continue;
                        }

                        if (line.Contains("Shader Preamble Stats") && mode == 0)
                        {
                            hasPreamble = true;
                        }

                        if (line.Contains("Main Shader Stats") && mode == 0)
                        {
                            mode = 1;
                        }

                        for (int k = 0; k < (int)ShaderStat.Count; k++)
                        {
                            if (!line.StartsWith(kShaderStatConfigs[k].inputKey))
                            {
                                continue;
                            }

                            int e = line.Length - 1;
                            int m = 1;
                            stats[mode][k] = 0;
                            while (line[e] >= '0' && line[e] <= '9')
                            {
                                stats[mode][k] += m * (line[e] - '0');
                                m *= 10;
                                e--;
                            }

                            break;
                        }
                    }

                    var shaderStat = new ShaderStatInfo
                    {
                        subShader = i,
                        pass = j,
                        name = p.Name,
                        renderPipeline = p.FindTagValue(new ShaderTagId("RenderPipeline")).name,
                        renderType = p.FindTagValue(new ShaderTagId("RenderType")).name,
                        renderQueue = p.FindTagValue(new ShaderTagId("Queue")).name,
                        lightMode = p.FindTagValue(new ShaderTagId("LightMode")).name,
                        keywords = keywords,
                        valid = mode != -1,
                        error = mode == -1 ? "Could Not Compile Shader" : string.Empty,
                        hasPreamble = hasPreamble,
                        fragmentPreambleStats = stats[0],
                        fragmentMainStats = stats[1],
                        vertexStats = stats[2],
                        binningVertexStats = stats[3]
                    };

                    if (statsCache != null && shaderHash != "")
                    {
                        statsCache[shaderHash] = shaderStat;
                        double timeSinceProcessed = (DateTime.Now - timeStart).TotalSeconds;
                        // UnityEngine.Debug.LogError("@@@@@@@@@@@@@@@@@@@@@ Cache Miss " + timeSinceProcessed);
                    }
                    statsList.Add(shaderStat);
                }
            }

            return statsList;
        }

        public static Color GetStatColor(int stat, int red, int green)
        {
            float t = Mathf.InverseLerp(red, green, stat);

            return t < 0.5f
                ? Color.Lerp(Color.red, Color.yellow, 2 * t)
                : Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2);
        }
    }
}
