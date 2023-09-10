using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public GameObject notificationPanel; // Reference to the panel created
    public TMP_Text notificationText; // Reference to the text element inside the panel
    public Image notificationBackgroundImage; // Reference to the image element inside the panel

    public Color successColor = Color.green;
    public Color failureColor = Color.red;

    public Sprite[] successFrames; // Frames of success GIF
    public Sprite[] failureFrames; // Frames of failure GIF

    public float frameRate = 0.1f; // Seconds per frame for animation
    private int currentFrame = 0;

    public AudioClip successSound;
    public AudioClip failureSound;
    private AudioSource audioSource;

    public static NotificationManager Instance;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // If there's no AudioSource attached to the GameObject, add one
            audioSource = gameObject.AddComponent<AudioSource>();
        }
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
        if (successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
    }

    public void ShowFailureNotification(string message)
    {
        ShowNotification(message, failureColor);
        StartCoroutine(AnimateSprite(failureFrames));
        if (failureSound != null)
        {
            audioSource.PlayOneShot(failureSound);
        }
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
        StopAllCoroutines(); // Ensure no other animations are running
        StartCoroutine(AnimateSprite(successFrames)); // Start the success sprite animation
    }

    public void PlayFailureGif()
    {
        StopAllCoroutines(); // Ensure no other animations are running
        StartCoroutine(AnimateSprite(failureFrames)); // Start the failure sprite animation
    }
}
