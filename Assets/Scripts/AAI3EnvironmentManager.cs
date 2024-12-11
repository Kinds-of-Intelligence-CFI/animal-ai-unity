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

    // Add the YAML file names you want to load from streaming assets at runtime:
    [SerializeField] private List<string> yamlFileNames = new List<string> { };

    private YAMLReader yamlReader;

    public void Awake()
    {
        _arenasConfigurations = new ArenasConfigurations();
        InitialiseSideChannel();

        _builder = new ArenaBuilder(arena, null, 50, 100);

        Dictionary<string, int> environmentParameters = RetrieveEnvironmentParameters();
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
        else
        {
            // Non-editor mode: load YAML from StreamingAssets
            // We'll do this in Start(), since we want to use coroutines if needed.
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
                ChangeRayCasts(
                    a.GetComponentInChildren<RayPerceptionSensorComponent3D>(),
                    raysPerSide,
                    rayMaxDegrees
                );
            }
            if (!useCamera)
            {
                DestroyImmediate(a.GetComponentInChildren<CameraSensorComponent>());
            }
            else
            {
                ChangeResolution(
                    a.GetComponentInChildren<CameraSensorComponent>(),
                    resolution,
                    resolution,
                    grayscale
                );
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
            yamlReader = new YAMLDefs.YAMLReader();
            StartCoroutine(LoadAllYamlFilesFromStreamingAssets());
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

    // Directly integrate YAML from StreamingAssets folder. Seems to be the best approach.
    private IEnumerator LoadAllYamlFilesFromStreamingAssets()
    {
        List<ArenaConfig> loadedConfigs = new List<ArenaConfig>();
        string basePath = Application.streamingAssetsPath + "/Yamls/";

        foreach (var fileName in yamlFileNames)
        {
            string filePath = basePath + fileName;
            string yamlContent = null;

#if UNITY_WEBGL
            // WebGL: use UnityWebRequest to load files from StreamingAssets folder. 
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                yamlContent = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"Failed to load {fileName}: {www.error}");
            }
#else
            // Non-WebGL: read directly from file system, so nothing changes here.
            if (System.IO.File.Exists(filePath))
            {
                yamlContent = System.IO.File.ReadAllText(filePath);
            }
            else
            {
                Debug.LogError("YAML file not found at " + filePath);
            }
#endif

            if (!string.IsNullOrEmpty(yamlContent))
            {
                ArenaConfig config = yamlReader.deserializer.Deserialize<ArenaConfig>(yamlContent);
                if (config != null)
                {
                    loadedConfigs.Add(config);
                    Debug.Log($"Loaded {fileName} successfully from StreamingAssets.");
                }
                else
                {
                    Debug.LogWarning($"Parsed YAML is null for {fileName}");
                }
            }
        }

        ArenaConfig finalConfig = MergeArenaConfigs(loadedConfigs);
        ApplyConfigToEnvironment(finalConfig);

        yield break;
    }

    private ArenaConfig MergeArenaConfigs(List<ArenaConfig> configs)
    {
        if (configs.Count == 0)
            return null;

        var finalConfig = configs[0];
        for (int i = 1; i < configs.Count; i++)
        {
            foreach (var kvp in configs[i].arenas)
            {
                finalConfig.arenas[kvp.Key] = kvp.Value;
            }
        }
        return finalConfig;
    }

    // Apply the final YAML config to the environment using _arenasConfigurations.UpdateWithYAML
    private void ApplyConfigToEnvironment(ArenaConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("No configuration found in YAML files.");
            return;
        }

        // Directly integrate with your arena configuration system
        _arenasConfigurations.UpdateWithYAML(config);

        Debug.Log("YAML configuration applied from StreamingAssets!");
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
                var YAMLReader = new YAMLDefs.YAMLReader();
                var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(configYAML.text);
                if (parsed != null)
                {
                    _arenasConfigurations.UpdateWithYAML(parsed);
                }
                else
                {
                    Debug.LogWarning("Deserialized YAML content is null.");
                }
            }
            else
            {
                Debug.LogWarning($"YAML file '{configFile}' could not be found or loaded via Resources.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while loading or processing the YAML file in editor: {ex.Message}");
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
            Debug.LogWarning($"Arena ID {arenaID} not found. Generating a default configuration dynamically.");

            ArenaConfiguration defaultConfig = new ArenaConfiguration
            {
                TimeLimit = 250,
                passMark = 50,
                randomSeed = UnityEngine.Random.Range(1, 1000),
                mergeNextArena = false
            };

            if (prefabs != null && prefabs.allPrefabs != null && prefabs.allPrefabs.Count > 0)
            {
                HashSet<Vector3> usedPositions = new HashSet<Vector3>();

                foreach (GameObject prefab in prefabs.allPrefabs)
                {
                    if (prefab != null)
                    {
                        Vector3 position;
                        int maxAttempts = 100;
                        int attempt = 0;

                        do
                        {
                            position = new Vector3(
                                UnityEngine.Random.Range(0f, _builder.ArenaWidth),
                                0,
                                UnityEngine.Random.Range(0f, _builder.ArenaDepth)
                            );
                            attempt++;
                        } while (usedPositions.Contains(position) && attempt < maxAttempts);

                        if (attempt >= maxAttempts)
                        {
                            Debug.LogWarning("Max attempts reached for unique position generation.");
                            continue;
                        }

                        usedPositions.Add(position);

                        Spawnable spawnable = new Spawnable(prefab)
                        {
                            positions = new List<Vector3> { position },
                            rotations = new List<float> { UnityEngine.Random.Range(0f, 360f) },
                            sizes = new List<Vector3> { new Vector3(1f, 1f, 1f) }
                        };

                        defaultConfig.spawnables.Add(spawnable);
                    }
                }
            }
            else
            {
                Debug.LogWarning("No prefabs available in ListOfPrefabs to generate default arena items.");
            }

            _arenasConfigurations.configurations[arenaID] = defaultConfig;
            return defaultConfig;
        }

        return _arenasConfigurations.configurations[arenaID];
    }

    public void AddConfiguration(int arenaID, ArenaConfiguration arenaConfiguration)
    {
        _arenasConfigurations.configurations.Add(arenaID, arenaConfiguration);
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