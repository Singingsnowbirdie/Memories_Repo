Shader "Memories/PortalView"
{
 Properties { _MainTex ("Courtyard", 2D) = "black" {} }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass
  {
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   struct Attributes { float4 positionOS : POSITION; };
   struct Varyings { float4 positionCS : SV_POSITION; float4 screenPos : TEXCOORD0; };
   Varyings vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.screenPos=ComputeScreenPos(o.positionCS); return o; }
   half4 frag(Varyings i) : SV_Target { return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.screenPos.xy/i.screenPos.w); }
   ENDHLSL
  }
 }
}