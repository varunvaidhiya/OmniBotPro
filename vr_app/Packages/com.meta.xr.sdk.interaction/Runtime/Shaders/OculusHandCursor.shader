/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Hidden/Interaction/Deprecated/OculusHandCursor"
{
    Properties
    {
        _OutlineWidth("OutlineWidth", Range( 0 , 0.4)) = 0.03
        _Color("Inner Color", Color) = (0,0,0,1)
        _OutlineColor("OutlineColor", Color) = (0,0.4410214,1,1)
        _Alpha("Alpha", Range( 0 , 1)) = 0
        _RadialGradientIntensity("RadialGradientIntensity", Range( 0 , 1)) = 0
        _RadialGradientScale("RadialGradientScale", Range( 0 , 1)) = 1
        _RadialGradientBackgroundOpacity("RadialGradientBackgroundOpacity", Range( 0 , 1)) = 0.1
        _RadialGradientOpacity("RadialGradientOpacity", Range( 0 , 1)) = 0.8550259
        [Toggle] _CLIP ("Use clip", Integer) = 1
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] __dirty( "", Int ) = 1
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+10" "IgnoreProjector" = "True"  }
        Cull Off
        ZTest LEqual
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Offset -5, -5
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _CLIP_ON

            #include "UnityCG.cginc"

            uniform float _RadialGradientScale;
            uniform float _RadialGradientOpacity;
            uniform float _RadialGradientIntensity;
            uniform float _RadialGradientBackgroundOpacity;
            uniform float _OutlineWidth;
            uniform float4 _Color;
            uniform float4 _OutlineColor;
            uniform float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 uv_texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv_texcoord.xy = v.uv_texcoord;
                o.uv_texcoord.z = 0.16 + _RadialGradientScale * 0.29;
                o.uv_texcoord.w = 5.0 + _RadialGradientIntensity * -3.5;
                return o;
            }

            static const float INLINE_OUTLINE_FALLOFF = 70.0;
            static const float OUTER_OUTLINE_FALLOFF = 20.0;
            static const float TRANSITION_THRESHOLD = 0.1;
            static const float2 CENTER =  float2(0.5, 0.5);

            fixed4 frag(v2f i) : SV_Target
            {
               float2 distToCenter = distance(i.uv_texcoord, CENTER);
               float gradientMaxRadius = i.uv_texcoord.z;
               float gradientFallOff = i.uv_texcoord.w;

               half radialGradientNormalized = 1.0 - (distToCenter * rcp(gradientMaxRadius));
               half mainRadialFalloff = saturate(rcp(exp(radialGradientNormalized * gradientFallOff)));
               half inlineOutlineFalloff = saturate(rcp(exp(radialGradientNormalized * INLINE_OUTLINE_FALLOFF)));
               half outlineShadowStrength = 1.0 - inlineOutlineFalloff;

               half radialGradientComposite = saturate(
                   _RadialGradientOpacity * (mainRadialFalloff - inlineOutlineFalloff)
                   + outlineShadowStrength * _RadialGradientBackgroundOpacity
               );

               half outlineRingNormalized = 1.0 - (distToCenter * rcp(gradientMaxRadius + _OutlineWidth));
               half outlineRingFalloff = 1.0 - saturate(rcp(exp(outlineRingNormalized * OUTER_OUTLINE_FALLOFF)));
               half4 outlineColorContribution = radialGradientComposite
                   + (outlineRingFalloff - outlineShadowStrength) * _OutlineColor;

               half transitionBlend = saturate((_RadialGradientScale - TRANSITION_THRESHOLD) * 1000);
               half4 pointerColor = lerp(_OutlineColor, outlineColorContribution, transitionBlend);

               half3 color = pointerColor.rgb * _Color.rgb;
               half alpha = pointerColor.a * _Alpha;

               #if _CLIP_ON
                   clip(alpha - 0.01);
               #endif
           
               return half4(color.rgb, alpha);
           }
           ENDCG
        }
    }
    Fallback "Diffuse"
}
