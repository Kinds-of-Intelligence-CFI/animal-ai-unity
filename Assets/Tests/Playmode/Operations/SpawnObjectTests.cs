using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;
using Unity.MLAgents.Actuators;
using ArenasParameters;

public class SpawnObjectTests
{
    private TrainingAgent agent;
    private Vector3 initialAgentPosition;
    private Vector3 expectedObjectPositionCentre;
    private Rigidbody agentRigidBody;

    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator TestSpawnReward()
    {
        yield return TestSpawnObject(
            AddGoodGoalToSpawnOperation,
            () => GameObject.FindGameObjectsWithTag("goodGoal"),
            "GoodGoal",
            true
        );
    }

    [UnityTest]
    public IEnumerator TestSpawnWall()
    {
        yield return TestSpawnObject(
            AddWallToSpawnOperation,
            () => FindGameObjectsByName("Wall"),
            "Wall",
            false
        );
    }

    private IEnumerator TestSpawnObject(
        System.Action<SpawnObject> addObjectToSpawnOperation,
        System.Func<GameObject[]> findObjectsMethod,
        string objectTypeName,
        bool testEpisodeCompletion)
    {
        yield return null;

        initialiseTestAgent();

        // Check arena is empty initially
        GameObject[] objects_initial = findObjectsMethod();
        Assert.IsTrue(objects_initial.Length == 0, $"Exactly zero {objectTypeName}s should be present initially, got " + objects_initial.Length);

        SpawnObject operation = GetSpawnObjectOperation(addObjectToSpawnOperation);
        operation.execute();

        // Find the spawned object
        GameObject[] objects = findObjectsMethod();
        Assert.IsTrue(objects.Length == 1, $"Exactly one {objectTypeName} should be spawned, got " + objects.Length);

        // Find the spawned object
        GameObject spawnedObject = objects[0];

        Assert.IsNotNull(spawnedObject, $"Spawned {objectTypeName.ToLower()} should not be null");
        Debug.Log($"Expected {objectTypeName.ToLower()} position: {expectedObjectPositionCentre}, Actual {objectTypeName.ToLower()} position: {spawnedObject.transform.position}");
        Assert.IsTrue(Vector3.Distance(spawnedObject.transform.position, expectedObjectPositionCentre) < 0.5f,
                     $"{objectTypeName} should spawn at expected position behind agent. Expected: {expectedObjectPositionCentre}, Actual: {spawnedObject.transform.position}");

        if (testEpisodeCompletion)
        {
            yield return MoveAgentBackward();

            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(agent.CompletedEpisodes > 0, "Episode should end after collecting reward");

            yield return new WaitForSeconds(0.5f);

            // Check that reward is despawned at the end of the episode
            GameObject[] objects_next_ep = findObjectsMethod();
            Assert.IsTrue(objects_next_ep.Length == 0, $"{objectTypeName} should be despawned at the end of the episode, got " + objects_next_ep.Length);
        }
    }

    private void initialiseTestAgent()
    {
        agent = GameObject.FindObjectOfType<TrainingAgent>();
        Assert.IsNotNull(agent, "TrainingAgent should be found in the scene");

        agentRigidBody = agent.GetComponent<Rigidbody>();
        Assert.IsNotNull(agentRigidBody, "Agent should have a Rigidbody component");

        initialAgentPosition = new Vector3(20, 0, 20);
        expectedObjectPositionCentre = new Vector3(20, 0.5f, 15);

        agent.transform.position = initialAgentPosition;
        agent.transform.rotation = Quaternion.identity;
        agentRigidBody.linearVelocity = Vector3.zero;
        agentRigidBody.angularVelocity = Vector3.zero;
    }

    private SpawnObject GetSpawnObjectOperation(Action<SpawnObject> addObjectToSpawnOperation)
    {
        // Create a temporary GameObject to host the SpawnReward operation
        GameObject tempOperationHost = new GameObject("TempOperationHost");
        SpawnObject spawnOperation = tempOperationHost.AddComponent<SpawnObject>();

        // Initialize with dummy details since we're not attached to a specific object
        AttachedObjectDetails details = new AttachedObjectDetails("test", Vector3.zero);
        
        // Use the provided function to add objects to the spawn operation
        addObjectToSpawnOperation(spawnOperation);
        
        spawnOperation.Initialize(details);
        return spawnOperation;
    }

    private void AddGoodGoalToSpawnOperation(SpawnObject spawnOperation)
    {
        // Attach a GoodGoal to the spawn operation
        GameObject goodGoalPrefab = Resources.Load<GameObject>("GoodGoal");
        Spawnable spawnable;
        if (goodGoalPrefab != null)
        {
            spawnable = new Spawnable(goodGoalPrefab);
            spawnOperation.spawnable = spawnable;
            spawnable.positions = new List<Vector3> { new Vector3(20, 0, 15) };
            spawnable.sizes = new List<Vector3> { new Vector3(1, 1, 1) };
        }
        else
        {
            Debug.LogError("Failed to load GoodGoal prefab from Resources");
        }
    }

    private void AddWallToSpawnOperation(SpawnObject spawnOperation)
    {
        string wallYaml = @"
        !ArenaConfig
        arenas:
          0: !Arena
            items:
            - !Item
              name: Wall
              positions:
              - !Vector3 {x: 20, y: 0, z: 15}
              rotations: [0]
              sizes:
              - !Vector3 {x: 1, y: 1, z: 1}
              colors:
              - !RGB {r: 153, g: 153, b: 153}";

        YAMLDefs.YAMLReader yamlReader = new YAMLDefs.YAMLReader();
        YAMLDefs.ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(wallYaml);
        YAMLDefs.Item wallItem = arenaConfig.arenas[0].items[0];

        Spawnable spawnable = new Spawnable(wallItem);
        
        // Create a simple test wall GameObject
        GameObject wallGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallGameObject.name = "Wall";
        
        // Add necessary components for the spawning system
        wallGameObject.AddComponent<Prefab>();
        
        spawnable.gameObject = wallGameObject;
        spawnOperation.spawnable = spawnable;
    }

    private IEnumerator MoveAgentBackward()
    {
        float startTime = Time.time;
        float moveTime = 1.5f;

        while (Vector3.Distance(agent.transform.position, expectedObjectPositionCentre) > 0.5f &&
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

    private GameObject[] FindGameObjectsByName(string name)
    {
        GameObject spawnedObjectsHolder = GameObject.Find("SpawnedObjectsHolder_Instance");
        if (spawnedObjectsHolder == null)
        {
            return new GameObject[0]; // Return empty array if no spawned objects holder exists
        }
        
        Transform[] allChildren = spawnedObjectsHolder.GetComponentsInChildren<Transform>();
        return System.Array.FindAll(allChildren, t => t.name.Contains(name) && t != spawnedObjectsHolder.transform)
               .Select(t => t.gameObject).ToArray();
    }

    // TODO: For switch to SpawnObject instead of SpawnGoal
    // Different kinds of items are spawned
    // OnRewardSpawned is only called when the item is a reward
}
