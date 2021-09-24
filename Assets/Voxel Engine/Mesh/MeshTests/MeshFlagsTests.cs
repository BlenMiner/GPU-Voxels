using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoxelEngine;
using Unity.Mathematics;

public class MeshFlagsTests
{
    [Test]
    public void MeshFlags_Conversion()
    {
        MeshFaceMask mask = MeshFaceMask.Top | MeshFaceMask.Bottom;
        int maskValue = (int)mask;

        Assert.AreEqual(3, maskValue);

        MeshFaceMask readBack = (MeshFaceMask)maskValue;

        Assert.AreEqual(mask, readBack);

        Assert.AreEqual(true, readBack.HasFlag(MeshFaceMask.Top));
        Assert.AreEqual(true, readBack.HasFlag(MeshFaceMask.Bottom));
    }

    [Test]
    public void MeshFlags_ToInt3()
    {
        Assert.AreEqual(math.int3(0, 1, 0), MeshFaceMaskExtensions.ToInt3(MeshFaceMask.Top));
    }
}
