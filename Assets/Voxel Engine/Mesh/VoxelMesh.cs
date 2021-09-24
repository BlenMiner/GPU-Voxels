using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    public class VoxelMesh : IDisposable, IDirty
    {
        public ComputeBuffer FacesBuffer => m_facesBuffer;

        VoxelChunk m_chunk;

        ComputeBuffer m_arguments;

        ComputeBuffer m_facesBuffer;

        ComputeBuffer m_mapFaceCounter;

        MaterialPropertyBlock m_properties;

        public bool IsDirty => m_dirty;
        
        public bool Busy => m_jobWorking;

        int FaceCounterFunction;

        int MeshBakerFunction;

        bool m_jobWorking = false;

        bool m_meshEmpty = true;

        bool m_dirty = false;

        public VoxelMesh(VoxelChunk chunk)
        {
            m_dirty = true;
            
            m_chunk = chunk;

            m_arguments = new ComputeBuffer(1, DrawCallArgBuffer.size, ComputeBufferType.IndirectArguments);

            m_mapFaceCounter = new ComputeBuffer(1, sizeof(uint));

            GPUMemoryTracker.Register(m_arguments);
            GPUMemoryTracker.Register(m_mapFaceCounter);

            m_properties = new MaterialPropertyBlock();

            FaceCounterFunction = chunk.World.FaceCounterShader.FindKernel("FaceCounter");
            MeshBakerFunction = chunk.World.MeshBakerShader.FindKernel("BakeMesh");
        }

        public void GenerateMeshGPU()
        {
            m_jobWorking = true;

            DispatchFaceCounter(m_chunk.World, m_chunk.Map, m_arguments, m_facesBuffer, (bfr) => 
            {
                m_facesBuffer = bfr;
                GPUMemoryTracker.Register(m_arguments);

                m_meshEmpty = m_facesBuffer == null || m_facesBuffer.count == 0;

                m_jobWorking = false;
            });

            ResetDirty();
        }

        private void DispatchFaceCounter(
                VoxelWorld world, VoxelMap map, 
                ComputeBuffer argsBuffer, ComputeBuffer facesBuffer,
                Action<ComputeBuffer> UpdateFaceBuffer)
        {
            var dispathSize = map.Size / 8;

            uint[] _faceCount = new uint[1];

            m_mapFaceCounter.SetData(_faceCount);

            var newArgs = new ComputeBuffer(1, DrawCallArgBuffer.size, ComputeBufferType.IndirectArguments);

            newArgs.SetData(new DrawCallArgBuffer[] {
                new DrawCallArgBuffer() {
                    vertexCountPerInstance = 0,
                    instanceCount = 1,
                    startVertexLocation = 0,
                    startInstanceLocation = 0,
                }
            });

            world.FaceCounterShader.SetInt("m_size_x", map.Size.x);
            world.FaceCounterShader.SetInt("m_size_y", map.Size.y);
            world.FaceCounterShader.SetInt("m_size_z", map.Size.z);

            world.FaceCounterShader.SetBuffer(FaceCounterFunction, "Counter", m_mapFaceCounter);
            world.FaceCounterShader.SetBuffer(FaceCounterFunction, "Map", map.MapBuffer);

            world.FaceCounterShader.Dispatch(FaceCounterFunction, dispathSize.x, dispathSize.y, dispathSize.z);

            AsyncGPUReadback.Request(m_mapFaceCounter, (dataReceived) => {
                    int faceCount = dataReceived.GetData<int>()[0];

                    if (faceCount == 0)
                    {
                        facesBuffer?.Release();
                        facesBuffer = null;
                    }
                    else if (facesBuffer == null || facesBuffer.count != faceCount)
                    {
                        facesBuffer?.Release();
                        facesBuffer = new ComputeBuffer(faceCount, QuadData.size, ComputeBufferType.Append);
                        GPUMemoryTracker.Register(facesBuffer);
                    }

                    DispatchMeshBaker(world, map, newArgs, facesBuffer);
                    UpdateFaceBuffer(facesBuffer);
            });
        }

        private void DispatchMeshBaker(VoxelWorld world, VoxelMap map, ComputeBuffer newArgs, ComputeBuffer facesBuffer)
        {
            if (facesBuffer != null)
            {
                facesBuffer.SetCounterValue(0);
                var dispathSize = map.Size / 8;

                world.MeshBakerShader.SetInt("m_size_x", map.Size.x);
                world.MeshBakerShader.SetInt("m_size_y", map.Size.y);
                world.MeshBakerShader.SetInt("m_size_z", map.Size.z);

                m_properties.SetInt("m_size_x", map.Size.x);
                m_properties.SetInt("m_size_y", map.Size.y);
                m_properties.SetInt("m_size_z", map.Size.z);

                world.MeshBakerShader.SetVector("m_worldPos", map.Chunk.LocalToWorld.MultiplyPoint(Vector3.zero));
                m_properties.SetVector("m_worldPos", map.Chunk.LocalToWorld.MultiplyPoint(Vector3.zero));

                world.MeshBakerShader.SetBuffer(MeshBakerFunction, "Blocks", Blocks.BlocksBuffer);
                world.MeshBakerShader.SetBuffer(MeshBakerFunction, "Map", map.MapBuffer);
                m_properties.SetBuffer("Map", map.MapBuffer);
                world.MeshBakerShader.SetBuffer(MeshBakerFunction, "Faces", facesBuffer);
                world.MeshBakerShader.SetBuffer(MeshBakerFunction, "Args", newArgs);

                foreach(var neighbour in m_chunk.Neighbours)
                {
                    var buffer = neighbour.Value == null ? world.EmptyVoxelMap : neighbour.Value.Map.MapBuffer;
                    world.MeshBakerShader.SetBuffer(MeshBakerFunction, neighbour.Key, buffer);
                    m_properties.SetBuffer(neighbour.Key, buffer);
                }

                world.MeshBakerShader.Dispatch(MeshBakerFunction, dispathSize.x, dispathSize.y, dispathSize.z);
                m_properties.SetBuffer("_Faces", facesBuffer);

                m_arguments?.Release();
                m_arguments = newArgs;
            }
        }

        public void Render()
        {
            if (m_facesBuffer != null)
            {
                var mat = m_chunk.World.VoxelMaterial;

                //m_properties.SetBuffer("_Faces", m_facesBuffer);

                Graphics.DrawProceduralIndirect(mat, m_chunk.Bounds, MeshTopology.Triangles, m_arguments, properties: m_properties);
            }
        }

        public void Dispose()
        {
            m_arguments?.Release();
            m_facesBuffer?.Release();
            m_mapFaceCounter?.Release();
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