Shader "Memories/Vehicles/Matte Faceted"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Matte"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                Light light = GetMainLight();
                half3 normal = normalize(input.normalWS);
                // Thin double-sided panels must light their visible side.
                normal *= dot(normal, GetWorldSpaceNormalizeViewDir(input.positionWS)) < 0 ? -1 : 1;
                half diffuse = saturate(dot(normal, light.direction));
                // Deliberately no textures, SSAO, specular, reflection probes or shadow sampling.
                half3 lighting = max(half3(.06,.06,.06), SampleSH(normal) * .35);
                lighting += diffuse * light.color * light.distanceAttenuation;
                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint i = 0; i < count; ++i)
                {
                    Light additional = GetAdditionalLight(i, input.positionWS);
                    lighting += saturate(dot(normal, additional.direction)) * additional.color * additional.distanceAttenuation;
                }
                #endif
                return half4(_BaseColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
}
