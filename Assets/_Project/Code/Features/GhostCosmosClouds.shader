Shader "Memories/Ghost Cosmos Clouds"{
	Properties {
		[Hdr]_Color ("Color", Color) = (3, 5, 6, 3)
		[Header(Drifting Clouds)]
		_CloudSpeed ("Cloud drift speed", Float) = 0.006
		_CloudScale ("Cloud scale", Float) = 1.2
		_CloudOpacity ("Cloud opacity", Range(0,1)) = 0.8
		[Header(Blue Nebula)]
        _BlueSpeed ("Blue motion speed", Float) = 0.12
        _BlueDistort ("Blue deformation", Range(0,0.3)) = 0.1
        _BlueIntensity ("Blue brightness", Range(0,2)) = 1
        _BlueColor ("Blue tint", Color) = (0.18,0.78,1.2,1)
        [Header(Red Nebula)]
        _RedSpeed ("Red motion speed", Float) = 0.08
        _RedDistort ("Red deformation", Range(0,0.3)) = 0.08
        _RedIntensity ("Red brightness", Range(0,2)) = 1
        _RedColor ("Red tint", Color) = (0.9,0.12,0.48,1)
        _RedShadowColor ("Red gradient dark violet", Color) = (0.12,0.025,0.28,1)
        [Header(Main Texture)]
		[NoScaleOffset]_MainTex ("", Cube) = "white" {}
		_Colorize ("Colorize", Range(0,2)) = 0.6

		[Space(20)]
		[Toggle(ANIM_ON)]_AnimOn ("Animation", FLOAT) = 1
		[Space(20)]
		[Header(Detail 1)]
		[NoScaleOffset]_Detail1 ("", 2D) = "white" {}
		_D1I ("Intensity", Range(0,2)) = 1
		_D1Scale ("Scale", FLOAT) = 2
		_D1Distort ("Distortion", FLOAT) = 0.1
        _D1Speed ("Speed", FLOAT) = 5
		[Space(20)]
		[Header(Detail 2)]
		[NoScaleOffset]_Detail2 ("", 2D) = "white" {}
		_D2I ("Star brightness", Range(0,8)) = 0.8
		_D2Scale ("Scale", FLOAT) = 5
		_D2Distort ("Distortion", FLOAT) = 0.1
        _D2Speed ("Speed", FLOAT) = 3
		[Space(20)]
		[Toggle(ROT_ON)]_RotOn ("Rotation Enabled", FLOAT) = 1
		_Rotation ("Rotation", VECTOR) = (0,0,0,0)
	}

	SubShader {
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off ZWrite Off Fog { Mode Off }

		Pass {
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature __ ANIM_ON
			#pragma shader_feature __ ROT_ON

			#include "UnityCG.cginc"
			
			struct appdata_t {
				float4 vertex : POSITION;
				float3 texcoord : TEXCOORD0;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : POSITION;
				float3 texcoord : TEXCOORD0;
				#if !defined(ANIM_ON)
				float3 texcoord1 : TEXCOORD1;
				float3 texcoord2 : TEXCOORD2;
				#endif
            	UNITY_VERTEX_OUTPUT_STEREO
			};

			float3 _Rotation;

			float2 rotate(float2 v, float a){
				float cs = cos(a);
				float sn = sin(a);
				return float2(
					v.x * cs - v.y * sn,
					v.x * sn + v.y * cs
				);
			}
			
			fixed4 _Color;
            float _CloudSpeed, _CloudScale, _CloudOpacity;
            float _BlueSpeed, _BlueDistort, _BlueIntensity;
            float _RedSpeed, _RedDistort, _RedIntensity;
            float4 _BlueColor, _RedColor, _RedShadowColor;
			samplerCUBE _MainTex;
			half _Colorize;
			sampler2D _Detail1;
			half _D1I;
			half _D1Scale;
			sampler2D _Detail2;
			half _D2I;
			half _D2Scale;

			#ifdef ANIM_ON
			half _D1Speed;
			half _D1Distort;
			half _D2Speed;
			half _D2Distort;
			#endif

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				#ifdef ROT_ON
				v.texcoord.yz = rotate(v.texcoord.yz, _Rotation.x/57.29578);
				v.texcoord.zx = rotate(v.texcoord.zx, _Rotation.y/57.29578);
				v.texcoord.xy = rotate(v.texcoord.xy, _Rotation.z/57.29578);
				#endif
				o.texcoord = v.texcoord;
				#if !defined(ANIM_ON)
				o.texcoord1 = v.texcoord * _D1Scale;
				o.texcoord2 = v.texcoord * _D2Scale;
				#endif
				return o;
			}

			fixed4 triplanar(sampler2D tex, float3 coord, half scale){
				fixed4 c = 
				     tex2D(tex, coord.xy * scale) * abs(coord.z);
				c += tex2D(tex, coord.yz * scale) * abs(coord.x);
				c += tex2D(tex, coord.zx * scale) * abs(coord.y);
				return c/(abs(coord.x) + abs(coord.y) + abs(coord.z));
			}
			fixed4 triplanar(sampler2D tex, float3 coord){
				fixed4 c = 
				     tex2D(tex, coord.xy) * abs(coord.z);
				c += tex2D(tex, coord.yz) * abs(coord.x);
				c += tex2D(tex, coord.zx) * abs(coord.y);
				return c/(abs(coord.x) + abs(coord.y) + abs(coord.z));
			}

			fixed4 saturate(fixed4 col,half sat){
				return lerp(
					(col.r + col.g + col.b)/3,
					col, sat
				);
			}

			fixed4 frag (v2f i) : COLOR
			{
				// Independent sampling coordinates and deformation for each nebula layer.
                float3 direction = normalize(i.texcoord);
                float3 blueDirection = direction;
                float3 redDirection = direction;
                #ifdef ANIM_ON
                float blueTime = _Time.y * _BlueSpeed;
                float redTime = _Time.y * _RedSpeed;
                float3 blueFlow = sin(direction.yzx * 7 + blueTime * float3(0.7, -0.6, 0.5))
                    * cos(direction.zxy * 5 + blueTime * float3(-0.4, 0.5, 0.6));
                float3 redFlow = sin(direction.zxy * 6 + redTime * float3(-0.5, 0.7, -0.4) + 2.3)
                    * cos(direction.yzx * 4 + redTime * float3(0.6, -0.3, 0.5) + 1.1);
                blueDirection = normalize(direction + blueFlow * _BlueDistort);
                redDirection = normalize(direction + redFlow * _RedDistort);
                #endif
                float3 blueSample = texCUBE(_MainTex, blueDirection).rgb;
                float3 redSample = texCUBE(_MainTex, redDirection).rgb;
                float cold = blueSample.b * 0.8 + blueSample.g * 0.65;
                float warm = max(0, redSample.r - redSample.b * 0.65);
                float blueDetail = triplanar(_Detail1, blueDirection, _D1Scale).r;
                float redDetail = triplanar(_Detail1, redDirection, _D1Scale).r;
                float3 redTint = lerp(_RedShadowColor.rgb, _RedColor.rgb, smoothstep(0.03, 0.4, warm));
                float3 nebula = cold * _BlueColor.rgb * _BlueIntensity * lerp(1, blueDetail, _D1I)
                    + warm * redTint * _RedIntensity * lerp(1, redDetail, _D1I);
                // Stars stay fixed in space; only brightness changes slightly.
                float starSample = triplanar(_Detail2, direction, _D2Scale).r;
                float stars = pow(starSample, 2.5) * _D2I;
                float twinkle = 1 + 0.035 * sin(_Time.y * 0.65 + dot(direction, float3(71, 43, 97)));
                fixed4 background = fixed4((nebula + stars * twinkle * float3(0.8, 0.9, 1)) * _Color.rgb, 1);

                float2 cloudUV = direction.xz / max(0.01, direction.y) * _CloudScale;
                float2 drift = float2(1, 0.28) * _Time.y * _CloudSpeed;
                float broad = tex2D(_Detail1, cloudUV + drift).r;
                float fine = tex2D(_Detail1, cloudUV * 2.13 + drift * 0.72 + 0.37).r;
                float density = smoothstep(0.45, 0.72, broad * 0.8 + fine * 0.2);
                float opacity = density * smoothstep(0.08, 0.35, direction.y) * _CloudOpacity;
                float3 cloudColor = background.rgb * lerp(0.12, 0.25, fine);
                return fixed4(lerp(background.rgb, cloudColor, opacity), 1);
            }
            ENDCG 
		}
	} 	
	Fallback Off

}

