// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "Hidden/Raymarch" {
    
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

            #define M_PI 3.14159265358979323846264338327950288

            #define MAX_DIST 100
            #define SURF_DIST 0.001

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
            float _Radius;

            v2f vp(VertexData v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            #define STEPS 64
            #define STEP_SIZE 1
            #define MIN_HIT_DISTANCE 0.001
            #define MAX_DISTANCE 1000

            float GetDist(float3 pos) {
                return length(pos - 0) - _Radius;
            }

            float Raymarch(float3 rayOrigin, float3 rayDir) {
                float distance = 0.0f;

                for (int i = 0; i < STEPS; ++i) {
                    float3 pos = rayOrigin + distance * rayDir;

                    float distanceToObject = GetDist(pos);

                    if (distanceToObject < MIN_HIT_DISTANCE) return distance;
                    else if (dot(pos, float3(0, 1, 0)) + 0 < MIN_HIT_DISTANCE) return MAX_DISTANCE;
                    if (distance > MAX_DISTANCE) return MAX_DISTANCE;

                    distance += distanceToObject;
                }

                return MAX_DISTANCE;
            }

            float3 CalcNormal(float3 p, float d) {
                float2 e = float2(0.001, 0);
                float3 n = GetDist(p) - float3(
                    GetDist(p - e.xyy), 
                    GetDist(p - e.yxy), 
                    GetDist(p - e.yyx)
                    );

                return normalize(n);
            }

            float4 fp(v2f i) : SV_Target {
                float3 origin = _WorldSpaceCameraPos;
                float2 uv = 1 - i.uv;
                float3 direction = mul(unity_CameraInvProjection, float4(uv * 2 - 1, 1.0f, 1.0f)).xyz;
                direction = mul(unity_CameraToWorld, float4(direction, 1.0f)).xyz;

                float3 rayDir = normalize(origin - direction);

                float d = Raymarch(origin, rayDir);

                if (d < MAX_DISTANCE) {
                    float2 e = float2(0.001f, 0);
                    float3 n = CalcNormal(origin + rayDir * d, d);

                    float ndotl = DotClamped(_WorldSpaceLightPos0.xyz, n) * 0.5f + 0.5f;
                    ndotl *= ndotl;

                    return ndotl;
                } else {
                    return tex2D(_MainTex, i.uv);
                }
            }

            ENDCG
        }
    }
}