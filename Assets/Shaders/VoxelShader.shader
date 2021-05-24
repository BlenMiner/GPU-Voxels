Shader "Unlit/VoxelShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD2;
            };

            float4x4 _InvProjectionMatrix;
            float4x4 _ViewToWorld;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.viewDir = WorldSpaceViewDir(v.vertex);

                return o;
            }

            bool getVoxel(int3 c)
            {
                return c.y < abs(sin(c.x * 0.5)) * 3;
            }

            inline int3 lessThanEqual(float3 a, float3 b)
            {
                int3 mask;
                
                mask.x = a.x <= b.x;
                mask.y = a.y <= b.y;
                mask.z = a.z <= b.z;

                return mask;
            }

            #define MAX_RAY_STEPS 64

            bool raycast(float3 rayPos, float3 rayDir, out float3 sideDist)
            {
                int3 mapPos = int3(floor(rayPos + 0.));
                float raylen = length(rayDir);
                float3 deltaDist = abs(float3(raylen, raylen, raylen) / rayDir);
                int3 rayStep = int3(sign(rayDir));
                sideDist = (sign(rayDir) * (float3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;

                bool3 mask;

                for (int i = 0; i < MAX_RAY_STEPS; i++) {
                    if (getVoxel(mapPos))
                        return true;

                    mask = lessThanEqual(sideDist, min(sideDist.yzx, sideDist.zxy));

                    sideDist += float3(mask) * deltaDist;
                    mapPos += int3(float3(mask)) * rayStep;

                    if (mapPos.x < 0 || mapPos.x >= 16 ||
                        mapPos.z < 0 || mapPos.z >= 16 ||
                        mapPos.y < 0 || mapPos.y >= 16)
                    {
                        return false;
                    }
                }

                return false;
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                float3 rayPos = facing > 0 ? i.worldPos : _WorldSpaceCameraPos.xyz;
                float3 rayDir = normalize(-mul(_ViewToWorld, float4(i.viewDir, 1)));

                float3 hitPoint;
                bool hit = raycast(rayPos, rayDir, hitPoint);

                clip(hit - 0.5);

                fixed4 col = fixed4(hitPoint.xyz / 16, 1);
                return col;
            }
            ENDCG
        }
    }
}
