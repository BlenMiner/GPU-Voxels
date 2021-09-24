using ImprovedPerlinNoiseProject;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public class VoxelWorld : MonoBehaviour
    {
        [SerializeField, Tooltip("Used to calculate if a chunk is visible")]
        Transform m_playerCamera;

        [SerializeField] Material m_voxelMaterial;

        [SerializeField] ComputeShader m_voxelFaceCounter;

        [SerializeField] ComputeShader m_voxelMeshBaker;

        [SerializeField] ComputeShader m_voxelMapBaker;

        public ComputeShaderManager ComputeShaders {get; private set;}

        public ComputeBuffer EmptyVoxelMap { get; private set; }

        public Material VoxelMaterial => m_voxelMaterial;

        public ComputeShader FaceCounterShader => m_voxelFaceCounter;

        public ComputeShader MeshBakerShader => m_voxelMeshBaker;

        public ComputeShader MapBakerShader => m_voxelMapBaker;

        public Transform Camera => m_playerCamera;

        public VoxelLifeManager VoxelCollection { get; private set; }

        private void Awake()
        {
            ComputeShaders = GetComponentInChildren<ComputeShaderManager>();
            
            Blocks.InitializeBlocks();

            VoxelCollection = new VoxelLifeManager(this, math.int2(4, 4));

            EmptyVoxelMap = new ComputeBuffer(VoxelChunk.CHUNK_SIZE.x * VoxelChunk.CHUNK_SIZE.y * VoxelChunk.CHUNK_SIZE.z, sizeof(uint));

            GPUMemoryTracker.Register(EmptyVoxelMap);
        }

        private void Update()
        {
            VoxelCollection.Update(Time.deltaTime);

            m_voxelMaterial.SetPass(0);

            VoxelCollection.Chunks.ForEach((chunk) => {
                chunk.Update();
                chunk.Render();
            });
        }

        private void OnDrawGizmos()
        {
            if (VoxelCollection != null)
            {
                VoxelCollection.Chunks.ForEach((chunk) => {
                    chunk.OnDrawGizmos();
                });
            }
        }

        private void FixedUpdate()
        {
            VoxelChunkPool.GarbageCollector();
        }

        private void OnDestroy()
        {
            VoxelCollection.Chunks.ForEach((chunk) => {
                chunk.Dispose();
            });

            Blocks.Dispose();
            VoxelChunkPool.FreeResources();

            EmptyVoxelMap?.Release();
        }
    }
}