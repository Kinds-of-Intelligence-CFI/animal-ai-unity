using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ArenasParameters;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.SideChannels;
using Unity.MLAgents.Policies;

/// <summary>
/// Manages the environment settings and configurations, including the arena, player controls, and UI canvas.
/// </summary>
public class AAI3EnvironmentManager : MonoBehaviour
{
    [Header("Arena Settings")]
    [SerializeField]
    public GameObject arena;

    [SerializeField]
    public GameObject uiCanvas;

    [SerializeField]
    public GameObject playerControls;

    [Header("Configuration File")]
    [SerializeField]
    public string configFile = "";

    [Header("Resolution Settings")]
    [SerializeField]
    private const int maximumResolution = 512;

    [SerializeField]
    private const int minimumResolution = 4;

    [SerializeField]
    private const int defaultResolution = 84;

    [SerializeField]
    private const int defaultRaysPerSide = 2;

    [SerializeField]
    private const int defaultRayMaxDegrees = 60;

    [SerializeField]
    private const int defaultDecisionPeriod = 3;

    public bool PlayerMode { get; private set; } = true;

    public ArenasConfigurations _arenasConfigurations;
    private TrainingArena _instantiatedArena;
    private ArenasParametersSideChannel _arenasParametersSideChannel;
    public ArenasParametersSideChannel ArenasParametersSideChannel => _arenasParametersSideChannel;

    public static event Action<int, int> OnArenaChanged;

    public void Awake()
    {
        _arenasConfigurations = new ArenasConfigurations();
        InitialiseSideChannel();

        Dictionary<string, int> environmentParameters = RetrieveEnvironmentParameters();
        int paramValue;
        bool playerMode =
            (environmentParameters.TryGetValue("playerMode", out paramValue) ? paramValue : 1) > 0;
        bool useCamera =
            (environmentParameters.TryGetValue("useCamera", out paramValue) ? paramValue : 0) > 0;
        int resolution = environmentParameters.TryGetValue("resolution", out paramValue)
            ? paramValue
            : defaultResolution;
        bool grayscale =
            (environmentParameters.TryGetValue("grayscale", out paramValue) ? paramValue : 0) > 0;
        bool useRayCasts =
            (environmentParameters.TryGetValue("useRayCasts", out paramValue) ? paramValue : 0) > 0;
        int raysPerSide = environmentParameters.TryGetValue("raysPerSide", out paramValue)
            ? paramValue
            : defaultRaysPerSide;
        int rayMaxDegrees = environmentParameters.TryGetValue("rayMaxDegrees", out paramValue)
            ? paramValue
            : defaultRayMaxDegrees;
        int decisionPeriod = environmentParameters.TryGetValue("decisionPeriod", out paramValue)
            ? paramValue
            : defaultDecisionPeriod;

        if (Application.isEditor)
        {
            Debug.Log("Using Unity Editor Default Configuration");
            playerMode = true;
            useCamera = true;
            resolution = 84;
            grayscale = false;
            useRayCasts = true;
            raysPerSide = 2;

            LoadYAMLFileInEditor();
        }
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            string currentUrl = Application.absoluteURL;
            Debug.Log("Current WebGL URL: " + currentUrl);
            string[] urlParts = currentUrl.Split('?');
            if (urlParts.Length != 2)
            {
                // TODO: Gracefully handle when the URL is incorrectly constructed
                Debug.LogError($"Unable to find config in URL: {currentUrl}");
            }
            string queryString = urlParts[urlParts.Length - 1];
            string[] queryArgs = queryString.Split('&');
            string experiment_id = queryArgs[0];
            Debug.Log(experiment_id);
            playerMode = true;
            useCamera = true;
            resolution = 84;
            grayscale = false;
            useRayCasts = true;
            raysPerSide = 2;

            // LoadYAMLFileInEditor();
            StartCoroutine(LoadConfigFromS3(experiment_id));
        }

        resolution = Math.Max(minimumResolution, Math.Min(maximumResolution, resolution));
        TrainingArena arena = FindAnyObjectByType<TrainingArena>();

        InstantiateArenas();

        playerControls.SetActive(playerMode);
        uiCanvas.GetComponent<Canvas>().enabled = playerMode;

        foreach (Agent a in FindObjectsByType<Agent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
                a.GetComponentInChildren<BehaviorParameters>().BehaviorType =
                    BehaviorType.HeuristicOnly;
            }
        }
        PrintDebugInfo(
            playerMode,
            useCamera,
            resolution,
            grayscale,
            useRayCasts,
            raysPerSide,
            rayMaxDegrees
        );
        _instantiatedArena._agent.gameObject.SetActive(true);
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
            _arenasParametersSideChannel.NewArenasParametersReceived +=
                _arenasConfigurations.UpdateWithConfigurationsReceived;
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

    public IEnumerator LoadConfigFromS3(string experiment_id)
    {
        string s3Url = $"https://test-experiment-data-storage.s3.eu-north-1.amazonaws.com/{experiment_id}/config.yaml";
        Debug.Log($"Loading config from S3: {s3Url}.");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(s3Url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load config from S3: {webRequest.error}");
                Debug.LogError($"Failed to load config from S3: {webRequest}");

                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit(1);
                #endif

                yield break;
            }

            try
            {
                string configText = webRequest.downloadHandler.text;
                var YAMLReader = new YAMLDefs.YAMLReader();
                var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(configText);

                if (parsed != null)
                {
                    _arenasConfigurations.UpdateWithYAML(parsed);
                    Debug.Log("Successfully loaded and parsed config from S3");
                }
                else
                {
                    Debug.LogWarning("Deserialized YAML content is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Fatal error while processing the YAML file from S3: {ex.Message}");

                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit(1);
                #endif

                throw;
            }
        }
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
                var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(
                    configYAML.text
                );
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
                Debug.LogWarning($"YAML file '{configFile}' could not be found or loaded.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Fatal error while loading or processing the YAML file: {ex.Message}");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit(1);
            #endif

            throw;
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

    public void ChangeRayCasts(
        RayPerceptionSensorComponent3D raySensor,
        int no_raycasts,
        int max_degrees
    )
    {
        raySensor.RaysPerDirection = no_raycasts;
        raySensor.MaxRayDegrees = max_degrees;
    }

    public void ChangeResolution(
        CameraSensorComponent cameraSensor,
        int cameraWidth,
        int cameraHeight,
        bool grayscale
    )
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
        ArenaConfiguration returnConfiguration;
        if (!_arenasConfigurations.configurations.TryGetValue(arenaID, out returnConfiguration))
        {
            Debug.LogWarning($"Arena ID not found. Configurations: {_arenasConfigurations.configurations}");
            throw new KeyNotFoundException($"Tried to load arena {arenaID} but it did not exist");
        }
        return returnConfiguration;
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

    private void PrintDebugInfo(
        bool playerMode,
        bool useCamera,
        int resolution,
        bool grayscale,
        bool useRayCasts,
        int raysPerSide,
        int rayMaxDegrees
    )
    {
        Debug.Log(
            "Environment loaded with options:"
                + "\n  PlayerMode: "
                + playerMode
                + "\n  useCamera: "
                + useCamera
                + "\n  Resolution: "
                + resolution
                + "\n  grayscale: "
                + grayscale
                + "\n  useRayCasts: "
                + useRayCasts
                + "\n  raysPerSide: "
                + raysPerSide
                + "\n  rayMaxDegrees: "
                + rayMaxDegrees
        );
    }

    public static byte[] ReadFully(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
