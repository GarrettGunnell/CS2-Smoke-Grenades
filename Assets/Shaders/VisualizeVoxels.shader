Shader "Hidden/VisualizeVoxels" {
	SubShader {

		Pass {
			CGPROGRAM

			#pragma vertex vp
			#pragma fragment fp

			#include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"

            StructuredBuffer<int> _Voxels;
            float3 _VoxelResolution, _BoundsExtent;
            float _VoxelSize;

			struct VertexData {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
                float3 hashCol : TEXCOORD0;
				float3 normal : TEXCOORD1;
			};

            float hash(uint n) {
                // integer hash copied from Hugo Elias
                n = (n << 13U) ^ n;
                n = n * (n * n * 15731U + 0x789221U) + 0x1376312589U;
                return float(n & uint(0x7fffffffU)) / float(0x7fffffff);
            }

			v2f vp(VertexData v, uint instanceID : SV_INSTANCEID) {
				v2f i;

                uint x = instanceID % (_VoxelResolution.x);
                uint y = (instanceID / _VoxelResolution.x) % _VoxelResolution.y;
                uint z = instanceID / (_VoxelResolution.x * _VoxelResolution.y);


				i.pos = UnityObjectToClipPos((v.vertex + float3(x, y, z)) * _VoxelSize + (_VoxelSize * 0.5f) - _BoundsExtent);
                i.pos *= _Voxels[instanceID];
				i.normal = UnityObjectToWorldNormal(v.normal);
                i.hashCol = float3(hash(instanceID), hash(instanceID * 2), hash(instanceID * 3));

				return i;
			}

			float4 fp(v2f i) : SV_TARGET {
                float ndotl = DotClamped(_WorldSpaceLightPos0.xyz, i.normal) * 0.5f + 0.5f;
                ndotl *= ndotl;

				return ndotl;
			}

			ENDCG
		}
	}
}