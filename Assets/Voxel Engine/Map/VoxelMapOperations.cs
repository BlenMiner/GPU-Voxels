using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public class VoxelMapOperations
    {
        ComputeShaderManager m_shaders;

        VoxelChunk m_chunk;

        int m_digBigSphere;

        public VoxelMapOperations(VoxelChunk chunk, ComputeShaderManager shaders)
        {
            m_shaders = shaders;
            m_chunk = chunk;

            m_digBigSphere = m_shaders.DigBigSphere.FindKernel("DigBigSphere");
        }

        public void DigBigSphere(float3 worldPosition, float radius, uint blockId = 0)
        {
            int3 dispatchSize = VoxelChunk.CHUNK_SIZE / 8;

            m_shaders.DigBigSphere.SetInt("m_size_x", VoxelChunk.CHUNK_SIZE.x);
            m_shaders.DigBigSphere.SetInt("m_size_y", VoxelChunk.CHUNK_SIZE.y);
            m_shaders.DigBigSphere.SetInt("m_size_z", VoxelChunk.CHUNK_SIZE.z);

            m_shaders.DigBigSphere.SetInt("B_BLOCK", (int)blockId);

            m_shaders.DigBigSphere.SetVector("m_worldPos", m_chunk.LocalToWorld.MultiplyPoint(Vector3.zero));
            m_shaders.DigBigSphere.SetVector("m_actionPosition", (Vector3)worldPosition);
            m_shaders.DigBigSphere.SetFloat("m_actionRadius", radius);
            
            m_shaders.DigBigSphere.SetBuffer(m_digBigSphere, "Map", m_chunk.Map.MapBuffer);

            m_shaders.DigBigSphere.Dispatch(m_digBigSphere, dispatchSize.x, dispatchSize.y, dispatchSize.z);

            m_chunk.Map.RequestCPUMap();
            m_chunk.Mesh.SetDirtyAndNeighbors();
        }
    }
}
