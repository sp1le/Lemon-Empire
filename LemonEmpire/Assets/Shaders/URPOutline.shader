Shader "Custom/URPHighlight"
{
    Properties
    {
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 0.6)
        _FresnelPower ("Fresnel Power", Range(0.5, 5.0)) = 2.5
        _FresnelIntensity ("Fresnel Intensity", Range(0.0, 3.0)) = 1.8
        _BaseIntensity ("Base Intensity", Range(0.0, 0.3)) = 0.04
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Name "Highlight"
            Tags { "LightMode"="SRPDefaultUnlit" }
            
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS  : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _HighlightColor;
                float _FresnelPower;
                float _FresnelIntensity;
                float _BaseIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Fresnel: bright at edges (view perpendicular to normal), fades at center
                float NdotV = saturate(dot(viewDir, normal));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                float intensity = fresnel * _FresnelIntensity + _BaseIntensity;
                
                return float4(_HighlightColor.rgb * intensity, intensity * _HighlightColor.a);
            }
            ENDHLSL
        }
    }
}