using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelTest : MonoBehaviour
{
    public Material m_material;
    public ComputeShader GPUMesher;
    public ComputeShader ArgsFixer;

    VTest btest;
    void Start()
    {
        btest = new VTest();
        btest.Init(Vector3.zero, m_material, GPUMesher, ArgsFixer);
    }
    private void Update()
    {
        btest.GenerateMesh();
        btest.Render();
    }
}

public class VTest
{
    const int dispathDiv = 16;

    Material m_material;
    Vector3 m_position;

    ComputeShader GPUMesher;
    ComputeShader ArgsFixer;

    ComputeBuffer m_mapBuffer;
    ComputeBuffer m_meshBuffer;
    ComputeBuffer argsBuffer;

    int m_kernel_mesh;
    int m_kernel_map;
    public void Init(Vector3 pos, Material mat, ComputeShader gpuMesh, ComputeShader fixer)
    {
        m_position = pos;
        m_material = mat;
        GPUMesher = gpuMesh;
        ArgsFixer = fixer;

        m_kernel_mesh = GPUMesher.FindKernel("CSMain");
        m_kernel_map = GPUMesher.FindKernel("CSMap");

        // Init map
        m_mapBuffer = new ComputeBuffer(GPUChunk.SIZE_3, sizeof(uint));

        // Init mesh
        m_meshBuffer = new ComputeBuffer(GPUChunk.SIZE_3, sizeof(uint) * 3, ComputeBufferType.Append);

        // Initialize buffer
        argsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        // Initilize voxel data
        GPUMesher.SetBuffer(m_kernel_map, "_Map", m_mapBuffer);

        int dispatch = GPUChunk.SIZE / dispathDiv;
        GPUMesher.Dispatch(m_kernel_map, dispatch, dispatch, dispatch);

        GenerateMesh();
    }

    public void GenerateMesh()
    {
        m_meshBuffer.SetCounterValue(0);

        GPUMesher.SetBuffer(m_kernel_mesh, "_Map", m_mapBuffer);
        GPUMesher.SetBuffer(m_kernel_mesh, "_Mesh", m_meshBuffer);
        int dispatch = GPUChunk.SIZE / dispathDiv;
        GPUMesher.Dispatch(m_kernel_mesh, dispatch, dispatch, dispatch);

        ComputeBuffer.CopyCount(m_meshBuffer, argsBuffer, 0);

        ArgsFixer.SetBuffer(0, "DrawCallArgs", argsBuffer);
        ArgsFixer.Dispatch(0, 1, 1, 1);

        m_material.SetBuffer("_Mesh", m_meshBuffer);
        m_material.SetBuffer("_Map", m_mapBuffer);
    }

    public void Render()
    {
        m_material.SetPass(0);

        Vector3 p = Vector3.one * GPUChunk.SIZE;

        Graphics.DrawProceduralIndirect(m_material,
            new Bounds(m_position + p * 0.5f, p), MeshTopology.Triangles, argsBuffer);
    }
}