Shader "Custom/VolumetricFlashlightCone_NoDepthURP"
{
    Properties
    {
        _Color("Color", Color) = (1,0,0,1)
        _Intensity("Intensity", Range(0,5)) = 2.0
        _Alpha("Alpha", Range(0,1)) = 0.35

        _EdgeSoftness("Edge Softness", Range(0.01,1)) = 0.55
        _RadialPower("Radial Power", Range(0.5,8)) = 1.6
        _DistanceFade("Distance Fade", Range(0.2,6)) = 0.8

        _ConeLength("Cone Length", Float) = 6
        _ConeHalfWidth("Cone Half Width", Float) = 3
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;
            float _Intensity;
            float _Alpha;

            float _EdgeSoftness;
            float _RadialPower;
            float _DistanceFade;

            float _ConeLength;
            float _ConeHalfWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;

                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(ws);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float z01 = saturate(IN.positionOS.z / max(0.0001, _ConeLength));
                float r01 = saturate(abs(IN.positionOS.x) / max(0.0001, _ConeHalfWidth));

                float edge = pow(saturate(1.0 - r01), _RadialPower);
                float dist = pow(saturate(1.0 - z01), _DistanceFade);
                float rim = smoothstep(0.0, _EdgeSoftness, edge);

                float beam = edge * dist * rim;

                float a = _Alpha * beam;
                half3 rgb = _Color.rgb * (_Intensity * beam);

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
