using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class NotificationManager : MonoBehaviour
{
	public GameObject notificationPanel; // Reference to the panel
	public Image notificationBackgroundImage; // Reference to the image for gradient background

	private AudioSource audioSource; // Reference to the AudioSource component
	public AudioClip successSound; // Sound to play on success
	public AudioClip failureSound; // Sound to play on failure

	public Sprite[] successFrames;
	public Sprite[] failureFrames;

	public static NotificationManager Instance; // Singleton instance

	void Start()
	{
		successFrames = Resources.LoadAll<Sprite>("happyGIF");
		failureFrames = Resources.LoadAll<Sprite>("sadGIF");
	}

	private void Awake()
	{
		// Singleton pattern to ensure only one instance exists
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}

		audioSource = GetComponent<AudioSource>();
		// Ensure there is an AudioSource component
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		// Initially hide the notification
		notificationPanel.SetActive(false);
	}

	public void ShowSuccessNotification()
	{
		ShowNotification(successSound, successFrames);
	}

	public void ShowFailureNotification()
	{
		ShowNotification(failureSound, failureFrames);
	}

	private void ShowNotification(AudioClip sound, Sprite[] frames)
	{
		notificationPanel.SetActive(true);
		StartCoroutine(AnimateGIF(frames, 0.025f));
		if (sound != null)
		{
			audioSource.PlayOneShot(sound);
		}
	}


	public void HideNotification()
	{
		StartCoroutine(FadeOutBackground(1f));
	}

	IEnumerator FadeInBackground(Color color, float duration)
	{
		float currentTime = 0f;
		while (currentTime < duration)
		{
			float alpha = Mathf.Lerp(0f, 1f, currentTime / duration);
			notificationBackgroundImage.color = new Color(color.r, color.g, color.b, alpha);
			currentTime += Time.deltaTime;
			yield return null;
		}
		notificationBackgroundImage.color = new Color(color.r, color.g, color.b, 1f);
	}

	IEnumerator FadeOutBackground(float duration)
	{
		float startAlpha = notificationBackgroundImage.color.a;
		float currentTime = 0f;
		while (currentTime < duration)
		{
			float alpha = Mathf.Lerp(startAlpha, 0f, currentTime / duration);
			notificationBackgroundImage.color = new Color(
				notificationBackgroundImage.color.r,
				notificationBackgroundImage.color.g,
				notificationBackgroundImage.color.b,
				alpha
			);
			currentTime += Time.deltaTime;
			yield return null;
		}
		notificationBackgroundImage.color = new Color(
			notificationBackgroundImage.color.r,
			notificationBackgroundImage.color.g,
			notificationBackgroundImage.color.b,
			0f
		);
		notificationPanel.SetActive(false);
	}

	IEnumerator AnimateGIF(Sprite[] frames, float frameRate)
	{
		int currentFrame = 0;
		while (true)
		{
			notificationBackgroundImage.sprite = frames[currentFrame];
			currentFrame = (currentFrame + 1) % frames.Length;
			yield return new WaitForSeconds(frameRate);
		}
	}


}
