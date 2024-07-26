using System.Collections.Generic;
using System;
using UnityEngine;
using Lights;
using System.Text;
using System.Linq;
using YAMLDefs;

/// <summary>
/// The classes in this file are used to store the parameters for the arenas.
/// These parameters are read from a YAML file and used to configure the arenas.
/// </summary>

// TODO: Optimize and refactor this script.
namespace ArenasParameters
{
    /// <summary>
    /// The ListOfPrefabs class is a simple data container that holds a list of GameObject instances and provides a method to access that list.
    /// </summary>
    [System.Serializable]
    public class ListOfPrefabs
    {
        public List<GameObject> allPrefabs;

        public List<GameObject> GetList()
        {
            return allPrefabs;
        }
    }

    /// <summary>
    /// The list of prefabs that can be passed as items to spawn in the various arenas.
    /// </summary>
    public class Spawnable
    {
        // ======== REQUIRED PARAMETERS ========
        public string name = "";
        public GameObject gameObject = null;
        public List<Vector3> positions = null;
        public List<float> rotations = null;
        public List<Vector3> sizes = null;
        public List<Vector3> colors = null;

        // ======== EXTRA/OPTIONAL PARAMETERS ========

        // Spawners/Dispensers/Tree
        public List<string> skins = null;
        public List<string> symbolNames = null;
        public List<float> delays = null;
        public List<float> initialValues = null;
        public List<float> finalValues = null;
        public List<float> changeRates = null;
        public List<int> spawnCounts = null;
        public List<Vector3> spawnColors = null;
        public List<float> timesBetweenSpawns = null;
        public List<float> ripenTimes = null;
        public List<float> doorDelays = null;
        public List<float> timesBetweenDoorOpens = null;
        public List<float> frozenAgentDelays = null;

        // SpawnerButton
        public List<float> moveDurations = null;
        public List<float> resetDurations = null;
        public float SpawnProbability { get; private set; }
        public List<string> RewardNames { get; private set; }
        public List<float> RewardWeights { get; private set; }
        public Vector3 rewardSpawnPos { get; private set; }
        public List<int> maxRewardCounts { get; private set; }
        public Dictionary<int, int> originalToNewIDMapping = new Dictionary<int, int>();

        // Trigger/DataZone
        public List<string> triggerZoneID = null;
        public bool zoneVisibility = true;

        /// <summary>
        /// The purpose of this constructor is to initialize the Spawnable object with the properties of the provided GameObject.
        /// The name property of the Spawnable object is set to the name of the GameObject, and the gameObject property of the Spawnable object is set to the GameObject itself.
        /// </summary>
        public Spawnable(GameObject obj)
        {
            name = obj.name;
            gameObject = obj;
            positions = new List<Vector3>();
            rotations = new List<float>();
            sizes = new List<Vector3>();
            colors = new List<Vector3>();
        }

        /// <summary>
        /// The purpose of this constructor is to initialize the properties of the Spawnable class with the values from the yamlItem object.
        /// </summary>
        internal Spawnable(YAMLDefs.Item yamlItem)
        {
            name = yamlItem.name;
            positions = yamlItem.positions;
            rotations = yamlItem.rotations;
            sizes = yamlItem.sizes;
            colors = initVec3sFromRGBs(yamlItem.colors);
            name = AliasMapper.ResolveAlias(yamlItem.name);

            // ======== EXTRA/OPTIONAL PARAMETERS ========

            skins = yamlItem.skins;
            frozenAgentDelays = yamlItem.frozenAgentDelays;

            delays = yamlItem.delays;
            initialValues = yamlItem.initialValues;
            finalValues = yamlItem.finalValues;
            changeRates = yamlItem.changeRates;
            spawnCounts = yamlItem.spawnCounts;
            spawnColors = initVec3sFromRGBs(yamlItem.spawnColors);
            timesBetweenSpawns = yamlItem.timesBetweenSpawns;
            ripenTimes = yamlItem.ripenTimes;
            doorDelays = yamlItem.doorDelays;
            timesBetweenDoorOpens = yamlItem.timesBetweenDoorOpens;

            symbolNames = yamlItem.symbolNames;

            moveDurations = yamlItem.moveDurations;
            resetDurations = yamlItem.resetDurations;
            SpawnProbability = yamlItem.spawnProbability;
            RewardNames = yamlItem.rewardNames;
            RewardWeights = yamlItem.rewardWeights;
            rewardSpawnPos = yamlItem.rewardSpawnPos;
            maxRewardCounts = yamlItem.maxRewardCounts;

            triggerZoneID = yamlItem.triggerZoneID;
            zoneVisibility = yamlItem.zoneVisibility;
        }

        /// <summary>
        /// The purpose of this method is to initialize a list of Vector3 objects from a list of RGB objects.
        /// </summary>
        internal List<Vector3> initVec3sFromRGBs(List<YAMLDefs.RGB> yamlList)
        {
            List<Vector3> cList = new List<Vector3>();
            foreach (YAMLDefs.RGB c in yamlList)
            {
                cList.Add(new Vector3(c.r, c.g, c.b));
            }
            return cList;
        }
    }

    /// <summary>
    /// The ArenaConfiguration class is used to define the configuration of an arena, such as the time limit, the spawnables, and the lights switch.
    /// </summary>
    public class ArenaConfiguration
    {
        public int TimeLimit = 0;
        public List<Spawnable> spawnables = new List<Spawnable>();
        public LightsSwitch lightsSwitch = new LightsSwitch();
        public bool toUpdate = false;
        public string protoString = "";
        public int randomSeed = 0;
        public bool mergeNextArena = false;

        public ArenaConfiguration() { }

        /// <summary>
        /// The purpose of this constructor is to initialize the properties of the ArenaConfiguration class with the values from the yamlArena object.
        /// </summary>
        public ArenaConfiguration(ListOfPrefabs listPrefabs)
        {
            foreach (GameObject prefab in listPrefabs.allPrefabs)
            {
                spawnables.Add(new Spawnable(prefab));
            }
            TimeLimit = 0;
            toUpdate = true;
        }

        /// <summary>
        /// The internal constructor initializes several properties of the ArenaConfiguration object.
        /// </summary>
        internal ArenaConfiguration(YAMLDefs.Arena yamlArena)
        {
            TimeLimit = yamlArena.timeLimit;
            spawnables = new List<Spawnable>();

            foreach (YAMLDefs.Item item in yamlArena.items)
            {
                spawnables.Add(new Spawnable(item));
            }

            List<int> blackouts = yamlArena.blackouts;
            lightsSwitch = new LightsSwitch(TimeLimit, blackouts);
            toUpdate = true;
            protoString = yamlArena.ToString();
            randomSeed = yamlArena.randomSeed;
            this.mergeNextArena = yamlArena.mergeNextArena;
        }

        /// <summary>
        /// The purpose of this method is to associate the GameObject instances with the Spawnable objects.
        /// </summary>
        public void SetGameObject(List<GameObject> listObj)
        {
            foreach (Spawnable spawn in spawnables)
            {
                spawn.gameObject = listObj.Find(x => x.name == spawn.name);
            }
        }
    }

    /// <summary>
    /// ArenaConfigurations is a dictionary of configurations for each arena.
    /// </summary>
    public class ArenasConfigurations
    {
        public Dictionary<int, ArenaConfiguration> configurations;
        public int seed;
        public static ArenasConfigurations Instance { get; private set; }
        public bool randomizeArenas = false;
        public bool showNotification { get; set; } = false;
        public bool canResetEpisode { get; set; } = true;
        public bool canChangePerspective { get; set; } = true;
        public int CurrentArenaID { get; set; } = 0;

        /// <summary>
        /// The purpose of this constructor is to initialize the configurations dictionary.
        /// </summary>
        public ArenasConfigurations()
        {
            if (Instance != null)
            {
                throw new Exception("Multiple instances of ArenasConfigurations!");
            }
            Instance = this;

            configurations = new Dictionary<int, ArenaConfiguration>();
        }

        /// <summary>
        /// This method is used to get the current arena configuration.
        /// </summary>
        public ArenaConfiguration CurrentArenaConfiguration
        {
            get
            {
                return configurations.ContainsKey(CurrentArenaID)
                    ? configurations[CurrentArenaID]
                    : null;
            }
        }

        /// <summary>
        /// This method is used to add a new arena configuration to the configurations dictionary.
        /// </summary>
        internal void Add(int k, YAMLDefs.Arena yamlConfig)
        {
            if (!configurations.ContainsKey(k))
            {
                configurations.Add(k, new ArenaConfiguration(yamlConfig));
            }
            else
            {
                if (yamlConfig.ToString() != configurations[k].protoString)
                {
                    configurations[k] = new ArenaConfiguration(yamlConfig);
                }
            }
            CurrentArenaID = k;
            yamlConfig.SetCurrentPassMark();
        }

        /// <summary>
        /// This method is used to add additional arenas to the configurations dictionary.
        /// </summary>
        public void AddAdditionalArenas(YAMLDefs.ArenaConfig yamlArenaConfig)
        {
            foreach (YAMLDefs.Arena arena in yamlArenaConfig.arenas.Values)
            {
                int i = configurations.Count;
                Add(i, arena);
                arena.SetCurrentPassMark();
            }
        }

        /// <summary>
        /// This method is used to update the current configurations with the new ones provided in the yamlArenaConfig object.
        /// Furthermore, it sets the randomizeArenas, showNotification, canResetEpisode, and canChangePerspective properties of the ArenasConfigurations object.
        /// Lastly, it ensures the arena IDs are unique and positive; assigns new IDs if required.
        /// </summary>
        public void UpdateWithYAML(YAMLDefs.ArenaConfig yamlArenaConfig)
        {
            configurations.Clear();
            List<int> existingIds = new List<int>();
            int nextAvailableId = 0;

            // Iterating over arenas in the order they appear in the YAML, top to bottom.
            foreach (KeyValuePair<int, YAMLDefs.Arena> arenaConfiguration in yamlArenaConfig.arenas)
            {
                int currentID = arenaConfiguration.Key;

                if (existingIds.Contains(currentID) || currentID < 0)
                {
                    Debug.LogWarning(
                        $"Issue with arenaID: {currentID}. Assigning a new unique ID: {nextAvailableId}."
                    );

                    // Assign a new unique ID if required.
                    Add(nextAvailableId, arenaConfiguration.Value);
                    existingIds.Add(nextAvailableId);
                }
                else
                {
                    Add(currentID, arenaConfiguration.Value);
                    existingIds.Add(currentID);
                }

                // Adjust the nextAvailableId for future entries.
                nextAvailableId = existingIds.Max() + 1;
            }

            randomizeArenas = yamlArenaConfig.randomizeArenas;
            showNotification = yamlArenaConfig.showNotification;
            canResetEpisode = yamlArenaConfig.canResetEpisode;
            canChangePerspective = yamlArenaConfig.canChangePerspective;
        }

        /// <summary>
        /// This method handles an event that is triggered when new arena configurations are received.
        /// It extracts the YAML data from the event, converts it into an ArenaConfig object, and then updates the current configurations with the new ones.
        /// </summary>
        public void UpdateWithConfigurationsReceived(
            object sender,
            ArenasParametersEventArgs arenasParametersEvent
        )
        {
            byte[] arenas = arenasParametersEvent.arenas_yaml;
            var YAMLReader = new YAMLDefs.YAMLReader();
            string utfString = Encoding.UTF8.GetString(arenas, 0, arenas.Length);
            var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(utfString);
            UpdateWithYAML(parsed);
        }

        /// <summary>
        /// The purpose of this method is to iterate over each entry in the configurations dictionary and set the toUpdate property of each ArenaConfiguration object to false.
        /// </summary>
        public void SetAllToUpdated()
        {
            foreach (KeyValuePair<int, ArenaConfiguration> configuration in configurations)
            {
                configuration.Value.toUpdate = false;
            }
        }

        public void ClearConfigurations()
        {
            configurations.Clear();
        }
    }
}
