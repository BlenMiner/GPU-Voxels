Shader "Unlit/VoxelShaderLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        CGPROGRAM
        #include "UnityCG.cginc"
        
        #pragma target 5.0
        #pragma surface surf Lambert vertex:vert addshadow 

        struct FaceData
        {
            int blockFaceMask;
            int blockUVs;
            float3 position;
        };
        
        sampler2D _MainTex;

        #define BLOCK_UV_SIZE 0.0625

        #ifdef SHADER_API_D3D11
        StructuredBuffer<FaceData> _Faces;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(int, m_size_x)
        UNITY_DEFINE_INSTANCED_PROP(int, m_size_y)
        UNITY_DEFINE_INSTANCED_PROP(int, m_size_z)
        UNITY_DEFINE_INSTANCED_PROP(float3, m_worldPos)
        UNITY_INSTANCING_BUFFER_END(Props)

        #include "VoxelMapBuffers.cginc"
        #include "VoxelMapBuffersFunctionsShader.cginc"
        #endif

        static const uint M_Top = 1u;
        static const uint M_Bottom = 2u;
        static const uint M_Left = 4u;
        static const uint M_Right = 8u;
        static const uint M_Front = 16u;
        static const uint M_Back = 32u;

        float4 GetOffset(uint vertexIndex)
        {
            switch(vertexIndex)
            {
                case 1:
                    return float4(0, 1, 0, 0);
                case 4:
                case 2:
                    return float4(1, 1, 0, 0);
                case 5:
                    return float4(1, 0, 0, 0);
                default:
                    return float4(0, 0, 0, 0);
            }
        }

        float4 GetOffsetInverted(uint vertexIndex)
        {
            switch(vertexIndex)
            { 
                case 2:
                    return float4(0, 1, 0, 0);
                case 5:
                case 1:
                    return float4(1, 1, 0, 0);
                case 4:
                    return float4(1, 0, 0, 0);
                default:
                    return float4(0, 0, 0, 0);
            }
        }

        int GetUV(int value, int pos)
        {
            value >>= (pos * 4);
            return value & 0xF;
        }

        float4 TransformUV(float4 uv, int blockUVs, int3 normal)
        {
            float4 offset = float4(0, 0, 0, 0);
            
            if (normal.y > 0)
            {
                offset.x = GetUV(blockUVs, 0);
                offset.y = GetUV(blockUVs, 1);
            }
            else if (normal.y < 0)
            {
                offset.x = GetUV(blockUVs, 2);
                offset.y = GetUV(blockUVs, 3);
            }
            else if (normal.z > 0)
            {
                offset.x = GetUV(blockUVs, 6);
                offset.y = GetUV(blockUVs, 7);
            }
            else
            {
                offset.x = GetUV(blockUVs, 4);
                offset.y = GetUV(blockUVs, 5);
            }



            uv *= BLOCK_UV_SIZE;
            uv.y = 1 - uv.y;

            return (offset / 16.0) + uv;
        }

        float4 GetVertex(FaceData face, uint vertexIndex, out float4 uv, out float3 normal)
        {
            float4 pos = float4(face.position.xyz, 1);

            switch(face.blockFaceMask)
            {
                case M_Top:
                    ++pos.y;
                    uv = GetOffset(vertexIndex);
                    pos.xz += uv.xy;
                    normal = float3(0, 1, 0);
                    break;
                case M_Bottom:
                    uv = GetOffsetInverted(vertexIndex);
                    pos.xz += uv.xy;
                    normal = float3(0, -1, 0);
                    break;

                case M_Left:
                    uv = GetOffset(vertexIndex);
                    pos.yz += uv.xy;
                    normal = float3(-1, 0, 0);

                    uv = float4(uv.y, 1 - uv.x, 0, 0);
                    break;

                case M_Right:
                    ++pos.x;
                    uv = GetOffsetInverted(vertexIndex);
                    pos.yz += uv.xy;
                    normal = float3(1, 0, 0);
                    uv = float4(uv.y, 1 - uv.x, 0, 0);
                    break;

                case M_Front:
                    ++pos.z;
                    uv = GetOffsetInverted(vertexIndex);
                    pos.xy += uv.xy;
                    normal = float3(0, 0, 1);
                    uv.y = 1 - uv.y;
                    break;

                case M_Back:
                    uv = GetOffset(vertexIndex);
                    pos.xy += uv.xy;
                    normal = float3(0, 0, -1);
                    uv.y = 1 - uv.y;
                    break;

                default: break;
            }

            uv = TransformUV(uv, face.blockUVs, normal);
            return pos;
        }

        #ifdef SHADER_API_D3D11

        uint getVoxel(float3 c)
        {
            int3 v = floor(c);

            return GetBlock(v);
        }

        bool isVoxel(int3 v)
        {
            return GetBlock(v) != 0;
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

        #endif

        struct appdata{
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 color : COLOR;
 
            uint id : SV_VertexID;
            uint instanceID : SV_InstanceID;
        };

        struct Input 
        {
            float4 color : COLOR;
            float3 worldPos;
            float2 uv_MainTex : TEXCOORD0;
            float3 localpos;
            float3 cWorldPos;
        };

        void vert (inout appdata v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            #ifdef SHADER_API_D3D11

            FaceData face = _Faces[v.id / 6];
 
            v.vertex = GetVertex(face, v.id % 6, v.texcoord, v.normal);

            o.localpos = (v.vertex - UNITY_ACCESS_INSTANCED_PROP(Props, m_worldPos));
            o.cWorldPos = v.vertex;
            #endif
        }

        #ifdef SHADER_API_D3D11

        bool3 lessThanEqual(float3 a, float3 b)
        {
            return bool3(a.x <= b.x, a.y <= b.y, a.z <= b.z);
        }

        #define MAX_RAY_STEPS 32

        bool raycast(float3 rayPos, float3 rayDir, out float3 mapPos, out float3 normal)
        {
            mapPos = int3(floor(rayPos));

            float rayDirLen = length(rayDir);

            float3 deltaDist = abs(float3(rayDirLen, rayDirLen, rayDirLen) / rayDir);
            
            int3 rayStep = int3(sign(rayDir));

            float3 sideDist = (sign(rayDir) * (float3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist; 
            
            bool3 mask;

            bool hit = false;
            
            for (int i = 0; i < MAX_RAY_STEPS; i++) {
                if (isVoxel(mapPos))
                {
                    hit = true;
                    continue;
                }

                mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));		
                
                sideDist += float3(mask) * deltaDist;
                mapPos += int3(float3(mask)) * rayStep;
            }

            normal = float3((mask.x ? 1 : -1), (mask.y ? 1 : -1), (mask.z ? 1 : -1));

            return hit;
        }

        #endif

        void surf (Input IN, inout SurfaceOutput o) 
        {
            float2 uv = IN.uv_MainTex;

            float3 color = tex2D(_MainTex, uv).rgb;

            #ifdef SHADER_API_D3D11

            float3 hitPosition = IN.localpos;
            float3 normal = o.Normal;

            uint3 mapPos = floor(hitPosition - normal * 0.5);

            int3 unormal = round(normal);
            uint3 mask = abs(unormal);

            float2 _uv = float2(dot(mask * hitPosition.yzx, float3(1, 1, 1)), dot(mask * hitPosition.zxy, float3(1, 1, 1))) % 1;
            float d = dot(float3(0, 1, 0), normal);

            uint3 aoBlock = mapPos + unormal;
            uint3 side0 = round(mask.zxy);
            uint3 side1 = round(mask.yzx);

            float4 ambient = voxelAo(aoBlock, side0, side1);
            float interpAo = lerp(lerp(ambient.z, ambient.w, _uv.x), lerp(ambient.y, ambient.x, _uv.x), _uv.y);
            interpAo = pow(interpAo, 1);

            color *= interpAo;
 
            #endif
            o.Albedo = color;
        }

        ENDCG
    }
}
