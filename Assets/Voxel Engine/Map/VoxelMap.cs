using System;
using ImprovedPerlinNoiseProject;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    public class VoxelMap : IDisposable, IDirty
    {
        int3 m_size;

        int m_count;

        NativeArray<uint> m_map;

        public int3 Size => m_size;

        public int Count => m_count;

        public bool Busy => m_jobWorking;

        public NativeArray<uint> NativeArray => m_map;

        public ComputeBuffer MapBuffer {get; private set;}

        private bool m_dirty = false;

        public bool IsDirty => m_dirty;

        VoxelChunk m_chunk;

        public VoxelChunk Chunk => m_chunk;

        private int GPU_BakeMapFunction;

        public VoxelMap(VoxelChunk chunk, int sizeX, int sizeY, int sizeZ)
        {
            GPU_BakeMapFunction = chunk.World.MapBakerShader.FindKernel("MapBaker");

            m_chunk = chunk;
            m_size = math.int3(sizeX, sizeY, sizeZ);
            m_count = sizeX * sizeY * sizeZ;

            m_map = new NativeArray<uint>(m_count, Allocator.Persistent);
            MapBuffer = new ComputeBuffer(m_count, sizeof(uint), ComputeBufferType.Default);

            GPUMemoryTracker.Register(MapBuffer);
        }

        VoxelMapBaker m_bakingJobData;

        JobHandle m_bakingJob;

        bool m_jobWorking = false;
        
        internal void GenerateMapGPU()
        {
            ResetDirty();
            m_jobWorking = true;

            int3 dispatchSize = Size / 8;

            var shader = m_chunk.World.MapBakerShader;

            shader.SetInt("B_AIR", (int)Blocks.BLOCK_AIR);
            shader.SetInt("B_DIRT", (int)Blocks.BLOCK_DIRT);
            shader.SetInt("B_GRASS", (int)Blocks.BLOCK_GRASS);

            shader.SetInt("m_size_x", Size.x);
            shader.SetInt("m_size_y", Size.y);
            shader.SetInt("m_size_z", Size.z);

            shader.SetVector("m_worldPos", Chunk.LocalToWorld.MultiplyPoint(Vector3.zero));

            shader.SetBuffer(GPU_BakeMapFunction, "Map", MapBuffer);

            shader.Dispatch(GPU_BakeMapFunction, dispatchSize.x, dispatchSize.y, dispatchSize.z);

            AsyncGPUReadback.Request(MapBuffer, (dataReceived) => 
                {
                    var data = dataReceived.GetData<uint>();
                    if (NativeArray.IsCreated && data.IsCreated)
                        data.CopyTo(NativeArray);
                    m_jobWorking = false;

                    m_chunk.UpdateNeighbours(false);
                }
            );
        }

        public static int to1D(int3 pos, int3 size) {
            return pos.x + pos.y * size.x + pos.z * size.x * size.y;
        }

        public static int3 to3D(int idx, int3 size) {
            int x = idx % size.x;
            int y = ( idx / size.x ) % size.y;
            int z = idx / ( size.x * size.y );
            return math.int3(x, y, z);
        }
        
        public void Dispose()
        {
            if (NativeArray.IsCreated)
                NativeArray.Dispose();
            MapBuffer?.Release();
        }

        public void SetDirty()
        {
            m_dirty = true;
        }

        public void ResetDirty()
        {
            m_dirty = false;
        }
    }
}