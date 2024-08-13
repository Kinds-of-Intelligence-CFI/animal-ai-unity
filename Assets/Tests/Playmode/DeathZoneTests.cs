using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for the DeathZone class.
/// </summary>
public class DeathZoneTests
{
    private GameObject _deathZoneObject;
    private DeathZone _deathZone;

    [SetUp]
    public void SetUp()
    {
        _deathZoneObject = new GameObject("DeathZone");

        _deathZoneObject.AddComponent<MeshRenderer>();
        _deathZoneObject.AddComponent<BoxCollider>();

        _deathZone = _deathZoneObject.AddComponent<DeathZone>();

        _deathZone.sizeMin = new Vector3(1, 1, 1);
        _deathZone.sizeMax = new Vector3(10, 10, 10);
        _deathZone.sizeAdjustment = 1.0f;
        _deathZone.ratioSize = Vector3.one;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_deathZoneObject);
    }

    [Test]
    public void TestSetSizeWithinLimits()
    {
        Vector3 size = new Vector3(5, 5, 5);
        _deathZone.SetSize(size);

        /* Check if the scale is set correctly within limits */
        Assert.AreEqual(size, _deathZoneObject.transform.localScale);
    }

    [Test]
    public void TestSetSizeClipsToMinMax()
    {
        Vector3 size = new Vector3(15, 15, 15); /* Here, size is above sizeMax */
        _deathZone.SetSize(size);

        /* Expected size should be clipped to sizeMax */
        Vector3 expectedSize = _deathZone.sizeMax;
        Assert.AreEqual(expectedSize, _deathZoneObject.transform.localScale);

        size = new Vector3(0.5f, 0.5f, 0.5f); /* Below sizeMin */
        _deathZone.SetSize(size);

        /* Expected size should be clipped to sizeMin */
        expectedSize = _deathZone.sizeMin;
        Assert.AreEqual(expectedSize, _deathZoneObject.transform.localScale);
    }

    [Test]
    public void TestSetSizeWithNegativeValues()
    {
        Vector3 size = new Vector3(-1, -1, -1);
        _deathZone.SetSize(size);

        Assert.GreaterOrEqual(_deathZoneObject.transform.localScale.x, _deathZone.sizeMin.x);
        Assert.LessOrEqual(_deathZoneObject.transform.localScale.x, _deathZone.sizeMax.x);
        Assert.GreaterOrEqual(_deathZoneObject.transform.localScale.y, _deathZone.sizeMin.y);
        Assert.LessOrEqual(_deathZoneObject.transform.localScale.y, _deathZone.sizeMax.y);
        Assert.GreaterOrEqual(_deathZoneObject.transform.localScale.z, _deathZone.sizeMin.z);
        Assert.LessOrEqual(_deathZoneObject.transform.localScale.z, _deathZone.sizeMax.z);
    }

    [Test]
    public void TestAdjustY()
    {
        var deathZoneObject = new GameObject("DeathZone");
        var deathZone = deathZoneObject.AddComponent<TestableDeathZone>();

        float adjustedY = deathZone.TestAdjustY(1.0f);

        Assert.AreEqual(-0.15f, adjustedY);
    }

    [Test]
    public void TestShaderScaleSet()
    {
        Vector3 size = new Vector3(5, 5, 5);
        _deathZoneObject.AddComponent<MeshRenderer>();
        _deathZone.SetSize(size);

        Vector3 shaderScale = _deathZoneObject
            .GetComponent<Renderer>()
            .material.GetVector("_ObjScale");
        Assert.AreEqual(size, shaderScale);
    }
}

public class TestableDeathZone : DeathZone
{
    /* Expose AdjustY method for testing. One implementation I found (could be other, better ones) */
    public float TestAdjustY(float y)
    {
        return AdjustY(y);
    }
}
