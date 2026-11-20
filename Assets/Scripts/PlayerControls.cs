using UnityEngine;
using TMPro;
using ArenasParameters;

/// <summary>
/// Manages the player controls and camera perspectives.
/// </summary>
public class PlayerControls : MonoBehaviour
{
    [Header("Camera Settings")]
    /* 0 = bird's eye (the scene's Main Camera, fixed above the arena), 1 = first-person (AgentCamMid),
       2 = third-person (the camBase camera behind the agent) */
    private Camera[] cameras = new Camera[3];
    public Camera[] Cameras => cameras;
    public int cameraID => activeCameraIndex;
    private int activeCameraIndex = 0;
    private bool camerasInitialized = false;
    private bool started = false;

    [Header("UI Components")]
    public TMP_Text scoreText;
    public Canvas effectCanvas;

    [Header("Gameplay Settings")]
    private bool canResetEpisode = true;
    private bool canChangePerspective = true;
    private string initialPerspective = null;

    [Header("External References")]
    private ScreenshotCamera screenshotCam;
    private TrainingArena arena;
    private TrainingAgent agent; // The arena's primary agent: player mode controls and follows agent 0
    private ArenasConfigurations arenasConfigurations;
    private AAI3EnvironmentManager environmentManager;

    [Header("Score Settings")]
    public float prevScore = 0;

    public string GetActiveCameraDescription()
    {
        return activeCameraIndex switch
        {
            0 => "0 (Bird's Eye)",
            1 => "1 (First-Person)",
            2 => "2 (Third-Person)",
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
        started = true;
    }

    /// <summary>
    /// Re-reads the global settings from the arena configuration and resets the camera.
    /// Call this when the configuration arrives after Start, as it does in WebGL.
    /// </summary>
    public void ApplyConfiguration()
    {
        if (!started)
        {
            return; /* Start reads the configuration itself */
        }
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
        screenshotCam = FindAnyObjectByType<ScreenshotCamera>();
        environmentManager = FindAnyObjectByType<AAI3EnvironmentManager>();
        arena = FindAnyObjectByType<TrainingArena>();
        agent = arena != null ? arena.PrimaryAgent : null;

        cameras[0] = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (agent != null)
        {
            cameras[1] = agent.transform.Find("AgentCamMid").GetComponent<Camera>();
            // The bird's-eye camera belongs to the agent's prefab instance, so look it up there rather than by tag
            // (every agent's prefab instance has one)
            foreach (Camera camera in agent.transform.parent.GetComponentsInChildren<Camera>(true))
            {
                if (camera.CompareTag("camBase"))
                {
                    cameras[2] = camera;
                    break;
                }
            }
        }
    }

    private void LoadConfigurationSettings()
    {
        if (environmentManager != null)
        {
            arenasConfigurations = environmentManager.GetArenasConfigurations();
            canResetEpisode = arenasConfigurations?.canResetEpisode ?? true;
            canChangePerspective = arenasConfigurations?.canChangePerspective ?? true;
            initialPerspective = arenasConfigurations?.initialPerspective;
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

        activeCameraIndex = ResolveInitialCameraIndex(initialPerspective, canChangePerspective);
        Debug.Log(
            $"Initializing Cameras. Can Change Perspective: {canChangePerspective}, Initial Perspective: {initialPerspective ?? "unset"}, Active Camera Index: {activeCameraIndex}"
        );

        SetActiveCamera(activeCameraIndex);
        camerasInitialized = true;
    }

    /// <summary>
    /// Returns the index of the camera to start in: the one named by initialPerspective ("first", "third" or
    /// "birds_eye"), or if that's unset or unrecognised, bird's eye when the player can change perspective
    /// and third-person when they can't.
    /// </summary>
    public static int ResolveInitialCameraIndex(string initialPerspective, bool canChangePerspective)
    {
        int defaultIndex = canChangePerspective ? 0 : 2;
        if (initialPerspective == null)
        {
            return defaultIndex;
        }
        switch (initialPerspective.Trim().ToLowerInvariant())
        {
            case "birds_eye":
                return 0;
            case "first":
                return 1;
            case "third":
                return 2;
            default:
                Debug.LogWarning(
                    $"initialPerspective '{initialPerspective}' isn't recognised (use first, third or birds_eye). Using camera {defaultIndex} instead."
                );
                return defaultIndex;
        }
    }

    private void HandleInput()
    {
        if (canChangePerspective && Input.GetKeyDown(KeyCode.C))
        {
            CycleCamera();
        }
        if (canResetEpisode && Input.GetKeyDown(KeyCode.R))
        {
            arena.EndArenaEpisode();
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
