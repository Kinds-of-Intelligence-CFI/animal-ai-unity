using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;
using Unity.MLAgents.Actuators;

public class DespawnObjectTests
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
    public IEnumerator TestdespawnReward()
    {
        yield return TestDespawnObject(
            "GoodGoal"
        );
    }

    [UnityTest]
    public IEnumerator TestdespawnWall()
    {
        yield return TestDespawnObject(
            "Wall"
        );
    }

    private IEnumerator TestDespawnObject(
        string objectTypeName
    )
    {
        yield return null;

        initialiseTestAgent();
        DespawnObject operation = GetDespawnObjectOperation(objectTypeName);

        // Check arena initially has the object
        GameObject[] objects_initial = FindGameObjectsByName(objectTypeName);
        Assert.IsTrue(objects_initial.Length == 1, $"Exactly one {objectTypeName} should be present initially, got " + objects_initial.Length);

        // Find the spawned object
        GameObject objectToDespawn = objects_initial[0];

        Assert.IsNotNull(objectToDespawn, $"Spawned {objectTypeName.ToLower()} should not be null");
        Debug.Log($"Expected {objectTypeName.ToLower()} position: {expectedObjectPositionCentre}, Actual {objectTypeName.ToLower()} position: {objectToDespawn.transform.position}");
        Assert.IsTrue(Vector3.Distance(objectToDespawn.transform.position, expectedObjectPositionCentre) < 0.5f,
                     $"{objectTypeName} should spawn at expected position behind agent. Expected: {expectedObjectPositionCentre}, Actual: {objectToDespawn.transform.position}");

        operation.execute();

        // Wait a frame for the object to be destroyed
        yield return null;

        // Find the spawned object
        GameObject[] objects = FindGameObjectsByName(objectTypeName);
        Assert.IsTrue(objects.Length == 0, $"Exactly zero {objectTypeName}s should be present after despawn, got " + objects.Length);
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

    private DespawnObject GetDespawnObjectOperation(string objectTypeName)
    {
        // Create a temporary GameObject to host the SpawnReward operation
        GameObject tempOperationHost = new GameObject("TempOperationHost");
        DespawnObject despawnOperation = tempOperationHost.AddComponent<DespawnObject>();

        // Initialize with dummy details since we're not attached to a specific object
        AttachedObjectDetails details = new AttachedObjectDetails("test", Vector3.zero);

        // Use the provided function to add objects to the spawn operation
        YAMLDefs.Item item = new YAMLDefs.Item
        {
            name = objectTypeName,
            positions = new List<Vector3> { new Vector3(20, 0, 15) },
            sizes = new List<Vector3> { new Vector3(1, 1, 1) },
        };
        despawnOperation.spawnable = item;

        despawnOperation.initialise(details);
        return despawnOperation;
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
