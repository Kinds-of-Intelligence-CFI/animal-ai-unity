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

    public bool AllArenasAttempted()
    {
        if (isFirstArenaReset)
        {
            Debug.LogError("Unexpected flow: AllArenasAttempted() called before first arena was reset");
            return false;
        }

        int totalArenas = _environmentManager.GetTotalArenas();

        // arenaID has just ended but not yet been recorded in playedArenas (that happens in the
        // next SetNextArenaID call), so include it explicitly in the count.
        return playedArenas.Count + 1 >= totalArenas;
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
        ToggleObject.RewardSpawned -= OnRewardSpawned;
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

        ToggleObject.RewardSpawned += OnRewardSpawned;
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

            List<int> attemptedArenas = _environmentManager.GetAttemptedArenas();
            if (attemptedArenas != null && attemptedArenas.Count > 0)
            {
                playedArenas = new List<int>(attemptedArenas);
                Debug.Log($"Initialized with {playedArenas.Count} attempted arenas");
            }

            if (randomizeArenas)
            {
                arenaID = ChooseRandomArenaID(totalArenas);
            }
            else
            {
                // For sequential mode: start from the first unattempted arena
                if (playedArenas.Count > 0)
                {
                    // Find the first arena not in playedArenas
                    arenaID = GetFirstArenaID();
                    // TODO: Replace ContainsKey bound with < totalArenas once YAMLs are validated to start from 0 (ATB-103)
                    while (_environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID) && playedArenas.Contains(arenaID))
                    {
                        arenaID++;
                    }
                    // If all arenas were attempted, wrap around to the first arena
                    if (!_environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID))
                    {
                        arenaID = GetFirstArenaID();
                        playedArenas = new List<int>();
                    }
                    Debug.Log($"Sequential mode: starting from arena {arenaID}");
                }
                else
                {
                    arenaID = GetFirstArenaID();
                }
            }
        }
        else
        {
            if (randomizeArenas)
            {
                arenaID = ChooseRandomArenaID(totalArenas);
            }
            else
            {
                // Track the current arena as completed
                if (!playedArenas.Contains(arenaID))
                {
                    playedArenas.Add(arenaID);
                }

                /* If the next arena is merged, sequentially search for the next unmerged one */
                ArenaConfiguration precedingArena = _arenaConfiguration;
                // TODO: Replace ContainsKey wrap with % totalArenas once YAMLs are validated to start from 0 (ATB-103)
                arenaID = _environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID + 1) ? arenaID + 1 : GetFirstArenaID();
                while (precedingArena.mergeNextArena)
                {
                    precedingArena = _environmentManager.GetConfiguration(arenaID);
                    arenaID = _environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID + 1) ? arenaID + 1 : GetFirstArenaID();
                }

                // Reset playedArenas when we've completed all arenas
                if (playedArenas.Count >= totalArenas)
                {
                    playedArenas = new List<int>();
                }
            }
        }
    }

    private int GetFirstArenaID()
    {
        if (!_environmentManager._arenasConfigurations.configurations.ContainsKey(0))
        {
            // TODO: Don't allow running arenas with nonstandard arena numbering (ATB-103)
            Debug.LogWarning("Arena 0 not found in configuration. Starting from arena 1.");
            return 1;
        }
        return 0;
    }

    private int ChooseRandomArenaID(int totalArenas)
    {
        if (_mergedArenas == null)
        {
            _mergedArenas = GetMergedArenas();
        }

        // Track the current arena as played
        if (!playedArenas.Contains(arenaID))
        {
            playedArenas.Add(arenaID);
        }

        if (playedArenas.Count >= totalArenas)
        {
            playedArenas = new List<int> { arenaID };
        }

        var availableArenas = _environmentManager._arenasConfigurations.configurations.Keys
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
