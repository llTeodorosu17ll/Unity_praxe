Shader "Custom/VolumetricFlashlightConeURP"
{
    Properties
    {
        _Color("Color", Color) = (1,0,0,1)
        _Intensity("Intensity", Range(0,5)) = 1.8
        _Alpha("Alpha", Range(0,1)) = 0.35

        _EdgeSoftness("Edge Softness", Range(0.01,1)) = 0.35
        _RadialPower("Radial Power", Range(0.5,8)) = 1.6
        _DistanceFade("Distance Fade", Range(0.2,6)) = 1.0

        _ConeLength("Cone Length", Float) = 6
        _ConeHalfWidth("Cone Half Width", Float) = 3

        _DepthFade("Depth Fade", Range(0.0,0.5)) = 0.15

        // 1 = use depth-based occlusion (Game view)
        // 0 = disable depth occlusion (Scene view / edit mode)
        _EnableDepthOcclusion("Enable Depth Occlusion", Float) = 1
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off

        // Less "overglow" than additive, more stable/controllable
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            float4 _Color;
            float _Intensity;
            float _Alpha;

            float _EdgeSoftness;
            float _RadialPower;
            float _DistanceFade;

            float _ConeLength;
            float _ConeHalfWidth;

            float _DepthFade;
            float _EnableDepthOcclusion;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 positionNDC : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;

                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(ws);
                OUT.positionNDC = OUT.positionHCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize cone coordinates
                float z01 = saturate(IN.positionOS.z / max(0.0001, _ConeLength));         // 0..1 forward
                float r01 = saturate(abs(IN.positionOS.x) / max(0.0001, _ConeHalfWidth)); // 0..1 to edges

                // Beam profile: strong center, fade to edges and over distance
                float edge = pow(saturate(1.0 - r01), _RadialPower);
                float dist = pow(saturate(1.0 - z01), _DistanceFade);

                // Extra smooth edges
                float rim = smoothstep(0.0, _EdgeSoftness, edge);

                float beam = edge * dist * rim;

                // Depth-based occlusion (prevents showing through walls)
                float occlusion = 1.0;

                if (_EnableDepthOcclusion > 0.5)
                {
                    float2 uv = (IN.positionNDC.xy / IN.positionNDC.w) * 0.5 + 0.5;

                    float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                    float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float fragEye = LinearEyeDepth(IN.positionHCS.z / IN.positionHCS.w, _ZBufferParams);

                    // If frag is behind geometry -> fade it out
                    float behind = saturate((fragEye - sceneEye) / max(0.0001, _DepthFade));
                    occlusion = 1.0 - behind;
                }

                float a = _Alpha * beam * occlusion;
                half3 rgb = _Color.rgb * (_Intensity * beam * occlusion);

                return half4(rgb, a);
            }
            ENDHLSL
        }
}
}
