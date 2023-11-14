using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
	public static NotificationManager Instance;
	public TrainingAgent trainingAgent;

	public GameObject notificationPanel; // Reference to the panel
	public Image notificationBackgroundImage; // Reference to the image element inside the panel

	public Sprite[] successFrames; // Frames for success animation
	public Sprite[] failureFrames; // Frames for failure animation
	public float frameRate = 0.1f; // Frame rate for the animation

	private int currentFrame = 0; // Current frame index for the animation
	private Coroutine gifCoroutine; // Coroutine for handling the GIF animation

	public Image successGradientBorderImage;
	public Image failureGradientBorderImage;

	void Start()
	{
		successFrames = Resources.LoadAll<Sprite>("happyGIF");
		failureFrames = Resources.LoadAll<Sprite>("sadGIF");
		trainingAgent = GameObject.FindObjectOfType<TrainingAgent>();
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
		trainingAgent.FreezeAgent(true);
	}

	public void ShowFailureNotification()
	{
		ShowNotification(false);
		trainingAgent.FreezeAgent(true);
	}

	private void ShowNotification(bool isSuccess)
	{
		Sprite[] framesToShow = isSuccess ? successFrames : failureFrames;
		Image gradientBorderToShow = isSuccess ? successGradientBorderImage : failureGradientBorderImage;

		notificationPanel.SetActive(true);
		successGradientBorderImage.gameObject.SetActive(isSuccess);
		failureGradientBorderImage.gameObject.SetActive(!isSuccess);

		if (gifCoroutine != null)
		{
			StopCoroutine(gifCoroutine);
		}
		gifCoroutine = StartCoroutine(AnimateSprite(framesToShow));
	}


	public void HideNotification()
	{
		notificationPanel.SetActive(false);
		successGradientBorderImage.gameObject.SetActive(false);
		failureGradientBorderImage.gameObject.SetActive(false);

		if (gifCoroutine != null)
		{
			StopCoroutine(gifCoroutine);
			gifCoroutine = null;
		}
		currentFrame = 0;
		
		trainingAgent.FreezeAgent(false);
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
