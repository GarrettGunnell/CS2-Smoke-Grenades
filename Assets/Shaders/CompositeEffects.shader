Shader "Hidden/CompositeEffects" {
    
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            #include "UnityCG.cginc"
			#include "UnityStandardBRDF.cginc"


            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex, _SmokeTex, _SmokeDepthTex, _SmokeMaskTex, _CameraDepthTexture;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
            int _DebugView;

            v2f vp(VertexData v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 fp(v2f i) : SV_Target {
                float4 col = tex2D(_MainTex, i.uv);
                float4 smokeAlbedo = tex2D(_SmokeTex, i.uv);
                float smokeDepth = tex2D(_SmokeDepthTex, i.uv).r;
                float smokeMask = tex2D(_SmokeMaskTex, i.uv).r;
                float polygonalDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);

                //return smoke;

                switch (_DebugView) {
                    case 0:
                        return lerp(col, smokeAlbedo, saturate(smokeMask * (polygonalDepth < smokeDepth)));
                    case 1:
                        return smokeAlbedo;
                    case 2:
                        return smokeMask;
                    case 3:
                        return smokeDepth;
                    case 4:
                        return polygonalDepth;
                }

                return float4(1, 0, 1, 0);
            }

            ENDCG
        }
    }
}