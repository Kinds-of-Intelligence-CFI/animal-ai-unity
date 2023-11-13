using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
	public GameObject notificationPanel; // Reference to the panel
	public Image notificationBackgroundImage; // Reference to the image element inside the panel
	public Image gradientOverlay; // Reference to the gradient overlay image

	public Sprite[] successFrames; // Frames for success animation
	public Sprite[] failureFrames; // Frames for failure animation
	public float frameRate = 0.1f; // Frame rate for the animation

	public Color successGradientColor = Color.green;
	public Color failureGradientColor = Color.red;

	private int currentFrame = 0; // Current frame index for the animation
	private Coroutine gifCoroutine; // Coroutine for handling the GIF animation

	public static NotificationManager Instance;

	void Start()
	{
		successFrames = Resources.LoadAll<Sprite>("happyGIF");
		failureFrames = Resources.LoadAll<Sprite>("sadGIF");
		HideNotification();
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject); // Ensure this object persists between scene loads
		}
		else
		{
			Destroy(gameObject); // Destroy any duplicates
		}
	}

	public void ShowSuccessNotification()
	{
		ShowNotification(true);
	}

	public void ShowFailureNotification()
	{
		ShowNotification(false);
	}

	private void ShowNotification(bool isSuccess)
	{
		Sprite[] framesToShow = isSuccess ? successFrames : failureFrames;
		Color gradientColor = isSuccess ? successGradientColor : failureGradientColor;

		gradientOverlay.color = gradientColor;

		notificationPanel.SetActive(true);
		gradientOverlay.gameObject.SetActive(true);

		// If there is an existing GIF animation coroutine, stop it
		if (gifCoroutine != null)
		{
			StopCoroutine(gifCoroutine);
		}
		// Start a new coroutine to animate the appropriate set of frames
		gifCoroutine = StartCoroutine(AnimateSprite(framesToShow));
	}

	public void HideNotification()
	{
		notificationPanel.SetActive(false);
		gradientOverlay.gameObject.SetActive(false);

		if (gifCoroutine != null)
		{
			StopCoroutine(gifCoroutine);
			gifCoroutine = null;
		}
		currentFrame = 0;
	}

	IEnumerator AnimateSprite(Sprite[] animationFrames)
	{
		while (true)
		{
			// Set the current sprite frame
			notificationBackgroundImage.sprite = animationFrames[currentFrame];
			// Increment the frame index, wrapping back to 0 if it exceeds the length of the array
			currentFrame = (currentFrame + 1) % animationFrames.Length;
			// Wait for the frame rate duration before the next frame
			yield return new WaitForSeconds(frameRate);
		}
	}
}
