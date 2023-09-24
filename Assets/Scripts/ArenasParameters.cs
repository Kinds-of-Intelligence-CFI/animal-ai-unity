using System.Collections.Generic;
using System;
using UnityEngine;
using Lights;
using System.Text;

namespace ArenasParameters
{
    /// <summary>
    /// The list of prefabs that can be passed as items to spawn in the various arenas
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
    /// We define a Spawnable item as a GameObject and a list of parameters to spawn it. These
    /// include whether or not colors and sizes should be randomized, as well as lists of positions
    /// rotations and sizes the user can provide. Any of these parameters left empty by the user
    /// will be randomized at the time we spawn the associated GameObject
    /// </summary>
    public class Spawnable
    {
        public string name = "";
        public GameObject gameObject = null;
        public List<Vector3> positions = null;
        public List<float> rotations = null;
        public List<Vector3> sizes = null;
        public List<Vector3> colors = null;

        // ======== EXTRA/OPTIONAL PARAMETERS ========
        // use for SignPosterboard symbols, Decay/SizeChange rates, Dispenser settings, etc.

        // Spawners/Dispensers //
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

        // InteractiveButton //
        public List<float> moveDurations = null;
        public List<float> resetDurations = null;
        public float SpawnProbability { get; private set; }
        public List<string> RewardNames { get; private set; }
        public List<float> RewardWeights { get; private set; }
        public Vector3 rewardSpawnPos { get; private set; }
        public List<int> maxRewardCounts { get; private set; }

        public Spawnable(GameObject obj)
        {
            name = obj.name;
            gameObject = obj;
            positions = new List<Vector3>();
            rotations = new List<float>();
            sizes = new List<Vector3>();
            colors = new List<Vector3>();
        }

        internal Spawnable(YAMLDefs.Item yamlItem)
        {
            name = yamlItem.name;
            positions = yamlItem.positions;
            rotations = yamlItem.rotations;
            sizes = yamlItem.sizes;
            colors = initVec3sFromRGBs(yamlItem.colors);

            // ======== EXTRA/OPTIONAL PARAMETERS ========
            // use for SignPosterboard symbols, Decay/SizeChange rates, Dispenser settings, etc.

            skins = yamlItem.skins;
            symbolNames = yamlItem.symbolNames;
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
            frozenAgentDelays = yamlItem.frozenAgentDelays;
            // InteractiveButton //
            moveDurations = yamlItem.moveDurations;
            resetDurations = yamlItem.resetDurations;
            SpawnProbability = yamlItem.spawnProbability;
            RewardNames = yamlItem.rewardNames;
            RewardWeights = yamlItem.rewardWeights;
            rewardSpawnPos = yamlItem.rewardSpawnPos;
            maxRewardCounts = yamlItem.maxRewardCounts;
        }

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
    /// An ArenaConfiguration contains the list of items that can be spawned in the arena, the
    /// maximum number of steps which can vary from one episode to the next (T) and whether all
    /// sizes and colors can be randomized
    /// </summary>
    public class ArenaConfiguration
    {
        public int T = 1000;
        public List<Spawnable> spawnables = new List<Spawnable>();
        public LightsSwitch lightsSwitch = new LightsSwitch();
        public bool toUpdate = false;
        public string protoString = "";
        public int randomSeed = 0;
        public bool showNotification { get; set; } = false;
        public bool canResetEpisode { get; set; } = true;
        public bool canChangePerspective { get; set; } = true;
        public int defaultPerspective { get; set; } = 0;

        public ArenaConfiguration() { }

        public ArenaConfiguration(ListOfPrefabs listPrefabs)
        {
            foreach (GameObject prefab in listPrefabs.allPrefabs)
            {
                spawnables.Add(new Spawnable(prefab));
            }
            T = 0;
            toUpdate = true;
        }

        internal ArenaConfiguration(YAMLDefs.Arena yamlArena)
        {
            T = yamlArena.t;
            spawnables = new List<Spawnable>();

            foreach (YAMLDefs.Item item in yamlArena.items)
            {
                spawnables.Add(new Spawnable(item));
            }

            List<int> blackouts = yamlArena.blackouts;
            lightsSwitch = new LightsSwitch(T, blackouts);
            toUpdate = true;
            protoString = yamlArena.ToString();
            randomSeed = yamlArena.random_seed;
            this.showNotification = yamlArena.showNotification;
            this.canResetEpisode = yamlArena.canResetEpisode;
            this.canChangePerspective = yamlArena.canChangePerspective;
            this.defaultPerspective = yamlArena.defaultPerspective;
        }

        public void SetGameObject(List<GameObject> listObj)
        {
            foreach (Spawnable spawn in spawnables)
            {
                spawn.gameObject = listObj.Find(x => x.name == spawn.name);
            }
        }
    }

    /// <summary>
    /// ArenaConfigurations is a dictionary of configurations for each arena
    /// </summary>
    public class ArenasConfigurations
    {
        public Dictionary<int, ArenaConfiguration> configurations;
        public int seed;
        public static ArenasConfigurations Instance { get; private set; }

        public ArenasConfigurations()
        {
            if (Instance != null)
            {
                throw new Exception("Multiple instances of ArenasConfigurations!");
            }
            Instance = this;

            configurations = new Dictionary<int, ArenaConfiguration>();
        }

        public ArenaConfiguration CurrentArenaConfiguration
        {
            get
            {
                if (configurations.ContainsKey(-1)) // Use '-1' as the default configuration key
                {
                    return configurations[-1];
                }
                return null;
            }
        }

        public void SetCurrentArenaConfiguration(ArenaConfiguration config)
        {
            if (configurations.ContainsKey(-1))
            {
                configurations[-1] = config;
            }
            else
            {
                configurations.Add(-1, config);
            }
        }

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
            SetCurrentArenaConfiguration(configurations[k]);
            yamlConfig.SetCurrentPassMark();
        }

        public void AddAdditionalArenas(YAMLDefs.ArenaConfig yamlArenaConfig)
        {
            foreach (YAMLDefs.Arena arena in yamlArenaConfig.arenas.Values)
            {
                int i = configurations.Count;
                Add(i, arena);
                arena.SetCurrentPassMark();
            }
        }

        public void UpdateWithYAML(YAMLDefs.ArenaConfig yamlArenaConfig)
        {
            if (yamlArenaConfig.arenas.ContainsKey(-1))
            {
                Debug.Log("We have a default arena key");
                Add(-1, yamlArenaConfig.arenas[-1]); // Map YAML key '-1' to dictionary key '-1'
            }
            else
            {
                Debug.Log("We have multiple arena keys");
                foreach (
                    KeyValuePair<int, YAMLDefs.Arena> arenaConfiguration in yamlArenaConfig.arenas
                )
                {
                    Add(arenaConfiguration.Key, arenaConfiguration.Value);
                }
            }
        }

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

        public void SetAllToUpdated()
        {
            foreach (KeyValuePair<int, ArenaConfiguration> configuration in configurations)
            {
                configuration.Value.toUpdate = false;
            }
        }

        public void Clear()
        {
            configurations.Clear();
        }
    }
}
