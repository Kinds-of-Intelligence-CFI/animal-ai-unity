using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Tests for the ComputeScaleRatios class.
/// </summary>
public class ComputeScaleRatiosPlayModeTests
{
    private GameObject _parentObject;
    private ComputeScaleRatios _computeScaleRatios;
    private GameObject _childObject;

    [SetUp]
    public void SetUp()
    {
        _parentObject = new GameObject("ParentObject");
        _parentObject.transform.localScale = new Vector3(2, 2, 2);

        var meshFilter = _parentObject.AddComponent<MeshFilter>();
        meshFilter.mesh = GameObject
            .CreatePrimitive(PrimitiveType.Cube)
            .GetComponent<MeshFilter>()
            .mesh;
        _parentObject.AddComponent<MeshRenderer>();

        _childObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _childObject.transform.SetParent(_parentObject.transform);
        _childObject.transform.localScale = new Vector3(1, 1, 1);
        _childObject.transform.localPosition = Vector3.zero;

        _computeScaleRatios = _parentObject.AddComponent<ComputeScaleRatios>();
    }

    [UnityTest]
    public IEnumerator ComputesScaleRatiosCorrectly()
    {
        yield return null;

        /* Calculate expected ratio values */
        Vector3 expectedSizeReal = _parentObject.GetComponent<Renderer>().bounds.size;
        Vector3 expectedScaleReal = _parentObject.transform.localScale;
        Vector3 expectedRatioSize = new Vector3(
            expectedSizeReal.x != 0
                ? expectedScaleReal.x / expectedSizeReal.x
                : float.PositiveInfinity,
            expectedSizeReal.y != 0
                ? expectedScaleReal.y / expectedSizeReal.y
                : float.PositiveInfinity,
            expectedSizeReal.z != 0
                ? expectedScaleReal.z / expectedSizeReal.z
                : float.PositiveInfinity
        );

        // A few assertions...
        Assert.AreEqual(
            expectedRatioSize.x,
            expectedScaleReal.x / expectedSizeReal.x,
            "Ratio X was not computed correctly."
        );
        Assert.AreEqual(
            expectedRatioSize.y,
            expectedScaleReal.y / expectedSizeReal.y,
            "Ratio Y was not computed correctly."
        );
        Assert.AreEqual(
            expectedRatioSize.z,
            expectedScaleReal.z / expectedSizeReal.z,
            "Ratio Z was not computed correctly."
        );
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_parentObject);
        Object.Destroy(_childObject);
    }
}
