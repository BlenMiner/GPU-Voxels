using System.Runtime.InteropServices;

namespace VoxelEngine
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Block
    {
        public int id;

        public int uv;

        public Block(string name, int uv)
        {
            //this.name = name;
            this.id = -1;
            this.uv = uv;
        }

        public const int SIZEOF = sizeof(int) * 2;
    }
}