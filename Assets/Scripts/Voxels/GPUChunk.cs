using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

public class GPUChunk : MonoBehaviour
{
    public const int SIZE = 128;
    public const int SIZE_2 = SIZE * SIZE;
    public const int SIZE_3 = SIZE * SIZE * SIZE;

    [SerializeField] Material m_material;
    [SerializeField] ComputeShader GPUMesher;
    [SerializeField] ComputeShader ArgsFixer;

    ComputeBuffer m_mapBuffer;
    ComputeBuffer m_meshBuffer;
    ComputeBuffer argsBuffer;

    int m_kernel_mesh;
    int m_kernel_map;

    const int dispathDiv = 16;

    private void Awake()
    {
        m_kernel_mesh = GPUMesher.FindKernel("CSMain");
        m_kernel_map = GPUMesher.FindKernel("CSMap");

        // Init map
        m_mapBuffer = new ComputeBuffer(SIZE_3, sizeof(uint));

        // Init mesh
        m_meshBuffer = new ComputeBuffer(SIZE_3, sizeof(uint) * 3, ComputeBufferType.Append);

        // Initialize buffer
        argsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        // Initilize voxel data
        GPUMesher.SetBuffer(m_kernel_map, "_Map", m_mapBuffer);
        GPUMesher.Dispatch(m_kernel_map, SIZE / dispathDiv, SIZE / dispathDiv, SIZE / dispathDiv);

        GenerateMesh();

        Debug.Log(
            m_mapBuffer.count * m_mapBuffer.stride +
            m_meshBuffer.count * m_meshBuffer.stride);
    }

    private void GenerateMesh()
    {
        m_meshBuffer.SetCounterValue(0);

        GPUMesher.SetBuffer(m_kernel_mesh, "_Map", m_mapBuffer);
        GPUMesher.SetBuffer(m_kernel_mesh, "_Mesh", m_meshBuffer);
        GPUMesher.Dispatch(m_kernel_mesh, SIZE / dispathDiv, SIZE / dispathDiv, SIZE / dispathDiv);

        ComputeBuffer.CopyCount(m_meshBuffer, argsBuffer, 0);

        ArgsFixer.SetBuffer(0, "DrawCallArgs", argsBuffer);
        ArgsFixer.Dispatch(0, 1, 1, 1);

        m_material.SetBuffer("_Mesh", m_meshBuffer);
        m_material.SetBuffer("_Map", m_mapBuffer);
    }

    private void Update()
    {
        m_material.SetPass(0);

        Vector3 p = new Vector3(SIZE, SIZE, SIZE);

        Graphics.DrawProceduralIndirect(m_material,
            new Bounds(transform.position + p * 0.5f, p), MeshTopology.Triangles, argsBuffer);
    }

    private void OnDestroy()
    {
        m_mapBuffer.Dispose();
        m_meshBuffer.Dispose();
        argsBuffer.Dispose();
    }
}
