using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages the notifications for the agent.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    [Header("Notification Settings")]
    public Image notificationBackgroundImage;
    public Image successGradientBorderImage;
    public Image failureGradientBorderImage;

    [Header("GIF Settings")]
    public Sprite[] successFrames;
    public Sprite[] failureFrames;
    public float frameRate = 0.1f;
    private int currentFrame = 0;

    private Coroutine gifCoroutine;
    private string currentNotificationState = "None";

    public GameObject notificationPanel;
    public static NotificationManager Instance;

    void Start()
    {
        LoadFrames();
        HideNotification();
    }

    void LoadFrames()
    {
        successFrames = Resources.LoadAll<Sprite>("happyGIF");
        failureFrames = Resources.LoadAll<Sprite>("sadGIF");
    }

    private void Awake()
    {
        SingletonCheck();
    }

    void SingletonCheck()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowSuccessNotification()
    {
        ShowNotification(true);
        currentNotificationState = "Success";
    }

    public void ShowFailureNotification()
    {
        ShowNotification(false);
        currentNotificationState = "Failure";
    }

    private void ShowNotification(bool isSuccess)
    {
        Sprite[] framesToShow = isSuccess ? successFrames : failureFrames;
        Image gradientBorderToShow = isSuccess
            ? successGradientBorderImage
            : failureGradientBorderImage;

        notificationPanel.SetActive(true);
        successGradientBorderImage.gameObject.SetActive(isSuccess);
        failureGradientBorderImage.gameObject.SetActive(!isSuccess);

        StopPreviousAnimation();

        gifCoroutine = StartCoroutine(AnimateSprite(framesToShow));
    }

    void StopPreviousAnimation()
    {
        if (gifCoroutine != null)
        {
            StopCoroutine(gifCoroutine);
        }
    }

    /// <summary>
    /// Hides the notification. Freezing/unfreezing agents while a notification is shown is the
    /// arena's responsibility (see TrainingArena.OnAgentFinished).
    /// </summary>
    public void HideNotification()
    {
        notificationPanel.SetActive(false);
        successGradientBorderImage.gameObject.SetActive(false);
        failureGradientBorderImage.gameObject.SetActive(false);

        StopPreviousAnimation();

        currentFrame = 0;
        currentNotificationState = "None";
    }

    public string GetCurrentNotificationState()
    {
        return currentNotificationState;
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
}
