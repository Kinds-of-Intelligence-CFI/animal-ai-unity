using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using ArenaBuilders;
using UnityEngineExtensions;
using ArenasParameters;
using Holders;
using Random = UnityEngine.Random;

public class TrainingArena : MonoBehaviour
{
    public ListOfPrefabs prefabs = new ListOfPrefabs();
    public GameObject spawnedObjectsHolder;
    public int maxSpawnAttemptsForAgent = 100;
    public int maxSpawnAttemptsForPrefabs = 20;
    public ListOfBlackScreens blackScreens = new ListOfBlackScreens();

    [HideInInspector]
    public int arenaID = -1;

    [HideInInspector]
    public int maxarenaID = -1;

    [HideInInspector]
    public TrainingAgent _agent;
    private ArenaBuilder _builder;
    public ArenaBuilder Builder
    {
        get { return _builder; }
    }
    private ArenaConfiguration _arenaConfiguration = new ArenaConfiguration();
    private AAI3EnvironmentManager _environmentManager;
    private List<Fade> _fades = new List<Fade>();
    private bool _lightStatus = true;
    private int _agentDecisionInterval; // How many frames between decisions, reads from agent's decision requester

    private bool _firstReset = true;

    private List<GameObject> spawnedRewards = new List<GameObject>();

    internal void Awake()
    {
        _builder = new ArenaBuilder(
            gameObject,
            spawnedObjectsHolder,
            maxSpawnAttemptsForPrefabs,
            maxSpawnAttemptsForAgent
        );
        _environmentManager = GameObject.FindObjectOfType<AAI3EnvironmentManager>();
        _agent = FindObjectsOfType<TrainingAgent>(true)[0];
        _agentDecisionInterval = _agent.GetComponentInChildren<DecisionRequester>().DecisionPeriod;
        _fades = blackScreens.GetFades();

        // Subscribe to the reward spawn event from spawner_InteractiveButton.cs
        Spawner_InteractiveButton.RewardSpawned += OnRewardSpawned;
    }

    private void OnDestroy()
    {
        // Unsubscribe when the object is destroyed to avoid memory leaks
        Spawner_InteractiveButton.RewardSpawned -= OnRewardSpawned;
    }

    private void OnRewardSpawned(GameObject reward)
    {
        spawnedRewards.Add(reward);
    }

    public void ResetArena()
    {
        Debug.Log("Resetting Arena");
        foreach (GameObject holder in transform.FindChildrenWithTag("spawnedObjects"))
        {
            holder.SetActive(false);
            Destroy(holder);
        }

        // Each time reset is called we cycle through the defined arenaIDs
        maxarenaID = _environmentManager.getMaxArenaID(); // Should perform this check once (after environmentManager is initialized).
        if (maxarenaID > 0 && !_firstReset)
        {
            arenaID = (arenaID + 1) % maxarenaID;
        }

        ArenaConfiguration newConfiguration;
        if (!_environmentManager.GetConfiguration(arenaID, out newConfiguration))
        {
            newConfiguration = new ArenaConfiguration(prefabs);
            _environmentManager.AddConfiguration(arenaID, newConfiguration);
        }
        _arenaConfiguration = newConfiguration;

        Debug.Log("Updating");
        _arenaConfiguration.SetGameObject(prefabs.GetList());
        _builder.Spawnables = _arenaConfiguration.spawnables;
        _arenaConfiguration.toUpdate = false;
        _agent.MaxStep = 0; //We never time the environment out unless agent health goes to 0
        _agent.timeLimit = _arenaConfiguration.T * _agentDecisionInterval;

        foreach (var spawnable in _builder.Spawnables)
        {
            Debug.Log(
                $"In ResetArena, moveDurations for {spawnable.name}: {string.Join(", ", spawnable.moveDurations)}"
            );
        }

        _builder.Build();
        _arenaConfiguration.lightsSwitch.Reset();
        _firstReset = false;

        if (_arenaConfiguration.randomSeed != 0)
        {
            Random.InitState(_arenaConfiguration.randomSeed);
        }

        foreach (var reward in spawnedRewards)
        {
            Destroy(reward);
        }
        spawnedRewards.Clear();
    }

    public void UpdateLigthStatus()
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

    void FixedUpdate()
    {
        UpdateLigthStatus();
    }
}
