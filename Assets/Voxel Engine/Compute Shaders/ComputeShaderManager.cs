using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderManager : MonoBehaviour
{
    [SerializeField] ComputeShader m_digBigSphere;

    public ComputeShader DigBigSphere => m_digBigSphere;
}
