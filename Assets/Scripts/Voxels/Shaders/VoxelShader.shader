Shader "Unlit/VoxelShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        //_LOD("LOD", Range(0.0001, 1.0)) = 1
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            #define MAX_RAY_STEPS 64
            #define SIZE 128
            #define SIZE_2 SIZE * SIZE

            #define FRONT 0
            #define BACK 1
            #define RIGHT 2
            #define LEFT 3
            #define UP 4
            #define DOWN 5

            struct CVertex
            {
                uint data;
            };

            struct Vertex
            {
                float4 position;
                uint normal;
            };

            StructuredBuffer<CVertex> _Mesh;
            StructuredBuffer<uint> _Map;
            float4x4 objMat;

            //float _LOD;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                uint rawnormal : TEXCOORD2;
            };

            sampler2D _MainTex;

            uint getVoxel(float3 c)
            {
                uint3 v = floor(c);
                uint id = v.z * SIZE_2 + v.y * SIZE + v.x;

                return _Map[id];
            }

            uint isVoxel(uint3 v)
            {
                uint id = v.z * SIZE_2 + v.y * SIZE + v.x;
                return _Map[id] != 0;
            }

            inline float vertexAo(float2 side, float corner) {
                return (side.x + side.y + max(corner, side.x * side.y)) / 3.0;
            }

            float4 voxelAo(uint3 pos, uint3 d1, uint3 d2) {
                float4 side = float4(isVoxel(pos + d1), isVoxel(pos + d2), isVoxel(pos - d1), isVoxel(pos - d2));
                float4 corner = float4(isVoxel(pos + d1 + d2), isVoxel(pos - d1 + d2), isVoxel(pos - d1 - d2), isVoxel(pos + d1 - d2));
                float4 ao;

                ao.x = vertexAo(side.xy, corner.x);
                ao.y = vertexAo(side.yz, corner.y);
                ao.z = vertexAo(side.zw, corner.z);
                ao.w = vertexAo(side.wx, corner.w);

                return 1.0 - ao;
            }

            float zeroToOne(float x) { return x + float(!bool(x)); }

            /*void raycast() {

                vec3 rayDir = cameraDir + screenPos.x * cameraPlaneU + screenPos.y * cameraPlaneV;
                vec3 rayPos = vec3(0.0, 2.0 * sin(iTime * 2.7), -12.0);

                rayPos.xz = rotate2d(rayPos.xz, iTime);
                rayDir.xz = rotate2d(rayDir.xz, iTime);

                vec3 mapPos = vec3(floor(rayPos));

                vec3 deltaDist = abs(vec3(length(rayDir)) / rayDir);

                vec3 rayStep = sign(rayDir);

                vec3 sideDist = (sign(rayDir) * (mapPos - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;

                for (int i = 0; i < MAX_RAY_STEPS; i++) {
                    if (bool(getVoxel(mapPos))) continue;
                    mask = step(sideDist.xyz, sideDist.yzx) * step(sideDist.xyz, sideDist.zxy);
                    sideDist += mask * deltaDist;
                    mapPos += mask * rayStep;
                }
            }*/

            inline Vertex Decompress(CVertex v)
            {
                Vertex c;
                uint data = v.data;

                c.position.x = data & 0x000000FF;
                c.position.y = (data & 0x0000FF00) >> 8;
                c.position.z = (data & 0x00FF0000) >> 16;
                c.position.w = 1;
                c.normal = (data & 0x0F000000) >> 24;

                return c;
            }

            v2f vert (uint index : SV_VertexID)
            {
                v2f o;

                Vertex vert = Decompress(_Mesh[index]);

                //vert.position = floor(vert.position * _LOD) / _LOD;

                o.vertex = UnityObjectToClipPos(vert.position);
                o.worldPos = mul(unity_ObjectToWorld, vert.position);
                o.rawnormal = vert.normal;

                float3 uvmask = vert.position.xyz % 1.0f;

                switch (vert.normal)
                {
                case FRONT:
                    o.normal = float3(0, 0, 1);
                    break;
                case BACK:
                    o.normal = float3(0, 0, -1);
                    break;
                case LEFT:
                    o.normal = float3(-1, 0, 0);
                    break;
                case RIGHT:
                    o.normal = float3(1, 0, 0);
                    break;
                case UP:
                    o.normal = float3(0, 1, 0);
                    break;
                case DOWN:
                    o.normal = float3(0, -1, 0);
                    break;
                }

                return o;
            }

            float2 GetUvs(float3 pos, uint rawNormal)
            {
                float3 uvmask = frac(pos.xyz);

                switch (rawNormal)
                {
                case FRONT:
                    return float2(uvmask.x, uvmask.y);
                case BACK:
                    return float2(uvmask.x, uvmask.y);
                case LEFT:
                    return float2(uvmask.y, uvmask.z);
                case RIGHT:
                    return float2(uvmask.y, uvmask.z);
                case UP:
                    return float2(uvmask.x, uvmask.z);
                default:
                    return float2(uvmask.x, uvmask.z);
                }
            }

            fixed4 frag(v2f i) : SV_Target
            {
                uint3 mapPos = floor(i.worldPos - i.normal * 0.5);
                float3 hitPosition = i.worldPos;

                int3 unormal = round(i.normal);
                uint3 mask = abs(unormal);

                float2 uv = float2(dot(mask * hitPosition.yzx, float3(1, 1, 1)), dot(mask * hitPosition.zxy, float3(1, 1, 1))) % 1;
                float d = dot(float3(0, 1, 0), i.normal);

                uint3 aoBlock = mapPos + unormal;
                uint3 side0 = round(mask.zxy);
                uint3 side1 = round(mask.yzx);

                float4 ambient = voxelAo(aoBlock, side0, side1);
                float interpAo = lerp(lerp(ambient.z, ambient.w, uv.x), lerp(ambient.y, ambient.x, uv.x), uv.y);
                interpAo = pow(interpAo, 1.0 / 3.0);

                fixed4 col = fixed4(hitPosition / SIZE, 1);// *ambient;
                return col * interpAo;
            }
            ENDCG
        }
    }
}
