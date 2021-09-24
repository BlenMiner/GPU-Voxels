using System.Runtime.InteropServices;

namespace VoxelEngine
{
    [StructLayout(LayoutKind.Sequential)]
    struct DrawCallArgBuffer
    {
        public const int size =
            sizeof(int) +
            sizeof(int) +
            sizeof(int) +
            sizeof(int);
        public int vertexCountPerInstance;
        public int instanceCount;
        public int startVertexLocation;
        public int startInstanceLocation;
    }
}