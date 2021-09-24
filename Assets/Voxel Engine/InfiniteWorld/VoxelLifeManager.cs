using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace VoxelEngine
{
    public class VoxelLifeManager
    {
        int iterationsPerFrame;

        int2 m_viewRadius;

        ChunkCollection<VoxelChunk> m_chunks;

        public ChunkCollection<VoxelChunk> Chunks => m_chunks;

        List<int3> m_sphericalLoop = new List<int3>();

        HashSet<int3> m_sphericalLoopSet = new HashSet<int3>();

        VoxelWorld m_world;

        public VoxelLifeManager(VoxelWorld world, int2 viewRadius)
        {
            m_world = world;

            m_chunks = new ChunkCollection<VoxelChunk>();

            m_viewRadius = viewRadius;

            for (int x = -m_viewRadius.x; x < m_viewRadius.x; ++x)
            {
                for (int z = -m_viewRadius.x; z < m_viewRadius.x; ++z)
                {
                    for (int y = -m_viewRadius.y; y < m_viewRadius.y; ++y)
                    {
                        int3 pos = math.int3(x, y, z);
                        float horizontalDistance = math.length(pos.xz);
                        float verticalDistance = math.length(pos.y);

                        if (horizontalDistance < viewRadius.x && verticalDistance < viewRadius.y)
                        {
                            m_sphericalLoop.Add(pos);
                            m_sphericalLoopSet.Add(pos * VoxelChunk.CHUNK_SIZE);
                        }
                    }
                }
            }

            m_sphericalLoop.Sort((int3 a, int3 b) => {
                float alength = math.length(a);
                float blength = math.length(b);

                if (alength == blength)
                    return 0;
                else if (alength < blength)
                    return -1;
                else 
                    return 1;
            });

            iterationsPerFrame = 3;
        }

        int m_currentIndex = 0;

        List<int3> m_outsideKeys = new List<int3>();

        public void Update(float deltaTime)
        {
            if (m_currentIndex >= m_sphericalLoop.Count)
                m_currentIndex = 0;

            int iter = math.min(iterationsPerFrame, m_sphericalLoop.Count - m_currentIndex);

            float3 playerPos = m_world.Camera.transform.position;
            int3 playerChunkPos = math.int3(math.round(playerPos / VoxelChunk.CHUNK_SIZE)) * VoxelChunk.CHUNK_SIZE;

            for (int i = 0; i < iter; ++i)
            {
                var key = playerChunkPos + m_sphericalLoop[i + m_currentIndex] * VoxelChunk.CHUNK_SIZE;

                Iteration(key);
            }

            m_currentIndex += iterationsPerFrame;

            m_outsideKeys.Clear();

            foreach(var outside in Chunks.Dictionary)
            {
                int3 localPos = outside.Key - playerChunkPos;
                if (!m_sphericalLoopSet.Contains(localPos))
                    m_outsideKeys.Add(outside.Key);
            }

            for(int i = 0; i < m_outsideKeys.Count; ++i)
            {
                OutsideRange(m_outsideKeys[i]);
            }
        }

        void OutsideRange(int3 key)
        {
            Chunks.RemoveChunk(key);
        }

        void Iteration(int3 worldChunkPos)
        {
            if (!Chunks.Contains(worldChunkPos))
            {
                VoxelChunk chunk;

                if (!VoxelChunkPool.RecoverChunk(out chunk))
                {
                    chunk = new VoxelChunk(m_world);
                }

                chunk.UpdatePosition((float3)worldChunkPos);
                chunk.OnEnable(m_world);
                m_chunks.Add(worldChunkPos, chunk);
            }
        }
    }
}