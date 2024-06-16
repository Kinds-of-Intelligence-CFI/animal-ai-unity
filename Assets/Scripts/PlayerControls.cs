using Unity.MLAgents;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArenasParameters;

/// <summary>
/// Manages the player controls and camera perspectives.
/// </summary>
public class PlayerControls : MonoBehaviour
{
	[Header("Camera Settings")]
	private Camera[] cameras = new Camera[3];
	public int cameraID => activeCameraIndex;
	private int activeCameraIndex = 0;
	private bool camerasInitialized = false;

	[Header("UI Components")]
	public TMP_Text scoreText;
	public Canvas effectCanvas;

	[Header("Gameplay Settings")]
	private bool canResetEpisode = true;
	private bool canChangePerspective = true;

	[Header("External References")]
	private ScreenshotCamera screenshotCam;
	private TrainingAgent agent;
	private ArenasConfigurations arenasConfigurations;

	[Header("Score Settings")]
	public float prevScore = 0;

	public Camera getActiveCam()
	{
		return cameras[activeCameraIndex];
	}

	private void Start()
	{
		InitializeCanvas();
		LoadCamerasAndAgent();
		LoadConfigurationSettings();
		InitializeCameras();
	}

	private void Update()
	{
		if (!camerasInitialized)
		{
			Debug.LogError("Cameras not initialized properly.");
			return;
		}

		HandleInput();
		UpdateScoreText();
	}

	private void InitializeCanvas()
	{
		effectCanvas.renderMode = RenderMode.ScreenSpaceCamera;
	}

	private void LoadCamerasAndAgent()
	{
		screenshotCam = FindObjectOfType<ScreenshotCamera>();
		cameras[0] = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		cameras[1] = GameObject.FindGameObjectWithTag("agent").transform.Find("AgentCamMid").GetComponent<Camera>();
		cameras[2] = GameObject.FindGameObjectWithTag("camBase").GetComponent<Camera>();
		agent = GameObject.FindGameObjectWithTag("agent").GetComponent<TrainingAgent>();
	}

	private void LoadConfigurationSettings()
	{
		arenasConfigurations = ArenasConfigurations.Instance;
		canResetEpisode = arenasConfigurations?.canResetEpisode ?? true;
		canChangePerspective = arenasConfigurations?.canChangePerspective ?? true;
	}

	private void InitializeCameras()
	{
		if (screenshotCam == null || agent == null || cameras[0] == null || cameras[1] == null || cameras[2] == null)
		{
			Debug.LogError("One or more essential components (Cameras, Agent, ScreenshotCam) were not found.");
			return;
		}

		activeCameraIndex = canChangePerspective ? 0 : 2; // If can't change perspectives, use the 3rd person camera
		Debug.Log($"Initializing Cameras. Can Change Perspective: {canChangePerspective}, Active Camera Index: {activeCameraIndex}");

		SetActiveCamera(activeCameraIndex);
		camerasInitialized = true;
	}

	private void HandleInput()
	{
		if (canChangePerspective && Input.GetKeyDown(KeyCode.C))
		{
			CycleCamera();
		}
		if (canResetEpisode && Input.GetKeyDown(KeyCode.R))
		{
			agent.EndEpisode();
		}
		if (Input.GetKeyDown(KeyCode.Q))
		{
			Application.Quit();
		}
		if (Input.GetKeyDown(KeyCode.P))
		{
			screenshotCam.Activate();
		}
	}

	private void UpdateScoreText()
	{
		if (agent != null && scoreText != null)
		{
			scoreText.text = $"Previous Reward: {agent.GetPreviousScore():0.000}\n" +
							 $"Current Reward: {agent.GetCumulativeReward():0.000}";
		}
	}

	private void CycleCamera()
	{
		cameras[activeCameraIndex].enabled = false;
		activeCameraIndex = (activeCameraIndex + 1) % cameras.Length;
		SetActiveCamera(activeCameraIndex);
	}

	private void SetActiveCamera(int index)
	{
		for (int i = 0; i < cameras.Length; i++)
		{
			cameras[i].enabled = (i == index);
		}
		effectCanvas.worldCamera = cameras[index];
		effectCanvas.planeDistance = index == 1 ? 0.02f : 0.31f;
	}

}
