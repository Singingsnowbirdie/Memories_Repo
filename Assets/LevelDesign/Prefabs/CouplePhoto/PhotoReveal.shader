Shader "Hidden/Memories/PhotoReveal"
{
    Properties { _MainTex ("Photo", 2D) = "white" {} _Reveal ("Reveal", Range(0,1)) = 0 }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Reveal;
            fixed4 frag(v2f_img i) : SV_Target
            {
                return lerp(fixed4(1,1,1,1), tex2D(_MainTex, i.uv), _Reveal);
            }
            ENDCG
        }
    }
}
