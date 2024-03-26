using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using ArenaBuilders;
using UnityEngineExtensions;
using ArenasParameters;
using Holders;
using Random = UnityEngine.Random;
using System.Linq;

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

	public bool showNotification { get; set; }

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
		UpdateLigthStatus();
	}

	private void OnDestroy()
	{
		Spawner_InteractiveButton.RewardSpawned -= OnRewardSpawned;
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
		_environmentManager = GameObject.FindObjectOfType<AAI3EnvironmentManager>();
		_agent = FindObjectsOfType<TrainingAgent>(true)[0];
		_agentDecisionInterval = _agent.GetComponentInChildren<DecisionRequester>().DecisionPeriod;
		_fades = blackScreens.GetFades();

		Spawner_InteractiveButton.RewardSpawned += OnRewardSpawned;
	}

	/// <summary>
	/// Resets the arena by destroying existing objects and spawning new ones based on the current arena configuration.
	/// This is a custom implementation of the ResetAcademy method from the MLAgents library. It is called by the TrainingAgent when it resets.
	/// </summary>
	public void ResetArena()
	{
		Debug.Log("Resetting Arena");

		CleanUpSpawnedObjects();

		DetermineNextArenaID();

		if (!TryLoadArenaConfiguration(out ArenaConfiguration newConfiguration))
		{
			Debug.LogError("Failed to load arena configuration");
			return;
		}

		ApplyNewArenaConfiguration(newConfiguration);

		CleanupRewards();

		NotifyArenaChange();

	}

	private void CleanUpSpawnedObjects()
	{
		foreach (GameObject holder in transform.FindChildrenWithTag("spawnedObjects"))
		{
			holder.SetActive(false);
			Destroy(holder);
		}
	}

	private void DetermineNextArenaID()
	{
		int totalArenas = _environmentManager.getMaxArenaID();
		bool randomizeArenas = _environmentManager.GetRandomizeArenasStatus();

		if (isFirstArenaReset)
		{
			isFirstArenaReset = false;
			arenaID = randomizeArenas ? Random.Range(0, totalArenas) : 0;
		}
		else
		{
			arenaID = randomizeArenas ? ChooseRandomArenaID(totalArenas) : (arenaID + 1) % totalArenas;
		}
	}

	private int ChooseRandomArenaID(int totalArenas)
	{
		playedArenas.Add(arenaID);
		if (playedArenas.Count >= totalArenas)
		{
			playedArenas = new List<int> { arenaID };
		}

		var availableArenas = Enumerable.Range(0, totalArenas).Except(playedArenas).ToList();
		return availableArenas[Random.Range(0, availableArenas.Count)];
	}

	private bool TryLoadArenaConfiguration(out ArenaConfiguration newConfiguration)
	{
		return _environmentManager.GetConfiguration(arenaID, out newConfiguration);
	}

	private void ApplyNewArenaConfiguration(ArenaConfiguration newConfiguration)
	{
		_arenaConfiguration = newConfiguration;
		_agent.showNotification = ArenasConfigurations.Instance.showNotification;
		Debug.Log("Updating Arena Configuration");

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

		Debug.Log($"TimeLimit set to: {_arenaConfiguration.TimeLimit}");
	}

	private void NotifyArenaChange()
	{
		_environmentManager.TriggerArenaChangeEvent(arenaID, _environmentManager.GetTotalArenas());
	}

	/// <summary>
	/// Destroys all spawned rewards in the arena.
	/// </summary>
	private void CleanupRewards()
	{
		foreach (var reward in spawnedRewards)
		{
			Destroy(reward);
		}
		spawnedRewards.Clear();
	}

	/// <summary>
	/// Updates the light status in the arena based on the current step count.
	/// </summary>
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

	/// <summary>
	/// Returns the total number of spawned objects in the arena.
	/// </summary>
	public int GetTotalSpawnedObjects()
	{
		Debug.Log("Total spawned objects: " + spawnedObjectsHolder.transform.childCount);
		return spawnedObjectsHolder.transform.childCount;
	}

	/// <summary>
	/// Callback for when a reward is spawned in the arena.
	/// </summary>
	private void OnRewardSpawned(GameObject reward)
	{
		spawnedRewards.Add(reward);
	}

}
