Shader "Custom/Dummy" {

	Properties {
		_Albedo1 ("Albedo 1", Color) = (1, 1, 1, 1)
		_Albedo2 ("Albedo 2", Color) = (1, 1, 1, 1)

        _Tiling ("Tiling", Vector) = (1, 1, 1, 1)
	}

	SubShader {

		Pass {
			Tags {
				"RenderType" = "Opaque"
                "LightMode" = "ForwardBase"
			}

			CGPROGRAM

			#pragma vertex vp
			#pragma fragment fp

            #pragma multi_compile _ SHADOWS_SCREEN

			#include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"

			float4 _Albedo1, _Albedo2, _Tiling;

			struct VertexData {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 normal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
                SHADOW_COORDS(3)
			};

			v2f vp(VertexData v) {
				v2f i;
				i.pos = UnityObjectToClipPos(v.vertex);
				i.worldPos = mul(unity_ObjectToWorld, v.vertex);
				i.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(i);

				return i;
			}

			float4 fp(v2f i) : SV_TARGET {
				float tile = cos(i.worldPos.x * _Tiling.x) * sin(i.worldPos.z * _Tiling.z) * cos(i.worldPos.y * _Tiling.y);
                float4 col = lerp(_Albedo1, _Albedo2, step(0, tile).x);

                float ndotl = DotClamped(_WorldSpaceLightPos0.xyz, i.normal) * 0.5f + 0.5f;
                ndotl *= ndotl;

                return lerp(col * 0.25, col * ndotl, SHADOW_ATTENUATION(i));
			}

			ENDCG
		}

		Pass {
            Tags {
				"LightMode" = "ShadowCaster"
			}
 
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            #include "UnityCG.cginc"
 
			struct VertexData {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

            struct v2f {
                float4 pos : SV_POSITION;
            };
 
            v2f vp(VertexData v) {
                v2f o;

				o.pos = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
				o.pos = UnityApplyLinearShadowBias(o.pos);

                return o;
            }
 
            float4 fp(v2f i) : SV_Target {
                return 0;
            }

            ENDCG
        }
	}
}