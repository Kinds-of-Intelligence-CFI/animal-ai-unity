using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Reflection;

/// <summary>
/// Test class for the PlayerControls class in play mode. Edit mode tests are also implemented.
/// </summary>
public class PlayerControlsTests
{
    private PlayerControls playerControls;
    private TrainingAgent agent;
    private Camera[] cameras = new Camera[3];

    [SetUp]
    public void Setup()
    {
        /* Load the scene so that the PlayerControls class is initialized. */
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator TestCamerasInitialized()
    {
        yield return null;

        playerControls = GameObject.FindAnyObjectByType<PlayerControls>();
        agent = GameObject.FindAnyObjectByType<TrainingAgent>();
        cameras[0] = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        cameras[1] = GameObject
            .FindGameObjectWithTag("agent")
            .transform.Find("AgentCamMid")
            .GetComponent<Camera>();
        cameras[2] = GameObject.FindGameObjectWithTag("camBase").GetComponent<Camera>();

        /* Assert that the cameras array is initialized and all cameras are assigned */
        Assert.IsNotNull(playerControls.Cameras);
        Assert.AreEqual(3, playerControls.Cameras.Length);
        Assert.IsNotNull(cameras[0]);
        Assert.IsNotNull(cameras[1]);
        Assert.IsNotNull(cameras[2]);
    }

    [UnityTest]
    public IEnumerator TestCycleCamera()
    {
        yield return null;

        playerControls = GameObject.FindAnyObjectByType<PlayerControls>();

        int initialCameraID = playerControls.cameraID;

        playerControls.CycleCamera();

        Assert.AreNotEqual(initialCameraID, playerControls.cameraID);

        /* Cycle twice to ensure the camera is back to the original */
        playerControls.CycleCamera();
        playerControls.CycleCamera();

        Assert.AreEqual(initialCameraID, playerControls.cameraID);
    }
    [UnityTest]
    public IEnumerator TestGetActiveCameraDescription()
    {
        yield return null;
        playerControls = GameObject.FindAnyObjectByType<PlayerControls>();

        for (int i = 0; i < playerControls.Cameras.Length; i++)
        {
            string expectedDescription = i switch
            {
                0 => "0 (First-Person)",
                1 => "1 (Third-Person)",
                2 => "2 (Bird's Eye)",
                _ => $"{i} (unknown)"
            };
            Assert.AreEqual(expectedDescription, playerControls.GetActiveCameraDescription());
            playerControls.CycleCamera();
        }
    }
}