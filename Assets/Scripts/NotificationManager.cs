using UnityEngine;
using UnityEngine.UI;

public class NotificationManager : MonoBehaviour
{
    public GameObject notificationPanel; // Reference to the panel created
    public Text notificationText; // Reference to the text element inside the panel

    public Color successColor = Color.green;
    public Color failureColor = Color.red;

    public static NotificationManager Instance;

    void Start()
    {
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
    }

    public void ShowFailureNotification(string message)
    {
        ShowNotification(message, failureColor);
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
        notificationPanel.SetActive(true);
    }

    public void HideNotification()
    {
        Debug.Log("Hiding notification panel.");
        notificationPanel.SetActive(false);
    }
}
