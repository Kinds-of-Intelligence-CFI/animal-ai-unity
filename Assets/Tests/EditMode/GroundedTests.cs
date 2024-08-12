using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the Grounded class.
/// </summary>
public class GroundedTests
{
    private GameObject gameObject;
    private TestableGrounded grounded;

    private class TestableGrounded : Grounded
    {
        public float InvokeAdjustY(float yIn)
        {
            return AdjustY(yIn);
        }
    }

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject();
        grounded = gameObject.AddComponent<TestableGrounded>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void AdjustY_ShouldAlwaysReturnZero()
    {
        float result = grounded.InvokeAdjustY(5f);
        Assert.AreEqual(0f, result);
    }

    [Test]
    public void AdjustY_ShouldReturnZeroForNegativeValues()
    {
        float result = grounded.InvokeAdjustY(-10f);
        Assert.AreEqual(0f, result);
    }

    [Test]
    public void AdjustY_ShouldReturnZeroForZeroInput()
    {
        float result = grounded.InvokeAdjustY(0f);
        Assert.AreEqual(0f, result);
    }
}
