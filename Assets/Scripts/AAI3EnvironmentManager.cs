using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.SideChannels;
using Unity.MLAgents.Policies;
using ArenasParameters;
using UnityEngineExtensions;
using YamlDotNet.Serialization;

/// Training scene must start automatically on launch by training process
/// Academy must reset the scene to a valid starting point for each episode
/// Training episode must have a definite end (MaxSteps or Agent.EndEpisode)
///
/// Scene requires:
///     GameObject with tag "agent" and component CameraSensorComponent
///     GameObject with tag MainCamera which views the full scene in Unity
///
public class AAI3EnvironmentManager : MonoBehaviour
{
    public GameObject arena; // A prefab for the training arena setup
    public GameObject uiCanvas;
    public string configFile = "";
    public int maximumResolution = 512;
    public int minimumResolution = 4;
    public int defaultResolution = 84;
    public int defaultRaysPerSide = 2;
    public int defaultRayMaxDegrees = 60;
    public int defaultDecisionPeriod = 3;
    public GameObject playerControls; //Just for camera and reset controls.

    [HideInInspector]
    public bool playerMode = true;

    private ArenasConfigurations _arenasConfigurations;
    private TrainingArena _instantiatedArena;
    private ArenasParametersSideChannel _arenasParametersSideChannel;


    public void Awake()
    {
        //This is used to initialise the ArenaParametersSideChannel wich is a subclass of MLAgents SideChannel
        _arenasParametersSideChannel = new ArenasParametersSideChannel();
        _arenasConfigurations = new ArenasConfigurations();

        _arenasParametersSideChannel.NewArenasParametersReceived += _arenasConfigurations.UpdateWithConfigurationsReceived;

        SideChannelManager.RegisterSideChannel(_arenasParametersSideChannel);

        //Get all commandline arguments and update starting parameters
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
        Debug.Log("Set playermode to " + playerMode);

        if (Application.isEditor)//Default settings for tests in Editor
        {
            Debug.Log("Using UnityEditor default configuration");
            playerMode = true;
            useCamera = true;
            resolution = 84;
            grayscale = false;
            useRayCasts = true;
            raysPerSide = 2;
            //If in editor mode load whichever config is specified in configFile field in editor
            if (configFile != "")
            {
                var configYAML = Resources.Load<TextAsset>(configFile);
                if (configYAML != null)
                {//If config file
                    var YAMLReader = new YAMLDefs.YAMLReader();
                    var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(configYAML.ToString());
                    _arenasConfigurations.UpdateWithYAML(parsed);
                }
                else
                {//If directory, then load all config files in the directory.
                    var configYAMLS = Resources.LoadAll<TextAsset>(configFile);
                    var YAMLReader = new YAMLDefs.YAMLReader();
                    foreach (TextAsset config in configYAMLS)
                    {
                        var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(config.ToString());
                        _arenasConfigurations.AddAdditionalArenas(parsed);
                    }
                }
            }
        }

        resolution = Math.Max(minimumResolution, Math.Min(maximumResolution, resolution));

        _instantiatedArena = new TrainingArena();
        InstantiateArenas();//Instantiate every new arena with agent and objects. Agents are currently deactivated until we set the sensors.

        //Add playerControls if in play mode
        playerControls.SetActive(playerMode);
        uiCanvas.GetComponent<Canvas>().enabled = playerMode;

        //Destroy the sensors that aren't being used and update the values of those that are
        // mlagents automatically registers cameras when the agent script is initialised so have to:
        //  1) use FindObjectsOfType as this returns deactivated objects
        //  2) start with agent deactivated and then set active after editing sensors
        //  3) use DestroyImmediate so that it is destroyed before agent is initialised
        // also sets agent decision period and the correct behaviour type for play mode
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
                //The following does nothing under normal execution - but when loading the built version
                //with the play script it sets the BehaviorType back to Heursitic 
                //from default as loading this autotamically attaches Academy for training (since mlagents 0.16.0)
                //@TODO must be a better way to do this.
                a.GetComponentInChildren<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
            }
        }

        //Enable the agent now that their sensors have been set.
        _instantiatedArena._agent.gameObject.SetActive(true);

        Debug.Log("Environment loaded with options:" +
            "\n  PlayerMode: " + playerMode +
            "\n  useCamera: " + useCamera +
            "\n  Resolution: " + resolution +
            "\n  grayscale: " + grayscale +
            "\n  useRayCasts: " + useRayCasts +
            "\n  raysPerSide: " + raysPerSide +
            "\n  rayMaxDegrees: " + rayMaxDegrees
            );
    }

    public int getMaxArenaID()
    {
        return _arenasConfigurations.configurations.Count;
    }

    private void ChangeRayCasts(RayPerceptionSensorComponent3D raySensor, int no_raycasts, int max_degrees)
    {
        raySensor.RaysPerDirection = no_raycasts;
        raySensor.MaxRayDegrees = max_degrees;
    }

    private void ChangeResolution(CameraSensorComponent cameraSensor, int cameraWidth, int cameraHeight, bool grayscale)
    {
        cameraSensor.Width = cameraWidth;
        cameraSensor.Height = cameraHeight;
        cameraSensor.Grayscale = grayscale;
    }

    ///<summary>
    /// We organize the arenas in a grid and position the main camera at the center, high enough
    /// to see all arenas at once.
    ///</summary>
    private void InstantiateArenas()
    {
        GameObject arenaInst = Instantiate(arena, new Vector3(0f, 0f, 0f), Quaternion.identity);
        _instantiatedArena = arenaInst.GetComponent<TrainingArena>();
        _instantiatedArena.arenaID = 0;
    }

    ///<summary>
    ///Parses command line arguments for:
    ///--playerMode: if true then can change camera angles and have control of agent
    ///--receiveConfiguration - adds the configuration file to load
    ///--numberOfArenas - the number of Arenas to spawn (always set to 1 in playerMode)
    ///--useCamera - if true adds camera obseravations
    ///--resolution - the resolution for camera observations (default 84, min4, max 512)
    ///--grayscale 
    ///--useRayCasts - if true adds raycast observations
    ///--raysPerSide - sets the number of rays per side (total = 2n+1)
    ///--rayAngle - sets the maximum angle of the rays (defaults to 60)
    ///--decisionPeriod - The frequency with which the agent requests a decision. 
    ///     A DecisionPeriod of 5 means that the Agent will request a decision every 5 Academy steps.
    ///     Range: (1, 20)
    ///     Default: 3
    /// ///</summary>
    private Dictionary<string, int> RetrieveEnvironmentParameters()
    {
        Dictionary<string, int> environmentParameters = new Dictionary<string, int>();

        string[] args = System.Environment.GetCommandLineArgs();
        Debug.Log("Command Line Args: " + String.Join(" ", args));
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
        Debug.Log("Args parsed by Unity: " + string.Join(" ", environmentParameters));
        return environmentParameters;
    }

    public bool GetConfiguration(int arenaID, out ArenaConfiguration arenaConfiguration)
    {
        return _arenasConfigurations.configurations.TryGetValue(arenaID, out arenaConfiguration);
    }

    public void AddConfiguration(int arenaID, ArenaConfiguration arenaConfiguration)
    {
        _arenasConfigurations.configurations.Add(arenaID, arenaConfiguration);
    }


    public void OnDestroy()
    {
        if (Academy.IsInitialized)
        {
            SideChannelManager.UnregisterSideChannel(_arenasParametersSideChannel);
        }
    }

    public static byte[] ReadFully(Stream stream)
    {
        byte[] buffer = new byte[32768];
        using (MemoryStream ms = new MemoryStream())
        {
            while (true)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return ms.ToArray();
                ms.Write(buffer, 0, read);
            }
        }
    }

}
