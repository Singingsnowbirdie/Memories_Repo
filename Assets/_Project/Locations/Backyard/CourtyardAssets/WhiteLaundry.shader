Shader "Memories/White Laundry"
{
 Properties { _BaseColor("Fabric", Color) = (1,1,1,1) _MinimumLight("Night visibility", Range(0,1)) = 0 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
 Pass {
 Tags { "LightMode"="UniversalForward" }
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _ADDITIONAL_LIGHTS
 #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor; float _MinimumLight;
 CBUFFER_END
 struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
 struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; };
 V vert(A v) { V o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o; }
 half4 frag(V i):SV_Target {
 half3 n=normalize(i.normalWS);
 // Fabric receives ambient/probe lighting as well as direct lamp lighting.
 half3 light=EvaluateAmbientProbeSRGB(n)+_MinimumLight*(0.8+0.2*abs(n.y));
 Light main=GetMainLight();light+=main.color*abs(dot(n,main.direction))*main.distanceAttenuation;
 #ifdef _ADDITIONAL_LIGHTS
 InputData inputData=(InputData)0;
 inputData.positionWS=i.positionWS;
 inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
 uint count=GetAdditionalLightsCount();
 LIGHT_LOOP_BEGIN(count)
 Light l=GetAdditionalLight(lightIndex,i.positionWS);light+=l.color*abs(dot(n,l.direction))*l.distanceAttenuation;
 LIGHT_LOOP_END
 #endif
 return half4(_BaseColor.rgb*light,1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}
