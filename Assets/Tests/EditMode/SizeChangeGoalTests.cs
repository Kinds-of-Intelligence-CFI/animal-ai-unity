using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the SizeChangeGoal class.
/// </summary>
public class SizeChangeGoalTests
{
    private SizeChangeGoal sizeChangeGoal;

    [SetUp]
    public void Setup()
    {
        sizeChangeGoal = new GameObject().AddComponent<SizeChangeGoal>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(sizeChangeGoal.gameObject);
    }

    [Test]
    public void RewardOverride_DefaultValue_IsZero()
    {
        Assert.AreEqual(0f, sizeChangeGoal.rewardOverride);
    }

    [Test]
    public void RewardOverride_SetValue_ReturnsCorrectValue()
    {
        float testValue = 5.5f;
        sizeChangeGoal.rewardOverride = testValue;
        Assert.AreEqual(testValue, sizeChangeGoal.rewardOverride);
    }

    [Test]
    public void RewardOverride_SetNegativeValue_ReturnsNegativeValue()
    {
        float testValue = -3.2f;
        sizeChangeGoal.rewardOverride = testValue;
        Assert.AreEqual(testValue, sizeChangeGoal.rewardOverride);
    }
}
