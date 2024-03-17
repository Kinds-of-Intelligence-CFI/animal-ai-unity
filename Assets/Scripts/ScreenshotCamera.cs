using System.IO;
using UnityEngine;

/// <summary>
/// Captures screenshots from a camera and saves them to the device's storage.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ScreenshotCamera : MonoBehaviour
{
	public int fileCounter = 0;
	public RenderTexture renderTexture;
	public string filePath = "Screenshots";
	public string fileName = "capture";
	private Camera screenshotCam;
	public bool testMode = false;

	private void Awake()
	{
		screenshotCam = GetComponent<Camera>();
		InitializeRenderTexture();
	}

	private void InitializeRenderTexture()
	{
		if (!testMode && renderTexture != null)
		{
			screenshotCam.targetTexture = new RenderTexture(renderTexture.width, renderTexture.height, renderTexture.depth, renderTexture.format, RenderTextureReadWrite.sRGB);
		}
		else
		{
			Debug.LogWarning("RenderTexture is not assigned or in Test Mode.");
		}
	}

	public void Activate(bool enable = true)
	{
		screenshotCam.enabled = enable;
	}

	private void LateUpdate()
	{
		if (screenshotCam.enabled && !testMode)
		{
			CaptureScreenshot();
			Activate(false); // Deactivate the camera after capturing to prevent multiple captures.
		}
	}

	private void CaptureScreenshot()
	{
		screenshotCam.Render();
		RenderTexture.active = screenshotCam.targetTexture;

		Texture2D image = new Texture2D(screenshotCam.targetTexture.width, screenshotCam.targetTexture.height, TextureFormat.RGB24, false);
		image.ReadPixels(new Rect(0, 0, screenshotCam.targetTexture.width, screenshotCam.targetTexture.height), 0, 0);
		image.Apply();
		byte[] bytes = image.EncodeToPNG();
		Destroy(image); // Free the image from memory after encoding.

		string directoryPath = Path.Combine(Application.persistentDataPath, filePath);
		Directory.CreateDirectory(directoryPath); // Ensure the directory exists.

		string formattedFileName = $"{fileName}{fileCounter}_{System.DateTime.Now:dd-MM_HH-mm-ss}.png";
		string fullPath = Path.Combine(directoryPath, formattedFileName);

		File.WriteAllBytes(fullPath, bytes);
		Debug.Log($"Screenshot saved to {fullPath}");
		fileCounter++;
	}
}
