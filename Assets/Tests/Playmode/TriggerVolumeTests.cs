using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Operations;

/// <summary>
/// Playmode tests for the parts of TriggerVolume that need a physics scene: trigger events from real agents,
/// the line-of-sight rule, and the per-step sweep of overlapping agents. The volume under test is a
/// standalone trigger sphere, not a DataZone or an agent's zone; the agents are the scene's, since a
/// TrainingAgent can't exist without an arena. The bookkeeping itself is unit-tested in the EditMode
/// TriggerVolumeTests.
/// </summary>
public class TriggerVolumeTests
{
    private const string TestObjectPrefix = "TestTriggerVolume";

    private AAI3EnvironmentManager _environmentManager;
    private TrainingArena _arena;

    private class RecordingOperation : Operation
    {
        public readonly List<TrainingAgent> Executed = new List<TrainingAgent>();
        public readonly List<TrainingAgent> Completed = new List<TrainingAgent>();

        public override void execute(TrainingAgent triggeringAgent)
        {
            Executed.Add(triggeringAgent);
        }

        public override void completeExecution(TrainingAgent triggeringAgent)
        {
            Completed.Add(triggeringAgent);
        }
    }

    /// <summary>An operation that, like endEpisode resetting the arena, clears the volume it runs on.</summary>
    private class ClearingOperation : Operation
    {
        public int Executions = 0;

        public override void execute(TrainingAgent triggeringAgent)
        {
            Executions++;
            ((TriggerVolume)attachedObjectDetails.obj).Clear();
        }
    }

    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject testObject in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (testObject.name.StartsWith(TestObjectPrefix))
            {
                Object.DestroyImmediate(testObject);
            }
        }
    }

    /// <summary>Loads the two-agent arena (agents at (10, 20) and (30, 20)) and rebuilds with it.</summary>
    private IEnumerator LoadTwoAgents()
    {
        yield return null;
        _environmentManager = GameObject.FindAnyObjectByType<AAI3EnvironmentManager>();
        _arena = GameObject.FindAnyObjectByType<TrainingArena>();
        _environmentManager.DisablePlayerModeForTesting();
        _environmentManager.configFile = "test_configs/multi_agent_2";
        _environmentManager.LoadYAMLFileInEditor();
        yield return new WaitForFixedUpdate();
        yield return null;
        _arena.EndArenaEpisode();
        yield return null;
    }

    /// <summary>A trigger sphere of the given radius at the given spot, with one recording operation.</summary>
    private static (TriggerVolume volume, RecordingOperation operation) Sphere(
        Vector3 centre,
        float radius,
        bool requiresLineOfSight
    )
    {
        GameObject host = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        host.name = TestObjectPrefix + "Sphere";
        host.transform.position = centre;
        host.transform.localScale = Vector3.one * (2f * radius);
        host.GetComponent<SphereCollider>().isTrigger = true;
        host.GetComponent<MeshRenderer>().enabled = false;

        TriggerVolume volume = host.AddComponent<TriggerVolume>();
        volume.RequiresLineOfSight = requiresLineOfSight;
        RecordingOperation operation = new GameObject(TestObjectPrefix + "Operation").AddComponent<RecordingOperation>();
        volume.Configure("test", new List<Operation> { operation });
        return (volume, operation);
    }

    /// <summary>A solid wall: a cube of the given size at the given spot.</summary>
    private static GameObject Wall(Vector3 centre, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = TestObjectPrefix + "Wall";
        wall.transform.position = centre;
        wall.transform.localScale = size;
        return wall;
    }

    private static IEnumerator Teleport(TrainingAgent agent, float x, float z)
    {
        Rigidbody body = agent.GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        agent.transform.position = new Vector3(x, agent.transform.position.y, z);
        Physics.SyncTransforms();
        yield return SettlePhysics();
    }

    private static IEnumerator SettlePhysics(int steps = 3)
    {
        for (int i = 0; i < steps; i++)
        {
            yield return new WaitForFixedUpdate();
        }
    }

    /* The agents' centres sit at y = 0.5, so the sphere is centred there too */
    private static readonly Vector3 Centre = new Vector3(20, 0.5f, 20);

    [UnityTest]
    public IEnumerator AnAgentEnteringTheCollider_RunsTheOperations_AndLeavingCompletesThem()
    {
        yield return LoadTwoAgents();
        TrainingAgent agent = _arena.Agents[0];
        var (volume, operation) = Sphere(Centre, 3f, requiresLineOfSight: false);
        yield return SettlePhysics();
        Assert.IsFalse(volume.IsAgentInZone, "Nothing is inside yet");

        yield return Teleport(agent, 22f, 20f);
        CollectionAssert.AreEqual(new[] { agent }, operation.Executed);
        CollectionAssert.AreEquivalent(new[] { agent }, volume.AgentsInZone);

        yield return Teleport(agent, 10f, 20f);
        CollectionAssert.AreEqual(new[] { agent }, operation.Completed);
        Assert.IsFalse(volume.IsAgentInZone);
    }

    [UnityTest]
    public IEnumerator TheOwner_DoesNotTriggerTheVolume()
    {
        yield return LoadTwoAgents();
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent other = _arena.Agents[1];
        var (volume, operation) = Sphere(Centre, 3f, requiresLineOfSight: false);
        volume.Owner = owner;

        yield return Teleport(owner, 21f, 20f);
        Assert.IsEmpty(operation.Executed);

        yield return Teleport(other, 19f, 20f);
        CollectionAssert.AreEqual(new[] { other }, operation.Executed);
    }

    [UnityTest]
    public IEnumerator WithoutLineOfSight_AWallInTheWayDoesNotMatter()
    {
        yield return LoadTwoAgents();
        TrainingAgent agent = _arena.Agents[0];
        var (volume, _) = Sphere(Centre, 4f, requiresLineOfSight: false);
        Wall(new Vector3(21, 1.5f, 20), new Vector3(0.5f, 3, 4));

        yield return Teleport(agent, 23f, 20f);

        Assert.IsFalse(volume.HasLineOfSightTo(agent), "The wall is between the centre and the agent");
        CollectionAssert.AreEquivalent(new[] { agent }, volume.AgentsInZone, "Pure overlap: the agent is in the zone");
    }

    [UnityTest]
    public IEnumerator WithLineOfSight_AWallBlocks_UntilItIsRemoved()
    {
        yield return LoadTwoAgents();
        TrainingAgent agent = _arena.Agents[0];
        var (volume, operation) = Sphere(Centre, 4f, requiresLineOfSight: true);
        GameObject wall = Wall(new Vector3(21, 1.5f, 20), new Vector3(0.5f, 3, 4));

        yield return Teleport(agent, 23f, 20f);
        Assert.IsFalse(volume.HasLineOfSightTo(agent));
        Assert.IsFalse(volume.IsAgentInZone, "Overlapping but out of sight is not in the zone");
        Assert.IsEmpty(operation.Executed);

        Object.Destroy(wall);
        yield return SettlePhysics();
        Assert.IsTrue(volume.HasLineOfSightTo(agent));
        CollectionAssert.AreEqual(new[] { agent }, operation.Executed, "The sweep notices the wall has gone");

        /* And the other way round: a wall appearing while the agent stays put counts as leaving */
        Wall(new Vector3(21, 1.5f, 20), new Vector3(0.5f, 3, 4));
        yield return SettlePhysics();
        CollectionAssert.AreEqual(new[] { agent }, operation.Completed);
        Assert.IsFalse(volume.IsAgentInZone);
    }

    [UnityTest]
    public IEnumerator WithLineOfSight_AnotherAgentInTheWayBlocks()
    {
        yield return LoadTwoAgents();
        TrainingAgent near = _arena.Agents[0];
        TrainingAgent far = _arena.Agents[1];
        var (volume, _) = Sphere(Centre, 6f, requiresLineOfSight: true);

        yield return Teleport(near, 22f, 20f);
        yield return Teleport(far, 24f, 20f);

        Assert.IsTrue(volume.HasLineOfSightTo(near));
        Assert.IsFalse(volume.HasLineOfSightTo(far), "The nearer agent's body is on the line to the farther one");
        CollectionAssert.AreEquivalent(new[] { near }, volume.AgentsInZone);
    }

    [UnityTest]
    public IEnumerator TheSweep_SurvivesAnOperationClearingTheVolumeMidLoop()
    {
        /* Two agents overlap but are out of sight behind a wall. Removing the wall makes one physics-step
           sweep evaluate both; the first one's operation clears the volume while the sweep is still going,
           as an endEpisode resetting the arena would. The loop must not throw. */
        yield return LoadTwoAgents();
        TrainingAgent first = _arena.Agents[0];
        TrainingAgent second = _arena.Agents[1];
        var (volume, _) = Sphere(Centre, 6f, requiresLineOfSight: true);
        ClearingOperation clearing = new GameObject(TestObjectPrefix + "Clearing").AddComponent<ClearingOperation>();
        volume.Configure("clearing", new List<Operation> { clearing });
        GameObject wall = Wall(new Vector3(22, 1.5f, 20), new Vector3(0.5f, 3, 8));

        yield return Teleport(first, 24f, 18f);
        yield return Teleport(second, 24f, 22f);
        Assert.IsFalse(volume.IsAgentInZone, "Both are behind the wall");

        Object.Destroy(wall);
        yield return SettlePhysics();

        Assert.GreaterOrEqual(clearing.Executions, 1, "At least one agent came into sight and ran the operation");
        /* An exception in the sweep would be logged and fail the test, so reaching here means it didn't throw */
    }
}
