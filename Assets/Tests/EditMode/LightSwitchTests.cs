using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lights;

/// <summary>
/// Tests for the Lights (blackout feature) class.
/// </summary>
public class LightsSwitchTests
{
    [Test]
    public void InfiniteEnumerator_IncrementValueOnMoveNext()
    {
        var enumerator = new InfiniteEnumerator(3);
        enumerator.MoveNext();
        Assert.AreEqual(3, enumerator.Current);
    }

    [Test]
    public void LightsSwitch_ThrowExceptionForNegativeEpisodeLength()
    {
        Assert.Throws<ArgumentException>(() => new LightsSwitch(-10, new List<int> { 1, 2, 3 }));
    }

    [Test]
    public void ResetEnumeratorToZero()
    {
        int initialValue = 5;
        var enumerator = new InfiniteEnumerator(initialValue);
        enumerator.MoveNext();

        enumerator.Reset();
        int currentValue = enumerator.Current;

        Assert.AreEqual(0, currentValue);
    }

    [Test]
    public void InfiniteEnumerator_ResetToZero()
    {
        var enumerator = new InfiniteEnumerator(5);
        enumerator.MoveNext();
        enumerator.Reset();
        Assert.AreEqual(0, enumerator.Current);
    }

    [Test]
    public void LightsSwitch_InitializeWithValidParameters()
    {
        var lightsSwitch = new LightsSwitch(10, new List<int> { 1, 2, 3 });
        Assert.DoesNotThrow(() => lightsSwitch.Reset());
    }

    [Test]
    public void LightsSwitch_HandleInfiniteBlackouts()
    {
        var lightsSwitch = new LightsSwitch(10, new List<int> { -2 });
        Assert.IsTrue(lightsSwitch.LightStatus(0, 1));
        Assert.IsFalse(lightsSwitch.LightStatus(2, 1));
        Assert.IsTrue(lightsSwitch.LightStatus(4, 1));
    }

    [Test]
    public void LightsSwitch_ResetBehavior()
    {
        var lightsSwitch = new LightsSwitch(10, new List<int> { 1, 3 });
        lightsSwitch.LightStatus(1, 1);
        lightsSwitch.Reset();
        Assert.IsTrue(lightsSwitch.LightStatus(0, 1));
    }

    [Test]
    public void LightsSwitch_HandleEmptyBlackoutList()
    {
        var lightsSwitch = new LightsSwitch(10, new List<int>());
        Assert.IsTrue(lightsSwitch.LightStatus(0, 1));
        Assert.IsTrue(lightsSwitch.LightStatus(5, 1));
    }

    [Test]
    public void LightsSwitch_HandleDifferentAgentDecisionIntervals()
    {
        var lightsSwitch = new LightsSwitch(20, new List<int> { 2, 4 });
        Assert.IsTrue(lightsSwitch.LightStatus(0, 2));
        Assert.IsFalse(lightsSwitch.LightStatus(4, 2));
        Assert.IsTrue(lightsSwitch.LightStatus(8, 2));
    }

    [Test]
    public void LightsSwitch_ThrowExceptionForInvalidBlackoutSequence()
    {
        Assert.Throws<ArgumentException>(() => new LightsSwitch(20, new List<int> { 10, 1 }));
    }

    [Test]
    public void LightsSwitch_HandleInfiniteBlackoutsWithNegativeNumber()
    {
        var lightsSwitch = new LightsSwitch(100, new List<int> { -20 });
        Assert.IsTrue(lightsSwitch.LightStatus(0, 1));
        Assert.IsTrue(lightsSwitch.LightStatus(19, 1));
        Assert.IsFalse(lightsSwitch.LightStatus(20, 1));
        Assert.IsFalse(lightsSwitch.LightStatus(39, 1));
        Assert.IsTrue(lightsSwitch.LightStatus(40, 1));
        Assert.IsTrue(lightsSwitch.LightStatus(59, 1));
        Assert.IsFalse(lightsSwitch.LightStatus(60, 1));
        Assert.IsFalse(lightsSwitch.LightStatus(79, 1));
        Assert.IsTrue(lightsSwitch.LightStatus(80, 1));
    }

}