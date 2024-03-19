using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine;
using UnityEngineExtensions;
using Holders;
using PrefabInterface;
using ArenasParameters;

// TODO: optimize this code

namespace ArenaBuilders
{
	/// <summary>
	/// An ArenaBuilder is attached to each instantiated arena. Each time the arena is reset the
	/// Builder takes a list of items to spawn in the form of a list of Spawnable items, and
	/// attempts to instantiate these in the arena. For each GameObject instantiated at a specific
	/// position, the builder will check if any object is already present at this position. If not
	/// the object is then moved to the desired position, if the position is occupied the object is
	/// destroyed and therefore ignored. Positions, rotations and scales can be passed by the
	/// user or randomized. In case they are randomized the builder will attempt to spawn items
	/// a certain number of times before giving up and moving on to the next object if no free space was found.
	/// </summary>
	public class ArenaBuilder
	{
		// Range of values X and Y can take (basically the size of the arena)
		private float _rangeX;
		private float _rangeZ;

		public float GetArenaWidth()
		{
			return _rangeX;
		}

		public float GetArenaDepth()
		{
			return _rangeZ;
		}

		// The arena we're in
		private Transform _arena;

		// Empty gameobject that will hold all instantiated GameObjects
		private GameObject _spawnedObjectsHolder;

		// Maximum number of attempts to spawn the objects and the agent
		private int _maxSpawnAttemptsForPrefabs;
		private int _maxSpawnAttemptsForAgent;

		// Agent and its components
		private GameObject _agent;
		private Collider _agentCollider;
		private Rigidbody _agentRigidbody;

		// List of good goals that have been instantiated, used to set numberOfGoals in these goals
		private List<Goal> _goodGoalsMultiSpawned;

		private int _totalObjectsSpawned;


		public void AddToGoodGoalsMultiSpawned(Goal ggm)
		{
			_goodGoalsMultiSpawned.Add(ggm);
			updateGoodGoalsMulti();
		}

		public void AddToGoodGoalsMultiSpawned(GameObject ggm)
		{
			_goodGoalsMultiSpawned.Add(ggm.GetComponent<Goal>());
			updateGoodGoalsMulti();
		}

		// Buffer to allow space around instantiated objects
		// public Vector3 safeSpawnBuffer = Vector3.zero;

		// The list of Spawnables the ArenaBuilder will attempt to spawn at each reset
		[HideInInspector]
		public List<Spawnable> Spawnables { get; set; }

		public ArenaBuilder(
			GameObject arenaGameObject,
			GameObject spawnedObjectsHolder,
			int maxSpawnAttemptsForPrefabs,
			int maxSpawnAttemptsForAgent
		)
		{
			_arena = arenaGameObject.GetComponent<Transform>();
			Transform spawnArenaTransform = _arena
				.FindChildWithTag("spawnArena")
				.GetComponent<Transform>();
			_rangeX = spawnArenaTransform.localScale.x;
			_rangeZ = spawnArenaTransform.localScale.z;
			_agent = _arena.Find("AAI3Agent").Find("Agent").gameObject;
			;
			_agentCollider = _agent.GetComponent<Collider>();
			_agentRigidbody = _agent.GetComponent<Rigidbody>();
			_spawnedObjectsHolder = spawnedObjectsHolder;
			_maxSpawnAttemptsForPrefabs = maxSpawnAttemptsForPrefabs;
			_maxSpawnAttemptsForAgent = maxSpawnAttemptsForAgent;
			Spawnables = new List<Spawnable>();
			_goodGoalsMultiSpawned = new List<Goal>();
		}

		public void Build()
		{
			_totalObjectsSpawned = 0;

			_goodGoalsMultiSpawned.Clear();

			GameObject spawnedObjectsHolder = GameObject.Instantiate(
				_spawnedObjectsHolder,
				_arena.transform,
				false
			);
			spawnedObjectsHolder.transform.parent = _arena;

			InstantiateSpawnables(spawnedObjectsHolder);

			TrainingAgent agentInstance = UnityEngine.Object.FindObjectOfType<TrainingAgent>();
			if (agentInstance != null && _arena != null)
			{
				TrainingArena trainingArena = _arena.GetComponent<TrainingArena>();
				if (trainingArena != null)
				{
					agentInstance.showNotification = ArenasConfigurations.Instance.showNotification;
				}
			}

			updateGoodGoalsMulti();
		}

		public int GetTotalObjectsSpawned()
		{
			return _totalObjectsSpawned;
		}


		private void InstantiateSpawnables(GameObject spawnedObjectsHolder)
		{
			Debug.Log("Spawnables has " + Spawnables.Capacity + " entries");
			List<Spawnable> agentSpawnablesFromUser = Spawnables
				.Where(x => x.gameObject != null && x.gameObject.CompareTag("agent"))
				.ToList();



			// If we are provided with an agent's caracteristics we want to instantiate it first and
			// then ignore any item spawning at the same spot. Otherwise we spawn it last to allow
			// more objects to spawn
			if (agentSpawnablesFromUser.Any())
			{
				_agentCollider.enabled = false;
				SpawnAgent(agentSpawnablesFromUser[0]);
				_agentCollider.enabled = true;
				SpawnObjects(spawnedObjectsHolder);
			}
			else
			{
				_agentCollider.enabled = false;
				SpawnObjects(spawnedObjectsHolder);
				SpawnAgent(null);
				_agentCollider.enabled = true;
			}
		}

		private void SpawnObjects(GameObject spawnedObjectsHolder)
		{
			foreach (Spawnable spawnable in Spawnables)
			{
				if (spawnable.gameObject != null)
				{
					if (!spawnable.gameObject.CompareTag("agent"))
					{
						InstantiateSpawnable(spawnable, spawnedObjectsHolder);
					}
				}
			}
		}

		private void InstantiateSpawnable(Spawnable spawnable, GameObject spawnedObjectsHolder)
		{
			List<Vector3> positions = spawnable.positions;
			List<float> rotations = spawnable.rotations;
			List<Vector3> sizes = spawnable.sizes;
			List<Vector3> colors = spawnable.colors;

			// ======== EXTRA/OPTIONAL PARAMETERS ========
			// use for SignPosterboard symbols, Decay/SizeChange rates, Dispenser settings, etc.

			List<string> symbolNames = spawnable.symbolNames;
			List<float> delays = spawnable.delays;
			List<float> initialValues = spawnable.initialValues;
			List<float> finalValues = spawnable.finalValues;
			List<float> changeRates = spawnable.changeRates;
			List<int> spawnCounts = spawnable.spawnCounts;
			List<Vector3> spawnColors = spawnable.spawnColors;
			List<float> timesBetweenSpawns = spawnable.timesBetweenSpawns;
			List<float> ripenTimes = spawnable.ripenTimes;
			List<float> doorDelays = spawnable.doorDelays;
			List<float> timesBetweenDoorOpens = spawnable.timesBetweenDoorOpens;
			List<float> moveDurations = spawnable.moveDurations;
			List<float> resetDurations = spawnable.resetDurations;

			int numberOfPositions = positions.Count;
			int numberOfRotations = rotations.Count;
			int numberOfSizes = sizes.Count;
			int numberOfColors = colors.Count;
			int numberOfSymbolNames = optionalCount(symbolNames);
			int numberOfDelays = optionalCount(delays);
			int numberOfInitialValues = optionalCount(initialValues);
			int numberOfFinalValues = optionalCount(finalValues);
			int numberOfChangeRates = optionalCount(changeRates);
			int numberOfSpawnCounts = optionalCount(spawnCounts);
			int numberOfSpawnColors = optionalCount(spawnColors);
			int numberOfTimesBetweenSpawns = optionalCount(timesBetweenSpawns);
			int numberOfRipenTimes = optionalCount(ripenTimes);
			int numberOfDoorDelays = optionalCount(doorDelays);
			int numberOfTimesBetweenDoorOpens = optionalCount(timesBetweenDoorOpens);
			int numberOfMoveDurations = optionalCount(moveDurations);
			int numberOfResetDurations = optionalCount(resetDurations);

			int[] ns = new int[]
			{
				numberOfPositions,
				numberOfRotations,
				numberOfSizes,
				numberOfColors,
				numberOfSymbolNames,
				numberOfDelays,
				numberOfInitialValues,
				numberOfFinalValues,
				numberOfChangeRates,
				numberOfSpawnCounts,
				numberOfSpawnColors,
				numberOfTimesBetweenSpawns,
				numberOfRipenTimes,
				numberOfDoorDelays,
				numberOfTimesBetweenDoorOpens,
				numberOfMoveDurations,
				numberOfResetDurations
			};
			int n = ns.Max();

			int k = 0;
			do
			{
				GameObject gameObjectInstance = GameObject.Instantiate(
					spawnable.gameObject,
					spawnedObjectsHolder.transform,
					false
				);
				gameObjectInstance.SetLayer(1);
				Vector3 position = k < ns[0] ? positions[k] : -Vector3.one;
				float rotation = k < ns[1] ? rotations[k] : -1;
				Vector3 size = k < ns[2] ? sizes[k] : -Vector3.one;
				Vector3 color = k < ns[3] ? colors[k] : -Vector3.one;

				// For optional parameters, use default values if not provided.
				string symbolName = k < ns[4] ? symbolNames[k] : null;
				float delay = k < ns[5] ? delays[k] : 0;
				bool tree = (spawnable.name.Contains("Tree"));
				bool ripen_or_grow = (
					spawnable.name.StartsWith("Anti") || spawnable.name.StartsWith("Grow") || tree
				);
				float initialValue =
					k < ns[6] ? initialValues[k] : (tree ? 0.2f : (ripen_or_grow ? 0.5f : 2.5f));
				float finalValue =
					k < ns[7] ? finalValues[k] : (tree ? 1f : (ripen_or_grow ? 2.5f : 0.5f));
				float changeRate = k < ns[8] ? changeRates[k] : -0.005f;
				int spawnCount = k < ns[9] ? spawnCounts[k] : -1;
				Vector3 spawnColor = k < ns[10] ? spawnColors[k] : -Vector3.one; // Special case to leave as default (HDR) spawn color
				float timeBetweenSpawns = k < ns[11] ? timesBetweenSpawns[k] : (tree ? 4f : 1.5f);
				float ripenTime = k < ns[12] ? ripenTimes[k] : 6f;
				float doorDelay = k < ns[13] ? doorDelays[k] : 10f;
				float timeBetweenDoorOpens = k < ns[14] ? timesBetweenDoorOpens[k] : -1f;
				float moveDuration = k < ns[15] ? moveDurations[k] : 1.0f;
				float resetDuration = k < ns[16] ? resetDurations[k] : 1.0f;
				float spawnProbability = spawnable.SpawnProbability;
				Vector3 rewardSpawnPos = spawnable.rewardSpawnPos;

				// Group together in dictionary so can pass as one argument to Spawner...
				// (means we won't have to keep updating the arguments of Spawner function...
				// each time we add to optional parameters)
				Dictionary<string, object> optionals = new Dictionary<string, object>()
				{
					{ nameof(symbolName), symbolName },
					{ nameof(delay), delay },
					{ nameof(initialValue), initialValue },
					{ nameof(finalValue), finalValue },
					{ nameof(changeRate), changeRate },
					{ nameof(spawnCount), spawnCount },
					{ nameof(spawnColor), spawnColor },
					{ nameof(timeBetweenSpawns), timeBetweenSpawns },
					{ nameof(ripenTime), ripenTime },
					{ nameof(doorDelay), doorDelay },
					{ nameof(timeBetweenDoorOpens), timeBetweenDoorOpens },
					{ nameof(moveDuration), moveDuration },
					{ nameof(resetDuration), resetDuration },
					{ nameof(spawnProbability), spawnProbability },
					{ "rewardNames", spawnable.RewardNames },
					{ "rewardWeights", spawnable.RewardWeights },
					{ "rewardSpawnPos", rewardSpawnPos },
					{ "maxRewardCounts", spawnable.maxRewardCounts }
				};

				PositionRotation spawnPosRot = SamplePositionRotation(
					gameObjectInstance,
					_maxSpawnAttemptsForPrefabs,
					position,
					rotation,
					size
				);

				SpawnGameObject(spawnable, gameObjectInstance, spawnPosRot, color, optionals);
				_totalObjectsSpawned++;
				k++;
			} while (k < n);
		}

		// Count of parameter entries in a list...
		// used for optional YAML parameters where list could be null
		private int optionalCount<T>(List<T> paramList)
		{
			return (paramList != null) ? paramList.Count : 0;
		}

		private void SpawnAgent(Spawnable agentSpawnableFromUser)
		{
			PositionRotation agentToSpawnPosRot;
			Vector3 agentSize = _agent.transform.localScale;
			Vector3 position;
			float rotation;
			string skin;
			float freezeDelay;

			position =
				(agentSpawnableFromUser == null || !agentSpawnableFromUser.positions.Any())
					? -Vector3.one
					: agentSpawnableFromUser.positions[0];
			rotation =
				(agentSpawnableFromUser == null || !agentSpawnableFromUser.rotations.Any())
					? -1
					: agentSpawnableFromUser.rotations[0];
			// Extra check for skins because optional param is not always initialised as a List<string> in Spawnable class
			if (agentSpawnableFromUser != null && agentSpawnableFromUser.skins == null)
			{
				agentSpawnableFromUser.skins = new List<string>();
			}
			skin =
				(agentSpawnableFromUser == null || !agentSpawnableFromUser.skins.Any())
					? "random"
					: agentSpawnableFromUser.skins[0];

			// Extra check for freeze delay for same reason as above w/skins
			if (agentSpawnableFromUser != null && agentSpawnableFromUser.frozenAgentDelays == null)
			{
				agentSpawnableFromUser.frozenAgentDelays = new List<float>();
			}
			freezeDelay =
				(agentSpawnableFromUser == null || !agentSpawnableFromUser.frozenAgentDelays.Any())
					? 0
					: agentSpawnableFromUser.frozenAgentDelays[0];

			agentToSpawnPosRot = SamplePositionRotation(
				_agent,
				_maxSpawnAttemptsForAgent,
				position,
				rotation,
				agentSize
			);

			_agentRigidbody.angularVelocity = Vector3.zero;
			_agentRigidbody.velocity = Vector3.zero;
			_agent.transform.localPosition = agentToSpawnPosRot.Position;
			_agent.transform.rotation = Quaternion.Euler(agentToSpawnPosRot.Rotation);

			AnimalSkinManager ASM = _agent.GetComponentInChildren<AnimalSkinManager>();
			Debug.Log("Setting AnimalSkin with ASM: " + ASM.ToString() + " and skin: " + skin);
			ASM.SetAnimalSkin(skin);
			_agent.GetComponent<TrainingAgent>().SetFreezeDelay(freezeDelay);
		}

		private void SpawnGameObject(
			Spawnable spawnable,
			GameObject gameObjectInstance,
			PositionRotation spawnLocRot,
			Vector3 color,
			Dictionary<string, object> optionals = null
		)
		{
			if (spawnLocRot != null)
			{
				gameObjectInstance.transform.localPosition = spawnLocRot.Position;
				gameObjectInstance.transform.Rotate(spawnLocRot.Rotation);
				gameObjectInstance.SetLayer(0);
				gameObjectInstance.GetComponent<IPrefab>().SetColor(color);

				if (
					gameObjectInstance.CompareTag("goodGoalMulti")
					|| gameObjectInstance.CompareTag("goodGoal")
				)
				{
					_goodGoalsMultiSpawned.Add(gameObjectInstance.GetComponent<Goal>());
				}
				if (optionals != null)
				{
					if (
						optionals.TryGetValue("symbolName", out var symbolNameValue)
						&& symbolNameValue is string symbolName
					)
					{
						AssignSymbolName(gameObjectInstance, symbolName, color);
					}

					var spawnerInteractiveButton =
						gameObjectInstance.GetComponentInChildren<Spawner_InteractiveButton>();
					if (spawnerInteractiveButton != null)
					{
						if (
							optionals.TryGetValue("moveDuration", out var moveDurationValue)
							&& moveDurationValue is float moveDuration
						)
						{
							spawnerInteractiveButton.MoveDuration = moveDuration;
						}
						if (
							optionals.TryGetValue("resetDuration", out var resetDurationValue)
							&& resetDurationValue is float resetDuration
						)
						{
							spawnerInteractiveButton.ResetDuration = resetDuration;
						}
						if (
							optionals.TryGetValue("spawnProbability", out var spawnProbabilityValue)
							&& spawnProbabilityValue is float spawnProbability
						)
						{
							spawnerInteractiveButton.SpawnProbability = spawnProbability;
						}
						if (
							optionals.TryGetValue("rewardNames", out var rewardNamesValue)
							&& rewardNamesValue is List<string> rewardNames
						)
						{
							spawnerInteractiveButton.RewardNames = rewardNames;
						}
						if (
							optionals.TryGetValue("rewardWeights", out var rewardWeightsValue)
							&& rewardWeightsValue is List<float> rewardWeights
						)
						{
							spawnerInteractiveButton.RewardWeights = rewardWeights;
						}
						if (
							optionals.TryGetValue("rewardSpawnPos", out var rewardSpawnPosValue)
							&& rewardSpawnPosValue is Vector3 rewardSpawnPos
						)
						{
							spawnerInteractiveButton.RewardSpawnPos = rewardSpawnPos;
						}
						if (
							optionals.TryGetValue("maxRewardCounts", out var maxRewardCountsValue)
							&& maxRewardCountsValue is List<int> maxRewardCounts
						)
						{
							spawnerInteractiveButton.MaxRewardCounts = maxRewardCounts;
						}
					}

					// Check for optional spawnColor for Spawner objects
					if (
						optionals.TryGetValue("spawnColor", out var spawnColorValue)
						&& spawnColorValue != null
						&& gameObjectInstance.TryGetComponent(out GoalSpawner GS)
					)
					{
						GS.SetSpawnColor((Vector3)spawnColorValue);
					}

					// Now check all floats relating to timing of changes
					// Each float param has a list of "acceptable types" to which it applies
					Dictionary<string, List<Type>> paramValidTypeLookup = new Dictionary<
						string,
						List<Type>
					>
					{
						{
							"delay",
							new List<Type>
							{
								typeof(DecayGoal),
								typeof(SizeChangeGoal),
								typeof(GoalSpawner)
							}
						},
						{
							"initialValue",
							new List<Type>
							{
								typeof(DecayGoal),
								typeof(SizeChangeGoal),
								typeof(GoalSpawner)
							}
						},
						{
							"finalValue",
							new List<Type>
							{
								typeof(DecayGoal),
								typeof(SizeChangeGoal),
								typeof(GoalSpawner)
							}
						},
						{
							"changeRate",
							new List<Type> { typeof(DecayGoal), typeof(SizeChangeGoal) }
						},
						{
							"spawnCount",
							new List<Type> { typeof(GoalSpawner) }
						},
						{
							"timeBetweenSpawns",
							new List<Type> { typeof(GoalSpawner) }
						},
						{
							"ripenTime",
							new List<Type> { typeof(GoalSpawner) }
						}, // TreeSpawners only! Ignored o/wise
						{
							"doorDelay",
							new List<Type> { typeof(SpawnerStockpiler) }
						}, // Dispensers/Containers only!
						{
							"timeBetweenDoorOpens",
							new List<Type> { typeof(SpawnerStockpiler) }
						}, // Dispensers/Containers only!
					};
					float v;
					foreach (string paramKey in paramValidTypeLookup.Keys)
					{
						// Try each valid type that we might be able to assign to
						if (optionals[paramKey] != null)
						{
							foreach (Type U in paramValidTypeLookup[paramKey])
							{
								// Check if gameObjectInstance has got the relevant component
								if (gameObjectInstance.TryGetComponent(U, out var component))
								{
									v = Convert.ToSingle(optionals[paramKey]);
									AssignTimingNumber(paramKey, v, component);
								}
							}
						}
					}
				}
				else
				{
					gameObjectInstance.SetActive(false);
					GameObject.Destroy(gameObjectInstance);
				}
			}
		}

		// Calls SetSymbol on SignPosterboard if such a component can be found - overrides colour setting also
		private void AssignSymbolName(GameObject gameObjectInstance, string sName, Vector3 color)
		{
			SignBoard SP = gameObjectInstance.GetComponent<SignBoard>();
			if (SP != null)
			{
				if (color != new Vector3(-1, -1, -1))
				{
					SP.SetColourOverride(color, true);
				}
				// Assertion-cast that symbolName is string (stored as object)
				SP.SetSymbol(sName, true); // UpdatePosterboard() for color/symbol texture is called here!
			}
		}

		// Calls correct Setter method according to arg paramName and corresponding method-name
		private void AssignTimingNumber<T>(string paramName, float value, T component)
		{
			paramName = paramName[0].ToString().ToUpper() + paramName.Substring(1); // "delay" -> "Delay" and so on...
			MethodInfo SetMethod = component.GetType().GetMethod("Set" + (paramName));

			if (SetMethod != null)
			{
				SetMethod.Invoke(component, new object[] { value });
			}
		}

		private PositionRotation SamplePositionRotation(
			GameObject gameObjectInstance,
			int maxSpawnAttempt,
			Vector3 positionIn,
			float rotationY,
			Vector3 size
		)
		{
			Vector3 gameObjectBoundingBox;
			Vector3 rotationOut = Vector3.zero;
			Vector3 positionOut = Vector3.zero;
			IPrefab gameObjectInstanceIPrefab = gameObjectInstance.GetComponent<IPrefab>();
			bool canSpawn = false;
			int k = 0;

			while (!canSpawn && k < maxSpawnAttempt)
			{
				gameObjectInstanceIPrefab.SetSize(size);
				gameObjectBoundingBox = gameObjectInstance.GetBoundsWithChildren().extents;
				if (positionIn.y == -1)
				{
					float minY = 0;
					float maxY = 100;
					positionIn.y = UnityEngine.Random.Range(minY, maxY);
				}

				positionOut = gameObjectInstanceIPrefab.GetPosition(
					positionIn,
					gameObjectBoundingBox,
					_rangeX,
					_rangeZ
				);
				rotationOut = gameObjectInstanceIPrefab.GetRotation(rotationY);

				Collider[] colliders = Physics.OverlapBox(
					positionOut + _arena.position,
					gameObjectBoundingBox,
					Quaternion.Euler(rotationOut),
					1 << 0
				);
				canSpawn = IsSpotFree(
					colliders,
					gameObjectInstance.CompareTag("agent"),
					gameObjectInstance.name.Contains("Zone")
				);
				k++;
			}
			if (canSpawn)
			{
				return new PositionRotation(positionOut, rotationOut);
			}
			return null;
		}

		private bool IsSpotFree(Collider[] colliders, bool isAgent, bool isZone = false)
		{
			if (isZone)
				return colliders.Length == 0
					|| (
						colliders.All(
							collider =>
								collider.isTrigger || !collider.gameObject.CompareTag("arena")
						) && !isAgent
					);
			else
				return colliders.Length == 0
					|| (colliders.All(collider => collider.isTrigger) && !isAgent);
		}

		private bool ObjectOutsideOfBounds(Vector3 position, Vector3 boundingBox)
		{
			return position.x > boundingBox.x
				&& position.x < _rangeX - boundingBox.x
				&& position.z > boundingBox.z
				&& position.z < _rangeZ - boundingBox.z;
		}

		private void updateGoodGoalsMulti()
		{
			int numberOfGoals = _goodGoalsMultiSpawned.Count;
			foreach (Goal goodGoalMulti in _goodGoalsMultiSpawned)
			{
				goodGoalMulti.numberOfGoals = numberOfGoals;
			}
		}
	}
}
