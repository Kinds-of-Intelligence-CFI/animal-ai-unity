using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.MLAgents.Sensors;
using Operations;

/// <summary>
/// Playmode tests for agent zones (the Agent item's `zone` block) and the TriggerVolume they share with
/// DataZones: the zone object, how it is configured per arena, who triggers it, what blocks it, and the
/// operations running through it.
///
/// Agents don't move on their own in the editor, so the tests teleport them into and out of zones.
/// </summary>
public class AgentZoneTests
{
    private AAI3EnvironmentManager _environmentManager;
    private TrainingArena _arena;

    /// <summary>An operation that records the agents it was executed and completed for.</summary>
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
            if (testObject.name.StartsWith("TestRecordingOperation"))
            {
                Object.DestroyImmediate(testObject);
            }
        }
    }

    /// <summary>
    /// Loads a configuration and ends the current episode so the arena rebuilds with it.
    /// </summary>
    private IEnumerator LoadConfiguration(string configFile)
    {
        yield return null;

        _environmentManager = GameObject.FindAnyObjectByType<AAI3EnvironmentManager>();
        Assert.IsNotNull(_environmentManager, "AAI3EnvironmentManager should be found in the scene");
        _arena = GameObject.FindAnyObjectByType<TrainingArena>();
        Assert.IsNotNull(_arena, "TrainingArena should be found in the scene");

        /* The editor always starts in player mode, which only supports one agent */
        _environmentManager.DisablePlayerModeForTesting();
        _environmentManager.configFile = configFile;
        _environmentManager.LoadYAMLFileInEditor();

        /* Wait for ML-Agents' first environment reset (the arena's first build) before forcing a rebuild */
        yield return new WaitForFixedUpdate();
        yield return null;
        _arena.EndArenaEpisode();
        yield return null;
    }

    /// <summary>
    /// Loads another configuration into the running arena and rebuilds it, as the next arena would be.
    /// </summary>
    private IEnumerator SwitchConfiguration(string configFile)
    {
        _environmentManager.configFile = configFile;
        _environmentManager.LoadYAMLFileInEditor();
        _arena.EndArenaEpisode();
        yield return null;
    }

    /// <summary>Moves an agent (keeping its height) and lets the physics catch up with it.</summary>
    private static IEnumerator Teleport(TrainingAgent agent, float x, float z)
    {
        Rigidbody body = agent.GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        agent.transform.position = new Vector3(x, agent.transform.position.y, z);
        Physics.SyncTransforms();
        yield return SettlePhysics();
    }

    /// <summary>A few physics steps: enough for trigger events and the line-of-sight check to run.</summary>
    private static IEnumerator SettlePhysics(int steps = 3)
    {
        for (int i = 0; i < steps; i++)
        {
            yield return new WaitForFixedUpdate();
        }
    }

    /// <summary>Replaces a zone's operations with one that records the agents it runs for.</summary>
    private static RecordingOperation Record(AgentZone zone)
    {
        GameObject host = new GameObject("TestRecordingOperation");
        RecordingOperation operation = host.AddComponent<RecordingOperation>();
        zone.Volume.Configure(zone.Volume.TriggerZoneID, new List<Operation> { operation });
        return operation;
    }

    private static float Distance(TrainingAgent first, TrainingAgent second)
    {
        return Vector3.Distance(first.transform.position, second.transform.position);
    }

    [UnityTest]
    public IEnumerator ZoneBlock_GivesTheAgentATriggerSphereOfTheRadius_RaysIgnore_Invisible()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_reward");
        TrainingAgent owner = _arena.Agents[0];

        AgentZone zone = AgentZone.Of(owner);
        Assert.IsNotNull(zone, "The Agent item's zone block should give the agent a zone");
        Assert.IsTrue(zone.gameObject.activeInHierarchy);
        Assert.AreEqual(3f, zone.Radius);
        Assert.AreEqual(owner.transform, zone.transform.parent, "The zone is a child of the agent");
        Assert.AreEqual(Vector3.zero, zone.transform.localPosition, "The zone is centred on the agent");

        SphereCollider collider = zone.GetComponent<SphereCollider>();
        Assert.IsTrue(collider.isTrigger);
        Assert.AreEqual(3f, collider.radius * zone.transform.lossyScale.x, 0.01f, "The sphere has the configured radius");

        Assert.AreEqual(LayerMask.NameToLayer("Ignore Raycast"), zone.gameObject.layer, "Ray sensors look through the zone");
        Assert.IsFalse(zone.gameObject.CompareTag("agent"), "The zone must not pass for an agent");
        Assert.IsFalse(zone.IsVisible, "Zones are invisible unless asked for");
        Assert.IsFalse(zone.GetComponent<Renderer>().enabled);

        Assert.AreSame(owner, zone.Volume.Owner);
        Assert.IsTrue(zone.Volume.RequiresLineOfSight);
        Assert.AreEqual("mother", zone.Volume.TriggerZoneID);
        Assert.AreEqual(1, zone.Volume.Operations.Count);
        Assert.IsInstanceOf<GrantContinuousReward>(zone.Volume.Operations[0]);
        Assert.AreSame(zone.Volume, zone.Volume.Operations[0].attachedObjectDetails.obj, "Operations see the zone as their host");

        Assert.IsNull(AgentZone.Of(_arena.Agents[1]), "An Agent item without a zone block gets no zone");
    }

    [UnityTest]
    public IEnumerator ZoneVisibility_DrawsATranslucentSphere()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_visible");

        AgentZone zone = AgentZone.Of(_arena.Agents[0]);
        Assert.AreEqual(2.5f, zone.Radius);
        Assert.IsTrue(zone.IsVisible);
        Renderer renderer = zone.GetComponent<Renderer>();
        Assert.IsTrue(renderer.enabled);
        Assert.AreEqual(5f, zone.transform.lossyScale.x, 0.01f, "The unit sphere is scaled to the zone's diameter");
        Assert.AreEqual(1f, renderer.sharedMaterial.GetFloat("_Surface"), "The zone uses the prefab's transparent material");
        Assert.AreEqual("agent0", zone.Volume.TriggerZoneID, "Without a triggerZoneID the zone of agent k is agent<k>");
    }

    [UnityTest]
    public IEnumerator SwitchingArenas_ReconfiguresOrSwitchesOffTheZone()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_reward");
        TrainingAgent owner = _arena.Agents[0];
        AgentZone zone = AgentZone.Of(owner);
        Assert.AreEqual(3f, zone.Radius);

        /* An arena without a zone block for this agent */
        yield return SwitchConfiguration("test_configs/multi_agent_2");
        Assert.AreSame(owner, _arena.Agents[0]);
        Assert.AreSame(zone, AgentZone.Of(owner), "The zone object is kept for later arenas");
        Assert.IsFalse(zone.gameObject.activeInHierarchy, "An arena without a zone block switches the zone off");

        /* ... and one with a different zone */
        yield return SwitchConfiguration("test_configs/agent_zones/agent_zone_visible");
        Assert.AreSame(zone, AgentZone.Of(owner));
        Assert.IsTrue(zone.gameObject.activeInHierarchy);
        Assert.AreEqual(2.5f, zone.Radius);
        Assert.IsTrue(zone.IsVisible);
        Assert.AreEqual("agent0", zone.Volume.TriggerZoneID);
    }

    [UnityTest]
    public IEnumerator AnotherAgent_ComingWithinTheRadius_RunsTheOperations_AndLeavingCompletesThem()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_reward");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];
        AgentZone zone = AgentZone.Of(owner);
        RecordingOperation recorder = Record(zone);
        yield return SettlePhysics();
        Assert.IsEmpty(recorder.Executed, "Nothing should run while the other agent is 20m away");

        /* Just inside: the sphere (3) plus the visitor's body (0.5) */
        yield return Teleport(visitor, owner.transform.position.x + 3.2f, owner.transform.position.z);
        CollectionAssert.AreEqual(new[] { visitor }, recorder.Executed);
        Assert.IsEmpty(recorder.Completed);
        CollectionAssert.AreEquivalent(new[] { visitor }, zone.Volume.AgentsInZone);
        yield return SettlePhysics(5);
        Assert.AreEqual(1, recorder.Executed.Count, "An agent staying inside doesn't trigger the operations again");

        /* Back out */
        yield return Teleport(visitor, owner.transform.position.x + 10f, owner.transform.position.z);
        CollectionAssert.AreEqual(new[] { visitor }, recorder.Completed);
        Assert.IsFalse(zone.Volume.IsAgentInZone);

        /* And in again */
        yield return Teleport(visitor, owner.transform.position.x + 3f, owner.transform.position.z);
        Assert.AreEqual(2, recorder.Executed.Count, "Re-entering triggers the operations again");
    }

    [UnityTest]
    public IEnumerator TheOwner_NeverTriggersItsOwnZone()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_reward");
        TrainingAgent owner = _arena.Agents[0];
        AgentZone zone = AgentZone.Of(owner);
        RecordingOperation recorder = Record(zone);
        float rewardBefore = owner.GetCumulativeReward();

        yield return SettlePhysics(10);
        /* Move the owner about: its body always overlaps its own zone */
        yield return Teleport(owner, 20f, 20f);
        yield return SettlePhysics(10);

        Assert.IsEmpty(recorder.Executed);
        Assert.IsFalse(zone.Volume.IsAgentInZone);
        Assert.LessOrEqual(owner.GetCumulativeReward(), rewardBefore, "The owner should only pay the time penalty");
    }

    [UnityTest]
    public IEnumerator AWall_BetweenTheAgents_BlocksTheZone_UntilItIsRemoved()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_wall");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];
        AgentZone zone = AgentZone.Of(owner);
        yield return SettlePhysics(5);

        Assert.Less(Distance(owner, visitor), zone.Radius, "The visitor spawned inside the zone's radius");
        Assert.IsFalse(zone.Volume.HasLineOfSightTo(visitor), "The wall is in the way");
        Assert.IsFalse(zone.Volume.IsAgentInZone, "An agent behind a wall is not in the zone");
        float visitorRewardBehindWall = visitor.GetCumulativeReward();
        yield return SettlePhysics(5);
        Assert.LessOrEqual(visitor.GetCumulativeReward(), visitorRewardBehindWall, "No reward behind the wall");

        GameObject wall = GameObject.FindGameObjectWithTag("spawnedObjects").transform
            .Cast<Transform>()
            .First(child => child.name.StartsWith("Wall"))
            .gameObject;
        Object.Destroy(wall);
        yield return SettlePhysics(3);

        Assert.IsTrue(zone.Volume.HasLineOfSightTo(visitor));
        CollectionAssert.AreEquivalent(new[] { visitor }, zone.Volume.AgentsInZone, "With the wall gone the visitor is in the zone");
        float visitorRewardAtEntry = visitor.GetCumulativeReward();
        yield return SettlePhysics(5);
        Assert.Greater(visitor.GetCumulativeReward(), visitorRewardAtEntry + 0.3f, "The visitor is now rewarded every step");
    }

    [UnityTest]
    public IEnumerator AGoal_BetweenTheAgents_BlocksTheZone_LikeAnyOtherSolidObject()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_goal");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];
        AgentZone zone = AgentZone.Of(owner);
        yield return SettlePhysics(5);

        Assert.Less(Distance(owner, visitor), zone.Radius, "The visitor spawned inside the zone's radius");
        Assert.IsFalse(zone.Volume.HasLineOfSightTo(visitor), "The goal is in the way");
        Assert.IsFalse(zone.Volume.IsAgentInZone, "A goal is solid, so an agent behind it is not in the zone");

        GameObject goal = GameObject.FindGameObjectWithTag("goodGoal");
        Assert.IsNotNull(goal);
        Object.Destroy(goal);
        yield return SettlePhysics(3);

        Assert.IsTrue(zone.Volume.HasLineOfSightTo(visitor));
        CollectionAssert.AreEquivalent(new[] { visitor }, zone.Volume.AgentsInZone, "With the goal gone the visitor is in the zone");
    }

    [UnityTest]
    public IEnumerator SteppingBehindAWall_WhileInsideTheRadius_CountsAsLeaving()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_wall");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];
        AgentZone zone = AgentZone.Of(owner);
        RecordingOperation recorder = Record(zone);

        /* Round the end of the wall (it spans z = 17..23): in sight, still inside the 6m radius */
        yield return Teleport(visitor, 13f, 25f);
        CollectionAssert.AreEqual(new[] { visitor }, recorder.Executed);

        /* Behind the wall again, without leaving the sphere */
        yield return Teleport(visitor, 15f, 20f);
        CollectionAssert.AreEqual(new[] { visitor }, recorder.Completed, "Losing sight of the agent completes the operations");
        Assert.IsFalse(zone.Volume.IsAgentInZone);
    }

    [UnityTest]
    public IEnumerator AnotherAgent_InTheWay_BlocksTheZone()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_three_agents");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent near = _arena.Agents[1];
        TrainingAgent far = _arena.Agents[2];
        AgentZone zone = AgentZone.Of(owner);
        yield return SettlePhysics(5);

        Assert.Less(Distance(owner, far), zone.Radius, "Both other agents are inside the radius");
        CollectionAssert.AreEquivalent(new[] { near }, zone.Volume.AgentsInZone, "Only the agent with a clear line to the owner is in the zone");
        Assert.IsFalse(zone.Volume.HasLineOfSightTo(far), "The nearer agent stands between the owner and the farther one");

        /* Move the nearer agent aside (still inside the radius) and the farther one comes into the zone */
        yield return Teleport(near, 14f, 24f);
        CollectionAssert.AreEquivalent(new[] { near, far }, zone.Volume.AgentsInZone);
    }

    [UnityTest]
    public IEnumerator GrantContinuousReward_OnAZone_RewardsTheVisitorOnlyWhileInside()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_reward");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];
        yield return SettlePhysics(5);
        float ownerRewardBefore = owner.GetCumulativeReward();
        float visitorRewardBefore = visitor.GetCumulativeReward();

        yield return Teleport(visitor, owner.transform.position.x + 3f, owner.transform.position.z);
        yield return SettlePhysics(5);
        float visitorRewardInside = visitor.GetCumulativeReward();
        Assert.Greater(visitorRewardInside, visitorRewardBefore + 0.3f, "The visitor should be rewarded every step inside the zone");
        Assert.LessOrEqual(owner.GetCumulativeReward(), ownerRewardBefore, "The owner should get nothing from its own zone");

        yield return Teleport(visitor, owner.transform.position.x + 15f, owner.transform.position.z);
        float visitorRewardAtExit = visitor.GetCumulativeReward();
        yield return SettlePhysics(5);
        Assert.LessOrEqual(visitor.GetCumulativeReward(), visitorRewardAtExit, "The reward stops once the visitor has left");
    }

    [UnityTest]
    public IEnumerator EndEpisode_OnAZone_EndsTheEpisode_WhenAnotherAgentTouches()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_end_episode");
        TrainingAgent predator = _arena.Agents[0];
        TrainingAgent prey = _arena.Agents[1];
        int[] episodeEnds = new int[2];
        predator.EpisodeEnded += agent => episodeEnds[0]++;
        prey.EpisodeEnded += agent => episodeEnds[1]++;
        yield return SettlePhysics(5);
        Assert.AreEqual(0, episodeEnds[1]);

        yield return Teleport(prey, predator.transform.position.x + 1.2f, predator.transform.position.z);

        Assert.AreEqual(1, episodeEnds[1], "Touching the predator should end the prey's episode");
        Assert.AreEqual(1, episodeEnds[0], "Under episodeEnd: any the predator's episode ends too");
    }

    [UnityTest]
    public IEnumerator RaySensor_SeesTheAgentInsideItsZone_NotTheZone()
    {
        /* Agent 1 faces agent 0 across 20m; agent 0's 3m zone is between them */
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_reward");
        yield return new WaitForFixedUpdate();
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent observer = _arena.Agents[1];

        RayPerceptionSensorComponent3D raySensor = observer.GetComponent<RayPerceptionSensorComponent3D>();
        Assert.IsNotNull(raySensor, "The editor configuration uses ray casts");
        RayPerceptionOutput output = RayPerceptionSensor.Perceive(raySensor.GetRayPerceptionInput(), false);
        RayPerceptionOutput.RayOutput middleRay = output.RayOutputs[0];

        Assert.IsTrue(middleRay.HasHit);
        Assert.AreSame(owner.gameObject, middleRay.HitGameObject, "The middle ray should hit the agent, not its zone");
        Assert.AreEqual(raySensor.DetectableTags.IndexOf("agent"), middleRay.HitTagIndex);
        /* 20m apart, bodies of radius 0.5: the ray travels about 19m, well past the zone's surface at 16.5m */
        Assert.Greater(middleRay.HitFraction * raySensor.RayLength, 18f, "The ray should pass through the zone");
    }

    [UnityTest]
    public IEnumerator AnAgent_CanSpawnInsideAnotherAgentsZone_AndIsInItStraightAway()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_inside");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];

        Assert.Less(Vector3.Distance(owner.transform.position, new Vector3(10, owner.transform.position.y, 20)), 0.5f);
        Assert.Less(Vector3.Distance(visitor.transform.position, new Vector3(13, visitor.transform.position.y, 20)), 0.5f,
            "The visitor should spawn at its position, 3m from the owner, inside the 5m zone");

        yield return SettlePhysics(3);
        CollectionAssert.AreEquivalent(new[] { visitor }, AgentZone.Of(owner).Volume.AgentsInZone);
    }

    [UnityTest]
    public IEnumerator ArenaReset_ForgetsTheAgentsInTheZone_SoTheyTriggerItAgain()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_inside");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];
        AgentZone zone = AgentZone.Of(owner);
        yield return SettlePhysics(3);
        CollectionAssert.AreEquivalent(new[] { visitor }, zone.Volume.AgentsInZone);

        /* The agents are put back where they started, which is inside the zone */
        _arena.EndArenaEpisode();
        yield return null;
        Assert.IsFalse(zone.Volume.IsAgentInZone, "A reset forgets who was in the zone");

        yield return SettlePhysics(3);
        CollectionAssert.AreEquivalent(new[] { visitor }, zone.Volume.AgentsInZone, "The visitor is in the zone again in the new episode");
        float rewardAtEntry = visitor.GetCumulativeReward();
        yield return SettlePhysics(5);
        Assert.Greater(visitor.GetCumulativeReward(), rewardAtEntry + 0.3f, "And is rewarded again");
    }

    [UnityTest]
    public IEnumerator AFinishedOwner_KeepsItsZoneActive_UnderEpisodeEndAll()
    {
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_all");
        TrainingAgent owner = _arena.Agents[0];
        TrainingAgent visitor = _arena.Agents[1];
        int episodeEnds = 0;
        visitor.EpisodeEnded += agent => episodeEnds++;

        /* The owner finishes and waits, frozen, for the visitor */
        owner.UpdateHealth(1f, andCompleteArena: true);
        yield return SettlePhysics(3);
        Assert.AreEqual(0, episodeEnds, "The episode goes on until the visitor finishes");

        yield return Teleport(visitor, owner.transform.position.x + 3f, owner.transform.position.z);
        float rewardAtEntry = visitor.GetCumulativeReward();
        yield return SettlePhysics(5);
        Assert.Greater(visitor.GetCumulativeReward(), rewardAtEntry + 0.3f, "The finished owner's zone still rewards the visitor");
    }

    [UnityTest]
    public IEnumerator ARadiusBelowTheBodyRadius_IsRaisedToTheMinimum()
    {
        LogAssert.Expect(LogType.Warning, new Regex("zone radius 0.2 is smaller than an agent's body radius"));
        yield return LoadConfiguration("test_configs/agent_zones/agent_zone_small_radius");

        Assert.AreEqual(AgentZone.MinimumRadius, AgentZone.Of(_arena.Agents[0]).Radius);
    }

    [UnityTest]
    public IEnumerator ADataZone_StillRunsItsOperations_ThroughTheSharedVolume()
    {
        yield return LoadConfiguration("test_configs/agent_zones/datazone_reward");
        TrainingAgent agent = _arena.Agents[0];
        DataZone dataZone = GameObject.FindAnyObjectByType<DataZone>();
        Assert.IsNotNull(dataZone);
        Assert.AreEqual("D1", dataZone.TriggerZoneID);
        Assert.IsNull(dataZone.Volume.Owner, "A DataZone has no owner");
        Assert.IsFalse(dataZone.Volume.RequiresLineOfSight, "A DataZone is pure overlap");
        Assert.AreSame(dataZone.Volume, dataZone.Operations[0].attachedObjectDetails.obj);
        yield return SettlePhysics(3);
        Assert.IsFalse(dataZone.isAgentInZone);

        yield return Teleport(agent, 25f, 20f);
        Assert.IsTrue(dataZone.isAgentInZone);
        float rewardAtEntry = agent.GetCumulativeReward();
        yield return SettlePhysics(5);
        Assert.Greater(agent.GetCumulativeReward(), rewardAtEntry + 0.3f, "The DataZone's continuous reward runs");

        yield return Teleport(agent, 10f, 20f);
        Assert.IsFalse(dataZone.isAgentInZone);
        float rewardAtExit = agent.GetCumulativeReward();
        yield return SettlePhysics(5);
        Assert.LessOrEqual(agent.GetCumulativeReward(), rewardAtExit, "And stops on leaving");
    }
}
