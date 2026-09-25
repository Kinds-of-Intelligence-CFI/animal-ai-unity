using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using ArenaBuilders;
using UnityEngineExtensions;
using ArenasParameters;
using Random = UnityEngine.Random;
using System.Linq;
using Operations;

/// <summary>
/// This class is responsible for managing the training arena.
/// It contains the logic to reset the arena, update the light status, and handle the spawning of rewards.
/// It also initializes the components required for the arena, such as the ArenaBuilder and the AAI3EnvironmentManager.
///
/// The arena owns the episode and its agents: it creates one agent per agent the configuration asks for
/// (all from the same prefab), agents report to it when they finish (see <see cref="OnAgentFinished"/>),
/// and it decides when the episode ends for everyone, rebuilds itself and repositions the agents.
/// </summary>
public class TrainingArena : MonoBehaviour
{
    // Functions in the hosting web page (WebGL template) for signalling that the experiment is complete
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void NotifyExperimentComplete();
    [DllImport("__Internal")]
    private static extern void NotifyUploadComplete();
    #endif

    /// <summary>Resources path of the prefab every agent is instantiated from.</summary>
    public const string AgentPrefabResourcePath = "AAI3Agent";

    /// <summary>How long a success/failure notification stays up before the episode ends.</summary>
    public const float NotificationDisplaySeconds = 2.5f;

    /// <summary>Agent counts above this still work, but each agent costs sensors and rendering.</summary>
    private const int RecommendedMaxAgents = 8;

    [SerializeField]
    private ListOfPrefabs prefabs;

    [SerializeField]
    private GameObject spawnedObjectsHolder;

    [SerializeField]
    private int maxSpawnAttemptsForAgent = 100;

    [SerializeField]
    private int maxSpawnAttemptsForPrefabs = 20;

    [HideInInspector]
    public int arenaID = -1;

    private ArenaBuilder _builder;
    private ArenaConfiguration _arenaConfiguration = new ArenaConfiguration();
    private AAI3EnvironmentManager _environmentManager;
    private GameObject _agentPrefab;
    /* Every agent created so far, by agent index, with the prefab instance holding it (the agent, its
       cameras and its black screens). Agents beyond the current arena's agent count are kept inactive. */
    private readonly List<TrainingAgent> _agents = new List<TrainingAgent>();
    private readonly List<GameObject> _agentInstances = new List<GameObject>();
    private readonly List<TrainingAgent> _activeAgents = new List<TrainingAgent>();
    private SimpleMultiAgentGroup _agentGroup;
    /* Agents that have finished the current episode, and whether they passed the arena */
    private readonly Dictionary<TrainingAgent, bool> _finishedAgents = new Dictionary<TrainingAgent, bool>();
    private Coroutine _episodeEndRoutine;
    private bool _notificationShown = false;
    /* Browser builds only: set once the experiment has finished (no further arenas are played) */
    private bool _experimentOver = false;
    private List<Fade> _fades = new List<Fade>();
    private bool _lightStatus = true;
    private int _agentDecisionInterval;
    private bool isFirstArenaReset = true;
    private List<GameObject> spawnedRewards = new List<GameObject>();
    private List<int> playedArenas = new List<int>();
    private List<int> _mergedArenas = null;
    public bool showNotification { get; set; }

    /// <summary>The agents taking part in the current arena, in agent-index order.</summary>
    public IReadOnlyList<TrainingAgent> Agents
    {
        get { return _activeAgents; }
    }

    /// <summary>Agent 0: the one player controls, the UI and the screen cameras follow.</summary>
    public TrainingAgent PrimaryAgent
    {
        get { return _agents.Count > 0 ? _agents[0] : null; }
    }

    public bool IsFirstArenaReset
    {
        get { return isFirstArenaReset; }
        set { isFirstArenaReset = value; }
    }

    public bool AllArenasAttempted()
    {
        if (isFirstArenaReset)
        {
            Debug.LogError("Unexpected flow: AllArenasAttempted() called before first arena was reset");
            return false;
        }

        int totalArenas = _environmentManager.GetTotalArenas();

        // arenaID has just ended but not yet been recorded in playedArenas (that happens in the
        // next SetNextArenaID call), so include it explicitly in the count.
        return playedArenas.Count + 1 >= totalArenas;
    }

    public bool mergeNextArena
    {
        get { return _arenaConfiguration.mergeNextArena; }
    }

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

    void Start()
    {
        /* ML-Agents only raises its environment reset on the Academy's first step. If the Academy has
           already stepped (the scene was reloaded, e.g. between play mode tests) that won't happen again,
           so build the arena now. */
        if (Academy.Instance.TotalStepCount != 0 && _environmentManager.GetTotalArenas() > 0)
        {
            ResetArena();
        }
    }

    void FixedUpdate()
    {
        UpdateLightStatus();
    }

    private void OnDestroy()
    {
        ToggleObject.RewardSpawned -= OnRewardSpawned;
        if (Academy.IsInitialized)
        {
            Academy.Instance.OnEnvironmentReset -= OnEnvironmentReset;
        }
        _agentGroup?.Dispose();
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
        _environmentManager = GameObject.FindAnyObjectByType<AAI3EnvironmentManager>();
        if (_environmentManager == null)
        {
            /* The manager supplies the configurations and sets up every agent (ConfigureAgent) */
            throw new InvalidOperationException("AAI3EnvironmentManager is not found in the scene");
        }

        _agentPrefab = Resources.Load<GameObject>(AgentPrefabResourcePath);
        if (_agentPrefab == null)
        {
            throw new InvalidOperationException(
                $"Agent prefab not found at Resources/{AgentPrefabResourcePath}"
            );
        }
        _agentGroup = new SimpleMultiAgentGroup();

        /* timeLimit and blackouts are scaled by the prefab's DecisionPeriod, not the configured one.
           This is probably wrong (agents decide every configured decisionPeriod steps, 3 by default, while
           the prefab says 5), but changing it changes episode lengths for every existing config. */
        _agentDecisionInterval = _agentPrefab.GetComponentInChildren<DecisionRequester>(true).DecisionPeriod;

        /* The primary agent always exists, so player controls and the UI can bind to it before the
           first configuration is known. Any further agents are created on the first arena reset. */
        SyncAgentsToArena(1);
        if (PrimaryAgent == null)
        {
            throw new InvalidOperationException("The arena failed to create its primary agent");
        }

        ToggleObject.RewardSpawned += OnRewardSpawned;

        /* The arena resets itself whenever ML-Agents resets the environment: on the first step and on
           every reset requested by the trainer. Agents never reset the arena from OnEpisodeBegin. */
        Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
    }

    private void OnEnvironmentReset()
    {
        ResetArena();
    }

    /// <summary>
    /// Provides a list of the arenas in the current config file that are preceeded by an arena with
    /// the mergeNextArena property, so that we can avoid loading them when arenas are randomised.
    /// </summary>
    private List<int> GetMergedArenas()
    {
        List<int> mergedArenas = new List<int>();
        int totalArenas = _environmentManager.GetTotalArenas();
        ArenaConfiguration currentArena = _environmentManager.GetConfiguration(0);
        bool currentlyMerged = currentArena.mergeNextArena;
        for (int i = 1; i < totalArenas; i++)
        {
            if (currentlyMerged)
            {
                mergedArenas.Add(i);
            }
            currentArena = _environmentManager.GetConfiguration(i);
            currentlyMerged = currentArena.mergeNextArena;
        }
        return mergedArenas;
    }

    /// <summary>
    /// Resets the arena by destroying existing objects and spawning new ones based on the current arena configuration.
    /// This is a custom implementation of the ResetAcademy method from the MLAgents library. It is called when
    /// ML-Agents resets the environment and at the end of every arena episode (see <see cref="EndArenaEpisode"/>).
    /// </summary>
    public void ResetArena()
    {
        Debug.Log("Resetting Arena");

        CancelPendingEpisodeEnd();
        _finishedAgents.Clear();

        CleanUpSpawnedObjects();

        SetNextArenaID();

        ArenaConfiguration newConfiguration = _environmentManager.GetConfiguration(arenaID);

        ApplyNewArenaConfiguration(newConfiguration);

        CleanupRewards();

        NotifyArenaChange();
    }

    public void LoadNextArena()
    {
        if (isFirstArenaReset)
        {
            throw new InvalidOperationException("LoadNextArena called before first reset");
        }
        Debug.Log($"Loading next arena. Previous: {arenaID}, next: {arenaID + 1}");

        CancelPendingEpisodeEnd();
        _finishedAgents.Clear();
        /* Under episodeEnd: all, agents that finished early are frozen waiting for the others */
        foreach (TrainingAgent agent in _activeAgents)
        {
            agent.FreezeAgent(false);
        }

        CleanUpSpawnedObjects();

        arenaID += 1;

        ArenaConfiguration newConfiguration = _environmentManager.GetConfiguration(arenaID);

        int totalArenas = _environmentManager.GetTotalArenas();
        if (arenaID == totalArenas - 1 && newConfiguration.mergeNextArena)
        {
            throw new InvalidOperationException(
                "The final arena cannot have mergeNextArena set to true."
            );
        }

        ApplyNewArenaConfiguration(newConfiguration);

        CleanupRewards();

        NotifyArenaChange();
    }

    public GameObject AddNewItemToArena(YAMLDefs.Item spawnable)
    {
        GameObject holderInstance = GameObject.FindGameObjectWithTag("spawnedObjects");
        if (holderInstance == null)
        {
            Debug.LogError("Can't find the spawned objects holder instance in scene");
            return null;
        }

        Spawnable spawnableToUse = new Spawnable(spawnable);
        spawnableToUse.gameObject = prefabs.GetList().Find(x => x.name == spawnable.name);

        return _builder.InstantiateSpawnable(spawnableToUse, holderInstance, true);
    }

    private void CleanUpSpawnedObjects()
    {
        foreach (GameObject holder in transform.FindChildrenWithTag("spawnedObjects"))
        {
            holder.SetActive(false);
            Destroy(holder);
        }
    }

    private void SetNextArenaID()
    {
        int totalArenas = _environmentManager.GetTotalArenas();
        bool randomizeArenas = _environmentManager.GetRandomizeArenasStatus();

        if (isFirstArenaReset)
        {
            isFirstArenaReset = false;

            List<int> attemptedArenas = _environmentManager.GetAttemptedArenas();
            if (attemptedArenas != null && attemptedArenas.Count > 0)
            {
                playedArenas = new List<int>(attemptedArenas);
                Debug.Log($"Initialized with {playedArenas.Count} attempted arenas");
            }

            if (randomizeArenas)
            {
                arenaID = ChooseRandomArenaID(totalArenas);
            }
            else
            {
                // For sequential mode: start from the first unattempted arena
                if (playedArenas.Count > 0)
                {
                    // Find the first arena not in playedArenas
                    arenaID = GetFirstArenaID();
                    // TODO: Replace ContainsKey bound with < totalArenas once YAMLs are validated to start from 0 (ATB-103)
                    while (_environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID) && playedArenas.Contains(arenaID))
                    {
                        arenaID++;
                    }
                    // If all arenas were attempted, wrap around to the first arena
                    if (!_environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID))
                    {
                        arenaID = GetFirstArenaID();
                        playedArenas = new List<int>();
                    }
                    Debug.Log($"Sequential mode: starting from arena {arenaID}");
                }
                else
                {
                    arenaID = GetFirstArenaID();
                }
            }
        }
        else
        {
            if (randomizeArenas)
            {
                arenaID = ChooseRandomArenaID(totalArenas);
            }
            else
            {
                // Track the current arena as completed
                if (!playedArenas.Contains(arenaID))
                {
                    playedArenas.Add(arenaID);
                }

                /* If the next arena is merged, sequentially search for the next unmerged one */
                ArenaConfiguration precedingArena = _arenaConfiguration;
                // TODO: Replace ContainsKey wrap with % totalArenas once YAMLs are validated to start from 0 (ATB-103)
                arenaID = _environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID + 1) ? arenaID + 1 : GetFirstArenaID();
                while (precedingArena.mergeNextArena)
                {
                    precedingArena = _environmentManager.GetConfiguration(arenaID);
                    arenaID = _environmentManager._arenasConfigurations.configurations.ContainsKey(arenaID + 1) ? arenaID + 1 : GetFirstArenaID();
                }

                // Reset playedArenas when we've completed all arenas
                if (playedArenas.Count >= totalArenas)
                {
                    playedArenas = new List<int>();
                }
            }
        }
    }

    private int GetFirstArenaID()
    {
        if (!_environmentManager._arenasConfigurations.configurations.ContainsKey(0))
        {
            // TODO: Don't allow running arenas with nonstandard arena numbering (ATB-103)
            Debug.LogWarning("Arena 0 not found in configuration. Starting from arena 1.");
            return 1;
        }
        return 0;
    }

    private int ChooseRandomArenaID(int totalArenas)
    {
        if (_mergedArenas == null)
        {
            _mergedArenas = GetMergedArenas();
        }

        // Track the current arena as played
        if (!playedArenas.Contains(arenaID))
        {
            playedArenas.Add(arenaID);
        }

        if (playedArenas.Count >= totalArenas)
        {
            playedArenas = new List<int> { arenaID };
        }

        var availableArenas = _environmentManager._arenasConfigurations.configurations.Keys
            .Except(playedArenas)
            .Except(_mergedArenas)
            .ToList();
        return availableArenas[Random.Range(0, availableArenas.Count)];
    }

    /*
       Note: to update the active arena to a new ID the following must be called in sequence
       GetConfiguration, ApplyNewArenaConfiguration, CleanupRewards, NotifyArenaChange
    */
    private void ApplyNewArenaConfiguration(ArenaConfiguration newConfiguration)
    {
        _arenaConfiguration = newConfiguration;
        _arenaConfiguration.passMark = newConfiguration.passMark;

        var arenasConfigurations = _environmentManager.GetArenasConfigurations();
        if (arenasConfigurations != null)
        {
            showNotification = arenasConfigurations.showNotification;
        }
        else
        {
            Debug.LogError("ArenasConfigurations is not initialized in ApplyNewArenaConfiguration.");
        }

        SyncAgentsToArena(newConfiguration.AgentCount);

        _arenaConfiguration.SetGameObject(prefabs.GetList());
        _builder.Spawnables = _arenaConfiguration.spawnables;
        _arenaConfiguration.toUpdate = false;
        foreach (TrainingAgent agent in _activeAgents)
        {
            agent.MaxStep = 0;
            agent.SetTimeLimit(_arenaConfiguration.TimeLimit * _agentDecisionInterval);
        }
        _builder.Build(_activeAgents);
        _arenaConfiguration.lightsSwitch.Reset();

        if (_arenaConfiguration.randomSeed != 0)
        {
            Random.InitState(_arenaConfiguration.randomSeed);
        }

        Debug.Log($"Final passMark value: {_arenaConfiguration.passMark}");
    }

    /// <summary>
    /// Makes exactly <paramref name="count"/> agents take part in the arena: creates any that don't
    /// exist yet, activates agents 0..count-1 and deactivates the rest.
    /// </summary>
    private void SyncAgentsToArena(int count)
    {
        if (count > 1 && _environmentManager.AgentsUsePlayerMode)
        {
            /* In player mode every agent is keyboard-controlled, so they would all move together */
            throw new InvalidOperationException(
                $"The arena has {count} Agent items, but player mode supports only one agent. Use a single "
                    + "Agent item, or run with player mode off (--playerMode 0, or play=False from Python) and "
                    + "control the agents from Python. The Unity editor always runs in player mode."
            );
        }
        if (count > RecommendedMaxAgents && count > _agents.Count)
        {
            Debug.LogWarning(
                $"Arena {arenaID} asks for {count} agents; each one adds sensors and rendering cost."
            );
        }

        for (int index = _agents.Count; index < count; index++)
        {
            CreateAgent(index, TeamForAgent(index));
        }

        /* The primary agent is created before any configuration arrives from Python, so its team is
           only known now; teams are otherwise fixed (the configuration validates they agree across arenas) */
        for (int index = 0; index < _agents.Count; index++)
        {
            ApplyTeam(_agents[index], TeamForAgent(index));
        }

        for (int index = 0; index < _agentInstances.Count; index++)
        {
            bool active = index < count;
            GameObject instance = _agentInstances[index];
            if (instance.activeSelf == active)
            {
                continue;
            }
            if (active)
            {
                /* Disabling an agent removes it from the group, so put it back before it re-initialises */
                _agentGroup.RegisterAgent(_agents[index]);
            }
            instance.SetActive(active);
        }

        _activeAgents.Clear();
        _activeAgents.AddRange(_agents.Take(count));

        /* Every agent carries its own black screens (blackouts fade each agent's cameras) */
        _fades = _activeAgents
            .SelectMany(agent => agent.transform.parent.GetComponentsInChildren<Fade>())
            .ToList();
    }

    /// <summary>
    /// Creates agent <paramref name="index"/> from the agent prefab. The Agent object is inactive in the
    /// prefab, so it is fully configured before ML-Agents initialises it. The arena builder positions it
    /// on every reset (see ArenaBuilder.PlaceAgents).
    /// </summary>
    private void CreateAgent(int index, int teamId)
    {
        GameObject instance = Instantiate(_agentPrefab, transform);
        instance.name = $"AAI3Agent_{index}";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        TrainingAgent agent = instance.GetComponentInChildren<TrainingAgent>(true);
        if (agent == null)
        {
            throw new InvalidOperationException(
                $"Agent prefab '{AgentPrefabResourcePath}' has no TrainingAgent component"
            );
        }
        agent.agentIndex = index;
        agent.Arena = this;
        ApplyTeam(agent, teamId);

        _environmentManager.ConfigureAgent(agent);

        if (index != 0)
        {
            /* Only the primary agent's cameras render to the screen. Camera sensors still produce
               observations for every agent: they render on demand when the sensor is read. */
            foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }
        }

        _agentGroup.RegisterAgent(agent);
        _agents.Add(agent);
        _agentInstances.Add(instance);

        agent.gameObject.SetActive(true);
        Debug.Log($"Created agent {index} (team {teamId})");
    }

    /// <summary>
    /// Sets the agent's ML-Agents team (its behavior becomes "AnimalAI?team=N"). TeamId is a plain field,
    /// so re-assigning the behavior name is what makes an already-initialised agent reload its policy.
    /// </summary>
    private static void ApplyTeam(TrainingAgent agent, int teamId)
    {
        BehaviorParameters behaviorParameters = agent.GetComponent<BehaviorParameters>();
        if (behaviorParameters == null || behaviorParameters.TeamId == teamId)
        {
            return;
        }
        behaviorParameters.TeamId = teamId;
        behaviorParameters.BehaviorName = behaviorParameters.BehaviorName;
    }

    /// <summary>
    /// The ML-Agents team for agent <paramref name="index"/>. Teams are fixed for the life of an agent,
    /// so this reads the team from any arena that specifies one (the configuration validates that
    /// they all agree) and defaults to team 0.
    /// </summary>
    private int TeamForAgent(int index)
    {
        ArenasConfigurations arenasConfigurations = _environmentManager.GetArenasConfigurations();
        if (arenasConfigurations == null)
        {
            return 0;
        }
        foreach (ArenaConfiguration configuration in arenasConfigurations.configurations.Values)
        {
            int? team = configuration.TeamForAgent(index);
            if (team != null)
            {
                return team.Value;
            }
        }
        return 0;
    }

    /// <summary>
    /// Called by an agent when it has finished the current arena (see <see cref="AgentFinishReason"/>).
    /// Depending on the arena's episode end policy this ends the episode straight away or waits for the
    /// other agents. A finished agent is frozen until the episode ends.
    /// </summary>
    public void OnAgentFinished(TrainingAgent agent, AgentFinishReason reason)
    {
        if (_experimentOver || _episodeEndRoutine != null || _finishedAgents.ContainsKey(agent))
        {
            return;
        }

        bool passed =
            reason == AgentFinishReason.ArenaCompleted
            && (_arenaConfiguration.passMark == 0 || agent.GetCumulativeReward() >= _arenaConfiguration.passMark);
        _finishedAgents[agent] = passed;

        bool everyoneFinished = _finishedAgents.Count >= _activeAgents.Count;
        if (_arenaConfiguration.episodeEndPolicy == EpisodeEndPolicy.All && !everyoneFinished)
        {
            /* The others play on, so this agent waits, frozen, until they finish */
            agent.FreezeAgent(true);
            return;
        }

        bool arenaPassed = _finishedAgents.Values.All(p => p);
        if (arenaPassed && _arenaConfiguration.mergeNextArena)
        {
            LoadNextArena();
            return;
        }

        bool showingNotification = showNotification;
        if (showNotification && NotificationManager.Instance == null)
        {
            Debug.LogError(
                "showNotification is set but the scene has no NotificationManager; ending the episode without one."
            );
            showingNotification = false;
        }

        if (showingNotification)
        {
            if (arenaPassed)
            {
                NotificationManager.Instance.ShowSuccessNotification();
            }
            else
            {
                NotificationManager.Instance.ShowFailureNotification();
            }
            _notificationShown = true;
            /* Everyone holds still while the notification is displayed */
            foreach (TrainingAgent activeAgent in _activeAgents)
            {
                activeAgent.FreezeAgent(true);
            }
            _episodeEndRoutine = StartCoroutine(FinishArenaEpisode(waitForNotification: true));
        }
        else
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            _episodeEndRoutine = StartCoroutine(FinishArenaEpisode(waitForNotification: false));
            #else
            EndArenaEpisode();
            #endif
        }
    }

    /// <summary>
    /// Ends the arena episode after the browser build has uploaded the primary agent's log (WebGL only; in
    /// the browser the experiment can also end here, after which no further arenas are played) and after
    /// any notification has been shown.
    /// </summary>
    private IEnumerator FinishArenaEpisode(bool waitForNotification)
    {
        yield return StartCoroutine(HandleArenaCompletedInBrowser());
        if (_experimentOver)
        {
            _episodeEndRoutine = null;
            yield break;
        }

        if (waitForNotification)
        {
            yield return new WaitForSeconds(NotificationDisplaySeconds);
        }
        _episodeEndRoutine = null;
        EndArenaEpisode();
    }

    /// <summary>
    /// Browser (WebGL) builds only: after an arena ends, uploads the primary agent's observation log and
    /// tells the host page when the experiment is over (a tutorial run, or every arena attempted), after
    /// which no further arenas are played. Does nothing in other builds.
    /// </summary>
    private IEnumerator HandleArenaCompletedInBrowser()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Upload CSV after each arena completion (in background, non-blocking)
        PrimaryAgent.FlushLog();

        // Extract experiment and user IDs from URL for CSV upload
        string currentUrl = Application.absoluteURL;
        Debug.Log("Current WebGL URL: " + currentUrl);
        string[] urlParts = currentUrl.Split('?');
        if (urlParts.Length == 2)
        {
            string queryString = urlParts[urlParts.Length - 1];
            string[] queryArgs = queryString.Split('&');
            if (queryArgs.Length >= 2)
            {
                string experimentId = queryArgs[0];
                string userId = queryArgs[1];

                // Don't write CSV files for tutorial runs
                if (currentUrl.Contains("tutorial"))
                {
                    NotifyExperimentComplete();
                    _experimentOver = true;
                    yield break;
                }

                if (AllArenasAttempted())
                {
                    Debug.Log("All arenas completed - notifying web page");

                    if (_notificationShown)
                    {
                        yield return new WaitForSeconds(NotificationDisplaySeconds);
                        NotificationManager.Instance.HideNotification();
                    }

                    // Show "Thank You! Redirecting..." message immediately to hide game UI
                    NotifyExperimentComplete();

                    // Final blocking upload to ensure all data is captured before redirect
                    yield return StartCoroutine(PrimaryAgent.UploadLog(experimentId, userId));
                    Debug.Log("Final CSV upload completed");
                    NotifyUploadComplete();

                    // No further arenas are played, so the episode isn't ended
                    _experimentOver = true;
                    yield break;
                }
                else
                {
                    Debug.Log("Arena completed - uploading CSV in background");
                    PrimaryAgent.StartLogUpload(experimentId, userId);
                }
            }
            else
            {
                Debug.LogError($"Unable to parse experiment/user ID from URL: {currentUrl}");
            }
        }
        else
        {
            Debug.LogError($"Unable to find config in URL: {currentUrl}");
        }
        #endif

        yield break;
    }

    /// <summary>
    /// Ends the episode for every agent in the arena and resets the arena. Agents record their final
    /// observations and reset their per-episode state first, then the arena is rebuilt and the agents
    /// repositioned.
    /// </summary>
    public void EndArenaEpisode()
    {
        CancelPendingEpisodeEnd();
        _finishedAgents.Clear();
        _agentGroup.EndGroupEpisode();
        ResetArena();
    }

    private void CancelPendingEpisodeEnd()
    {
        if (_episodeEndRoutine != null)
        {
            StopCoroutine(_episodeEndRoutine);
            _episodeEndRoutine = null;
        }
        /* Only hide a notification this arena showed: the NotificationManager singleton outlives scene
           reloads and its UI references don't, so it must not be touched unless it's in use */
        if (_notificationShown)
        {
            _notificationShown = false;
            NotificationManager.Instance.HideNotification();
        }
    }

    private void NotifyArenaChange()
    {
        _environmentManager.TriggerArenaChangeEvent(arenaID, _environmentManager.GetTotalArenas());
    }

    private void CleanupRewards()
    {
        foreach (var reward in spawnedRewards)
        {
            Destroy(reward);
        }
        spawnedRewards.Clear();
    }

    public void UpdateLightStatus()
    {
        /* All agents step in lockstep, so the primary agent's step count is the arena's */
        int stepCount = PrimaryAgent.StepCount;
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

    public int GetTotalSpawnedObjects()
    {
        return spawnedObjectsHolder.transform.childCount;
    }

    private void OnRewardSpawned(GameObject reward)
    {
        spawnedRewards.Add(reward);
    }
}
