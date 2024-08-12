using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the BallGoal class.
/// </summary>
public class BallGoalTests
{
    private BallGoal ballGoal;
    private GameObject testObject;

    [SetUp]
    public void Setup()
    {
        testObject = new GameObject("BallGoal");
        ballGoal = testObject.AddComponent<BallGoal>();

        ballGoal.sizeMin = Vector3.one * 1.0f;
        ballGoal.sizeMax = Vector3.one * 3.0f;
        ballGoal.sizeAdjustment = 1.0f;
        ballGoal.ratioSize = Vector3.one;
    }

    [TearDown]
    public void Teardown()
    {
        if (testObject != null)
        {
            Object.DestroyImmediate(testObject);
        }
    }

    [Test]
    public void TestSetSize_WithinLimits()
    {
        Vector3 testSize = new Vector3(2.0f, 2.0f, 2.0f);
        ballGoal.SetSize(testSize);

        Assert.AreEqual(
            testSize,
            ballGoal.transform.localScale,
            "The size should be set correctly within the limits."
        );
        Assert.AreEqual(
            2.0f,
            ballGoal.reward,
            "The reward should be adjusted according to the size."
        );
    }

    [Test]
    public void TestSetSize_Clipping()
    {
        Vector3 testSize = new Vector3(4.0f, 4.0f, 4.0f); /* Size exceeding the maximum */
        ballGoal.SetSize(testSize);

        Vector3 expectedSize = ballGoal.sizeMax;
        Assert.AreEqual(
            expectedSize,
            ballGoal.transform.localScale,
            "The size should be clipped to the maximum allowed size."
        );
        Assert.AreEqual(3.0f, ballGoal.reward, "The reward should correspond to the clipped size.");
    }

    [Test]
    public void TestSetSize_NegativeSize()
    {
        Vector3 testSize = new Vector3(-1.0f, -1.0f, -1.0f); /* Negative size components */
        ballGoal.SetSize(testSize);

        float randomSize = ballGoal.transform.localScale.x;
        Assert.IsTrue(
            randomSize >= ballGoal.sizeMin.x && randomSize <= ballGoal.sizeMax.x,
            "The size should be set to a random value within the limits."
        );
        Assert.AreEqual(
            randomSize,
            ballGoal.reward,
            "The reward should match the randomly selected size."
        );
    }

    [Test]
    public void TestSetSize_WithSizeAdjustment()
    {
        ballGoal.sizeAdjustment = 2.0f;
        Vector3 testSize = new Vector3(1.5f, 1.5f, 1.5f);
        ballGoal.SetSize(testSize);

        Vector3 expectedSize = testSize * 2.0f;
        expectedSize = Vector3.Min(expectedSize, ballGoal.sizeMax);

        Assert.AreEqual(
            expectedSize,
            ballGoal.transform.localScale,
            "The size should be adjusted correctly."
        );
        Assert.AreEqual(
            expectedSize.x,
            ballGoal.reward,
            "The reward should be adjusted according to the scaled size."
        );
    }
}
