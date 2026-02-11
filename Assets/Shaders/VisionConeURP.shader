Shader "Custom/VisionConeURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,0,0,0.25)
        _EdgeSoftness("Edge Softness", Range(0.001, 0.5)) = 0.12
        _OuterFade("Outer Fade", Range(0.001, 1)) = 0.22
        _InnerFade("Inner Fade", Range(0.0, 1)) = 0.05

        _CoreWidth("Core Width", Range(0.001, 0.5)) = 0.12
        _CoreStrength("Core Strength", Range(0.0, 3.0)) = 1.2
        _CoreAlphaBoost("Core Alpha Boost", Range(0.0, 2.0)) = 0.6
    }

        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 _BaseColor;
            float _EdgeSoftness;
            float _OuterFade;
            float _InnerFade;

            float _CoreWidth;
            float _CoreStrength;
            float _CoreAlphaBoost;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float u = saturate(IN.uv.x);
                float v = saturate(IN.uv.y);

                float edge = min(u, 1.0 - u);
                float edgeFade = smoothstep(0.0, _EdgeSoftness, edge);

                float outerFade = 1.0 - smoothstep(1.0 - _OuterFade, 1.0, v);

                float innerFade = smoothstep(_InnerFade, _InnerFade + 0.05, v);

                float distToCenter = abs(u - 0.5);
                float core = 1.0 - smoothstep(_CoreWidth, _CoreWidth + 0.15, distToCenter);
                core *= (0.35 + 0.65 * v);

                half4 col = _BaseColor;

                col.rgb = saturate(col.rgb + core * _CoreStrength * col.rgb);

                float alpha = col.a * edgeFade * outerFade * innerFade;
                alpha = saturate(alpha + core * _CoreAlphaBoost * _BaseColor.a);

                col.a = alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
