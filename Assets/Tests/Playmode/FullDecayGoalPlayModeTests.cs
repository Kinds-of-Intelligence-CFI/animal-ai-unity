using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Tests for the FullDecayGoal class.
/// </summary>
public class FullDecayGoalPlayModeTests
{
    private GameObject _goalObject;
    private FullDecayGoal _fullDecayGoal;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        _goalObject = new GameObject("FullDecayGoal");

        _goalObject.AddComponent<MeshRenderer>();
        _goalObject.AddComponent<MeshFilter>();
        _fullDecayGoal = _goalObject.AddComponent<FullDecayGoal>();

        _fullDecayGoal.initialReward = 10f;
        _fullDecayGoal.finalReward = 0f;
        _fullDecayGoal.decayRate = -0.1f;

        _fullDecayGoal.StartDecay(reset: true);

        yield return null;
    }

    [UnityTest]
    public IEnumerator DecayBehavior_ShouldDecayRewardOverTime()
    {
        float initialReward = _fullDecayGoal.reward;
        _fullDecayGoal.StartDecay(reset: true);
        yield return new WaitForSeconds(1.0f);
        Assert.Less(_fullDecayGoal.reward, initialReward, "Reward did not decay over time.");
    }

    [UnityTest]
    public IEnumerator EpisodeEndCondition_ShouldSetCorrectly()
    {
        _fullDecayGoal.useGoodEpisodeEndThreshold = true;
        _fullDecayGoal.goodRewardEndThreshold = 7f;

        _fullDecayGoal.reward = 8f;
        _fullDecayGoal.FixedUpdate();
        Assert.IsFalse(_fullDecayGoal.isMulti, "Episode should have ended.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator StartDecay_ShouldBeginDecay()
    {
        _fullDecayGoal.StartDecay(reset: true);
        Assert.IsTrue(_fullDecayGoal.isDecaying, "Decay did not start.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator StopDecay_ShouldStopDecay()
    {
        _fullDecayGoal.StartDecay();
        _fullDecayGoal.StopDecay(reset: true);
        Assert.IsFalse(_fullDecayGoal.isDecaying, "Decay did not stop.");
        Assert.AreEqual(
            _fullDecayGoal.reward,
            _fullDecayGoal.finalReward,
            "Reward did not reset to final reward."
        );
        yield return null;
    }

    [UnityTest]
    public IEnumerator ColorUpdate_ShouldUpdateColorBasedOnReward()
    {
        float p = 0.25f;
        _fullDecayGoal.UpdateColour(p);

        Color expectedColor;
        if (_fullDecayGoal.useMiddle && p < _fullDecayGoal.middleDecayProportion)
        {
            p = p / _fullDecayGoal.middleDecayProportion;
            expectedColor = Color.Lerp(_fullDecayGoal.badColour, _fullDecayGoal.neutralColour, p);
        }
        else if (_fullDecayGoal.useMiddle)
        {
            p =
                (p - _fullDecayGoal.middleDecayProportion)
                / (1 - _fullDecayGoal.middleDecayProportion);
            expectedColor = Color.Lerp(_fullDecayGoal.neutralColour, _fullDecayGoal.goodColour, p);
        }
        else
        {
            expectedColor = Color.Lerp(_fullDecayGoal.badColour, _fullDecayGoal.goodColour, p);
        }

        Color actualColor = _fullDecayGoal
            .GetComponent<MeshRenderer>()
            .material.GetColor("_EmissionColor");

        Assert.IsTrue(
            AreColorsEqualWithTolerance(expectedColor, actualColor, 0.01f),
            $"Color did not update correctly based on reward. Expected: {expectedColor}, But was: {actualColor}"
        );

        yield return null;
    }

    private bool AreColorsEqualWithTolerance(Color color1, Color color2, float tolerance)
    {
        return Mathf.Abs(color1.r - color2.r) < tolerance
            && Mathf.Abs(color1.g - color2.g) < tolerance
            && Mathf.Abs(color1.b - color2.b) < tolerance
            && Mathf.Abs(color1.a - color2.a) < tolerance;
    }
}
