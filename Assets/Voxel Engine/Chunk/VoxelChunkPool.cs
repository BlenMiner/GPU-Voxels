using System.Collections.Generic;

namespace VoxelEngine
{
    public static class VoxelChunkPool
    {
        static LinkedList<VoxelChunk> m_freeChunks = new LinkedList<VoxelChunk>();

        public static void FreeChunk(VoxelChunk c)
        {
            m_freeChunks.AddLast(c);
        }

        public static bool RecoverChunk(out VoxelChunk chunk)
        {
            if (m_freeChunks.Count > 0)
            {
                chunk = m_freeChunks.First.Value;
                m_freeChunks.RemoveFirst();
                return true;
            }

            chunk = null;
            return false;
        }

        public static void GarbageCollector()
        {
            foreach(var unused in m_freeChunks)
            {
                if (unused.TimeSinceDisabled > 5f)
                {
                    unused.Dispose();
                    m_freeChunks.Remove(unused);
                    break;
                }
            }
        }

        public static void FreeResources()
        {
            foreach(var c in m_freeChunks)
            {
                c.Dispose();
            }

            m_freeChunks.Clear();
        }
    }
}