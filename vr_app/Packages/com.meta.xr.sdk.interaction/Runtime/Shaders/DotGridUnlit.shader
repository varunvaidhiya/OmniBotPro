/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Interaction/DotGridUnlit"
{
    Properties {
        _Color("Color", Color) = (0, 0, 0, 1)

        // rows, columns, radius, displacement
        _Dimensions("Dimensions", Vector) = (1, 1, .1, 0)

        [Toggle] _CLIP ("Use clip", Integer) = 1
        _OffsetFactor("Offset Factor", float) = 0
        _OffsetUnits("Offset Units", float) = 0
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Offset[_OffsetFactor],[_OffsetUnits]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIP_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
                fixed4 color: COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            uniform float _OffsetFactor;
            uniform float _OffsetUnits;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Dimensions)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 dimensions = UNITY_ACCESS_INSTANCED_PROP(Props, _Dimensions);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv.xy = v.uv;
                o.uv.zw = float2(1/dimensions.x, 1/dimensions.y);
                o.color = color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 dimensions = UNITY_ACCESS_INSTANCED_PROP(Props, _Dimensions);
                float2 diameter = i.uv.zw;
                float2 uvOffset = i.uv + diameter * 0.5;
                float2 index = floor(uvOffset / diameter);
                float2 xy = index * diameter;
                float dist = distance(i.uv + dimensions.ww, xy);

                fixed4 color = i.color;
                #if _CLIP_ON
                    clip(dimensions.z - dist);
                #else
                    color.a *= saturate((dimensions.z - dist) * 100);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
