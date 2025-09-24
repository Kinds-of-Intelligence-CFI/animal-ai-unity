using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;

/// <summary>
/// Playmode tests for the GrantReward operation class.
/// </summary>
public class GrantContinuousRewardTests
{
    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator Execute_GrantsARewardOverTime()
    {
        yield return null;

        TrainingAgent agent = GameObject.FindAnyObjectByType<TrainingAgent>();
        Assert.IsNotNull(agent, "TrainingAgent should be found in the scene");

        var operation = CreateGrantContinuousRewardOperation();

        operation.execute();
        yield return new WaitForSeconds(0.5f);
        operation.completeExecution();

        float expectedHealth = 100f;
        Assert.AreEqual(expectedHealth, agent.health, 0.01f,
            $"Agent health should be {expectedHealth}, as health should reach its cap after granting reward of {operation.rewardPerStep}, ({agent.GetCumulativeReward()})");
        float expectedReward = 2.58f;
        Assert.AreEqual(expectedReward, agent.GetCumulativeReward(), 0.01f,
            $"Agent reward should be {expectedReward}, got {agent.GetCumulativeReward()}");
    }

    private GrantContinuousReward CreateGrantContinuousRewardOperation()
    {
        var gameObject = new GameObject("TestGrantContinuousRewardOperation");
        var operation = gameObject.AddComponent<GrantContinuousReward>();
        var dataZone = gameObject.AddComponent<DataZone>();
        operation.initialise(new AttachedObjectDetails
                {
                    obj = dataZone,
                    ID = "dummy-id",
                    location = new Vector3()
                });
        operation.rewardPerStep = 0.1f;
        return operation;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any test GameObjects
        var testObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in testObjects)
        {
            if (go.name.Contains("TestGrantContinuousRewardOperation"))
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}