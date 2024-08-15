using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using ArenasParameters;
using YAMLDefs;

/// <summary>
/// Tests for the classes in the ArenasParameters class.
/// </summary>
[TestFixture]
public class ArenasParametersTests
{
    private ListOfPrefabs _listOfPrefabs;
    private GameObject _testGameObject;
    private Spawnable _spawnable;
    private ArenaConfiguration _arenaConfiguration;
    private ArenasConfigurations _arenasConfigurations;

    [SetUp]
    public void SetUp()
    {
        _testGameObject = new GameObject("TestObject");
        _listOfPrefabs = new ListOfPrefabs
        {
            allPrefabs = new List<GameObject> { _testGameObject }
        };

        _spawnable = new Spawnable(_testGameObject);

        _arenaConfiguration = new ArenaConfiguration(_listOfPrefabs);

        _arenasConfigurations = new ArenasConfigurations();
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.DestroyImmediate(_testGameObject);
    }

    [Test]
    public void ListOfPrefabs_GetList_ReturnsCorrectList()
    {
        List<GameObject> prefabsList = _listOfPrefabs.GetList();
        Assert.AreEqual(1, prefabsList.Count);
        Assert.AreSame(_testGameObject, prefabsList[0]);
    }

    [Test]
    public void Spawnable_Constructor_InitializesCorrectly()
    {
        Assert.AreEqual(_testGameObject.name, _spawnable.name);
        Assert.AreSame(_testGameObject, _spawnable.gameObject);
        Assert.IsNotNull(_spawnable.positions);
        Assert.IsNotNull(_spawnable.rotations);
        Assert.IsNotNull(_spawnable.sizes);
        Assert.IsNotNull(_spawnable.colors);
    }

    [Test]
    public void ArenaConfiguration_Constructor_InitializesCorrectly()
    {
        Assert.AreEqual(1, _arenaConfiguration.spawnables.Count);
        Assert.AreEqual(_testGameObject.name, _arenaConfiguration.spawnables[0].name);
        Assert.AreEqual(0, _arenaConfiguration.TimeLimit);
    }

    [Test]
    public void ArenaConfiguration_SetGameObject_AssignsCorrectGameObject()
    {
        _arenaConfiguration.SetGameObject(_listOfPrefabs.GetList());
        Assert.AreSame(_testGameObject, _arenaConfiguration.spawnables[0].gameObject);
    }

    [Test]
    public void ArenasConfigurations_Add_AddsConfigurationCorrectly()
    {
        YAMLDefs.Arena yamlArena = new YAMLDefs.Arena();
        _arenasConfigurations.Add(0, yamlArena);

        Assert.AreEqual(1, _arenasConfigurations.configurations.Count);
        Assert.AreEqual(0, _arenasConfigurations.CurrentArenaID);
        Assert.AreEqual(yamlArena.ToString(), _arenasConfigurations.configurations[0].protoString);
    }

    [Test]
    public void ArenasConfigurations_UpdateWithYAML_UpdatesCorrectly()
    {
        YAMLDefs.ArenaConfig yamlConfig = new YAMLDefs.ArenaConfig
        {
            arenas = new Dictionary<int, YAMLDefs.Arena>
            {
                { 0, new YAMLDefs.Arena() },
                { 1, new YAMLDefs.Arena() }
            },
            randomizeArenas = true,
            showNotification = true,
            canResetEpisode = false,
            canChangePerspective = false
        };

        _arenasConfigurations.UpdateWithYAML(yamlConfig);

        Assert.AreEqual(2, _arenasConfigurations.configurations.Count);
        Assert.IsTrue(_arenasConfigurations.randomizeArenas);
        Assert.IsTrue(_arenasConfigurations.showNotification);
        Assert.IsFalse(_arenasConfigurations.canResetEpisode);
        Assert.IsFalse(_arenasConfigurations.canChangePerspective);
    }

    [Test]
    public void ArenasConfigurations_ClearConfigurations_ClearsAllConfigurations()
    {
        YAMLDefs.Arena yamlArena = new YAMLDefs.Arena();
        _arenasConfigurations.Add(0, yamlArena);
        Assert.AreEqual(1, _arenasConfigurations.configurations.Count);

        _arenasConfigurations.ClearConfigurations();
        Assert.AreEqual(0, _arenasConfigurations.configurations.Count);
    }

    [Test]
    public void ArenasConfigurations_SetAllToUpdated_SetsAllToUpdateFalse()
    {
        YAMLDefs.Arena yamlArena = new YAMLDefs.Arena();
        _arenasConfigurations.Add(0, yamlArena);
        _arenasConfigurations.configurations[0].toUpdate = true;

        _arenasConfigurations.SetAllToUpdated();
        Assert.IsFalse(_arenasConfigurations.configurations[0].toUpdate);
    }

    [Test]
    public void ArenasConfigurations_CurrentArenaConfiguration_ReturnsNullIfNotSet()
    {
        Assert.IsNull(_arenasConfigurations.CurrentArenaConfiguration);
    }

    [Test]
    public void ArenasConfigurations_CurrentArenaConfiguration_ReturnsCorrectConfiguration()
    {
        YAMLDefs.Arena yamlArena = new YAMLDefs.Arena();
        _arenasConfigurations.Add(0, yamlArena);
        _arenasConfigurations.CurrentArenaID = 0;

        Assert.IsNotNull(_arenasConfigurations.CurrentArenaConfiguration);
    }
}
