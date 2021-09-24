using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public struct VoxelRaycastHit
    {
        public int3 hitPoint;

        public float3 brokenNormal;

        public int3 localBlockPosition;

        public uint blockId;

        public VoxelChunk chunk;
    }

    public class VoxelRaycaster
    {
        public static bool Raycast(VoxelWorld world, float3 rayPos, float3 rayDir, out VoxelRaycastHit rayHit)
        {
            rayHit = new VoxelRaycastHit();
            int3 chunkPosition = math.int3(math.floor(rayPos / VoxelChunk.CHUNK_SIZE)) * VoxelChunk.CHUNK_SIZE;

            if (world.VoxelCollection.Chunks.TryGetValue(chunkPosition, out var chunk))
            {
                var mapPos = math.int3(math.floor(rayPos));

                float rayDirLen = math.length(rayDir);

                float3 deltaDist = math.abs(math.float3(rayDirLen, rayDirLen, rayDirLen) / rayDir);
                
                int3 rayStep = math.int3(math.sign(rayDir));

                float3 sideDist = (math.sign(rayDir) * (math.float3(mapPos) - rayPos) + (math.sign(rayDir) * 0.5f) + 0.5f) * deltaDist; 
                
                bool3 mask = math.bool3(false, false, false);

                for (int i = 0; i < 50; i++) 
                {
                    if (chunk.IsVoxel(mapPos, out var blockChunk, out var blockId))
                    {
                        int3 chunkPos = math.int3(blockChunk.LocalToWorld.MultiplyPoint(Vector3.zero));
                        rayHit.localBlockPosition = mapPos - chunkPos;
                        rayHit.chunk = blockChunk;
                        rayHit.hitPoint = mapPos;
                        rayHit.blockId = blockId;
                        rayHit.brokenNormal = math.float3((mask.x ? 1 : -1), (mask.y ? 1 : -1), (mask.z ? 1 : -1));
                        return true;
                    }

                    mask = sideDist.xyz <= math.min(sideDist.yzx, sideDist.zxy);		
                    
                    sideDist += math.float3(mask) * deltaDist;
                    mapPos += math.int3(math.float3(mask)) * rayStep;
                }

                return false;
            }
            else 
                return false;
        }
    }
}
