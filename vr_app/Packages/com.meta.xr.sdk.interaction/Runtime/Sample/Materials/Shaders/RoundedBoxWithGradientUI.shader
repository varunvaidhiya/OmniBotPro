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

Shader "Unlit/RoundedBoxWithGradientUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        [HideInInspector] _StencilComp		("Stencil Comparison", Float) = 0
	    [HideInInspector] _Stencil			("Stencil ID", Float) = 0
	    [HideInInspector] _StencilOp		("Stencil Operation", Float) = 0
	    [HideInInspector] _StencilWriteMask	("Stencil Write Mask", Float) = 255
	    [HideInInspector] _StencilReadMask	("Stencil Read Mask", Float) = 255

	    _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _BorderWidth ("Border Width", Float) = 0
        [Enum(Off,0,On,1)]_ZWrite ("ZWrite", Float) = 1.0

        [Space(20)]

        [Enum(Linear,0,Smooth,1)]_Interpolator ("Interpolator", Float) = 1.0
        [Space(10)]
        _BorderColorA ("Color Border A", Color) = (1,1,1,1)
        _BorderColorB ("Color Border B", Color) = (1,1,1,1)
        _BorderLine ("Border Line Start End", Vector) = (0,0,0,0)

        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _Line ("Line Start End", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags
	    {
		    "Queue"="Transparent"
		    "IgnoreProjector"="True"
		    "RenderType"="Transparent"
	    }

	    Stencil
	    {
		    Ref [_Stencil]
		    Comp [_StencilComp]
		    Pass [_StencilOp]
		    ReadMask [_StencilReadMask]
		    WriteMask [_StencilWriteMask]
	    }

	    Cull Off
        Lighting Off
        ZWrite [_ZWrite]
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha, OneMinusDstAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
		    #pragma multi_compile __ UNITY_UI_CLIP_RECT
		    #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "../../ThirdParty/Box2DSignedDistance.cginc"

            struct vertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                //--- Custom
                float4 borderRadius : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct fragmentInput
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                //--- Custom
                float4 borderRadius : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _BorderWidth;

            float _Interpolator;

            fixed4 _BorderColorA;
            fixed4 _BorderColorB;
            float4 _BorderLine;

            fixed4 _ColorA;
            fixed4 _ColorB;
            float4 _Line;

            fragmentInput vert(vertexInput input)
            {
                fragmentInput output;

                UNITY_INITIALIZE_OUTPUT(fragmentInput, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = input.texcoord;
                output.color = input.color;
                output.borderRadius = input.borderRadius;

                return output;
            }

            fixed4 frag (fragmentInput input) : SV_Target
            {
                float2 rectSize = input.texcoord.zw;
                float2 uv = input.texcoord.xy * rectSize;
                uv = uv - rectSize * 0.5;

                float dist = 1.0 - 1.0 - sdRoundBox(uv, rectSize * 0.5, input.borderRadius);
                float2 ddDist = float2(ddx(dist), ddy(dist));
                float ddDistLen = length(ddDist);

                float borderWidth = max(_BorderWidth, 0.0);
                float borderDist = borderWidth - dist;

                dist = dist / ddDistLen;
                borderDist = borderDist / ddDistLen;

                float4 scaledLine = _Line * float4(rectSize, rectSize) * 0.5;
                float2 innerStart = scaledLine.xy;
                float2 innerEnd = scaledLine.zw;

                float2 lineDir = innerEnd - innerStart;
                float sqrLen = dot(lineDir, lineDir);
                float4 innerColor = _ColorA;
                if (sqrLen > 0.0001) {
                    float2 uvLine = uv - innerStart;
                    float lineParam = dot(uvLine, lineDir) / sqrLen;

                    if (_Interpolator < 0.5){
                        lineParam = saturate(lineParam);
                    }else {
                        lineParam = smoothstep(0.0, 1.0, lineParam);
                    }
                    innerColor = lerp(_ColorA, _ColorB, lineParam);
                }

                float4 scaledBorderLine = _BorderLine * float4(rectSize, rectSize) * 0.5;
                float2 outerStart = scaledBorderLine.xy;
                float2 outerEnd = scaledBorderLine.zw;

                float2 borderLineDir = outerEnd - outerStart;
                float sqrBorderLen = dot(borderLineDir, borderLineDir);
                float4 outerColor = _BorderColorA;
                if (sqrBorderLen > 0.0001) {
                    float2 uvLine = uv - outerStart;
                    float lineParam = dot(uvLine, borderLineDir) / sqrBorderLen;

                    if (_Interpolator < 0.5){
                        lineParam = saturate(lineParam);
                    }else {
                        lineParam = smoothstep(0.0, 1.0, lineParam);
                    }
                    outerColor = lerp(_BorderColorA, _BorderColorB, lineParam);
                }

                half4 color = input.color;

                color *= lerp(innerColor, outerColor, clamp(borderDist, 0.0, 1.0));
                color.a *= clamp(dist, 0.0, 1.0);

                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip (color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
