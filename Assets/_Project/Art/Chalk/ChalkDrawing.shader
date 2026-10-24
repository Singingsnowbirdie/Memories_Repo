Shader "Memories/Chalk Drawing"
{
 Properties { _MainTex ("Drawing", 2D) = "black" {} _Color ("Chalk", Color) = (0.9,0.88,0.8,1) }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 Offset -1, -1
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float4 _MainTex_ST; half4 _Color;
 CBUFFER_END
 struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
 struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
 V vert(A v) { V o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw; return o; }
 half4 frag(V i):SV_Target {
 half3 rgb=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
 half mask=smoothstep(0.12,0.6,max(rgb.r,max(rgb.g,rgb.b)));
 return half4(rgb*_Color.rgb,mask*_Color.a);
 }
 ENDHLSL
 }
 }
}