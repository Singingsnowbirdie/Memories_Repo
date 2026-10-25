Shader "Memories/HighwayEdgeFog"
{
    Properties { _Density ("Density", Range(0, 2)) = 0.8 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Density;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 origin = GetCameraPositionWS();
                float3 ray = normalize(input.positionWS - origin);
                float3 localOrigin = TransformWorldToObject(origin);
                float3 localRay = mul((float3x3)unity_WorldToObject, ray);
                float3 safeRay = lerp(-1.0, 1.0, step(0.0, localRay)) * max(abs(localRay), 0.000001);
                float3 t0 = (-0.5 - localOrigin) / safeRay;
                float3 t1 = (0.5 - localOrigin) / safeRay;
                float3 nearT = min(t0, t1);
                float3 farT = max(t0, t1);
                float entry = max(0, max(nearT.x, max(nearT.y, nearT.z)));
                float exitDistance = min(farT.x, min(farT.y, farT.z));
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 surface = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                exitDistance = min(exitDistance, dot(surface - origin, ray));
                float stepLength = max(0, exitDistance - entry) / 24.0;
                float opticalDepth = 0;
                for (int i = 0; i < 24; i++)
                {
                    float distance = entry + (i + 0.5) * stepLength;
                    float3 p = localOrigin + localRay * distance;
                    float edge = smoothstep(0, 0.15, 0.5 - abs(p.x));
                    edge *= smoothstep(0, 0.2, 0.5 - abs(p.y));
                    float entrance = smoothstep(0, 0.35, 0.5 - p.z);
                    float3 world = origin + ray * distance;
                    float mist = 0.85 + 0.15 * sin(world.x * 0.23 + world.z * 0.17 + _Time.y * 0.12)
                        * sin(world.y * 0.31 - world.z * 0.13);
                    opticalDepth += edge * entrance * mist * _Density * stepLength;
                }
                return half4(0, 0, 0, 1 - exp(-opticalDepth));
            }
            ENDHLSL
        }
    }
}
