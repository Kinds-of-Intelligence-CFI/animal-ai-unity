using UnityEngine;
using TMPro;
using ArenasParameters;

/// <summary>
/// Manages the player controls and camera perspectives.
/// </summary>
public class PlayerControls : MonoBehaviour
{
    [Header("Camera Settings")]
    private Camera[] cameras = new Camera[3];
    public Camera[] Cameras => cameras;
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
    private AAI3EnvironmentManager environmentManager;

    [Header("Score Settings")]
    public float prevScore = 0;

    public string GetActiveCameraDescription()
    {
        return activeCameraIndex switch
        {
            0 => "0 (First-Person)",
            1 => "1 (Third-Person)",
            2 => "2 (Bird's Eye)",
            _ => $"{activeCameraIndex} (unknown)"
        };
    }

    public Camera getActiveCam()
    {
        return cameras[activeCameraIndex];
    }

    public int GetActiveCameraIndex()
    {
        return activeCameraIndex;
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
        cameras[1] = GameObject
            .FindGameObjectWithTag("agent")
            .transform.Find("AgentCamMid")
            .GetComponent<Camera>();
        cameras[2] = GameObject.FindGameObjectWithTag("camBase").GetComponent<Camera>();
        agent = GameObject.FindGameObjectWithTag("agent").GetComponent<TrainingAgent>();
        environmentManager = FindObjectOfType<AAI3EnvironmentManager>();
    }

    private void LoadConfigurationSettings()
    {
        if (environmentManager != null)
        {
            arenasConfigurations = environmentManager.GetArenasConfigurations();
            canResetEpisode = arenasConfigurations?.canResetEpisode ?? true;
            canChangePerspective = arenasConfigurations?.canChangePerspective ?? true;
        }
        else
        {
            Debug.LogError("Environment Manager not found. Using default settings.");
        }
    }

    private void InitializeCameras()
    {
        if (
            screenshotCam == null
            || agent == null
            || cameras[0] == null
            || cameras[1] == null
            || cameras[2] == null
        )
        {
            Debug.LogError(
                "One or more essential components (Cameras, Agent, ScreenshotCam) were not found."
            );
            return;
        }

        activeCameraIndex = canChangePerspective ? 0 : 2; /* If can't change perspectives, use the bird's-eye camera */
        Debug.Log(
            $"Initializing Cameras. Can Change Perspective: {canChangePerspective}, Active Camera Index: {activeCameraIndex}"
        );

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
            scoreText.text =
                $"Previous Reward: {agent.GetPreviousScore():0.000}\n"
                + $"Current Reward: {agent.GetCumulativeReward():0.000}";
        }
    }
    public void UpdateScoreTextWrapper()
    {
        UpdateScoreText();
    }

    public void CycleCamera()
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
