Shader "Memories/Chalk Drawing"
{
 Properties { _MainTex ("Drawing", 2D) = "black" {} _Color ("Chalk", Color) = (0.9,0.88,0.8,1) }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Tags { "LightMode"="UniversalForwardOnly" }
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 Offset -1, -1
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
 #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
 #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
 #pragma multi_compile_fog
 #define _SURFACE_TYPE_TRANSPARENT 1
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float4 _MainTex_ST; half4 _Color;
 CBUFFER_END
 struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float3 normalOS:NORMAL; };
 struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; half3 normalWS:TEXCOORD2; half fog:TEXCOORD3; half3 vertexLight:TEXCOORD4; };
 V vert(A v) {
 V o;
 o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
 o.positionCS=TransformWorldToHClip(o.positionWS);
 o.normalWS=TransformObjectToWorldNormal(v.normalOS);
 o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw;
 o.fog=ComputeFogFactor(o.positionCS.z);
 o.vertexLight=VertexLighting(o.positionWS,o.normalWS);
 return o;
 }
 half4 frag(V i):SV_Target {
 half3 rgb=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
 half mask=smoothstep(0.12,0.6,max(rgb.r,max(rgb.g,rgb.b)));
 InputData inputData=(InputData)0;
 inputData.positionWS=i.positionWS;
 inputData.normalWS=normalize(i.normalWS);
 inputData.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
 inputData.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
 inputData.bakedGI=SampleSH(inputData.normalWS);
 inputData.vertexLighting=i.vertexLight;
 inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
 inputData.shadowMask=half4(1,1,1,1);
 SurfaceData surfaceData=(SurfaceData)0;
 surfaceData.albedo=rgb*_Color.rgb;
 surfaceData.alpha=mask*_Color.a;
 surfaceData.occlusion=1;
 surfaceData.normalTS=half3(0,0,1);
 half4 color=UniversalFragmentBlinnPhong(inputData,surfaceData);
 color.rgb=MixFog(color.rgb,i.fog);
 return color;
 }
 ENDHLSL
 }
 }
}