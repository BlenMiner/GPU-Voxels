using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public static class UVPacker
    {
        private static int Add(int value, int toAdd, int pos)
        {
            value |= (toAdd & 0xF) << (pos * 4);
            return value;
        }

        private static int Get(int value, int pos)
        {
            value >>= (pos * 4);
            return value & 0xF;
        }

        public static int Pack(int2 top, int2 bottom, int2 sides, int2 front)
        {
            int result = 0;
            int pos = 0;

            result = Add(result, top.x, pos++);
            result = Add(result, top.y, pos++);

            result = Add(result, bottom.x, pos++);
            result = Add(result, bottom.y, pos++);

            result = Add(result, sides.x, pos++);
            result = Add(result, sides.y, pos++);

            result = Add(result, front.x, pos++);
            result = Add(result, front.y, pos++);

            return result;
        }

        public static void Unpack(int value, out int2 top, out int2 bottom, out int2 sides, out int2 front)
        {
            int pos = 0;

            top = math.int2(Get(value, pos++), Get(value, pos++));
            bottom = math.int2(Get(value, pos++), Get(value, pos++));
            sides = math.int2(Get(value, pos++), Get(value, pos++));
            front = math.int2(Get(value, pos++), Get(value, pos++));
            front = math.int2(Get(value, pos++), Get(value, pos++));
        }
    }
}