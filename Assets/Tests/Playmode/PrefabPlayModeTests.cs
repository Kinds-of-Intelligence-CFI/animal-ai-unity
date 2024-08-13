using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Tests for the Prefab class.
/// </summary>
public class PrefabPlayModeTests
{
    private GameObject _prefabObject;
    private Prefab _prefab;

    [SetUp]
    public void SetUp()
    {
        _prefabObject = new GameObject("TestPrefab");
        _prefab = _prefabObject.AddComponent<Prefab>();

        _prefabObject.AddComponent<MeshRenderer>();
        _prefabObject.AddComponent<MeshFilter>();

        /* Default parameters */
        _prefab.sizeMin = new Vector3(1, 1, 1);
        _prefab.sizeMax = new Vector3(5, 5, 5);
        _prefab.ratioSize = new Vector3(1, 1, 1);
        _prefab.rotationRange = new Vector2(0, 360);
        _prefab.canRandomizeColor = true;
        _prefab.sizeAdjustment = 0.999f;
    }

    [UnityTest]
    public IEnumerator SetColor_AppliesCorrectColor()
    {
        Vector3 testColor = new Vector3(255, 0, 0);
        _prefab.SetColor(testColor);

        Color appliedColor = _prefabObject.GetComponent<Renderer>().material.color;

        Assert.AreEqual(new Color(1f, 0f, 0f, 1f), appliedColor);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SetSize_AppliesCorrectSize()
    {
        Vector3 testSize = new Vector3(3, 3, 3);
        _prefab.SetSize(testSize);

        /* IMPORTANT ASSERTION: assert that the size was applied correctly, taking ratioSize and sizeAdjustment into account */
        Vector3 expectedSize = new Vector3(3, 3, 3) * _prefab.sizeAdjustment;
        Assert.AreEqual(expectedSize, _prefabObject.transform.localScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GetRotation_ReturnsCorrectRotation()
    {
        float testRotationY = 45f;
        Vector3 rotation = _prefab.GetRotation(testRotationY);

        Assert.AreEqual(new Vector3(0, 45f, 0), rotation);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SetDelay_LogsCorrectValue()
    {
        LogAssert.Expect(LogType.Log, "Void SetDelay(Single) activated in Prefab with value 5");

        _prefab.SetDelay(5f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SetInitialValue_LogsCorrectValue()
    {
        LogAssert.Expect(
            LogType.Log,
            "Void SetInitialValue(Single) activated in Prefab with value 10"
        );

        _prefab.SetInitialValue(10f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SetFinalValue_LogsCorrectValue()
    {
        LogAssert.Expect(
            LogType.Log,
            "Void SetFinalValue(Single) activated in Prefab with value 15"
        );

        _prefab.SetFinalValue(15f);
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_prefabObject);
    }
}
