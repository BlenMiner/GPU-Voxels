using Unity.Mathematics;

namespace VoxelEngine
{
    [System.Flags]
    public enum MeshFaceMask
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Front = 16,
        Back = 32
    }

    public static class MeshFaceMaskExtensions
    {
        public static int3 ToInt3(this MeshFaceMask mask)
        {
            switch (mask)
            {
                case MeshFaceMask.Top: return math.int3(0, 1, 0);
                case MeshFaceMask.Bottom: return math.int3(0, -1, 0);

                case MeshFaceMask.Left: return math.int3(-1, 0, 0);
                case MeshFaceMask.Right: return math.int3(1, 0, 0);

                case MeshFaceMask.Front: return math.int3(0, 0, 1);
                case MeshFaceMask.Back: return math.int3(0, 0, -1);

                default: return math.int3(0, 0, 0);
            }
        }

        public static float3 ToFloat3(this MeshFaceMask mask)
        {
            switch (mask)
            {
                case MeshFaceMask.Top: return math.float3(0f, 1f, 0f);
                case MeshFaceMask.Bottom: return math.float3(0f, -1f, 0f);

                case MeshFaceMask.Left: return math.float3(-1f, 0f, 0f);
                case MeshFaceMask.Right: return math.float3(1f, 0f, 0f);

                case MeshFaceMask.Front: return math.float3(0f, 0f, 1f);
                case MeshFaceMask.Back: return math.float3(0f, 0f, -1f);

                default: return math.float3(0f, 0f, 0f);
            }
        }
    }
}