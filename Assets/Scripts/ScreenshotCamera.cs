using System.IO;
using UnityEngine;

/// <summary>
/// Captures screenshots from the main camera and saves them to the device's storage.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ScreenshotCamera : MonoBehaviour
{
    [Header("Screenshot Settings")]
    public int fileCounter = 0;
    public RenderTexture renderTexture;
    public string filePath = "Screenshots";
    public string fileName = "capture";
    private Camera screenshotCam;
    public bool testMode = false;

    private void Awake()
    {
        screenshotCam = GetComponent<Camera>();
        if (screenshotCam == null)
        {
            Debug.LogError("Camera component is missing from the GameObject.");
            return;
        }
        InitializeRenderTexture();
    }

    private void InitializeRenderTexture()
    {
        if (!testMode && renderTexture != null)
        {
            screenshotCam.targetTexture = new RenderTexture(
                renderTexture.width,
                renderTexture.height,
                renderTexture.depth,
                renderTexture.format,
                RenderTextureReadWrite.sRGB
            );
        }
        else
        {
            Debug.LogWarning("RenderTexture is not assigned or in Test Mode.");
        }
    }

    public void Activate(bool enable = true)
    {
        if (screenshotCam == null)
        {
            Debug.LogError("Screenshot Camera is not assigned.");
            return;
        }
        screenshotCam.enabled = enable;
    }

    private void LateUpdate()
    {
        if (screenshotCam.enabled && !testMode)
        {
            CaptureScreenshot();
            Activate(false);
        }
    }

    private void CaptureScreenshot()
    {
        if (screenshotCam == null)
        {
            Debug.LogError("Screenshot Camera is not assigned.");
            return;
        }

        if (screenshotCam.targetTexture == null)
        {
            Debug.LogError("No RenderTexture assigned to the Camera.");
            return;
        }

        screenshotCam.Render();
        RenderTexture.active = screenshotCam.targetTexture;

        Texture2D image = new Texture2D(
            screenshotCam.targetTexture.width,
            screenshotCam.targetTexture.height,
            TextureFormat.RGB24,
            false
        );
        image.ReadPixels(
            new Rect(0, 0, screenshotCam.targetTexture.width, screenshotCam.targetTexture.height),
            0,
            0
        );
        image.Apply();
        byte[] bytes = image.EncodeToPNG();
        Destroy(image);

        string directoryPath = Path.Combine(Application.persistentDataPath, filePath);
        Directory.CreateDirectory(directoryPath);

        string formattedFileName =
            $"{fileName}{fileCounter}_{System.DateTime.Now:dd-MM_HH-mm-ss}.png";
        string fullPath = Path.Combine(directoryPath, formattedFileName);

        File.WriteAllBytes(fullPath, bytes);
        fileCounter++;
    }
}
