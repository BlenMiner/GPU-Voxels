using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    const long msBudget = 5;

    [SerializeField] Material m_chunkMaterial;

    [SerializeField] public int m_viewRange = 5;
    [SerializeField] public int m_despawnRange = 10;

    Dictionary<Vector3Int, GPUChunk> m_chunks =
        new Dictionary<Vector3Int, GPUChunk>();

    List<GPUChunk> m_chunks_list = 
        new List<GPUChunk>();

    HashSet<Vector3Int> m_despawnQueue = 
        new HashSet<Vector3Int>();

    List<Vector3Int> m_sphere_loop;

    private float m_viewDistance;

    public float ViewDistance => m_viewDistance;


    private void Awake()
    {
        m_viewDistance = m_despawnRange * GPUChunk.SIZE;

        m_sphere_loop = new List<Vector3Int>(m_viewRange * m_viewRange * m_viewRange);

        for (int x = -m_viewRange; x < m_viewRange; ++x)
            for (int y = -m_viewRange; y < m_viewRange; ++y)
                for (int z = -m_viewRange; z < m_viewRange; ++z)
                    m_sphere_loop.Add(new Vector3Int(x, y, z) * GPUChunk.SIZE);

        m_sphere_loop.Sort((Vector3Int a, Vector3Int b) =>
        {
            float aDist = a.sqrMagnitude;
            float bDist = b.sqrMagnitude;

            if (aDist < bDist) return -1;
            else if (aDist > bDist) return 1;
            return 0;
        });
    }

    private void SpawnChunk(Vector3Int position)
    {
        /*var c = new GPUChunk(this, position);

        m_chunks_list.Add(c);
        m_chunks.Add(position, c);*/
    }

    public void QueueDespawnChunk(Vector3Int position)
    {
        const int iterationsPerFrame = 50;

        if (m_despawnQueue.Count < iterationsPerFrame)
            m_despawnQueue.Add(position);
    }

    public GPUChunk FetchGPUChunk(Vector3Int chunkPosition)
    {
        bool v = m_chunks.TryGetValue(chunkPosition, out GPUChunk res);
        return v ? res : null;
    }

    public GPUChunk FetchGPUChunk(Vector3 worldPosition)
    {
        return FetchGPUChunk(worldPosition.ToChunkPosition());
    }

    private void Update()
    {
        playerPosition = transform.position.ToChunkPosition();

        HandleDespawning();
        HandleSpawning(playerPosition);
    }

    private void HandleDespawning()
    {
        foreach (var p in m_despawnQueue)
        {
            if (m_chunks.ContainsKey(p))
            {
                var c = m_chunks[p];

                OnDestroyChunk(c);

                m_chunks_list.Remove(c);
                m_chunks.Remove(p);
            }
        }
        m_despawnQueue.Clear();
    }

    int m_spawningIterIndex = 0;
    internal Vector3Int playerPosition;

    private void HandleSpawning(Vector3Int pPosition)
    {
        const int iterationsPerFrame = 100;

        int iterationsThisFrame = Mathf.Min(m_sphere_loop.Count - m_spawningIterIndex, iterationsPerFrame);

        if (iterationsThisFrame == 0)
        {
            m_spawningIterIndex = 0;
            return;
        }

        for (int i = 0; i < iterationsThisFrame; ++i)
        {
            int index = i + m_spawningIterIndex;
            var chunkPos = pPosition + m_sphere_loop[index];

            if (!m_chunks.ContainsKey(chunkPos))
                SpawnChunk(chunkPos);
        }

        m_spawningIterIndex += iterationsThisFrame;
    }

    private void OnDestroyChunk(GPUChunk chunk)
    {
        /*if (chunk.gameObject != null)
            PoolManager.ReleaseObject(chunk.gameObject);*/
    }

    private void OnDestroy()
    {
        foreach (var chunk in m_chunks)
        {
            OnDestroyChunk(chunk.Value);
        }
    }
}
