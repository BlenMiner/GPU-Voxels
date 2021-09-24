using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine
{
    public struct VoxelMapBaker : IJobParallelFor
    {
        [ReadOnly] public int3 m_size;

        [ReadOnly] public Matrix4x4 LocalToWorld;

        [WriteOnly] public NativeArray<uint> m_map;

        public void Execute(int index)
        {
            int3 pos = VoxelMap.to3D(index, m_size);
            int3 worldPos = math.int3(LocalToWorld.MultiplyPoint((float3)pos));

            float heightmap = noise.snoise(math.float2(worldPos.x * 0.01f, worldPos.z * 0.01f));
            int worldHeight = (int)((heightmap + 1) * 64);

            m_map[index] = worldPos.y < worldHeight ? Blocks.BLOCK_DIRT : Blocks.BLOCK_AIR;
        }
    }
}