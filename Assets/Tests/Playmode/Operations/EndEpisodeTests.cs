using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;

/// <summary>
/// Playmode tests for the EndEpisode operation class.
/// </summary>
public class EndEpisodeTests
{
    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    private IEnumerator LoadArenaConfiguration()
    {
        yield return null;

        AAI3EnvironmentManager environmentManager = GameObject.FindAnyObjectByType<AAI3EnvironmentManager>();
        Assert.IsNotNull(environmentManager, "AAI3EnvironmentManager should be found in the scene");

        environmentManager.configFile = "test_configs/arenas_multiple_cycling_test";
        environmentManager.LoadYAMLFileInEditor();
        yield return null; // Allow one frame for configuration to be processed
    }

    [UnityTest]
    public IEnumerator Execute_EndsTheEpisode()
    {
        yield return LoadArenaConfiguration();

        TrainingArena arena = GameObject.FindAnyObjectByType<TrainingArena>();
        Assert.IsNotNull(arena, "TrainingArena should be found in the scene");

        Assert.AreEqual(0, arena.arenaID, "Arena should start at ID 0");

        var operation = CreateEndEpisodeOperation();

        operation.execute();

        Assert.AreEqual(1, arena.arenaID, "Arena should advance to ID 1 after EndEpisode operation");
    }

    private EndEpisode CreateEndEpisodeOperation()
    {
        var gameObject = new GameObject("TestEndEpisodeOperation");
        var operation = gameObject.AddComponent<EndEpisode>();
        operation.reward = 1;
        return operation;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any test GameObjects
        var testObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in testObjects)
        {
            if (go.name.Contains("TestEndEpisodeOperation"))
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}