using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using ArenaBuilders;
using UnityEngineExtensions;
using ArenasParameters;
using Holders;
using Random = UnityEngine.Random;
using System.Linq;


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
	private bool _isFirstReset = true;
	private List<GameObject> spawnedRewards = new List<GameObject>();
	public bool showNotification { get; set; }


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
		arenaID = 0;
		_isFirstReset = true;
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
		Debug.Log($"Resetting Arena - Current Arena ID: {arenaID}");

		// Destroy existing objects in the arena
		ClearArena();

		// Handle arena cycling
		HandleArenaCycling();

		// Apply the configuration for the current arena
		ApplyCurrentArenaConfiguration();
	}

	private void ClearArena()
	{
		foreach (GameObject holder in transform.FindChildrenWithTag("spawnedObjects"))
		{
			holder.SetActive(false);
			Destroy(holder);
		}
		// Clear any other states or objects as needed
	}

	private void HandleArenaCycling()
{
    // Get total number of arenas (assuming index starts at 0)
    int totalArenas = _environmentManager.getMaxArenaID() + 1;

    // If it's not the first reset, update arenaID
    if (!_isFirstReset)
    {
        // Sequentially move to the next arena for both training and manual play
        arenaID = (arenaID + 1) % totalArenas;
    }
    else
    {
        // For the first reset, keep the first arena (index 0) if in training mode
        if (Academy.Instance.IsCommunicatorOn)
        {
            arenaID = 0;
        }
        else if (_environmentManager.GetRandomizeArenasStatus())
        {
            // In manual play with randomization
            arenaID = Random.Range(0, totalArenas);
        }

        _isFirstReset = false; // Mark the first reset as completed
    }

    Debug.Log($"Next Arena ID: {arenaID}");
}

	

	private void ApplyCurrentArenaConfiguration()
	{
		if (_environmentManager.GetConfiguration(arenaID, out ArenaConfiguration newConfiguration))
		{
			_arenaConfiguration = newConfiguration;
			UpdateArenaConfiguration();
		}
		else
		{
			Debug.LogError($"Error: Failed to retrieve configuration for arenaID: {arenaID}.");
		}
	}

	private void UpdateArenaConfiguration()
	{
		_arenaConfiguration.SetGameObject(prefabs.GetList());
		_builder.Spawnables = _arenaConfiguration.spawnables;
		_arenaConfiguration.toUpdate = false;
		_agent.MaxStep = 0;
		_agent.timeLimit = _arenaConfiguration.T * _agentDecisionInterval;
		_builder.Build();
		_arenaConfiguration.lightsSwitch.Reset();

		if (_arenaConfiguration.randomSeed != 0)
		{
			Random.InitState(_arenaConfiguration.randomSeed);
		}

		// Clear any spawned rewards
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

	public ArenaConfiguration ArenaConfig
	{
		get { return _arenaConfiguration; }
	}
}
