using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using VoxelEngine;

public class UVPackerTests
{
    [Test]
    public void TestTopUv()
    {
        int val = UVPacker.Pack(math.int2(1, 9), int2.zero, int2.zero, int2.zero);

        UVPacker.Unpack(val, out int2 resultTop, out _, out _, out _);

        Assert.AreEqual(math.int2(1, 9), resultTop);
    }

    [Test]
    public void TestBottomUv()
    {
        int val = UVPacker.Pack(math.int2(15, 15), math.int2(6, 9), math.int2(14, 14), math.int2(13, 13));

        UVPacker.Unpack(val, out _, out var resultBottom, out _, out _);

        Assert.AreEqual(math.int2(6, 9), resultBottom);
    }

    [Test]
    public void TestSidesUv()
    {
        int val = UVPacker.Pack(math.int2(15, 15), math.int2(6, 9), math.int2(14, 14), math.int2(11, 11));


        UVPacker.Unpack(val, out _, out _, out var sides, out _);

        Assert.AreEqual(math.int2(14, 14), sides);
    }
}
