// OhhO glass panel — URP unlit, transparent. Recreates the website's frosted
// glass card (components/GlassCard.tsx) for world-space VR panels over Quest
// passthrough: a translucent dark fill with a rounded-rect accent border that
// glows in OhhO cyan/violet, plus a soft inner vignette.
//
// Real backdrop blur is intentionally omitted: over passthrough there is no
// camera scene-colour to sample, and blur is costly on Quest. A tasteful
// translucent gradient + glowing rim reads as "glass" and stays cheap.
//
// Apply to a panel Quad or a UI RawImage. Drive _FillColor/_AccentColor from
// OhhoTheme via the GlassPanel component (UI/Theme/GlassPanel.cs).
Shader "OhhO/Glass"
{
    Properties
    {
        _FillColor   ("Fill Color", Color)   = (0.039, 0.055, 0.102, 0.62) // ~#0A0E1A @ .62
        _AccentColor ("Accent Color", Color) = (0.0, 0.831, 1.0, 1.0)      // cyan #00D4FF
        _BorderWidth ("Border Width", Range(0,0.2)) = 0.012
        _BorderGlow  ("Border Glow", Range(0,4)) = 1.6
        _Radius      ("Corner Radius", Range(0,0.5)) = 0.06
        _Softness    ("Edge Softness", Range(0.0005,0.05)) = 0.004
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            float4 _FillColor;
            float4 _AccentColor;
            float  _BorderWidth;
            float  _BorderGlow;
            float  _Radius;
            float  _Softness;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            // Signed distance to a rounded rectangle centered in [0,1] UV space.
            float roundedBoxSDF(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // UV → centered coords [-0.5,0.5]
                float2 p = IN.uv - 0.5;
                float d = roundedBoxSDF(p, float2(0.5, 0.5), _Radius);

                // Panel mask (inside the rounded rect), with soft edge.
                float panel = 1.0 - smoothstep(-_Softness, _Softness, d);

                // Border band: bright near the edge, fading inward.
                float border = smoothstep(-_BorderWidth - _Softness, -_BorderWidth, d)
                             * (1.0 - smoothstep(-_Softness, _Softness, d));

                // Subtle inner vignette so the fill feels lit from the top-left,
                // matching the site's gradient glass.
                float vignette = saturate(1.0 - length(p) * 0.6);

                half4 fill = _FillColor;
                fill.rgb *= (0.85 + 0.3 * vignette);

                // Compose: fill + glowing accent border.
                half3 rgb = fill.rgb + _AccentColor.rgb * border * _BorderGlow;
                half  a   = max(fill.a * panel, border) * IN.color.a;

                return half4(rgb * IN.color.rgb, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
