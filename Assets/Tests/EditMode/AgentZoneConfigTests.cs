using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ArenasParameters;
using Operations;
using YAMLDefs;

/// <summary>
/// Tests for the configuration side of agent zones: the Agent item's `zone` block, how it reaches the
/// Spawnable, and its validation.
/// </summary>
public class AgentZoneConfigTests
{
    private YAMLReader _yamlReader;

    [SetUp]
    public void Setup()
    {
        _yamlReader = new YAMLReader();
    }

    private static Item AgentItem(Zone zone = null)
    {
        Item item = new Item { name = "Agent", zone = zone };
        item.positions.Add(new Vector3(10, 0, 10));
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
    public void Deserialize_ReadsTheZoneBlock()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            timeLimit: 100
            items:
            - !Item
              name: Agent
              positions:
              - !Vector3 {x: 10, y: 0, z: 10}
              zone:
                radius: 3
                triggerZoneID: mother
                zoneVisibility: true
                operations:
                - !grantContinuousReward
                  rewardPerStep: 0.01
                - !endEpisode
                  reward: -1
            - !Item
              name: Agent
              positions:
              - !Vector3 {x: 30, y: 0, z: 30}";

        ArenaConfig arenaConfig = _yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);
        Zone zone = arenaConfig.arenas[0].items[0].zone;

        Assert.IsNotNull(zone);
        Assert.AreEqual(3f, zone.radius);
        Assert.AreEqual("mother", zone.triggerZoneID);
        Assert.IsTrue(zone.zoneVisibility);
        Assert.AreEqual(2, zone.operations.Count);
        Assert.IsInstanceOf<GrantContinuousReward>(zone.operations[0]);
        Assert.AreEqual(0.01f, ((GrantContinuousReward)zone.operations[0]).rewardPerStep, 1e-6f);
        Assert.IsInstanceOf<EndEpisode>(zone.operations[1]);
        Assert.AreEqual(-1f, ((EndEpisode)zone.operations[1]).reward);

        Assert.IsNull(arenaConfig.arenas[0].items[1].zone, "An Agent item without a zone block has no zone");
    }

    [Test]
    public void Zone_DefaultsTo_NoID_Invisible_NoOperations()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            items:
            - !Item
              name: Agent
              zone:
                radius: 2";

        Zone zone = _yamlReader.deserializer.Deserialize<ArenaConfig>(yaml).arenas[0].items[0].zone;

        Assert.AreEqual(2f, zone.radius);
        Assert.AreEqual("", zone.triggerZoneID);
        Assert.IsFalse(zone.zoneVisibility);
        Assert.IsEmpty(zone.operations);
    }

    [Test]
    public void Spawnable_CarriesTheItemsZone()
    {
        Zone zone = new Zone { radius = 4 };
        Spawnable spawnable = new Spawnable(AgentItem(zone));
        Assert.AreSame(zone, spawnable.zone);

        Assert.IsNull(new Spawnable(AgentItem()).zone);
    }

    [Test]
    public void Configuration_KeepsTheZoneOnTheAgentSpawnable()
    {
        ArenaConfiguration configuration = Configuration(AgentItem(), AgentItem(new Zone { radius = 2.5f }));

        Assert.IsNull(configuration.AgentSpawnables[0].zone);
        Assert.AreEqual(2.5f, configuration.AgentSpawnables[1].zone.radius);
    }

    [Test]
    public void Validation_RejectsAZoneOnANonAgentItem()
    {
        Item wall = new Item { name = "Wall", zone = new Zone { radius = 2 } };
        wall.positions.Add(new Vector3(20, 0, 20));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => Configuration(AgentItem(), wall));
        StringAssert.Contains("only Agent items can carry a zone", exception.Message);
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    public void Validation_RejectsAZoneWithoutAPositiveRadius(float radius)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Configuration(AgentItem(new Zone { radius = radius }))
        );
        StringAssert.Contains("positive radius", exception.Message);
    }

    [Test]
    public void Validation_RaisesARadiusBelowTheBodyRadiusToTheMinimum_WithAWarning()
    {
        LogAssert.Expect(LogType.Warning, new Regex("zone radius 0.2 is smaller than an agent's body radius"));

        ArenaConfiguration configuration = Configuration(AgentItem(new Zone { radius = 0.2f }));

        Assert.AreEqual(AgentZone.MinimumRadius, configuration.AgentSpawnables[0].zone.radius);
    }

    [Test]
    public void Validation_AcceptsTheMinimumRadiusWithoutAWarning()
    {
        ArenaConfiguration configuration = Configuration(AgentItem(new Zone { radius = AgentZone.MinimumRadius }));

        Assert.AreEqual(AgentZone.MinimumRadius, configuration.AgentSpawnables[0].zone.radius);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void MinimumRadius_IsTheAgentsBodyRadius()
    {
        /* The agent prefab's body is a sphere collider of radius 0.5: a smaller zone could never fire
           before the agents touch */
        Assert.AreEqual(0.5f, AgentZone.MinimumRadius);
    }
}
