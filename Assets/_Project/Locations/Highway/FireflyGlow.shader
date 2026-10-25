Shader "Memories/FireflyGlow"
{
    Properties
    {
        _Brightness ("Brightness", Float) = 0
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Brightness;
                float4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 offset = input.uv * 2 - 1;
                float radiusSquared = dot(offset, offset);
                // A tiny luminous core inside a broad halo, with no visible quad edge.
                float core = exp(-radiusSquared * 95);
                float halo = exp(-radiusSquared * 6) * 0.35;
                float edge = 1 - smoothstep(0.55, 1, radiusSquared);
                float alpha = (core + halo) * edge * input.color.a;
                return half4(input.color.rgb * _Tint.rgb * _Brightness, alpha);
            }
            ENDHLSL
        }
    }
}
