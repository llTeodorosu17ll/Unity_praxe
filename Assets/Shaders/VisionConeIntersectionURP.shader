Shader "Custom/VisionConeIntersectionURP"
{
    Properties
    {
        _Color("Color", Color) = (1,0,0,0.35)

        _SoftEdge("Soft Edge", Range(0.001, 0.2)) = 0.04
        _IntersectionWidth("Intersection Width", Range(0.001, 0.3)) = 0.06
        _Intensity("Intensity", Range(0.1, 5.0)) = 1.4

        _CoreWidth("Core Width", Range(0.001, 0.3)) = 0.08
        _CoreBoost("Core Boost", Range(0.0, 3.0)) = 0.8
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            float4 _Color;
            float _SoftEdge;
            float _IntersectionWidth;
            float _Intensity;
            float _CoreWidth;
            float _CoreBoost;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 positionNDC : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 positionOS  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.positionNDC = OUT.positionHCS;
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = (IN.positionNDC.xy / IN.positionNDC.w) * 0.5 + 0.5;

                float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                float fragEyeDepth = LinearEyeDepth(IN.positionHCS.z / IN.positionHCS.w, _ZBufferParams);

                float depthDiff = sceneEyeDepth - fragEyeDepth;

                float inter = smoothstep(0.0, _IntersectionWidth, depthDiff);

                inter *= smoothstep(_IntersectionWidth * 6.0, _IntersectionWidth, depthDiff);

                float z = saturate(IN.positionOS.z);
                float r = length(IN.positionOS.xy);

                float rim = 1.0 - smoothstep(1.0 - _SoftEdge, 1.0, r);

                float core = 1.0 - smoothstep(_CoreWidth, _CoreWidth + 0.15, r);
                core *= (0.35 + 0.65 * z);

                half4 col = _Color;
                col.rgb = saturate(col.rgb * _Intensity + core * _CoreBoost);

                float alpha = col.a * inter * rim;
                alpha = saturate(alpha + core * col.a * 0.35);

                col.a = alpha;
                return col;
            }
            ENDHLSL
        }
}
}
