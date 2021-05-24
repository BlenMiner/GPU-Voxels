using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

public static class IntExtensions
{
    public static int3 VoxelToCoords(this int idx)
    {
        int z = idx / GPUChunk.SIZE_2;
        idx -= z * GPUChunk.SIZE_2;

        int y = idx / GPUChunk.SIZE;
        int x = idx % GPUChunk.SIZE;

        return new int3(x, y, z);
    }

    public static int VoxelToIndex(this int3 v)
    {
        return (v.z * GPUChunk.SIZE_2) + (v.y * GPUChunk.SIZE) + v.x;
    }

    public static Vector3Int ToChunkPosition(this Vector3 v)
    {
        return Vector3Int.FloorToInt(v / GPUChunk.SIZE) * GPUChunk.SIZE;
    }

    public static bool ElapsedMs(this Stopwatch v, long ms)
    {
        return v.ElapsedMilliseconds >= ms;
    }
}
