using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// Tests for the DecayGoal class.
/// </summary>
public class DecayGoalTests
{
    private GameObject _goalObject;
    private DecayGoal _decayGoal;

    [SetUp]
    public void SetUp()
    {
        _goalObject = new GameObject("DecayGoal");

        var meshRenderer = _goalObject.AddComponent<MeshRenderer>();

        meshRenderer.materials = new Material[3];

        _decayGoal = _goalObject.AddComponent<DecayGoal>();

        /* Set initial values */
        _decayGoal.initialReward = 5.0f;
        _decayGoal.finalReward = 0.0f;
        _decayGoal.decayRate = -0.1f;
        _decayGoal.initialColour = Color.green;
        _decayGoal.finalColour = Color.red;
        _decayGoal.fixedFrameDelay = 0;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_goalObject);
    }

    [UnityTest]
    public IEnumerator TestDecayOverTime()
    {
        yield return new WaitForSeconds(_decayGoal.fixedFrameDelay * Time.fixedDeltaTime);

        _decayGoal.isDecaying = true;

        for (int i = 0; i < 50; i++)
        {
            yield return new WaitForFixedUpdate();
            _decayGoal.UpdateGoal(_decayGoal.decayRate);
        }

        Assert.Less(_decayGoal.reward, _decayGoal.initialReward);
        Assert.GreaterOrEqual(_decayGoal.reward, _decayGoal.finalReward);
    }

    [UnityTest]
    public IEnumerator TestFinalDecayReached()
    {
        _decayGoal.isDecaying = true;

        while (!_decayGoal.HasFinalDecayBeenReached())
        {
            yield return new WaitForFixedUpdate();
            _decayGoal.UpdateGoal(_decayGoal.decayRate);
        }

        Assert.AreEqual(_decayGoal.reward, _decayGoal.finalReward);
    }

    [UnityTest]
    public IEnumerator TestStopDecay()
    {
        _decayGoal.initialReward = 1.0f;
        _decayGoal.finalReward = 0.0f;
        _decayGoal.decayRate = -0.01f;

        _decayGoal.StartDecay();

        for (int i = 0; i < 50; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        float rewardBeforeStopping = _decayGoal.reward;
        Debug.Log("Reward before stopping decay: " + rewardBeforeStopping);

        _decayGoal.StopDecay();

        /* Record the reward immediately after stopping decay */
        float rewardAfterStopping = _decayGoal.reward;
        Debug.Log("Reward after stopping decay: " + rewardAfterStopping);

        for (int i = 0; i < 50; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        /* Final reward should be the same as when decay was stopped */
        float finalReward = _decayGoal.reward;
        Debug.Log("Final reward after additional updates: " + finalReward);

        Assert.AreEqual(
            rewardBeforeStopping,
            rewardAfterStopping,
            "Reward should not change immediately after stopping decay"
        );
        Assert.AreEqual(
            rewardAfterStopping,
            finalReward,
            "Reward should remain constant after decay is stopped"
        );
    }
}
