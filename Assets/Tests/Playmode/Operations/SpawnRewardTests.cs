using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;
using Unity.MLAgents.Actuators;

public class SpawnRewardTests
{
    private TrainingAgent agent;
    private Vector3 initialAgentPosition;
    private Vector3 expectedRewardPosition;
    private Rigidbody agentRigidBody;

    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator TestSpawnReward()
    {
        yield return null;

        initialiseTestAgent();

        // Check arena is empty initially
        GameObject[] goodGoals_initial = GameObject.FindGameObjectsWithTag("goodGoal");
        Assert.IsTrue(goodGoals_initial.Length == 0, "Exactly zero GoodGoals should be present initially, got " + goodGoals_initial.Length);

        SpawnReward operation = GetSpawnGoalOperation();
        operation.execute();

        // Find the spawned GoodGoal object
        GameObject[] goodGoals = GameObject.FindGameObjectsWithTag("goodGoal");
        Assert.IsTrue(goodGoals.Length == 1, "Exactly one GoodGoal should be spawned, got " + goodGoals.Length);

        // Find the GoodGoal closest to the expected position
        GameObject spawnedReward = goodGoals[0];

        Assert.IsNotNull(spawnedReward, "Spawned reward should not be null");
        Assert.IsTrue(Vector3.Distance(spawnedReward.transform.position, expectedRewardPosition) < 0.5f,
                     "Reward should spawn at expected position behind agent");

        yield return MoveAgentBackward();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(agent.CompletedEpisodes > 0, "Episode should end after collecting reward");

        yield return new WaitForSeconds(0.5f);

        // Find the spawned GoodGoal object
        GameObject[] goodGoals_next_ep = GameObject.FindGameObjectsWithTag("goodGoal");
        Assert.IsTrue(goodGoals_next_ep.Length == 0, "Reward should be despawned at the end of the episode, got " + goodGoals_next_ep.Length);
    }

    [UnityTest]
    public IEnumerator TestShouldNotSpawnRewardIfMaxSpawnsReached()
    {
        yield return null;

        initialiseTestAgent();

        // Check arena is empty initially
        GameObject[] goodGoals_initial = GameObject.FindGameObjectsWithTag("goodGoal");
        Assert.IsTrue(goodGoals_initial.Length == 0, "Exactly zero GoodGoals should be present initially, got " + goodGoals_initial.Length);

        SpawnReward operation = GetSpawnGoalOperation(1);
        operation.execute();

        // Find the spawned GoodGoal object
        GameObject[] goodGoals = GameObject.FindGameObjectsWithTag("goodGoal");
        Assert.IsTrue(goodGoals.Length == 1, "Exactly one GoodGoal should be spawned, got " + goodGoals.Length);

        operation.execute();

        // Find the spawned GoodGoal object
        GameObject[] goodGoals_second_invocation = GameObject.FindGameObjectsWithTag("goodGoal");
        Assert.IsTrue(goodGoals_second_invocation.Length == 1, "Exactly one GoodGoal should be spawned, got " + goodGoals_second_invocation.Length);
    }

    private void initialiseTestAgent()
    {
        agent = GameObject.FindObjectOfType<TrainingAgent>();
        Assert.IsNotNull(agent, "TrainingAgent should be found in the scene");

        agentRigidBody = agent.GetComponent<Rigidbody>();
        Assert.IsNotNull(agentRigidBody, "Agent should have a Rigidbody component");

        initialAgentPosition = new Vector3(20, 0, 20);
        expectedRewardPosition = new Vector3(20, 0, 15);

        agent.transform.position = initialAgentPosition;
        agent.transform.rotation = Quaternion.identity;
        agentRigidBody.linearVelocity = Vector3.zero;
        agentRigidBody.angularVelocity = Vector3.zero;
    }

    private SpawnReward GetSpawnGoalOperation(int? max_spawns = null)
    {
        // Create a temporary GameObject to host the SpawnReward operation
        GameObject tempOperationHost = new GameObject("TempOperationHost");
        SpawnReward spawnOperation = tempOperationHost.AddComponent<SpawnReward>();

        // Initialize with dummy details since we're not attached to a specific object
        AttachedObjectDetails details = new AttachedObjectDetails("test", Vector3.zero);
        spawnOperation.rewardNames = new System.Collections.Generic.List<string> { "GoodGoal" };
        spawnOperation.rewardSpawnPos = expectedRewardPosition;
        spawnOperation.rewardWeights = new System.Collections.Generic.List<float> { 1 };
        if (max_spawns != null)
        {
            spawnOperation.MaxRewardCounts = new System.Collections.Generic.List<int> { max_spawns.Value };
        }
        spawnOperation.Initialize(details);
        return spawnOperation;
    }

    private IEnumerator MoveAgentBackward()
    {
        float startTime = Time.time;
        float moveTime = 1.5f;

        while (Vector3.Distance(agent.transform.position, expectedRewardPosition) > 0.5f &&
               Time.time - startTime < moveTime)
        {
            SimulateAgentMovement(2, 0);
            yield return new WaitForFixedUpdate();
        }

        SimulateAgentMovement(0, 0);
        yield return new WaitForSeconds(0.1f);
    }

    private void SimulateAgentMovement(int actionForward, int actionRotate)
    {
        ActionBuffers actionBuffers = new ActionBuffers(
            new float[] { },
            new int[] { actionForward, actionRotate }
        );

        agent.OnActionReceived(actionBuffers);
    }

    // Different kinds of items are spawned
    // OnRewardSpawned is only called when the item is a reward
}
