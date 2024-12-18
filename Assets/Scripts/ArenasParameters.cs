using System.Collections.Generic;
using UnityEngine;
using Lights;
using System.Text;
using System.Linq;
using YAMLDefs;

/// <summary>
/// The classes in this file are used to store the parameters for the arenas.
/// These parameters are read from a YAML file and used to configure the arenas.
/// </summary>
namespace ArenasParameters
{
    [System.Serializable]
    public class ListOfPrefabs
    {
        public List<GameObject> allPrefabs;

        public List<GameObject> GetList()
        {
            return allPrefabs;
        }
    }

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
            name = AliasMapper.ResolveAlias(yamlItem.name);

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

    public class ArenaConfiguration
    {
        public int TimeLimit = 0;
        public List<Spawnable> spawnables = new List<Spawnable>();
        public LightsSwitch lightsSwitch = new LightsSwitch();
        public bool toUpdate = false;
        public string protoString = "";
        public int randomSeed = 0;
        public bool mergeNextArena = false;
        public float passMark = 0;

        public ArenaConfiguration() { }

        public ArenaConfiguration(ListOfPrefabs listPrefabs)
        {
            foreach (GameObject prefab in listPrefabs.allPrefabs)
            {
                spawnables.Add(new Spawnable(prefab));
            }
            TimeLimit = 0;
            toUpdate = true;
        }

        internal ArenaConfiguration(YAMLDefs.Arena yamlArena)
        {
            TimeLimit = yamlArena.timeLimit;
            passMark = yamlArena.passMark;
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

        public void SetGameObject(List<GameObject> listObj)
        {
            foreach (Spawnable spawn in spawnables)
            {
                spawn.gameObject = listObj.Find(x => x.name == spawn.name);
            }
        }
    }

    public class ArenasConfigurations
    {
        /*
           The configurations dictionary is used to store the configurations for each arena.
           The key is the arena ID and the value is the ArenaConfiguration object.
         */
        public Dictionary<int, ArenaConfiguration> configurations;
        public int seed;
        public bool randomizeArenas = false;
        public bool showNotification { get; set; } = false;
        public bool canResetEpisode { get; set; } = true;
        public bool canChangePerspective { get; set; } = true;
        public int CurrentArenaID { get; set; } = 0;

        public ArenasConfigurations()
        {
            configurations = new Dictionary<int, ArenaConfiguration>();
        }

        public ArenaConfiguration CurrentArenaConfiguration
        {
            get
            {
                return configurations.ContainsKey(CurrentArenaID)
                    ? configurations[CurrentArenaID]
                    : null;
            }
        }

        public void Add(int k, YAMLDefs.Arena yamlConfig)
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
        }

        public void AddAdditionalArenas(YAMLDefs.ArenaConfig yamlArenaConfig)
        {
            foreach (YAMLDefs.Arena arena in yamlArenaConfig.arenas.Values)
            {
                int i = configurations.Count;
                Add(i, arena);
            }
        }

        public void UpdateWithYAML(YAMLDefs.ArenaConfig yamlArenaConfig)
        {
            // Clear existing configurations before updating
            configurations.Clear();

            // Gather all arenas from the YAML config into a list, ignoring the given keys
            List<YAMLDefs.Arena> arenasList = new List<YAMLDefs.Arena>(yamlArenaConfig.arenas.Values);

            // Assign new IDs starting from 0 in the order they appear, which simplifies the logic
            for (int i = 0; i < arenasList.Count; i++)
            {
                Add(i, arenasList[i]);
            }

            randomizeArenas = yamlArenaConfig.randomizeArenas;
            showNotification = yamlArenaConfig.showNotification;
            canResetEpisode = yamlArenaConfig.canResetEpisode;
            canChangePerspective = yamlArenaConfig.canChangePerspective;
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

        public void ClearConfigurations()
        {
            configurations.Clear();
        }
    }
}