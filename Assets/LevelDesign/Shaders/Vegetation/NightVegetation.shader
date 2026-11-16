Shader "Memories/Night Vegetation"
{
 Properties
 {
  _BaseMap("Texture",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
  [Toggle] _UseGradient("Use original foliage gradient",Float)=0
  _GroundColor("Ground tint",Color)=(1,1,1,1)
  _TopColor("Top tint",Color)=(1,1,1,1)
  _Gradient("Gradient",Float)=1
  _Cutoff("Alpha cutoff",Range(0,1))=0.5
  [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=0
  [Toggle] _NeutralTexture("Neutral texture, separate tint",Float)=0
  _TextureLuminanceScale("Texture detail normalization",Float)=1
  _CanopyOcclusion("Leaf underside shading",Range(0,1))=0.65
  [Toggle] _WindEnabled("Wind",Float)=0
  _WindMovement("Wind movement",Float)=0.5
  _WindDensity("Wind density",Float)=3.3
  _WindStrength("Wind strength",Float)=0.3
  _WindMask("UV wind mask",Float)=0
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull [_Cull]
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
  float4 _BaseMap_ST, _BaseColor, _GroundColor, _TopColor;
  float _UseGradient,_Gradient,_Cutoff,_Cull,_NeutralTexture,_TextureLuminanceScale,_CanopyOcclusion;
  float _WindEnabled,_WindMovement,_WindDensity,_WindStrength,_WindMask;
  CBUFFER_END
  float _MemoriesVegetationWindPaused;
  struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;float height:TEXCOORD3;half fog:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
  // Same simplex noise and displacement used by the source vegetation shader.
  float3 Mod289(float3 x){return x-floor(x/289.0)*289.0;}
  float2 Mod289(float2 x){return x-floor(x/289.0)*289.0;}
  float3 Permute(float3 x){return Mod289((x*34.0+1.0)*x);}
  float Noise(float2 v){
   const float4 C=float4(.211324865405187,.366025403784439,-.577350269189626,.024390243902439);
   float2 i=floor(v+dot(v,C.yy));float2 x0=v-i+dot(i,C.xx);
   float2 i1=x0.x>x0.y?float2(1,0):float2(0,1);float4 x12=x0.xyxy+C.xxzz;x12.xy-=i1;i=Mod289(i);
   float3 p=Permute(Permute(i.y+float3(0,i1.y,1))+i.x+float3(0,i1.x,1));
   float3 m=max(.5-float3(dot(x0,x0),dot(x12.xy,x12.xy),dot(x12.zw,x12.zw)),0);m=m*m;m=m*m;
   float3 x=2*frac(p*C.www)-1;float3 h=abs(x)-.5;float3 a=x-floor(x+.5);m*=1.79284291400159-.85373472095314*(a*a+h*h);
   float3 g;g.x=a.x*x0.x+h.x*x0.y;g.yz=a.yz*x12.xz+h.yz*x12.yw;return 130*dot(m,g);
  }
  float3 Position(Attributes v){float3 p=v.positionOS.xyz;
   [branch] if (_MemoriesVegetationWindPaused > 0.5 || _WindEnabled < 0.5) return p;
   float mask=lerp(p.y,(1-v.uv.y)*5,_WindMask);p.x+=Noise((p.xy+_Time.y*_WindMovement)*_WindDensity)*.05*_WindStrength*mask*_WindEnabled;return p;}
  Varyings Vert(Attributes v){Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   VertexPositionInputs p=GetVertexPositionInputs(Position(v));o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.height=v.positionOS.y;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
  half4 Surface(Varyings i){half4 t=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);clip(t.a*_BaseColor.a-_Cutoff);half3 gradient=lerp(_GroundColor.rgb,_TopColor.rgb,clamp((.5+i.height*1.5)*_Gradient,-1,1));half detail=saturate(dot(t.rgb,half3(.2126,.7152,.0722))*_TextureLuminanceScale); half3 tint=lerp(_BaseColor.rgb,saturate(gradient),_UseGradient); half3 original=lerp(t.rgb*_BaseColor.rgb,saturate(gradient),_UseGradient); return half4(lerp(original,detail*tint,_NeutralTexture),1);}
  half3 Diffuse(Light l,half3 n){return l.color*(l.distanceAttenuation*l.shadowAttenuation*saturate(dot(n,l.direction)));}
  ENDHLSL
  Pass
  {
   Name "ForwardLit" Tags{"LightMode"="UniversalForwardOnly"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   half4 Frag(Varyings i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target{
    UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half3 albedo=Surface(i).rgb;half3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
    InputData inputData=(InputData)0;inputData.positionWS=i.positionWS;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
    half3 illumination=Diffuse(GetMainLight(TransformWorldToShadowCoord(i.positionWS)),n);
    #if USE_CLUSTER_LIGHT_LOOP
    UNITY_LOOP for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++){CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK illumination+=Diffuse(GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1)),n);}
    #endif
    uint count=GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(count)
     illumination+=Diffuse(GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1)),n);
    LIGHT_LOOP_END
    // Use the scene's diffuse probes and lights without vegetation-only exposure limits.
    // Approximate sky visibility from the surface orientation, not renderer bounds.
    // Bounds change across LODs and static batches; material shading must not.
    half skyVisibility=lerp(1-_CanopyOcclusion,1,saturate(n.y*.5+.5));
    half3 sky=max(SampleSH(n),0)*lerp(1,skyVisibility,_NeutralTexture);
    return half4(MixFog(albedo*(illumination+sky),i.fog),1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex ShadowVert
   #pragma fragment DepthFrag
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 _LightDirection;float3 _LightPosition;
   Varyings ShadowVert(Attributes v){Varyings o=Vert(v);float3 direction=_LightDirection;
   #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    direction=normalize(_LightPosition-o.positionWS);
   #endif
    o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction));
   #if UNITY_REVERSED_Z
    o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #else
    o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #endif
    return o;}
   half4 DepthFrag(Varyings i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);Surface(i);return 0;}
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment DepthFrag
   #pragma multi_compile_instancing
   half4 DepthFrag(Varyings i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);Surface(i);return i.positionCS.z;}
   ENDHLSL
  }
 }
}
