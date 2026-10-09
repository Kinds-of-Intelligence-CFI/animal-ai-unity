using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

/// <summary>
/// Playmode tests for arenas with several agents: agent creation from the prefab, positioning,
/// teams, the agent pool across arenas of different sizes, and the episode end policies.
/// </summary>
public class MultiAgentArenaTests
{
    private AAI3EnvironmentManager _environmentManager;
    private TrainingArena _arena;

    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
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

    [UnityTest]
    public IEnumerator SingleAgentConfiguration_HasExactlyOneAgent()
    {
        yield return LoadConfiguration("test_configs/empty-arena");

        Assert.AreEqual(1, _arena.Agents.Count);
        Assert.AreSame(_arena.PrimaryAgent, _arena.Agents[0]);
        Assert.AreEqual(0, _arena.PrimaryAgent.agentIndex);
        Assert.AreEqual(1, Object.FindObjectsByType<TrainingAgent>(FindObjectsSortMode.None).Length);
    }

    [UnityTest]
    public IEnumerator TwoAgentConfiguration_CreatesTwoAgentsAtTheirPositions()
    {
        yield return LoadConfiguration("test_configs/multi_agent_2");

        Assert.AreEqual(2, _arena.Agents.Count);
        TrainingAgent first = _arena.Agents[0];
        TrainingAgent second = _arena.Agents[1];

        Assert.AreEqual(0, first.agentIndex);
        Assert.AreEqual(1, second.agentIndex);
        Assert.AreNotSame(first, second);
        Assert.IsTrue(first.gameObject.activeInHierarchy);
        Assert.IsTrue(second.gameObject.activeInHierarchy);
        Assert.AreSame(_arena, second.Arena);

        Assert.Less(Vector3.Distance(first.transform.position, new Vector3(10, first.transform.position.y, 20)), 0.5f);
        Assert.Less(Vector3.Distance(second.transform.position, new Vector3(30, second.transform.position.y, 20)), 0.5f);

        /* Both agents share the behavior name and team, so they are two agents of one behavior */
        BehaviorParameters firstBehavior = first.GetComponent<BehaviorParameters>();
        BehaviorParameters secondBehavior = second.GetComponent<BehaviorParameters>();
        Assert.AreEqual(firstBehavior.FullyQualifiedBehaviorName, secondBehavior.FullyQualifiedBehaviorName);

        /* Only the primary agent renders to the screen */
        Assert.IsTrue(second.transform.parent.GetComponentsInChildren<Camera>(true).All(camera => !camera.enabled));
    }

    [UnityTest]
    public IEnumerator PlayerMode_RejectsAnArenaWithSeveralAgents()
    {
        yield return null;
        _environmentManager = GameObject.FindAnyObjectByType<AAI3EnvironmentManager>();
        _arena = GameObject.FindAnyObjectByType<TrainingArena>();
        Assert.IsTrue(_environmentManager.AgentsUsePlayerMode, "The editor should start in player mode");

        /* Let the first environment reset build the scene's single-agent arena */
        yield return new WaitForFixedUpdate();
        yield return null;

        _environmentManager.configFile = "test_configs/multi_agent_2";
        _environmentManager.LoadYAMLFileInEditor();

        System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(() => _arena.EndArenaEpisode());
        StringAssert.Contains("player mode supports only one agent", exception.Message);
        Assert.AreEqual(1, Object.FindObjectsByType<TrainingAgent>(FindObjectsSortMode.None).Length, "No second agent should be created");
    }

    [UnityTest]
    public IEnumerator RaySensor_DetectsTheOtherAgent()
    {
        /* In multi_agent_2 the agents stand at (10, 0, 20) facing +x and (30, 0, 20) facing -x */
        yield return LoadConfiguration("test_configs/multi_agent_2");
        yield return new WaitForFixedUpdate();
        TrainingAgent first = _arena.Agents[0];
        TrainingAgent second = _arena.Agents[1];

        RayPerceptionSensorComponent3D raySensor = first.GetComponent<RayPerceptionSensorComponent3D>();
        Assert.IsNotNull(raySensor, "The editor configuration uses ray casts");
        Assert.AreEqual("agent", raySensor.DetectableTags[raySensor.DetectableTags.Count - 1],
            "'agent' should be the last detectable tag so existing tag indices are unchanged");

        RayPerceptionOutput output = RayPerceptionSensor.Perceive(raySensor.GetRayPerceptionInput(), false);
        /* With alternating ray order the first ray is the middle one, pointing straight ahead */
        RayPerceptionOutput.RayOutput middleRay = output.RayOutputs[0];

        Assert.IsTrue(middleRay.HasHit, "The middle ray should hit the other agent");
        Assert.AreSame(second.gameObject, middleRay.HitGameObject, "The middle ray should hit the other agent, not itself");
        Assert.AreEqual(raySensor.DetectableTags.IndexOf("agent"), middleRay.HitTagIndex);
    }

    [UnityTest]
    public IEnumerator TeamsConfiguration_AssignsTeamsPerAgent()
    {
        yield return LoadConfiguration("test_configs/multi_agent_3_teams");

        Assert.AreEqual(3, _arena.Agents.Count);
        Assert.AreEqual(0, _arena.Agents[0].GetComponent<BehaviorParameters>().TeamId);
        Assert.AreEqual(0, _arena.Agents[1].GetComponent<BehaviorParameters>().TeamId);
        Assert.AreEqual(1, _arena.Agents[2].GetComponent<BehaviorParameters>().TeamId);
        Assert.AreEqual("AnimalAI?team=1", _arena.Agents[2].GetComponent<BehaviorParameters>().FullyQualifiedBehaviorName);
    }

    [UnityTest]
    public IEnumerator SurplusAgents_AreDeactivatedWhenAnArenaNeedsFewer()
    {
        yield return LoadConfiguration("test_configs/multi_agent_2");
        Assert.AreEqual(2, _arena.Agents.Count);
        TrainingAgent second = _arena.Agents[1];

        _environmentManager.configFile = "test_configs/empty-arena";
        _environmentManager.LoadYAMLFileInEditor();
        _arena.EndArenaEpisode();
        yield return null;

        Assert.AreEqual(1, _arena.Agents.Count);
        Assert.IsFalse(second.gameObject.activeInHierarchy, "The second agent's prefab instance should be inactive");

        /* ... and brought back when needed again */
        _environmentManager.configFile = "test_configs/multi_agent_2";
        _environmentManager.LoadYAMLFileInEditor();
        _arena.EndArenaEpisode();
        yield return null;

        Assert.AreEqual(2, _arena.Agents.Count);
        Assert.AreSame(second, _arena.Agents[1]);
        Assert.IsTrue(second.gameObject.activeInHierarchy);
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

    /// <summary>
    /// The agents in the order ML-Agents calls them each step, which is the order of their rows on the Python
    /// side. That's the order they subscribed to the Academy's AgentSendState event (agents subscribe when
    /// enabled); the event is internal to ML-Agents, so its subscribers are read by reflection.
    /// </summary>
    private static List<TrainingAgent> AgentsInPythonRowOrder()
    {
        FieldInfo sendStateField = typeof(Academy).GetField(
            "AgentSendState",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.IsNotNull(
            sendStateField,
            "Academy.AgentSendState wasn't found: check how this ML-Agents version orders agents' rows"
        );
        var sendState = (System.Action)sendStateField.GetValue(Academy.Instance);
        return sendState.GetInvocationList().Select(handler => handler.Target).OfType<TrainingAgent>().ToList();
    }

    /* Python gets the agents' rows in the order of the Agent items in the YAML (documented in the Multi-Agent
       Guide). The hard case is an arena that both brings back an agent that sat out and creates a new one. */
    [UnityTest]
    public IEnumerator PythonRowOrder_FollowsAgentIndex_WhenReturningAndNewAgentsJoinTogether()
    {
        yield return LoadConfiguration("test_configs/multi_agent_2");
        CollectionAssert.AreEqual(_arena.Agents, AgentsInPythonRowOrder());

        /* Agent 1 sits out */
        yield return SwitchConfiguration("test_configs/empty-arena");
        CollectionAssert.AreEqual(_arena.Agents, AgentsInPythonRowOrder());

        /* Agent 1 comes back while agent 2 is created */
        yield return SwitchConfiguration("test_configs/multi_agent_3");
        Assert.AreEqual(3, _arena.Agents.Count);
        CollectionAssert.AreEqual(
            _arena.Agents,
            AgentsInPythonRowOrder(),
            "Rows should follow agent index, got agents "
                + string.Join(", ", AgentsInPythonRowOrder().Select(agent => agent.agentIndex))
        );
    }

    /* Episode ends are counted with TrainingAgent.EpisodeEnded rather than Agent.CompletedEpisodes: ML-Agents
       only counts one episode per decision interval (an agent stays "done" until it next sends a decision),
       so ending episodes back to back, as these tests do, under-counts CompletedEpisodes. */
    private static int[] CountEpisodeEnds(TrainingAgent first, TrainingAgent second)
    {
        int[] counts = new int[2];
        first.EpisodeEnded += agent => counts[0]++;
        second.EpisodeEnded += agent => counts[1]++;
        return counts;
    }

    [UnityTest]
    public IEnumerator RepeatedEpisodeEnds_KeepBothAgentsInPlace()
    {
        yield return LoadConfiguration("test_configs/multi_agent_2");
        int[] episodeEnds = CountEpisodeEnds(_arena.Agents[0], _arena.Agents[1]);

        for (int i = 0; i < 20; i++)
        {
            _arena.EndArenaEpisode();
            yield return new WaitForFixedUpdate();
        }

        Assert.AreEqual(2, _arena.Agents.Count);
        Assert.AreEqual(20, episodeEnds[0]);
        Assert.AreEqual(20, episodeEnds[1]);
        Assert.Less(Vector3.Distance(_arena.Agents[0].transform.position, new Vector3(10, _arena.Agents[0].transform.position.y, 20)), 0.5f);
        Assert.Less(Vector3.Distance(_arena.Agents[1].transform.position, new Vector3(30, _arena.Agents[1].transform.position.y, 20)), 0.5f);
    }

    [UnityTest]
    public IEnumerator AnyPolicy_EndsTheEpisodeForEveryoneWhenOneAgentFinishes()
    {
        yield return LoadConfiguration("test_configs/multi_agent_2");
        TrainingAgent first = _arena.Agents[0];
        TrainingAgent second = _arena.Agents[1];
        int[] episodeEnds = CountEpisodeEnds(first, second);

        first.UpdateHealth(1f, andCompleteArena: true);

        Assert.AreEqual(1, episodeEnds[0]);
        Assert.AreEqual(1, episodeEnds[1], "The other agent's episode should end too");
    }

    [UnityTest]
    public IEnumerator GrantContinuousReward_RewardsEachAgentOnlyWhileItIsInTheZone()
    {
        yield return LoadConfiguration("test_configs/multi_agent_2");
        TrainingAgent first = _arena.Agents[0];
        TrainingAgent second = _arena.Agents[1];

        GameObject zoneObject = new GameObject("TestContinuousRewardZone");
        Operations.GrantContinuousReward operation = zoneObject.AddComponent<Operations.GrantContinuousReward>();
        operation.initialise(new Operations.AttachedObjectDetails
        {
            obj = zoneObject.AddComponent<DataZone>(),
            ID = "test-zone",
            location = Vector3.zero
        });
        operation.rewardPerStep = 0.1f;

        /* Both agents enter; the first leaves while the second stays */
        operation.execute(first);
        operation.execute(second);
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        operation.completeExecution(first);
        float firstAtExit = first.GetCumulativeReward();
        float secondAtExit = second.GetCumulativeReward();

        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.Greater(firstAtExit, 0.3f, "The first agent should have been rewarded while in the zone");
        Assert.LessOrEqual(first.GetCumulativeReward(), firstAtExit, "The first agent should not be rewarded after leaving");
        Assert.Greater(second.GetCumulativeReward(), secondAtExit + 0.3f, "The second agent should still be rewarded");

        operation.completeExecution(second);
        Object.Destroy(zoneObject);
    }

    [UnityTest]
    public IEnumerator AllPolicy_WaitsForEveryAgentToFinish()
    {
        yield return LoadConfiguration("test_configs/multi_agent_all");
        TrainingAgent first = _arena.Agents[0];
        TrainingAgent second = _arena.Agents[1];
        int[] episodeEnds = CountEpisodeEnds(first, second);

        first.UpdateHealth(1f, andCompleteArena: true);

        Assert.AreEqual(0, episodeEnds[0], "The episode should not end until every agent has finished");
        Assert.AreEqual(0, episodeEnds[1]);

        second.UpdateHealth(1f, andCompleteArena: true);

        Assert.AreEqual(1, episodeEnds[0]);
        Assert.AreEqual(1, episodeEnds[1]);
    }
}
