/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Unlit/LightmapPBR"
{
	Properties
	{
		[Header(Blending)]
		[Space]
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Int) = 1
		
		[Header(Properties)]
		[Space]
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset] _MetallicGloss("Metallic Gloss", 2D) = "black" {}
		[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
		[NoScaleOffset] _Occlusion("Occlusion", 2D) = "white" {}
		[NoScaleOffset] _Emission("Emission", 2D) = "black" {}

		[Header(Lighting)]
		[Space]
		[NoScaleOffset] _Lightmap("Light Map", 2D) = "white" {}		
		[Toggle] _VertexColorLightmap("Vertex Color Lightmap", Float) = 0
		_LightmapExposure ("Exposure", Float) = 1
	}

		CGINCLUDE
#include "UnityCG.cginc"
			#pragma shader_feature _ ENABLE_SPECULAR_GI
			#pragma shader_feature _ ENABLE_FOG
			#pragma shader_feature VERTEX_COLOR_LIGHTMAP

			struct appdata
			{
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				fixed4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float3 tangentWS: TEXCOORD3;
				float3 bitangentWS: TEXCOORD4;
				float3 positionWS  : TEXCOORD5;
				fixed4 color : COLOR;
				#ifndef ENABLE_FOG
				UNITY_FOG_COORDS(6)
				#endif
					float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// standard properties
			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _BumpMap;
			sampler2D _MetallicGloss;
			sampler2D _Occlusion;
			sampler2D _Emission;
			sampler2D _Lightmap;
			fixed _VertexColorLightmap;
			float _LightmapExposure;

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv0 = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.uv1 = v.texcoord1;
				o.positionWS = mul(unity_ObjectToWorld, v.vertex);
				o.normalWS = normalize(UnityObjectToWorldNormal(v.normal));
				o.tangentWS = normalize(mul(unity_ObjectToWorld, v.tangent).xyz);
				o.bitangentWS = normalize(cross(o.normalWS, o.tangentWS.xyz));
				o.color = v.color;
				#ifndef ENABLE_FOG
				UNITY_TRANSFER_FOG(o, o.vertex);
				#endif
				

				return o;
			}

			// UnityStandardUtils.cginc#L46
			half3 DiffuseAndSpecularFromMetallic(half3 albedo, half metallic, out half3 specColor, out half oneMinusReflectivity)
			{
				specColor = lerp(unity_ColorSpaceDielectricSpec.rgb, albedo, metallic);
				half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a;
				oneMinusReflectivity = oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
				return albedo * oneMinusReflectivity;
			}

			// UnityImageBasedLighting.cginc#L522
			half3 Unity_GlossyEnvironment(half3 reflectDir, half perceptualRoughness)
			{
				perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
				half mip = perceptualRoughness * 6;
				half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectDir, mip);
				return DecodeHDR(rgbm, unity_SpecCube1_HDR);
			}

			inline half3 Pow4(half3 x)
			{
				return x * x * x * x;
			}

			// UnityStandardBRDF.cginc#L92
			inline half3 FresnelLerp(half3 F0, half3 F90, half cosA)
			{
				half t = Pow4(1 - cosA);
				return lerp(F0, F90, t);
			}

			// UnityStandardBRDF.cginc#L299
			half SurfaceReduction(float perceptualRoughness)
			{
				float roughness = perceptualRoughness * perceptualRoughness;
#ifdef UNITY_COLORSPACE_GAMMA
				return 1.0 - 0.28 * roughness * perceptualRoughness;
#else
				return 1.0 / (roughness * roughness + 1.0);
#endif
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// BRDF texture inputs
				fixed4 mainTex = tex2D(_MainTex, i.uv0);
				float4 bumpMap = tex2D(_BumpMap, i.uv0);
				float4 metallicGloss = tex2D(_MetallicGloss, i.uv0);
				float4 occlusion = tex2D(_Occlusion, i.uv0);
				float4 emission = tex2D(_Emission, i.uv0);

				// BEDF values
				fixed3 albedo = mainTex.rgb * _Color.rgb;
				float metalness = metallicGloss.r;
				float smoothness = metallicGloss.a;

				float oneMinusReflectivity;
				float3 specColor;
				float3 diffColor = DiffuseAndSpecularFromMetallic(albedo.rgb, metalness, /*out*/ specColor, /*out*/ oneMinusReflectivity);

				float3x3 tangentMatrix = transpose(float3x3(i.tangentWS, i.bitangentWS, i.normalWS));
				float3 normal = normalize(mul(tangentMatrix, UnpackNormal(bumpMap)));
				
				float3 ambient = _VertexColorLightmap ? i.color : tex2D(_Lightmap, i.uv1).rgb;
				ambient *= _LightmapExposure;

				half3 diffuse = diffColor * ambient * occlusion.r;

				half3 specular = 0;
				#ifndef ENABLE_SPECULAR_GI
				float perceptualRoughness = 1 - smoothness;

				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
				half nv = abs(dot(normal, viewDir));

				float3 reflectDir = -reflect(viewDir, normal);
				float3 specularGI = Unity_GlossyEnvironment(reflectDir, perceptualRoughness);
				half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));

				specular = SurfaceReduction(perceptualRoughness) * specularGI * FresnelLerp(specColor, grazingTerm, nv);
                #endif
				
				// non BRDF texture inputs
				half3 color = diffuse + specular + emission;
				#ifndef ENABLE_FOG
				UNITY_APPLY_FOG(i.fogCoord, color);
				#endif

				half alpha = mainTex.a * _Color.a;
				return fixed4(color, alpha);
			}
				ENDCG

			SubShader
			{
				Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
				Pass
				{
					Tags{ "LightMode" = "UniversalForward" }
					Blend [_SrcBlend] [_DstBlend], OneMinusDstAlpha One
					ZWrite [_ZWrite]
					CGPROGRAM
					#pragma shader_feature VERTEX_COLOR_LIGHTMAP
					#pragma vertex vert
					#pragma fragment frag
					#pragma multi_compile_fog
					ENDCG
				}
			}

			SubShader
			{
				Tags{ "RenderType" = "Opaque" }
				Pass
				{
					Tags{ "LightMode" = "ForwardBase" }
					Blend [_SrcBlend] [_DstBlend], OneMinusDstAlpha One
					ZWrite [_ZWrite]
					CGPROGRAM
					#pragma shader_feature VERTEX_COLOR_LIGHTMAP
					#pragma vertex vert
					#pragma fragment frag
					#pragma multi_compile_fog
					ENDCG
				}
			}
}
