using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using ArenaBuilders;
using UnityEngineExtensions;
using ArenasParameters;
using Holders;
using Random = UnityEngine.Random;
using System.Linq;
using Operations;

/// <summary>
/// This class is responsible for managing the training arena.
/// It contains the logic to reset the arena, update the light status, and handle the spawning of rewards.
/// It also initializes the components required for the arena, such as the ArenaBuilder and the AAI3EnvironmentManager.
/// </summary>
public class TrainingArena : MonoBehaviour
{
    [SerializeField]
    private ListOfPrefabs prefabs;

    [SerializeField]
    private GameObject spawnedObjectsHolder;

    [SerializeField]
    private int maxSpawnAttemptsForAgent = 100;

    [SerializeField]
    private int maxSpawnAttemptsForPrefabs = 20;

    [SerializeField]
    private ListOfBlackScreens blackScreens;

    [HideInInspector]
    public int arenaID = -1;

    public TrainingAgent _agent;
    private ArenaBuilder _builder;
    private ArenaConfiguration _arenaConfiguration = new ArenaConfiguration();
    private AAI3EnvironmentManager _environmentManager;
    private List<Fade> _fades = new List<Fade>();
    private bool _lightStatus = true;
    private int _agentDecisionInterval;
    private bool isFirstArenaReset = true;
    private List<GameObject> spawnedRewards = new List<GameObject>();
    private List<int> playedArenas = new List<int>();
    private List<int> _mergedArenas = null;
    public bool showNotification { get; set; }

    public bool IsFirstArenaReset
    {
        get { return isFirstArenaReset; }
        set { isFirstArenaReset = value; }
    }

    public bool mergeNextArena
    {
        get { return _arenaConfiguration.mergeNextArena; }
    }

    public ArenaBuilder Builder
    {
        get { return _builder; }
    }

    public ArenaConfiguration ArenaConfig
    {
        get { return _arenaConfiguration; }
    }

    internal void Awake()
    {
        InitializeArenaComponents();
    }

    void FixedUpdate()
    {
        UpdateLightStatus();
    }

    private void OnDestroy()
    {
        SpawnObject.RewardSpawned -= OnRewardSpawned;
    }

    /// <summary>
    /// Initializes the components required for the arena, such as the ArenaBuilder and the AAI3EnvironmentManager.
    /// </summary>
    private void InitializeArenaComponents()
    {
        _builder = new ArenaBuilder(
            gameObject,
            spawnedObjectsHolder,
            maxSpawnAttemptsForPrefabs,
            maxSpawnAttemptsForAgent
        );
        _environmentManager = GameObject.FindAnyObjectByType<AAI3EnvironmentManager>();
        if (_environmentManager == null)
        {
            Debug.LogError("AAI3EnvironmentManager is not found in the scene.");
        }
        _agent = FindObjectsByType<TrainingAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        _agentDecisionInterval = _agent.GetComponentInChildren<DecisionRequester>().DecisionPeriod;
        _fades = blackScreens.GetFades();

        SpawnObject.RewardSpawned += OnRewardSpawned;
    }

    /// <summary>
    /// Provides a list of the arenas in the current config file that are preceeded by an arena with
    /// the mergeNextArena property, so that we can avoid loading them when arenas are randomised.
    /// </summary>
    private List<int> GetMergedArenas()
    {
        List<int> mergedArenas = new List<int>();
        int totalArenas = _environmentManager.GetTotalArenas();
        ArenaConfiguration currentArena = _environmentManager.GetConfiguration(0);
        bool currentlyMerged = currentArena.mergeNextArena;
        for (int i = 1; i < totalArenas; i++)
        {
            if (currentlyMerged)
            {
                mergedArenas.Add(i);
            }
            currentArena = _environmentManager.GetConfiguration(i);
            currentlyMerged = currentArena.mergeNextArena;
        }
        return mergedArenas;
    }

    /// <summary>
    /// Resets the arena by destroying existing objects and spawning new ones based on the current arena configuration.
    /// This is a custom implementation of the ResetAcademy method from the MLAgents library. It is called by the TrainingAgent when it resets.
    /// </summary>
    public void ResetArena()
    {
        Debug.Log("Resetting Arena");

        CleanUpSpawnedObjects();

        SetNextArenaID();

        ArenaConfiguration newConfiguration = _environmentManager.GetConfiguration(arenaID);

        ApplyNewArenaConfiguration(newConfiguration);

        CleanupRewards();

        NotifyArenaChange();
    }

    public void LoadNextArena()
    {
        if (isFirstArenaReset)
        {
            throw new InvalidOperationException("LoadNextArena called before first reset");
        }
        Debug.Log($"Loading next arena. Previous: {arenaID}, next: {arenaID + 1}");

        CleanUpSpawnedObjects();

        arenaID += 1;

        ArenaConfiguration newConfiguration = _environmentManager.GetConfiguration(arenaID);

        int totalArenas = _environmentManager.GetTotalArenas();
        if (arenaID == totalArenas - 1 && newConfiguration.mergeNextArena)
        {
            throw new InvalidOperationException(
                "The final arena cannot have mergeNextArena set to true."
            );
        }

        ApplyNewArenaConfiguration(newConfiguration);

        CleanupRewards();

        NotifyArenaChange();
    }

    public GameObject AddNewItemToArena(YAMLDefs.Item spawnable)
    {
        GameObject holderInstance = GameObject.FindGameObjectWithTag("spawnedObjects");
        if (holderInstance == null)
        {
            Debug.LogError("Can't find the spawned objects holder instance in scene");
            return null;
        }

        Spawnable spawnableToUse = new Spawnable(spawnable);
        spawnableToUse.gameObject = prefabs.GetList().Find(x => x.name == spawnable.name);

        return _builder.InstantiateSpawnable(spawnableToUse, holderInstance, true);
    }

    private void CleanUpSpawnedObjects()
    {
        foreach (GameObject holder in transform.FindChildrenWithTag("spawnedObjects"))
        {
            holder.SetActive(false);
            Destroy(holder);
        }
    }

    private void SetNextArenaID()
    {
        int totalArenas = _environmentManager.GetTotalArenas();
        bool randomizeArenas = _environmentManager.GetRandomizeArenasStatus();

        if (isFirstArenaReset)
        {
            isFirstArenaReset = false;
            arenaID = randomizeArenas ? ChooseRandomArenaID(totalArenas) : 0;
        }
        else
        {
            if (randomizeArenas)
            {
                arenaID = ChooseRandomArenaID(totalArenas);
            }
            else
            {
                /* If the next arena is merged, sequentially search for the next unmerged one */
                ArenaConfiguration precedingArena = _arenaConfiguration;
                arenaID = (arenaID + 1) % totalArenas;
                while (precedingArena.mergeNextArena)
                {
                    precedingArena = _environmentManager.GetConfiguration(arenaID);
                    arenaID = (arenaID + 1) % totalArenas;
                }
            }
        }
    }

    private int ChooseRandomArenaID(int totalArenas)
    {
        if (_mergedArenas == null)
        {
            _mergedArenas = GetMergedArenas();
        }

        playedArenas.Add(arenaID);
        if (playedArenas.Count >= totalArenas)
        {
            playedArenas = new List<int> { arenaID };
        }

        var availableArenas = Enumerable
            .Range(0, totalArenas)
            .Except(playedArenas)
            .Except(_mergedArenas)
            .ToList();
        return availableArenas[Random.Range(0, availableArenas.Count)];
    }

    /*
       Note: to update the active arena to a new ID the following must be called in sequence
       GetConfiguration, ApplyNewArenaConfiguration, CleanupRewards, NotifyArenaChange
    */
    private void ApplyNewArenaConfiguration(ArenaConfiguration newConfiguration)
    {
        if (_environmentManager == null)
        {
            Debug.LogError("Environment Manager is null in ApplyNewArenaConfiguration.");
            return;
        }

        _arenaConfiguration = newConfiguration;
        _arenaConfiguration.passMark = newConfiguration.passMark;

        var arenasConfigurations = _environmentManager.GetArenasConfigurations();
        if (arenasConfigurations != null)
        {
            _agent.showNotification = arenasConfigurations.showNotification;
        }
        else
        {
            Debug.LogError("ArenasConfigurations is not initialized in ApplyNewArenaConfiguration.");
        }

        _arenaConfiguration.SetGameObject(prefabs.GetList());
        _builder.Spawnables = _arenaConfiguration.spawnables;
        _arenaConfiguration.toUpdate = false;
        _agent.MaxStep = 0;
        _agent.timeLimit = _arenaConfiguration.TimeLimit * _agentDecisionInterval;
        _builder.Build();
        _arenaConfiguration.lightsSwitch.Reset();

        if (_arenaConfiguration.randomSeed != 0)
        {
            Random.InitState(_arenaConfiguration.randomSeed);
        }

        Debug.Log($"Final passMark value: {_arenaConfiguration.passMark}");
    }

    private void NotifyArenaChange()
    {
        _environmentManager.TriggerArenaChangeEvent(arenaID, _environmentManager.GetTotalArenas());
    }

    private void CleanupRewards()
    {
        foreach (var reward in spawnedRewards)
        {
            Destroy(reward);
        }
        spawnedRewards.Clear();
    }

    public void UpdateLightStatus()
    {
        int stepCount = _agent.StepCount;
        bool newLight = _arenaConfiguration.lightsSwitch.LightStatus(
            stepCount,
            _agentDecisionInterval
        );
        if (newLight != _lightStatus)
        {
            _lightStatus = newLight;
            foreach (Fade fade in _fades)
            {
                fade.StartFade();
            }
        }
    }

    public int GetTotalSpawnedObjects()
    {
        return spawnedObjectsHolder.transform.childCount;
    }

    private void OnRewardSpawned(GameObject reward)
    {
        spawnedRewards.Add(reward);
    }
}
