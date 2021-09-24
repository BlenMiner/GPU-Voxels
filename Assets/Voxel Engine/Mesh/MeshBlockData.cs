using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace VoxelEngine
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct QuadData
    {
        public const int size = sizeof(int) + sizeof(int) + sizeof(float) * 3;

        public int blockFaceMask;

        public int blockUVs;

        public float3 position;

        public override string ToString()
        {
            return $"{blockFaceMask}, {position}, {blockUVs}";
        }
    }

    public static class MeshBlockDataExtension
    {
        public static IEnumerable<Enum> GetFlags(this Enum e)
        {
            return Enum.GetValues(e.GetType()).Cast<Enum>().Where(e.HasFlag);
        }

        public static int NumberOfSetBits(this int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }
    }
}