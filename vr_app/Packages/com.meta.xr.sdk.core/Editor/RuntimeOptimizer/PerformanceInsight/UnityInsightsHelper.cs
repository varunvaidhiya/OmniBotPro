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

using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.RuntimeOptimizer.Editor;

using System.Threading;
using RO = Meta.XR.RuntimeOptimizer.Core;

namespace Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight
{
    public class InsightData
    {

        public long totalTextureMemory = 0;
        public int drawcallCount = 0;
        public uint totalVertexCount = 0;
        public uint totalTriangleCount = 0;
        public int MSAALevel = 0;

        public List<(GameObject, int)> meshVertArray = new List<(GameObject, int)>();
        public List<(Mesh, MeshRuntimeData)> meshAssetVertArray = new List<(Mesh, MeshRuntimeData)>();
        public List<(Material, ShaderData)> materialArray = new List<(Material, ShaderData)>();
        public List<(Texture, TextureRuntimeData)> textureArray = new List<(Texture, TextureRuntimeData)>();

        private Dictionary<string, (UnityInsight, List<InsightTrigger>)> insightMap = UnityInsightsHelper.ProcessInsights();
        public Dictionary<string, InsightTrigger> insightTriggersMap = UnityInsightsHelper.ProcessTriggers();

        public int NumOfTriggeredInsights()
        {
            int activeInsights = 0;
            foreach (var entry in insightMap)
            {
                activeInsights += entry.Value.Item2.Count > 0 ? 1 : 0;
            }
            return activeInsights;
        }

        public List<UnityInsight> GetTriggeredInsights()
        {
            List<UnityInsight> activeInsights = new List<UnityInsight>();
            foreach (var entry in insightMap)
            {
                if (entry.Value.Item2.Count > 0)
                {
                    activeInsights.Add(entry.Value.Item1);
                }
            }
            return activeInsights;
        }

        public void AddTriggerToCategory(string category, InsightTrigger trigger)
        {
            if (insightMap.ContainsKey(category))
            {
                insightMap[category].Item2.Add(trigger);
            }
            else
            {
                RO.Util.DebugLog("Failed to add trigger to category: " + category);
            }
        }

        public void Clear()
        {
            meshVertArray.Clear();
            materialArray.Clear();
            textureArray.Clear();
            meshAssetVertArray.Clear();

            foreach (var entry in insightMap)
            {
                entry.Value.Item2.Clear();
            }
        }
    };

    static class UnityInsightsHelper
    {
        private const float kGPUTimeMax = 13.0f;
        // this is suppose to be 13.888888888889
        // though as perf capture cost some time, in general under 14 would be 72fps
        private const float kCPUTimeMax = 14.0f;

        private const float kHighBinning = 3.0f;
        private const float kALURunningThreashLow = 60.0f;

        private const float kALURunningThreashHigh = 90.0f;
        private const float kTimeShadingFregmentThreshHigh = 85.0f;
        private const float kTimeShadingFregmentThreshLow = 60.0f;

        private const float kThreadBusyThresh = 65.0f;
        private const float kThreadNotScheduled = 35.0f;

        private const int kVertexThresh = 1000000;
        private const float kVertexStallThresh = 20.0f;

        public static bool printDebug = false;

        private static string metricVersion = "";

        //TODO: Refactor to get the data from PIL
        private static Dictionary<string, AdrenoOfflineCompilerUtilityRO.ShaderStatInfo> statsCache = new Dictionary<string, AdrenoOfflineCompilerUtilityRO.ShaderStatInfo>();

        public static JSONObject GetNodeVal(JSONObject jsonNode, string key)
        {
            JSONObject val = jsonNode[key] != null ? jsonNode[key].AsObject : null;
            if (val == null && printDebug)
            {
                RO.Util.DebugLog("Failed to get " + key + " from perf metrics");
            }
            return val;
        }

        public static float GetFloatVal(JSONObject jsonNode, string key)
        {
            if (jsonNode == null)
            {
                return -1.0f;
            }

            float val = jsonNode[key] != null ? jsonNode[key].AsFloat : -1.0f;
            if (val < 0.0f && printDebug)
            {
                RO.Util.DebugLog("Failed to get " + key + " from perf metrics");
            }
            return val;
        }

        private static void GetMeshFromGO(GameObject go, List<Mesh> outList)
        {
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                // Get the mesh from the MeshFilter
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    outList.Add(mesh);
                }
            }

            SkinnedMeshRenderer[] skinnedRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedRenderers)
            {
                // Get the mesh from the MeshFilter
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                if (mesh != null)
                {
                    outList.Add(mesh);
                }
            }

        }

        private static List<Mesh> LoadUnityAssetMesh(string name, string type)
        {
            string searchPattern = name + "  t:" + type;
            // UnityEngine.Debug.Log("searchPattern: " + searchPattern);
            string[] assetGUIDs = AssetDatabase.FindAssets(searchPattern, null);
            // UnityEngine.Debug.Log("assetGUIDs: " + assetGUIDs.Length);
            List<Mesh> outlist = new List<Mesh>();
            foreach (string assetGUID in assetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                // UnityEngine.Debug.Log("MeshPath: " + assetPath);
                var asset = EditorGUIUtility.Load(assetPath);
                GameObject assetT = asset as GameObject;
                if (assetT != null)
                {
                    GetMeshFromGO(assetT, outlist);
                }
            }
            return outlist;
        }

        public static List<(Mesh, MeshRuntimeData)> GetMeshList(JSONObject sceneSnapshot, out uint totalVertCount)
        {
            JSONArray itemArray = new JSONArray();
            if (sceneSnapshot["items"] is JSONArray)
            {
                itemArray = (JSONArray)sceneSnapshot["items"];
            }
            Dictionary<string, List<(Mesh, MeshRuntimeData)>> parsedSceneData = new Dictionary<string, List<(Mesh, MeshRuntimeData)>>();

            foreach (var item in itemArray)
            {
                int vertCount = item.Value["vertCount"].AsInt;
                if (item.Value["meshes"] != null && item.Value["meshes"] is JSONArray)
                {
                    JSONArray meshesArra = (JSONArray)item.Value["meshes"];
                    foreach (var mesh in meshesArra)
                    {
                        string meshName = mesh.Value["name"];
                        string shortPath = item.Value["parentName"] + "/" + item.Value["name"];
                        int meshVertCount = mesh.Value["vertexCount"].AsInt;
                        if (meshName == "")
                        {
                            continue;
                        }

                        string meshKey = meshName + "+" + meshVertCount;

                        if (parsedSceneData.ContainsKey(meshKey))
                        {
                            var meshList = parsedSceneData[meshKey];
                            int meshCount = meshList.Count;
                            for (int i = 0; i < meshCount; i++)
                            {
                                if (meshList[i].Item1 != null)
                                {
                                    if (meshList[i].Item1.vertexCount == meshVertCount)
                                    {
                                        meshList[i].Item2.drawCount += 1;
                                        meshList[i] = (meshList[i].Item1, meshList[i].Item2);
                                    }
                                }
                                else
                                {
                                    if (meshList[i].Item2.dynamicVertexCount == meshVertCount)
                                    {
                                        meshList[i].Item2.drawCount += 1;
                                        meshList[i] = (null, meshList[i].Item2);
                                    }
                                }
                            }
                        }
                        else
                        {
                            List<Mesh> meshes = LoadUnityAssetMesh(meshName, "Mesh");
                            List<(Mesh, MeshRuntimeData)> meshList = new List<(Mesh, MeshRuntimeData)>();
                            foreach (var meshAsset in meshes)
                            {
                                if (meshAsset is Mesh)
                                {
                                    Mesh uMesh = meshAsset as Mesh;
                                    MeshRuntimeData mrd = new MeshRuntimeData();

                                    if (uMesh.vertexCount == meshVertCount)
                                    {
                                        mrd.shortPath = shortPath;
                                        mrd.drawCount = 1;
                                        meshList.Add((uMesh, mrd));
                                        break;
                                        //UnityEngine.Debug.Log("Mesh Asset: " + uMesh.name);
                                    }
                                    else
                                    {
                                        mrd.drawCount = 0;
                                        meshList.Add((uMesh, mrd));
                                        //UnityEngine.Debug.Log("Mesh Asset: " + uMesh.name + " Vertex coun" + meshVertCount + "/" + uMesh.vertexCount);
                                    }
                                }
                            }

                            // these might be dynamic mesh, we can't find them in the asset database but still track them
                            if (meshList.Count == 0)
                            {
                                MeshRuntimeData mrd = new MeshRuntimeData();
                                mrd.shortPath = shortPath;
                                mrd.drawCount = 1;
                                mrd.dynamicVertexCount = meshVertCount;

                                meshList.Add((null, mrd));
                            }

                            // UnityEngine.Debug.Log("Mesh Asset: " + meshName + "  count: " + meshList.Count);
                            parsedSceneData.Add(meshKey, meshList);

                        }
                    }
                }
            }

            List<(Mesh, MeshRuntimeData)> visibleGoList = new List<(Mesh, MeshRuntimeData)>();
            foreach (var item in parsedSceneData)
            {
                List<(Mesh, MeshRuntimeData)> meshList = item.Value;
                foreach (var mitem in meshList)
                {
                    if (mitem.Item2.drawCount > 0)
                    {
                        visibleGoList.Add((mitem.Item1, mitem.Item2));
                        // UnityEngine.Debug.Log("Mesh Asset: " + mitem.Item1.name);
                    }
                }
            }
            visibleGoList.Sort((a, b) =>
            {
                if (b.Item1 != null && a.Item1 != null)
                {
                    return b.Item1.vertexCount * b.Item2.drawCount - a.Item1.vertexCount * a.Item2.drawCount;
                }
                else
                {
                    return b.Item2.dynamicVertexCount * b.Item2.drawCount - a.Item2.dynamicVertexCount * a.Item2.drawCount;
                }
            });
            totalVertCount = (uint)sceneSnapshot["totalVertCount"].AsInt;
            return visibleGoList;
        }

        public static List<(GameObject, int)> GetMeshAndVertCount(JSONObject sceneSnapshot, out uint totalVertCount)
        {
            JSONArray itemArray = new JSONArray();
            // TODO(nicolpz): Fix hacky way of getting json array
            if (sceneSnapshot["items"] is JSONArray)
            {
                itemArray = (JSONArray)sceneSnapshot["items"];
            }
            Dictionary<string, int> parsedSceneData = new Dictionary<string, int>();

            foreach (var item in itemArray)
            {
                int vertCount = item.Value["vertCount"].AsInt;
                string gameObjectName = item.Value["name"];
                if (item.Value["parentName"] != null)
                {
                    gameObjectName = item.Value["parentName"] + "/" + gameObjectName;
                }
                if (parsedSceneData.ContainsKey(gameObjectName))
                {
                    parsedSceneData[gameObjectName] += vertCount;
                }
                else
                {
                    parsedSceneData.Add(gameObjectName, vertCount);
                }
            }

            var sortedSceneData = parsedSceneData.OrderByDescending(kvp => kvp.Value).Select(kvp => (kvp.Key, kvp.Value)).ToList();

            List<(GameObject, int)> visibleGoList = new List<(GameObject, int)>();
            foreach (var item in sortedSceneData)
            {
                var obj = GameObject.Find(item.Item1);
                if (obj == null)
                {
                    //TODO seach via Resource.FindObjectsOfType and filter by name + pareant name
                    UnityEngine.Debug.LogWarning("The GO might be disabled: " + item.Item1);
                }
                visibleGoList.Add((obj, item.Item2));
            }

            totalVertCount = (uint)sceneSnapshot["totalVertCount"].AsInt;
            return visibleGoList;
        }

        private static (List<Material>, List<Texture>) LoadUnityAssetMaterial(string name, string type)
        {
            string searchPattern = name + "  t:" + type;
            // UnityEngine.Debug.Log("searchPattern: " + searchPattern);
            string[] assetGUIDs = AssetDatabase.FindAssets(searchPattern, null);
            // UnityEngine.Debug.Log("assetGUIDs: " + assetGUIDs.Length);
            List<Material> outlist = new List<Material>();
            List<Texture> textures = new List<Texture>();
            foreach (string assetGUID in assetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                // UnityEngine.Debug.Log("MeshPath: " + assetPath);
                var asset = EditorGUIUtility.Load(assetPath);
                Material assetT = asset as Material;
                if (assetT != null)
                {
                    outlist.Add(assetT);
                    UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { assetT });
                    foreach (UnityEngine.Object obj in dependencies)
                    {
                        if (obj is Texture texture)
                        {
                            textures.Add(texture);
                            // RO.Util.DebugLog("Texture Dependency: " + texture.name);
                        }
                    }
                    // only fetch the first material
                    break;
                }
            }
            return (outlist, textures);
        }

        public static List<(Material, ShaderData)> GetMaterialList(JSONObject sceneSnapshot)
        {
            JSONArray itemArray = new JSONArray();
            if (sceneSnapshot["items"] is JSONArray)
            {
                itemArray = (JSONArray)sceneSnapshot["items"];
            }
            Dictionary<string, (Material, ShaderData)> materialDict = new Dictionary<string, (Material, ShaderData)>();
            string[] keywords = { "STEREO_MULTIVIEW_ON" };
            foreach (var item in itemArray)
            {
                if (item.Value["materialNames"] != null && item.Value["materialNames"] is JSONArray)
                {
                    JSONArray meterialArray = (JSONArray)item.Value["materialNames"];
                    int materialCount = meterialArray.Count;
                    for (int i = 0; i < materialCount; i++)
                    {
                        string materialName = meterialArray[i];
                        materialName = materialName.Replace(" (Instance)", "");
                        // since we don't really have the material instance, we just use the first one
                        if (materialDict.ContainsKey(materialName))
                        {
                            (Material, ShaderData) keyPair = materialDict[materialName];
                            keyPair.Item2.usedByCount += 1;
                            materialDict[materialName] = keyPair;
                        }
                        else
                        {
                            (List<Material>, List<Texture>) materials = LoadUnityAssetMaterial(materialName, "Material");
                            if (materials.Item1.Count > 0)
                            {
                                var thisMaterial = materials.Item1[0];
                                if (thisMaterial != null)
                                {
                                    ShaderData matData = new ShaderData();
                                    matData.usedByCount = 1;
                                    matData.shader = thisMaterial.shader;
                                    matData.shaderName = thisMaterial.shader.name;
                                    //TODO: Refactor to get the data from PIL
                                    matData.shaderStats = AdrenoOfflineCompilerUtilityRO.GetShaderStats2(thisMaterial.shader, matData.shaderName, 1, 1, keywords, ShaderCompilerPlatform.Vulkan, "a740", statsCache);
                                    materialDict.Add(materialName, (thisMaterial, matData));
                                    matData.textureList = materials.Item2;
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning("Failed to find material: " + materialName);
                            }
                        }
                    }

                }
            }
            List<(Material, ShaderData)> outlist = materialDict.Values.ToList();
            return outlist;
        }


        public static void ProcessTextureUsedCount(List<(Material, ShaderData)> materialList, Dictionary<string, (Texture, TextureRuntimeData)> texDictionary)
        {
            foreach ((Material, ShaderData) matData in materialList)
            {
                foreach (Texture texture in matData.Item2.textureList)
                {
                    if (texDictionary.ContainsKey(texture.name))
                    {
                        texDictionary[texture.name].Item2.usedByCount += matData.Item2.usedByCount;
                    }
                }
            }
        }

        public static List<(Material, ShaderData)> GetMaterialList(List<(GameObject, int)> visibleGoList)
        {
            List<(string, Material)> materialList = new List<(string, Material)>();
            Dictionary<string, (Material, ShaderData)> materialDict = new Dictionary<string, (Material, ShaderData)>();
            string[] keywords = { "STEREO_MULTIVIEW_ON" };
            foreach (var item in visibleGoList)
            {
                if (item.Item1 != null)
                {
                    var go = item.Item1;
                    Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.sharedMaterials;
                        foreach (Material material in materials)
                        {
                            string assetPath = AssetDatabase.GetAssetPath(material);
                            if (materialDict.ContainsKey(assetPath))
                            {
                                (Material, ShaderData) keyPair = materialDict[assetPath];
                                keyPair.Item2.usedByCount += 1;
                                materialDict[assetPath] = keyPair;
                            }
                            else
                            {
                                ShaderData matData = new ShaderData();
                                matData.usedByCount = 1;
                                matData.shader = material.shader;
                                matData.shaderName = material.shader.name;
                                //TODO: Refactor to get the data from PIL
                                matData.shaderStats = AdrenoOfflineCompilerUtilityRO.GetShaderStats2(material.shader, matData.shaderName, 1, 1, keywords, ShaderCompilerPlatform.Vulkan, "a740", statsCache);
                                materialDict.Add(assetPath, (material, matData));
                            }
                        }
                    }
                }
            }
            List<(Material, ShaderData)> outlist = materialDict.Values.ToList();
            return outlist;
        }

        public static Dictionary<string, (Texture, TextureRuntimeData)> GetTextureDictionary(JSONObject sceneSnapshot, out long totalTextureMemory)
        {

            JSONArray itemArray = new JSONArray();
            Dictionary<string, (Texture, TextureRuntimeData)> loadedTextures = new Dictionary<string, (Texture, TextureRuntimeData)>();
            totalTextureMemory = 0;
            if (sceneSnapshot["textures"] is JSONArray)
            {
                itemArray = (JSONArray)sceneSnapshot["textures"];

                foreach (var item in itemArray)
                {
                    int depth = item.Value["depth"].AsInt;
                    string format = item.Value["format"];
                    string name = item.Value["name"];
                    if (name != "")
                    {
                        string searchPattern = name + "  t:" + (depth > 1 ? "texture3D" : "texture2D");
                        string[] assetGUIDs = AssetDatabase.FindAssets(searchPattern, null);
                        foreach (string assetGUID in assetGUIDs)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                            // UnityEngine.Debug.Log("TexturePath: " + assetPath);
                            var asset = EditorGUIUtility.Load(assetPath);

                            if (asset != null && (asset is Texture))
                            {
                                Texture texture = asset as Texture;
                                TextureRuntimeData trd = new TextureRuntimeData();
                                trd.format = format;
                                trd.width = item.Value["width"];
                                trd.height = item.Value["height"];
                                trd.runtimeSize = item.Value["runtimeSize"];
                                trd.anisoLevel = item.Value["anisoLevel"];
                                trd.mipCount = item.Value["mipCount"];
                                if (item.Value["isReadable"] != null)
                                {
                                    trd.isReadable = item.Value["isReadable"];
                                }

                                totalTextureMemory += trd.runtimeSize;
                                loadedTextures[name] = (texture, trd);
                            }
                        }
                    }
                }
            }

            return loadedTextures;
        }

        public static List<(Texture, TextureRuntimeData)> GetTextureArray(Dictionary<string, (Texture, TextureRuntimeData)> textureDictionary)
        {
            if (textureDictionary != null)
            {
                List<(Texture, TextureRuntimeData)> textureArray = textureDictionary.Values.ToList();
                textureArray.Sort(delegate ((Texture, TextureRuntimeData) x, (Texture, TextureRuntimeData) y)
                {
                    return (int)y.Item2.runtimeSize - (int)x.Item2.runtimeSize;
                });
                return textureArray;
            }
            return new List<(Texture, TextureRuntimeData)>();
        }

        private static UnityInsight TestForVertexBoundness(JSONObject metrics, JSONObject sceneSnapshot)
        {

            return null;
        }

        private static UnityInsight TestForVertexStall(JSONObject metrics, JSONObject sceneSnapshot)
        {

            return null;
        }

        private static void FregmentHeavy(JSONObject metrics, List<string> insights)
        {
            float fregPercent = GetFloatVal(GetNodeVal(metrics, "% Time Shading Fragments"), "mean");
            float aluRunning = GetFloatVal(GetNodeVal(metrics, "% Time ALUs Working"), "mean");

            if (fregPercent > kTimeShadingFregmentThreshHigh)
            {
                insights.Add(string.Format("Fregment bound {0:0.00} % shading fregment, thresh is {1} %", fregPercent, kTimeShadingFregmentThreshHigh));
                insights.Add(string.Format("ALU utilization {0:0.00} {1}", aluRunning, " %"));
            }
        }

        private static void Boundness(JSONObject metrics, List<string> insights, bool displayTimingInfo, bool aswOn)
        {
            float kASWMultipiler = aswOn ? 0.5f : 1.0f;
            const float kProfilerCostReduciton = 0.95f;
            float appGpuRenderTime = GetFloatVal(GetNodeVal(metrics, "app_gpu_time"), "mean");
            float appFrameTime = GetFloatVal(GetNodeVal(metrics, "render_thread_frame_time"), "mean");
            float mainThreadFrameTime = GetFloatVal(GetNodeVal(metrics, "main_thread_frame_time"), "mean");
            int mainBufferMSAA = (int)GetFloatVal(GetNodeVal(metrics, "MSAA"), "mean");
            bool gpuMaxExceeded = (appGpuRenderTime * kASWMultipiler) > kGPUTimeMax;
            bool cpuMaxExceeded = (mainThreadFrameTime * kASWMultipiler) > kCPUTimeMax;

            string fpsStr = string.Format("FPS: {0:0.00}", 1000.0f / (appFrameTime * kASWMultipiler * kProfilerCostReduciton));
            if (aswOn)
            {
                fpsStr += " (ASW)";
            }
            insights.Add(fpsStr);

            string insight = (gpuMaxExceeded ? "GPU" : "");
            if (cpuMaxExceeded || gpuMaxExceeded)
            {
                if (cpuMaxExceeded && gpuMaxExceeded)
                {
                    insight += "&";
                }
                insight += (cpuMaxExceeded ? "CPU" : "");
                insight += (gpuMaxExceeded | cpuMaxExceeded) ? " bound" : "";
                insights.Add(insight);
            }

            insights.Add(string.Format("CPU: {0:0.00}ms", (appFrameTime * kProfilerCostReduciton)));
            insights.Add(string.Format("GPU: {0:0.00}ms", appGpuRenderTime));

            if (displayTimingInfo)
            {
                insights.Add(string.Format("main thread time: {0:0.00} {1}", mainThreadFrameTime, " ms"));
                if (appGpuRenderTime > 0.0f)
                {
                    insights.Add(string.Format("gpu time: {0:0.00} {1}", appGpuRenderTime, " ms"));
                }
                else
                {
                    insights.Add(string.Format("Missing GPU data, please restart app"));
                }
            }
        }

        private static void RuntimeResource(JSONObject runtimeSnapshot, List<string> insights)
        {
            // TODO Display in Render Overview
            if (runtimeSnapshot["totalDrawcount"] != null)
            {
                int drawcallCount = runtimeSnapshot["totalDrawcount"].AsInt;
                // if (triggerMap.ContainsKey("DrawCall Count"))
                // {
                // InsightTrigger trigger = triggerMap["DrawCall Count"];
                //        insights.Add(string.Format($"DrawCount: {drawcallCount} (< {trigger.Min} is good) "));
                // }
            }

            // TODO Display in Render Overview
            if (runtimeSnapshot["totalTriangleCount"] != null)
            {
                uint triCount = (uint)runtimeSnapshot["totalTriangleCount"].AsInt;
                // insights.Add(string.Format($"Triangles: {triCount} "));
            }

            // hide GPU memory for now until we have a meaningful suggestion
            // uint gmem = (uint)runtimeSnapshot["totalGMemAlloc"].AsInt;
            // uint gmemInMB = gmem / 1024 / 1024;
            // insights.Add(string.Format($"GPU Memory Usage: {gmemInMB} mb "));
        }

        private static void TopFrameInformation(JSONObject metrics, List<string> insights)
        {

            float midFrameCPURunningTime = GetFloatVal(GetNodeVal(metrics, "main_thread_mid_frame_run_ratio"), "running_time");

            float topFrameTime = GetFloatVal(metrics, "top_main_thread_frame_time");
            if (topFrameTime > kCPUTimeMax)
            {
                insights.Add(string.Format("Top main thread frame time: {0:0.00} {1}", topFrameTime, " ms"));

                float topFrameCPURunningTime = GetFloatVal(GetNodeVal(metrics, "main_thread_top_frame_run_ratio"), "running_time");
                float topFrameCPURunningRatio = GetFloatVal(GetNodeVal(metrics, "main_thread_top_frame_run_ratio"), "run_ratio");

                insights.Add(string.Format("cpu ran for: {0:0.00} {1}", topFrameCPURunningTime, " ms"));
                if (Math.Abs((topFrameCPURunningTime - midFrameCPURunningTime) / topFrameCPURunningTime) > 0.30f)
                {
                    insights.Add(string.Format("This is a spiked frame"));
                }

                if (topFrameCPURunningRatio > 75.0f)
                {
                    insights.Add(string.Format("and core is {0:0.00} {1} busy", topFrameCPURunningRatio, "%"));
                    insights.Add(string.Format("Please check unity profiler for higher than normal CPU usage"));
                }
                else
                {
                    insights.Add(string.Format("and core is {0:0.00} {1} Busy", topFrameCPURunningRatio, "%"));
                    insights.Add(string.Format("thread is likely stuck on a lock or waiting for other task to finish"));
                }
            }
        }

        public static Dictionary<string, InsightTrigger> ProcessTriggers()
        {
            try
            {
                string jsonPath = PythonUtil.GetScriptPath("InsightsMetricRules.json");
                string jsonStr = File.ReadAllText(jsonPath);

                JSONObject ruleSetJson = JSONObject.Parse(jsonStr) as JSONObject;

                if (ruleSetJson["version"] != null)
                {
                    metricVersion = ruleSetJson["version"].Value;
                }

                JSONArray itemArray = new JSONArray();
                if (ruleSetJson["items"] is JSONArray)
                {
                    itemArray = (JSONArray)ruleSetJson["items"];
                }
                else
                {
                    RO.Util.DebugLog("Failed to get Metric Rule item array");
                }

                Dictionary<string, InsightTrigger> tiggerMap = new Dictionary<string, InsightTrigger>();
                foreach (var item in itemArray)
                {
                    InsightTrigger trigger = new InsightTrigger();
                    trigger.Title = item.Value["Title"];
                    trigger.Description = item.Value["Description"];
                    trigger.Max = item.Value["Max"].AsFloat;
                    trigger.Min = item.Value["Min"].AsFloat;
                    JSONArray categoryArray = item.Value["Category"].AsArray;
                    int count = categoryArray.Count;
                    List<string> category = new List<string>();
                    for (int i = 0; i < count; i++)
                    {
                        category.Add(categoryArray[i]);
                    }
                    trigger.Category = category.ToArray();

                    tiggerMap[trigger.Title] = trigger;
                }
                return tiggerMap;
            }
            catch (Exception e)
            {
                RO.Util.DebugLog("Failed to load InsightMetricRules.json: " + e.Message);
            }
            return null;
        }

        public static Dictionary<string, (UnityInsight, List<InsightTrigger>)> ProcessInsights()
        {
            try
            {
                string jsonPath = PythonUtil.GetScriptPath("InsightsForCategory.json");
                string jsonStr = File.ReadAllText(jsonPath);

                JSONObject ruleSetJson = JSONObject.Parse(jsonStr) as JSONObject;

                JSONArray itemArray = new JSONArray();
                if (ruleSetJson["items"] is JSONArray)
                {
                    itemArray = (JSONArray)ruleSetJson["items"];
                }
                else
                {
                    RO.Util.DebugLog("Failed to get Metric Rule item array");
                }

                Dictionary<string, (UnityInsight, List<InsightTrigger>)> tiggerMap = new Dictionary<string, (UnityInsight, List<InsightTrigger>)>();
                foreach (var item in itemArray)
                {

                    JSONArray linksArray = item.Value["Links"].AsArray;
                    List<string> links = new List<string>();
                    foreach (var link in linksArray)
                    {
                        links.Add(link.Value);
                    }

                    UnityInsight unityInsight = new UnityInsight(item.Value["Category"], item.Value["Description"], item.Value["Insight"], links.ToArray());
                    List<InsightTrigger> triggerList = new List<InsightTrigger>();

                    tiggerMap[unityInsight.Category] = (unityInsight, triggerList);
                }
                return tiggerMap;
            }
            catch (Exception e)
            {
                RO.Util.DebugLog("Failed to load InsightsForCategory.json: " + e.Message);
            }
            return null;
        }

        static public string[] GenerateLightweightInsights(JSONObject metrics, JSONObject runtimeSnapshot, bool aswOn)
        {
            // RO.Util.DebugLog("GenerateLightweightInsights start");
            List<string> insightsStr = new List<string>();
            Boundness(metrics, insightsStr, false, aswOn);
            if (runtimeSnapshot != null)
            {
                RuntimeResource(runtimeSnapshot, insightsStr);
            }
            return insightsStr.ToArray();
        }

        static public List<InsightTrigger> GenerateTriggerts(Dictionary<string, InsightTrigger> triggerMap, JSONObject runtimeMetrics, JSONObject runtimeSnapshot)
        {
            List<InsightTrigger> triggeredInsights = new List<InsightTrigger>();


            if (runtimeSnapshot["totalDrawcount"] != null)
            {
                int drawcallCount = runtimeSnapshot["totalDrawcount"].AsInt;
                if (triggerMap.ContainsKey("DrawCall Count"))
                {
                    InsightTrigger trigger = triggerMap["DrawCall Count"];
                    if (drawcallCount > trigger.Min)
                    {
                        triggeredInsights.Add(trigger);
                    }
                }
            }

            if (runtimeMetrics != null)
            {
                foreach (var trigger in triggerMap)
                {
                    var valNode = GetNodeVal(runtimeMetrics, trigger.Value.Title);
                    if (valNode != null)
                    {
                        float val = GetFloatVal(valNode, "quantile_75");
                        if (val > trigger.Value.Min)
                        {
                            triggeredInsights.Add(trigger.Value);

                            RO.Util.DebugLog("Trigger: " + trigger.Value.Title + " " + val + " / " + trigger.Value.Min);
                        }
                    }
                    else
                    {
                        // exception drawcall count is captured in runtimeSnapshot
                        if (trigger.Value.Title != "DrawCall Count")
                        {
                            UnityEngine.Debug.LogWarning("Failed to find " + trigger.Value.Title + " in perfetto metrics");
                        }
                    }
                }
            }

            return triggeredInsights;
        }

        static public void GenerateAnalysis(JSONObject runtimeSnapshot, JSONObject runtimeMetrics, InsightData insights, bool genMaterial)
        {

            insights.Clear();
            // IN favor of using meshAssetVertArray
            // insights.meshVertArray = GetMeshAndVertCount(runtimeSnapshot, out insights.totalVertexCount);

            Dictionary<string, (Texture, TextureRuntimeData)> textureDict = GetTextureDictionary(runtimeSnapshot, out insights.totalTextureMemory);
            insights.textureArray = GetTextureArray(textureDict);

            if (genMaterial)
            {
                insights.materialArray = GetMaterialList(runtimeSnapshot);
                ProcessTextureUsedCount(insights.materialArray, textureDict);
            }

            insights.meshAssetVertArray = GetMeshList(runtimeSnapshot, out insights.totalVertexCount);

            insights.drawcallCount = runtimeSnapshot["totalDrawcount"].AsInt;
            insights.totalTriangleCount = (uint)runtimeSnapshot["totalTriangleCount"].AsInt;
            insights.MSAALevel = (int)GetFloatVal(GetNodeVal(runtimeMetrics, "MSAA"), "mean");

            List<InsightTrigger> tiggers = GenerateTriggerts(insights.insightTriggersMap, runtimeMetrics, runtimeSnapshot);
            foreach (var trigger in tiggers)
            {
                foreach (string category in trigger.Category)
                {
                    insights.AddTriggerToCategory(category, trigger);
                }
            }
        }

        static public (int, bool) GetTextureFormatInfo(string format)
        {
            return format switch
            {
                "Alpha8" => (8, false),
                "ARGB4444" => (16, false),
                "RGB24" => (24, false),
                "RGBA32" => (32, false),
                "ARGB32" => (32, false),
                "RGB565" => (16, false),
                "R16" => (16, false),
                "DXT1" => (4, true),
                "DXT5" => (8, true),
                "RGBA4444" => (16, false),
                "BGRA32" => (32, false),
                "RHalf" => (16, false),
                "RGHalf" => (32, false),
                "RGBAHalf" => (64, false),
                "RFloat" => (32, false),
                "RGFloat" => (64, false),
                "RGBAFloat" => (128, false),
                "YUY2" => (16, false),
                "RGB9e5Float" => (32, false),
                "BC4" => (4, true),
                "BC5" => (8, true),
                "BC6H" => (8, true),
                "BC7" => (8, true),
                "DXT1Crunched" => (4, true),
                "DXT5Crunched" => (8, true),
                "PVRTC_RGB2" => (2, true),
                "PVRTC_RGBA2" => (2, true),
                "PVRTC_RGB4" => (4, true),
                "PVRTC_RGBA4" => (4, true),
                "ETC_RGB4" => (4, true),
                "EAC_R" => (4, true),
                "EAC_R_SIGNED" => (4, true),
                "EAC_RG" => (8, true),
                "EAC_RG_SIGNED" => (8, true),
                "ETC2_RGB" => (4, true),
                "ETC2_RGBA1" => (5, true),
                "ETC2_RGBA8" => (8, true),
                "ASTC_4x4" => (8, true),
                "ASTC_5x5" => (5, true),
                "ASTC_6x6" => (4, true),
                "ASTC_8x8" => (2, true),
                "ASTC_10x10" => (1, true),
                "ASTC_12x12" => (1, true),
                "RG16" => (16, false),
                "R8" => (8, false),
                "ETC_RGB4Crunched" => (4, true),
                "ETC2_RGBA8Crunched" => (8, true),
                _ => (0, false)
            };
        }

        static public string GetMetricVersion()
        {
            return metricVersion;
        }
    }
}
