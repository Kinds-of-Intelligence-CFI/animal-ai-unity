using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.SideChannels;
using Unity.MLAgents.Policies;
using ArenasParameters;
using YamlDotNet.Serialization;

/// <summary>
/// Tests for the AAI3EnvironmentManager class.
/// </summary>
[TestFixture]
public class AAI3EnvironmentManagerTests
{
    private GameObject _gameObject;
    private AAI3EnvironmentManager _environmentManager;
    private TrainingArena _trainingArena;
    private Agent _agent;
    private DecisionRequester _decisionRequester;
    private RayPerceptionSensorComponent3D _raySensor;
    private CameraSensorComponent _cameraSensor;
    private BehaviorParameters _behaviorParameters;
    private Canvas _canvas;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject();
        _environmentManager = _gameObject.AddComponent<AAI3EnvironmentManager>();

        _trainingArena = new GameObject().AddComponent<TrainingArena>();
        _agent = new GameObject().AddComponent<Agent>();
        _decisionRequester = _agent.gameObject.AddComponent<DecisionRequester>();
        _raySensor = _agent.gameObject.AddComponent<RayPerceptionSensorComponent3D>();
        _cameraSensor = _agent.gameObject.AddComponent<CameraSensorComponent>();
        _behaviorParameters = _agent.gameObject.AddComponent<BehaviorParameters>();
        _canvas = new GameObject().AddComponent<Canvas>();

        _environmentManager.arena = _trainingArena.gameObject;
        _environmentManager.uiCanvas = _canvas.gameObject;
        _environmentManager.playerControls = new GameObject();

        _environmentManager._arenasConfigurations = new ArenasConfigurations();
    }

    [Test]
    public void AAI3EnvironmentManager_RetrieveEnvironmentParameters_HandlesInvalidArgsGracefully()
    {
        string[] args = { "--invalidArgument", "someValue" };
        var parameters = _environmentManager.RetrieveEnvironmentParameters(args);

        Assert.IsFalse(parameters.ContainsKey("invalidArgument"));
    }

    [Test]
    public void AAI3EnvironmentManager_GetTotalArenas_ReturnsCorrectCount()
    {
        _environmentManager.AddConfiguration(0, new ArenaConfiguration());
        _environmentManager.AddConfiguration(1, new ArenaConfiguration());

        int totalArenas = _environmentManager.GetTotalArenas();
        Assert.AreEqual(2, totalArenas);
    }

    [Test]
    public void AAI3EnvironmentManager_LoadYAMLFileInEditor_LoadsValidYAML()
    {
        _environmentManager.configFile = "test_configs/decoy-file-test"; /* File should exist and be in resources folder */
        _environmentManager.LoadYAMLFileInEditor();

        Assert.IsTrue(_environmentManager.GetTotalArenas() > 0);
    }

    [Test]
    public void AAI3EnvironmentManager_InstantiateArenas_CreatesMultipleArenas()
    {
        _environmentManager.InstantiateArenas();

        Assert.IsNotNull(_environmentManager.arena);
        Assert.IsInstanceOf<TrainingArena>(_environmentManager.arena.GetComponent<TrainingArena>());
    }

    [Test]
    public void AAI3EnvironmentManager_OnDestroy_UnregistersSideChannel()
    {
        _environmentManager.InitialiseSideChannel();
        _environmentManager.OnDestroy();

        Assert.Pass("Side channel unregistered without exceptions.");
    }

    [Test]
    public void AAI3EnvironmentManager_InitialiseSideChannel_CorrectlyInitializesSideChannel()
    {
        _environmentManager.InitialiseSideChannel();
        Assert.IsNotNull(_environmentManager.ArenasParametersSideChannel);
    }

    [Test]
    public void AAI3EnvironmentManager_RetrieveEnvironmentParameters_WorksCorrectly()
    {
        string[] args = { "--playerMode", "1", "--useCamera", "1", "--resolution", "84" };
        var parameters = _environmentManager.RetrieveEnvironmentParameters(args);
        Assert.IsTrue(parameters.ContainsKey("playerMode"));
        Assert.AreEqual("1", parameters["playerMode"].ToString());
        Assert.AreEqual("1", parameters["useCamera"].ToString());
        Assert.AreEqual("84", parameters["resolution"].ToString());
    }

    [Test]
    public void AAI3EnvironmentManager_RetrieveEnvironmentParameters_HandlesMissingArgs()
    {
        string[] args = { "--playerMode", "1" };
        var parameters = _environmentManager.RetrieveEnvironmentParameters(args);
        Assert.IsTrue(parameters.ContainsKey("playerMode"));
        Assert.AreEqual("1", parameters["playerMode"].ToString());
        Assert.IsFalse(parameters.ContainsKey("useCamera"));
        Assert.IsFalse(parameters.ContainsKey("resolution"));
    }

    [Test]
    public void AAI3EnvironmentManager_ChangeResolution_HandlesDifferentResolutions()
    {
        _environmentManager.ChangeResolution(_cameraSensor, 256, 256, false);
        Assert.AreEqual(256, _cameraSensor.Width);
        Assert.AreEqual(256, _cameraSensor.Height);
        Assert.IsFalse(_cameraSensor.Grayscale);
    }

    [Test]
    public void AAI3EnvironmentManager_ChangeRayCasts_WorksCorrectly()
    {
        _environmentManager.ChangeRayCasts(_raySensor, 4, 90);
        Assert.AreEqual(4, _raySensor.RaysPerDirection);
        Assert.AreEqual(90, _raySensor.MaxRayDegrees);
    }

    [Test]
    public void AAI3EnvironmentManager_ChangeRayCasts_HandlesDifferentSettings()
    {
        _environmentManager.ChangeRayCasts(_raySensor, 8, 180);
        Assert.AreEqual(8, _raySensor.RaysPerDirection);
        Assert.AreEqual(180, _raySensor.MaxRayDegrees);
    }

    [Test]
    public void AAI3EnvironmentManager_InstantiateArenas_CreatesArenaInstance()
    {
        _environmentManager.InstantiateArenas();
        Assert.IsNotNull(_environmentManager.arena);
        Assert.IsInstanceOf<TrainingArena>(_environmentManager.arena.GetComponent<TrainingArena>());
    }

    [Test]
    public void AAI3EnvironmentManager_LoadYAMLFileInEditor_WarnsIfConfigFileIsEmpty()
    {
        _environmentManager.configFile = string.Empty;
        LogAssert.Expect(LogType.Warning, "Config file path is null or empty.");
        _environmentManager.LoadYAMLFileInEditor();
    }

    [Test]
    public void AAI3EnvironmentManager_LoadYAMLFileInEditor_HandlesMissingFile()
    {
        _environmentManager.configFile = "NonExistentFile";
        LogAssert.Expect(
            LogType.Warning,
            "YAML file 'NonExistentFile' could not be found or loaded."
        );
        _environmentManager.LoadYAMLFileInEditor();
    }


    [Test]
    public void AAI3EnvironmentManager_GetConfiguration_ReturnsCorrectConfiguration()
    {
        var config = new ArenaConfiguration();
        _environmentManager.AddConfiguration(1, config);
        var retrievedConfig = _environmentManager.GetConfiguration(1);
        Assert.AreSame(config, retrievedConfig);
    }

    [Test]
    public void AAI3EnvironmentManager_TriggerArenaChangeEvent_InvokesEventCorrectly()
    {
        bool eventTriggered = false;
        AAI3EnvironmentManager.OnArenaChanged += (currentArenaIndex, totalArenas) =>
            eventTriggered = true;

        _environmentManager.TriggerArenaChangeEvent(0, 1);
        Assert.IsTrue(eventTriggered);

        AAI3EnvironmentManager.OnArenaChanged -= (currentArenaIndex, totalArenas) =>
            eventTriggered = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (_environmentManager != null && _environmentManager.ArenasParametersSideChannel != null)
        {
            SideChannelManager.UnregisterSideChannel(
                _environmentManager.ArenasParametersSideChannel
            );
        }

        GameObject.DestroyImmediate(_gameObject);
        GameObject.DestroyImmediate(_trainingArena.gameObject);
        GameObject.DestroyImmediate(_agent.gameObject);
        GameObject.DestroyImmediate(_canvas.gameObject);
    }
}
