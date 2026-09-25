using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ArenasParameters;
using YAMLDefs;

/// <summary>
/// Tests for the multi-agent parts of the YAML configuration: one Agent item per agent, teams,
/// the episodeEnd policy and their validation.
/// </summary>
public class MultiAgentConfigTests
{
    private YAMLReader _yamlReader;

    [SetUp]
    public void Setup()
    {
        _yamlReader = new YAMLReader();
    }

    private static Item ItemNamedAgent(int positions = 1, List<int> teams = null)
    {
        Item item = new Item { name = "Agent" };
        for (int i = 0; i < positions; i++)
        {
            item.positions.Add(new Vector3(10 + i, 0, 10 + i));
        }
        if (teams != null)
        {
            item.teams = teams;
        }
        return item;
    }

    private static ArenaConfiguration Configuration(params Item[] items)
    {
        Arena arena = new Arena();
        arena.items.AddRange(items);
        ArenasConfigurations configurations = new ArenasConfigurations();
        configurations.Add(0, arena);
        return configurations.configurations[0];
    }

    [Test]
    public void AgentCount_IsOne_WhenNoItemIsNamedAgent()
    {
        ArenaConfiguration configuration = Configuration();
        Assert.AreEqual(1, configuration.AgentCount);
        Assert.IsEmpty(configuration.AgentSpawnables);
    }

    [Test]
    public void AgentCount_IsOne_ForOneAgentWithSeveralPositions()
    {
        // Several positions on one Agent item are candidate spawn points for that one agent
        Assert.AreEqual(1, Configuration(ItemNamedAgent(positions: 3)).AgentCount);
    }

    [Test]
    public void AgentCount_IsTheNumberOfItemsNamedAgent()
    {
        ArenaConfiguration configuration = Configuration(ItemNamedAgent(), ItemNamedAgent(positions: 2), ItemNamedAgent());
        Assert.AreEqual(3, configuration.AgentCount);
        Assert.AreEqual(2, configuration.AgentSpawnables[1].positions.Count, "Agent items keep their order");
    }

    [Test]
    public void TeamForAgent_ReturnsEachItemsTeam_OrNullWhenUnspecified()
    {
        ArenaConfiguration configuration = Configuration(
            ItemNamedAgent(teams: new List<int> { 0 }),
            ItemNamedAgent(teams: new List<int> { 1 }),
            ItemNamedAgent()
        );
        Assert.AreEqual(0, configuration.TeamForAgent(0));
        Assert.AreEqual(1, configuration.TeamForAgent(1));
        Assert.IsNull(configuration.TeamForAgent(2));
        Assert.IsNull(configuration.TeamForAgent(3));
    }

    [Test]
    public void EpisodeEndPolicy_DefaultsToAny()
    {
        Assert.AreEqual(EpisodeEndPolicy.Any, Configuration().episodeEndPolicy);
    }

    [Test]
    public void EpisodeEndPolicy_ParsesAll()
    {
        Arena arena = new Arena { episodeEnd = "all" };
        ArenasConfigurations configurations = new ArenasConfigurations();
        configurations.Add(0, arena);
        Assert.AreEqual(EpisodeEndPolicy.All, configurations.configurations[0].episodeEndPolicy);
    }

    [TestCase("sometimes")]
    [TestCase("All")]
    [TestCase(" all")]
    [TestCase("")]
    [TestCase(null)]
    public void EpisodeEndPolicy_RejectsAnythingButExactlyAnyOrAll(string value)
    {
        Arena arena = new Arena { episodeEnd = value };
        ArenasConfigurations configurations = new ArenasConfigurations();
        Assert.Throws<ArgumentException>(() => configurations.Add(0, arena));
    }

    [Test]
    public void Validation_RejectsSeveralTeamsForOneAgent()
    {
        Assert.Throws<ArgumentException>(() => Configuration(ItemNamedAgent(teams: new List<int> { 0, 1 })));
    }

    [Test]
    public void Validation_RejectsNegativeTeams()
    {
        Assert.Throws<ArgumentException>(() => Configuration(ItemNamedAgent(teams: new List<int> { -1 })));
    }

    [Test]
    public void Validation_RejectsConflictingTeamsAcrossArenas()
    {
        Arena first = new Arena();
        first.items.Add(ItemNamedAgent(teams: new List<int> { 0 }));
        first.items.Add(ItemNamedAgent(teams: new List<int> { 1 }));
        Arena second = new Arena();
        second.items.Add(ItemNamedAgent(teams: new List<int> { 0 }));
        second.items.Add(ItemNamedAgent(teams: new List<int> { 0 }));
        ArenaConfig config = new ArenaConfig
        {
            arenas = new Dictionary<int, Arena> { { 0, first }, { 1, second } }
        };

        Assert.Throws<ArgumentException>(() => new ArenasConfigurations().UpdateWithYAML(config));
    }

    [Test]
    public void Validation_AllowsArenasThatLeaveTeamsUnspecified()
    {
        Arena first = new Arena();
        first.items.Add(ItemNamedAgent(teams: new List<int> { 0 }));
        first.items.Add(ItemNamedAgent(teams: new List<int> { 1 }));
        Arena second = new Arena();
        second.items.Add(ItemNamedAgent());
        second.items.Add(ItemNamedAgent());
        ArenaConfig config = new ArenaConfig
        {
            arenas = new Dictionary<int, Arena> { { 0, first }, { 1, second } }
        };

        Assert.DoesNotThrow(() => new ArenasConfigurations().UpdateWithYAML(config));
    }

    /// <summary>Two arenas, the first with <paramref name="firstAgents"/> Agent items and optionally merging into the second.</summary>
    private static ArenaConfig TwoArenas(int firstAgents, int secondAgents, bool merge)
    {
        Arena first = new Arena { mergeNextArena = merge };
        for (int i = 0; i < firstAgents; i++)
        {
            first.items.Add(ItemNamedAgent());
        }
        Arena second = new Arena();
        for (int i = 0; i < secondAgents; i++)
        {
            second.items.Add(ItemNamedAgent());
        }
        return new ArenaConfig
        {
            arenas = new Dictionary<int, Arena> { { 0, first }, { 1, second } }
        };
    }

    [Test]
    public void Validation_RejectsMergedArenasWithDifferentAgentCounts()
    {
        Assert.Throws<ArgumentException>(
            () => new ArenasConfigurations().UpdateWithYAML(TwoArenas(2, 1, merge: true))
        );
    }

    [Test]
    public void Validation_AllowsMergedArenasWithTheSameAgentCount()
    {
        Assert.DoesNotThrow(() => new ArenasConfigurations().UpdateWithYAML(TwoArenas(2, 2, merge: true)));
    }

    [Test]
    public void Validation_AllowsDifferentAgentCountsInArenasThatDontMerge()
    {
        Assert.DoesNotThrow(() => new ArenasConfigurations().UpdateWithYAML(TwoArenas(2, 1, merge: false)));
    }

    [Test]
    public void Deserialize_ReadsAgentsTeamsAndEpisodeEnd()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            timeLimit: 100
            episodeEnd: all
            items:
            - !Item
              name: Agent
              positions:
              - !Vector3 {x: 10, y: 0, z: 10}
              - !Vector3 {x: 10, y: 0, z: 30}
              teams: [0]
            - !Item
              name: Agent
              positions:
              - !Vector3 {x: 30, y: 0, z: 30}
              teams: [1]";

        ArenaConfig arenaConfig = _yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);
        Arena arena = arenaConfig.arenas[0];

        Assert.AreEqual("all", arena.episodeEnd);
        CollectionAssert.AreEqual(new List<int> { 1 }, arena.items[1].teams);

        ArenasConfigurations configurations = new ArenasConfigurations();
        configurations.UpdateWithYAML(arenaConfig);
        ArenaConfiguration configuration = configurations.configurations[0];
        Assert.AreEqual(2, configuration.AgentCount);
        Assert.AreEqual(1, configuration.TeamForAgent(1));
        Assert.AreEqual(EpisodeEndPolicy.All, configuration.episodeEndPolicy);
    }
}
