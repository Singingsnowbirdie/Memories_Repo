Shader "Memories/Graffiti Ink"
{
 Properties { _MainTex ("Artwork", 2D) = "white" {} _Strength ("Ink strength", Range(0,1)) = 0.9 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
 Pass {
 Blend DstColor Zero
 ZWrite Off
 Cull Off
 Offset -1, -1
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float4 _MainTex_ST; float _Strength;
 CBUFFER_END
 struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
 struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
 V vert(A v) { V o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw; return o; }
 half4 frag(V i):SV_Target {
 half3 ink=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
 // Multiplication leaves the white paper invisible and preserves wall lighting.
 ink=saturate((ink-0.025)/0.93);
 return half4(lerp(half3(1,1,1),ink,_Strength),1);
 }
 ENDHLSL
 }
 }
}
