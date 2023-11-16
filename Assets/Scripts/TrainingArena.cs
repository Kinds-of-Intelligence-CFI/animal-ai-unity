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
	private bool _firstReset = true;
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

		// Destroy existing objects in the arena
		foreach (GameObject holder in transform.FindChildrenWithTag("spawnedObjects"))
		{
			holder.SetActive(false);
			Destroy(holder);
		}

		// Calculate the total number of arenas
		int totalArenas = _environmentManager.getMaxArenaID();

		if (_firstReset)
		{
			_firstReset = false;

			// Check if the application is in training mode
			if (Academy.Instance.IsCommunicatorOn)
			{
				// If in training mode, always start with the first arena
				arenaID = 0;
			}
			else if (_environmentManager.GetRandomizeArenasStatus())
			{
				// If randomizeArenas is true, randomly select an arena
				arenaID = Random.Range(0, totalArenas + 1);
			}
			else
			{
				// If not in training mode and randomizeArenas is false, start with the first arena
				arenaID = 0;
			}
		}
		else
		{
			if (_environmentManager.GetRandomizeArenasStatus())
			{
				int newArenaID;
				do
				{
					newArenaID = Random.Range(0, totalArenas + 1);
				} while (newArenaID == arenaID); // Ensure a different arena than the current one

				arenaID = newArenaID;
			}
			else
			{
				arenaID = (arenaID + 1) % (totalArenas + 1); // Sequentially move to the next arena
			}
		}

		ArenaConfiguration newConfiguration;
		int attempts = 0;
		while (!_environmentManager.GetConfiguration(arenaID, out newConfiguration) && attempts <= totalArenas)
		{
			Debug.LogWarning($"Failed to retrieve configuration for arenaID: {arenaID}. Trying next arena or recycling arenas.");
			arenaID = (arenaID + 1) % (totalArenas + 1);
			attempts++;
		}

		if (attempts > totalArenas)
		{
			Debug.LogError($"Critical error: Failed to retrieve configuration for any arena.");
			return;
		}

		_arenaConfiguration = newConfiguration;

		// Updating showNotification from the global configuration
		_agent.showNotification = ArenasConfigurations.Instance.showNotification;

		Debug.Log("Updating Arena Configuration");
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
