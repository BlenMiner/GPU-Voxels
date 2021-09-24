using Unity.Mathematics;
using UnityEngine;
using VoxelEngine;
using Gizmos = Popcron.Gizmos;

public class VoxelEditor : MonoBehaviour
{
    [SerializeField] VoxelWorld m_world;

    void Update()
    {
        if (VoxelRaycaster.Raycast(m_world, transform.position, transform.forward, out var hit))
        {
            float3 worldPos = (float3)hit.hitPoint + math.float3(0.5f, 0.5f, 0.5f);
            Gizmos.Bounds(new Bounds(worldPos, Vector3.one * 1.01f), Color.white);

            if (Input.GetMouseButtonDown(0))
            {
                hit.chunk.MapOperations.DigBigSphere(worldPos, 1f, Blocks.BLOCK_AIR);
            }
        }
    }
}
