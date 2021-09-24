using Unity.Mathematics;
using UnityEngine;
using VoxelEngine;

public class VoxelEditor : MonoBehaviour
{
    [SerializeField] VoxelWorld m_world;

    void Update()
    {
        /*if (VoxelRaycaster.Raycast(m_world, transform.position, transform.forward, out var hit))
        {
        }*/
    }

    private void OnDrawGizmos()
    {
        if (VoxelRaycaster.Raycast(m_world, transform.position, transform.forward, out var hit))
        {
            Gizmos.DrawWireCube((float3)hit.hitPoint + math.float3(0.5f, 0.5f, 0.5f), Vector3.one);
        }
    }
}
