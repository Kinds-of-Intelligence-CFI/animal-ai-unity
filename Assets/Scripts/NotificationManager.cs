using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
	public GameObject notificationPanel; // Reference to the panel created
	public Text notificationText; // Reference to the text element inside the panel
	public Image notificationBackgroundImage; // Reference to the image element inside the panel

	public Color successColor = Color.green;
	public Color failureColor = Color.red;

	public Sprite[] successFrames;
	public Sprite[] failureFrames;

	public float frameRate = 0.1f; // seconds per frame for animation
	private int currentFrame = 0;

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
			DontDestroyOnLoad(gameObject); // This ensures the manager persists between scenes
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public void ShowSuccessNotification(string message)
	{
		ShowNotification(message, successColor);
		StartCoroutine(AnimateSprite(successFrames));
	}

	public void ShowFailureNotification(string message)
	{
		ShowNotification(message, failureColor);
		StartCoroutine(AnimateSprite(failureFrames));
	}

	private void ShowNotification(string message, Color color)
	{
		Debug.Log("Notification Panel: " + notificationPanel);
		Debug.Log("Notification Text: " + notificationText);
		Debug.Log("Message: " + message);
		Debug.Log("Color: " + color);

		Debug.Log("Is notificationText null? " + (notificationText == null));
		if (notificationText != null)
		{
			Debug.Log(
				"Is the Text component missing? " + (notificationText.GetComponent<Text>() == null)
			);
		}
		Debug.Log("Trying to show notification with message: " + message);
		notificationText.text = message;
		notificationPanel.GetComponent<Image>().color = color;
		notificationText.color = color;
		notificationBackgroundImage.color = color;
		notificationPanel.SetActive(true);
	}

	public void HideNotification()
	{
		if (notificationPanel == null)
		{
			Debug.Log("Notification panel is null.");
		}
		else
		{
			Debug.Log("Hiding notification panel.");
			notificationPanel.SetActive(false);
			StopAllCoroutines();
		}
	}

	IEnumerator AnimateSprite(Sprite[] animationFrames)
	{
		while (true)
		{
			notificationBackgroundImage.sprite = animationFrames[currentFrame];
			currentFrame = (currentFrame + 1) % animationFrames.Length;
			yield return new WaitForSeconds(frameRate);
		}
	}

	public void PlaySuccessGif()
	{
		StopAllCoroutines();
		StartCoroutine(AnimateSprite(successFrames));
	}

	public void PlayFailureGif()
	{
		StopAllCoroutines();
		StartCoroutine(AnimateSprite(failureFrames));
	}
}