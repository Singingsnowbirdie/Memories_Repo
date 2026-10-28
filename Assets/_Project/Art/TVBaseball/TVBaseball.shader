Shader "Memories/TV Baseball"
{
    Properties
    {
        _Atlas ("Three comic frames (top to bottom)", 2D) = "white" {}
        _FrameDuration ("Seconds per frame", Float) = 4
        _StartTime ("Playback start", Float) = 0
        _Brightness ("Brightness", Range(0, 2)) = 0.85
    }    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "TVBaseball"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);
            float4 _Atlas_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                float _FrameDuration;
                float _StartTime;
                float _Brightness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                // Match the existing glass: narrower at its bottom, with a small inset.
                float halfWidth = lerp(0.4458, 0.499, uv.y);
                clip(halfWidth - abs(uv.x - 0.5));
                float frame = fmod(floor(max(0, _Time.y - _StartTime) / max(0.1, _FrameDuration)), 3.0);
                float2 atlasUV = float2(uv.x, (2.0 - frame + lerp(0.002, 0.998, uv.y)) / 3.0);
                half3 color = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, atlasUV).rgb;
                float scan = 0.96 + 0.04 * sin(uv.y * 1100.0);
                return half4(color * _Brightness * scan, 1);
            }
            ENDHLSL
        }
    }
}