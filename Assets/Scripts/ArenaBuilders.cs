using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine;
using UnityEngineExtensions;
using Holders;
using PrefabInterface;
using ArenasParameters;

/// <summary>
/// An ArenaBuilder linked to an arena instantiates a list of Spawnable items within the arena each reset.
/// It checks for pre-existing objects at the intended spawn positions.
/// Unoccupied positions allow the object to be placed; otherwise, it's destroyed.
/// User-defined or randomized positions, rotations, and scales are supported
/// ... with repeated spawn attempts made for random placements until free space is found or the builder moves to the next item.
/// </summary>

// TODO: Optimize and refactor the this script.
// TODO: Overhaul object spawn and agent spawn logic for a more unified and central implementation (for all objects, inc. agent)
namespace ArenaBuilders
{
    public class ArenaBuilder
    {
        #region Properties and Fields

        // Arena size
        private float _rangeX;
        private float _rangeZ;

        // Getters for the arena size (used for spawning objects within the arena bounds in other scripts)
        public float ArenaWidth => _rangeX;
        public float ArenaDepth => _rangeZ;

        // Arena to which the builder is attached
        private Transform _arena;

        // Holder for all spawned objects
        private GameObject _spawnedObjectsHolder;

        // Max number of attempts to spawn an object or agent
        private int _maxSpawnAttemptsForPrefabs;
        private int _maxSpawnAttemptsForAgent;

        // Agent components
        private GameObject _agent;
        private Collider _agentCollider;
        private Rigidbody _agentRigidbody;

        // Total number of good goals instantiated
        private List<Goal> _goodGoalsMultiSpawned;

        // Total number of objects spawned (for UI)
        private int _totalObjectsSpawned;

        // The list of Spawnables the ArenaBuilder will attempt to spawn at each reset
        [HideInInspector]
        public List<Spawnable> Spawnables { get; set; }

        #endregion

        #region Arena Constructor

        /// <summary>
        /// Constructor for the ArenaBuilder class.
        /// It initializes the arena, the spawned objects holder, and the maximum spawn attempts for prefabs and the agent.
        /// </summary>
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

        #endregion

        #region Arena Builder

        /// <summary>
        /// Builds the arena by instantiating the Spawnable objects within the arena.
        /// </summary>
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

        #endregion

        #region Instantiate and Spawn Objects Methods

        /// <summary>
        /// Instantiates the Spawnable objects within the arena.
        /// </summary>
        private void InstantiateSpawnables(GameObject spawnedObjectsHolder)
        {
            Debug.Log("Spawnables has " + Spawnables.Capacity + " entries");
            List<Spawnable> agentSpawnablesFromUser = Spawnables
                .Where(x => x.gameObject != null && x.gameObject.CompareTag("agent"))
                .ToList();

            // Instantiate the agent first based on its characteristics, then prevent item spawning at the same location;
            // ... otherwise, spawn it last to enable more object spawns.
            if (agentSpawnablesFromUser.Any())
            {
                _agentCollider.enabled = false;

                // Check for problematic positions and adjust if necessary
                Vector3 agentPosition = agentSpawnablesFromUser[0].positions[0];
                if (IsProblematicPosition(agentPosition))
                {
                    Debug.LogWarning("Agent position is problematic. Adjusting to (1, 0, 1)");
                    agentPosition = new Vector3(1, 0, 1);
                    agentSpawnablesFromUser[0].positions[0] = agentPosition;
                }

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

        // Helper method to check for agent position problems. When arena size can be changed, this method will need to be updated.
        private bool IsProblematicPosition(Vector3 position)
        {
            return position.x == 0 || position.z == 0 || position.x == 40 || position.z == 40;
        }

        /// <summary>
        /// Spawns the objects within the arena.
        /// </summary>
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

        /// <summary>
        /// InstantiateSpawnable spawns game objects in a game environment.
        /// It takes two parameters: a Spawnable object and a GameObject that serves as a holder for the spawned objects.
        /// The method instantiates the game object, sets its layer, position, rotation, and scale, and then spawns the game object.
        /// The method also sets the color of the game object and assigns a symbol name to the game object's SignBoard component.
        /// </summary>
        private void InstantiateSpawnable(Spawnable spawnable, GameObject spawnedObjectsHolder)
        {
            // Required parameters
            List<Vector3> positions = spawnable.positions;
            List<float> rotations = spawnable.rotations;
            List<Vector3> sizes = spawnable.sizes;
            List<Vector3> colors = spawnable.colors;

            // Optional parameters
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
            List<string> triggerZoneID = spawnable.triggerZoneID;
            bool zoneVisibility = spawnable.zoneVisibility;

            // Get the number of elements in the lists
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

            // Get the number of elements in the lists
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
            // Get the maximum number of elements in the lists
            int n = ns.Max();

            // Spawn the objects
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

                // For optional parameters, use default values if not provided
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
                Vector3 spawnColor = k < ns[10] ? spawnColors[k] : -Vector3.one;
                float timeBetweenSpawns = k < ns[11] ? timesBetweenSpawns[k] : (tree ? 4f : 1.5f);
                float ripenTime = k < ns[12] ? ripenTimes[k] : 6f;
                float doorDelay = k < ns[13] ? doorDelays[k] : 10f;
                float timeBetweenDoorOpens = k < ns[14] ? timesBetweenDoorOpens[k] : -1f;
                float moveDuration = k < ns[15] ? moveDurations[k] : 1.0f;
                float resetDuration = k < ns[16] ? resetDurations[k] : 1.0f;
                float spawnProbability = spawnable.SpawnProbability;
                Vector3 rewardSpawnPos = spawnable.rewardSpawnPos;

                // Assign the optional parameters to a dictionary
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
                    { "maxRewardCounts", spawnable.maxRewardCounts },
                    { "triggerZoneID", spawnable.triggerZoneID },
                    { "zoneVisibility", spawnable.zoneVisibility }
                };

                // Determines a suitable position and rotation for the object to spawn
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

        /// <summary>
        /// The SpawnGameObject function instantiates a game object with specified properties, sets its position, rotation, and color, assigns it a symbol name if provided,
        /// ...adjusts properties of its Spawner_InteractiveButton and GoalSpawner components based on optional parameters, and assigns timing parameters to relevant components.
        /// </summary>
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
                    if (gameObjectInstance.CompareTag("DataZone"))
                    {
                        DataZone dataZone = gameObjectInstance.GetComponent<DataZone>();
                        if (dataZone != null)
                        {
                            if (
                                optionals.TryGetValue("triggerZoneID", out var triggerZoneIDsValue)
                                && triggerZoneIDsValue is List<string> triggerZoneIDs
                                && triggerZoneIDs.Count > 0
                            )
                            {
                                dataZone.TriggerZoneID = triggerZoneIDs[0];
                            }

                            if (
                                optionals.TryGetValue("zoneVisibility", out var zoneVisibilityValue)
                                && zoneVisibilityValue is bool zoneVisibility
                            )
                            {
                                dataZone.SetVisibility(zoneVisibility);
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

        #endregion

        #region Spawn Agent

        /// <summary>
        /// The SpawnAgent function spawns an agent in a game environment.
        /// It takes a Spawnable object as a parameter and spawns the agent at a specified position and rotation.
        /// The function also sets the agent's skin and freeze delay.
        /// </summary>
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
            ASM.SetAnimalSkin(skin);
            _agent.GetComponent<TrainingAgent>().SetFreezeDelay(freezeDelay);
        }

        #endregion

        #region Check Position/Rotation and Object Placement Methods

        /// <summary>
        /// The SamplePositionRotation function samples a position and rotation for a game object to spawn.
        /// It takes five parameters: a game object instance, the maximum number of spawn attempts, a position, a rotation, and a size.
        /// The function returns a PositionRotation object that contains the position and rotation of the game object to spawn.
        /// </summary>
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

        /// <summary>
        /// The IsSpotFree function checks if a spot is free for a game object to spawn.
        /// It takes three parameters: an array of colliders, a boolean value indicating if the object is an agent, and a boolean value indicating if the object is a zone.
        /// The function returns true if the spot is free; otherwise, it returns false.
        /// </summary>
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

        /// <summary>
        /// The ObjectOutsideOfBounds function checks if a game object is outside of the arena bounds (walls).
        /// It takes two parameters: a position and a bounding box.
        /// The function returns true if the object is outside of the bounds; otherwise, it returns false.
        /// </summary>
        private bool ObjectOutsideOfBounds(Vector3 position, Vector3 boundingBox)
        {
            return position.x > boundingBox.x
                && position.x < _rangeX - boundingBox.x
                && position.z > boundingBox.z
                && position.z < _rangeZ - boundingBox.z;
        }

        #endregion

        #region Goal Spawner-Logic Methods

        /// <summary>
        /// Updates the number of goals in the goodGoalsMultiSpawned list.
        /// </summary>
        private void updateGoodGoalsMulti()
        {
            int numberOfGoals = _goodGoalsMultiSpawned.Count;
            foreach (Goal goodGoalMulti in _goodGoalsMultiSpawned)
            {
                goodGoalMulti.numberOfGoals = numberOfGoals;
            }
        }

        /// <summary>
        /// Adds a goal to the goodGoalsMultiSpawned list.
        /// </summary>
        public void AddToGoodGoalsMultiSpawned(Goal ggm)
        {
            _goodGoalsMultiSpawned.Add(ggm);
            updateGoodGoalsMulti();
        }

        /// <summary>
        /// Adds a goal to the goodGoalsMultiSpawned list as a GameObject.
        /// </summary>
        public void AddToGoodGoalsMultiSpawned(GameObject ggm)
        {
            _goodGoalsMultiSpawned.Add(ggm.GetComponent<Goal>());
            updateGoodGoalsMulti();
        }

        #endregion

        #region Other Methods

        /// <summary>
        /// Returns the number of elements in a list if not null; otherwise, returns 0.
        /// </summary>
        private int optionalCount<T>(List<T> paramList)
        {
            return (paramList != null) ? paramList.Count : 0;
        }

        /// <summary>
        /// Returns the total number of objects spawned.
        /// </summary>
        public int GetTotalObjectsSpawned()
        {
            return _totalObjectsSpawned;
        }

        /// <summary>
        /// Assigns a symbol name to a component's SignBoard.
        /// </summary>
        private void AssignSymbolName(GameObject gameObjectInstance, string sName, Vector3 color)
        {
            SignBoard SP = gameObjectInstance.GetComponent<SignBoard>();
            if (SP != null)
            {
                if (color != new Vector3(-1, -1, -1))
                {
                    SP.SetColourOverride(color, true);
                }

                SP.SetSymbol(sName, true);
                Debug.Log("Assigned symbol name: " + sName + " to " + gameObjectInstance.name);
            }
            else
            {
                Debug.Log("No SignBoard component found on " + gameObjectInstance.name);
            }
        }

        /// <summary>
        /// Assigns a float value to a component's timing parameter.
        /// </summary>
        private void AssignTimingNumber<T>(string paramName, float value, T component)
        {
            paramName = paramName[0].ToString().ToUpper() + paramName.Substring(1);
            MethodInfo SetMethod = component.GetType().GetMethod("Set" + (paramName));

            if (SetMethod != null)
            {
                SetMethod.Invoke(component, new object[] { value });
            }
        }

        #endregion
    }
}
