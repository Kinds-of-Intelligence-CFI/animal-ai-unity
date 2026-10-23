using System;
using System.Collections.Generic;
using UnityEngine;
using Lights;
using System.Text;
using System.Linq;
using YAMLDefs;
using Operations;

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
        public List<int> teams = null;

        // SpawnerButton
        public List<float> moveDurations = null;
        public List<float> resetDurations = null;
        public float SpawnProbability { get; private set; }
        public List<string> RewardNames { get; private set; }
        public List<float> RewardWeights { get; private set; }
        public Vector3 rewardSpawnPos { get; private set; }
        public List<int> maxRewardCounts { get; private set; }
        public Vector3 spawnedRewardSize { get; private set; }
        public Dictionary<int, int> originalToNewIDMapping = new Dictionary<int, int>();

        // Trigger/DataZone
        public List<string> triggerZoneID = null;
        public bool zoneVisibility = true;

        // Generic
        public List<Operations.Operation> operations { get; private set; }

        /// <summary>
        /// Whether this item describes an agent (an item named Agent) rather than an object to spawn.
        /// </summary>
        public bool IsAgent => name == ArenaConfiguration.ItemNameForAgents;

        public Spawnable(GameObject obj)
        {
            name = obj.name;
            gameObject = obj;
            positions = new List<Vector3>();
            rotations = new List<float>();
            sizes = new List<Vector3>();
            colors = new List<Vector3>();
        }

        public Spawnable(YAMLDefs.Item yamlItem)
        {
            name = yamlItem.name;
            positions = yamlItem.positions;
            rotations = yamlItem.rotations;
            sizes = yamlItem.sizes;
            colors = initVec3sFromRGBs(yamlItem.colors);
            name = AliasMapper.ResolveAlias(yamlItem.name);

            skins = yamlItem.skins;
            frozenAgentDelays = yamlItem.frozenAgentDelays;
            teams = yamlItem.teams;

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
            spawnedRewardSize = yamlItem.spawnedRewardSize;

            triggerZoneID = yamlItem.triggerZoneID;
            zoneVisibility = yamlItem.zoneVisibility;

            operations = yamlItem.operations;
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
    /// How a multi-agent arena decides its episode is over.
    /// </summary>
    public enum EpisodeEndPolicy
    {
        /// The episode ends for every agent as soon as any agent finishes.
        Any,
        /// The episode ends once every agent has finished; finished agents are frozen until then.
        All,
    }

    public class ArenaConfiguration
    {
        public const string ItemNameForAgents = "Agent";

        public int TimeLimit = 0;
        public List<Spawnable> spawnables = new List<Spawnable>();
        public LightsSwitch lightsSwitch = new LightsSwitch();
        public bool toUpdate = false;
        public string protoString = "";
        public int randomSeed = 0;
        public bool mergeNextArena = false;
        public float passMark = 0;
        public EpisodeEndPolicy episodeEndPolicy = EpisodeEndPolicy.Any;

        /// <summary>
        /// The arena's Agent items in order: item k describes agent k. Empty when the arena doesn't
        /// specify an agent (a single agent is then spawned with default settings).
        /// </summary>
        public List<Spawnable> AgentSpawnables
        {
            get { return spawnables.FindAll(s => s.IsAgent); }
        }

        /// <summary>
        /// Number of agents this arena needs: one per Agent item, and at least one.
        /// </summary>
        public int AgentCount
        {
            get { return Math.Max(1, AgentSpawnables.Count); }
        }

        /// <summary>
        /// The team requested for agent <paramref name="agentIndex"/> (its Agent item's <c>teams</c> entry),
        /// or null when this arena does not specify one.
        /// </summary>
        public int? TeamForAgent(int agentIndex)
        {
            List<Spawnable> agents = AgentSpawnables;
            if (agentIndex >= agents.Count)
            {
                return null;
            }
            List<int> teams = agents[agentIndex].teams;
            return teams != null && teams.Count > 0 ? teams[0] : (int?)null;
        }

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
            episodeEndPolicy = ParseEpisodeEndPolicy(yamlArena.episodeEnd);

            ValidateAgentTeams();
        }

        private static EpisodeEndPolicy ParseEpisodeEndPolicy(string value)
        {
            /* Matched exactly, like other YAML strings; an empty value is an error rather than the default */
            switch (value)
            {
                case "any":
                    return EpisodeEndPolicy.Any;
                case "all":
                    return EpisodeEndPolicy.All;
                default:
                    throw new ArgumentException(
                        $"Invalid episodeEnd value '{value}'. Expected 'any' or 'all'."
                    );
            }
        }

        private void ValidateAgentTeams()
        {
            List<Spawnable> agents = AgentSpawnables;
            for (int k = 0; k < agents.Count; k++)
            {
                List<int> teams = agents[k].teams;
                if (teams == null)
                {
                    continue;
                }
                if (teams.Count > 1)
                {
                    throw new ArgumentException(
                        $"Agent item {k} lists {teams.Count} teams. Each Agent item is one agent, so give it "
                            + "a single team (e.g. teams: [1]) and add an Agent item per agent."
                    );
                }
                if (teams.Exists(t => t < 0))
                {
                    throw new ArgumentException($"Agent item {k} has a negative team; teams must be >= 0.");
                }
            }
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
        public int? initialPerspective { get; set; } = null;
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
            configurations.Clear();
            List<int> existingIds = new List<int>();
            int nextAvailableId = 0;

            foreach (KeyValuePair<int, YAMLDefs.Arena> arenaConfiguration in yamlArenaConfig.arenas)
            {
                int currentID = arenaConfiguration.Key;

                if (existingIds.Contains(currentID) || currentID < 0)
                {
                    Debug.LogWarning(
                        $"Issue with arenaID: {currentID}. Assigning a new unique ID: {nextAvailableId}."
                    );
                    Add(nextAvailableId, arenaConfiguration.Value);
                    existingIds.Add(nextAvailableId);
                }
                else
                {
                    Add(currentID, arenaConfiguration.Value);
                    existingIds.Add(currentID);
                }

                nextAvailableId = existingIds.Max() + 1;
            }

            randomizeArenas = yamlArenaConfig.randomizeArenas;
            showNotification = yamlArenaConfig.showNotification;
            canResetEpisode = yamlArenaConfig.canResetEpisode;
            canChangePerspective = yamlArenaConfig.canChangePerspective;
            initialPerspective = yamlArenaConfig.initialPerspective;

            ValidateAgentTeamsAcrossArenas();
            ValidateMergedArenaAgentCounts();
        }

        /// <summary>
        /// An arena with mergeNextArena hands over to the next arena without ending the episode, so the two
        /// must have the same number of agents: otherwise agents would be switched on or off mid-episode.
        /// </summary>
        private void ValidateMergedArenaAgentCounts()
        {
            foreach (KeyValuePair<int, ArenaConfiguration> entry in configurations)
            {
                if (
                    entry.Value.mergeNextArena
                    && configurations.TryGetValue(entry.Key + 1, out ArenaConfiguration next)
                    && next.AgentCount != entry.Value.AgentCount
                )
                {
                    throw new ArgumentException(
                        $"Arena {entry.Key} has {entry.Value.AgentCount} agent(s) and merges into arena "
                            + $"{entry.Key + 1}, which has {next.AgentCount}. Merged arenas must have the same "
                            + "number of Agent items."
                    );
                }
            }
        }

        /// <summary>
        /// Agents persist across arenas and an agent's ML-Agents TeamId cannot change once it has been
        /// created, so every arena that specifies a team for agent k must specify the same team.
        /// </summary>
        private void ValidateAgentTeamsAcrossArenas()
        {
            Dictionary<int, int> teamByAgent = new Dictionary<int, int>();
            foreach (KeyValuePair<int, ArenaConfiguration> entry in configurations)
            {
                ArenaConfiguration configuration = entry.Value;
                for (int agentIndex = 0; agentIndex < configuration.AgentCount; agentIndex++)
                {
                    int? team = configuration.TeamForAgent(agentIndex);
                    if (team == null)
                    {
                        continue;
                    }
                    if (teamByAgent.TryGetValue(agentIndex, out int existingTeam) && existingTeam != team)
                    {
                        throw new ArgumentException(
                            $"Agent {agentIndex} is assigned to team {team} in arena {entry.Key} but to team "
                                + $"{existingTeam} in another arena. Agent teams must be the same in every arena."
                        );
                    }
                    teamByAgent[agentIndex] = team.Value;
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

        public void ClearConfigurations()
        {
            configurations.Clear();
        }
    }
}
