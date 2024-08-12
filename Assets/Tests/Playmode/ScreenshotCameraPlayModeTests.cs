using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for the ScreenshotCamera feature in Play Mode.
/// </summary>
public class ScreenshotCameraPlayModeTests
{
    private GameObject cameraObject;
    private ScreenshotCamera screenshotCamera;
    private RenderTexture renderTexture;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("TestCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        screenshotCamera = cameraObject.AddComponent<ScreenshotCamera>();

        renderTexture = new RenderTexture(256, 256, 24);
        screenshotCamera.renderTexture = renderTexture;
        camera.targetTexture = renderTexture;

        screenshotCamera.filePath = "TestScreenshots";
        screenshotCamera.fileName = "test_capture";
        screenshotCamera.testMode = false; /* Ensure test mode is off for actual rendering */

        screenshotCamera.Activate(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (cameraObject != null)
        {
            Object.DestroyImmediate(cameraObject);
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }

        string directoryPath = Path.Combine(
            Application.persistentDataPath,
            screenshotCamera.filePath
        );
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }

    [UnityTest]
    public IEnumerator ScreenshotCamera_CaptureScreenshot_CreatesFile()
    {
        screenshotCamera.Activate(true);
        yield return new WaitForEndOfFrame();

        screenshotCamera.Activate(false);

        string directoryPath = Path.Combine(
            Application.persistentDataPath,
            screenshotCamera.filePath
        );
        Assert.IsTrue(Directory.Exists(directoryPath), "Screenshot directory was not created.");

        string[] files = Directory.GetFiles(directoryPath);
        Assert.IsTrue(files.Length > 0, "No screenshot files were created.");

        /* Just checking here that the file is a PNG image. Not a requirement but still... */
        string filePath = files[0];
        Assert.IsTrue(filePath.EndsWith(".png"), "Captured file is not a PNG image.");
    }
}
