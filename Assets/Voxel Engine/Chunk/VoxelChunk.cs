using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public class VoxelChunk: IDisposable, IDestructible
    {
        public static int3 CHUNK_SIZE = new int3(144, 144, 144);

        public Matrix4x4 LocalToWorld => m_matrix;

        public Bounds Bounds => m_bounds;

        public VoxelWorld World => m_world;

        public VoxelMesh Mesh => m_mesh;

        public VoxelMap Map => m_map;

        public VoxelMapOperations MapOperations => m_mapOperations;

        public bool IsInitialized { get; private set; }

        public bool IsEnabled { get; private set; }

        public float TimeSinceDisabled => Time.time - m_disabledTimeStamp;

        private float m_disabledTimeStamp;

        static Dictionary<int3, string> m_offsetToMapName;

        static Dictionary<string, int3> m_mapNameToOffset;

        static Dictionary<string, string> m_oppositeMapName;

        Dictionary<string, VoxelChunk> m_neighbourMaps;

        public Dictionary<string, VoxelChunk> Neighbours => m_neighbourMaps;

        public Dictionary<int3, string> OffsetMaps => m_offsetToMapName;

        VoxelWorld m_world;

        Matrix4x4 m_matrix;

        Bounds m_bounds;

        VoxelMesh m_mesh;

        VoxelMap m_map;

        VoxelMapOperations m_mapOperations;

        string GetMapName(int3 offset)
        {
            int x = offset.x;
            int y = offset.y;
            int z = offset.z;

            return $"Map{(x < 0 ? (char)('_') : (char)('0' + x))}{(y < 0 ? (char)('_') : (char)('0' + y))}{(z < 0 ? (char)('_') : (char)('0' + z))}";
        }

        internal bool IsVoxel(int3 mapPos, out VoxelChunk blockChunk, out uint blockId)
        {
            int3 myPosition = math.int3(LocalToWorld.MultiplyPoint(Vector3.zero));
            int3 worldMapPos = mapPos;

            mapPos -= myPosition;
            blockChunk = this;

            if (Map.IsOutOfBounds(mapPos, out int3 offset))
            {
                mapPos = mapPos - offset * CHUNK_SIZE;
                var neighbor = this.Neighbours[OffsetMaps[offset]];

                if (neighbor == null)
                {
                    blockId = Blocks.BLOCK_AIR;
                    return false;
                }
                else return neighbor.IsVoxel(worldMapPos, out blockChunk, out blockId);;
            }

            blockId = blockChunk.Map.NativeArray[VoxelMap.to1D(mapPos, CHUNK_SIZE)];
            return blockId != Blocks.BLOCK_AIR;
        }

        public VoxelChunk(VoxelWorld world)
        {
            m_world = world;
            m_mesh = new VoxelMesh(this);
            m_map = new VoxelMap(this, CHUNK_SIZE.x, CHUNK_SIZE.y, CHUNK_SIZE.z);
            m_mapOperations = new VoxelMapOperations(this, World.ComputeShaders);

            InitNeighbourData();
        }

        private void InitNeighbourData()
        {
            m_neighbourMaps = new Dictionary<string, VoxelChunk>();

            if (m_offsetToMapName == null)
            {
                m_mapNameToOffset = new Dictionary<string, int3>();
                m_offsetToMapName = new Dictionary<int3, string>();
                m_oppositeMapName = new Dictionary<string, string>();

                for (int x = -1; x <= 1; ++x)
                {
                    for (int y = -1; y <= 1; ++y)
                    {
                        for (int z = -1; z <= 1; ++z)
                        {
                            if (x == 0 && y == 0 && z == 0) continue;

                            var offset = math.int3(x, y, z);

                            string mapName = GetMapName(offset);
                            string oppositeMapName = GetMapName(-offset);

                            m_oppositeMapName.Add(mapName, oppositeMapName);
                            m_offsetToMapName.Add(offset, mapName);

                            m_mapNameToOffset.Add(mapName, offset);
                        }
                    }
                }
            }

            foreach (var neighbour in m_offsetToMapName)
            {
                m_neighbourMaps.Add(neighbour.Value, null);
            }
        }

        public void UpdateNeighbours(bool chunkWillBeDeleted)
        {
            int3 key = math.int3(LocalToWorld.MultiplyPoint(Vector3.zero));

            foreach(var neighbour in m_offsetToMapName)
            {
                int3 pos = key + neighbour.Key * CHUNK_SIZE;

                if (World.VoxelCollection.Chunks.TryGetValue(pos, out var chunk))
                {
                    m_neighbourMaps[neighbour.Value] = chunk;

                    string oppositeMap = m_oppositeMapName[neighbour.Value];
                    chunk.m_neighbourMaps[oppositeMap] = chunkWillBeDeleted ? null : this;
                    chunk.Mesh.SetDirty();
                }
                else
                {
                    m_neighbourMaps[neighbour.Value] = null;
                }
            }

            m_mesh.SetDirty();
        }

        public void OnEnable(VoxelWorld world)
        {
            m_world = world;
            
            m_mesh.SetDirty();
            m_map.SetDirty();
            
            IsEnabled = true;
        }

        public void OnDisable()
        {
            UpdateNeighbours(true);
            
            IsEnabled = false;
            m_disabledTimeStamp = Time.time;
        }

        public void Update()
        {
            if (!m_map.Busy && !m_mesh.Busy)
            {
                if (m_map.IsDirty)
                {
                    m_map.GenerateMapGPU();
                }
                else if (m_mesh.IsDirty)
                {
                    m_mesh.GenerateMeshGPU();
                }
            }
        }

        public void UpdatePosition(Vector3 position)
        {
            m_matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            m_bounds = new Bounds(position + (Vector3)(float3)CHUNK_SIZE * 0.5f, (float3)CHUNK_SIZE);
        }

        public void Render()
        {
            m_mesh.Render();
        }

        public void OnRemovedFromWorld()
        {
            OnDisable();

            VoxelChunkPool.FreeChunk(this);
        }


        public void Dispose()
        {
            m_mesh.Dispose();
            m_map.Dispose();
        }

        public void OnDrawGizmos()
        {
            /*Vector3 origin = LocalToWorld.MultiplyPoint(Vector3.zero);

            Gizmos.color = Color.white * 0.5f;
            Gizmos.DrawWireCube(m_bounds.center, m_bounds.size);*/
        }
    }
}