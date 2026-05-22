Shader "Custom/CurvedWorldURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}

        _CurveStrength ("Curve Strength", Float) = 0.003
        _CurveStartDistance ("Curve Start Distance", Float) = 8
        _CurveSideStrength ("Side Curve Strength", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _CurveStrength;
                float _CurveStartDistance;
                float _CurveSideStrength;
            CBUFFER_END

            float4 _CurveOrigin;

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float distanceZ = worldPos.z - _CurveOrigin.z;
                float curveDistance = max(0, distanceZ - _CurveStartDistance);

                float curveAmount = curveDistance * curveDistance * _CurveStrength;

                worldPos.y -= curveAmount;
                worldPos.x += curveDistance * curveDistance * _CurveSideStrength;

                output.positionHCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                return texColor * _BaseColor;
            }

            ENDHLSL
        }
    }
}