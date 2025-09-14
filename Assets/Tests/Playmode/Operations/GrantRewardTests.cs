using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;

/// <summary>
/// Playmode tests for the GrantReward operation class.
/// </summary>
public class GrantRewardTests
{
    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator Execute_GrantsAReward()
    {
        yield return null;

        TrainingAgent agent = GameObject.FindAnyObjectByType<TrainingAgent>();
        Assert.IsNotNull(agent, "TrainingAgent should be found in the scene");

        float initialHealth = agent.health;

        var operation = CreateGrantRewardOperation();

        operation.execute();

        float expectedHealth = initialHealth + (operation.reward * 100);
        Assert.AreEqual(expectedHealth, agent.health, 0.001f,
            $"Agent health should be {expectedHealth} after granting reward of {operation.reward}");
    }

    private GrantReward CreateGrantRewardOperation()
    {
        var gameObject = new GameObject("TestGrantRewardOperation");
        var operation = gameObject.AddComponent<GrantReward>();
        operation.reward = -0.5f;
        return operation;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any test GameObjects
        var testObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in testObjects)
        {
            if (go.name.Contains("TestGrantRewardOperation"))
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}