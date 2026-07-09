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
Shader "Interaction/SurfaceRipple_Sprite_Cursor"
{
    Properties
    {
        [HDR]_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,1)
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _SpriteSize ("Sprite Sheet Tile Count (e.g., 10 for a 10x10 sheet)", Float ) = 10
        _BeforeTouch ("Sprite Sheet Before Touching", 2D) = "white" {}
        _AfterTouch ("Sprite Sheet After Touching", 2D) = "white" {}

        [HideInInspector]_MainTex ("Main Texture", 2D) = "white" {}
    }

    // SubShader for Universal Render Pipeline (URP)
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1,-1
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "Cursor.cginc"

            UNITY_DECLARE_TEX2D(_AfterTouch);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_BeforeTouch);

            float _RippleProgress;

            CBUFFER_START(UnityPerMaterial)
                float4 _AfterTouch_ST, _BeforeTouch_ST;
                float4 _TintColor;
                float _Opacity, _SpriteSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 spriteUV : TEXCOORD1; float rippleProgress : TEXTCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = UnityObjectToClipPos(input.positionOS.xyz);
                output.uv = input.uv;

                float progressAbs = abs(_RippleProgress);
                float progressFactor = _RippleProgress > 0 ? (1.0 - progressAbs) : progressAbs;

                float p = CalculateUVwithProgress((_SpriteSize * _SpriteSize - 1.0) * progressFactor, _SpriteSize);
                float invSpriteSize = 1.0 / _SpriteSize;
                float y_unit = floor(p * invSpriteSize);
                float x_unit = p - _SpriteSize * y_unit;

                output.spriteUV = (input.uv + float2(x_unit, y_unit)) * invSpriteSize.xx;
                output.rippleProgress = _RippleProgress;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 spriteColor = (input.rippleProgress < 0) ?
                    UNITY_SAMPLE_TEX2D(_AfterTouch, input.spriteUV) :
                    UNITY_SAMPLE_TEX2D_SAMPLER(_BeforeTouch, _AfterTouch, input.spriteUV);

                half3 emissive = (spriteColor.rgb * _TintColor.rgb * 1.5);
                return half4(emissive, spriteColor.a * _Opacity);
            }
            ENDHLSL
        }
    }

    // SubShader for Built-in Render Pipeline
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1,-1
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Cursor.cginc"

            UNITY_DECLARE_TEX2D(_AfterTouch);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_BeforeTouch);

            uniform float _RippleProgress;

            UNITY_INSTANCING_BUFFER_START( Props )
                UNITY_DEFINE_INSTANCED_PROP(float, _SpriteSize)
                UNITY_DEFINE_INSTANCED_PROP(float, _Opacity)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TintColor)
            UNITY_INSTANCING_BUFFER_END( Props )

            struct VertexInput { UNITY_VERTEX_INPUT_INSTANCE_ID float4 vertex : POSITION; float2 texcoord0 : TEXCOORD0; };
            struct VertexOutput { float4 pos : SV_POSITION; float2 uv0 : TEXCOORD0; float2 spriteUV : TEXTCOORD1; float rippleProgress : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(VertexOutput, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex);

                float _SpriteSize_var = UNITY_ACCESS_INSTANCED_PROP(Props, _SpriteSize);

                float progressAbs = abs(_RippleProgress);
                float progressFactor = _RippleProgress > 0 ? (1.0 - progressAbs) : progressAbs;
                float p = CalculateUVwithProgress((_SpriteSize_var * _SpriteSize_var - 1.0) * progressFactor, _SpriteSize_var);
                float invSpriteSize = 1.0 / _SpriteSize_var;
                float y_unit = floor(p * invSpriteSize);
                float x_unit = p - _SpriteSize_var * y_unit;

                o.spriteUV = (v.texcoord0 + float2(x_unit, y_unit)) * invSpriteSize.xx;
                o.rippleProgress = _RippleProgress;
                return o;
            }

            float4 frag(VertexOutput i) : COLOR
            {
                float _Opacity_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Opacity );
                float4 _TintColor_var = UNITY_ACCESS_INSTANCED_PROP( Props, _TintColor );

                float4 spriteColor = (i.rippleProgress < 0) ?
                    UNITY_SAMPLE_TEX2D(_AfterTouch, i.spriteUV) :
                    UNITY_SAMPLE_TEX2D_SAMPLER(_BeforeTouch, _AfterTouch, i.spriteUV);

                float3 emissive = (spriteColor.rgb * _TintColor_var.rgb * 1.5);
                return fixed4(emissive, spriteColor.a * _Opacity_var);
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
