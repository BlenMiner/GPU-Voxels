using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GPUMemoryTracker
{
    static HashSet<ComputeBuffer> m_buffers = new HashSet<ComputeBuffer>();

    public static void Register(ComputeBuffer buffer)
    {
        m_buffers.Add(buffer);
    }

    public static long Count
    {
        get
        {
            long total = 0;

            m_buffers.Remove(null);

            foreach(var b in m_buffers)
            {
                if (b.IsValid()) total += (long)(b.count * b.stride);
            }

            return total;
        }
    }
}
