using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ArenasParameters;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.SideChannels;
using Unity.MLAgents.Policies;

// TODO: optimize this code

/// <summary>
/// Manages the environment settings and configurations for the AAI project. 
/// </summary>
public class AAI3EnvironmentManager : MonoBehaviour
{
	[Header("Arena Settings")]
	[SerializeField] private GameObject arena;
	[SerializeField] private GameObject uiCanvas;
	[SerializeField] private GameObject playerControls;

	[Header("Configuration File")]
	[SerializeField] private string configFile = "";

	[Header("Resolution Settings")]
	[SerializeField] private const int maximumResolution = 512;
	[SerializeField] private const int minimumResolution = 4;
	[SerializeField] private const int defaultResolution = 84;
	[SerializeField] private const int defaultRaysPerSide = 2;
	[SerializeField] private const int defaultRayMaxDegrees = 60;
	[SerializeField] private const int defaultDecisionPeriod = 3;

	public bool PlayerMode { get; private set; } = true;

	private ArenasConfigurations _arenasConfigurations;
	private TrainingArena _instantiatedArena;
	private ArenasParametersSideChannel _arenasParametersSideChannel;
	public static event Action<int, int> OnArenaChanged;

	private void InitialiseSideChannel()
	{
		_arenasConfigurations = new ArenasConfigurations();
		_arenasParametersSideChannel = new ArenasParametersSideChannel();
		_arenasParametersSideChannel.NewArenasParametersReceived += _arenasConfigurations.UpdateWithConfigurationsReceived;
		SideChannelManager.RegisterSideChannel(_arenasParametersSideChannel);
	}

	public void Awake()
	{
		InitialiseSideChannel();

		// Get all commandline arguments and update starting parameters
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
		Debug.Log("Set playermode to " + playerMode);

		if (Application.isEditor) // Default settings for tests in Editor
		{
			Debug.Log("Using UnityEditor default configuration");
			playerMode = true;
			useCamera = true;
			resolution = 84;
			grayscale = false;
			useRayCasts = true;
			raysPerSide = 2;

			// If in editor mode, load the configuration file specified in the inspector
			if (configFile != "")
			{
				var configYAML = Resources.Load<TextAsset>(configFile);
				if (configYAML != null)
				{
					var YAMLReader = new YAMLDefs.YAMLReader();
					var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(
						configYAML.ToString()
					);
					_arenasConfigurations.UpdateWithYAML(parsed);
				}
				else
				{
					var configYAMLS = Resources.LoadAll<TextAsset>(configFile);
					var YAMLReader = new YAMLDefs.YAMLReader();
					foreach (TextAsset config in configYAMLS)
					{
						var parsed = YAMLReader.deserializer.Deserialize<YAMLDefs.ArenaConfig>(
							config.ToString()
						);
						_arenasConfigurations.AddAdditionalArenas(parsed);
					}
				}
			}
		}

		resolution = Math.Max(minimumResolution, Math.Min(maximumResolution, resolution));
		TrainingArena arena = FindObjectOfType<TrainingArena>();

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
				// The following does nothing under normal execution - but when loading the built version
				// with the play script it sets the BehaviorType back to Heursitic
				// from default as loading this autotamically attaches Academy for training (since mlagents 0.16.0)
				a.GetComponentInChildren<BehaviorParameters>().BehaviorType =
					BehaviorType.HeuristicOnly;
			}
		}

		_instantiatedArena._agent.gameObject.SetActive(true);

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

	public void TriggerArenaChangeEvent(int currentArenaIndex, int totalArenas)
	{
		OnArenaChanged?.Invoke(currentArenaIndex, totalArenas);
	}

	public int getMaxArenaID()
	{
		return _arenasConfigurations.configurations.Count;
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

	private void ChangeRayCasts(
		RayPerceptionSensorComponent3D raySensor,
		int no_raycasts,
		int max_degrees
	)
	{
		raySensor.RaysPerDirection = no_raycasts;
		raySensor.MaxRayDegrees = max_degrees;
	}

	private void ChangeResolution(
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

	private void InstantiateArenas()
	{
		GameObject arenaInst = Instantiate(arena, new Vector3(0f, 0f, 0f), Quaternion.identity);
		_instantiatedArena = arenaInst.GetComponent<TrainingArena>();
		_instantiatedArena.arenaID = 0;
	}

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
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		return ms.ToArray();
	}
}
