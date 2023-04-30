Shader "Hidden/CompositeEffects" {
    
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader {
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "UnityStandardBRDF.cginc"

        sampler2D _MainTex;
        texture2D _CameraDepthTexture;
        SamplerState point_clamp_sampler;
        SamplerState linear_clamp_sampler;
        float4 _MainTex_TexelSize;
        float4 _MainTex_ST;

        struct VertexData {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vp(VertexData v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }
        ENDCG

        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                return _CameraDepthTexture.Sample(point_clamp_sampler, i.uv).r;
            }

            ENDCG
        }

        // 9-Tap Catmull-Rom filtering from: https://gist.github.com/TheRealMJP/c83b8c0f46b63f3a88a5986f4fa982b1
        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float2 samplePos = i.uv * _MainTex_TexelSize.zw;
                float2 texPos1 = floor(samplePos - 0.5f) + 0.5f;

                float2 f = samplePos - texPos1;

                float2 w0 = f * (-0.5f + f * (1.0f - 0.5f * f));
                float2 w1 = 1.0f + f * f * (-2.5f + 1.5f * f);
                float2 w2 = f * (0.5f + f * (2.0f - 1.5f * f));
                float2 w3 = f * f * (-0.5f + 0.5f * f);

                float2 w12 = w1 + w2;
                float2 offset12 = w2 / (w1 + w2);

                float2 texPos0 = texPos1 - 1;
                float2 texPos3 = texPos1 + 2;
                float2 texPos12 = texPos1 + offset12;

                texPos0 /= _MainTex_TexelSize.zw;
                texPos3 /= _MainTex_TexelSize.zw;
                texPos12 /= _MainTex_TexelSize.zw;

                float4 result = 0.0f;
                result += tex2D(_MainTex, float2(texPos0.x, texPos0.y)) * w0.x * w0.y;
                result += tex2D(_MainTex, float2(texPos12.x, texPos0.y)) * w12.x * w0.y;
                result += tex2D(_MainTex, float2(texPos3.x, texPos0.y)) * w3.x * w0.y;

                result += tex2D(_MainTex, float2(texPos0.x, texPos12.y)) * w0.x * w12.y;
                result += tex2D(_MainTex, float2(texPos12.x, texPos12.y)) * w12.x * w12.y;
                result += tex2D(_MainTex, float2(texPos3.x, texPos12.y)) * w3.x * w12.y;

                result += tex2D(_MainTex, float2(texPos0.x, texPos3.y)) * w0.x * w3.y;
                result += tex2D(_MainTex, float2(texPos12.x, texPos3.y)) * w12.x * w3.y;
                result += tex2D(_MainTex, float2(texPos3.x, texPos3.y)) * w3.x * w3.y;

                return result;
            }

            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            sampler2D _SmokeTex, _SmokeMaskTex;
            Texture2D _DepthTex;
            int _DebugView;
            float _Sharpness;

            float4 fp(v2f i) : SV_Target {
                float4 col = tex2D(_MainTex, i.uv);
                float4 smokeAlbedo = tex2D(_SmokeTex, i.uv);
                float smokeMask = saturate(tex2D(_SmokeMaskTex, i.uv).r);

                //Apply Sharpness
                float neighbor = _Sharpness * -1;
                float center = _Sharpness * 4 + 1;

                float4 n = tex2D(_SmokeTex, i.uv + _MainTex_TexelSize.xy * float2(0, 1));
                float4 e = tex2D(_SmokeTex, i.uv + _MainTex_TexelSize.xy * float2(1, 0));
                float4 s = tex2D(_SmokeTex, i.uv + _MainTex_TexelSize.xy * float2(0, -1));
                float4 w = tex2D(_SmokeTex, i.uv + _MainTex_TexelSize.xy * float2(-1, 0));

                float4 sharpenedSmoke = n * neighbor + e * neighbor + smokeAlbedo * center + s * neighbor + w * neighbor;

                switch (_DebugView) {
                    case 0:
                        //return col + smokeAlbedo;
                        return lerp(col, saturate(sharpenedSmoke), 1 - smokeMask);
                    case 1:
                        return saturate(sharpenedSmoke);
                    case 2:
                        return 1 - smokeMask;
                    case 3:
                        return _DepthTex.Sample(point_clamp_sampler, i.uv);
                }

                return float4(1, 0, 1, 0);
            }

            ENDCG
        }
    }
}