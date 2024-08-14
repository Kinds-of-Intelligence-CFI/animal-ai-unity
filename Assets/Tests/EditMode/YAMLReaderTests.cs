using NUnit.Framework;
using UnityEngine;
using YAMLDefs;
using System.Collections.Generic;

/// <summary>
/// Tests for the YAMLReader class. These tests are comprehensive and cover a wide range of scenarios.
/// </summary>
public class YAMLReaderTests
{
    private YAMLReader yamlReader;

    [SetUp]
    public void Setup()
    {
        yamlReader = new YAMLReader();
    }

    [Test]
    public void DeserializeArenaConfig_WithEmptyItemsList_DeserializesCorrectly()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            passMark: 0.5
            timeLimit: 120";

        ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);

        Assert.NotNull(arenaConfig);
        Assert.AreEqual(1, arenaConfig.arenas.Count, "There should be 1 arena in the config.");

        Arena arena = arenaConfig.arenas[0];
        Assert.AreEqual(0.5f, arena.passMark, "Pass mark should be 0.5.");
        Assert.AreEqual(120, arena.timeLimit, "Time limit should be 120.");
        Assert.AreEqual(0, arena.items.Count, "There should be no items in this arena.");
    }

    [Test]
    public void DeserializeArenaConfig_WithMultipleItemsAndProperties_DeserializesCorrectly()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            passMark: 0.75
            timeLimit: 150
            items:
            - !Item
              name: ComplexItem
              positions:
              - !Vector3 {x: 10, y: 20, z: 30}
              rotations: [45, 90]
              sizes:
              - !Vector3 {x: 2, y: 2, z: 2}
              colors:
              - !RGB {r: 0.5, g: 0.5, b: 0.5}
            - !Item
              name: SimpleItem
              positions:
              - !Vector3 {x: 5, y: 5, z: 5}
              sizes:
              - !Vector3 {x: 1, y: 1, z: 1}
              colors:
              - !RGB {r: 1, g: 0, b: 0}";

        ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);

        Assert.NotNull(arenaConfig);
        Assert.AreEqual(1, arenaConfig.arenas.Count, "There should be 1 arena in the config.");

        Arena arena = arenaConfig.arenas[0];
        Assert.AreEqual(0.75f, arena.passMark, "Pass mark should be 0.75.");
        Assert.AreEqual(150, arena.timeLimit, "Time limit should be 150.");
        Assert.AreEqual(2, arena.items.Count, "There should be 2 items in this arena.");

        Item complexItem = arena.items[0];
        Assert.AreEqual("ComplexItem", complexItem.name);
        Assert.AreEqual(new Vector3(10, 20, 30), complexItem.positions[0]);
        Assert.AreEqual(2, complexItem.rotations.Count);
        Assert.AreEqual(45f, complexItem.rotations[0]);
        Assert.AreEqual(90f, complexItem.rotations[1]);
        Assert.AreEqual(new Vector3(2, 2, 2), complexItem.sizes[0]);
        Assert.AreEqual(
            new Color(0.5f, 0.5f, 0.5f),
            new Color(complexItem.colors[0].r, complexItem.colors[0].g, complexItem.colors[0].b)
        );

        Item simpleItem = arena.items[1];
        Assert.AreEqual("SimpleItem", simpleItem.name);
        Assert.AreEqual(new Vector3(5, 5, 5), simpleItem.positions[0]);
        Assert.AreEqual(new Vector3(1, 1, 1), simpleItem.sizes[0]);
        Assert.AreEqual(
            new Color(1f, 0f, 0f),
            new Color(simpleItem.colors[0].r, simpleItem.colors[0].g, simpleItem.colors[0].b)
        );
    }

    [Test]
    public void DeserializeArenaConfig_WithMissingOptionalFields_HandlesGracefully()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            timeLimit: 200
            items:
            - !Item
              name: TestItem
              positions:
              - !Vector3 {x: 1, y: 1, z: 1}";

        ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);

        Assert.NotNull(arenaConfig);
        Assert.AreEqual(1, arenaConfig.arenas.Count, "There should be 1 arena in the config.");

        Arena arena = arenaConfig.arenas[0];
        Assert.AreEqual(200, arena.timeLimit, "Time limit should be 200.");
        Assert.AreEqual(1, arena.items.Count, "There should be 1 item in this arena.");

        Item testItem = arena.items[0];
        Assert.AreEqual("TestItem", testItem.name);
        Assert.AreEqual(new Vector3(1, 1, 1), testItem.positions[0]);

        Assert.AreEqual(0, testItem.rotations.Count, "Rotations list should be empty.");
        Assert.AreEqual(0, testItem.sizes.Count, "Sizes list should be empty.");
        Assert.AreEqual(0, testItem.colors.Count, "Colors list should be empty.");
        Assert.AreEqual(0, testItem.skins.Count, "Skins list should be empty.");
        Assert.AreEqual(0, testItem.symbolNames.Count, "Symbol names list should be empty.");
    }

    [Test]
    public void DeserializeArenaConfig_WithInvalidYaml_ThrowsException()
    {
        string yaml =
            @"
    !ArenaConfig
    arenas:
      0: !Arena
        timeLimit: 100
        items:
        - !Item
          name: InvalidItem
          positions:
          - !Vector3 {x: 10, y: 0 z: 20}  # Missing comma in Vector3";

        /* Expecting a SemanticErrorException due to invalid YAML content */
        Assert.Throws<YamlDotNet.Core.SemanticErrorException>(() =>
        {
            yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);
        });
    }

    [Test]
    public void DeserializeArenaConfig_WithAliasMapping_CorrectlyResolvesAliases()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            items:
            - !Item
              name: Cardbox1
              positions:
              - !Vector3 {x: 10, y: 0, z: 20}";

        ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);

        Assert.NotNull(arenaConfig);
        Assert.AreEqual(1, arenaConfig.arenas.Count, "There should be 1 arena in the config.");

        Arena arena = arenaConfig.arenas[0];
        Assert.AreEqual(1, arena.items.Count, "There should be 1 item in this arena.");

        Item mappedItem = arena.items[0];
        Assert.AreEqual(
            "LightBlock",
            AliasMapper.ResolveAlias(mappedItem.name),
            "Alias should be resolved to 'LightBlock'."
        );
    }

    [Test]
    public void DeserializeArenaConfig_WithEmptyArenaList_DeserializesCorrectly()
    {
        /* YAML input with no arenas */
        string yaml =
            @"
        !ArenaConfig
        arenas: {}";

        ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);

        Assert.NotNull(arenaConfig);
        Assert.AreEqual(0, arenaConfig.arenas.Count, "There should be no arenas in the config.");
    }

    [Test]
    public void DeserializeArenaConfig_WithCustomColorValues_DeserializesCorrectly()
    {
        string yaml =
            @"
        !ArenaConfig
        arenas:
          0: !Arena
            items:
            - !Item
              name: ColorItem
              positions:
              - !Vector3 {x: 10, y: 0, z: 10}
              colors:
              - !RGB {r: 0.2, g: 0.4, b: 0.6}";

        ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);

        Assert.NotNull(arenaConfig);
        Assert.AreEqual(1, arenaConfig.arenas.Count, "There should be 1 arena in the config.");

        Arena arena = arenaConfig.arenas[0];
        Assert.AreEqual(1, arena.items.Count, "There should be 1 item in this arena.");

        Item colorItem = arena.items[0];
        Assert.AreEqual("ColorItem", colorItem.name);
        Assert.AreEqual(
            new Color(0.2f, 0.4f, 0.6f),
            new Color(colorItem.colors[0].r, colorItem.colors[0].g, colorItem.colors[0].b),
            "RGB color should be correctly deserialized."
        );
    }

    [Test]
    public void DeserializeArenaConfig_WithMultipleArenasAndSharedItems_DeserializesCorrectly()
    {
        string yaml =
            @"
    !ArenaConfig
    arenas:
      0: !Arena
        passMark: 0.5
        timeLimit: 100
        items:
        - !Item
          name: Goal
          positions:
          - !Vector3 {x: 15, y: 0, z: 15}
          sizes:
          - !Vector3 {x: 3, y: 3, z: 3}
      1: !Arena
        passMark: 0.75
        timeLimit: 200
        items:
        - !Item
          name: Goal
          positions:
          - !Vector3 {x: 20, y: 0, z: 20}
          sizes:
          - !Vector3 {x: 5, y: 5, z: 5}";

        ArenaConfig arenaConfig = yamlReader.deserializer.Deserialize<ArenaConfig>(yaml);

        Assert.NotNull(arenaConfig);
        Assert.AreEqual(2, arenaConfig.arenas.Count, "There should be 2 arenas in the config.");

        Arena arena0 = arenaConfig.arenas[0];
        Assert.AreEqual(0.5f, arena0.passMark, "Pass mark for Arena 0 should be 0.5.");
        Assert.AreEqual(100, arena0.timeLimit, "Time limit for Arena 0 should be 100.");
        Assert.AreEqual(1, arena0.items.Count, "Arena 0 should have 1 item.");

        Item goalItem0 = arena0.items[0];
        Assert.AreEqual("Goal", goalItem0.name);
        Assert.AreEqual(new Vector3(15, 0, 15), goalItem0.positions[0]);
        Assert.AreEqual(new Vector3(3, 3, 3), goalItem0.sizes[0]);

        Arena arena1 = arenaConfig.arenas[1];
        Assert.AreEqual(0.75f, arena1.passMark, "Pass mark for Arena 1 should be 0.75.");
        Assert.AreEqual(200, arena1.timeLimit, "Time limit for Arena 1 should be 200.");
        Assert.AreEqual(1, arena1.items.Count, "Arena 1 should have 1 item.");

        Item goalItem1 = arena1.items[0];
        Assert.AreEqual("Goal", goalItem1.name);
        Assert.AreEqual(new Vector3(20, 0, 20), goalItem1.positions[0]);
        Assert.AreEqual(new Vector3(5, 5, 5), goalItem1.sizes[0]);
    }
}
