using NUnit.Framework;
using UnityEngine;
using ArenaBuilders;
using System;
using ArenasParameters;

/// <summary>
/// Tests for the ArenaBuilder class. Contains half of the tests for the ArenaBuilder class.
/// </summary>
public class ArenaBuilderEditModeTests
{
    private GameObject _arenaGameObject;
    private GameObject _spawnedObjectsHolder;
    private ArenaBuilder _arenaBuilder;

    [SetUp]
    public void Setup()
    {
        _arenaGameObject = new GameObject("Arena");
        var spawnArena = new GameObject("spawnArena");
        spawnArena.tag = "spawnArena";
        spawnArena.transform.localScale = new Vector3(10, 1, 20);
        spawnArena.transform.parent = _arenaGameObject.transform;

        var agent = new GameObject("Agent");
        agent.AddComponent<BoxCollider>();
        agent.AddComponent<Rigidbody>();
        var aai3Agent = new GameObject("AAI3Agent");
        agent.transform.parent = aai3Agent.transform;
        aai3Agent.transform.parent = _arenaGameObject.transform;

        _spawnedObjectsHolder = new GameObject("SpawnedObjectsHolder");

        _arenaBuilder = new ArenaBuilder(
            _arenaGameObject,
            _spawnedObjectsHolder,
            maxSpawnAttemptsForPrefabs: 10,
            maxSpawnAttemptsForAgent: 5
        );

        var spawnableObject = new GameObject("SpawnableObject");

        _arenaBuilder.Spawnables.Add(new Spawnable(spawnableObject));

        Debug.Log("Setup completed");
        Debug.Log($"_arenaGameObject: {_arenaGameObject}");
        Debug.Log($"_spawnedObjectsHolder: {_spawnedObjectsHolder}");
        Debug.Log($"_arenaBuilder: {_arenaBuilder}");
        Debug.Log($"Spawnables count: {_arenaBuilder.Spawnables.Count}");
    }

    [Test]
    public void ArenaDepth_ShouldBeInitializedCorrectly()
    {
        float expectedDepth = 20;

        Assert.IsNotNull(_arenaGameObject, "_arenaGameObject is null");
        Assert.IsNotNull(_spawnedObjectsHolder, "_spawnedObjectsHolder is null");
        Assert.IsNotNull(_arenaBuilder, "_arenaBuilder is null");
        Assert.IsTrue(_arenaBuilder.Spawnables.Count > 0, "Spawnables list is empty");
    }

    [Test]
    public void ArenaDepth_ShouldReturnCorrectValue()
    {
        float expectedDepth = 20;

        float actualDepth = _arenaBuilder.ArenaDepth;

        Assert.AreEqual(expectedDepth, actualDepth);
    }

    [Test]
    public void MaxSpawnAttemptsForPrefabs_ShouldBeInitializedCorrectly()
    {
        int expectedMaxSpawnAttempts = 10;

        int actualMaxSpawnAttempts = _arenaBuilder._maxSpawnAttemptsForPrefabs;

        Assert.AreEqual(expectedMaxSpawnAttempts, actualMaxSpawnAttempts);
    }
}
