using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ArenasParameters;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.SideChannels;
using Unity.MLAgents.Policies;
using ArenaBuilders;
using UnityEngine.Networking;
using YAMLDefs;
using System.Collections;

// TODO: Implement the following:
// 1. Implement testing for the YAML files in WebGL builds. Also create new unit tests specifically for this feature (WEBGL).
// 2. Implement the logic for the canChangePerspective flag in the YAML files. This flag should be used to enable or disable the ability to change the perspective of the camera in the training environment.
// 3. Clean up the code and remove any unnecessary comments or debug logs, whilst ensuring that the code is well-documented.

/// <summary>
/// Manages the environment settings and configurations, including the arena, player controls, and UI canvas.
/// </summary>
public class AAI3EnvironmentManager : MonoBehaviour
{
    [Header("Arena Settings")]
    [SerializeField] public GameObject arena;
    [SerializeField] public GameObject uiCanvas;
    [SerializeField] public GameObject playerControls;

    [Header("Configuration File")]
    [SerializeField] public string configFile = "";

    [Header("Resolution Settings")]
    [SerializeField] private const int maximumResolution = 512;
    [SerializeField] private const int minimumResolution = 4;
    [SerializeField] private const int defaultResolution = 84;
    [SerializeField] private const int defaultRaysPerSide = 2;
    [SerializeField] private const int defaultRayMaxDegrees = 60;
    [SerializeField] private const int defaultDecisionPeriod = 3;

    public bool PlayerMode { get; private set; } = true;

    public ArenasConfigurations _arenasConfigurations;
    private TrainingArena _instantiatedArena;
    private ArenasParametersSideChannel _arenasParametersSideChannel;
    public ArenasParametersSideChannel ArenasParametersSideChannel => _arenasParametersSideChannel;
    public static event Action<int, int> OnArenaChanged;

    [Header("Prefabs for Default Configuration")]
    [SerializeField] private ListOfPrefabs prefabs;

    private ArenaBuilder _builder;

    [Header("YAML Files to Load (Ordered)")]
    // Add the YAML file names you want to load from streaming assets at runtime in the inspector. 
    // This script is attached to the EnvironmentManager GameObject in the scene.
    [SerializeField] private List<string> yamlFileNames = new List<string> { };

    private YAMLReader yamlReader;

    public void Awake()
    {
        _arenasConfigurations = new ArenasConfigurations();
        InitialiseSideChannel();

        _builder = new ArenaBuilder(arena, null, 50, 100);

        var environmentParameters = RetrieveEnvironmentParameters(); // Changed from dictionary to var to avoid explicit type
        int paramValue;

        bool playerMode = (environmentParameters.TryGetValue("playerMode", out paramValue) ? paramValue : 1) > 0;
        bool useCamera = (environmentParameters.TryGetValue("useCamera", out paramValue) ? paramValue : 0) > 0;
        int resolution = environmentParameters.TryGetValue("resolution", out paramValue) ? paramValue : defaultResolution;
        bool grayscale = (environmentParameters.TryGetValue("grayscale", out paramValue) ? paramValue : 0) > 0;
        bool useRayCasts = (environmentParameters.TryGetValue("useRayCasts", out paramValue) ? paramValue : 0) > 0;
        int raysPerSide = environmentParameters.TryGetValue("raysPerSide", out paramValue) ? paramValue : defaultRaysPerSide;
        int rayMaxDegrees = environmentParameters.TryGetValue("rayMaxDegrees", out paramValue) ? paramValue : defaultRayMaxDegrees;
        int decisionPeriod = environmentParameters.TryGetValue("decisionPeriod", out paramValue) ? paramValue : defaultDecisionPeriod;

        if (Application.isEditor)
        {
            // If not in editor, load YAML files from StreamingAssets for runtime configuration.
            Debug.Log("Using Unity Editor Default Configuration");
            playerMode = true;
            useCamera = true;
            resolution = 84;
            grayscale = false;
            useRayCasts = true;
            raysPerSide = 2;

            // Editor mode: load using the original Resources.Load logic
            LoadYAMLFileInEditor();
        }

        resolution = Math.Max(minimumResolution, Math.Min(maximumResolution, resolution));

        TrainingArena foundArena = FindObjectOfType<TrainingArena>();

        InstantiateArenas();

        playerControls.SetActive(playerMode);
        uiCanvas.GetComponent<Canvas>().enabled = playerMode;

        foreach (Agent a in FindObjectsOfType<Agent>(true))
        {
            a.GetComponentInChildren<DecisionRequester>().DecisionPeriod = decisionPeriod;
            if (!useRayCasts)
            {
                DestroyImmediate(a.GetComponentInChildren<RayPerceptionSensorComponent3D>());
            }
            else
            {
                ChangeRayCasts(a.GetComponentInChildren<RayPerceptionSensorComponent3D>(), raysPerSide, rayMaxDegrees);
            }
            if (!useCamera)
            {
                DestroyImmediate(a.GetComponentInChildren<CameraSensorComponent>());
            }
            else
            {
                ChangeResolution(a.GetComponentInChildren<CameraSensorComponent>(), resolution, resolution, grayscale);
            }
            if (playerMode)
            {
                a.GetComponentInChildren<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
            }
        }

        PrintDebugInfo(playerMode, useCamera, resolution, grayscale, useRayCasts, raysPerSide, rayMaxDegrees);
        _instantiatedArena._agent.gameObject.SetActive(true);
    }

    void Start()
    {
        // If not in editor, load YAML files from StreamingAssets for runtime configuration.
        if (!Application.isEditor)
        {
            // Load YAML files from StreamingAssets for runtime configuration.
            yamlReader = new YAMLReader();
            StartCoroutine(LoadArenasFromYAMLFiles());
        }
    }

    public void InitialiseSideChannel()
    {
        if (_arenasParametersSideChannel != null)
        {
            try
            {
                SideChannelManager.UnregisterSideChannel(_arenasParametersSideChannel);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to unregister existing side channel: {ex.Message}");
            }
        }

        try
        {
            _arenasParametersSideChannel = new ArenasParametersSideChannel();
            _arenasParametersSideChannel.NewArenasParametersReceived += _arenasConfigurations.UpdateWithConfigurationsReceived;
            SideChannelManager.RegisterSideChannel(_arenasParametersSideChannel);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize or register the side channel: {ex.Message}");
            throw;
        }
    }

    public void InstantiateArenas()
    {
        GameObject arenaInst = Instantiate(arena, new Vector3(0f, 0f, 0f), Quaternion.identity);
        _instantiatedArena = arenaInst.GetComponent<TrainingArena>();
        _instantiatedArena.arenaID = 0;
    }

    /// <summary>
    /// Load arenas from YAML files in the StreamingAssets folder. 
    /// Platform Restrictions: WebGL builds run within a web browser environment, which enforces strict security measures. 
    /// One significant restriction is the inaccessibility of the local file system. Unlike desktop builds, WebGL cannot perform direct file I/O operations using System.IO methods like File.ReadAllText.
    /// This method uses UnityWebRequest to load YAML files in WebGL builds and File.ReadAllText in desktop builds.
    /// </summary>
    private IEnumerator LoadArenasFromYAMLFiles()
    {
        string basePath = Application.streamingAssetsPath + "/Yamls/";
        int nextArenaId = 0;

        // Go through each YAML file in the list and load the arenas in the order they are placed (i.e. element 0 is loaded first, then element 1, and so on).
        // The file names needs to match the ones in the StreamingAssets folder.
        foreach (var fileName in yamlFileNames)
        {
            string filePath = Path.Combine(basePath, fileName);
            string yamlContent = null;
            ///
            /// UnityWebRequest: Since direct file access is unavailable, UnityWebRequest is employed to fetch YAML files over HTTP(S). This method aligns with the web’s client-server model, allowing the application to request resources hosted on a server or embedded within the application’s build (e.g., in the StreamingAssets folder).
            /// Asynchronous Handling: The yield return www.SendWebRequest(); line ensures that the request is sent asynchronously, preventing the application from freezing while waiting for the response. 
            /// Error Handling: If the request fails, an error is logged, and the script proceeds to the next file without halting the entire loading process.
            ///
#if UNITY_WEBGL
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                yamlContent = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"Failed to load {fileName}: {www.error}");
                continue; // move on to next file if failed
            }
#else
            if (File.Exists(filePath))
            {
                yamlContent = File.ReadAllText(filePath);
            }
            else
            {
                Debug.LogError($"YAML file not found at {filePath}");
                continue;
            }
#endif

            if (!string.IsNullOrEmpty(yamlContent))
            {
                ArenaConfig config = yamlReader.deserializer.Deserialize<ArenaConfig>(yamlContent);
                if (config != null && config.arenas != null && config.arenas.Count > 0)
                {
                    // Append arenas to configurations, renumbering IDs from nextArenaId upward.
                    foreach (var kvp in config.arenas)
                    {
                        _arenasConfigurations.configurations[nextArenaId] = new ArenaConfiguration(kvp.Value);
                        nextArenaId++;
                    }

                    // Update global flags if needed (just take last file's flags or combine logically)
                    // TODO: THe below needs to be tested with multiple files with different flags. canChangePerspective is not working and so it should be debugged.
                    _arenasConfigurations.randomizeArenas |= config.randomizeArenas;
                    _arenasConfigurations.showNotification |= config.showNotification;
                    _arenasConfigurations.canResetEpisode &= config.canResetEpisode;
                    _arenasConfigurations.canChangePerspective &= config.canChangePerspective;

                    Debug.Log($"Loaded {config.arenas.Count} arenas from {fileName}, total arenas now: {_arenasConfigurations.configurations.Count}");
                }
                else
                {
                    Debug.LogWarning($"No arenas found or parsed as null in {fileName}");
                }
            }
        }

        if (_arenasConfigurations.configurations.Count == 0)
        {
            Debug.LogError("No arenas loaded from any YAML files!");
        }
        else
        {
            Debug.Log($"All YAML files processed. Total arenas loaded: {_arenasConfigurations.configurations.Count}");
        }
        yield break;
    }

    public void LoadYAMLFileInEditor()
    {
        if (string.IsNullOrWhiteSpace(configFile))
        {
            Debug.LogWarning("Config file path is null or empty.");
            return;
        }

        try
        {
            var configYAML = Resources.Load<TextAsset>(configFile);
            if (configYAML != null)
            {
                var YAMLReader = new YAMLReader();
                var parsed = YAMLReader.deserializer.Deserialize<ArenaConfig>(configYAML.text);
                if (parsed != null && parsed.arenas != null && parsed.arenas.Count > 0)
                {
                    int nextArenaId = 0;
                    foreach (var kvp in parsed.arenas)
                    {
                        _arenasConfigurations.configurations[nextArenaId] = new ArenaConfiguration(kvp.Value);
                        nextArenaId++;
                    }
                    _arenasConfigurations.randomizeArenas = parsed.randomizeArenas;
                    _arenasConfigurations.showNotification = parsed.showNotification;
                    _arenasConfigurations.canResetEpisode = parsed.canResetEpisode;
                    _arenasConfigurations.canChangePerspective = parsed.canChangePerspective;

                    Debug.Log($"Editor mode: Loaded {_arenasConfigurations.configurations.Count} arenas from {configFile}");
                }
                else
                {
                    Debug.LogWarning("Deserialized YAML is empty or null for arenas.");
                }
            }
            else
            {
                Debug.LogWarning($"YAML file '{configFile}' not found in Resources.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading YAML in editor: {ex.Message}");
        }
    }

    public void TriggerArenaChangeEvent(int currentArenaIndex, int totalArenas)
    {
        OnArenaChanged?.Invoke(currentArenaIndex, totalArenas);
    }

    public bool GetRandomizeArenasStatus()
    {
        return _arenasConfigurations.randomizeArenas;
    }

    public int GetCurrentArenaIndex()
    {
        return _arenasConfigurations.CurrentArenaID;
    }

    public int GetTotalArenas()
    {
        return _arenasConfigurations.configurations.Count;
    }

    public ArenasConfigurations GetArenasConfigurations()
    {
        return _arenasConfigurations;
    }

    public void ChangeRayCasts(RayPerceptionSensorComponent3D raySensor, int no_raycasts, int max_degrees)
    {
        raySensor.RaysPerDirection = no_raycasts;
        raySensor.MaxRayDegrees = max_degrees;
    }

    public void ChangeResolution(CameraSensorComponent cameraSensor, int cameraWidth, int cameraHeight, bool grayscale)
    {
        cameraSensor.Width = cameraWidth;
        cameraSensor.Height = cameraHeight;
        cameraSensor.Grayscale = grayscale;
    }

    public Dictionary<string, int> RetrieveEnvironmentParameters(string[] args = null)
    {
        Dictionary<string, int> environmentParameters = new Dictionary<string, int>();
        if (args == null)
        {
            args = System.Environment.GetCommandLineArgs();
        }

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--playerMode":
                    int playerMode = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : 1;
                    environmentParameters.Add("playerMode", playerMode);
                    break;
                case "--receiveConfiguration":
                    environmentParameters.Add("receiveConfiguration", 0);
                    break;
                case "--numberOfArenas":
                    int nArenas = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : 1;
                    environmentParameters.Add("numberOfArenas", nArenas);
                    break;
                case "--useCamera":
                    environmentParameters.Add("useCamera", 1);
                    break;
                case "--resolution":
                    int camW = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : defaultResolution;
                    environmentParameters.Add("resolution", camW);
                    break;
                case "--grayscale":
                    environmentParameters.Add("grayscale", 1);
                    break;
                case "--useRayCasts":
                    environmentParameters.Add("useRayCasts", 1);
                    break;
                case "--raysPerSide":
                    int rps = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : 2;
                    environmentParameters.Add("raysPerSide", rps);
                    break;
                case "--rayMaxDegrees":
                    int rmd = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : 60;
                    environmentParameters.Add("rayMaxDegrees", rmd);
                    break;
                case "--decisionPeriod":
                    int dp = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : 3;
                    environmentParameters.Add("decisionPeriod", dp);
                    break;
            }
        }
        return environmentParameters;
    }

    public ArenaConfiguration GetConfiguration(int arenaID)
    {
        if (!_arenasConfigurations.configurations.ContainsKey(arenaID))
        {
            Debug.LogError($"Arena ID {arenaID} not found in configurations!");
            throw new KeyNotFoundException($"Arena ID {arenaID} not found.");
        }

        return _arenasConfigurations.configurations[arenaID];
    }

    public void AddConfiguration(int arenaID, ArenaConfiguration arenaConfiguration)
    {
        _arenasConfigurations.configurations[arenaID] = arenaConfiguration;
    }

    public void OnDestroy()
    {
        if (Academy.IsInitialized && _arenasParametersSideChannel != null)
        {
            SideChannelManager.UnregisterSideChannel(_arenasParametersSideChannel);
        }
    }

    private void PrintDebugInfo(bool playerMode, bool useCamera, int resolution, bool grayscale, bool useRayCasts, int raysPerSide, int rayMaxDegrees)
    {
        Debug.Log(
            "Environment loaded with options:"
            + "\n  PlayerMode: " + playerMode
            + "\n  useCamera: " + useCamera
            + "\n  Resolution: " + resolution
            + "\n  grayscale: " + grayscale
            + "\n  useRayCasts: " + useRayCasts
            + "\n  raysPerSide: " + raysPerSide
            + "\n  rayMaxDegrees: " + rayMaxDegrees
        );
    }

    public static byte[] ReadFully(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}