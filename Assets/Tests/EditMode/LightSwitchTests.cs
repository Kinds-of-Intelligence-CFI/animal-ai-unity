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
    public void IncrementValueOnMoveNext()
    {
        int initialValue = 3;
        var enumerator = new InfiniteEnumerator(initialValue);

        enumerator.MoveNext();
        int currentValue = enumerator.Current;

        Assert.AreEqual(initialValue, currentValue);
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
    public void KeepIncrementingOnMultipleMoveNext()
    {
        int initialValue = 2;
        var enumerator = new InfiniteEnumerator(initialValue);

        enumerator.MoveNext();
        Assert.AreEqual(2, enumerator.Current);

        enumerator.MoveNext();
        Assert.AreEqual(4, enumerator.Current);

        enumerator.MoveNext();
        Assert.AreEqual(6, enumerator.Current);
    }

    [Test]
    public void ThrowExceptionForNegativeEpisodeLength()
    {
        Assert.Throws<ArgumentException>(() => new LightsSwitch(-1, new List<int> { 1 }));
    }

    [Test]
    public void ThrowExceptionForInvalidBlackoutInterval()
    {
        Assert.Throws<ArgumentException>(() => new LightsSwitch(5, new List<int> { -1, 6 }));
    }

    [Test]
    public void ReturnTrueIfNoBlackouts()
    {
        var lightsSwitch = new LightsSwitch(10, new List<int>());

        bool lightStatus = lightsSwitch.LightStatus(0, 1);

        Assert.IsTrue(lightStatus);
    }

    [Test]
    public void HandleMultipleBlackoutsProperly()
    {
        var blackouts = new List<int> { 1, 2, 3 };
        var lightsSwitch = new LightsSwitch(10, blackouts);

        Assert.IsTrue(lightsSwitch.LightStatus(0, 1));
        Assert.IsFalse(lightsSwitch.LightStatus(1, 1));
        Assert.IsTrue(lightsSwitch.LightStatus(2, 1));
        Assert.IsFalse(lightsSwitch.LightStatus(3, 1));
    }

    [Test]
    public void ThrowExceptionForInvalidStepOrAgentDecisionInterval()
    {
        var lightsSwitch = new LightsSwitch(10, new List<int> { 1 });

        Assert.Throws<ArgumentException>(() => lightsSwitch.LightStatus(-1, 1));
        Assert.Throws<ArgumentException>(() => lightsSwitch.LightStatus(1, 0));
    }
}
