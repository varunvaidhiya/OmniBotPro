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

Shader "Interaction/RoundedPanelUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        [HideInInspector] _StencilComp    ("Stencil Comparison", Float) = 0
        [HideInInspector] _Stencil        ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp      ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask   ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask    ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Enum(Off,0,On,1)]_ZWrite ("ZWrite", Float) = 1.0

        [Space(20)]
        [Header(Render Mode)]
        [Enum(FillOnly,0,FillAndBorder,1,BorderOnly,2)] _RenderMode ("Render Mode", Int) = 1
        [Min(0)] _BorderWidth ("Border Width", Float) = 2

        [Space(20)]
        [Header(Fill Settings)]
        [Enum(Solid,0,Gradient,1)] _FillMode ("Fill Color Mode", Int) = 0
        _FillColorA ("Fill Color A (Solid or Gradient Start)", Color) = (1,1,1,1)
        _FillColorB ("Fill Color B (Gradient End)", Color) = (0.8,0.8,0.8,1)
        _FillLine ("Fill Line Start End", Vector) = (-1,0,1,0)
        [Enum(Linear,0,Smooth,1)] _FillInterpolator ("Fill Interpolator", Float) = 1.0

        [Space(20)]
        [Header(Border Settings)]
        [Enum(Solid,0,Gradient,1)] _BorderMode ("Border Color Mode", Int) = 0
        _BorderColorA ("Border Color A (Solid or Gradient Start)", Color) = (0.5,0.5,0.5,1)
        _BorderColorB ("Border Color B (Gradient End)", Color) = (0.3,0.3,0.3,1)
        _BorderLine ("Border Line Start End", Vector) = (-1,0,1,0)
        [Enum(Linear,0,Smooth,1)] _BorderInterpolator ("Border Interpolator", Float) = 1.0
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
            #include "../ThirdParty/Box2DSignedDistance.cginc"

            struct vertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                float4 borderRadius : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct fragmentInput
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 borderRadius : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            // Render mode
            int _RenderMode;
            float _BorderWidth;

            // Fill properties
            int _FillMode;
            fixed4 _FillColorA;
            fixed4 _FillColorB;
            float4 _FillLine;
            float _FillInterpolator;

            // Border properties
            int _BorderMode;
            fixed4 _BorderColorA;
            fixed4 _BorderColorB;
            float4 _BorderLine;
            float _BorderInterpolator;

            // Helper function to calculate gradient color along a line
            float4 calculateGradientColor(float2 uv, float2 rectSize, float4 colorA, float4 colorB, float4 gradientLine, float interpolator)
            {
                float4 scaledLine = gradientLine * float4(rectSize, rectSize) * 0.5;
                float2 lineStart = scaledLine.xy;
                float2 lineEnd = scaledLine.zw;

                float2 lineDir = lineEnd - lineStart;
                float sqrLen = dot(lineDir, lineDir);

                float2 uvLine = uv - lineStart;
                float lineParam = dot(uvLine, lineDir) / sqrLen;

                if (interpolator < 0.5) {
                    lineParam = saturate(lineParam);
                } else {
                    lineParam = smoothstep(0.0, 1.0, lineParam);
                }

                return lerp(colorA, colorB, lineParam);
            }

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

                float dist = -sdRoundBox(uv, rectSize * 0.5, input.borderRadius);
                float2 ddDist = float2(ddx(dist), ddy(dist));
                float ddDistLen = length(ddDist);

                float borderDist = _BorderWidth - dist;

                dist = dist / ddDistLen;
                borderDist = borderDist / ddDistLen;

                // Calculate fill color based on mode
                float4 fillColor;
                if (_FillMode == 0) {
                    fillColor = _FillColorA;
                } else {
                    fillColor = calculateGradientColor(uv, rectSize, _FillColorA, _FillColorB, _FillLine, _FillInterpolator);
                }

                // Calculate border color based on mode
                float4 borderColor;
                if (_BorderMode == 0) {
                    borderColor = _BorderColorA;
                } else {
                    borderColor = calculateGradientColor(uv, rectSize, _BorderColorA, _BorderColorB, _BorderLine, _BorderInterpolator);
                }

                half4 color = input.color;
                half4 texColor = tex2D(_MainTex, input.texcoord.xy) + _TextureSampleAdd;
                color *= texColor;

                float borderBlend = clamp(borderDist, 0.0, 1.0);
                float edgeAlpha = clamp(dist, 0.0, 1.0);

                // Apply render mode
                if (_RenderMode == 0) {
                    // Fill Only - no border
                    color *= fillColor;
                    color.a *= edgeAlpha;
                }
                else if (_RenderMode == 1) {
                    // Fill + Border
                    color *= lerp(fillColor, borderColor, borderBlend);
                    color.a *= edgeAlpha;
                }
                else {
                    // Border Only
                    color *= borderColor;
                    color.a *= edgeAlpha * borderBlend;
                }

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