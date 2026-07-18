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
using UnityEditor.Rendering;
using UnityEngine;

namespace Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight
{
    [System.Serializable]
    public class UnityInsight
    {
        public string Category { get; }

        public string Description { get; }

        public string Insight { get; }

        public string[] LinksToDoc { get; }

        public UnityInsight(string category, string description, string insight, string[] links)
        {
            Category = category;
            Description = description;
            Insight = insight;
            LinksToDoc = links;
        }
    }

    [System.Serializable]
    public class TextureRuntimeData
    {
        public string format;
        public string filterMode;
        public long runtimeSize;
        public int width;
        public int height;
        public int anisoLevel;
        public int depth;
        public int mipCount;
        public int usedByCount;
        public bool isReadable;
    }

    [System.Serializable]
    public class MeshRuntimeData
    {
        public string shortPath = "";
        public int drawCount = 0;

        public int dynamicVertexCount = 0;
    }

    public class ShaderData
    {
        //TODO: Refactor to get the data from PIL
        public List<AdrenoOfflineCompilerUtilityRO.ShaderStatInfo> shaderStats { get; set; }
        public int usedByCount { get; set; }
        public Shader shader { get; set; }
        public string shaderName { get; set; }
        public int subshaderCount { get; set; }
        public List<Texture> textureList { get; set; }
    }

    [System.Serializable]
    public class InsightTrigger
    {
        public string Title { get; set; }
        public string[] Category { get; set; }
        public string Description { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
    }

    [Serializable]
    public class InsightEventData
    {
        public string[] Devices;
        public bool IsForzen;
    }

    [Serializable]
    public class InsightEventDataStr
    {
        public string data;
        InsightEventDataStr(string src)
        {
            data = src;
        }

        static public string ToJsonStr(string src)
        {
            InsightEventDataStr item = new InsightEventDataStr(src);
            return JsonUtility.ToJson(item);
        }
    }
}
