Shader "Memories/TV Static"
{
    Properties
    {
        _Tint ("Screen tint", Color) = (0.86, 0.93, 1, 1)
        _Brightness ("Brightness", Range(0, 2)) = 0.85
        _Grain ("Grain resolution", Range(64, 640)) = 320
        _Speed ("Frames per second", Range(1, 60)) = 24
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "TVStatic"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Brightness;
                float _Grain;
                float _Speed;
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
            float Noise(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                // Match the existing glass: narrower at its bottom, with a small inset.
                float halfWidth = lerp(0.4458, 0.499, uv.y);
                clip(halfWidth - abs(uv.x - 0.5));
                float frame = floor(_Time.y * _Speed);
                float2 cell = floor(uv * float2(_Grain, _Grain * 0.83));
                float grain = Noise(float3(cell, frame));
                float scan = 0.92 + 0.08 * sin(uv.y * 1100.0);
                float band = 1.0 - 0.16 * exp(-pow((frac(uv.y + _Time.y * 0.12) - 0.5) * 25.0, 2.0));
                float2 edge = (uv - 0.5) * 2.0;
                float vignette = saturate(1.0 - 0.22 * dot(edge, edge));
                float luminance = lerp(0.025, 1.0, grain * grain) * scan * band * vignette;
                return half4(_Tint.rgb * luminance * _Brightness, 1);
            }
            ENDHLSL
        }
    }
}