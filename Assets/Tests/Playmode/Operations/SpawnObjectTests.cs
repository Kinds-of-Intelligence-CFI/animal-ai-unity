using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;
using Unity.MLAgents.Actuators;

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
            "GoodGoal",
            true
        );
    }

    [UnityTest]
    public IEnumerator TestSpawnWall()
    {
        yield return TestSpawnObject(
            "Wall",
            false
        );
    }

    private IEnumerator TestSpawnObject(
        string objectTypeName,
        bool testEpisodeCompletion)
    {
        yield return null;

        initialiseTestAgent();

        // Check arena is empty initially
        GameObject[] objects_initial = FindGameObjectsByName(objectTypeName);
        Assert.IsTrue(objects_initial.Length == 0, $"Exactly zero {objectTypeName}s should be present initially, got " + objects_initial.Length);

        SpawnObject operation = GetSpawnObjectOperation(objectTypeName);//addObjectToSpawnOperation);
        operation.execute();

        // Find the spawned object
        GameObject[] objects = FindGameObjectsByName(objectTypeName);
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
            GameObject[] objects_next_ep = FindGameObjectsByName(objectTypeName);
            Assert.IsTrue(objects_next_ep.Length == 0, $"{objectTypeName} should be despawned at the end of the episode, got " + objects_next_ep.Length);
        }
    }

    private void initialiseTestAgent()
    {
        agent = GameObject.FindAnyObjectByType<TrainingAgent>();
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

    private SpawnObject GetSpawnObjectOperation(string objectTypeName)//Action<SpawnObject> addObjectToSpawnOperation)
    {
        // Create a temporary GameObject to host the SpawnReward operation
        GameObject tempOperationHost = new GameObject("TempOperationHost");
        SpawnObject spawnOperation = tempOperationHost.AddComponent<SpawnObject>();

        // Initialize with dummy details since we're not attached to a specific object
        AttachedObjectDetails details = new AttachedObjectDetails{
            obj = spawnOperation,
            ID = "test",
            location = Vector3.zero
        };
        
        // Use the provided function to add objects to the spawn operation
        // addObjectToSpawnOperation(spawnOperation);
        YAMLDefs.Item item = new YAMLDefs.Item
        {
            name = objectTypeName,
            positions = new List<Vector3> { new Vector3(20, 0, 15) },
            sizes = new List<Vector3> { new Vector3(1, 1, 1) },
        };
        spawnOperation.spawnable = item;
        
        spawnOperation.initialise(details);
        return spawnOperation;
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
        GameObject spawnedObjectsHolder = GameObject.FindGameObjectWithTag("spawnedObjects");
        if (spawnedObjectsHolder == null)
        {
            return new GameObject[0]; // Return empty array if no spawned objects holder exists
        }
        
        Transform[] allChildren = spawnedObjectsHolder.GetComponentsInChildren<Transform>();
        return System.Array.FindAll(allChildren, t => t.name.Contains(name) && t != spawnedObjectsHolder.transform)
               .Select(t => t.gameObject).ToArray();
    }
}
